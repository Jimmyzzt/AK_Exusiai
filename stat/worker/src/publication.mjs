import {metered,account} from './usage.mjs';
import {readFacts,status} from './incremental.mjs';
import {contribution,ALGORITHM,PROTECTED,projectionsFor} from '../../shared/statistics.mjs';
import {publicCell} from '../../shared/public-data.mjs';
import {packBlock} from '../../shared/block-codec.mjs';
import {hash} from './validation.mjs';
export async function publicationStatus(database){
 const meter=metered(database);const value=await status(meter.db);await account(database,meter.usage);return {...value,usage:meter.usage};
}
export async function beginWork(database,{bootstrap=false,now=Date.now()}={}){
 const {db,usage}=metered(database),nonce=crypto.randomUUID();
 const lease=await db.prepare('UPDATE stat_state SET lease=?,lease_until=? WHERE id=1 AND lease_until<=? AND cooldown<=?').bind(nonce,now+60000,now,now).run();
 if(!lease.meta.changes){await account(database,usage);return {state:'busy',usage};}
 let keep=false;
 try{
  const state=await db.prepare('SELECT * FROM stat_state WHERE id=1').first();
  if((state.window===Math.floor(now/900000)&&state.reads>=(bootstrap?180000:18000))||(state.day===Math.floor(now/86400000)&&(state.day_reads>=1800000||state.day_writes>=30000)))return {state:'budget',usage};
  const pending=await db.prepare('SELECT seq,run_id,generation FROM stat_dirty ORDER BY seq LIMIT 1').first();if(!pending)return {state:'idle',usage};
  let pairs=JSON.parse(state.policy);if(pairs==null)pairs=(await db.prepare("SELECT id FROM mod_catalog WHERE id NOT IN(SELECT value FROM json_each(?)) ORDER BY uses DESC,id LIMIT 6").bind(JSON.stringify(PROTECTED)).all()).results.map(r=>r.id).sort();
  const oldRow=await db.prepare('SELECT contribution,algorithm FROM stat_facts WHERE run_id=?').bind(pending.run_id).first();
  const old=oldRow?{...JSON.parse(oldRow.contribution),algorithm:oldRow.algorithm}:null,source=await readFacts(db,pending.run_id),next=source?{...contribution(source),algorithm:ALGORITHM,pair_mods:pairs}:null;
  // Multiple Mod labels may change without changing this run's tag mask or sums.
  // Acknowledge only the generation read above; a concurrent change stays queued.
  if(old&&next&&JSON.stringify(old)===JSON.stringify(next)){
   const acknowledged=await db.prepare('DELETE FROM stat_dirty WHERE run_id=? AND generation=? AND EXISTS(SELECT 1 FROM stat_state WHERE id=1 AND lease=? AND lease_until>=? AND revision=?)').bind(pending.run_id,pending.generation,nonce,now,state.revision).run();
   return {state:acknowledged.meta.changes?'unchanged':'busy',usage,source_revision:state.revision};
  }
  const specs=new Map();for(const c of [old,next])if(c)for(const projection of projectionsFor(c)){const key=await hash(JSON.stringify([c.algorithm,c.dims,projection]));specs.set(key,{key,algorithm:c.algorithm,dims:c.dims,projection});}
  const keys=[...specs.values()],revision=state.revision+1;
  await db.batch([db.prepare('DELETE FROM stat_work WHERE expires<?').bind(now),db.prepare('INSERT INTO stat_work VALUES(?,?,?,?,?,?,?,?)').bind(nonce,pending.run_id,pending.generation,revision,JSON.stringify(pairs),next?JSON.stringify(next):null,JSON.stringify(keys),now+60000)]);
  keep=true;return {state:'work',work:{nonce,revision,old,next,keys},usage};
 }catch(error){
  await db.prepare('UPDATE stat_state SET cooldown=? WHERE id=1 AND lease=?').bind(now+900000,nonce).run();throw error;
 }finally{
  if(!keep)await db.prepare('UPDATE stat_state SET lease=NULL,lease_until=0 WHERE lease=?').bind(nonce).run();
  await account(database,usage);
 }
}
export async function workCells(database,nonce,offset){
 const {db,usage}=metered(database),work=await db.prepare('SELECT keys FROM stat_work WHERE nonce=? AND expires>? AND EXISTS(SELECT 1 FROM stat_state WHERE lease=? AND lease_until>?)').bind(nonce,Date.now(),nonce,Date.now()).first();
 if(!work)throw new RangeError('Expired work');
 const keys=JSON.parse(work.keys).slice(offset,offset+8).map(r=>r.key);
 const rows=(await db.prepare('SELECT key,payload FROM stat_cells WHERE key IN(SELECT value FROM json_each(?))').bind(JSON.stringify(keys)).all()).results;
 await account(database,usage);return {cells:rows,usage};
}
export async function stageWork(database,input,allowedIds){
 if(!input||Object.keys(input).sort().join()!=='nonce,ordinal,updates'||typeof input.nonce!=='string'||!Number.isInteger(input.ordinal)||input.ordinal<0||input.ordinal>1000||!Array.isArray(input.updates)||input.updates.length>8)throw new RangeError('Invalid work chunk');
 const {db,usage}=metered(database),now=Date.now(),work=await db.prepare('SELECT keys,revision FROM stat_work WHERE nonce=? AND expires>? AND EXISTS(SELECT 1 FROM stat_state WHERE lease=? AND lease_until>?)').bind(input.nonce,now,input.nonce,now).first();
 if(!work)throw new RangeError('Expired work');
 const specs=JSON.parse(work.keys),keys=new Set(),updates=input.updates.map(raw=>{
  if(Object.keys(raw).sort().join()!=='block,dims,key,projection,revision')throw new RangeError('Unexpected work field');
  const cell=publicCell(raw),spec=specs.find(s=>s.key===cell.key);
  if(!spec||cell.revision!==work.revision||JSON.stringify(spec.dims)!==JSON.stringify(cell.dims)||JSON.stringify(spec.projection)!==JSON.stringify(cell.projection)||keys.has(cell.key)||allowedIds&&cell.block.entities.some(row=>!allowedIds.has(row[2])))throw new RangeError('Invalid work identity');
  keys.add(cell.key);return {key:cell.key,dims:JSON.stringify(cell.dims),projection:JSON.stringify(cell.projection),payload:JSON.stringify(packBlock(cell.block)),active:+cell.block.overview.some(v=>v[0]>0)};
 });
 const payload=JSON.stringify(updates);if(payload.length>450000)throw new RangeError('Work chunk too large');
 const digest=await hash(payload),existing=await db.prepare('SELECT hash FROM stat_work_chunks WHERE nonce=? AND ordinal=?').bind(input.nonce,input.ordinal).first();
 if(existing&&existing.hash!==digest)throw new RangeError('Chunk conflict');
 await db.batch([db.prepare('INSERT OR IGNORE INTO stat_work_chunks VALUES(?,?,?,?)').bind(input.nonce,input.ordinal,digest,payload),db.prepare('UPDATE stat_work SET expires=? WHERE nonce=?').bind(now+60000,input.nonce),db.prepare('UPDATE stat_state SET lease_until=? WHERE lease=?').bind(now+60000,input.nonce)]);
 await account(database,usage);return {state:'staged',usage};
}
export async function commitWork(database,nonce){
 const {db,usage}=metered(database),now=Date.now(),w=await db.prepare('SELECT * FROM stat_work WHERE nonce=? AND expires>?').bind(nonce,now).first();
 if(!w)throw new RangeError('Expired work');
 const got=(await db.prepare("SELECT json_extract(value,'$.key') key,COUNT(*) n FROM stat_work_chunks c,json_each(c.payload) WHERE c.nonce=? GROUP BY json_extract(value,'$.key')").bind(nonce).all()).results;
 const expected=JSON.parse(w.keys).map(k=>k.key);if(got.length!==expected.length||got.some(r=>r.n!==1||!expected.includes(r.key)))throw new RangeError('Incomplete work');
 const guard='EXISTS(SELECT 1 FROM stat_state WHERE id=1 AND lease=? AND lease_until>? AND revision=?)',next=w.next?JSON.parse(w.next):null;
 const statements=[db.prepare('UPDATE stat_state SET revision=?,policy=? WHERE id=1 AND revision=? AND lease=? AND lease_until>?').bind(w.revision,w.policy,w.revision-1,nonce,now),
 db.prepare("INSERT INTO stat_cells(key,dims,projection,payload,revision,active) SELECT json_extract(value,'$.key'),json_extract(value,'$.dims'),json_extract(value,'$.projection'),json_extract(value,'$.payload'),?,json_extract(value,'$.active') FROM stat_work_chunks c,json_each(c.payload) WHERE c.nonce=? AND "+guard+" ON CONFLICT(key) DO UPDATE SET dims=excluded.dims,projection=excluded.projection,payload=excluded.payload,revision=excluded.revision,active=excluded.active").bind(w.revision,nonce,nonce,now,w.revision)];
 if(next)statements.push(db.prepare('INSERT INTO stat_facts(run_id,contribution,max_act,extended,win3,algorithm) SELECT ?,?,?,?,?,? WHERE '+guard+' ON CONFLICT(run_id) DO UPDATE SET contribution=excluded.contribution,max_act=excluded.max_act,extended=excluded.extended,win3=excluded.win3,algorithm=excluded.algorithm').bind(w.run_id,w.next,next.normalized.max_act,next.normalized.extended,next.normalized.win3,ALGORITHM,nonce,now,w.revision));
 else statements.push(db.prepare('DELETE FROM stat_facts WHERE run_id=? AND '+guard).bind(w.run_id,nonce,now,w.revision));
 statements.push(db.prepare('DELETE FROM stat_dirty WHERE run_id=? AND generation=? AND '+guard).bind(w.run_id,w.generation,nonce,now,w.revision),db.prepare('DELETE FROM stat_work WHERE nonce=?').bind(nonce),db.prepare('UPDATE stat_state SET lease=NULL,lease_until=0 WHERE lease=?').bind(nonce));
 const results=await db.batch(statements);await account(database,usage);return {state:results[0].meta.changes?'processed':'busy',usage};
}
