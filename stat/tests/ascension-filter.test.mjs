import test from 'node:test';
import assert from 'node:assert/strict';
import {filters} from '../shared/filter.mjs';
import {selectPublic} from '../shared/public-data.mjs';
import {emptyBlock,OVERVIEW_FIELDS,SCOPES} from '../shared/statistics.mjs';
test('other ascension selects only above A10 in public statistics',()=>{
 const cells=[0,10,11,100].map(a=>{const block=emptyBlock();for(const scope of SCOPES)block.overview[SCOPES.indexOf(scope)][OVERVIEW_FIELDS.indexOf('runs')]=1;return {dims:['2026-10-11','v','r',a,1,'Standard',0,0,0],projection:[],block};});
 const b={generated_at:1,mods:[],versions:[],pair_mods:[],cells};const ask=a=>selectPublic(b,filters(new URL('https://test/?ascension='+a))).overview.runs;
 assert.equal(ask('other'),2);assert.equal(ask('10'),1);assert.equal(ask(''),4);assert.throws(()=>ask('11'));
});
