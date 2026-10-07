const HEX = /^[a-f0-9]{64}$/;
export class InvalidInput extends Error {}
const fail = () => { throw new InvalidInput('Invalid request'); };
const object = (v, keys) => {
  if (!v || typeof v !== 'object' || Array.isArray(v) || Object.keys(v).some(k => !keys.includes(k))) fail();
};
const integer = (v, min, max) => { if (!Number.isSafeInteger(v) || v < min || v > max) fail(); return v; };
const text = (v, max = 128) => { if (typeof v !== 'string' || !v.length || v.length > max || /[\x00-\x1f]/.test(v)) fail(); return v; };
export function credential(request) {
  const token = request.headers.get('Authorization')?.replace(/^Bearer /, '');
  if (!token || !HEX.test(token)) fail();
  return token;
}
export async function hash(value) {
  return Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(value))), b => b.toString(16).padStart(2, '0')).join('');
}
export async function readJson(request) {
  if (!request.headers.get('Content-Type')?.startsWith('application/json')) fail();
  const limit = 128 * 1024;
  if (Number(request.headers.get('Content-Length')) > limit) fail();
  const reader = request.body?.getReader();
  if (!reader) fail();
  const chunks = []; let size = 0;
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    size += value.byteLength;
    if (size > limit) { await reader.cancel(); fail(); }
    chunks.push(value);
  }
  const body = new Uint8Array(size); let offset = 0;
  for (const chunk of chunks) { body.set(chunk, offset); offset += chunk.length; }
  try { return JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(body)); } catch { fail(); }
}
export function validateRun(v, catalog, today = new Date().toISOString().slice(0, 10)) {
  object(v, ['schema','id','day','version','revision','game_version','ascension','players','mode','victory','abandoned','floor','duration','mods','entities']);
  if (v.schema !== 'exusiai.run.v1' || !HEX.test(v.id)) fail();
  text(v.day, 10);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(v.day) || new Date(v.day).toISOString().slice(0,10) !== v.day || v.day > today || v.day < '2026-10-06') fail();
  text(v.version,64); text(v.revision,64); text(v.game_version,64);
  integer(v.ascension,0,100); integer(v.players,1,64); integer(v.floor,0,2000); integer(v.duration,0,31536000);
  if (!['Standard','Daily','Custom','None'].includes(v.mode) || typeof v.victory !== 'boolean' || typeof v.abandoned !== 'boolean' || (v.victory && v.abandoned)) fail();
  if (!Array.isArray(v.mods) || v.mods.length > 512 || !Array.isArray(v.entities) || v.entities.length > 512) fail();
  const mods = new Set();
  for (const mod of v.mods) { object(mod,['id','version']); text(mod.id); text(mod.version,64); if (mods.has(mod.id)) fail(); mods.add(mod.id); }
  if (!mods.has('AK_Exusiai')) fail();
  const entities = new Set();
  for (const m of v.entities) {
    object(m,['id','act','owned','offered','picked','obtained','floor_sum','upgraded','removed']);
    if (!catalog.has(m.id)) fail();
    integer(m.act,0,32); integer(m.owned,0,1);
    for (const key of ['offered','picked','obtained','upgraded','removed']) integer(m[key],0,10000);
    integer(m.floor_sum,0,20000000);
    if (m.picked > m.offered || (m.obtained === 0 && m.floor_sum !== 0) || (m.obtained > 0 && m.floor_sum > m.obtained * v.floor)) fail();
    if (m.act > 0 && (m.owned || m.obtained || m.upgraded || m.removed || m.floor_sum || !m.id.startsWith('AK_EXUSIAI_CARD_'))) fail();
    const key = m.id + ':' + m.act; if (entities.has(key)) fail(); entities.add(key);
  }
  for (const m of v.entities.filter(m => m.act === 0)) {
    const acts = v.entities.filter(a => a.id === m.id && a.act > 0);
    if (m.id.startsWith('AK_EXUSIAI_CARD_') && (acts.reduce((s,a)=>s+a.offered,0) !== m.offered || acts.reduce((s,a)=>s+a.picked,0) !== m.picked)) fail();
  }
  if (v.entities.some(m=>m.act>0 && !entities.has(m.id+':0'))) fail();
  return v;
}
export function filters(url) {
  const p = url.searchParams;
  if ([...p.keys()].some(k=>!['party','mode','abandoned','from','to','version','revision','ascension','exclude'].includes(k))) fail();
  const f = {party:p.get('party') || 'solo',mode:p.get('mode') || 'Standard',abandoned:p.get('abandoned') || 'exclude',from:p.get('from') || '',to:p.get('to') || '',version:p.get('version') || '',revision:p.get('revision') || '',ascension:p.get('ascension') || '',exclude:[...new Set(p.getAll('exclude'))].sort()};
  if (!['solo','multi','all'].includes(f.party) || !['Standard','Daily','Custom','all'].includes(f.mode) || !['exclude','loss'].includes(f.abandoned) || f.exclude.length>10) fail();
  for (const day of [f.from,f.to]) if (day && (!/^\d{4}-\d{2}-\d{2}$/.test(day) || new Date(day).toISOString().slice(0,10)!==day)) fail();
  if (f.from && f.to && f.from > f.to) fail();
  if (f.ascension && (!/^\d{1,2}$/.test(f.ascension) || Number(f.ascension)>10)) fail();
  if (f.version.length>64 || f.revision.length>64 || f.exclude.some(m=>m.length>128)) fail();
  return f;
}
