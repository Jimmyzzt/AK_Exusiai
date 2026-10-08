export const METRICS={
 offered:['提供次数','在奖励中出现的次数，同局重复出现分别计数。'],
 pick:['选取率','实际选择次数 / 奖励提供次数。'],
 win:['持有胜率','范围终点持有此牌或遗物且通关的玩家局 / 可确认结果的持有玩家局。'],
 owned:['持有率','范围终点持有的玩家局 / 有该终点持有摘要的玩家局。'],
 picked_win:['选后胜率','在选定幕数中选择过且通关的玩家局 / 可确认结果的选择玩家局；同局只计一次。'],
 war:['胜利贡献 WAR','所有提供的实际胜利减去玩家与楼层校正的预期胜利之和。仅新采集版本可用；相关性不是因果。'],
 pwar:['选取贡献 pWAR','实际选取的实际胜利减去校正预期胜利之和，仅新采集版本可用。'],
 pwar_rate:['选取贡献 pWAR%','100 × pWAR / 新版本选取次数，至少10次。代表每次选取相对预期的胜率差。'],
 delta_pick:['后期偏好 ΔPick','选取率对累计层数的线性趋势，单位为每层百分点。至少20次提供；未采用参考站的 Elo 校正。'],
 delta_war:['后期贡献 ΔWAR','胜利减去校正预期胜利的逐层线性趋势，单位为每层百分点。至少20次提供。'],
 delta_hp:['损血变化 ΔHP','选取后战斗损血与相同遭遇历史均值的平均差；负数表示损血更少。至少10个选取玩家局且基线至少5场。'],
 delta_turns:['回合变化 ΔTurns','选取后战斗回合数与相同遭遇历史均值的平均差；负数表示更快。至少10个选取玩家局且基线至少5场。'],
 obtained:['获得次数','从历史获得与永久栏记录计数，遗物按 ID / 层数去重，中转副本不计入。'],
 floor:['获得层数','可确认的获得记录的平均累计层数，初始牌没有获得事件时显示 —。'],
 upgraded:['升级次数','所选幕数中的历史升级次数，区分升级版时归入升级版。'],
 removed:['删除次数','所选幕数中的历史删除次数。'],
};
const ratio=(n,d)=>d>0?100*(n||0)/d:null;
export function metric(key,m,o={}) {
 let value=null,samples=0,unit='',signed=false;
 switch(key){
 case 'offered':value=m.offered||0;samples=m.offered||0;break;
 case 'pick':value=ratio(m.picked,m.offered);samples=m.offered||0;unit='%';break;
 case 'win':value=ratio(m.owned_wins,m.owned_win_samples);samples=m.owned_win_samples||0;unit='%';break;
 case 'owned':value=ratio(m.owned,o.hold_samples);samples=o.hold_samples||0;unit='%';break;
 case 'picked_win':value=ratio(m.picked_wins,m.picked_win_samples);samples=m.picked_win_samples||0;unit='%';break;
 case 'war':case 'pwar':samples=m[key+'_samples']||0;value=samples?m[key]:null;signed=true;break;
 case 'pwar_rate':samples=m.pwar_samples||0;value=samples>=10?ratio(m.pwar,samples):null;unit='%';signed=true;break;
 case 'delta_pick':case 'delta_war':samples=m.n||0;value=m[key]??null;unit='pp';signed=true;break;
 case 'delta_hp':case 'delta_turns':samples=m.delta_picks||0;value=m[key]??null;signed=true;break;
 case 'floor':samples=m.obtained||0;value=samples?m.floor_sum/samples:null;break;
 default:value=m[key]||0;samples=value;
 }
 const text=value==null?'—':`${signed&&value>0?'+':''}${Number(value).toLocaleString('zh-CN',{maximumFractionDigits:key==='offered'||['obtained','upgraded','removed'].includes(key)?0:2})}${unit}`;
 return {value,text,samples,low:samples>0&&samples<30,signed};
}
