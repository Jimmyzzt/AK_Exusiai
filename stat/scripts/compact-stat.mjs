import {execFileSync} from 'node:child_process';
import {mkdir,writeFile} from 'node:fs/promises';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {resolve} from 'node:path';
import assert from 'node:assert/strict';
import {packBlock,unpackBlock} from '../shared/block-codec.mjs';
import {validateBundle} from '../shared/public-data.mjs';
import {previousRelease} from './public-release.mjs';
const literal=value=>"'"+value.replaceAll("'","''")+"'";
/** Only change encoding of the exact aggregate revision present in a verified public release. */
export function compactStatements(bundle){
 validateBundle(bundle);let before=0,after=0,skipped=0;const statements=[];
 for(const cell of bundle.cells){
  const packed=packBlock(cell.block);assert.deepEqual(unpackBlock(packed),cell.block);
  const payload=JSON.stringify(packed),sql='UPDATE stat_cells SET payload='+literal(payload)+' WHERE key='+literal(cell.key)+' AND revision='+cell.revision+';';
  if(Buffer.byteLength(sql)>90000){skipped++;continue;}
  before+=Buffer.byteLength(JSON.stringify(cell.block));after+=Buffer.byteLength(payload);statements.push(sql);
 }
 return {statements,summary:{cells:statements.length,skipped,expanded_payload_bytes:before,compact_payload_bytes:after}};
}
if(process.argv[1]&&pathToFileURL(resolve(process.argv[1])).href===import.meta.url){
 if(process.argv.slice(2).join()!=='--remote')throw new Error('Choose --remote explicitly; this tool uses the verified production publication');
 const release=await previousRelease();if(!release)throw new Error('Publish a complete release before compacting');
 const {statements,summary}=compactStatements(release.bundle),folder=new URL('../.publish/',import.meta.url),file=new URL('compact.sql',folder);
 await mkdir(folder,{recursive:true});await writeFile(file,statements.join('\n'));
 console.log(JSON.stringify({event:'compaction_plan',source_revision:release.bundle.source_revision,...summary}));
 if(statements.length)execFileSync(process.execPath,[fileURLToPath(new URL('../node_modules/wrangler/bin/wrangler.js',import.meta.url)),'d1','execute','exusiai-stat','--remote','--config','worker/wrangler.jsonc','--file',fileURLToPath(file)],{cwd:fileURLToPath(new URL('../',import.meta.url)),stdio:'inherit'});
 console.log('Encoding conversion complete. Newer aggregate revisions were preserved by the revision guard. Allocated database size may not immediately shrink.');
}
