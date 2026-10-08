import {validateBundle} from '../shared/public-data.mjs';
const digest=async bytes=>[...new Uint8Array(await crypto.subtle.digest('SHA-256',bytes))].map(b=>b.toString(16).padStart(2,'0')).join('');
export function validateManifest(m){
 if(!m||Object.keys(m).sort().join()!=='bytes,file,generated_at,model,previous,schema_version,sha256,source_revision'||m.schema_version!==1||m.model!=='personal-logistic-floor-v2'||!Number.isSafeInteger(m.generated_at)||!Number.isSafeInteger(m.bytes)||m.bytes<1||m.bytes>67108864||!/^[a-f0-9]{64}$/.test(m.sha256)||m.file!=='statistics-'+m.sha256+'.json')throw new Error('Invalid manifest');
 if(m.previous!=null&&(Object.keys(m.previous).sort().join()!=='bytes,file,sha256'||!Number.isSafeInteger(m.previous.bytes)||m.previous.bytes<1||!/^[a-f0-9]{64}$/.test(m.previous.sha256)||m.previous.file!=='statistics-'+m.previous.sha256+'.json'||m.previous.sha256===m.sha256))throw new Error('Invalid previous release');
 return m;
}
export async function readPublished(manifest,fetcher,base){
 validateManifest(manifest);const response=await fetcher(new URL(manifest.file,base),{cache:'force-cache'});
 if(!response.ok)throw new Error('Statistics download failed');
 const bytes=await response.arrayBuffer();if(bytes.byteLength!==manifest.bytes||await digest(bytes)!==manifest.sha256)throw new Error('Statistics integrity failed');
 const b=validateBundle(JSON.parse(new TextDecoder().decode(bytes)));
 if(b.generated_at!==manifest.generated_at||JSON.stringify(b.source_revision)!==JSON.stringify(manifest.source_revision))throw new Error('Inconsistent release');
 return b;
}
export function indexedCache(){
 const database=()=>new Promise((resolve,reject)=>{const request=indexedDB.open('exusiai-public-statistics',1);request.onupgradeneeded=()=>request.result.createObjectStore('release');request.onerror=()=>reject(request.error);request.onsuccess=()=>resolve(request.result);});
 const action=async(mode,fn)=>{const db=await database();try{return await new Promise((resolve,reject)=>{const tx=db.transaction('release',mode),r=fn(tx.objectStore('release'));let value;r.onsuccess=()=>{value=r.result;};tx.oncomplete=()=>resolve(value);tx.onerror=()=>reject(tx.error);});}finally{db.close();}};
 return {get:()=>action('readonly',s=>s.get('current')),set:value=>action('readwrite',s=>s.put(value,'current'))};
}
export function staticSource({fetcher=fetch,cache=indexedCache(),base=new URL('./data/',location.href)}={}){
 let current=null,restored=false,pending=null;
 async function refresh(){
  if(!restored){restored=true;try{const saved=await cache.get();if(saved){validateManifest(saved.manifest);validateBundle(saved.bundle);const bytes=new TextEncoder().encode(JSON.stringify(saved.bundle));if(bytes.length!==saved.manifest.bytes||await digest(bytes)!==saved.manifest.sha256||saved.bundle.generated_at!==saved.manifest.generated_at)throw new Error('Corrupt cached release');current=saved;}}catch{}}
  try{
   const response=await fetcher(new URL('manifest.json',base),{cache:'no-cache'});if(!response.ok)throw new Error('Manifest unavailable');
   const manifest=validateManifest(await response.json());
   if(current?.manifest.sha256===manifest.sha256)return {bundle:current.bundle,state:'unchanged'};
   const bundle=await readPublished(manifest,fetcher,base);current={manifest,bundle};try{await cache.set(current);}catch{}
   return {bundle,state:'updated'};
  }catch(error){if(current)return {bundle:current.bundle,state:'offline'};throw error;}
 }
 // Manual and automatic checks share one operation; a slower response cannot replace a newer release.
 return {refresh(){return pending??=(refresh().finally(()=>{pending=null;}));}};
}
