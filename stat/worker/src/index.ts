import catalog from '../web-catalog.json';
import { InvalidInput, credential, hash, readJson, validateRun, filters } from './validation.mjs';
import { snapshot } from './statistics';
const ids=new Set(catalog.map(item=>item.id));
const json=(value:unknown,status=200)=>Response.json(value,{status,headers:{'Access-Control-Allow-Origin':'*','Cache-Control':'no-store','X-Content-Type-Options':'nosniff'}});
async function upload(request:Request,env:Env) {
  if(env.UPLOAD_ENABLED!=='true') return json({error:'Uploads paused'},503);
  const token=credential(request),owner=await hash(token);
  if(!(await env.UPLOAD_LIMIT.limit({key:request.headers.get('CF-Connecting-IP') || owner})).success) return json({error:'Retry later'},429);
  const run=validateRun(await readJson(request),ids);
  const existing=await env.DB.prepare('SELECT owner_hash FROM runs WHERE id=?').bind(run.id).first<{owner_hash:string}>();
  if(existing) return existing.owner_hash===owner ? json({accepted:true,duplicate:true}) : json({error:'Invalid identity'},422);
  const now=Date.now();
  const cohort=await hash(JSON.stringify([run.day,run.version,run.revision,run.ascension,run.players,run.mode,run.abandoned,run.mods.map((m:{id:string})=>m.id).sort()]));
  const nonce=crypto.randomUUID();
  // All writes are atomic. JSON table insertion keeps the whole run below D1's statement/bind limits.
  const results=await env.DB.batch([
    env.DB.prepare('INSERT OR IGNORE INTO cohorts(id,day,version,revision,ascension,players,mode,abandoned) VALUES(?,?,?,?,?,?,?,?)').bind(cohort,run.day,run.version,run.revision,run.ascension,run.players,run.mode,+run.abandoned),
    env.DB.prepare(`INSERT INTO runs(id,owner_hash,day,version,revision,game_version,ascension,players,mode,victory,abandoned,floor,duration,received_at,payload,cohort_id,write_nonce)
      SELECT ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,? WHERE (SELECT COUNT(*) FROM runs WHERE owner_hash=? AND received_at>=?)<30 ON CONFLICT(id) DO NOTHING`)
      .bind(run.id,owner,run.day,run.version,run.revision,run.game_version,run.ascension,run.players,run.mode,+run.victory,+run.abandoned,run.floor,run.duration,now,JSON.stringify(run),cohort,nonce,owner,Math.floor(now/86400000)*86400000),
    env.DB.prepare(`INSERT OR IGNORE INTO cohort_mods(cohort_id,mod_id) SELECT ?,json_extract(value,'$.id') FROM json_each(?) WHERE EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)`).bind(cohort,JSON.stringify(run.mods),nonce),
    env.DB.prepare(`INSERT OR IGNORE INTO entities(run_id,entity_id,act,owned,offered,picked,obtained,floor_sum,upgraded,removed)
      SELECT ?,json_extract(value,'$.id'),json_extract(value,'$.act'),json_extract(value,'$.owned'),json_extract(value,'$.offered'),json_extract(value,'$.picked'),json_extract(value,'$.obtained'),json_extract(value,'$.floor_sum'),json_extract(value,'$.upgraded'),json_extract(value,'$.removed') FROM json_each(?) WHERE EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)`).bind(run.id,JSON.stringify(run.entities),nonce),
    env.DB.prepare(`UPDATE cohorts SET runs=runs+1,wins=wins+?,floor_sum=floor_sum+?,duration_sum=duration_sum+? WHERE id=? AND EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)`).bind(+run.victory,run.floor,run.duration,cohort,nonce),
    env.DB.prepare(`INSERT INTO cohort_entities(cohort_id,entity_id,act,owned,owned_wins,offered,picked,obtained,floor_sum,upgraded,removed,picked_runs,picked_wins)
      SELECT ?,entity_id,act,owned,owned*?,offered,picked,obtained,floor_sum,upgraded,removed,CASE WHEN picked>0 THEN 1 ELSE 0 END,CASE WHEN picked>0 THEN ? ELSE 0 END FROM entities WHERE run_id=? AND EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)
      ON CONFLICT(cohort_id,entity_id,act) DO UPDATE SET owned=owned+excluded.owned,owned_wins=owned_wins+excluded.owned_wins,offered=offered+excluded.offered,picked=picked+excluded.picked,obtained=obtained+excluded.obtained,floor_sum=floor_sum+excluded.floor_sum,upgraded=upgraded+excluded.upgraded,removed=removed+excluded.removed,picked_runs=picked_runs+excluded.picked_runs,picked_wins=picked_wins+excluded.picked_wins`).bind(cohort,+run.victory,+run.victory,run.id,nonce),
    env.DB.prepare('DELETE FROM cohorts WHERE id=? AND runs=0 AND NOT EXISTS(SELECT 1 FROM runs WHERE cohort_id=?)').bind(cohort,cohort),
  ]);
  if(!results[1].meta.changes) {
    if(await env.DB.prepare('SELECT 1 FROM runs WHERE id=? AND owner_hash=?').bind(run.id,owner).first()) return json({accepted:true,duplicate:true});
    return json({error:'Daily upload limit reached'},429);
  }
  return json({accepted:true});
}
export default {
  async fetch(request,env,ctx) {
    const url=new URL(request.url);
    if(!url.pathname.startsWith('/api/')) return env.ASSETS.fetch(request);
    if(request.method==='OPTIONS') return new Response(null,{status:204,headers:{'Access-Control-Allow-Origin':'*','Access-Control-Allow-Methods':'GET,POST,OPTIONS','Access-Control-Allow-Headers':'Content-Type,Authorization','Access-Control-Max-Age':'86400'}});
    try {
      if(url.pathname==='/api/upload' && request.method==='POST') return await upload(request,env);
      if(url.pathname==='/api/health' && request.method==='GET') return json({schema:'exusiai.statistics.v1',status:'ok',uploads:env.UPLOAD_ENABLED==='true'});
      if(url.pathname==='/api/stats' && request.method==='GET') {
        const f=filters(url), key=new Request(url.origin+'/api/cache/'+await hash(JSON.stringify(f))), cache=caches.default;
        const cached=await cache.match(key);
        if(cached) {
          const data=await cached.clone().json<{updated_at:number}>();
          if(Date.now()-data.updated_at<900000) return json(data);
          // An old successful snapshot survives a temporary database quota failure at this edge.
          try {
            const data=await snapshot(env.DB,f);
            ctx.waitUntil(cache.put(key,new Response(JSON.stringify(data),{headers:{'Cache-Control':'public,max-age=86400','Content-Type':'application/json'}})));
            return json(data);
          } catch { return json({...data,stale:true}); }
        }
        const data=await snapshot(env.DB,f);
        ctx.waitUntil(cache.put(key,new Response(JSON.stringify(data),{headers:{'Cache-Control':'public,max-age=86400','Content-Type':'application/json'}})));
        return json(data);
      }
      return json({error:'Not found'},404);
    } catch(error) {
      if(error instanceof InvalidInput || error instanceof RangeError) return json({error:'Invalid request'},422);
      // Avoid logging credentials, identifiers, payloads, SQL bindings, or raw database exceptions.
      console.error(JSON.stringify({event:'statistics_service_unavailable',operation:url.pathname}));
      return json({error:'Service temporarily unavailable; retry later'},503);
    }
  },
  async scheduled(_event,env,_ctx) { await snapshot(env.DB,filters(new URL('https://exusiai.zzt.si/api/stats')),true); },
} satisfies ExportedHandler<Env>;
