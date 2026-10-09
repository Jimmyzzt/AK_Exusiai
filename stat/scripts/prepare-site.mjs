import {execFileSync} from 'node:child_process';
import {mkdir,readFile,appendFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
const stat=fileURLToPath(new URL('../',import.meta.url)),repo=fileURLToPath(new URL('../../',import.meta.url));
const run=args=>execFileSync('gh',args,{encoding:'utf8',cwd:repo});
let reuse=process.argv.includes('--reuse'),artifact;
if(reuse){
 const response=JSON.parse(run(['api','repos/Jimmyzzt/AK_Exusiai/actions/artifacts?name=github-pages&per_page=30']));
 for(const a of response.artifacts||[]){
  if(a.expired||a.workflow_run?.head_branch!=='main'||String(a.workflow_run.id)===process.env.GITHUB_RUN_ID)continue;
  const record=JSON.parse(run(['api','repos/Jimmyzzt/AK_Exusiai/actions/runs/'+a.workflow_run.id]));
  if(record.status==='completed'&&record.conclusion==='success'){artifact=a;break;}
 }
 if(!artifact)reuse=false;
 if(artifact){
  const paths=['stat/web','stat/shared','stat/scripts/build.mjs','stat/scripts/card-metadata.mjs','AK_ExusiaiCode/Cards','AK_ExusiaiCode/Relics','AK_Exusiai/images','AK_Exusiai/localization','tools/card_art_manager/card_art_preview.gd','references/official/asset/立绘_新约能天使_1.png'];
  if(execFileSync('git',['diff',artifact.workflow_run.head_sha,'HEAD','--',...paths],{cwd:repo,encoding:'utf8'}))reuse=false;
 }
}
if(reuse){
 const download=fileURLToPath(new URL('../.publish/previous-artifact/',import.meta.url));await mkdir(download,{recursive:true});
 run(['run','download',String(artifact.workflow_run.id),'--repo','Jimmyzzt/AK_Exusiai','--name','github-pages','--dir',download]);
 await mkdir(new URL('../dist/',import.meta.url),{recursive:true});
 execFileSync('tar',['-xf',download+'artifact.tar','-C',stat+'dist'],{stdio:'inherit'});
 const build=JSON.parse(await readFile(new URL('../dist/build.json',import.meta.url),'utf8'));
 if(build.public_schema!==2)throw new Error('Previous website schema mismatch');

 console.log(JSON.stringify({event:'website_reused',run:artifact.workflow_run.id}));
}else{
 execFileSync(process.execPath,['scripts/build.mjs'],{cwd:stat,stdio:'inherit'});
 execFileSync(process.execPath,['node_modules/typescript/bin/tsc','--noEmit'],{cwd:stat,stdio:'inherit'});
 execFileSync(process.execPath,['--test',...['worker.test.mjs','web.test.mjs','card-metadata.test.mjs','incremental.test.mjs','publication.test.mjs'].map(s=>'tests/'+s)],{cwd:stat,stdio:'inherit'});
}

if(process.env.GITHUB_OUTPUT)await appendFile(process.env.GITHUB_OUTPUT,'website_changed='+(!reuse)+'\n');
