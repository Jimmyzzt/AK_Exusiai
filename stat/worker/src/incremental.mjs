import {contribution,emptyBlock,mergeBlock,ALGORITHM,PROTECTED} from '../../shared/statistics.mjs';
import {publicCell} from '../../shared/public-data.mjs';
import {hash} from './validation.mjs';
import {metered,account} from './usage.mjs';
export {metered} from './usage.mjs';
export async function readFacts(db,id){
 const sqls=[
 'SELECT day,version,revision,ascension,players,mode,victory,abandoned,floor,duration FROM runs WHERE id=?',
 'SELECT win3,floor3,max_act FROM run_details WHERE run_id=?',
 'SELECT act,floors,snapshot_known FROM run_acts WHERE run_id=?',
 'SELECT entity_id,act,variant FROM act_decks WHERE run_id=?',
 'SELECT entity_id,act,owned,offered,picked,obtained,floor_sum,upgraded,removed FROM entities WHERE run_id=?',
 'SELECT entity_id,act,variant,offered,picked,obtained,floor_sum,upgraded,removed FROM detailed_entities WHERE run_id=?',
 'SELECT entity_id,act,variant,position,picked,expected3,expected_all FROM card_offers WHERE run_id=?',
 'SELECT act,position,damage,turns,expected_damage,expected_turns FROM fights WHERE run_id=?',
 "SELECT m.mod_id id,COALESCE(c.primary_tag,'untagged') primary_tag FROM runs r JOIN cohort_mods m ON m.cohort_id=r.cohort_id LEFT JOIN mod_catalog c ON c.id=m.mod_id WHERE r.id=?",
 ];
 const a=await db.batch(sqls.map(sql=>db.prepare(sql).bind(id)));
 if(!a[0].results.length)return null;
 return {run:a[0].results[0],details:a[1].results[0]??null,acts:a[2].results,decks:a[3].results,entities:a[4].results,items:a[5].results,offers:a[6].results,fights:a[7].results,mods:a[8].results};
}
function projections(c){if(!c)return [];const ids=c.mods,common=ids.filter(id=>c.pair_mods.includes(id));return [[],...ids.map(id=>[id]),...common.flatMap((a,i)=>common.slice(i+1).map(b=>[a,b]))];}
export async function status(db){
 const a=await db.batch([db.prepare('SELECT revision,catalog_revision,policy,window,reads,writes,day,day_reads,day_writes,cooldown FROM stat_state WHERE id=1'),db.prepare('SELECT 1 pending FROM stat_dirty ORDER BY seq LIMIT 1')]);
 const s=a[0].results[0];return {source_revision:{statistics:s.revision,catalog:s.catalog_revision},pair_mods:JSON.parse(s.policy)||[],pending:!!a[1].results.length,budget:{window:s.window,rows_read:s.reads,rows_written:s.writes,day:s.day,day_reads:s.day_reads,day_writes:s.day_writes},cooldown:s.cooldown};
}
export async function processOne(database,{now=Date.now(),bootstrap=false,beforeCommit}={}){
 const {db,usage}=metered(database),nonce=crypto.randomUUID(),window=Math.floor(now/900000),day=Math.floor(now/86400000);
 const lease=await db.prepare('UPDATE stat_state SET lease=?,lease_until=? WHERE id=1 AND lease_until<=? AND cooldown<=?').bind(nonce,now+60000,now,now).run();
 if(!lease.meta.changes)return {state:'busy',usage};
 let applied=false;
 try{
  const state=await db.prepare('SELECT * FROM stat_state WHERE id=1').first();
  if((state.window===window&&state.reads>=(bootstrap?200000:18000))||(state.day===day&&(state.day_reads>=1800000||state.day_writes>=30000)))return {state:'budget',usage};
  const pending=await db.prepare('SELECT seq,run_id,generation FROM stat_dirty ORDER BY seq LIMIT 1').first();
  if(!pending)return {state:'idle',usage};
  let pairs=JSON.parse(state.policy);
  if(pairs==null)pairs=(await db.prepare("SELECT id FROM mod_catalog WHERE id NOT IN(SELECT value FROM json_each(?)) ORDER BY uses DESC,id LIMIT 6").bind(JSON.stringify(PROTECTED)).all()).results.map(r=>r.id).sort();
  const oldRow=await db.prepare('SELECT contribution,algorithm FROM stat_facts WHERE run_id=?').bind(pending.run_id).first();
  const old=oldRow?{...JSON.parse(oldRow.contribution),algorithm:oldRow.algorithm}:null,source=await readFacts(db,pending.run_id);
  const next=source?{...contribution(source),algorithm:ALGORITHM,pair_mods:pairs}:null;
  const operations=new Map();
  for(const [c,sign] of [[old,-1],[next,1]])if(c)for(const projection of projections(c)){
   const key=await hash(JSON.stringify([c.algorithm,c.dims,projection]));
   if(!operations.has(key))operations.set(key,{key,dims:c.dims,projection,deltas:[]});
   operations.get(key).deltas.push([c.block,sign]);
  }
  const keys=[...operations.keys()];
  const existing=(await db.prepare('SELECT key,payload FROM stat_cells WHERE key IN(SELECT value FROM json_each(?))').bind(JSON.stringify(keys)).all()).results;
  const current=new Map(existing.map(r=>[r.key,JSON.parse(r.payload)])),revision=state.revision+1,updates=[];
  for(const o of operations.values()){
   const block=current.get(o.key)||emptyBlock();for(const [delta,sign] of o.deltas)mergeBlock(block,delta,sign);
   block.entities=block.entities.filter(r=>r[5]>0);if(block.overview.some(v=>v.some(n=>n<0)))throw new Error('Negative rollup');
   updates.push({key:o.key,dims:JSON.stringify(o.dims),projection:JSON.stringify(o.projection),payload:JSON.stringify(block),active:+block.overview.some(v=>v[0]>0)});
  }
  // Small JSON batches preserve transactionality without one SQL statement per marginal.
  const chunks=[];let chunk=[],bytes=0;
  for(const u of updates){const length=JSON.stringify(u).length;if(length>900000)throw new Error('Cell size budget');if(bytes+length>900000&&chunk.length){chunks.push(chunk);chunk=[];bytes=0;}chunk.push(u);bytes+=length;}if(chunk.length)chunks.push(chunk);
  if(chunks.length>24)throw new Error('Run maintenance size budget');
  const guard='EXISTS(SELECT 1 FROM stat_state WHERE id=1 AND lease=? AND lease_until>=? AND revision=?)';
  const stmts=[db.prepare('UPDATE stat_state SET revision=?,policy=? WHERE id=1 AND lease=? AND lease_until>=? AND revision=?').bind(revision,JSON.stringify(pairs),nonce,now,state.revision),
   ...chunks.map(part=>db.prepare("INSERT INTO stat_cells(key,dims,projection,payload,revision,active) SELECT json_extract(value,'$.key'),json_extract(value,'$.dims'),json_extract(value,'$.projection'),json_extract(value,'$.payload'),?,json_extract(value,'$.active') FROM json_each(?) WHERE "+guard+" ON CONFLICT(key) DO UPDATE SET dims=excluded.dims,projection=excluded.projection,payload=excluded.payload,revision=excluded.revision,active=excluded.active").bind(revision,JSON.stringify(part),nonce,now,revision))];
  if(next)stmts.push(db.prepare('INSERT INTO stat_facts(run_id,contribution,max_act,extended,win3,algorithm) SELECT ?,?,?,?,?,? WHERE '+guard+' ON CONFLICT(run_id) DO UPDATE SET contribution=excluded.contribution,max_act=excluded.max_act,extended=excluded.extended,win3=excluded.win3,algorithm=excluded.algorithm').bind(pending.run_id,JSON.stringify(next),next.normalized.max_act,next.normalized.extended,next.normalized.win3,ALGORITHM,nonce,now,revision));
  else stmts.push(db.prepare('DELETE FROM stat_facts WHERE run_id=? AND '+guard).bind(pending.run_id,nonce,now,revision));
  stmts.push(db.prepare('DELETE FROM stat_dirty WHERE run_id=? AND generation=? AND '+guard).bind(pending.run_id,pending.generation,nonce,now,revision));
  if(beforeCommit)await beforeCommit();
  const committed=await db.batch(stmts);applied=!!committed[0].meta.changes;
  return {state:applied?'processed':'busy',usage,source_revision:revision};
 }catch(error){
  await database.prepare('UPDATE stat_state SET cooldown=? WHERE id=1 AND lease=?').bind(now+900000,nonce).run();
  console.error(JSON.stringify({event:'stat_maintenance_deferred'}));throw error;
 }finally{
  await database.prepare('UPDATE stat_state SET window=?,reads=CASE WHEN window=? THEN reads+? ELSE ? END,writes=CASE WHEN window=? THEN writes+? ELSE ? END,day=?,day_reads=CASE WHEN day=? THEN day_reads+? ELSE ? END,day_writes=CASE WHEN day=? THEN day_writes+? ELSE ? END,lease=NULL,lease_until=0 WHERE id=1 AND lease=?')
   .bind(window,window,usage.rows_read,usage.rows_read,window,usage.rows_written,usage.rows_written,day,day,usage.rows_read,usage.rows_read,day,usage.rows_written,usage.rows_written,nonce).run();
 }
}
export async function exportPage(database,{after=0,through,revision=after,key='',catalog=false,bootstrap=false}){
 const {db,usage}=metered(database);try{const s=await status(db);
 if(s.pending||s.source_revision.statistics!==through||revision<after) return {changed:true,usage};
 if((s.budget.day===Math.floor(Date.now()/86400000)&&(s.budget.day_reads>=1800000||s.budget.day_writes>=30000))||(s.budget.window===Math.floor(Date.now()/900000)&&s.budget.rows_read>=(bootstrap?200000:20000))) return {budget:true,usage};
 const rows=(await db.prepare('SELECT key,dims,projection,payload,revision,active FROM stat_cells WHERE (revision,key)>(?,?) AND revision<=? ORDER BY revision,key LIMIT 2').bind(revision,key||(revision===after?'\uffff':''),through).all()).results;
 const cells=rows.map(r=>r.active?{key:r.key,revision:r.revision,dims:r.dims,projection:r.projection,payload:r.payload}:{key:r.key,revision:r.revision,deleted:true});
 let mods;if(catalog)mods=(await db.prepare('SELECT id,title,uses,primary_tag,official_tags,workshop_id,status FROM mod_catalog WHERE id NOT IN(SELECT value FROM json_each(?)) ORDER BY uses DESC,id').bind(JSON.stringify(PROTECTED)).all()).results.map(m=>({id:m.id,title:m.title,uses:m.uses,primary_tag:m.primary_tag,official_tags:JSON.parse(m.official_tags),workshop_id:m.workshop_id,status:m.status}));
 const tail=rows.at(-1);const end=await status(db);
 if(end.pending||JSON.stringify(end.source_revision)!==JSON.stringify(s.source_revision))return {changed:true,usage};

 return {source_revision:s.source_revision,pair_mods:s.pair_mods,cells,mods,next:rows.length===2?{revision:tail.revision,key:tail.key}:null,usage};
 }finally{await account(database,usage);}
}
