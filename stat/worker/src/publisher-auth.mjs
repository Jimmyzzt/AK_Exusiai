const ISSUER='https://token.actions.githubusercontent.com';
export const AUDIENCE='https://exusiai.zzt.si/stat-publisher';
export function checkClaims(c,now=Math.floor(Date.now()/1000)){
 return c.iss===ISSUER&&c.aud===AUDIENCE&&c.repository==='Jimmyzzt/AK_Exusiai'&&c.repository_id==='1341664712'&&c.repository_owner_id==='57975018'&&c.ref==='refs/heads/main'&&c.job_workflow_ref==='Jimmyzzt/AK_Exusiai/.github/workflows/stat-release.yml@refs/heads/main'&&['push','schedule','workflow_dispatch'].includes(c.event_name)&&Number.isSafeInteger(c.exp)&&Number.isSafeInteger(c.iat)&&Number.isSafeInteger(c.nbf)&&c.exp>now&&c.nbf<=now+30&&c.iat<=now+30&&c.exp-c.iat<=600&&c.exp-c.iat>0;
}
const bytes=s=>Uint8Array.from(atob(s.replaceAll('-','+').replaceAll('_','/')+'='.repeat((4-s.length%4)%4)),c=>c.charCodeAt(0));
export function verifier(fetcher=fetch){
 let cache=null;
 return async function verify(request,now=Math.floor(Date.now()/1000)){
  try{
   const value=request.headers.get('Authorization')||'';if(!value.startsWith('Bearer ')||value.length>12000)return null;
   const token=value.slice(7),parts=token.split('.');if(parts.length!==3)return null;
   const header=JSON.parse(new TextDecoder().decode(bytes(parts[0]))),claims=JSON.parse(new TextDecoder().decode(bytes(parts[1])));
   if(header.alg!=='RS256'||header.typ!=='JWT'||typeof header.kid!=='string'||!checkClaims(claims,now))return null;
   if(!cache||cache.until<now){
    const response=await fetcher(ISSUER+'/.well-known/jwks',{signal:AbortSignal.timeout(10000),cf:{cacheTtl:3600}});
    if(!response.ok)return null;const body=await response.json();
    cache={until:now+3600,keys:body.keys};
   }
   const jwk=cache.keys.find(k=>k.kid===header.kid&&k.kty==='RSA'&&k.alg==='RS256'&&k.use==='sig');
   if(!jwk)return null;
   const key=await crypto.subtle.importKey('jwk',jwk,{name:'RSASSA-PKCS1-v1_5',hash:'SHA-256'},false,['verify']);
   return await crypto.subtle.verify('RSASSA-PKCS1-v1_5',key,bytes(parts[2]),new TextEncoder().encode(parts[0]+'.'+parts[1]))?claims:null;
  }catch{return null;}
 };
}
export const verifyPublisher=verifier();
