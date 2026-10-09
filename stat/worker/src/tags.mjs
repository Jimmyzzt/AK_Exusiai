// Only Steam's official STS2 taxonomy. Custom tags and language tags never enter the catalogue.
export const OFFICIAL_TAGS=['Acts','Ancients','Audio','Cards','Characters','Cosmetics','Events','Expansion','Extensions','Humor','Modifiers','Monsters','Potions','QoL','Relics','Rooms','Tools & APIs','Utility','Misc'];
export const TAG_GROUPS={acts:'幕数',character:'角色',extend:'内容扩展',visual:'视觉',audio:'音频',qol:'QoL',library:'工具/依赖',misc:'其他',untagged:'无标签'};
export const PROTECTED_MODS=['AK_Exusiai','STS2-RitsuLib'];
export function officialTags(tags) {
  const normalized=new Set(tags.map(t=>String(typeof t==='string'?t:t?.tag).toLowerCase()));
  return OFFICIAL_TAGS.filter(t=>normalized.has(t.toLowerCase()));
}
export function primaryTag(tags) {
  // Gameplay-changing categories take precedence over cosmetic/QoL labels.
  if(tags.includes('Characters'))return 'character';
  if(tags.includes('Acts'))return 'acts';
  if(tags.some(t=>['Expansion','Extensions','Cards','Relics','Potions','Ancients','Events','Modifiers','Monsters','Rooms'].includes(t)))return 'extend';
  if(tags.includes('Tools & APIs'))return 'library';
  if(tags.includes('QoL')||tags.includes('Utility'))return 'qol';
  if(tags.includes('Cosmetics'))return 'visual';
  if(tags.includes('Audio'))return 'audio';
  if(tags.includes('Misc')||tags.includes('Humor'))return 'misc';
  return 'untagged';
}
