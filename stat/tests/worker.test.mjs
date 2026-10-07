import {test,before,after} from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {build} from 'esbuild';
import {Miniflare,convertV4MiniflareOptions} from 'miniflare';
import {validateRun,filters,InvalidInput} from '../worker/src/validation.mjs';
let mf,db;
const token='1'.repeat(64), token2='2'.repeat(64);
const card='AK_EXUSIAI_CARD_CHARGING_MODE';
function run(id='a',options={}) {return {schema:'exusiai.run.v1',id:id.repeat(64),day:'2026-10-06',version:'1.1.0',revision:'cards-v1.3-stat-v1',game_version:'0.111.0',ascension:10,players:1,mode:'Standard',victory:true,abandoned:false,floor:48,duration:1800,mods:[{id:'AK_Exusiai',version:'1.1.0'},{id:'STS2-RitsuLib',version:'0.6.5'}],entities:[{id:card,act:0,owned:1,offered:4,picked:2,obtained:2,floor_sum:14,upgraded:1,removed:0},{id:card,act:1,owned:0,offered:4,picked:2,obtained:0,floor_sum:0,upgraded:0,removed:0}],...options};}
const send=(path,body,credential=token)=>mf.dispatchFetch('https://test.local'+path,{method:'POST',headers:{Authorization:'Bearer '+credential,'Content-Type':'application/json'},body:JSON.stringify(body)});
before(async()=>{
  const result=await build({entryPoints:['worker/src/index.ts'],bundle:true,write:false,format:'esm',platform:'browser',target:'es2022'});
  mf=new Miniflare(convertV4MiniflareOptions({modules:true,script:result.outputFiles[0].text,compatibilityDate:'2026-10-06',d1Databases:{DB:'test-db'},bindings:{UPLOAD_ENABLED:'true'},serviceBindings:{ASSETS:async()=>new Response('static')},ratelimits:{UPLOAD_LIMIT:{namespace_id:'1001',simple:{limit:1000,period:60}}}}));
  db=await mf.getD1Database('DB');
  const sql=await readFile('worker/migrations/0001_initial.sql','utf8');
  // D1 exec expects each complete statement on a line.
  await db.exec(sql.replace(/--[^\n]*/g,'').split(';').map(s=>s.trim().replace(/\s+/g,' ')).filter(Boolean).join(';\n')+';');
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
test('refresh uses the same 15-minute snapshot instead of recomputing after every upload',async()=>{
  const uri='https://test.local/api/stats?ascension=0';
  const old=await(await mf.dispatchFetch(uri)).json();
  assert.equal((await send('/api/upload',run('0',{ascension:0}),token2)).status,200);
  const refreshed=await(await mf.dispatchFetch(uri)).json();
  assert.equal(refreshed.updated_at,old.updated_at);
  assert.equal(refreshed.overview.runs,old.overview.runs);
  // A different edge bypasses its Cache API entry, but still reuses the shared D1 snapshot.
  const coldEdge=await(await mf.dispatchFetch('https://another-edge.local/api/stats?ascension=0')).json();
  assert.equal(coldEdge.updated_at,old.updated_at);
  assert.equal(coldEdge.overview.runs,old.overview.runs);
  await db.exec("DELETE FROM runs WHERE ascension=0; DELETE FROM cohorts WHERE ascension=0; DELETE FROM snapshots;");
});
test('persist before ack, retry deduplicates, acts count player-runs once, mod exclusions and abandon switch',async()=>{
  assert.equal((await send('/api/upload',run())).status,200);
  assert.equal((await (await send('/api/upload',run())).json()).duplicate,true);
  assert.equal((await send('/api/upload',run('a'),token2)).status,422);
  assert.equal((await send('/api/upload',run('b',{victory:false,abandoned:true,mods:[...run().mods,{id:'BalanceMod',version:'1'}]}))).status,200);
  assert.equal((await send('/api/upload',run('c',{players:2,victory:false}),token2)).status,200);
  assert.equal((await send('/api/upload',run('d',{victory:false}),token2)).status,200);
  const solo=await(await mf.dispatchFetch('https://test.local/api/stats')).json();
  assert.equal(solo.overview.runs,2);assert.equal(solo.overview.wins,1);
  const act=solo.entities.find(x=>x.act===1);assert.equal(act.picked,4);assert.equal(act.picked_runs,2);assert.equal(act.picked_wins,1);
  const all=await(await mf.dispatchFetch('https://test.local/api/stats?party=all&abandoned=loss')).json();
  assert.equal(all.overview.runs,4);assert.equal(all.overview.wins,1);assert.equal(all.overview.abandoned,1);
  const excluded=await(await mf.dispatchFetch('https://test.local/api/stats?party=all&abandoned=loss&exclude=BalanceMod')).json();
  assert.equal(excluded.overview.runs,3);
  const revision=await(await mf.dispatchFetch('https://test.local/api/stats?revision=other')).json();assert.equal(revision.overview.runs,0);
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
