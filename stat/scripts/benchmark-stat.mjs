import {beginWork,workCells,stageWork,commitWork,publicationStatus} from '../worker/src/publication.mjs';
import {mergeWork} from '../shared/statistics.mjs';
import {readFile,writeFile} from 'node:fs/promises';
import {build} from 'esbuild';
import {Miniflare,convertV4MiniflareOptions} from 'miniflare';
import {applySchema} from '../tests/helpers/schema.mjs';
import {filters} from '../shared/filter.mjs';
import {metered,processOne,status,exportPage} from '../worker/src/incremental.mjs';
import {selectPublic,validateBundle,publicCell} from '../shared/public-data.mjs';
import assert from 'node:assert/strict';
async function module(path){const b=await build({entryPoints:[path],bundle:true,write:false,format:'esm',platform:'node'});return import('data:text/javascript;base64,'+Buffer.from(b.outputFiles[0].text).toString('base64'));}
const mf=new Miniflare(convertV4MiniflareOptions({modules:true,script:'export default {fetch(){return new Response("bench")}}',compatibilityDate:'2026-10-06',d1Databases:{DB:'performance-v3'}}));
async function incrementalStep(db){const start=await beginWork(db,{bootstrap:true});if(start.state!=='work')return start;const work=start.work,usage={...start.usage};const collect=u=>{for(const k of ['rows_read','rows_written','duration_ms','queries'])usage[k]+=u[k]||0;};work.existing=[];
 for(let offset=0;offset<work.keys.length;offset+=8){const page=await workCells(db,work.nonce,offset);work.existing.push(...page.cells);collect(page.usage);}
 const updates=mergeWork(work);for(let ordinal=0;ordinal<updates.length;ordinal++){const r=await stageWork(db,{nonce:work.nonce,ordinal,updates:[updates[ordinal]]});collect(r.usage);}
 const committed=await commitWork(db,work.nonce);collect(committed.usage);return {state:committed.state,usage};}
const report={dataset:{runs:82,entities:7100,mods:505,relations:1450,detailed_runs:4,offers:0,fights:0,act_decks:0},queries:[],maintenance:{}};
try{
 const db=await mf.getD1Database('DB');await applySchema(db);
 await db.prepare("INSERT INTO mod_catalog(id,title,primary_tag) SELECT 'm'||printf('%03d',value),'Synthetic Mod','qol' FROM json_each(?)").bind(JSON.stringify(Array.from({length:505},(_,i)=>i))).run();
 async function seed(i){
  const id='synthetic-'+i,n=i<48?87:86,cards=Array.from({length:n},(_,j)=>({id:'AK_EXUSIAI_CARD_BENCH_'+Math.floor(j/4),act:j%4})),mods=Array.from({length:i<56?18:17},(_,j)=>'m'+String((i*17+j)%505).padStart(3,'0'));
  const stmts=[db.prepare("INSERT INTO cohorts(id,day,version,revision,ascension,players,mode,abandoned,runs,wins) VALUES(?,'2026-10-08','bench','bench',0,1,'Standard',0,1,1)").bind(id),db.prepare("INSERT INTO runs VALUES(?,?,'2026-10-08','bench','bench','bench',0,1,'Standard',1,0,48,1800,0,'{}',?,?)").bind(id,'synthetic-owner',id,id),db.prepare("INSERT INTO entities SELECT ?,json_extract(value,'$.id'),json_extract(value,'$.act'),1,3,1,1,8,0,0 FROM json_each(?)").bind(id,JSON.stringify(cards)),db.prepare("INSERT INTO cohort_mods SELECT ?,value FROM json_each(?)").bind(id,JSON.stringify(mods))];
  if(i<4)stmts.push(db.prepare('INSERT INTO run_details VALUES(?,1,48,3)').bind(id));await db.batch(stmts);
 }
 for(let i=0;i<82;i++)await seed(i);
 const f=filters(new URL('https://x'));
 for(const [name,path] of [['original','tests/fixtures/legacy-analytics.ts'],['optimized','worker/src/analytics.ts']]){
  const {calculate}=await module(path),meter=metered(db);
  const detail=[];const wrapped={prepare:meter.db.prepare,batch:async qs=>{const results=await meter.db.batch(qs);detail.push(...results.map((r,i)=>({query:i,rows_read:r.meta.rows_read,rows_written:r.meta.rows_written,duration_ms:r.meta.duration,returned:r.results.length})));return results;}};
  const data=await calculate(wrapped,f);report.queries.push({name,...meter.usage,phases:detail});if(name==='original')report.reference=data;else assert.deepEqual(data.entities,report.reference.entities);
 }
 const init={rows_read:0,rows_written:0,duration_ms:0,jobs:0};
 while(true){const r=await incrementalStep(db);if(r.state!=='processed')break;init.jobs++;for(const k of ['rows_read','rows_written','duration_ms'])init[k]+=r.usage[k];}
 report.maintenance.initialization=init;report.maintenance.implementation="Actions work protocol (one cell per stage in this benchmark; production batches up to eight)";
 const s=await status(db);let page=await exportPage(db,{after:0,through:s.source_revision.statistics,catalog:true,bootstrap:true}),next=page.next,mods=page.mods,cells=[...page.cells],exportUsage={...page.usage};
 while(next){page=await exportPage(db,{after:0,through:s.source_revision.statistics,...next,bootstrap:true});cells.push(...page.cells);next=page.next;for(const k of ['rows_read','rows_written','duration_ms'])exportUsage[k]+=page.usage[k];}
 const live=cells.filter(c=>!c.deleted).map(publicCell),bundle=validateBundle({schema_version:1,generated_at:0,source_revision:s.source_revision,model:'personal-logistic-floor-v2',algorithm:1,pair_mods:s.pair_mods,mods,versions:[{version:'bench',revision:'bench'}],cells:live});
 const local=selectPublic(bundle,f);for(const r of report.reference.entities){const m=local.entities.find(v=>v.id===r.id&&v.act===r.act&&v.variant===r.variant);for(const k of ['offered','picked','picked_runs','owned','owned_wins','picked_wins','obtained','upgraded','removed'])assert.equal(m[k]||0,r[k]||0);}
 report.maintenance.initial_export={...exportUsage,cells:cells.length,bytes:Buffer.byteLength(JSON.stringify(bundle))};
 const idle=await publicationStatus(db);report.maintenance.unchanged_cycle={...idle.usage};
 const before=s.source_revision.statistics;await seed(82);const step=await incrementalStep(db);report.maintenance.one_new_run={...step.usage,state:step.state};
 const now=await status(db);const delta=await exportPage(db,{after:before,through:now.source_revision.statistics,catalog:true,bootstrap:true});report.maintenance.first_delta_page={...delta.usage,returned:delta.cells.length};
 const p=await db.prepare('EXPLAIN QUERY PLAN SELECT key,payload FROM stat_cells WHERE (revision,key)>(?,?) AND revision<=? ORDER BY revision,key LIMIT 16').bind(1,'',100).all();report.pagination_plan=p.results.map(r=>r.detail);
 delete report.reference;await writeFile('docs/PERFORMANCE_2026-10-09.json',JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
}finally{await mf.dispose();}
