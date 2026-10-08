import {applySchema} from './helpers/schema.mjs';
import {test,before,after} from 'node:test';
import assert from 'node:assert/strict';
import {readFile,readdir} from 'node:fs/promises';
import {build} from 'esbuild';
import {Miniflare,convertV4MiniflareOptions} from 'miniflare';
import {validateRun,filters,InvalidInput} from '../worker/src/validation.mjs';
let mf,db,calculate,enrichMods,staticPage;
const token='1'.repeat(64), token2='2'.repeat(64);
const card='AK_EXUSIAI_CARD_CHARGING_MODE';
function run(id='a',options={}) {return {schema:'exusiai.run.v1',id:id.repeat(64),day:'2026-10-06',version:'1.1.0',revision:'cards-v1.3-stat-v1',game_version:'0.111.0',ascension:10,players:1,mode:'Standard',victory:true,abandoned:false,floor:48,duration:1800,mods:[{id:'AK_Exusiai',version:'1.1.0'},{id:'STS2-RitsuLib',version:'0.6.5'}],entities:[{id:card,act:0,owned:1,offered:4,picked:2,obtained:2,floor_sum:14,upgraded:1,removed:0},{id:card,act:1,owned:0,offered:4,picked:2,obtained:0,floor_sum:0,upgraded:0,removed:0}],...options};}
const send=(path,body,credential=token)=>mf.dispatchFetch('https://test.local'+path,{method:'POST',headers:{Authorization:'Bearer '+credential,'Content-Type':'application/json'},body:JSON.stringify(body)});
before(async()=>{
  const result=await build({entryPoints:['worker/src/index.ts'],bundle:true,write:false,format:'esm',platform:'browser',target:'es2022'});
  ({staticPage}=await import('data:text/javascript;base64,'+Buffer.from(result.outputFiles[0].text).toString('base64')));
  mf=new Miniflare(convertV4MiniflareOptions({modules:true,script:result.outputFiles[0].text,compatibilityDate:'2026-10-06',d1Databases:{DB:'test-db'},bindings:{UPLOAD_ENABLED:'true'},serviceBindings:{ASSETS:async()=>new Response('static')},ratelimits:{UPLOAD_LIMIT:{namespace_id:'1001',simple:{limit:1000,period:60}}}}));
  db=await mf.getD1Database('DB');
 await applySchema(db);
  const analytics=await build({entryPoints:['worker/src/analytics.ts'],bundle:true,write:false,format:'esm',platform:'node'});
  ({calculate}=await import('data:text/javascript;base64,'+Buffer.from(analytics.outputFiles[0].text).toString('base64')));
  const steam=await build({entryPoints:['worker/src/steam.ts'],bundle:true,write:false,format:'esm',platform:'node'});
  ({enrichMods}=await import('data:text/javascript;base64,'+Buffer.from(steam.outputFiles[0].text).toString('base64')));
  await calculate(db,filters(new URL('https://x/api/stats')));
});
after(async()=>{await mf?.dispose();});
test('no raw identity or out-of-catalog fields accepted',()=>{
  assert.equal(validateRun(run(),new Set([card]),'2026-10-06').entities[0].picked,2);
  assert.throws(()=>validateRun({...run(),steam_id:'secret'},new Set([card])),InvalidInput);
  const v=run();v.entities[0].id='STRIKE';assert.throws(()=>validateRun(v,new Set([card])),InvalidInput);
  const mismatch=run();mismatch.entities[1].picked=1;assert.throws(()=>validateRun(mismatch,new Set([card])),InvalidInput);
  assert.throws(()=>filters(new URL('https://x/api/stats?exclude='+('x'.repeat(200)))),InvalidInput);
  assert.equal(filters(new URL('https://x/api/stats?ascension=10')).ascension,'10');
  assert.throws(()=>filters(new URL('https://x/api/stats?ascension=11')),InvalidInput);
});
test('retired public statistics route cannot create a snapshot or trigger a D1 calculation',async()=>{for(const method of ['GET','POST'])assert.equal((await mf.dispatchFetch('https://test.local/api/stats?ascension=0',{method})).status,410);assert.equal((await db.prepare('SELECT COUNT(*) n FROM snapshots').first()).n,0);assert.equal((await mf.dispatchFetch('https://test.local/api/publication/status')).status,401);});
test('persist before ack, retry deduplicates, acts count player-runs once, mod exclusions and abandon switch',async()=>{
  assert.equal((await send('/api/upload',run())).status,200);
  assert.equal((await (await send('/api/upload',run())).json()).duplicate,true);
  assert.equal((await send('/api/upload',run('a'),token2)).status,422);
  assert.equal((await send('/api/upload',run('b',{victory:false,abandoned:true,mods:[...run().mods,{id:'BalanceMod',version:'1'}]}))).status,200);
  assert.equal((await send('/api/upload',run('c',{players:2,victory:false}),token2)).status,200);
  assert.equal((await send('/api/upload',run('d',{victory:false}),token2)).status,200);
  const solo=await calculate(db,filters(new URL('https://test.local/api/stats')));
  assert.equal(solo.overview.runs,2);assert.equal(solo.overview.wins,1);
  const act=solo.entities.find(x=>x.act===1);assert.equal(act.picked,4);assert.equal(act.picked_runs,2);assert.equal(act.picked_wins,1);
  const all=await calculate(db,filters(new URL('https://test.local/api/stats?party=all&abandoned=loss')));
  assert.equal(all.overview.runs,4);assert.equal(all.overview.wins,1);assert.equal(all.overview.abandoned,1);
  const excluded=await calculate(db,filters(new URL('https://test.local/api/stats?party=all&abandoned=loss&exclude=BalanceMod')));
  assert.equal(excluded.overview.runs,3);
  const revision=await calculate(db,filters(new URL('https://test.local/api/stats?revision=other')));assert.equal(revision.overview.runs,0);
  const serialized=JSON.stringify(all);for(const secret of ['owner_hash','received_at','payload',run().id,token])assert.equal(serialized.includes(secret),false);
  assert.equal((await mf.dispatchFetch('https://test.local/api/runs')).status,404);
  assert.equal((await db.prepare('SELECT COUNT(*) n FROM runs').first()).n,4);
});
test('removed deletion and identity routes cannot modify uploaded data',async()=>{
  assert.equal((await send('/api/delete',{confirm:'delete-my-statistics'})).status,404);
  assert.equal((await mf.dispatchFetch('https://test.local/api/identity',{headers:{Authorization:'Bearer '+token}})).status,404);
  assert.equal((await db.prepare('SELECT COUNT(*) n FROM runs').first()).n,4);
});
test('failed D1 batch rolls back run and all rollups; invalid body never stores partial data',async()=>{
  await db.exec("CREATE TRIGGER force_failure BEFORE INSERT ON entities BEGIN SELECT RAISE(ABORT,'test failure'); END;");
  assert.equal((await send('/api/upload',run('e'),token2)).status,503);
  assert.equal(await db.prepare('SELECT id FROM runs WHERE id=?').bind('e'.repeat(64)).first(),null);
  await db.exec('DROP TRIGGER force_failure;');
  assert.equal((await send('/api/upload',{...run('e'),entities:[{id:'forbidden'}]},token2)).status,422);
  assert.equal((await send('/api/upload',run('e'),token2)).status,200);
  assert.equal((await db.prepare('SELECT SUM(runs) n FROM cohorts').first()).n,5);
});
test('concurrent retries persist one coherent payload',async()=>{
  const owner='3'.repeat(64), first=run('f'), second=run('f');
  second.entities[0].offered=6;second.entities[1].offered=6;second.entities[1].act=2;
  const responses=await Promise.all(Array.from({length:8},(_,i)=>send('/api/upload',i%2?first:second,owner)));
  for(const response of responses)assert.equal(response.status,200);
  const stored=JSON.parse((await db.prepare('SELECT payload FROM runs WHERE id=?').bind(first.id).first()).payload);
  const rows=(await db.prepare('SELECT entity_id id,act,owned,offered,picked,obtained,floor_sum,upgraded,removed FROM entities WHERE run_id=? ORDER BY act').bind(first.id).all()).results;
  assert.deepEqual(rows,stored.entities);
  assert.equal((await db.prepare('SELECT SUM(runs) n FROM cohorts').first()).n,6);
});
function detailed(id=16){
 const m=(act,variant,picked=1)=>({id:card,act,variant,offered:1,picked,obtained:picked,floor_sum:picked*(act*2-1),upgraded:0,removed:0});
 const r=run('1',{schema:'exusiai.run.v2',id:id.toString(16).padStart(64,'0'),ascension:7,victory:false,abandoned:true,floor:8,mods:[...run().mods,{id:'ActLikeIt2',version:'1',title:'Act Like It 2',workshop_id:'12345678'}],entities:[{id:card,act:0,owned:1,offered:3,picked:2,obtained:2,floor_sum:4,upgraded:0,removed:0},...[1,2,4].map(act=>({id:card,act,owned:0,offered:1,picked:act===4?0:1,obtained:0,floor_sum:0,upgraded:0,removed:0}))]});
 r.details={win3:true,acts:[1,2,3,4].map(act=>({act,floors:2,completed:act<4,snapshot_known:true,deck:[{id:card,variant:0},{id:card,variant:1}]})),items:[m(1,0),m(2,1),m(4,0,0)],offers:[1,2,4].map(act=>({id:card,act,floor:1,position:act*2-1,variant:act===2?1:0,picked:act!==4})),fights:[1,2,4].map(act=>({act,floor:2,position:act*2,encounter:'TEST_FIGHT',damage:8,turns:2}))};return r;
}
test('v2 third-act wins, upgrade variants, ownership deduplication, frozen WAR and combat baselines',async()=>{
 for(const act of [1,2,4])await db.prepare('INSERT INTO fight_baselines VALUES(7,1,\'Standard\',?,\'TEST_FIGHT\',5,50,15)').bind(act).run();
 const first=detailed();assert.equal((await send('/api/upload',first,'4'.repeat(64))).status,200);
 assert.equal((await send('/api/upload',first,'4'.repeat(64))).status,200);
 let f=filters(new URL('https://x/api/stats?ascension=7')),s=await calculate(db,f),m=s.entities.find(e=>e.act===0);
 assert.equal(s.overview.runs,1);assert.equal(s.overview.wins,1);assert.equal(s.overview.average_floor,6);assert.equal(s.overview.abandoned,0);
 assert.equal(m.offered,2);assert.equal(m.owned,1);assert.equal(m.picked_runs,1);assert.equal(m.war,1);assert.equal(m.pwar,1);assert.equal(m.delta_hp,null);
 const split=await calculate(db,filters(new URL('https://x/api/stats?ascension=7&split=1')));assert.equal(split.entities.filter(e=>e.act===0).length,2);assert.equal(split.entities.find(e=>e.act===0&&e.variant===1).owned,1);
 const all=await calculate(db,filters(new URL('https://x/api/stats?ascension=7&act=all&abandoned=loss')));assert.equal(all.overview.wins,0);assert.equal(all.overview.average_floor,8);assert.equal(all.entities.find(e=>e.act===0).offered,3);
 const single=await calculate(db,filters(new URL('https://x/api/stats?ascension=7&act=2')));assert.equal(single.overview.average_floor,2);assert.equal(single.entities.find(e=>e.act===0).offered,1);
 const frozen=(await db.prepare('SELECT expected3 FROM card_offers WHERE run_id=? ORDER BY seq').bind(first.id).all()).results;
 for(let i=17;i<27;i++)assert.equal((await send('/api/upload',detailed(i),i.toString(16).padStart(64,'0'))).status,200);
 assert.deepEqual((await db.prepare('SELECT expected3 FROM card_offers WHERE run_id=? ORDER BY seq').bind(first.id).all()).results,frozen);
 s=await calculate(db,f);m=s.entities.find(e=>e.act===0);assert.equal(m.picked_runs,11);assert.equal(m.owned,11);assert.equal(m.delta_picks,11);assert.ok(m.delta_hp<0);assert.ok(m.delta_turns<0);assert.equal((await db.prepare('SELECT samples FROM skill_models WHERE owner_hash=(SELECT owner_hash FROM runs WHERE id=?) AND horizon=\'standard\'').bind(first.id).first()).samples,1);
 assert.equal((await db.prepare('SELECT uses FROM mod_catalog WHERE id=\'ActLikeIt2\'').first()).uses,11);
 const invalid=detailed(28);invalid.details.offers[0].position=2;assert.equal((await send('/api/upload',invalid)).status,422);
});
test('tag lists ignore mandatory dependencies; retired query routes stay closed',async()=>{
 await db.prepare("UPDATE mod_catalog SET primary_tag='acts',official_tags='[\"Acts\"]' WHERE id='ActLikeIt2'").run();
 const read=q=>calculate(db,filters(new URL('https://x/api/stats?ascension=7&'+q)));
 assert.equal((await read('tag_mode=black&tags=acts')).overview.runs,0);
 assert.equal((await read('tag_mode=white&tags=acts')).overview.runs,11);
 assert.equal((await read('tag_mode=white&tags=qol')).overview.runs,0);
 assert.equal((await read('tag_mode=white')).overview.runs,0);
 assert.equal((await read('exclude=AK_Exusiai&exclude=STS2-RitsuLib')).overview.runs,11);
 assert.ok((await read('')).mods.every(m=>!['AK_Exusiai','STS2-RitsuLib'].includes(m.id)));
 assert.equal((await mf.dispatchFetch('https://test.local/api/stats?ascension=7')).status,410);assert.equal((await send('/api/stats',{query:'ascension=7'})).status,410);
});
test('Steam official taxonomy, bounded batches, daily refresh and cooldown preserve cached tags',async()=>{
 await db.exec('DELETE FROM enrichment_state; UPDATE mod_catalog SET next_check=9999999999999;');
 await db.prepare("UPDATE mod_catalog SET next_check=0,checked_at=0 WHERE id='ActLikeIt2'").run();
 const now=Date.now();let calls=0;
 const fetcher=async()=>{calls++;return Response.json({response:{publishedfiledetails:[{publishedfileid:'12345678',result:1,consumer_app_id:2868840,title:'Act Like It 2',tags:[{tag:'Acts'},{tag:'Custom secretly QoL'},{tag:'English'},{tag:'Cards'}]}]}});};
 await enrichMods(db,now,fetcher);let m=await db.prepare("SELECT * FROM mod_catalog WHERE id='ActLikeIt2'").first();assert.deepEqual(JSON.parse(m.official_tags),['Acts','Cards']);assert.equal(m.primary_tag,'acts');assert.equal(m.next_check,now+86400000);assert.equal(calls,1);
 await enrichMods(db,now+900001,fetcher);assert.equal(calls,1);
 await enrichMods(db,now+86400001,async()=>new Response('',{status:429}));m=await db.prepare("SELECT * FROM mod_catalog WHERE id='ActLikeIt2'").first();assert.equal(m.primary_tag,'acts');assert.equal(m.failures,1);
 await enrichMods(db,now+86400001+900001,fetcher);assert.equal(calls,1);
});
test('shared Pages release proxy strips visitor credentials and filters, and falls back on outages',async()=>{
 const assets={fetch:async()=>new Response('fallback')},env={SHARED_PAGES:'true',ASSETS:assets};let remote,options;
 const request=new Request('https://exusiai.zzt.si/index.html?exclude=PrivateMod&v=012345abcdef',{headers:{Authorization:'secret',Cookie:'private'}});
 const result=await staticPage(request,env,async(url,init)=>{remote=String(url);options=init;return new Response('shared release');});
 assert.equal(remote,'https://jimmyzzt.github.io/AK_Exusiai/index.html?v=012345abcdef');assert.equal(options.headers,undefined);assert.equal(await result.text(),'shared release');assert.equal(result.headers.get('X-Frame-Options'),'DENY');
 assert.equal(await(await staticPage(request,env,async()=>new Response('down',{status:503}))).text(),'fallback');
 assert.equal(await(await staticPage(request,{...env,SHARED_PAGES:'false'},async()=>{throw new Error('must not fetch');})).text(),'fallback');
});
