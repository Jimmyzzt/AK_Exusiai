import {validateBundle} from './public-data.mjs';
import {packBlock,unpackBlock} from './block-codec.mjs';
/** Version 2 only changes transport, not the statistical schema or model. */
export function encodePublished(bundle){
 validateBundle(bundle);const blocks=[],index=new Map();
 const cells=bundle.cells.map(cell=>{
  const packed=packBlock(cell.block),key=JSON.stringify(packed);let block=index.get(key);
  if(block===undefined){block=blocks.length;index.set(key,block);blocks.push(packed);}
  return {...cell,block};
 });
 return {...bundle,schema_version:2,cells,blocks};
}
export function decodePublished(wire){
 if(wire?.schema_version===1)return validateBundle(wire);
 if(!wire||wire.schema_version!==2||Object.keys(wire).sort().join()!=='algorithm,blocks,cells,generated_at,model,mods,pair_mods,schema_version,source_revision,versions'||!Array.isArray(wire.blocks)||!Array.isArray(wire.cells))throw new Error('Invalid public encoding');
 if(wire.blocks.some(block=>block?.codec!==1))throw new Error('Invalid public block encoding');
 const blocks=wire.blocks.map(unpackBlock),used=new Set();
 const cells=wire.cells.map(cell=>{
  if(!Number.isInteger(cell.block)||cell.block<0||cell.block>=blocks.length)throw new Error('Invalid public block reference');
  used.add(cell.block);
  return {...cell,block:blocks[cell.block]};
 });
 if(used.size!==blocks.length)throw new Error('Unused public block');
 const {blocks:unused,...root}=wire;return validateBundle({...root,schema_version:1,cells});
}
export function publishedText(bundle,version=2){return JSON.stringify(version===2?encodePublished(bundle):validateBundle(bundle));}
