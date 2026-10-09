import {unpackBlock} from './block-codec.mjs';
/** Sufficient statistics shared by D1 maintenance, publication validation and the browser. */
export const SCHEMA=1,MODEL='personal-logistic-floor-v2',ALGORITHM=1;
export const TAG_ORDER=['acts','character','extend','visual','audio','qol','library','misc','untagged'];
export const OVERVIEW_FIELDS=['runs','wins','win_samples','abandoned','floor_sum','floor_samples','duration_sum','detailed_runs','hold_samples'];
export const ENTITY_FIELDS=['presence','owned','owned_wins','owned_win_samples','offered','picked','picked_runs','picked_wins','picked_win_samples','obtained','floor_sum','upgraded','removed','war','pwar','war_samples','pwar_samples','n','sx','sx2','sy','sxy','wy','wxy','damage_delta','turns_delta','delta_fights','delta_picks'];
export const SCOPES=['standard','all','1','2','3'];
export const PROTECTED=['AK_Exusiai','STS2-RitsuLib'];
const EXTRA=['ActLikeIt2','EndlessMode','ActToggler','YUILongMap'];
const vec=fields=>Array(fields.length).fill(0);
export const emptyBlock=()=>({overview:SCOPES.map(()=>vec(OVERVIEW_FIELDS)),entities:[]});
export function vectorAdd(a,b,sign=1){for(let i=0;i<a.length;i++)a[i]+=sign*b[i];return a;}
export function mergeBlock(a,b,sign=1){
 for(let i=0;i<SCOPES.length;i++)vectorAdd(a.overview[i],b.overview[i],sign);
 const rows=new Map(a.entities.map(r=>[r.slice(0,5).join(':'),r]));
 for(const source of b.entities){const key=source.slice(0,5).join(':');let target=rows.get(key);if(!target){target=[...source.slice(0,5),...vec(ENTITY_FIELDS)];rows.set(key,target);}for(let i=5;i<source.length;i++)target[i]+=sign*source[i];}
 a.entities=[...rows.values()].sort((x,y)=>x.slice(0,5).join(':').localeCompare(y.slice(0,5).join(':')));return a;
}
/** Input is private, indexed per-run facts; only the returned sufficient sums enter rollups. */
export function contribution(facts){
 const {run:r,details:d,entities:legacy,acts,decks,items,offers,fights,mods}=facts;
 const maxAct=d?.max_act??Math.max(1,...legacy.map(e=>e.act));
 const extended=mods.some(m=>m.primary_tag==='acts'||EXTRA.includes(m.id));
 const win3=d?.win3??(legacy.some(e=>e.act>3)?1:extended?null:r.victory);
 const tagMask=mods.filter(m=>!PROTECTED.includes(m.id)).reduce((mask,m)=>mask|(1<<TAG_ORDER.indexOf(TAG_ORDER.includes(m.primary_tag)?m.primary_tag:'untagged')),0);
 const dims=[r.day,r.version,r.revision,r.ascension,r.players,r.mode,r.abandoned,+(r.abandoned&&win3!==1),tagMask];
 const block=emptyBlock(),out=new Map();
 const add=(s,split,id,act,values)=>{const variant=values.__variant??-1,key=[s,split,id,act,variant].join(':');let row=out.get(key);if(!row){row=[s,split,id,act,variant,...vec(ENTITY_FIELDS)];out.set(key,row);}row[5]++;for(const [k,v] of Object.entries(values))if(k!=='__variant'&&v!=null)row[5+ENTITY_FIELDS.indexOf(k)]+=v;};
 for(let s=0;s<SCOPES.length;s++){
  const scope=SCOPES[s],one=Number(scope),single=Number.isInteger(one)&&one>0;
  if(single&&maxAct<one)continue;
  const inScope=act=>scope==='all'?act>0:single?act===one:act>=1&&act<=3;
  const snapshot=scope==='all'?maxAct:single?one:Math.min(maxAct,3);
  const w=scope==='all'?r.victory:win3,known=w!=null;
  const safe=scope==='all'||(!single&&!extended&&maxAct<=3);
  const floor=scope==='all'?r.floor:single?acts.find(a=>a.act===one)?.floors??null:d?d.floor3:!extended?r.floor:null;
  const hold=d?+(acts.some(a=>a.act===snapshot&&a.snapshot_known===1)):+safe;
  block.overview[s]=[1,w??0,+known,+(r.abandoned&&w!==1),floor??0,+(floor!=null),r.duration,+!!d,hold];
  for(const split of [0,1]){
   const v=variant=>split?variant:-1;
   const chosen=new Map();
   if(!d&&!split){
    for(const e of legacy){
     if(e.act>0&&inScope(e.act)&&e.entity_id.startsWith('AK_EXUSIAI_CARD_')){
      const a={offered:e.offered,picked:e.picked,picked_runs:+(e.picked>0),picked_wins:e.picked>0?w??0:0,picked_win_samples:+(e.picked>0&&known)};
      add(s,split,e.entity_id,e.act,a);add(s,split,e.entity_id,0,a);
      if(e.picked>0)chosen.set(e.entity_id+':-1',{id:e.entity_id,variant:-1});
     }
     if(e.act===0&&safe){
      add(s,split,e.entity_id,0,{obtained:e.obtained,floor_sum:e.floor_sum,upgraded:e.upgraded,removed:e.removed,offered:e.entity_id.startsWith('AK_EXUSIAI_RELIC_')?e.offered:0,picked:e.entity_id.startsWith('AK_EXUSIAI_RELIC_')?e.picked:0});
      if(e.owned===1)add(s,split,e.entity_id,0,{owned:1,owned_wins:w??0,owned_win_samples:+known});
     }
    }
   }
   const actPicked=new Set(),firstPicked=new Map();
   for(const o of offers.filter(o=>inScope(o.act))){
    const variant=v(o.variant),residual=known?w-o[scope==='all'?'expected_all':'expected3']:null;
    const values={__variant:variant,offered:1,picked:o.picked,war:residual,pwar:o.picked?residual:0,war_samples:+known,pwar_samples:o.picked,n:1,sx:o.position,sx2:o.position**2,sy:o.picked,sxy:o.position*o.picked,wy:residual,wxy:residual==null?null:o.position*residual};
    add(s,split,o.entity_id,o.act,values);add(s,split,o.entity_id,0,values);
    if(o.picked){
     const key=o.entity_id+':'+variant;chosen.set(key,{id:o.entity_id,variant});
     const actKey=key+':'+o.act;
     if(!actPicked.has(actKey)){add(s,split,o.entity_id,o.act,{picked_runs:1,picked_wins:w??0,picked_win_samples:+known,__variant:variant});actPicked.add(actKey);}
     const old=firstPicked.get(key);if(!old||o.position<old.position)firstPicked.set(key,{id:o.entity_id,variant,position:o.position});
    }
    // add uses the variant in its identity below, assigned via current loop.
   }
   for(const e of items.filter(e=>inScope(e.act))){
    const values={obtained:e.obtained,floor_sum:e.floor_sum,upgraded:e.upgraded,removed:e.removed,offered:e.entity_id.startsWith('AK_EXUSIAI_RELIC_')?e.offered:0,picked:e.entity_id.startsWith('AK_EXUSIAI_RELIC_')?e.picked:0};
    add(s,split,e.entity_id,e.act,{...values,__variant:v(e.variant)});add(s,split,e.entity_id,0,{...values,__variant:v(e.variant)});
   }
   const held=new Set();
   for(const e of decks.filter(e=>e.act===snapshot)){
    const variant=v(e.variant),key=e.entity_id+':'+variant;if(held.has(key))continue;held.add(key);
    add(s,split,e.entity_id,0,{owned:1,owned_wins:w??0,owned_win_samples:+known,__variant:variant});
   }
   // Overall pick counts are DISTINCT per run across acts, not the sum of act counts.
   for(const c of chosen.values()){
    const key=[s,split,c.id,0,c.variant].join(':');let row=out.get(key);
    if(row){for(const field of ['picked_runs','picked_wins','picked_win_samples'])row[5+ENTITY_FIELDS.indexOf(field)]=0;}
    add(s,split,c.id,0,{picked_runs:1,picked_wins:w??0,picked_win_samples:+known,__variant:c.variant});
   }
   for(const p of firstPicked.values()){
    const later=fights.filter(b=>b.position>p.position&&b.expected_damage!=null&&inScope(b.act));
    if(later.length)add(s,split,p.id,0,{damage_delta:later.reduce((n,b)=>n+b.damage-b.expected_damage,0),turns_delta:later.reduce((n,b)=>n+(b.expected_turns==null?0:b.turns-b.expected_turns),0),delta_fights:later.length,delta_picks:1,__variant:p.variant});
   }
  }
 }
 block.entities=[...out.values()];
 return {dims,block,mods:mods.filter(m=>!PROTECTED.includes(m.id)).map(m=>m.id).sort(),normalized:{max_act:maxAct,extended:+extended,win3}};
}
export function finishEntities(rows){
 return rows.map(row=>{const n=row.n||0,den=n*(row.sx2||0)-(row.sx||0)**2;return {...row,picked_wins:row.picked_runs>0&&!row.picked_win_samples?null:row.picked_wins,delta_pick:n>=20&&den>0?100*(n*(row.sxy||0)-(row.sx||0)*(row.sy||0))/den:null,delta_war:n>=20&&den>0?100*(n*(row.wxy||0)-(row.sx||0)*(row.wy||0))/den:null,delta_hp:row.delta_picks>=10?row.damage_delta/row.delta_fights:null,delta_turns:row.delta_picks>=10?row.turns_delta/row.delta_fights:null};});
}
/** Actions performs these merges privately in memory. No per-run work enters a Pages artifact. */
export function mergeWork(work){
 const existing=new Map(work.existing.map(row=>[row.key,unpackBlock(typeof row.payload==='string'?JSON.parse(row.payload):row.payload)]));
 return work.keys.map(spec=>{
  const block=existing.get(spec.key)||emptyBlock();
  for(const [c,sign] of [[work.old,-1],[work.next,1]])if(c&&c.algorithm===spec.algorithm&&JSON.stringify(c.dims)===JSON.stringify(spec.dims)&&projectionsFor(c).some(p=>JSON.stringify(p)===JSON.stringify(spec.projection)))mergeBlock(block,c.block,sign);
  block.entities=block.entities.filter(r=>r[5]>0);
  if(block.overview.some(row=>row.some(v=>v<0)))throw new Error('Negative contribution');
  return {key:spec.key,revision:work.revision,dims:spec.dims,projection:spec.projection,block};
 });
}
export function projectionsFor(c){
 if(!c)return [];const common=c.mods.filter(id=>c.pair_mods.includes(id));
 return [[],...c.mods.map(id=>[id]),...common.flatMap((a,i)=>common.slice(i+1).map(b=>[a,b]))];
}
