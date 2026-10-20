(async function () {
  "use strict";
  const $=(id)=>document.getElementById(id);
  const assets=MT_ASSETS,canvas=$("stage"),ctx=canvas.getContext("2d"),images={};
  let family="MT-10",stage=0,progress=1,playing=false,serial=0,start=0;
  const paths=new Set([assets.player,...Object.values(assets.boss),...assets.backgrounds.map(x=>x.path),...assets.events.map(x=>x.path)]);
  for(const states of Object.values(assets.families)) for(const s of states) {paths.add(s.path);if(s.layer)paths.add(s.layer);}
  const failures=[];
  await Promise.all([...paths].map(path=>new Promise(resolve=>{
    const image=new Image();image.onload=()=>{images[path]=image;resolve();};
    image.onerror=()=>{failures.push(path);resolve();};image.src=path;
  })));
  if(failures.length) {$("load-error").textContent="载入失败："+failures.join(", ");canvas.dataset.state="error";return;}
  function states(){return family==="normal"?assets.backgrounds:family==="event"?assets.events:assets.families[family]||[];}
  function limit(){return family==="MT-S-5"&&!$("extra").checked?3:states().length-1;}
  function stopped(){playing=false;$("play").textContent="播放转场";}
  function paint(time=0){
    if(family==="enemies")return;
    const current=MTArtRenderer.render(ctx,images,assets,{family,stage,progress,time,hud:$("hud").checked});
    canvas.dataset.state=family+":"+stage;canvas.dataset.progress=progress.toFixed(3);canvas.dataset.renderSerial=String(++serial);
    $("heading").textContent=(family==="normal"||family==="event"?"":family+" · ")+current.label;
    $("progress").value=String(progress);
    $("image-link").href=current.path;
    $("original").href=family==="normal"?assets.backgrounds[stage].source:family==="event"?"01_originals/event_cg/":family==="MT-10"?"01_originals/maps/MT-10/" : "01_originals/maps/MT-S-5/";
    const phaseLabel=family==="normal"||family==="event"?"":stage===0?"梦中":current.label+(stage===4?" · 条件阶段":"");
    $("status").textContent=[phaseLabel,playing?"转场中":"",progress<1?Math.round(progress*100)+"%":"1920 × 1080"].filter(Boolean).join(" · ");
    $("prev").disabled=stage===0;$("next").disabled=stage===limit();
  }
  function picks(){
    $("picks").replaceChildren();
    if(family==="enemies"){const span=document.createElement("span");span.textContent="MT-1 至 MT-10 / 附MT-S-5";$("picks").append(span);return;}
    states().forEach((s,i)=>{
      const button=document.createElement("button");button.className="pick"+(stage===i?" active":"");button.disabled=i>limit();
      const thumb=document.createElement("img");thumb.src=s.path;thumb.alt="";
      const label=document.createElement("span");label.textContent=s.label;
      if(s.extra){const small=document.createElement("small");small.textContent="条件触发";label.append(small);}
      button.append(thumb,label);button.addEventListener("click",()=>{stopped();stage=i;progress=1;picks();paint();});$("picks").append(button);
    });
  }
  function mode(nextFamily){
    stopped();family=nextFamily;stage=0;progress=1;
    document.querySelectorAll("nav button").forEach(b=>b.setAttribute("aria-selected",String(b.dataset.family===family)));
    $("art").classList.toggle("hidden",family==="enemies");$("enemies").classList.toggle("hidden",family!=="enemies");
    const phaseFamily=family==="MT-10"||family==="MT-S-5";
    $("hud").checked=phaseFamily;
    ["reset","prev","play","next","progress"].forEach(id=>$(id).classList.toggle("hidden",!phaseFamily));
    $("extra-label").classList.toggle("hidden",family!=="MT-S-5");
    picks();paint();
  }
  function frame(now){
    if(!playing)return;
    progress=Math.min(1,(now-start)/3200);paint(now/1000);
    if(progress>=1){stopped();paint(now/1000);picks();return;}
    requestAnimationFrame(frame);
  }
  $("play").addEventListener("click",()=>{
    if(playing){stopped();paint();return;}
    if(stage===0||progress>=1){stage=stage>=limit()?1:stage+1;progress=0;}
    playing=true;$("play").textContent="暂停转场";start=performance.now()-progress*3200;picks();requestAnimationFrame(frame);
  });
  $("reset").addEventListener("click",()=>{stopped();stage=0;progress=1;picks();paint();});
  $("prev").addEventListener("click",()=>{stopped();stage=Math.max(0,stage-1);progress=1;picks();paint();});
  $("next").addEventListener("click",()=>{stopped();stage=Math.min(limit(),stage+1);progress=1;picks();paint();});
  $("progress").addEventListener("input",()=>{stopped();if(stage===0)stage=1;progress=Number($("progress").value);picks();paint();});
  $("extra").addEventListener("change",()=>{stopped();stage=Math.min(stage,limit());progress=1;picks();paint();});
  $("hud").addEventListener("change",()=>paint());
  $("export").addEventListener("click",()=>{
    try {const a=document.createElement("a");a.download=family+"_"+stage+".png";a.href=canvas.toDataURL("image/png");a.click();}
    catch {$("status").textContent="当前浏览器限制本地图片导出；可直接使用独立PNG或通过HTTP预览。";}
  });
  document.querySelectorAll("nav button").forEach(b=>b.addEventListener("click",()=>mode(b.dataset.family)));
  function enemyRows(query=""){
    const list=$("enemy-rows");list.replaceChildren();query=query.toLowerCase();
    assets.enemies.filter(e=>(e.id+e.name+e.stages.join(" ")).toLowerCase().includes(query)).forEach(e=>{
      const row=document.createElement("tr"),name=document.createElement("td"),stages=document.createElement("td"),actions=document.createElement("td"),files=document.createElement("td");
      name.className="name";const strong=document.createElement("strong");strong.textContent=e.name;name.append(strong);
      const small=document.createElement("div");small.textContent=e.id;small.style.fontSize="11px";name.append(small);
      stages.textContent=e.stages.join("、");const details=document.createElement("details"),summary=document.createElement("summary");summary.textContent=e.action_count+"个动作";
      const anims=document.createElement("div");anims.className="anim-list";e.actions.forEach(a=>{const s=document.createElement("span");s.textContent=a.animation+" · "+a.duration_seconds+"s";anims.append(s);});details.append(summary,anims);actions.append(details);
      const link=document.createElement("a");link.href=e.folder+"/动作清单.txt";link.textContent="动作清单";files.append(link,document.createElement("br"));
      const skeleton=document.createElement("a");skeleton.href=e.folder+"/"+e.id+".skel";skeleton.textContent="原始SKEL";files.append(skeleton);
      row.append(name,stages,actions,files);list.append(row);
    });
  }
  $("filter").addEventListener("input",()=>enemyRows($("filter").value));enemyRows();mode("MT-10");
})();
