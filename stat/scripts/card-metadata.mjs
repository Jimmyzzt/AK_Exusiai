// Resolve the card library's static rules from current source; no combat or saved-run values.
const KEYWORDS={Retain:['保留','Retain'],Exhaust:['消耗','Exhaust'],Ethereal:['虚无','Ethereal'],Innate:['固有','Innate'],Unplayable:['不能被打出','Unplayable']};
function balanced(text,start,open,close){let depth=0,quoted=false;for(let i=start;i<text.length;i++){if(text[i]==='"'&&text[i-1]!=='\\')quoted=!quoted;if(quoted)continue;if(text[i]===open)depth++;if(text[i]===close&&!--depth)return text.slice(start+1,i);}throw new Error('Unclosed source expression');}
function branches(text){let depth=0;for(let i=0;i<text.length;i++){if(text[i]==='{')depth++;if(text[i]==='}')depth--;if(text[i]==='|'&&depth===0)return [text.slice(0,i),text.slice(i+1)];}throw new Error('Missing localization branch');}
export function renderRules(text,vars,upgraded,language='zhs'){
 let result='';
 for(let i=0;i<text.length;i++){
  if(text[i]!=='{'){result+=text[i];continue;}
  const expression=balanced(text,i,'{','}');i+=expression.length+1;
  const colon=expression.indexOf(':'),key=colon<0?expression:expression.slice(0,colon),format=colon<0?'':expression.slice(colon+1);
  if(key==='IfUpgraded'){if(!format.startsWith('show:'))throw new Error('Unsupported upgrade formatter');result+=renderRules(branches(format.slice(5))[upgraded?0:1],vars,upgraded,language);}
  else if(key==='InCombat')result+=renderRules(branches(format)[1],vars,upgraded,language);
  else {if(!Object.hasOwn(vars,key)||vars[key]==null)throw new Error('Unresolved card variable '+key);if(format&&!['diff()','energyIcons()'].includes(format))throw new Error('Unsupported card formatter '+format);result+=format==='energyIcons()'?(language==='zhs'?`${vars[key]}点能量`:`${vars[key]} Energy`):vars[key];}
 }
 return result.replace(/(?:\\)+n/g,'\n');
}
export function cardMetadata(source,id,translations){
 const strings=Object.fromEntries([...source.matchAll(/const string (\w+)\s*=\s*"([^"]+)"/g)].map(m=>[m[1],m[2]]));
 const key=expression=>{expression=expression.trim();if(/^"[^"]+"$/.test(expression))return expression.slice(1,-1);const named=/^nameof\(([\w.]+)\)$/.exec(expression);return named?named[1].split('.').at(-1):strings[expression]||expression;};
 const number=expression=>{expression=expression.trim();if(/^-?\d+(?:\.\d+)?m?$/.test(expression))return Number(expression.replace(/m$/,''));const initial=new RegExp(`(?:int|decimal)\\s+${expression}\\s*=\\s*(-?[\\d.]+)m?`).exec(source);if(initial)return +initial[1];const getter=new RegExp(`\\b${expression}\\s*\\{\\s*get\\s*=>\\s*(\\w+)`).exec(source);if(getter)return number(getter[1]);throw new Error(id+': unresolved numeric source '+expression);};
 const vars={},start=source.indexOf('CanonicalVars =>');
 if(start>=0){const bracket=source.indexOf('[',start),declaration=balanced(source,bracket,'[',']');
  for(const m of declaration.matchAll(/new\s*(?:(\w+)(?:<([\w.]+)>)?)?\(([^)]*)\)/g)){
   const type=m[1]||'DynamicVar',args=m[3].split(',').map(s=>s.trim());if(type==='CalculatedVar')continue;
   if(type==='DynamicVar')vars[key(args[0])]=number(args[1]);else if(type==='PowerVar')vars[m[2].split('.').at(-1)]=number(args[0]);else if(type.endsWith('Var'))vars[type.slice(0,-3)]=number(args[0]);
  }
 }
 const upgradedVars={...vars},method=/\bOnUpgrade\(\)\s*(=>|\{)/.exec(source);let upgrade='';
 if(method){const bodyStart=method.index+method[0].length;if(method[1]==='{')upgrade=balanced(source,bodyStart-1,'{','}');else upgrade=source.slice(bodyStart,source.indexOf(';',bodyStart));}
 const alias={Vulnerable:'VulnerablePower',Weak:'WeakPower',Strength:'StrengthPower'};
 for(const m of upgrade.matchAll(/DynamicVars(?:\.(\w+)|\[((?:nameof\([^)]*\)|[^\]]+))\])\.UpgradeValueBy\(([^)]+)\)/g)){
  const name=m[1]?(alias[m[1]]||m[1]):key(m[2]);if(!Object.hasOwn(upgradedVars,name))throw new Error(id+': upgrade of unknown variable '+name);upgradedVars[name]+=number(m[3]);
 }
 const base=/\bbase\((-?\d+),\s*CardType\.(\w+),\s*CardRarity\.(\w+)/.exec(source);if(!base)throw new Error(id+': missing card constructor');
 const delta=[...upgrade.matchAll(/EnergyCost\.UpgradeBy\((-?\d+)\)/g)].reduce((n,m)=>n+Number(m[1]),0);
 const x=/HasEnergyCostX\s*=>\s*true/.test(source),cost=x?['X','X']:[+base[1],+base[1]+delta].map(n=>n<0?'—':String(n));
 const keywordSource=/CanonicalKeywords\s*=>\s*\[([^\]]*)\]/.exec(source)?.[1]||'';
 const basic=[...keywordSource.matchAll(/CardKeyword\.(\w+)/g)].map(m=>m[1]),plus=new Set(basic);
 for(const m of upgrade.matchAll(/(Add|Remove)Keyword\(CardKeyword\.(\w+)\)/g))m[1]==='Add'?plus.add(m[2]):plus.delete(m[2]);
 const descriptions={};for(const lang of ['zhs','eng'])descriptions[lang]=[false,true].map(up=>{
  const raw=translations[lang][id+'.description'];if(!raw)throw new Error(id+': missing '+lang+' rules');
  const words=(up?[...plus]:basic).map(k=>{if(!KEYWORDS[k])throw new Error('Unknown keyword '+k);return KEYWORDS[k][lang==='zhs'?0:1];});
  return renderRules(raw,{...(up?upgradedVars:vars),MysteryRelic:translations[lang]['AK_EXUSIAI_MYSTERY_RELIC.title']},up,lang)+(words.length?'\n'+words.map(w=>'[gold]'+w+'[/gold]').join(lang==='zhs'?'。':' · ')+(lang==='zhs'?'。':''): '');
 });
 return {type:base[2],rarity:base[3],cost,descriptions,can_upgrade:! /MaxUpgradeLevel\s*=>\s*0/.test(source)};
}
