import {initializePublication} from './initialize.mjs';
import {metered} from './usage.mjs';
import catalog from '../web-catalog.json';
import { InvalidInput, credential, hash, readJson, validateRun,filters } from './validation.mjs';
import {exportPage} from './incremental.mjs';
import {publicationStatus,beginWork,workCells,stageWork,commitWork} from './publication.mjs';
import {verifyPublisher} from './publisher-auth.mjs';
import { detailStatements } from './store-details';
import { enrichMods } from './steam';
const ids=new Set(catalog.map(item=>item.id));
const json=(value:unknown,status=200)=>Response.json(value,{status,headers:{'Access-Control-Allow-Origin':'*','Cache-Control':'no-store','X-Content-Type-Options':'nosniff'}});
export async function staticPage(request:Request,env:Env,fetcher:typeof fetch=fetch) {
  const url=new URL(request.url);
  if(env.SHARED_PAGES!=='true')return env.ASSETS.fetch(request);
  if(!['GET','HEAD'].includes(request.method))return new Response('Method not allowed',{status:405});
  if(!/^\/(?:|index\.html|app\.js|metrics\.js|static-data\.mjs|public-data\.mjs|statistics\.mjs|block-codec\.mjs|public-wire\.mjs|filter\.mjs|tags\.mjs|style\.css|catalog\.json|build\.json|data\/(?:manifest\.json|statistics-[a-f0-9]{64}\.json)|art\/[A-Za-z0-9_-]+\.(?:webp|png))$/.test(url.pathname))return env.ASSETS.fetch(request);
  const remote=new URL('https://jimmyzzt.github.io/AK_Exusiai/'+url.pathname.slice(1));
  const immutableData=/^\/data\/statistics-[a-f0-9]{64}\.json$/.test(url.pathname);
  const version=url.searchParams.get('v');if(version&&/^[a-f0-9]{12}$/.test(version))remote.searchParams.set('v',version);
  try {
    // Share the Pages publication without forwarding visitor cookies, credentials, or filter parameters.
    const response=await fetcher(remote,{method:request.method,signal:AbortSignal.timeout(10000),cf:{cacheTtl:immutableData?31536000:version?86400:60,cacheEverything:true}});
    if(response.ok){const headers=new Headers(response.headers);headers.set('X-Content-Type-Options','nosniff');headers.set('X-Frame-Options','DENY');headers.set('Referrer-Policy','no-referrer');headers.set('Content-Security-Policy',"default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; connect-src 'self' https://exusiai.zzt.si; frame-ancestors 'none'; base-uri 'self'; form-action 'self'");headers.set('Cache-Control',immutableData?'public,max-age=31536000,immutable':version?'public,max-age=86400':'public,max-age=60');return new Response(response.body,{status:response.status,headers});}
  }catch{}
  return env.ASSETS.fetch(request);
}
async function upload(request:Request,env:Env) {
  const {db,usage}=metered(env.DB);try{
  if(env.UPLOAD_ENABLED!=='true') return json({error:'Uploads paused'},503);
  const token=credential(request),owner=await hash(token);
  if(!(await env.UPLOAD_LIMIT.limit({key:request.headers.get('CF-Connecting-IP') || owner})).success) return json({error:'Retry later'},429);
  const run=validateRun(await readJson(request),ids);
  const existing=await db.prepare('SELECT owner_hash FROM runs WHERE id=?').bind(run.id).first<{owner_hash:string}>();
  if(existing) return existing.owner_hash===owner ? json({accepted:true,duplicate:true}) : json({error:'Invalid identity'},422);
  const now=Date.now();
  const cohort=await hash(JSON.stringify([run.day,run.version,run.revision,run.ascension,run.players,run.mode,run.abandoned,run.mods.map((m:{id:string})=>m.id).sort()]));
  const nonce=crypto.randomUUID();
  const details=await detailStatements(db,run,owner,nonce);
  // All writes are atomic. JSON table insertion keeps the whole run below D1's statement/bind limits.
  const results=await db.batch([
    db.prepare('INSERT OR IGNORE INTO cohorts(id,day,version,revision,ascension,players,mode,abandoned) VALUES(?,?,?,?,?,?,?,?)').bind(cohort,run.day,run.version,run.revision,run.ascension,run.players,run.mode,+run.abandoned),
    db.prepare(`INSERT INTO runs(id,owner_hash,day,version,revision,game_version,ascension,players,mode,victory,abandoned,floor,duration,received_at,payload,cohort_id,write_nonce)
      SELECT ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,? WHERE (SELECT COUNT(*) FROM runs WHERE owner_hash=? AND received_at>=?)<30 ON CONFLICT(id) DO NOTHING`)
      .bind(run.id,owner,run.day,run.version,run.revision,run.game_version,run.ascension,run.players,run.mode,+run.victory,+run.abandoned,run.floor,run.duration,now,JSON.stringify(run),cohort,nonce,owner,Math.floor(now/86400000)*86400000),
    db.prepare(`INSERT OR IGNORE INTO cohort_mods(cohort_id,mod_id) SELECT ?,json_extract(value,'$.id') FROM json_each(?) WHERE EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)`).bind(cohort,JSON.stringify(run.mods),nonce),
    db.prepare(`INSERT OR IGNORE INTO entities(run_id,entity_id,act,owned,offered,picked,obtained,floor_sum,upgraded,removed)
      SELECT ?,json_extract(value,'$.id'),json_extract(value,'$.act'),json_extract(value,'$.owned'),json_extract(value,'$.offered'),json_extract(value,'$.picked'),json_extract(value,'$.obtained'),json_extract(value,'$.floor_sum'),json_extract(value,'$.upgraded'),json_extract(value,'$.removed') FROM json_each(?) WHERE EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)`).bind(run.id,JSON.stringify(run.entities),nonce),
    db.prepare(`UPDATE cohorts SET runs=runs+1,wins=wins+?,floor_sum=floor_sum+?,duration_sum=duration_sum+? WHERE id=? AND EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)`).bind(+run.victory,run.floor,run.duration,cohort,nonce),
    db.prepare(`INSERT INTO cohort_entities(cohort_id,entity_id,act,owned,owned_wins,offered,picked,obtained,floor_sum,upgraded,removed,picked_runs,picked_wins)
      SELECT ?,entity_id,act,owned,owned*?,offered,picked,obtained,floor_sum,upgraded,removed,CASE WHEN picked>0 THEN 1 ELSE 0 END,CASE WHEN picked>0 THEN ? ELSE 0 END FROM entities WHERE run_id=? AND EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)
      ON CONFLICT(cohort_id,entity_id,act) DO UPDATE SET owned=owned+excluded.owned,owned_wins=owned_wins+excluded.owned_wins,offered=offered+excluded.offered,picked=picked+excluded.picked,obtained=obtained+excluded.obtained,floor_sum=floor_sum+excluded.floor_sum,upgraded=upgraded+excluded.upgraded,removed=removed+excluded.removed,picked_runs=picked_runs+excluded.picked_runs,picked_wins=picked_wins+excluded.picked_wins`).bind(cohort,+run.victory,+run.victory,run.id,nonce),
    db.prepare('DELETE FROM cohorts WHERE id=? AND runs=0 AND NOT EXISTS(SELECT 1 FROM runs WHERE cohort_id=?)').bind(cohort,cohort),
    db.prepare(`INSERT INTO mod_catalog(id,title,workshop_id,uses) SELECT json_extract(value,'$.id'),COALESCE(json_extract(value,'$.title'),''),json_extract(value,'$.workshop_id'),1 FROM json_each(?) WHERE EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)
      ON CONFLICT(id) DO UPDATE SET uses=uses+1,title=CASE WHEN status='ready' THEN title ELSE COALESCE(NULLIF(excluded.title,''),title) END,next_check=CASE WHEN workshop_id IS NULL AND excluded.workshop_id IS NOT NULL THEN 0 ELSE next_check END,workshop_id=COALESCE(workshop_id,excluded.workshop_id)`)
      .bind(JSON.stringify(run.mods),nonce),
    ...details,
  ]);
  if(!results[1].meta.changes) {
    if(await db.prepare('SELECT 1 FROM runs WHERE id=? AND owner_hash=?').bind(run.id,owner).first()) return json({accepted:true,duplicate:true});
    return json({error:'Daily upload limit reached'},429);
  }
  return json({accepted:true});
  }finally{console.info(JSON.stringify({event:'stat_upload_usage',...usage}));}
}
export default {
  async fetch(request,env,ctx) {
    const url=new URL(request.url);
    if(!url.pathname.startsWith('/api/')) return staticPage(request,env);
    if(request.method==='OPTIONS') return new Response(null,{status:204,headers:{'Access-Control-Allow-Origin':'*','Access-Control-Allow-Methods':'GET,POST,OPTIONS','Access-Control-Allow-Headers':'Content-Type,Authorization','Access-Control-Max-Age':'86400'}});
    try {
      if(url.pathname==='/api/upload' && request.method==='POST') return await upload(request,env);
      if(url.pathname==='/api/health' && request.method==='GET') return json({schema:'exusiai.statistics.v1',status:'ok',uploads:env.UPLOAD_ENABLED==='true'});
      if(url.pathname==='/api/stats'&&String(env.STATIC_STATS_READY)==='false'){
        let target=url;if(request.method==='POST'){const body=await readJson(request);if(typeof body?.query!=='string')throw new InvalidInput();target=new URL(url.origin+'/api/stats?'+body.query);}
        try{const ready=await fetch('https://jimmyzzt.github.io/AK_Exusiai/data/manifest.json',{method:'HEAD',signal:AbortSignal.timeout(5000),cf:{cacheTtl:60,cacheEverything:true}});if(ready.ok&&ready.headers.get('Content-Type')?.includes('application/json'))return json({error:'Use the published static statistics'},410);}catch{}
        const f=filters(target),key=new Request(url.origin+'/api/cache/'+await hash(JSON.stringify(f))),cached=await caches.default.match(key);
        if(cached)return json({...await cached.json<Record<string,unknown>>(),stale:true});
        const row=await env.DB.prepare('SELECT payload FROM snapshots WHERE key=?').bind(JSON.stringify(f)).first<{payload:string}>();
        return row?json({...JSON.parse(row.payload),stale:true}):json({error:'Static publication preparing'},503);
      }
      if(url.pathname==='/api/stats')return json({error:'Statistics are published as static JSON',manifest:'https://jimmyzzt.github.io/AK_Exusiai/data/manifest.json'},410);
      if(url.pathname.startsWith('/api/publication/')) {
        const claims=await verifyPublisher(request);if(!claims)return json({error:'Unauthorized'},401);
        if(url.pathname==='/api/publication/initialize'&&request.method==='POST')return json(await initializePublication(env.DB));
        if(url.pathname==='/api/publication/tick'&&request.method==='POST')return json(await beginWork(env.DB,{bootstrap:url.searchParams.get('bootstrap')==='1'&&claims.event_name==='workflow_dispatch'}));
        if(url.pathname==='/api/publication/status'&&request.method==='GET')return json(await publicationStatus(env.DB));
        if(url.pathname==='/api/publication/work-cells'&&request.method==='GET'){const offset=Number(url.searchParams.get('offset')||0);if(!Number.isInteger(offset)||offset<0||offset>1200)throw new InvalidInput();return json(await workCells(env.DB,url.searchParams.get('nonce'),offset));}
        if(url.pathname==='/api/publication/stage'&&request.method==='POST')return json(await stageWork(env.DB,await readJson(request,500000),ids));
        if(url.pathname==='/api/publication/commit'&&request.method==='POST'){const input=await readJson(request);if(!input||Object.keys(input).join()!=='nonce'||typeof input.nonce!=='string')throw new InvalidInput();return json(await commitWork(env.DB,input.nonce));}
        if(url.pathname==='/api/publication/export'&&request.method==='GET'){
          const after=Number(url.searchParams.get('after')||0),through=Number(url.searchParams.get('through')),revision=Number(url.searchParams.get('revision')||after),key=url.searchParams.get('key')||'',catalogRevision=url.searchParams.has('catalog_revision')?Number(url.searchParams.get('catalog_revision')):undefined;
          if(![after,through,revision,...(catalogRevision===undefined?[]:[catalogRevision])].every(n=>Number.isSafeInteger(n)&&n>=0)||after>through||revision>through||key&&!/^[a-f0-9]{64}$/.test(key))throw new InvalidInput();
          return json(await exportPage(env.DB,{after,through,revision,key,catalogRevision,catalog:url.searchParams.get('catalog')==='1',bootstrap:url.searchParams.get('bootstrap')==='1'&&claims.event_name==='workflow_dispatch'}));
        }
      }
      return json({error:'Not found'},404);
    } catch(error) {
      if(error instanceof InvalidInput || error instanceof RangeError) return json({error:'Invalid request'},422);
      // Avoid logging credentials, identifiers, payloads, SQL bindings, or raw database exceptions.
      console.error(JSON.stringify({event:'statistics_service_unavailable',operation:url.pathname}));
      return json({error:'Service temporarily unavailable; retry later'},503);
    }
  },
  async scheduled(_event,env,ctx) {
    ctx.waitUntil(enrichMods(env.DB));
    // Steam enrichment is independent. The publisher drains the bounded rollup queue.
    // No raw statistics calculation is reachable from visitors or this cron.
  },
} satisfies ExportedHandler<Env>;
