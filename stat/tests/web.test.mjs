import {test} from 'node:test';
import assert from 'node:assert/strict';
import {metric} from '../web/metrics.js';
import {officialTags,primaryTag} from '../worker/src/tags.mjs';
test('metric coverage cannot turn absent legacy data into zero or a false win rate',()=>{
 assert.equal(metric('war',{}).text,'—');assert.equal(metric('pwar_rate',{pwar:2,pwar_samples:9}).text,'—');assert.equal(metric('win',{owned:5,owned_wins:2,owned_win_samples:4}).value,50);assert.equal(metric('owned',{owned:5},{hold_samples:10}).value,50);assert.equal(metric('delta_hp',{delta_hp:null,delta_picks:3}).text,'—');assert.equal(metric('pwar_rate',{pwar:2,pwar_samples:10}).value,20);
});
test('main tags prioritize gameplay changes and discard custom tags',()=>{
 const tags=officialTags(['QoL','Cards','Characters','My Character','Chinese']);assert.deepEqual(tags,['Cards','Characters','QoL']);assert.equal(primaryTag(tags),'character');assert.equal(primaryTag(['Cards','QoL']),'extend');assert.equal(primaryTag([]),'untagged');assert.equal(primaryTag(['Cosmetics']),'visual');
});
