import {prepareDetails} from './baselines';
export async function detailStatements(db:D1Database,run:Record<string,any>,owner:string,nonce:string) {
  const d=run.details;if(!d)return [];
  const prepared=await prepareDetails(db,run,owner),id=run.id;
  const guarded='EXISTS(SELECT 1 FROM runs WHERE write_nonce=?)';
  const dimensions=[run.ascension,run.players,run.mode];
  const decks=d.acts.flatMap((a:any)=>a.deck.map((c:any)=>({...c,act:a.act})));
  return [
    db.prepare(`INSERT OR IGNORE INTO run_details SELECT ?,?,?,? WHERE ${guarded}`).bind(id,+d.win3,d.acts.slice(0,3).reduce((n:number,a:any)=>n+a.floors,0),d.acts.length,nonce),
    db.prepare(`INSERT OR IGNORE INTO run_acts SELECT ?,json_extract(value,'$.act'),json_extract(value,'$.floors'),json_extract(value,'$.completed'),json_extract(value,'$.snapshot_known') FROM json_each(?) WHERE ${guarded}`).bind(id,JSON.stringify(d.acts),nonce),
    db.prepare(`INSERT OR IGNORE INTO act_decks SELECT ?,json_extract(value,'$.id'),json_extract(value,'$.act'),json_extract(value,'$.variant') FROM json_each(?) WHERE ${guarded}`).bind(id,JSON.stringify(decks),nonce),
    db.prepare(`INSERT OR IGNORE INTO detailed_entities SELECT ?,json_extract(value,'$.id'),json_extract(value,'$.act'),json_extract(value,'$.variant'),json_extract(value,'$.offered'),json_extract(value,'$.picked'),json_extract(value,'$.obtained'),json_extract(value,'$.floor_sum'),json_extract(value,'$.upgraded'),json_extract(value,'$.removed') FROM json_each(?) WHERE ${guarded}`).bind(id,JSON.stringify(d.items),nonce),
    db.prepare(`INSERT OR IGNORE INTO card_offers SELECT ?,CAST(key AS INTEGER),json_extract(value,'$.id'),json_extract(value,'$.variant'),json_extract(value,'$.act'),json_extract(value,'$.floor'),json_extract(value,'$.position'),json_extract(value,'$.picked'),json_extract(value,'$.expected3'),json_extract(value,'$.expected_all') FROM json_each(?) WHERE ${guarded}`).bind(id,JSON.stringify(prepared.offers),nonce),
    db.prepare(`INSERT OR IGNORE INTO fights SELECT ?,CAST(key AS INTEGER),json_extract(value,'$.act'),json_extract(value,'$.floor'),json_extract(value,'$.position'),json_extract(value,'$.encounter'),json_extract(value,'$.damage'),json_extract(value,'$.turns'),json_extract(value,'$.expected_damage'),json_extract(value,'$.expected_turns') FROM json_each(?) WHERE ${guarded}`).bind(id,JSON.stringify(prepared.combat),nonce),
    db.prepare(`INSERT INTO skill_models SELECT ?,?,?,?,json_extract(value,'$.horizon'),json_extract(value,'$.mean'),json_extract(value,'$.variance'),1 FROM json_each(?) WHERE ${guarded} ON CONFLICT(owner_hash,ascension,players,mode,horizon) DO UPDATE SET mean=excluded.mean,variance=excluded.variance,samples=samples+1`).bind(owner,...dimensions,JSON.stringify(prepared.models),nonce),
    db.prepare(`INSERT INTO survival_baselines SELECT ?,?,?,json_extract(value,'$.horizon'),json_extract(value,'$.act'),json_extract(value,'$.floor'),1,json_extract(value,'$.win') FROM json_each(?) WHERE ${guarded} ON CONFLICT(ascension,players,mode,horizon,act,floor) DO UPDATE SET samples=samples+1,wins=wins+excluded.wins`).bind(...dimensions,JSON.stringify(prepared.exposures),nonce),
    db.prepare(`INSERT INTO fight_baselines SELECT ?,?,?,json_extract(value,'$.act'),json_extract(value,'$.encounter'),COUNT(*),SUM(json_extract(value,'$.damage')),SUM(json_extract(value,'$.turns')) FROM json_each(?) WHERE ${guarded} GROUP BY json_extract(value,'$.act'),json_extract(value,'$.encounter') ON CONFLICT(ascension,players,mode,act,encounter) DO UPDATE SET samples=samples+excluded.samples,damage_sum=damage_sum+excluded.damage_sum,turns_sum=turns_sum+excluded.turns_sum`).bind(...dimensions,JSON.stringify(d.fights),nonce),
  ];
}
