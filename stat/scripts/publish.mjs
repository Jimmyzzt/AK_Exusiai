import {execFileSync} from 'node:child_process';
import {mkdir,mkdtemp,cp,writeFile,readFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import {join,resolve} from 'node:path';
const stat=fileURLToPath(new URL('../',import.meta.url)),repo=resolve(stat,'..');
const git=(args,options={})=>execFileSync('git',['-c','safe.directory='+repo.replaceAll('\\','/'),...args],{cwd:repo,encoding:'utf8',...options});
const wrangler=resolve(stat,'node_modules/wrangler/bin/wrangler.js');
const runWrangler=args=>execFileSync(process.execPath,[wrangler,...args,'--config','worker/wrangler.jsonc'],{cwd:stat,stdio:'inherit'});
let stage='validate';
const report={build_id:null,worker:'pending',pages:'pending'};
try {
  // Build exactly once. Both platforms receive these identical bytes.
  delete process.env.STAT_API_ORIGIN;
  execFileSync(process.execPath,[resolve(stat,'scripts/build.mjs')],{cwd:stat,stdio:'inherit'});
  execFileSync(process.execPath,[resolve(stat,'node_modules/typescript/bin/tsc'),'--noEmit'],{cwd:stat,stdio:'inherit'});
  execFileSync(process.execPath,['--test','tests/worker.test.mjs','tests/web.test.mjs'],{cwd:stat,stdio:'inherit'});
  report.build_id=JSON.parse(await readFile(join(stat,'dist/build.json'),'utf8')).build_id;
  runWrangler(['deploy','--dry-run']);
  stage='worker'; runWrangler(['d1','migrations','apply','exusiai-stat','--remote']); runWrangler(['deploy']); report.worker='deployed';
  stage='pages';
  const parent=join(stat,'.publish');await mkdir(parent,{recursive:true});const temp=await mkdtemp(join(parent,'pages-'));
  const tempGit=args=>execFileSync('git',['-c','safe.directory='+temp.replaceAll('\\','/'),...args],{cwd:temp,encoding:'utf8'});
  tempGit(['init','--quiet']);tempGit(['config','user.name','Exusiai statistics publisher']);tempGit(['config','user.email','41898282+github-actions[bot]@users.noreply.github.com']);
  tempGit(['remote','add','origin','https://github.com/Jimmyzzt/AK_Exusiai.git']);
  const remote=git(['ls-remote','origin','refs/heads/gh-pages']).trim();
  if(remote)tempGit(['fetch','--depth','1','origin','gh-pages']);
  await cp(join(stat,'dist'),temp,{recursive:true});tempGit(['add','--all']);
  const tree=tempGit(['write-tree']).trim();
  const parentArgs=remote?['-p',tempGit(['rev-parse','FETCH_HEAD']).trim()]:[];
  const commit=execFileSync('git',['-c','safe.directory='+temp.replaceAll('\\','/'),'commit-tree',tree,...parentArgs],{cwd:temp,encoding:'utf8',input:`Publish Exusiai statistics build ${report.build_id}\n`}).trim();
  tempGit(['update-ref','refs/heads/gh-pages',commit]);tempGit(['push','origin','refs/heads/gh-pages:refs/heads/gh-pages']);
  let exists=true;
  try {execFileSync('gh',['api','repos/Jimmyzzt/AK_Exusiai/pages'],{stdio:'pipe'});}catch(error){if(!String(error.stderr).includes('404'))throw error;exists=false;}
  const config={build_type:'workflow'};
  execFileSync('gh',['api','--method',exists?'PUT':'POST','repos/Jimmyzzt/AK_Exusiai/pages','--input','-'],{encoding:'utf8',input:JSON.stringify(config),stdio:['pipe','ignore','inherit']});
  execFileSync('gh',['workflow','run','stat-pages.yml','--repo','Jimmyzzt/AK_Exusiai','--ref','main'],{stdio:'inherit'});
  report.pages='submitted';
  console.log('Worker: https://exusiai.zzt.si\nGitHub Pages: https://jimmyzzt.github.io/AK_Exusiai/');
} catch(error) {report[stage]='failed';console.error(`Publish failed at ${stage}; completed deployments remain live. Retry npm run publish after resolving the error.`);process.exitCode=1;}
await writeFile(join(stat,'dist/deployment-status.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify(report));
