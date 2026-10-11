import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile,readdir} from 'node:fs/promises';
import {cardMetadata,renderRules} from '../scripts/card-metadata.mjs';
const translations=Object.fromEntries(await Promise.all(['zhs','eng'].map(async language=>[language,{...JSON.parse(await readFile('../AK_Exusiai/localization/'+language+'/cards.json','utf8')),...JSON.parse(await readFile('../AK_Exusiai/localization/'+language+'/static_hover_tips.json','utf8'))}])));
const extract=async name=>cardMetadata(await readFile('../AK_ExusiaiCode/Cards/'+name+'.cs','utf8'),'AK_EXUSIAI_CARD_'+name.replace(/([a-z0-9])([A-Z])/g,'$1_$2').replace(/([A-Z])([A-Z][a-z])/g,'$1_$2').toUpperCase(),translations);
test('card rules reflect actual upgrade values, cost, keywords and conditional effects',async()=>{
 const shot=await extract('Marksmanship');assert.ok(shot.descriptions.zhs[0].includes('7点伤害'));assert.ok(shot.descriptions.zhs[1].includes('10点伤害'));assert.ok(shot.descriptions.eng[1].includes('10'));
 const star=await extract('Star');assert.ok(star.descriptions.zhs[1].includes('11点伤害'));assert.ok(star.descriptions.zhs[1].includes('所有'));
 const travel=await extract('TheSaintsTravels');assert.deepEqual(travel.cost,['2','1']);assert.ok(travel.descriptions.zhs[0].includes('1点'));
 const ammo=await extract('ExplosiveAmmo');assert.ok(ammo.descriptions.zhs[0].includes('[gold]消耗[/gold]'));assert.ok(!ammo.descriptions.zhs[1].includes('[gold]消耗[/gold]'));
 assert.deepEqual((await extract('LoadEmUp')).cost,['X','X']);assert.equal((await extract('Bewildered')).can_upgrade,false);
 assert.ok((await extract('FreeDelivery')).descriptions.zhs[0].includes('神秘遗物'));
});
test('all current library descriptions resolve without exposing formatting syntax or combat-only values',async()=>{
 let count=0;for(const file of await readdir('../AK_ExusiaiCode/Cards')){if(!file.endsWith('.cs')||file==='ExusiaiCardTemplate.cs')continue;const source=await readFile('../AK_ExusiaiCode/Cards/'+file,'utf8');if(!source.includes('public sealed class'))continue;const metadata=await extract(file.slice(0,-3));for(const language of ['zhs','eng'])for(const description of metadata.descriptions[language]){assert.ok(description.trim());assert.ok(!/[{}]|\\n/.test(description),file+' contains unresolved formatting');}count++;}assert.equal(count,105);
 const strike=await extract('DisruptiveStrike');assert.ok(!strike.descriptions.zhs[0].includes('（命中'));
 assert.throws(()=>renderRules('{Missing:diff()}',{},false),/Unresolved/);
});

test('all relic and ancient descriptions resolve source values in both languages',async()=>{
 const {relicMetadata}=await import('../scripts/card-metadata.mjs');
 const t=Object.fromEntries(await Promise.all(['zhs','eng'].map(async l=>[l,JSON.parse(await readFile('../AK_Exusiai/localization/'+l+'/relics.json','utf8'))])));let count=0;
 for(const file of await readdir('../AK_ExusiaiCode/Relics')){if(!file.endsWith('.cs'))continue;const s=await readFile('../AK_ExusiaiCode/Relics/'+file,'utf8');for(const m of s.matchAll(/public sealed class (\w+)/g)){const id='AK_EXUSIAI_RELIC_'+m[1].replace(/([a-z0-9])([A-Z])/g,'$1_$2').replace(/([A-Z])([A-Z][a-z])/g,'$1_$2').toUpperCase();if(!t.zhs[id+'.title'])continue;const next=s.indexOf('public sealed class ',m.index+m[0].length),v=relicMetadata(s.slice(m.index,next<0?undefined:next),id,t);for(const l of ['zhs','eng']){assert.ok(v.descriptions[l][0].trim());assert.ok(!/[{}]|\\n/.test(v.descriptions[l][0]));}assert.equal(v.can_upgrade,false);if(id.endsWith('EXUSIAI_BADGE'))assert.match(v.descriptions.zhs[0],/5/);if(id.endsWith('CONFESS47'))assert.match(v.descriptions.zhs[0],/2/);count++;}}assert.equal(count,29);
});
