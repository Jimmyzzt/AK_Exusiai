import {setTimeout as pause} from 'node:timers/promises';
/** Only retry reads: the same cursor is safe to replay after a transient failure. */
export async function publicationFetch(fetcher,url,options={}, {wait=pause,log=console.log}={}){
 const attempts=(options.method||'GET')==='GET'?4:1;
 for(let attempt=0;attempt<attempts;attempt++){
  let response,error;
  try{response=await fetcher(url,{...options,signal:AbortSignal.timeout(60000)});}catch(e){error=e;}
  const retry=error||[429,500,502,503,504].includes(response.status);
  if(!retry||attempt===attempts-1){if(error)throw error;return response;}
  await response?.body?.cancel();
  log(JSON.stringify({event:'publication_request_retry',operation:new URL(url).pathname.split('/').at(-1),http_status:response?.status??null,attempt:attempt+1}));
  await wait([500,1500,3500][attempt]);
 }
}
