/** Bounded independent I/O. Wait for all in-flight calls even after failure. */
export async function mapLimited(values,limit,operation){
 if(!Number.isInteger(limit)||limit<1)throw new RangeError('Invalid concurrency');
 const results=Array(values.length);let next=0,failed=false;
 const workers=Array.from({length:Math.min(limit,values.length)},async()=>{
  while(!failed&&next<values.length){const index=next++;try{results[index]=await operation(values[index],index);}catch(error){failed=true;throw error;}}
 });
 const completed=await Promise.allSettled(workers),error=completed.find(r=>r.status==='rejected');
 if(error)throw error.reason;return results;
}
