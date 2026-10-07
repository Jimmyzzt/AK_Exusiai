import { filters } from './validation.mjs';
export type Filter = ReturnType<typeof filters>;
export function where(f: Filter) {
  const terms: string[] = [], values: (string|number)[] = [];
  if(f.party==='solo') terms.push('r.players=1');
  if(f.party==='multi') terms.push('r.players>1');
  if(f.mode!=='all') { terms.push('r.mode=?'); values.push(f.mode); }
  terms.push('r.runs>0');
  if(f.abandoned==='exclude') terms.push('r.abandoned=0');
  for(const [value,expr] of [[f.from,'r.day>=?'],[f.to,'r.day<=?'],[f.version,'r.version=?'],[f.revision,'r.revision=?'],[f.ascension,'r.ascension=?']]) if(value) {terms.push(expr);values.push(value);}
  if(f.exclude.length) {terms.push(`NOT EXISTS (SELECT 1 FROM cohort_mods m WHERE m.cohort_id=r.id AND m.mod_id IN (${f.exclude.map(()=>'?').join(',')}))`); values.push(...f.exclude);}
  return {sql:terms.length ? 'WHERE '+terms.join(' AND ') : '',values};
}
export async function calculate(db: D1Database, f: Filter) {
  const w=where(f);
  const [overview, entities, mods, versions] = await db.batch([
    db.prepare(`SELECT COALESCE(SUM(runs),0) runs,COALESCE(SUM(wins),0) wins,COALESCE(SUM(abandoned*runs),0) abandoned,COALESCE(1.0*SUM(floor_sum)/SUM(runs),0) average_floor,COALESCE(1.0*SUM(duration_sum)/SUM(runs),0) average_duration FROM cohorts r ${w.sql}`).bind(...w.values),
    db.prepare(`SELECT e.entity_id id,e.act,SUM(e.owned) owned,SUM(e.owned_wins) owned_wins,SUM(e.offered) offered,SUM(e.picked) picked,SUM(e.obtained) obtained,SUM(e.floor_sum) floor_sum,SUM(e.upgraded) upgraded,SUM(e.removed) removed,SUM(e.picked_runs) picked_runs,SUM(e.picked_wins) picked_wins FROM cohort_entities e JOIN cohorts r ON r.id=e.cohort_id ${w.sql} GROUP BY e.entity_id,e.act ORDER BY e.entity_id,e.act`).bind(...w.values),
    db.prepare('SELECT DISTINCT mod_id id FROM cohort_mods ORDER BY mod_id LIMIT 512'),
    db.prepare('SELECT DISTINCT version,revision FROM cohorts WHERE runs>0 ORDER BY version,revision LIMIT 100'),
  ]);
  return {schema:'exusiai.statistics.v1',updated_at:Date.now(),filters:f,overview:overview.results[0],entities:entities.results,mods:mods.results,versions:versions.results,sample_unit:'exusiai_player_run'};
}
export async function snapshot(db: D1Database, f: Filter, force=false) {
  const key=JSON.stringify(f), now=Date.now();
  const old=await db.prepare('SELECT updated_at,payload FROM snapshots WHERE key=?').bind(key).first<{updated_at:number;payload:string}>();
  if(!force && old && now-old.updated_at<900000) return JSON.parse(old.payload);
  try {
    const result=await calculate(db,f);
    await db.batch([
      db.prepare('INSERT INTO snapshots(key,updated_at,payload) VALUES(?,?,?) ON CONFLICT(key) DO UPDATE SET updated_at=excluded.updated_at,payload=excluded.payload').bind(key,result.updated_at,JSON.stringify(result)),
      // Bound query-cache growth; no run data or statistical history is removed.
      db.prepare('DELETE FROM snapshots WHERE key NOT IN (SELECT key FROM snapshots ORDER BY updated_at DESC LIMIT 128)'),
    ]);
    return result;
  } catch(error) { if(old) return {...JSON.parse(old.payload),stale:true}; throw error; }
}
