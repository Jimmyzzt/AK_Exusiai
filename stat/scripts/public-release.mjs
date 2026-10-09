import {mkdir,writeFile,readFile,readdir,unlink} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {validateBundle} from '../shared/public-data.mjs';
import {validateManifest,readPublished} from '../web/static-data.mjs';
import {decodePublished,publishedText} from '../shared/public-wire.mjs';
export const PAGES='https://jimmyzzt.github.io/AK_Exusiai/';
export async function previousRelease(fetcher=fetch,folder){
 const response=await fetcher(PAGES+'data/manifest.json',{cache:'no-store'});if(response.status===404)return null;if(!response.ok)throw new Error('Previous release unavailable');
 const manifest=validateManifest(await response.json());if(folder){try{const bytes=await readFile(new URL(manifest.file,folder));if(bytes.length===manifest.bytes&&createHash('sha256').update(bytes).digest('hex')===manifest.sha256){const bundle=decodePublished(JSON.parse(bytes));if(JSON.stringify(bundle.source_revision)===JSON.stringify(manifest.source_revision))return {manifest,bundle};}}catch{}}
 return {manifest,bundle:await readPublished(manifest,fetcher,new URL('data/',PAGES))};
}
export async function writeRelease(bundle,folder,previous){
 validateBundle(bundle);await mkdir(folder,{recursive:true});
 const bytes=Buffer.from(publishedText(bundle)),sha256=createHash('sha256').update(bytes).digest('hex');
 const prior=previous?(previous.manifest.sha256===sha256?previous.manifest.previous:{file:previous.manifest.file,sha256:previous.manifest.sha256,bytes:previous.manifest.bytes}):null;
 const manifest={schema_version:2,generated_at:bundle.generated_at,source_revision:bundle.source_revision,model:bundle.model,file:'statistics-'+sha256+'.json',sha256,bytes:bytes.length,previous:prior};
 validateManifest(manifest);
 if(prior){let priorBytes;if(previous.manifest.sha256!==sha256)priorBytes=Buffer.from(publishedText(previous.bundle,previous.manifest.schema_version));else{try{priorBytes=await readFile(new URL(prior.file,folder));}catch{const response=await fetch(PAGES+'data/'+prior.file,{cache:'no-store'});if(!response.ok)throw new Error('Previous data retention failed');priorBytes=Buffer.from(await response.arrayBuffer());}}
  if(priorBytes.length!==prior.bytes||createHash('sha256').update(priorBytes).digest('hex')!==prior.sha256)throw new Error('Previous data integrity failed');decodePublished(JSON.parse(priorBytes));await writeFile(new URL(prior.file,folder),priorBytes);
 }
 await writeFile(new URL(manifest.file,folder),bytes);
 await writeFile(new URL('manifest.json',folder),JSON.stringify(manifest));
 for(const name of await readdir(folder))if(/^statistics-[a-f0-9]{64}\.json$/.test(name)&&name!==manifest.file&&name!==prior?.file)await unlink(new URL(name,folder));
 return manifest;
}
