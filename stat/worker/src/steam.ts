import {metered} from './usage.mjs';
import {officialTags,primaryTag,PROTECTED_MODS} from './tags.mjs';
type Row={id:string;title:string;workshop_id:string|null;uses:number;failures:number};
type SteamItem={publishedfileid:string;result:number;consumer_app_id:number;title:string;tags?:{tag:string}[]};
const normalize=(text:string)=>text.normalize('NFKC').toLowerCase().replace(/mod$/,'').replace(/[^\p{L}\p{N}]/gu,'');
async function textBody(response:Response,max=2*1024*1024) {
  if(!response.ok)throw new Error(String(response.status));
  const reader=response.body!.getReader(),decoder=new TextDecoder();let length=0,result='';
  for(;;){const {done,value}=await reader.read();if(done)break;length+=value.byteLength;if(length>max){await reader.cancel();throw new Error('Steam response limit');}result+=decoder.decode(value,{stream:true});}
  return result+decoder.decode();
}
export async function enrichMods(database:D1Database,now=Date.now(),fetcher:typeof fetch=fetch) {
  const {db,usage}=metered(database);try{
  const cooldown=await db.prepare("SELECT value FROM enrichment_state WHERE key='cooldown'").first<{value:number}>();
  if(cooldown && cooldown.value>now)return;
  const lease=await db.prepare("INSERT INTO enrichment_state(key,value) VALUES('lease',?) ON CONFLICT(key) DO UPDATE SET value=excluded.value WHERE enrichment_state.value<=?").bind(now+900000,now).run();
  if(!lease.meta.changes)return;
  const protectedJson=JSON.stringify(PROTECTED_MODS);
  const [known,unknown]=await db.batch<Row>([
    db.prepare('SELECT id,title,workshop_id,uses,failures FROM mod_catalog WHERE next_check<=? AND workshop_id IS NOT NULL AND id NOT IN(SELECT value FROM json_each(?)) ORDER BY checked_at=0 DESC,uses DESC,next_check LIMIT 16').bind(now,protectedJson),
    db.prepare('SELECT id,title,workshop_id,uses,failures FROM mod_catalog WHERE next_check<=? AND workshop_id IS NULL AND id NOT IN(SELECT value FROM json_each(?)) ORDER BY checked_at=0 DESC,uses DESC,next_check LIMIT 6').bind(now,protectedJson),
  ]);
  const rows=[...known.results,...unknown.results];if(!rows.length)return;
  const candidates=new Map<string,string[]>(),ids=new Set(known.results.map(m=>m.workshop_id!));
  try {
    // At most six public search requests and one bulk detail request per fifteen-minute tick.
    for(const mod of unknown.results) {
      const query=mod.title || mod.id.replace(/([a-z])([A-Z])/g,'$1 $2').replace(/[_-]/g,' ');
      const url=new URL('https://steamcommunity.com/workshop/browse/');
      url.search=new URLSearchParams({appid:'2868840',searchtext:query,browsesort:'textsearch',section:'readytouseitems',l:'english'}).toString();
      const html=await textBody(await fetcher(url,{signal:AbortSignal.timeout(15000)}));
      const decoded=html.replace(/\\"/g,'"').replace(/\\u0026/g,'&');
      const found=[...new Set([...decoded.matchAll(/(?:filedetails\/\?id=|"publishedfileid"\s*:\s*")([0-9]{6,20})/g)].map(m=>m[1]))].slice(0,8);
      candidates.set(mod.id,found);for(const id of found)ids.add(id);
    }
    let items:SteamItem[]=[];
    if(ids.size) {
      const body=new URLSearchParams({itemcount:String(ids.size)});[...ids].forEach((id,i)=>body.set(`publishedfileids[${i}]`,id));
      const response=await fetcher('https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/',{method:'POST',body,signal:AbortSignal.timeout(20000)});
      items=JSON.parse(await textBody(response)).response?.publishedfiledetails || [];
    }
    const statements=rows.map(mod=>{
      let item=items.find(i=>i.publishedfileid===mod.workshop_id);
      if(!mod.workshop_id) {
        const wanted=normalize(mod.title||mod.id);
        const matches=items.filter(i=>i.result===1&&i.consumer_app_id===2868840&&candidates.get(mod.id)?.includes(i.publishedfileid)&&normalize(i.title)===wanted);
        if(matches.length===1)item=matches[0];
      }
      const valid=item?.result===1&&item.consumer_app_id===2868840;
      const tags=valid?officialTags(item!.tags||[]):[];
      return db.prepare('UPDATE mod_catalog SET title=?,workshop_id=COALESCE(?,workshop_id),official_tags=?,primary_tag=?,status=?,checked_at=?,next_check=?,failures=0 WHERE id=?')
        .bind(valid?item!.title:mod.title,valid?item!.publishedfileid:null,JSON.stringify(tags),primaryTag(tags),valid?'ready':'not_found',now,now+86400000,mod.id);
    });
    await db.batch(statements);
  } catch(error) {
    const blocked=['403','429'].includes(error instanceof Error?error.message:'');
    await db.batch([
      db.prepare("INSERT INTO enrichment_state(key,value) VALUES('cooldown',?) ON CONFLICT(key) DO UPDATE SET value=excluded.value").bind(now+(blocked?21600000:1800000)),
      ...rows.map(m=>db.prepare('UPDATE mod_catalog SET failures=failures+1,next_check=? WHERE id=?').bind(now+(blocked?21600000:1800000),m.id)),
    ]);
    console.warn(JSON.stringify({event:'steam_metadata_deferred',blocked}));
  }
  }finally{console.info(JSON.stringify({event:'stat_steam_usage',...usage}));}
}
