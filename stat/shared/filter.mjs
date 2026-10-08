import {PROTECTED_MODS,TAG_GROUPS} from '../worker/src/tags.mjs';
const fail=()=>{throw new RangeError('Invalid filter');};
export function filters(url) {
  const p = url.searchParams;
  if ([...p.keys()].some(k=>!['party','mode','abandoned','from','to','version','revision','ascension','exclude','act','split','tag_mode','tags'].includes(k))) fail();
  const f = {party:p.get('party') || 'solo',mode:p.get('mode') || 'Standard',abandoned:p.get('abandoned') || 'exclude',from:p.get('from') || '',to:p.get('to') || '',version:p.get('version') || '',revision:p.get('revision') || '',ascension:p.get('ascension') || '',exclude:[...new Set(p.getAll('exclude'))].filter(id=>!PROTECTED_MODS.includes(id)).sort(),act:p.get('act')||'standard',split:p.get('split')==='1',tag_mode:p.get('tag_mode')||'black',tags:[...new Set(p.getAll('tags'))].sort()};
  if (!['solo','multi','all'].includes(f.party) || !['Standard','Daily','Custom','all'].includes(f.mode) || !['exclude','loss'].includes(f.abandoned) || f.exclude.length>512) fail();
  if(!['standard','all','1','2','3'].includes(f.act)||!['black','white'].includes(f.tag_mode)||f.tags.some(t=>!Object.hasOwn(TAG_GROUPS,t)))fail();
  for (const day of [f.from,f.to]) if (day && (!/^\d{4}-\d{2}-\d{2}$/.test(day) || new Date(day).toISOString().slice(0,10)!==day)) fail();
  if (f.from && f.to && f.from > f.to) fail();
  if (f.ascension && (!/^\d{1,2}$/.test(f.ascension) || Number(f.ascension)>10)) fail();
  if (f.version.length>64 || f.revision.length>64 || f.exclude.some(m=>m.length>128)) fail();
  return f;
}
