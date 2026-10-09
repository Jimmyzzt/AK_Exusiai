import {mergeWork} from '../shared/statistics.mjs';
import {mkdir,cp,readFile,appendFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import {previousRelease,writeRelease,PAGES} from './public-release.mjs';
import {validateBundle,publicCell} from '../shared/public-data.mjs';
import {AUDIENCE} from '../worker/src/publisher-auth.mjs';
import {mapLimited} from './parallel.mjs';
import {publicationFetch} from './publication-http.mjs';
import {packBlock} from '../shared/block-codec.mjs';
const origin='https://exusiai.zzt.si',folder=new URL('../dist/data/',import.meta.url);
let auth=null,until=0,operation='previous_release';
const usage={rows_read:0,rows_written:0,duration_ms:0};
async function authorization(){
 if(auth&&Date.now()<until)return auth;
 const url=new URL(process.env.ACTIONS_ID_TOKEN_REQUEST_URL);url.searchParams.set('audience',AUDIENCE);
 const response=await fetch(url,{headers:{Authorization:'Bearer '+process.env.ACTIONS_ID_TOKEN_REQUEST_TOKEN},signal:AbortSignal.timeout(15000)});
 if(!response.ok)throw new Error('OIDC unavailable');const {value}=await response.json();if(typeof value!=='string')throw new Error('OIDC unavailable');
 auth=value;until=Date.now()+180000;return auth;
}
async function api(path,options={}){
 const endpoint=path.split('?')[0];operation=endpoint;
 try{
  const {response,data}=await publicationFetch(fetch,origin+'/api/publication/'+path,{...options,headers:{...options.headers,Authorization:'Bearer '+await authorization()}},{readJson:true});
  if(!response.ok)throw Object.assign(new Error('Publication service HTTP '+response.status),{status:response.status});if(data.usage)for(const k of Object.keys(usage))usage[k]+=data.usage[k]||0;return data;
 }catch(error){error.operation=endpoint;throw error;}
}
async function preserve(previous){
 if(!previous){console.log(JSON.stringify({event:'publication_deferred',reason:'initialization_pending',usage}));return false;}
 await writeRelease(previous.bundle,folder,previous);
 console.log(JSON.stringify({event:'publication_preserved',source_revision:previous.bundle.source_revision,usage}));
 return true;
}
let previous=null,changed=false,ready=false,processed=0,unchanged=0;
try{
 previous=await previousRelease(fetch,folder);
 await api('initialize',{method:'POST'});
 const bootstrap=process.env.STAT_BOOTSTRAP==='true',maxJobs=bootstrap?200:40;
 let state=await api('status');
 for(let i=0;state.pending&&i<maxJobs;i++){
  const task=await api('tick'+(bootstrap?'?bootstrap=1':''),{method:'POST'});
  if(task.state==='unchanged'){unchanged++;state=await api('status');continue;}
  if(task.state!=='work')break;
  const work=task.work;work.existing=[];
  const offsets=Array.from({length:Math.ceil(work.keys.length/8)},(_,i)=>i*8);
  const existing=await mapLimited(offsets,3,offset=>api('work-cells?'+new URLSearchParams({nonce:work.nonce,offset:String(offset)})));
  for(const page of existing)work.existing.push(...page.cells);
  operation='merge-work';const updates=mergeWork(work).map(cell=>({...cell,block:packBlock(cell.block)}));operation='chunk-work';let chunks=[],part=[],bytes=0;
  for(const update of updates){const size=JSON.stringify(update).length;if(part.length>=8||bytes+size>240000){chunks.push(part);part=[];bytes=0;}if(size>240000)throw new Error('Maintenance cell size budget');part.push(update);bytes+=size;}if(part.length)chunks.push(part);
  await mapLimited(chunks,3,(updates,ordinal)=>api('stage',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({nonce:work.nonce,ordinal,updates})}));
  const committed=await api('commit',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({nonce:work.nonce})});if(committed.state!=='processed')break;
  processed++;
  if(usage.rows_read>(bootstrap?200000:20000))break;
  state=await api('status');
 }
 state=await api('status');
 console.log(JSON.stringify({event:'publication_maintenance_summary',processed,unchanged,pending:state.pending,usage}));
 if(state.pending){ready=await preserve(previous);}
 else if(previous&&JSON.stringify(previous.bundle.source_revision)===JSON.stringify(state.source_revision)){ready=await preserve(previous);}
 else{
  let published=false;
  for(let attempt=0;attempt<2&&!published;attempt++){
   const start=await api('status');if(start.pending)break;
   const through=start.source_revision.statistics,after=previous?.bundle.source_revision.statistics||0;
   if(after>through)throw new Error('Source revision moved backwards');
   const cells=new Map((previous?.bundle.cells||[]).map(c=>[c.key,c]));let next={},mods,inconsistent=false,pages=0;
   do{
    const q=new URLSearchParams({after:String(after),through:String(through),revision:String(next.revision??after),key:next.key||'',catalog_revision:String(start.source_revision.catalog),catalog:pages===0?'1':'0',bootstrap:bootstrap?'1':'0'});
    const page=await api('export?'+q);pages++;
    if(page.changed||page.budget){inconsistent=true;break;}
    if(JSON.stringify(page.source_revision)!==JSON.stringify(start.source_revision)){inconsistent=true;break;}
    if(page.mods)mods=page.mods;
    for(const c of page.cells){if(c.deleted)cells.delete(c.key);else cells.set(c.key,publicCell(c));}
    next=page.next;
    if(pages%100===0)console.log(JSON.stringify({event:'publication_export_progress',pages,cells:cells.size,source_revision:start.source_revision,usage}));
    if(usage.rows_read>(bootstrap?200000:20000)||pages>3000)throw new Error('Publication read budget');
   }while(next);
   const end=await api('status');
   if(inconsistent||end.source_revision.statistics!==through)continue;
   const live=[...cells.values()].sort((a,b)=>a.key.localeCompare(b.key));
   const versions=[...new Map(live.filter(c=>!c.projection.length).map(c=>[JSON.stringify(c.dims.slice(1,3)),{version:c.dims[1],revision:c.dims[2]}])).values()];
   const bundle=validateBundle({schema_version:1,generated_at:Date.now(),source_revision:start.source_revision,model:'personal-logistic-floor-v2',algorithm:1,pair_mods:start.pair_mods,mods,versions,cells:live});
   const manifest=await writeRelease(bundle,folder,previous);published=true;changed=true;ready=true;
   console.log(JSON.stringify({event:'publication_generated',source_revision:manifest.source_revision,cells:live.length,bytes:manifest.bytes,usage}));
  }
  if(!published)ready=await preserve(previous);
 }
}catch(error){
 const known=['Negative contribution','Maintenance cell size budget','Publication read budget','Source revision moved backwards','OIDC unavailable'];
 console.error(JSON.stringify({event:'publication_failed',operation:error.operation||operation,http_status:Number.isInteger(error.status)?error.status:null,reason:known.includes(error.message)?error.message:['SyntaxError','TimeoutError','TypeError','AbortError'].includes(error.name)?error.name:'validation_or_internal',usage}));
 if(previous)await preserve(previous);process.exitCode=1;
}

if(process.env.GITHUB_OUTPUT)await appendFile(process.env.GITHUB_OUTPUT,'stats_changed='+changed+'\npublish_ready='+ready+'\n');
