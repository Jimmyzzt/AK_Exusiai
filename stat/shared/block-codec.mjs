/** Lossless storage: sparse sums, interned IDs, shared rows across scope/upgrade views. */
export function packBlock(block){
 const ids=[],index=new Map(),groups=new Map();
 for(const row of block.entities){
  let id=index.get(row[2]);if(id===undefined){id=ids.length;ids.push(row[2]);index.set(row[2],id);}
  let mask=0;const values=[];for(let i=5;i<row.length;i++)if(row[i]!==0){mask|=1<<(i-5);values.push(row[i]);}
  const body=[id,row[3],row[4],mask,...values],key=JSON.stringify(body),flag=1<<(row[0]*2+row[1]);
  let candidates=groups.get(key);if(!candidates){candidates=[];groups.set(key,candidates);}
  const previous=candidates.find(group=>(group[0]&flag)===0);if(previous)previous[0]|=flag;else candidates.push([flag,...body]);
 }
 return {codec:1,ids,overview:block.overview,rows:[...groups.values()].flat()};
}
export function unpackBlock(value){
 if(value?.codec===undefined)return value;
 if(value.codec!==1||Object.keys(value).sort().join()!=='codec,ids,overview,rows'||!Array.isArray(value.ids)||value.ids.some(id=>typeof id!=='string')||!Array.isArray(value.rows))throw new Error('Invalid block encoding');
 const entities=[];
 for(const row of value.rows){
  if(!Array.isArray(row)||row.length<5)throw new Error('Invalid encoded row');
  const [views,id,act,variant,mask]=row;
  if(!Number.isInteger(views)||views<1||views>=1024||!Number.isInteger(id)||id<0||id>=value.ids.length||!Number.isInteger(mask)||mask<0||mask>=2**28)throw new Error('Invalid encoded identity');
  const sums=Array(28).fill(0);let offset=5;
  for(let i=0;i<28;i++)if(mask&(1<<i)){if(!Number.isFinite(row[offset]))throw new Error('Invalid encoded sum');sums[i]=row[offset++];}
  if(offset!==row.length)throw new Error('Unexpected encoded values');
  for(let view=0;view<10;view++)if(views&(1<<view))entities.push([Math.floor(view/2),view%2,value.ids[id],act,variant,...sums]);
 }
 entities.sort((a,b)=>a.slice(0,5).join(':').localeCompare(b.slice(0,5).join(':')));
 return {overview:value.overview,entities};
}
