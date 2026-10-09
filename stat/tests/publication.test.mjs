import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {checkClaims,verifier,AUDIENCE} from '../worker/src/publisher-auth.mjs';
import {staticSource} from '../web/static-data.mjs';
import {emptyBlock} from '../shared/statistics.mjs';
import {readFile,readdir,mkdtemp,rm,writeFile} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {pathToFileURL} from 'node:url';
import {writeRelease} from '../scripts/public-release.mjs';
import {Miniflare,convertV4MiniflareOptions} from 'miniflare';
import {initializePublication} from '../worker/src/initialize.mjs';
import {mapLimited} from '../scripts/parallel.mjs';
import {packBlock,unpackBlock} from '../shared/block-codec.mjs';
import {encodePublished,decodePublished,publishedText} from '../shared/public-wire.mjs';
import {execFile} from 'node:child_process';
import {promisify} from 'node:util';
import {compactStatements} from '../scripts/compact-stat.mjs';
const now=1750000000;
const claims={iss:'https://token.actions.githubusercontent.com',aud:AUDIENCE,repository:'Jimmyzzt/AK_Exusiai',repository_id:'1341664712',repository_owner_id:'57975018',ref:'refs/heads/main',job_workflow_ref:'Jimmyzzt/AK_Exusiai/.github/workflows/stat-release.yml@refs/heads/main',event_name:'schedule',iat:now,nbf:now,exp:now+300};
test('initialization budget defers safely without reporting failure or publishing an empty website',async()=>{
 const directory=await mkdtemp(join(tmpdir(),'exusiai-deferred-')),output=join(directory,'output');
 try{
  const script=`globalThis.fetch=async url=>{const path=new URL(url).pathname;if(path.endsWith('manifest.json'))return new Response('',{status:404});if(path==='/oidc')return Response.json({value:'fixture'});if(path.endsWith('/initialize'))return Response.json({state:'ready'});if(path.endsWith('/status'))return Response.json({pending:true});if(path.endsWith('/tick'))return Response.json({state:'budget'});throw new Error('unexpected endpoint');};await import(${JSON.stringify(new URL('../scripts/publish-data.mjs',import.meta.url).href)});`;
  const {stdout}=await promisify(execFile)(process.execPath,['--input-type=module','-e',script],{env:{...process.env,ACTIONS_ID_TOKEN_REQUEST_URL:'https://fixture.test/oidc',ACTIONS_ID_TOKEN_REQUEST_TOKEN:'fixture',GITHUB_OUTPUT:output,STAT_BOOTSTRAP:'false'}});
  assert.match(stdout,/publication_deferred/);assert.equal(await readFile(output,'utf8'),'stats_changed=false\npublish_ready=false\n');
  const workflow=await readFile('../.github/workflows/stat-release.yml','utf8');
  assert.equal((workflow.match(/if: steps\.statistics\.outputs\.publish_ready == 'true'/g)||[]).length,3);
 }finally{await rm(directory,{recursive:true,force:true});}
});
test('bounded parallel I/O preserves order and waits for in-flight requests after failure',async()=>{
 let active=0,peak=0;
 const result=await mapLimited([0,1,2,3,4],3,async n=>{active++;peak=Math.max(peak,active);await new Promise(r=>setTimeout(r,10*(5-n)));active--;return n*2;});
 assert.deepEqual(result,[0,2,4,6,8]);assert.equal(peak,3);
 let completed=false,started=0;
 await assert.rejects(mapLimited([0,1,2,3],2,async n=>{started++;if(n===0)throw new Error('failure');await new Promise(r=>setTimeout(r,20));completed=true;}),/failure/);
 assert.equal(completed,true);assert.equal(started,2);
});
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
test('compact sums preserve signed values, all views and the highest field; public blocks reject secret and unused data',()=>{
 const block=emptyBlock(),values=Array(28).fill(0);values[0]=1;values[13]=-0.0123456789;values[27]=123;
 block.entities=[0,1,2,3,4].flatMap(scope=>[0,1].map(split=>[scope,split,'AK_EXUSIAI_CARD_CHARGING_MODE',3,1,...values]));
 const packed=packBlock(block);assert.equal(packed.rows.length,1);assert.deepEqual(unpackBlock(packed),block);
 const duplicate={...block,entities:[block.entities[0],...block.entities]};assert.deepEqual(unpackBlock(packBlock(duplicate)),duplicate);
 assert.ok(JSON.stringify(packed).length<JSON.stringify(block).length/2);
 assert.throws(()=>unpackBlock({...packed,owner_hash:'secret'}));
 const first=release(),b={...first.bundle,source_revision:{statistics:1,catalog:1},cells:['a','b'].map(c=>({key:c.repeat(64),revision:1,dims:['2026-10-09','v','r',0,1,'Standard',0,0,0],projection:[],block}))};
 const wire=encodePublished(b);assert.equal(wire.blocks.length,1);assert.deepEqual(decodePublished(wire),b);
 assert.throws(()=>decodePublished({...wire,blocks:[...wire.blocks,packed]}),/Unused/);
 assert.throws(()=>decodePublished({...wire,blocks:[{...packed,payload:'secret'}]}));
});
test('compact public transport remains integrity checked and restores offline from IndexedDB cache',async()=>{
 const b=release().bundle,bytes=Buffer.from(publishedText(b)),sha256=createHash('sha256').update(bytes).digest('hex');
 const manifest={...release().manifest,schema_version:2,file:'statistics-'+sha256+'.json',sha256,bytes:bytes.length};
 let saved=null,offline=false;
 const cache={get:async()=>saved,set:async value=>{saved=value;}};
 const fetcher=async url=>{if(offline)throw new Error('offline');return String(url).endsWith('manifest.json')?Response.json(manifest):new Response(bytes);};
 const options={fetcher,cache,base:new URL('https://pages.test/data/')};
 assert.deepEqual((await staticSource(options).refresh()).bundle,b);
 offline=true;const result=await staticSource(options).refresh();assert.equal(result.state,'offline');assert.deepEqual(result.bundle,b);
});
test('storage conversion changes only matching aggregate revisions and preserves newer writes',async()=>{
 const mf=new Miniflare(convertV4MiniflareOptions({modules:true,script:'export default {fetch(){return new Response("test")}}',compatibilityDate:'2026-10-06',d1Databases:{DB:'compaction-test'}}));
 try{
  const db=await mf.getD1Database('DB'),block=emptyBlock();
  const values=Array(28).fill(0);values[0]=1;values[13]=-0.25;
  block.entities=[0,1].map(scope=>[scope,0,'AK_EXUSIAI_CARD_CHARGING_MODE',0,-1,...values]);
  const b={...release().bundle,cells:['a','b'].map(c=>({key:c.repeat(64),revision:1,dims:['2026-10-09','v','r',0,1,'Standard',0,0,0],projection:[],block}))};
  await db.prepare('CREATE TABLE stat_cells(key TEXT PRIMARY KEY,revision INTEGER,payload TEXT)').run();
  await db.batch(b.cells.map((cell,i)=>db.prepare('INSERT INTO stat_cells VALUES(?,?,?)').bind(cell.key,i+1,JSON.stringify(cell.block))));
  const plan=compactStatements(b);assert.ok(plan.summary.compact_payload_bytes<plan.summary.expanded_payload_bytes);
  await db.batch(plan.statements.map(sql=>db.prepare(sql)));
  const rows=(await db.prepare('SELECT key,payload FROM stat_cells ORDER BY key').all()).results;
  assert.equal(JSON.parse(rows[0].payload).codec,1);assert.deepEqual(unpackBlock(JSON.parse(rows[0].payload)),block);
  assert.equal(rows[1].payload,JSON.stringify(block));
 }finally{await mf.dispose();}
});
test('website publication retains current and previous data; invalid retention cannot replace the last manifest',async()=>{
 const directory=await mkdtemp(join(tmpdir(),'exusiai-publication-')),folder=pathToFileURL(directory+'/');
 try{
  const first=release(),second=release(2);
  const one=await writeRelease(first.bundle,folder,null);
  const two=await writeRelease(second.bundle,folder,{manifest:one,bundle:first.bundle});
  assert.equal(two.previous.sha256,one.sha256);
  assert.deepEqual((await readdir(folder)).sort(),['manifest.json',one.file,two.file].sort());
  assert.deepEqual(await writeRelease(second.bundle,folder,{manifest:two,bundle:second.bundle}),two);
  const before=await readFile(new URL('manifest.json',folder),'utf8');
  await writeFile(new URL(one.file,folder),'corrupted');
  await assert.rejects(writeRelease(second.bundle,folder,{manifest:two,bundle:second.bundle}),/integrity/);
  assert.equal(await readFile(new URL('manifest.json',folder),'utf8'),before);
 }finally{await rm(directory,{recursive:true,force:true});}
});
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
