import {applySchema} from './helpers/schema.mjs';
import {test,before,after} from 'node:test';
import assert from 'node:assert/strict';
import {readFile,readdir,writeFile} from 'node:fs/promises';
import {build} from 'esbuild';
import {Miniflare,convertV4MiniflareOptions} from 'miniflare';
import {contribution,emptyBlock,mergeBlock} from '../shared/statistics.mjs';
import {selectPublic,validateBundle,publicCell} from '../shared/public-data.mjs';
import {filters} from '../worker/src/validation.mjs';
import {encodePublished,decodePublished,publishedText} from '../shared/public-wire.mjs';
import {readFacts,processOne,status,exportPage,metered} from '../worker/src/incremental.mjs';
let mf,db,reference,optimized;const card='AK_EXUSIAI_CARD_CHARGING_MODE',other='AK_EXUSIAI_CARD_READY_FOR_ACTION',token='1'.repeat(64);
async function module(path){const b=await build({entryPoints:[path],bundle:true,write:false,format:'esm',platform:'node'});return import('data:text/javascript;base64,'+Buffer.from(b.outputFiles[0].text).toString('base64'));}
before(async()=>{
 const w=await build({entryPoints:['worker/src/index.ts'],bundle:true,write:false,format:'esm',platform:'browser'});
 mf=new Miniflare(convertV4MiniflareOptions({modules:true,script:w.outputFiles[0].text,compatibilityDate:'2026-10-06',d1Databases:{DB:'incremental-tests'},bindings:{UPLOAD_ENABLED:'true'},serviceBindings:{ASSETS:async()=>new Response('static')},ratelimits:{UPLOAD_LIMIT:{namespace_id:'22',simple:{limit:1000,period:60}}}}));
 db=await mf.getD1Database('DB');
 await applySchema(db);
 ({calculate:reference}=await module('tests/fixtures/legacy-analytics.ts'));({calculate:optimized}=await module('worker/src/analytics.ts'));
 for(let i=0;i<30;i++){
  const run={schema:'exusiai.run.v1',id:i.toString(16).padStart(64,'0'),day:'2026-10-08',version:i%2?'1.1.0':'1.2.0',revision:'r1',game_version:'0.111.0',ascension:i%3,players:i%4===0?2:1,mode:'Standard',victory:i%3!==0,abandoned:i%5===0&&i%3===0,floor:48,duration:100,mods:[{id:'AK_Exusiai',version:'1'},{id:'STS2-RitsuLib',version:'1'},...((i%2)?[{id:'ModA',version:'1'}]:[]),...((i%3)?[{id:'ModB',version:'1'}]:[]),...((i%4)?[{id:'ModC',version:'1'}]:[])],entities:[{id:card,act:0,owned:1,offered:4,picked:3,obtained:2,floor_sum:5,upgraded:1,removed:1},{id:card,act:1,owned:0,offered:2,picked:2,obtained:0,floor_sum:0,upgraded:0,removed:0},{id:card,act:2,owned:0,offered:2,picked:1,obtained:0,floor_sum:0,upgraded:0,removed:0}]};
  if(i>=6){
   run.schema='exusiai.run.v2';run.details={win3:i%3!==0,acts:[1,2,3,4].slice(0,i%3+2).map(act=>({act,floors:12,completed:act<=3,snapshot_known:true,deck:[{id:card,variant:0},{id:card,variant:1}]})),items:[{id:card,act:1,variant:0,offered:2,picked:2,obtained:2,floor_sum:5,upgraded:0,removed:0},{id:card,act:2,variant:1,offered:2,picked:1,obtained:0,floor_sum:0,upgraded:1,removed:1}],offers:[1,1,2,2].map((act,j)=>({id:card,act,floor:j%2+1,position:(act-1)*12+j%2+1,variant:act-1,picked:j!==3})),fights:[{act:2,floor:5,position:17,encounter:'TEST',damage:8,turns:3}]};
   run.ascension=7;run.players=i===7?2:1;if(i===22)run.mode='Daily';if(i===8){run.victory=false;run.abandoned=true;}run.details.fights[0].damage=6+i%4;run.floor=run.details.acts.length*12;run.entities=run.entities.map(e=>({...e}));run.entities[0].floor_sum=5;
  }
  if(i===0)run.day='2026-10-07';if(i===4)run.mode='Custom';
  const response=await mf.dispatchFetch('https://x/api/upload',{method:'POST',headers:{Authorization:'Bearer '+token,'Content-Type':'application/json'},body:JSON.stringify(run)});
  assert.equal(response.status,200,'fixture upload '+i+': '+await response.text());
 }
 await db.prepare("UPDATE mod_catalog SET primary_tag='acts' WHERE id='ModC'").run();
});
after(()=>mf.dispose());
function compare(a,b){
 for(const k of ['runs','wins','win_samples','abandoned','floor_samples','detailed_runs','hold_samples','average_floor','average_duration'])assert.ok(Math.abs((a.overview[k]||0)-(b.overview[k]||0))<1e-8,k);
 const map=x=>new Map(x.entities.map(e=>[[e.id,e.act,e.variant].join(':'),e]));const am=map(a),bm=map(b);
 assert.deepEqual([...am.keys()].sort(),[...bm.keys()].sort());
 for(const [key,x] of am)for(const [field,v] of Object.entries(x))if(!['id','act','variant','presence'].includes(field)){const w=bm.get(key)[field];if(v==null)assert.equal(w??null,null,key+' '+field);else if(typeof v==='number')assert.ok(Math.abs(v-(w??0))<1e-8,key+' '+field+' '+v+' != '+w);}
}
async function bundle(){
 const s=await status(db),p=await exportPage(db,{after:0,through:s.source_revision.statistics,catalog:true});let cells=p.cells,next=p.next;
 while(next){const q=await exportPage(db,{after:0,through:s.source_revision.statistics,...next});cells.push(...q.cells);next=q.next;}
 const live=cells.filter(c=>!c.deleted).map(publicCell),versions=[...new Map(live.filter(c=>!c.projection.length).map(c=>[c.dims[1]+':'+c.dims[2],{version:c.dims[1],revision:c.dims[2]}])).values()];
 return validateBundle({schema_version:1,generated_at:Date.now(),source_revision:s.source_revision,model:'personal-logistic-floor-v2',algorithm:1,pair_mods:s.pair_mods,mods:p.mods,versions,cells:live});
}
test('reference SQL, optimized SQL, incremental rollups and local filters agree',async()=>{
 while((await processOne(db,{bootstrap:true})).state==='processed'){}
 const b=await bundle();
 const wire=encodePublished(b),restored=decodePublished(wire);
 assert.deepEqual(restored,b);assert.equal(publishedText(restored),JSON.stringify(wire));
 assert.ok(JSON.stringify(wire).length<JSON.stringify(b).length);
 for(const query of ['','party=all&abandoned=loss','mode=all&party=all&abandoned=loss','mode=Daily','party=multi','to=2026-10-07','act=all&party=all&abandoned=loss','act=1','act=2','act=3','split=1','act=all&split=1&abandoned=loss','exclude=ModA','exclude=ModA&exclude=ModB','tag_mode=black&tags=acts','tag_mode=white&tags=acts&tags=untagged','version=1.1.0','ascension=2','from=2026-10-09','exclude=AK_Exusiai&exclude=STS2-RitsuLib']){
  const f=filters(new URL('https://x/api/stats?'+query)),a=await reference(db,f),o=await optimized(db,f),c=selectPublic(restored,f);try{compare(a,o);}catch(e){throw new Error('SQL '+query,{cause:e});}try{compare(a,c);}catch(e){throw new Error('LOCAL '+query,{cause:e});}
 }
});
test('idempotent drain, replayable rebuild, transaction failure and label invalidation',async()=>{
 const first=await bundle();assert.equal((await processOne(db)).state,'idle');
 await db.prepare('INSERT INTO stat_dirty(run_id) SELECT id FROM runs WHERE true ON CONFLICT(run_id) DO UPDATE SET generation=generation+1').run();
 let injected=false;
 await assert.rejects(processOne(db,{now:Date.now()+900001,beforeCommit:async()=>{injected=true;await db.exec("CREATE TRIGGER fail_cells BEFORE UPDATE ON stat_cells BEGIN SELECT RAISE(ABORT,'failure'); END;");}}));
 assert.ok(injected);assert.ok((await status(db)).pending);await db.exec('DROP TRIGGER fail_cells; UPDATE stat_state SET cooldown=0;');
 while((await processOne(db,{bootstrap:true})).state==='processed'){}
 compare(selectPublic(first,filters(new URL('https://x'))),selectPublic(await bundle(),filters(new URL('https://x'))));
 await db.prepare("UPDATE mod_catalog SET primary_tag='qol' WHERE id='ModC'").run();assert.ok((await status(db)).pending);
 while((await processOne(db,{bootstrap:true})).state==='processed'){}
 const f=filters(new URL('https://x?tags=qol'));compare(await reference(db,f),selectPublic(await bundle(),f));
});
test('public whitelist rejects secrets, unsupported filters do not fall back to D1',async()=>{
 const b=await bundle();assert.throws(()=>validateBundle({...b,payload:'secret'}));
 assert.throws(()=>validateBundle({...b,cells:[{...b.cells[0],owner_hash:'secret'}]}));
 assert.throws(()=>selectPublic(b,{...filters(new URL('https://x')),exclude:['ModA','ModB','ModC']}));
 assert.throws(()=>selectPublic(b,{...filters(new URL('https://x')),exclude:['ModA','not-in-pair-policy']}));
 const serialized=JSON.stringify(b);for(const secret of ['owner_hash','run_id','payload','synthetic-owner',token])assert.equal(serialized.includes(secret),false);
});
import {beginWork,workCells,stageWork,commitWork} from '../worker/src/publication.mjs';
import {mergeWork} from '../shared/statistics.mjs';
import {packBlock} from '../shared/block-codec.mjs';
test('Actions work protocol stages privately, deduplicates chunks, fences commits and recovers label races',async()=>{
 const id=(5).toString(16).padStart(64,'0');
 await db.prepare('INSERT INTO stat_dirty(run_id) VALUES(?) ON CONFLICT(run_id) DO UPDATE SET generation=generation+1').bind(id).run();
 const start=await status(db),job=await beginWork(db,{bootstrap:true});assert.equal(job.state,'work');
 assert.equal((await beginWork(db,{bootstrap:true})).state,'busy');
 const work=job.work;work.existing=[];for(let offset=0;offset<work.keys.length;offset+=8)work.existing.push(...(await workCells(db,work.nonce,offset)).cells);
 const updates=mergeWork(work),allowed=new Set([card,other]);
 assert.ok(updates.length>1);
 await stageWork(db,{nonce:work.nonce,ordinal:0,updates:updates.slice(0,1)},allowed);
 await stageWork(db,{nonce:work.nonce,ordinal:0,updates:updates.slice(0,1)},allowed);
 assert.equal((await status(db)).source_revision.statistics,start.source_revision.statistics);
 await assert.rejects(commitWork(db,work.nonce),/Incomplete/);
 for(let i=1;i<updates.length;i++)await stageWork(db,{nonce:work.nonce,ordinal:i,updates:[{...updates[i],block:packBlock(updates[i].block)}]},allowed);
 await db.prepare("UPDATE mod_catalog SET primary_tag='acts' WHERE id='ModC'").run();
 assert.equal((await commitWork(db,work.nonce)).state,'processed');assert.ok((await status(db)).pending);
 while((await processOne(db,{bootstrap:true})).state==='processed'){}
 const f=filters(new URL('https://x?party=all&abandoned=loss'));compare(await reference(db,f),selectPublic(await bundle(),f));
 await assert.rejects(commitWork(db,work.nonce),/Expired/);
 const b=await bundle();assert.throws(()=>validateBundle({...b,nonce:work.nonce,next:work.next}));if(process.env.STAT_PREVIEW_OUTPUT)await writeFile(process.env.STAT_PREVIEW_OUTPUT,JSON.stringify(b));
});
test('removal reverses the saved contribution and public Mod usage without rebuilding every run',async()=>{
 const id=(5).toString(16).padStart(64,'0'),uses=(await db.prepare("SELECT uses FROM mod_catalog WHERE id='ModA'").first()).uses;
 await db.prepare('DELETE FROM runs WHERE id=?').bind(id).run();
 assert.equal((await db.prepare("SELECT uses FROM mod_catalog WHERE id='ModA'").first()).uses,uses-1);
 assert.ok((await status(db)).pending);
 while((await processOne(db,{bootstrap:true})).state==='processed'){}
 assert.equal(await db.prepare('SELECT run_id FROM stat_facts WHERE run_id=?').bind(id).first(),null);
 const f=filters(new URL('https://x?party=all&abandoned=loss'));compare(await reference(db,f),selectPublic(await bundle(),f));
});
test('publication pins its captured catalog; queued tags/uploads do not invalidate unchanged aggregate pages',async()=>{
 const start=await status(db),through=start.source_revision.statistics,catalogRevision=start.source_revision.catalog;
 const first=await exportPage(db,{through,catalog:true,catalogRevision,bootstrap:true});assert.equal(first.changed,undefined);assert.ok(first.next);
 await db.prepare('UPDATE stat_state SET catalog_revision=catalog_revision+1 WHERE id=1').run();
 await db.prepare("INSERT INTO stat_dirty(run_id) VALUES('queued-after-capture')").run();
 const next=await exportPage(db,{through,...first.next,catalogRevision,bootstrap:true});
 assert.equal(next.changed,undefined);assert.deepEqual(next.source_revision,start.source_revision);
 assert.equal((await exportPage(db,{through,catalog:true,catalogRevision,bootstrap:true})).changed,true);
 while((await processOne(db,{bootstrap:true})).state==='processed'){}
 assert.equal((await exportPage(db,{through,...first.next,catalogRevision,bootstrap:true})).changed,true);
});
