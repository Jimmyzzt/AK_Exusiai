// Metadata only: never exports game source, textures, scenes or sound banks.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../..');
const args = Object.fromEntries(process.argv.slice(2).map((v,i,a)=>v.startsWith('--')?[v.slice(2),a[i+1]]:[]).filter(x=>x.length));
if (!args.source || !args.game) throw Error('Usage: node build_catalog.mjs --source <fresh ilspy project> --game <game directory>');
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const source = path.resolve(args.source);
const game = path.resolve(args.game);
const dllHash = hash(path.join(game,'data_sts2_windows_x86_64/sts2.dll'));
const stamp = path.join(source,'catalog-source.sha256');
if (!fs.existsSync(stamp) || fs.readFileSync(stamp,'utf8').trim() !== dllHash) throw Error('Decompiled source must have the matching catalog-source.sha256 stamp. Run rebuild_catalog.ps1.');

function openPack(filename) {
  const fd=fs.openSync(filename,'r'), header=Buffer.alloc(112);
  fs.readSync(fd,header,0,header.length,0);
  if(header.toString('ascii',0,4)!=='GDPC' || ![2,3].includes(header.readUInt32LE(4))) throw Error('Unsupported PCK header');
  const version=header.readUInt32LE(4),flags=header.readUInt32LE(20);
  if(flags&1) throw Error('Encrypted PCK directory is unsupported');
  const base=Number(header.readBigUInt64LE(24));
  const tableOffset=version===3?Number(header.readBigUInt64LE(32)):96;
  const table=Buffer.alloc(fs.fstatSync(fd).size-tableOffset);
  fs.readSync(fd,table,0,table.length,tableOffset);
  let at=4;const entries=new Map();
  for(let j=0;j<table.readUInt32LE(0);j++){
    const len=table.readUInt32LE(at);at+=4;
    const name=table.toString('utf8',at,at+len).replace(/\0+$/,'');at+=len;
    const offset=Number(table.readBigUInt64LE(at))+(flags&2?base:0),size=Number(table.readBigUInt64LE(at+8)),ef=table.readUInt32LE(at+32);at+=36;
    if(offset<0||offset+size>fs.fstatSync(fd).size) throw Error('PCK entry bounds: '+name);
    entries.set(name,{offset,size,flags:ef});
  }
  return {entries,read(name){const e=entries.get(name);if(!e)return '';if(e.flags&1)throw Error('Encrypted entry: '+name);if(e.size>8e6)return '';const b=Buffer.alloc(e.size);fs.readSync(fd,b,0,b.length,e.offset);return b.toString('utf8');},close(){fs.closeSync(fd);}};
}
const pack=openPack(path.join(game,'SlayTheSpire2.pck'));
const titles={};for(const locale of ['zhs','eng'])for(const group of ['cards','monsters'])titles[locale+group]=JSON.parse(pack.read(`localization/${locale}/${group}.json`)||'{}');
const snake=s=>s.replace(/([A-Z]+)([A-Z][a-z])/g,'$1_$2').replace(/([a-z0-9])([A-Z])/g,'$1_$2').toUpperCase();
const display=(name,group='cards')=>({name:titles['zhs'+group][snake(name)+'.title']||name,english:titles['eng'+group][snake(name)+'.title']||name});
function walk(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(dir,e.name)):e.name.endsWith('.cs')?[path.join(dir,e.name)]:[]);}
const files=walk(source), texts=new Map(files.map(p=>[path.basename(p,'.cs'),fs.readFileSync(p,'utf8')]));
const quoted=s=>[...s.matchAll(/"([^"\r\n]+)"/g)].map(m=>m[1]);
const unique=a=>[...new Set(a)].sort();
const allEntries=new Map(),audit=[];
function entry(id,kind,name,extra={}){if(!allEntries.has(id))allEntries.set(id,{id,kind,name,english:name,status:'candidate',reason:'需要检查调用参数和场景依赖',sources:[],resources:[],...extra});return allEntries.get(id);}
const constants=new Map([...texts.get('VfxCmd').matchAll(/const string (\w+) = "([^"]+)"/g)].map(m=>['VfxCmd.'+m[1],m[2]]));
const basicNames={vfx_attack_slash:'斩击',vfx_attack_blunt:'钝击',vfx_attack_lightning:'闪电',vfx_heavy_blunt:'重击',vfx_bloody_impact:'血色冲击',vfx_dagger_throw:'飞刀',vfx_dagger_spray:'匕首喷射',vfx_cross_heal:'治疗',vfx_block:'格挡',vfx_scratch:'抓挠',vfx_bite:'撕咬'};
for(const scene of unique([...constants.values()]))entry('scene:'+scene,'vfx',basicNames[path.basename(scene)]||path.basename(scene).replace(/^vfx_/,'').replaceAll('_',' '),{status:'adapted',reason:'通用原版场景；待游戏内验证',adapter:'scene',resource:scene,resources:['res://scenes/'+scene+'.tscn']});

// Reviewed visual-only position factories. Do not add factories with damage/model parameters.
const factories={
 NHyperbeamVfx:['source_target','超能光束 · 发射'],NHyperbeamImpactVfx:['source_target','超能光束 · 命中'],
 NGrandFinaleVfx:['source','华丽收场 · 主演出'],NGrandFinaleImpactVfx:['center_ground','华丽收场 · 命中'],
 NSweepingBeamVfx:['source_targets','扫荡射线 · 发射'],NSweepingBeamImpactVfx:['target','扫荡射线 · 命中'],
 NBigSlashVfx:['target_bool','巨型斩击'],NBigSlashImpactVfx:['target','巨型斩击 · 命中'],
 NDaggerSprayFlurryVfx:['target_color_bool','匕首喷射 · 连击'],NDaggerSprayImpactVfx:['target_color_bool','匕首喷射 · 命中'],
 NShivThrowVfx:['source_target_color','小刀投掷'],NScratchVfx:['target_bool','抓挠粒子'],
 NFireBurstVfx:['ground_scale','火焰爆发'],NFireBurningVfx:['ground_scale_bool','持续火焰'],
 NGoopyImpactVfx:['target_color','黏液冲击'],NGaseousImpactVfx:['target_color','气体冲击'],
 NPoisonImpactVfx:['target','毒素冲击'],NHeavyBluntVfx:['target','重击粒子'],
 NLineBurstVfx:['target','线状爆发'],NScreamVfx:['target','尖叫'],
 NLargeMagicMissileVfx:['ground_color','大型魔法飞弹'],NSmallMagicMissileVfx:['target_color','小型魔法飞弹'],
 NMinionDiveBombVfx:['source_ground','俯冲轰炸'],NSporeImpactVfx:['ground_color','孢子冲击'],
 NBolasVfx:['creature_pair','流星锤 · 投射（使用原生发射点）'],
 NGroundFireVfx:['creature_color','地面火焰'],NSpikeSplashVfx:['creature_color','地刺飞溅'],
 NThinSliceVfx:['creature_color','细刃斩击'],NStabVfx:['creature_bool_color','穿刺'],
 NHorizontalLinesVfx:['screen_color','旋风斩 · 速度线'],NSmokyVignetteVfx:['screen_color','旋风斩 · 屏幕晕影'],
};
for(const [name,code] of texts){
 if(!name.startsWith('N')||!name.endsWith('Vfx'))continue;
 const scene=code.match(/scenePath = SceneHelper.GetScenePath\("([^"]+)"\)/)?.[1] || code.match(/(?:_path|_scenePath|scenePath) = "res:\/\/scenes\/([^"]+)\.tscn"/)?.[1];if(!scene)continue;
 const f=factories[name];
 entry('factory:'+name,'vfx',f?.[1]||name,{adapter:f?'factory':'',factory:name,signature:f?.[0]||'',resource:scene,resources:['res://scenes/'+scene+'.tscn'],status:f?'adapted':'candidate',reason:f?'位置工厂已接入；待游戏内验证':'依赖专用初始化；尚不能独立绑定',embedded_audio:quoted(code).filter(x=>x.startsWith('event:/')),shake:code.includes('ScreenShake(')});
}
function sound(s){if(s.startsWith('event:/'))return entry('event:'+s,'sfx',s.split('/').pop().replaceAll('_',' '),{adapter:'event',resource:s,status:'adapted',reason:'原版音频事件；可试播，待逐项验证'});if(/\.(mp3|ogg|wav)$/.test(s)&&!s.includes('/'))return entry('audio:'+s,'sfx',s,{adapter:'audio',resource:'res://debug_audio/'+s,status:pack.entries.has('debug_audio/'+s)?'adapted':'candidate',reason:'原版临时音频文件'});}
function references(code){const ids=[];for(const s of quoted(code)){if(s.startsWith('vfx/'))ids.push(entry('scene:'+s,'vfx',path.basename(s),{adapter:'',resource:s}).id);const a=sound(s);if(a)ids.push(a.id);}for(const m of code.matchAll(/\b(N\w+Vfx)\.(?:Create|scenePath)/g))if(allEntries.has('factory:'+m[1]))ids.push('factory:'+m[1]);for(const [k,v]of constants)if(code.includes(k))ids.push('scene:'+v);return unique(ids);}
for(const [name,code]of texts)if(!name.startsWith('<')){const refs=references(code);for(const id of refs){const e=allEntries.get(id);if(e)e.sources.push(name);} }
// Scene text and dependency strings are read locally, only identifiers enter the catalog.
for(const e of [...allEntries.values()].filter(e=>e.kind==='vfx')){
 const seen=new Set();const todo=[`scenes/${e.resource}.tscn`];
 while(todo.length&&seen.size<150){const p=todo.pop();if(seen.has(p))continue;seen.add(p);const txt=pack.read(p);for(const s of quoted(txt)){if(s.startsWith('event:/')){sound(s).sources.push(e.factory||e.resource);e.embedded_audio=unique([...(e.embedded_audio||[]),s]);}if(s.startsWith('res://')){e.resources.push(s);const dep=s.slice(6);if(/\.(tscn|tres|gd)$/.test(dep)&&pack.entries.has(dep))todo.push(dep);}}}
 e.resources=unique(e.resources);e.scene_inspected=pack.entries.has(`scenes/${e.resource}.tscn`);
}
for(const name of pack.entries.keys())if(/^debug_audio\/.+\.(mp3|ogg|wav)$/.test(name))sound(path.basename(name));
const knownPresets={
 Hyperbeam:{launch:'factory:NHyperbeamVfx',hit:'factory:NHyperbeamImpactVfx',delay:0.5,animation:'Cast',embedded_audio:true},
 GrandFinale:{launch:'factory:NGrandFinaleVfx',hit:'factory:NGrandFinaleImpactVfx',hit_sound:'audio:blunt_attack.mp3',delay:1.625,animation:'Attack',pre_animation:true},
 SweepingBeam:{launch:'factory:NSweepingBeamVfx',delay:0.5,animation:'Cast'},
 Bolas:{launch:'factory:NBolasVfx',pre_animation:true,hit:'scene:vfx/vfx_attack_blunt',hit_sound:'audio:blunt_attack.mp3'},
 Bombardment:{launch:'factory:NLargeMagicMissileVfx',launch_on_targets:true,pre_animation:true,tint:'50b598',delay:0.2,hit:'scene:vfx/vfx_attack_blunt',hit_sound:'audio:blunt_attack.mp3'},
 MeteorStrike:{launch:'factory:NLargeMagicMissileVfx',launch_on_targets:true,pre_animation:true,tint:'50b598',delay:0.2,hit_sound:'audio:blunt_attack.mp3'},
 Comet:{launch:'factory:NSmallMagicMissileVfx',launch_on_targets:true,tint:'50b598',delay:0.2,animation:'Cast'},
 GuidingStar:{launch:'factory:NSmallMagicMissileVfx',launch_on_targets:true,tint:'50b598',delay:0.2,animation:'Cast',launch_sound:'event:event:/sfx/characters/regent/regent_guiding_star'},
 Breakthrough:{launch:'scene:vfx/vfx_bloody_impact',pre_animation:true,hit:'scene:vfx/vfx_attack_blunt',hit_sound:'audio:heavy_attack.mp3'},
 Cinder:{hit:'factory:NFireBurstVfx',scale:0.75},
 Claw:{hit:'factory:NScratchVfx'},Maul:{hit:'factory:NScratchVfx'},RipAndTear:{hit:'factory:NScratchVfx'},
 Conflagration:{launch:'factory:NGroundFireVfx',launch_on_targets:true,pre_animation:true,hit:'scene:vfx/vfx_attack_blunt',hit_sound:'audio:heavy_attack.mp3'},
 CrushUnder:{launch:'factory:NSpikeSplashVfx',launch_on_targets:true,animation:'Cast',hit:'scene:vfx/vfx_heavy_blunt',ground:true,hit_sound:'audio:blunt_attack.mp3'},
 Stomp:{launch:'factory:NSpikeSplashVfx',launch_on_targets:true,animation:'Cast',hit:'scene:vfx/vfx_heavy_blunt',ground:true,hit_sound:'audio:heavy_attack.mp3'},
 DaggerSpray:{launch:'factory:NDaggerSprayFlurryVfx',hit:'factory:NDaggerSprayImpactVfx',tint:'b1ccca',launch_sound:'event:event:/sfx/characters/silent/silent_dagger_spray'},
 DaggerThrow:{launch:'factory:NDaggerSprayFlurryVfx',hit:'factory:NDaggerSprayImpactVfx',tint:'b1ccca'},
 FiendFire:{hit:'factory:NGroundFireVfx',hit_sound:'event:event:/sfx/characters/attack_fire'},
 Finisher:{hit:'factory:NStabVfx',hit_sound:'audio:blunt_attack.mp3'},
 FlashOfSteel:{launch:'factory:NThinSliceVfx',launch_on_targets:true,pre_animation:true,vfx_color:'Cyan'},
 Neutralize:{launch:'factory:NThinSliceVfx',launch_on_targets:true,pre_animation:true,vfx_color:'Cyan'},
 Suppress:{launch:'factory:NThinSliceVfx',launch_on_targets:true,pre_animation:true,vfx_color:'Cyan'},
 Slice:{launch:'factory:NThinSliceVfx',launch_on_targets:true,pre_animation:true,vfx_color:'Red',hit:'scene:vfx/vfx_attack_slash',hit_sound:'audio:slash_attack.mp3'},
 GunkUp:{hit:'factory:NGoopyImpactVfx',tint:'00ff00',hit_sound:'audio:blunt_attack.mp3'},
 MinionDiveBomb:{launch:'factory:NMinionDiveBombVfx',launch_on_targets:true,animation:'Cast',delay:0.65},
 PerfectedStrike:{hit:'factory:NBigSlashVfx',hit_extras:['factory:NBigSlashImpactVfx'],hit_sound:'audio:heavy_attack.mp3'},
 Shiv:{hit:'factory:NShivThrowVfx',tint:'00ff00'},Skewer:{hit:'factory:NStabVfx',vfx_color:'Gold'},
 Whirlwind:{launch:'factory:NHorizontalLinesVfx',launch_extras:['factory:NSmokyVignetteVfx'],pre_animation:true,tint:'ffffff80',launch_sound:'event:event:/sfx/characters/ironclad/ironclad_whirlwind',hit:'scene:vfx/vfx_giant_horizontal_slash'},
 DramaticEntrance:{launch:'scene:vfx/vfx_dramatic_entrance_fullscreen',hit:'scene:vfx/vfx_attack_slash'},
 DyingStar:{hit:'scene:vfx/vfx_starry_impact'},
 HandOfGreed:{hit:'scene:vfx/vfx_attack_blunt',hit_sound:'audio:blunt_attack.mp3'},
};
const presetNotes={
 Bolas:'原生工厂跟随角色默认发射点，暂不支持枪口偏移。',
 FiendFire:'复用地面火焰和声音；不复制消耗手牌、随手牌数逐段放大的逻辑。',
 Whirlwind:'速度线使用 2 秒视觉窗口；段数由能天使卡牌决定，建议出手仅一次。',
 Shiv:'复用绿色投射物；不复制猎手专用动作及刀扇能力。',
 CrushUnder:'使用能天使 Cast 动作，保留地刺和脚底重击。',
 Stomp:'使用能天使 Cast 动作，保留地刺和脚底重击。',
 Conflagration:'建议出手仅一次，命中随实际攻击段执行。',
 DaggerSpray:'建议出手仅一次，避免群体音效逐段重叠。',
 DyingStar:'复用攻击星光；不包含原卡施加减益后的额外斩击。',
 HandOfGreed:'复用攻击钝击与声音；击杀后的金币奖励演出不作为无条件命中特效。',
};
const limitedSources={
 SovereignBlade:'完整演出依赖已锻造君王之剑的持久节点与攻击状态；可单独选择巨型斩击及关联声音。',
 DramaticEntrance:'全屏场景依赖独立屏幕坐标容器，尚未适配；可单独选择斩击。',
 DyingStar:'攻击星光和施加减益后的斩击处于不同阶段，尚未合成为完整预设。',
 HandOfGreed:'金币爆炸只在击杀后发生，不能作为每次命中的无条件预设。',
};
// This direct scene uses the same native WithHitFx path as the common scenes.
entry('scene:vfx/vfx_molten_fist','vfx','熔融之拳',{status:'adapted',adapter:'scene',resource:'vfx/vfx_molten_fist',resources:['res://scenes/vfx/vfx_molten_fist.tscn'],reason:'原版 WithHitFx 场景；待游戏内验证'});
Object.assign(allEntries.get('scene:vfx/vfx_molten_fist'),{status:'adapted',adapter:'scene'});
Object.assign(allEntries.get('scene:vfx/vfx_dramatic_entrance_fullscreen'),{status:'adapted',adapter:'fullscreen',reason:'使用原版视口中心锚点；待游戏内验证'});
knownPresets.MoltenFist={hit:'scene:vfx/vfx_molten_fist',hit_sound:'audio:blunt_attack.mp3'};
function atom(s){s=s?.trim().replace(/^\w+:\s*/,'');if(!s||s==='null')return '';return s.startsWith('"')?s.slice(1,-1):constants.get(s)||'';}
for(const filename of files){
 const name=path.basename(filename,'.cs'),code=texts.get(name);const isCard=filename.includes('Models.Cards'+path.sep),monster=filename.includes('Models.Monsters'+path.sep);
 if(!isCard&&!monster)continue;
 if(!(isCard?/:\s*base\([^;{}]*CardType.Attack/.test(code):/DamageCmd.Attack|CreatureCmd.Damage/.test(code)))continue;
 const refs=references(code);const indirect=unique([...code.matchAll(/\b(N\w+Vfx)\./g)].map(m=>m[1]));
 for(const n of indirect)if(texts.has(n))refs.push(...references(texts.get(n)));
 const sourceInfo={id:name,...display(name,monster?'monsters':'cards'),origin:monster?'monster':'card',effects:unique(refs)};
 let preset=knownPresets[name];
 const hitMatches=[...code.matchAll(/\.WithHitFx\(([^\)]*)\)/g)];
 if(!preset&&isCard&&indirect.length===0&&hitMatches.length===1&&!/\.BeforeDamage\(|\.AfterAttackerAnim\(|VfxCmd\.|SfxCmd\.|\.WithAttackerFx\(/.test(code)){
  const values={vfx:'',sfx:'',tmpSfx:''};
  hitMatches[0][1].split(',').forEach((part,i)=>{const key=part.match(/^\s*(\w+):/)?.[1]||['vfx','sfx','tmpSfx'][i];values[key]=atom(part);});
  const {vfx:v,sfx:s,tmpSfx:t}=values;
  if((!v||allEntries.get('scene:'+v)?.status==='adapted')&&hitMatches[0][1].split(',').every(x=>/^\s*(?:\w+:\s*)?(?:"[^"]*"|null|VfxCmd\.\w+)\s*$/.test(x)))
   preset={hit:v?'scene:'+v:'',hit_sound:sound(s||t||'')?.id||'',animation:code.match(/WithAttackerAnim\("(\w+)"/)?.[1]||'Attack',delay:0};
 }
 if(preset?.animation && !['Attack','Cast'].includes(preset.animation)){
  preset={...preset,animation:'Attack'};
  presetNotes[name]=(presetNotes[name]||'')+'原角色专用动作映射为能天使已有 Attack 动作。';
 }
 if(preset){entry('card:'+name,'preset',sourceInfo.name,{...sourceInfo,id:'card:'+name,status:'adapted',reason:'演出映射已接入；待游戏内验证。'+(presetNotes[name]||''),adapter:'preset',recipe:preset,sources:[name],english:sourceInfo.english});sourceInfo.status='mapped';}
 else {const noEffect=['EchoingSlash','Omnislice'].includes(name);const reason=noEffect?'当前 OnPlay 直接结算伤害，无独立出手/命中特效；可自行绑定效果。':limitedSources[name]||(monster?'怪物攻击需拆分具体招式及锚点':'专用演出或条件时序待适配；可从关联素材中选择已支持项');entry('card:'+name,'preset',sourceInfo.name,{...sourceInfo,id:'card:'+name,status:'candidate',reason,sources:[name]});sourceInfo.status=noEffect?'no_independent_effect':refs.length?'needs_adapter':'needs_investigation';sourceInfo.reason=reason;}
 audit.push(sourceInfo);
}
// Generic skill/power recipes use the same native one-shot assets as combat commands.
for (const [id,name,recipe] of [
 ['Buff','增益通用 · 技能 / 能力',{launch:'scene:vfx/vfx_starry_impact',launch_sound:'event:event:/sfx/buff'}],
 ['Block','格挡通用 · 技能',{launch:'scene:vfx/vfx_block',launch_sound:'event:event:/sfx/block_gain'}],
 ['Heal','治疗通用 · 技能 / 能力',{launch:'scene:vfx/vfx_cross_heal',launch_sound:'event:event:/sfx/heal'}],
 ['Debuff','减益通用 · 技能',{hit:'scene:vfx/vfx_starry_impact',hit_sound:'event:event:/sfx/debuff'}],
 ['Power','能力启动通用 · 能力',{launch:'scene:vfx/vfx_starry_impact',launch_sound:'event:event:/sfx/buff'}],
]) entry('generic:'+id,'preset',name,{status:'adapted',adapter:'preset',origin:'combat',recipe:{...recipe,animation:''},english:id+' skill power',reason:'通用技能 / 能力组合，复用原版素材；不改变卡牌结算。增益、格挡、治疗作用于自身，减益显示于试播目标；待游戏内验证。',sources:[]});
// Give resource searches the original localized source names without duplicating resources.
for(const e of allEntries.values()){
 e.sources=unique(e.sources);e.aliases=unique(e.sources.flatMap(n=>[display(n).name,display(n).english,display(n,'monsters').name]));
 e.origins=unique([...(e.origin?[e.origin]:[]),...audit.filter(s=>s.effects.includes(e.id)).map(s=>s.origin)]);
 if(e.adapter==='event'&&/\/characters\/|\/enemy\//.test(e.resource))e.origins.push('combat');
 if(e.adapter==='audio')e.origins.push('combat');
 if(!e.origins.length)e.origins=['other'];
}
const entries=[...allEntries.values()].sort((a,b)=>a.id.localeCompare(b.id));
// Loop/ambient events cannot be owned or stopped through the one-shot API.
const loopEvents=new Set([...texts.values()].flatMap(code=>[...code.matchAll(/(?:PlayLoop|StartSfxLoop)\([^;]*?"(event:\/[^"\r\n]+)"/g)].map(m=>m[1])));
for(const e of entries)if(e.adapter==='event'&&(loopEvents.has(e.resource)||/loop|event:\/(?:music|ambience)\//i.test(e.resource))){e.status='candidate';e.reason='循环/环境音需要独立生命周期控制，暂不支持作为一次性卡牌声音。';}
for(const e of entries.filter(e=>e.kind==='preset'&&e.status==='adapted'))for(const key of ['launch','hit','launch_sound','hit_sound','launch_extras','hit_extras'])
 for(const id of [e.recipe[key]||[]].flat())if(allEntries.get(id)?.status!=='adapted')throw Error('Unsupported recipe dependency: '+e.id+' '+key+' '+id);
const catalog={schema:1,game_sha256:dllHash,entries};
const auditDoc={schema:1,game_sha256:dllHash,method:'C# source references + recursive text scene dependencies + PCK debug audio inventory',limitations:['动态资源名及间接助手仍需逐项复核','FMOD 事件来自代码/场景引用，未声称穷举音频 bank','adapted 表示已接入，游戏内逐项验证另行记录'],counts:{sources:audit.length,mapped:audit.filter(x=>x.status==='mapped').length,entries:entries.length,adapted:entries.filter(x=>x.status==='adapted').length},sources:audit.sort((a,b)=>a.id.localeCompare(b.id))};
fs.mkdirSync(path.join(root,'AK_Exusiai/config'),{recursive:true});
fs.writeFileSync(path.join(root,'AK_Exusiai/config/card_effect_catalog.json'),JSON.stringify(catalog,null,2)+'\n');
fs.writeFileSync(path.join(here,'catalog_audit.json'),JSON.stringify(auditDoc,null,2)+'\n');
pack.close();console.log(JSON.stringify(auditDoc.counts));
