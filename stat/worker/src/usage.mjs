/** @param {D1Database} db */
export function metered(db){
 const usage={rows_read:0,rows_written:0,duration_ms:0,queries:0},originals=new WeakMap();
 const take=r=>{usage.rows_read+=r.meta?.rows_read||0;usage.rows_written+=r.meta?.rows_written||0;usage.duration_ms+=r.meta?.duration||0;usage.queries++;return r;};
 const wrap=statement=>{const proxy=new Proxy(statement,{get(target,property){
  if(property==='bind')return (...args)=>wrap(target.bind(...args));
  if(property==='all'||property==='run')return async(...args)=>take(await target[property](...args));
  if(property==='first')return async(column)=>{const result=take(await target.all()).results[0]??null;return column===undefined?result:result?.[column]??null;};
  const value=target[property];return typeof value==='function'?value.bind(target):value;
 }});originals.set(proxy,statement);return proxy;};
 return {usage,db:new Proxy(db,{get(target,property){
  if(property==='prepare')return sql=>wrap(target.prepare(sql));
  if(property==='batch')return async statements=>(await target.batch(statements.map(s=>originals.get(s)||s))).map(take);
  const value=target[property];return typeof value==='function'?value.bind(target):value;
 }})};
}
export async function account(database,usage){
 const now=Date.now(),window=Math.floor(now/900000),day=Math.floor(now/86400000);
 const r=await database.prepare('UPDATE stat_state SET window=?,reads=CASE WHEN window=? THEN reads+? ELSE ? END,writes=CASE WHEN window=? THEN writes+? ELSE ? END,day=?,day_reads=CASE WHEN day=? THEN day_reads+? ELSE ? END,day_writes=CASE WHEN day=? THEN day_writes+? ELSE ? END WHERE id=1')
 .bind(window,window,usage.rows_read+1,usage.rows_read+1,window,usage.rows_written+1,usage.rows_written+1,day,day,usage.rows_read+1,usage.rows_read+1,day,usage.rows_written+1,usage.rows_written+1).run();
 usage.rows_read+=r.meta.rows_read;usage.rows_written+=r.meta.rows_written;usage.duration_ms+=r.meta.duration;usage.queries++;
}
