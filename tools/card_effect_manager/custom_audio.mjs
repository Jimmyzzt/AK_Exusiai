// Custom metadata is kept separately so rebuilding the native catalog preserves it.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

export function loadCustomAudio(root) {
  const manifestPath = path.join(root, 'tools/card_effect_manager/custom_audio.json');
  if (!fs.existsSync(manifestPath)) return [];
  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  if (manifest.schema !== 1 || !Array.isArray(manifest.entries)) throw Error('Invalid custom audio manifest');
  const seen = new Set();
  return manifest.entries.map(item => {
    if (!/^custom:[a-z0-9_]+$/.test(item.id) || seen.has(item.id) || typeof item.name !== 'string' || !item.name.trim())
      throw Error('Invalid or duplicate custom audio ID/name: ' + item.id);
    seen.add(item.id);
    if (typeof item.file !== 'string' || !item.file.startsWith('AK_Exusiai/audio/card_effects/') || item.file.includes('\\') || item.file.split('/').includes('..') || !/\.(wav|ogg|mp3)$/i.test(item.file))
      throw Error('Custom audio must be inside AK_Exusiai/audio/card_effects: ' + item.file);
    if (!fs.statSync(path.join(root, item.file)).isFile()) throw Error('Missing custom audio: ' + item.file);
    return {id:item.id, name:item.name, english:path.basename(item.file), kind:'sfx', status:'adapted', adapter:'audio',
      resource:'res://' + item.file, origin:'custom', origins:['custom','combat'], sources:[], aliases:['自定义音效','换弹'],
      reason:'Mod 自定义一次性音效；音量由独立音量控制。'};
  });
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
  const custom = loadCustomAudio(root);
  const catalogPath = path.join(root, 'AK_Exusiai/config/card_effect_catalog.json');
  const catalog = JSON.parse(fs.readFileSync(catalogPath, 'utf8'));
  const retained = catalog.entries.filter(e => e.origin !== 'custom');
  const nativeIds = new Set(retained.map(e => e.id));
  for (const e of custom) if (nativeIds.has(e.id)) throw Error('Catalog ID collision: ' + e.id);
  catalog.entries = [...retained, ...custom].sort((a,b) => a.id.localeCompare(b.id));
  const auditPath = path.join(root, 'tools/card_effect_manager/catalog_audit.json');
  const audit = JSON.parse(fs.readFileSync(auditPath, 'utf8'));
  audit.counts.entries = catalog.entries.length;
  audit.counts.adapted = catalog.entries.filter(e => e.status === 'adapted').length;
  for (const [target, doc] of [[catalogPath,catalog],[auditPath,audit]]) {
    fs.writeFileSync(target + '.tmp', JSON.stringify(doc,null,2) + '\n');
    fs.renameSync(target + '.tmp', target);
  }
  console.log('Registered custom audio: ' + custom.map(e => e.id).join(', '));
}
