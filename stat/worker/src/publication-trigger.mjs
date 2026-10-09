const endpoint='https://api.github.com/repos/Jimmyzzt/AK_Exusiai/actions/workflows/stat-data.yml/dispatches';
/** Optional secret: no new authorization is provisioned by deploying this code. */
export async function dispatchPublication(env,fetcher=fetch,log=console.info){
 if(!env.GITHUB_PUBLICATION_TOKEN)return {state:'disabled'};
 try{
  const response=await fetcher(endpoint,{method:'POST',redirect:'error',signal:AbortSignal.timeout(10000),headers:{Authorization:'Bearer '+env.GITHUB_PUBLICATION_TOKEN,Accept:'application/vnd.github+json','Content-Type':'application/json','User-Agent':'exusiai-stat-worker','X-GitHub-Api-Version':'2026-03-10'},body:JSON.stringify({ref:'main',inputs:{bootstrap:false}})});
  const result={state:[200,204].includes(response.status)?'dispatched':'failed',http_status:response.status};
  await response.body?.cancel();log(JSON.stringify({event:'publication_trigger',...result}));return result;
 }catch{
  const result={state:'failed',http_status:null};log(JSON.stringify({event:'publication_trigger',...result}));return result;
 }
}
/** Steam or D1 failures must not prevent the independent publication trigger. */
export function scheduleMaintenance(env,ctx,enrich,dispatch=dispatchPublication,log=console.error){
 ctx.waitUntil(Promise.allSettled([
  Promise.resolve().then(()=>enrich(env.DB)),
  Promise.resolve().then(()=>dispatch(env)),
 ]).then(results=>{results.forEach((result,i)=>{if(result.status==='rejected')log(JSON.stringify({event:'scheduled_component_failed',component:i?'publication_trigger':'steam'}));});}));
}
