type Model={mean:number;variance:number;samples:number};
type Cell={horizon:string;act:number;floor:number;samples:number;wins:number};
const sigmoid=(v:number)=>1/(1+Math.exp(-v));
const clip=(p:number)=>Math.min(.99,Math.max(.01,p));
const logit=(p:number)=>Math.log(clip(p)/(1-clip(p)));
export async function prepareDetails(db:D1Database,run:Record<string,unknown>,owner:string) {
  const r=run as {ascension:number;players:number;mode:string;victory:boolean;details:{win3:boolean;acts:{act:number;floors:number}[];offers:{id:string;act:number;floor:number;position:number;variant:number;picked:boolean}[];fights:{act:number;encounter:string;damage:number;turns:number}[]}};
  const dimensions=[r.ascension,r.players,r.mode];
  const [models,cells,fights,previous]=await db.batch<Record<string,string|number>>([
    db.prepare('SELECT horizon,mean,variance,samples FROM skill_models WHERE owner_hash=? AND ascension=? AND players=? AND mode=?').bind(owner,...dimensions),
    db.prepare('SELECT horizon,act,floor,samples,wins FROM survival_baselines WHERE ascension=? AND players=? AND mode=?').bind(...dimensions),
    db.prepare('SELECT act,encounter,samples,damage_sum,turns_sum FROM fight_baselines WHERE ascension=? AND players=? AND mode=?').bind(...dimensions),
    db.prepare('SELECT COUNT(*) samples,COALESCE(SUM(victory),0) wins FROM runs WHERE owner_hash=? AND ascension=? AND players=? AND mode=? AND abandoned=0').bind(owner,...dimensions),
  ]);
  const prior=previous.results[0] as {samples:number;wins:number};
  const outputModels=[];
  const expected=(horizon:string,act:number,floor:number)=>{
    const m=models.results.find(m=>m.horizon===horizon) as Model|undefined;
    const belief=m?sigmoid(m.mean):(prior.wins+1)/(prior.samples+2);
    const center=cells.results.find(c=>c.horizon===horizon&&c.act===0&&c.floor===0) as Cell|undefined;
    const cell=cells.results.find(c=>c.horizon===horizon&&c.act===act&&c.floor===floor) as Cell|undefined;
    const rate=center?.samples ?center.wins/center.samples:.5;
    const floorRate=cell&&cell.samples>=5?cell.wins/cell.samples:rate;
    return sigmoid(logit(belief)+logit(floorRate)-logit(rate));
  };
  for(const horizon of ['standard','all']) {
    const m=models.results.find(m=>m.horizon===horizon) as Model|undefined;
    let mean=m?.mean??logit((prior.wins+1)/(prior.samples+2)),variance=(m?.variance??4)+.02;
    const probability=sigmoid(mean),weight=probability*(1-probability),gain=variance*weight/(1+variance*weight);
    mean+=gain*((horizon==='all'?+r.victory:+r.details.win3)-probability);variance*=1-gain*weight;
    outputModels.push({horizon,mean,variance});
  }
  const offers=r.details.offers.map(o=>({...o,expected3:expected('standard',o.act,o.floor),expected_all:expected('all',o.act,o.floor)}));
  const combat=r.details.fights.map(f=>{
    const b=fights.results.find(c=>c.act===f.act&&c.encounter===f.encounter) as {samples:number;damage_sum:number;turns_sum:number}|undefined;
    return {...f,expected_damage:b&&b.samples>=5?b.damage_sum/b.samples:null,expected_turns:b&&b.samples>=5?b.turns_sum/b.samples:null};
  });
  const exposures=[];
  for(const horizon of ['standard','all']) {
    const win=horizon==='all'?+r.victory:+r.details.win3;
    exposures.push({horizon,act:0,floor:0,win});
    for(const act of r.details.acts)if(horizon==='all'||act.act<=3)for(let floor=1;floor<=act.floors;floor++)exposures.push({horizon,act:act.act,floor,win});
  }
  return {offers,combat,models:outputModels,exposures};
}
