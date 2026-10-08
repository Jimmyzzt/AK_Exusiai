import {build as bundleModule} from 'esbuild';
import {readFile,writeFile,mkdir,cp,readdir,rm} from 'node:fs/promises';
import {execFileSync} from 'node:child_process';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {relative} from 'node:path';
import sharp from 'sharp';
import {cardMetadata} from './card-metadata.mjs';
const root=new URL('../../',import.meta.url), dist=new URL('../dist/',import.meta.url);
const read=(path)=>readFile(new URL(path,root),'utf8');
const catalog=[],translations={};
for(const [file,kind] of [['cards','card'],['relics','relic']]) {
  const zh=JSON.parse(await read(`AK_Exusiai/localization/zhs/${file}.json`));
  const en=JSON.parse(await read(`AK_Exusiai/localization/eng/${file}.json`));
  translations[file]={zhs:zh,eng:en};
  if(file==='cards')for(const language of ['zhs','eng'])Object.assign(translations.cards[language],JSON.parse(await read(`AK_Exusiai/localization/${language}/static_hover_tips.json`)));
  for(const [key,name] of Object.entries(zh)) {
    if(!key.endsWith('.title') || !key.startsWith(`AK_EXUSIAI_${kind.toUpperCase()}_`)) continue;
    const id=key.slice(0,-6);
    catalog.push({id,name,en:en[key] || name,kind});
  }
}
// Class inheritance defines the ancient subset, independently of translated names.
const ancientSource=await read('AK_ExusiaiCode/Relics/AncientRelics.cs');
const ancientClasses=[...ancientSource.matchAll(/class (\w+) : ExusiaiAncientRelic/g)].map(m=>m[1].replace(/([a-z0-9])([A-Z])/g,'$1_$2').replace(/([A-Z])([A-Z][a-z])/g,'$1_$2').toUpperCase());
for(const item of catalog) if(ancientClasses.includes(item.id.replace('AK_EXUSIAI_RELIC_',''))) item.kind='ancient';
catalog.sort((a,b)=>a.id.localeCompare(b.id));
// Only this script's generated stat/dist directory may be cleared.
if(relative(fileURLToPath(new URL('../',import.meta.url)),fileURLToPath(dist))!=='dist') throw new Error('Invalid build output path');
await rm(dist,{recursive:true,force:true});
await mkdir(dist,{recursive:true});
await cp(new URL('../web/',import.meta.url),dist,{recursive:true});
await mkdir(new URL('art/',dist),{recursive:true});
await cp(new URL('AK_Exusiai/images/character/exusiai_icon.png',root),new URL('art/portrait.png',dist));
await sharp(fileURLToPath(new URL('references/official/asset/立绘_新约能天使_1.png',root)))
  .resize({height:880,withoutEnlargement:true}).webp({quality:90}).toFile(fileURLToPath(new URL('art/hero.webp',dist)));
// Read the skill guide from the art tool; keep webpage framing synchronized with that source.
const guide=await read('tools/card_art_manager/card_art_preview.gd');
const skillGuide=/else:\s*\n\t\tnormalized_points = \[([\s\S]*?)\]/.exec(guide)?.[1];
if(!skillGuide) throw new Error('Skill portrait guide not found');
const points=[...skillGuide.matchAll(/Vector2\(([\d.]+), ([\d.]+)\)/g)].map(m=>[+m[1],+m[2]]);
if(points.length!==4) throw new Error('Unexpected skill portrait guide');
const crop={left:Math.min(...points.map(p=>p[0])),right:Math.max(...points.map(p=>p[0])),top:Math.min(...points.map(p=>p[1])),bottom:Math.max(...points.map(p=>p[1]))};
for(const folder of ['Cards','Relics']) {
  const dir=new URL(`AK_ExusiaiCode/${folder}/`,root);
  for(const file of await readdir(dir)) {
    if(!file.endsWith('.cs')) continue;
    const source=await readFile(new URL(file,dir),'utf8');
    for(const m of source.matchAll(/public sealed class (\w+)/g)) {
      const slug=m[1].replace(/([a-z0-9])([A-Z])/g,'$1_$2').replace(/([A-Z])([A-Z][a-z])/g,'$1_$2').toUpperCase();
      const item=catalog.find(c=>c.id===`AK_EXUSIAI_${folder==='Cards'?'CARD':'RELIC'}_${slug}`);
      if(!item) continue;
      if(folder==='Cards')Object.assign(item,cardMetadata(source,item.id,translations.cards));
      const input=new URL(`AK_Exusiai/images/${folder.toLowerCase()}/${m[1]}.png`,root);
      let bytes;try {bytes=await readFile(input);}catch(error){if(error.code==='ENOENT')continue;throw error;}
      let pipeline=sharp(bytes);
      if(folder==='Cards') {
        const meta=await pipeline.metadata(),left=Math.round(meta.width*crop.left),top=Math.round(meta.height*crop.top);
        pipeline=pipeline.extract({left,top,width:Math.round(meta.width*crop.right)-left,height:Math.round(meta.height*crop.bottom)-top});
      }
      await pipeline.webp({quality:92}).toFile(fileURLToPath(new URL(`art/${m[1]}.webp`,dist)));
      item.art=`art/${m[1]}.webp`;item.art_revision=createHash('sha256').update(bytes).update(JSON.stringify(crop)).digest('hex').slice(0,12);
    }
  }
}
await writeFile(new URL('../worker/web-catalog.json',import.meta.url),JSON.stringify(catalog));
await writeFile(new URL('catalog.json',dist),JSON.stringify(catalog));
let commit='local'; try {commit=execFileSync('git',['-c','safe.directory='+decodeURIComponent(root.pathname).replace(/^\/([A-Z]:)/,'$1').replace(/\/$/,''),'rev-parse','--short','HEAD'],{cwd:root,encoding:'utf8',stdio:['ignore','pipe','ignore']}).trim();} catch {}
const fingerprint=createHash('sha256');
for(const path of ['web/index.html','web/app.js','web/metrics.js','web/style.css','worker/src/index.ts','worker/src/analytics.ts','worker/src/baselines.ts','worker/src/steam.ts','worker/src/tags.mjs','worker/src/store-details.ts','worker/src/validation.mjs','mod/ExusiaiTelemetry.cs','mod/ExusiaiUploadAdapter.cs','mod/RunStatistics.cs','mod/DetailedStatistics.cs','mod/ActSnapshots.cs']) fingerprint.update(await readFile(new URL('../'+path,import.meta.url)));
for(const path of ['shared/statistics.mjs','shared/public-data.mjs','shared/filter.mjs','web/static-data.mjs','worker/src/incremental.mjs','worker/src/publisher-auth.mjs','worker/src/publication.mjs','worker/src/usage.mjs'])fingerprint.update(await readFile(new URL('../'+path,import.meta.url)));
fingerprint.update(JSON.stringify(catalog));
fingerprint.update(guide);
fingerprint.update(await readFile(new URL('./card-metadata.mjs',import.meta.url)));
const buildId=fingerprint.digest('hex').slice(0,12);
for(const name of ['statistics.mjs','public-data.mjs'])await cp(new URL('../shared/'+name,import.meta.url),new URL(name,dist));
await bundleModule({entryPoints:[fileURLToPath(new URL('../web/static-data.mjs',import.meta.url))],bundle:true,format:'esm',platform:'browser',outfile:fileURLToPath(new URL('static-data.mjs',dist))});
await bundleModule({entryPoints:[fileURLToPath(new URL('../shared/filter.mjs',import.meta.url))],bundle:true,format:'esm',platform:'browser',outfile:fileURLToPath(new URL('filter.mjs',dist))});
await writeFile(new URL('build.json',dist),JSON.stringify({commit,public_schema:1,build_id:buildId,built_at:new Date().toISOString(),api:process.env.STAT_API_ORIGIN || 'https://exusiai.zzt.si'}));
await writeFile(new URL('.nojekyll',dist),'');
console.log(`Built ${catalog.filter(c=>c.kind==='card').length} cards, ${catalog.filter(c=>c.kind==='relic').length} relics, ${catalog.filter(c=>c.kind==='ancient').length} ancient relics.`);
