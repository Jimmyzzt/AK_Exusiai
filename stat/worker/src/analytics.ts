import {filters} from './validation.mjs';
import {PROTECTED_MODS} from './tags.mjs';
export type Filter=ReturnType<typeof filters>;
type Row=Record<string,string|number|null>;
export function selected(f:Filter) {
  const values:(string|number)[]=[],terms:string[]=[];
  if(f.party==='solo')terms.push('r.players=1');if(f.party==='multi')terms.push('r.players>1');
  if(f.mode!=='all'){terms.push('r.mode=?');values.push(f.mode);}
  for(const [value,expr] of [[f.from,'r.day>=?'],[f.to,'r.day<=?'],[f.version,'r.version=?'],[f.revision,'r.revision=?'],[f.ascension,'r.ascension=?']])if(value){terms.push(expr);values.push(value);}
  if(f.abandoned==='exclude')terms.push(f.act==='all'?'r.abandoned=0':'(r.abandoned=0 OR r.standard_win=1)');
  if(f.exclude.length){terms.push('NOT EXISTS(SELECT 1 FROM cohort_mods m WHERE m.cohort_id=r.cohort_id AND m.mod_id IN(SELECT value FROM json_each(?)))');values.push(JSON.stringify(f.exclude));}
  if(f.tags.length||f.tag_mode==='white'){terms.push(`NOT EXISTS(SELECT 1 FROM cohort_mods m LEFT JOIN mod_catalog c ON c.id=m.mod_id WHERE m.cohort_id=r.cohort_id AND m.mod_id NOT IN(SELECT value FROM json_each(?)) AND COALESCE(c.primary_tag,'untagged') ${f.tag_mode==='white'?'NOT IN':'IN'}(SELECT value FROM json_each(?)))`);values.push(JSON.stringify(PROTECTED_MODS),JSON.stringify(f.tags));}
  if(['1','2','3'].includes(f.act))terms.push(`r.max_act>=${f.act}`);
  const snap=f.act==='all'?'r.max_act':f.act==='standard'?'MIN(r.max_act,3)':f.act;
  const floor=f.act==='all'?'r.floor':f.act==='standard'?'CASE WHEN r.detailed THEN r.floor3 WHEN NOT r.extended THEN r.floor END':`(SELECT a.floors FROM run_acts a WHERE a.run_id=r.id AND a.act=${f.act})`;
  const extra=`EXISTS(SELECT 1 FROM cohort_mods m LEFT JOIN mod_catalog c ON c.id=m.mod_id WHERE m.cohort_id=r.cohort_id AND (c.primary_tag='acts' OR m.mod_id IN('ActLikeIt2','EndlessMode','ActToggler','YUILongMap')))`;
  const cte=`WITH candidates AS MATERIALIZED (
    SELECT r.id,r.day,r.version,r.revision,r.ascension,r.players,r.mode,r.abandoned,r.cohort_id,r.victory,r.floor,r.duration,d.run_id IS NOT NULL detailed,d.floor3,COALESCE(d.max_act,MAX(1,COALESCE((SELECT MAX(e.act) FROM entities e WHERE e.run_id=r.id),1))) max_act,${extra} extended,
      COALESCE(d.win3,CASE WHEN EXISTS(SELECT 1 FROM entities e WHERE e.run_id=r.id AND e.act>3) THEN 1 WHEN ${extra} THEN NULL ELSE r.victory END) standard_win
    FROM runs r LEFT JOIN run_details d ON d.run_id=r.id
  ), selected AS ${f.exclude.length||f.tags.length||f.tag_mode==='white'?'MATERIALIZED ':''}(SELECT r.*,${f.act==='all'?'r.victory':'r.standard_win'} w,${floor} scope_floor,${snap} snapshot_act FROM candidates r ${terms.length?'WHERE '+terms.join(' AND '):''})`;
  return {cte,values};
}
const scope=(a:string,f:Filter)=>f.act==='all'?`${a}.act>0`:f.act==='standard'?`${a}.act BETWEEN 1 AND 3`:`${a}.act=${f.act}`;
const variant=(a:string,f:Filter)=>f.split?`${a}.variant`:'CAST(-1 AS INTEGER)';
export async function calculate(db:D1Database,f:Filter) {
  const {cte,values}=selected(f),safe=f.act==='all'?'1':f.act==='standard'?'NOT r.extended AND r.max_act<=3':'0';
  const q=(sql:string)=>db.prepare(cte+' '+sql).bind(...values);
  const results=await db.batch<Row>([
    q(`SELECT COUNT(*) runs,COALESCE(SUM(w),0) wins,COUNT(w) win_samples,COALESCE(SUM(CASE WHEN abandoned=1 AND COALESCE(w,0)=0 THEN 1 ELSE 0 END),0) abandoned,COALESCE(AVG(scope_floor),0) average_floor,COUNT(scope_floor) floor_samples,COALESCE(AVG(duration),0) average_duration,COALESCE(SUM(detailed),0) detailed_runs,
      COALESCE(SUM(CASE WHEN detailed THEN EXISTS(SELECT 1 FROM run_acts a WHERE a.run_id=selected.id AND a.act=selected.snapshot_act AND a.snapshot_known=1) ELSE ${safe.replaceAll('r.','selected.')} END),0) hold_samples FROM selected`),
    q(`SELECT e.entity_id id,e.act,-1 variant,SUM(e.offered) offered,SUM(e.picked) picked,COUNT(CASE WHEN e.picked>0 THEN 1 END) picked_runs,SUM(CASE WHEN e.picked>0 THEN r.w ELSE 0 END) picked_wins,COUNT(CASE WHEN e.picked>0 THEN r.w END) picked_win_samples FROM entities e JOIN selected r ON r.id=e.run_id WHERE r.detailed=0 AND ${f.split?'0':'1'} AND e.entity_id LIKE 'AK_EXUSIAI_CARD_%' AND ${scope('e',f)} GROUP BY e.entity_id,e.act`),
    q(`SELECT o.entity_id id,o.act,${variant('o',f)} variant,COUNT(*) offered,SUM(o.picked) picked,COUNT(DISTINCT CASE WHEN o.picked THEN o.run_id END) picked_runs,COUNT(DISTINCT CASE WHEN o.picked AND r.w=1 THEN o.run_id END) picked_wins,COUNT(DISTINCT CASE WHEN o.picked AND r.w IS NOT NULL THEN o.run_id END) picked_win_samples,
      SUM(r.w-o.${f.act==='all'?'expected_all':'expected3'}) war,SUM(CASE WHEN o.picked THEN r.w-o.${f.act==='all'?'expected_all':'expected3'} ELSE 0 END) pwar,COUNT(r.w) war_samples,SUM(o.picked) pwar_samples,
      COUNT(*) n,SUM(o.position) sx,SUM(o.position*o.position) sx2,SUM(o.picked) sy,SUM(o.position*o.picked) sxy,SUM(r.w-o.${f.act==='all'?'expected_all':'expected3'}) wy,SUM(o.position*(r.w-o.${f.act==='all'?'expected_all':'expected3'})) wxy
      FROM card_offers o JOIN selected r ON r.id=o.run_id WHERE ${scope('o',f)} GROUP BY o.entity_id,o.act,${variant('o',f)}`),
    q(`SELECT e.entity_id id,e.act,${variant('e',f)} variant,SUM(e.obtained) obtained,SUM(e.floor_sum) floor_sum,SUM(e.upgraded) upgraded,SUM(e.removed) removed,SUM(CASE WHEN e.entity_id LIKE 'AK_EXUSIAI_RELIC_%' THEN e.offered ELSE 0 END) offered,SUM(CASE WHEN e.entity_id LIKE 'AK_EXUSIAI_RELIC_%' THEN e.picked ELSE 0 END) picked FROM detailed_entities e JOIN selected r ON r.id=e.run_id WHERE ${scope('e',f)} GROUP BY e.entity_id,e.act,${variant('e',f)}`),
    q(`SELECT e.entity_id id,0 act,-1 variant,SUM(e.obtained) obtained,SUM(e.floor_sum) floor_sum,SUM(e.upgraded) upgraded,SUM(e.removed) removed,SUM(CASE WHEN e.entity_id LIKE 'AK_EXUSIAI_RELIC_%' THEN e.offered ELSE 0 END) offered,SUM(CASE WHEN e.entity_id LIKE 'AK_EXUSIAI_RELIC_%' THEN e.picked ELSE 0 END) picked FROM entities e JOIN selected r ON r.id=e.run_id WHERE r.detailed=0 AND ${f.split?'0':'1'} AND e.act=0 AND ${safe} GROUP BY e.entity_id`),
    q(`SELECT id,0 act,variant,COUNT(*) owned,SUM(w) owned_wins,COUNT(w) owned_win_samples FROM (
      SELECT DISTINCT d.run_id,d.entity_id id,${variant('d',f)} variant,r.w FROM act_decks d JOIN selected r ON r.id=d.run_id WHERE d.act=r.snapshot_act
      UNION ALL SELECT e.run_id,e.entity_id,-1,r.w FROM entities e JOIN selected r ON r.id=e.run_id WHERE r.detailed=0 AND ${f.split?'0':'1'} AND e.act=0 AND e.owned=1 AND ${safe}) GROUP BY id,variant`),
    q(`SELECT c.entity_id id,0 act,c.variant,SUM(b.damage-b.expected_damage) damage_delta,SUM(b.turns-b.expected_turns) turns_delta,COUNT(*) delta_fights,COUNT(DISTINCT c.run_id) delta_picks FROM (
      SELECT o.run_id,o.entity_id,${variant('o',f)} variant,MIN(o.position) picked_at FROM card_offers o WHERE o.picked=1 AND ${scope('o',f)} GROUP BY o.run_id,o.entity_id,${variant('o',f)}) c
      JOIN selected r ON r.id=c.run_id JOIN fights b ON b.run_id=c.run_id AND b.position>c.picked_at WHERE b.expected_damage IS NOT NULL AND ${scope('b',f)} GROUP BY c.entity_id,c.variant`),
    db.prepare('SELECT id,title,uses,primary_tag,official_tags,workshop_id,status,checked_at FROM mod_catalog ORDER BY uses DESC,id LIMIT 5000'),
    db.prepare('SELECT DISTINCT version,revision FROM cohorts WHERE runs>0 ORDER BY version,revision LIMIT 100'),
  ]);
  const rows=new Map<string,Row>();
  const add=(source:Row)=>{
    const key=[source.id,source.variant,source.act].join(':');let target=rows.get(key);
    if(!target){target={id:source.id,variant:source.variant,act:source.act};rows.set(key,target);}
    for(const [k,v] of Object.entries(source))if(!['id','variant','act'].includes(k)&&typeof v==='number')target[k]=Number(target[k]||0)+v;
  };
  for(let i=1;i<=6;i++)for(const row of results[i].results){add(row);if(Number(row.act)>0)add({...row,act:0});}
  const entities:Row[]=[...rows.values()].map((row):Row=>{
    const n=Number(row.n||0),den=n*Number(row.sx2||0)-Number(row.sx||0)**2;
    return {...row,delta_pick:n>=20&&den>0?100*(n*Number(row.sxy||0)-Number(row.sx||0)*Number(row.sy||0))/den:null,
      delta_war:n>=20&&den>0?100*(n*Number(row.wxy||0)-Number(row.sx||0)*Number(row.wy||0))/den:null,
      delta_hp:Number(row.delta_picks)>=10?Number(row.damage_delta)/Number(row.delta_fights):null,
      delta_turns:Number(row.delta_picks)>=10?Number(row.turns_delta)/Number(row.delta_fights):null};
  });
  const overall=await q(`SELECT id,variant,COUNT(*) picked_runs,SUM(w) picked_wins,COUNT(w) picked_win_samples FROM (
    SELECT DISTINCT o.run_id,o.entity_id id,${variant('o',f)} variant,r.w FROM card_offers o JOIN selected r ON r.id=o.run_id WHERE o.picked=1 AND ${scope('o',f)}
    UNION ALL SELECT DISTINCT e.run_id,e.entity_id,-1,r.w FROM entities e JOIN selected r ON r.id=e.run_id WHERE r.detailed=0 AND ${f.split?'0':'1'} AND e.picked>0 AND e.entity_id LIKE 'AK_EXUSIAI_CARD_%' AND ${scope('e',f)}) GROUP BY id,variant`).all<Row>();
  for(const counts of overall.results){const target=entities.find(e=>e.id===counts.id&&e.variant===counts.variant&&e.act===0);if(target)Object.assign(target,counts);}
  const mods=results[7].results.filter(m=>!PROTECTED_MODS.includes(String(m.id))).map(m=>({...m,official_tags:JSON.parse(String(m.official_tags))}));
  return {schema:'exusiai.statistics.v1',updated_at:Date.now(),filters:f,overview:results[0].results[0],entities,mods,versions:results[8].results,sample_unit:'exusiai_player_run',model:'personal-logistic-floor-v2'};
}
