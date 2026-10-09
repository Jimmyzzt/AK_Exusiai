import {unpackBlock} from './block-codec.mjs';
import {SCHEMA,MODEL,ALGORITHM,SCOPES,TAG_ORDER,OVERVIEW_FIELDS,ENTITY_FIELDS,PROTECTED,emptyBlock,mergeBlock,finishEntities} from './statistics.mjs';
export {SCHEMA,MODEL,ALGORITHM};
export class UnsupportedFilter extends Error {}
export function selectPublic(bundle,f){
 const exclude=[...new Set(f.exclude)].filter(m=>!PROTECTED.includes(m));
 if(exclude.length>2)throw new UnsupportedFilter('最多同时排除两个 Mod。');
 if(exclude.length===2&&!exclude.every(id=>bundle.pair_mods.includes(id)))throw new UnsupportedFilter('这两个 Mod 暂无双项交集统计；请单独排除，或使用标签筛选。');
 const overviewSums=Array(OVERVIEW_FIELDS.length).fill(0),entities=new Map(),s=SCOPES.indexOf(f.act),tagMask=f.tags.reduce((m,t)=>m|(1<<TAG_ORDER.indexOf(t)),0);
 for(const cell of bundle.cells){
  const [day,version,revision,ascension,players,mode,abandoned,standardLoss,mask]=cell.dims;
  if((f.from&&day<f.from)||(f.to&&day>f.to)||(f.version&&version!==f.version)||(f.revision&&revision!==f.revision)||(f.ascension&&ascension!==Number(f.ascension))||(f.party==='solo'&&players!==1)||(f.party==='multi'&&players<=1)||(f.mode!=='all'&&mode!==f.mode)||(f.abandoned==='exclude'&&(f.act==='all'?abandoned:standardLoss)))continue;
  if(f.tag_mode==='white'?(mask&~tagMask)!==0:(mask&tagMask)!==0)continue;
  const p=cell.projection;
  const sign=p.length===0?1:p.length===1&&exclude.includes(p[0])?-1:p.length===2&&exclude.length===2&&p.every(id=>exclude.includes(id))?1:0;
  if(sign){for(let i=0;i<overviewSums.length;i++)overviewSums[i]+=sign*cell.block.overview[s][i];for(const r of cell.block.entities){if(r[0]!==s||r[1]!==+f.split)continue;const key=r.slice(2,5).join(':');let target=entities.get(key);if(!target){target=[...r.slice(0,5),...Array(ENTITY_FIELDS.length).fill(0)];entities.set(key,target);}for(let i=5;i<r.length;i++)target[i]+=sign*r[i];}}
 }
 const counts=Object.fromEntries(OVERVIEW_FIELDS.map((k,i)=>[k,overviewSums[i]]));
 const overview={...counts,average_floor:counts.floor_samples?counts.floor_sum/counts.floor_samples:0,average_duration:counts.runs?counts.duration_sum/counts.runs:0};
 const rows=[...entities.values()].filter(r=>r[0]===s&&r[1]===+f.split&&r[5]>0).map(r=>({id:r[2],act:r[3],variant:r[4],...Object.fromEntries(ENTITY_FIELDS.filter(k=>k!=='presence').map(k=>[k,r[5+ENTITY_FIELDS.indexOf(k)]]))}));
 return {schema:'exusiai.statistics.v1',updated_at:bundle.generated_at,filters:f,overview,entities:finishEntities(rows),mods:bundle.mods,versions:bundle.versions,sample_unit:'exusiai_player_run',model:MODEL};
}
export function publicCell(row,options){
 const copy=options?.copy!==false;
 const dims=typeof row.dims==='string'?JSON.parse(row.dims):row.dims,projection=typeof row.projection==='string'?JSON.parse(row.projection):row.projection,block=unpackBlock(typeof row.payload==='string'?JSON.parse(row.payload):row.block);
 if(!/^[a-f0-9]{64}$/.test(row.key)||!Number.isSafeInteger(row.revision)||row.revision<0)throw new Error('Invalid cell revision');
 if(!Array.isArray(dims)||dims.length!==9||dims.slice(0,3).some(v=>typeof v!=='string'||v.length>64)||dims.slice(3).filter((_,i)=>i!==2).some(v=>!Number.isSafeInteger(v)||v<0)||dims[6]>1||dims[7]>1||dims[8]>=1<<TAG_ORDER.length)throw new Error('Invalid dimensions');
 if(!Array.isArray(projection)||projection.length>2||projection.some(v=>typeof v!=='string'||v.length>128||PROTECTED.includes(v)))throw new Error('Invalid marginal');
 if(!block||Object.keys(block).sort().join()!=='entities,overview'||block.overview.length!==SCOPES.length||block.overview.some(v=>!Array.isArray(v)||v.length!==OVERVIEW_FIELDS.length||v.some(n=>!Number.isFinite(n)||n<0)))throw new Error('Invalid overview sums');
 if(!Array.isArray(block.entities)||block.entities.some(r=>!Array.isArray(r)||r.length!==5+ENTITY_FIELDS.length||!Number.isInteger(r[0])||r[0]<0||r[0]>=SCOPES.length||![0,1].includes(r[1])||typeof r[2]!=='string'||!/^AK_EXUSIAI_(?:CARD|RELIC)_[A-Z0-9_]+$/.test(r[2])||!Number.isInteger(r[3])||r[3]<0||r[3]>32||![-1,0,1].includes(r[4])||r.slice(5).some(n=>!Number.isFinite(n))))throw new Error('Invalid entity sums');
 const signed=new Set(['war','pwar','wy','wxy','damage_delta','turns_delta']);if(block.entities.some(r=>r[5]<=0||ENTITY_FIELDS.some((field,i)=>!signed.has(field)&&r[5+i]<0)))throw new Error('Negative public count');
 return {key:row.key,revision:row.revision,dims:[...dims],projection:[...projection],block:copy?{overview:block.overview.map(v=>[...v]),entities:block.entities.map(v=>[...v])}:block};
}
export function validateBundle(b){
 const allowed=['schema_version','generated_at','source_revision','model','algorithm','pair_mods','mods','versions','cells'];
 if(!b||Object.keys(b).sort().join()!==allowed.sort().join()||b.schema_version!==SCHEMA||b.model!==MODEL||b.algorithm!==ALGORITHM||!Number.isSafeInteger(b.generated_at)||b.generated_at<0)throw new Error('Unsupported public schema');
 if(!b.source_revision||Object.keys(b.source_revision).sort().join()!=='catalog,statistics'||Object.values(b.source_revision).some(n=>!Number.isSafeInteger(n)||n<0))throw new Error('Invalid source revision');
 if(!Array.isArray(b.pair_mods)||b.pair_mods.length>12||b.pair_mods.some(id=>typeof id!=='string'||PROTECTED.includes(id)))throw new Error('Invalid pair policy');
 if(!Array.isArray(b.mods)||b.mods.some(m=>Object.keys(m).sort().join()!=='id,official_tags,primary_tag,status,title,uses,workshop_id'||typeof m.id!=='string'||m.id.length>128||PROTECTED.includes(m.id)||typeof m.title!=='string'||m.title.length>256||!Number.isSafeInteger(m.uses)||m.uses<0||!TAG_ORDER.includes(m.primary_tag)||!Array.isArray(m.official_tags)||m.official_tags.some(t=>typeof t!=='string')||m.workshop_id!=null&&!/^\d{6,20}$/.test(m.workshop_id)||typeof m.status!=='string'))throw new Error('Invalid public catalogue');
 if(!Array.isArray(b.versions)||b.versions.some(v=>Object.keys(v).sort().join()!=='revision,version'||typeof v.version!=='string'||typeof v.revision!=='string'))throw new Error('Invalid versions');
 const seen=new Set();for(const c of b.cells){if(Object.keys(c).sort().join()!=='block,dims,key,projection,revision')throw new Error('Unexpected public field');publicCell(c,{copy:false});if(seen.has(c.key)||c.revision>b.source_revision.statistics)throw new Error('Invalid cell identity');seen.add(c.key);if(c.projection.length===2&&!c.projection.every(id=>b.pair_mods.includes(id)))throw new Error('Unknown pair policy');}
 return b;
}

