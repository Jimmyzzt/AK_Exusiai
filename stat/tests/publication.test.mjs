import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {checkClaims,verifier,AUDIENCE} from '../worker/src/publisher-auth.mjs';
import {staticSource} from '../web/static-data.mjs';
import {emptyBlock} from '../shared/statistics.mjs';
import {readFile,readdir} from 'node:fs/promises';
import {Miniflare,convertV4MiniflareOptions} from 'miniflare';
import {initializePublication} from '../worker/src/initialize.mjs';
const now=1750000000;
const claims={iss:'https://token.actions.githubusercontent.com',aud:AUDIENCE,repository:'Jimmyzzt/AK_Exusiai',repository_id:'1341664712',repository_owner_id:'57975018',ref:'refs/heads/main',job_workflow_ref:'Jimmyzzt/AK_Exusiai/.github/workflows/stat-release.yml@refs/heads/main',event_name:'schedule',iat:now,nbf:now,exp:now+300};
test('fixed additive initialization is atomic, concurrent-safe and journaled once without changing original records',async()=>{
 const mf=new Miniflare(convertV4MiniflareOptions({modules:true,script:'export default {fetch(){return new Response("test")}}',compatibilityDate:'2026-10-06',d1Databases:{DB:'initialization-test'}}));
 try{
  const db=await mf.getD1Database('DB');
  for(const name of (await readdir('worker/migrations')).sort().filter(n=>!n.startsWith('0003'))){
   const sql=(await readFile('worker/migrations/'+name,'utf8')).replace(/--[^\n]*/g,'');
   for(const statement of sql.match(/\s*CREATE TRIGGER[\s\S]*?END;|[^;]+;/g)||[])await db.prepare(statement.trim()).run();
  }
  await db.prepare('CREATE TABLE d1_migrations(id INTEGER PRIMARY KEY AUTOINCREMENT,name TEXT UNIQUE,applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL)').run();
  await db.prepare("INSERT INTO cohorts(id,day,version,revision,ascension,players,mode,abandoned) VALUES('c','2026-10-08','v','r',0,1,'Standard',0)").run();
  await db.prepare("INSERT INTO runs VALUES('r','private','2026-10-08','v','r','g',0,1,'Standard',1,0,48,100,0,'original','c','n')").run();
  const canonical=(await readFile('worker/migrations/0003_incremental.sql','utf8')).replace(/--[^\n]*/g,'').match(/\s*CREATE TRIGGER[\s\S]*?END;|[^;]+;/g).map(s=>s.trim());
  assert.deepEqual(JSON.parse(await readFile('worker/migration-0003.json','utf8')),canonical);
  const failed=new Proxy(db,{get(target,key){if(key==='batch')return statements=>target.batch([...statements,target.prepare('INSERT INTO missing_table VALUES(1)')]);const value=target[key];return typeof value==='function'?value.bind(target):value;}});
  await assert.rejects(initializePublication(failed));
  assert.equal(await db.prepare("SELECT name FROM sqlite_master WHERE name='stat_state'").first(),null);
  await Promise.all([initializePublication(db),initializePublication(db)]);
  assert.equal((await db.prepare("SELECT COUNT(*) n FROM d1_migrations WHERE name='0003_incremental.sql'").first()).n,1);
  assert.equal((await db.prepare('SELECT generation FROM stat_dirty WHERE run_id=?').bind('r').first()).generation,1);
  assert.deepEqual((await db.prepare('SELECT id,payload FROM runs').all()).results,[{id:'r',payload:'original'}]);
  await db.prepare('DELETE FROM stat_dirty').run();
  assert.equal((await initializePublication(db)).state,'ready');
  assert.equal((await db.prepare('SELECT COUNT(*) n FROM stat_dirty').first()).n,0);
 }finally{await mf.dispose();}
});
test('OIDC verifies RSA signature and restricts repository, reusable workflow, branch and events',async()=>{
 const keys=await crypto.subtle.generateKey({name:'RSASSA-PKCS1-v1_5',modulusLength:2048,publicExponent:new Uint8Array([1,0,1]),hash:'SHA-256'},true,['sign','verify']);
 const jwk={...await crypto.subtle.exportKey('jwk',keys.publicKey),kid:'test-key',alg:'RS256',use:'sig'};
 const verify=verifier(async()=>Response.json({keys:[jwk]}));
 const encode=x=>Buffer.from(JSON.stringify(x)).toString('base64url');
 async function request(payload){const content=encode({typ:'JWT',alg:'RS256',kid:'test-key'})+'.'+encode(payload),signature=Buffer.from(await crypto.subtle.sign('RSASSA-PKCS1-v1_5',keys.privateKey,new TextEncoder().encode(content))).toString('base64url');return new Request('https://x',{headers:{Authorization:'Bearer '+content+'.'+signature}});}
 assert.deepEqual(await verify(await request(claims),now),claims);
 for(const c of [{...claims,exp:now-1},{...claims,repository_id:'other'},{...claims,repository_owner_id:'other'},{...claims,ref:'refs/pull/1/merge'},{...claims,event_name:'pull_request_target'},{...claims,job_workflow_ref:'attacker'},{...claims,aud:'wrong'}])assert.equal(await verify(await request(c),now),null);
 const tampered=await request(claims);tampered.headers.set('Authorization',tampered.headers.get('Authorization').slice(0,-4)+'AAAA');assert.equal(await verify(tampered,now),null);
 assert.equal(checkClaims({...claims,exp:now+7200},now),false);
});
function release(revision=1){
 const bundle={schema_version:1,generated_at:revision,source_revision:{statistics:revision,catalog:1},model:'personal-logistic-floor-v2',algorithm:1,pair_mods:[],mods:[],versions:[],cells:[]};
 const bytes=Buffer.from(JSON.stringify(bundle)),sha256=createHash('sha256').update(bytes).digest('hex');
 return {bundle,manifest:{schema_version:1,generated_at:revision,source_revision:bundle.source_revision,model:bundle.model,previous:null,file:'statistics-'+sha256+'.json',sha256,bytes:bytes.length},bytes};
}
test('manifest-only refresh, content integrity, offline startup and malformed update keep the last release',async()=>{
 const first=release(),second=release(2);let current=first,broken=false,offline=false,downloads=0,saved=null;
 const cache={get:async()=>saved,set:async v=>{saved=v;}};
 const fetcher=async url=>{if(offline)throw new Error('offline');if(String(url).endsWith('manifest.json'))return Response.json(current.manifest);downloads++;return new Response(broken?'corrupted':current.bytes);};
 const source=staticSource({fetcher,cache,base:new URL('https://pages.test/data/')});
 assert.equal((await source.refresh()).bundle.generated_at,1);assert.equal(downloads,1);
 assert.equal((await source.refresh()).state,'unchanged');assert.equal(downloads,1);
 current=second;broken=true;assert.equal((await source.refresh()).state,'offline');assert.equal(saved.bundle.generated_at,1);
 broken=false;assert.equal((await source.refresh()).bundle.generated_at,2);
 offline=true;const restored=staticSource({fetcher,cache,base:new URL('https://pages.test/data/')});assert.equal((await restored.refresh()).bundle.generated_at,2);
});
test('public raw-record and accidental root fields fail schema checks before cache replacement',async()=>{
 const r=release(),bad={...r.bundle,runs:[{owner_hash:'secret'}]},bytes=Buffer.from(JSON.stringify(bad)),sha256=createHash('sha256').update(bytes).digest('hex');
 const source=staticSource({fetcher:async url=>String(url).endsWith('manifest.json')?Response.json({...r.manifest,file:'statistics-'+sha256+'.json',sha256,bytes:bytes.length}):new Response(bytes),cache:{get:async()=>null,set:async()=>assert.fail('invalid release saved')},base:new URL('https://pages.test/data/')});
 await assert.rejects(source.refresh(),/schema/);
});
test('overlapping manual and automatic refreshes share one manifest and data request',async()=>{
 const r=release();let requests=0,continueManifest;
 const gate=new Promise(resolve=>{continueManifest=resolve;});
 const source=staticSource({fetcher:async url=>{requests++;if(String(url).endsWith('manifest.json')){await gate;return Response.json(r.manifest);}return new Response(r.bytes);},cache:{get:async()=>null,set:async()=>{}},base:new URL('https://pages.test/data/')});
 const first=source.refresh(),second=source.refresh();assert.equal(first,second);
 continueManifest();assert.equal((await second).bundle.generated_at,1);assert.equal(requests,2);
 assert.equal((await source.refresh()).state,'unchanged');assert.equal(requests,3);
});
import {mergeWork} from '../shared/statistics.mjs';
test('an algorithm revision removes old cell contributions before introducing the new revision',()=>{
 const dims=['2026-10-08','v','r',0,1,'Standard',0,0,0],oldBlock=emptyBlock(),nextBlock=emptyBlock();
 oldBlock.overview[0][0]=1;nextBlock.overview[0][0]=2;
 const old={algorithm:1,dims,mods:[],pair_mods:[],block:oldBlock},next={algorithm:2,dims,mods:[],pair_mods:[],block:nextBlock};
 const updates=mergeWork({revision:2,old,next,keys:[{key:'1'.repeat(64),algorithm:1,dims,projection:[]},{key:'2'.repeat(64),algorithm:2,dims,projection:[]}],existing:[{key:'1'.repeat(64),payload:JSON.stringify(oldBlock)}]});
 assert.equal(updates[0].block.overview[0][0],0);assert.equal(updates[1].block.overview[0][0],2);
});
