(function (scope) {
  "use strict";
  const W = 1920, H = 1080;
  const clamp = (x) => Math.max(0, Math.min(1, x));
  const ease = (x) => { x = clamp(x); return 1 - Math.pow(1 - x, 3); };

  function diamond(ctx, x, y, size, fill, stroke) {
    ctx.beginPath(); ctx.moveTo(x, y-size); ctx.lineTo(x+size, y);
    ctx.lineTo(x, y+size); ctx.lineTo(x-size, y); ctx.closePath();
    if (fill) { ctx.fillStyle = fill; ctx.fill(); }
    if (stroke) { ctx.strokeStyle = stroke; ctx.lineWidth = 2; ctx.stroke(); }
  }

  function plate(ctx, x, y, width, height, fill, stroke) {
    const cut = Math.min(8, height/4);
    ctx.beginPath(); ctx.moveTo(x+cut,y); ctx.lineTo(x+width-cut,y);
    ctx.lineTo(x+width,y+cut); ctx.lineTo(x+width,y+height-cut);
    ctx.lineTo(x+width-cut,y+height); ctx.lineTo(x+cut,y+height);
    ctx.lineTo(x,y+height-cut); ctx.lineTo(x,y+cut); ctx.closePath();
    ctx.fillStyle = fill; ctx.fill();
    ctx.strokeStyle = stroke; ctx.lineWidth = 2; ctx.stroke();
  }

  function sprite(ctx, image, bounds, centerX, feetY, maxWidth, maxHeight, opacity=1) {
    const [sx,sy,x1,y1] = bounds, sw=x1-sx, sh=y1-sy;
    const scale = Math.min(maxWidth/sw,maxHeight/sh), width=sw*scale,height=sh*scale;
    ctx.save(); ctx.globalAlpha=opacity;
    ctx.drawImage(image,sx,sy,sw,sh,centerX-width/2,feetY-height,width,height);
    ctx.restore(); return {top:feetY-height,width,height};
  }

  function trace(ctx,x,y,width,height,opacity) {
    if (opacity<=0) return;
    ctx.save(); ctx.globalAlpha=opacity;
    ctx.strokeStyle="#a6f5f7"; ctx.lineWidth=2;
    ctx.strokeRect(x+1,y+1,width-2,height-2); ctx.restore();
  }

  function transitionLayer(ctx, image, progress, phase, family, atmosphere=true) {
    if (progress >= 1) { ctx.drawImage(image,0,0,W,H); return; }
    if (progress <= 0) return;
    const regions=family==="MT-10"?[[0,.25,.24,.75],[.76,.25,1,.75]]:[
      [[0,.16,.24,.38],[.76,.16,1,.38],[0,.38,.18,.69],[.82,.38,1,.69]],
      [[0,.39,.24,.75],[.76,.39,1,.75],[.10,.68,.90,.77]],
      [[.10,.20,.90,.40]],
      [[0,.04,.25,.73],[.75,.04,1,.73],[.25,.17,.75,.30]]
    ][phase-1];
    const cols=16,rows=9,cw=W/cols,ch=H/rows;
    for (let row=0;row<rows;row++) for (let col=0;col<cols;col++) {
      const x=col*cw,y=row*ch;
      if(!regions.some(([x0,y0,x1,y1])=>x+cw>x0*W-16 && x<x1*W+16 && y+ch>y0*H-16 && y<y1*H+16))continue;
      const axis = phase===2 ? (rows-1-row)/(rows-1) : phase===3 ? row/(rows-1) : Math.abs(col-(cols-1)/2)/((cols-1)/2);
      const jitter=((col*7+row*11+phase*3)%13)/13;
      const delay=.12+axis*.38+jitter*.07;
      const local=clamp((progress-delay)/.34),alpha=ease(local);
      if (!alpha) continue;
      const lift=(1-ease(local))*24;
      ctx.save(); ctx.globalAlpha=alpha;
      ctx.drawImage(image,x,y,cw,ch,x,y+lift,cw,ch);
      ctx.restore();
      if (atmosphere && local<1) {
        const edgeOpacity = Math.sin(local*Math.PI)*.35;
        // Edge traces stay behind the actors; quiet top and card areas remain clean.
        if(y>H*.2 && y<H*.70) trace(ctx,x,y+lift,cw,ch,edgeOpacity);
      }
    }
    if (atmosphere) {
      const amount = Math.sin(clamp((progress-.1)/.85)*Math.PI);
      for (let i=0;i<20;i++) {
        const x=((i*137+phase*83)%W),startY=H*.46+((i*71)%230);
        ctx.save(); ctx.globalAlpha=Math.max(0,amount)*.32;
        diamond(ctx,x,startY+progress*52,2+(i%3),i%2?"#d7c698":"#ecf7f5"); ctx.restore();
      }
    }
  }

  function hud(ctx, images, assets, stage, time, family, transition) {
    const airborne = family!=="normal" && stage>0 && stage%2===1;
    const bossKey=airborne?"air":"ground",feet=H*.68;
    const drift=airborne?Math.sin(time*1.25)*5-28:0;
    // The ground contact and health bars remain fixed throughout every reveal.
    ctx.save(); ctx.fillStyle="rgba(17,20,25,.24)";
    ctx.beginPath(); ctx.ellipse(W*.75,feet+8,155,17,0,0,Math.PI*2); ctx.fill(); ctx.restore();
    const figure=sprite(ctx,images[assets.boss[bossKey]],assets.boss_bounds[bossKey],W*.755,feet+drift,560,540);
    sprite(ctx,images[assets.player],assets.player_bounds,W*.235,feet,320,340);
    const intentY=Math.max(78,figure.top-62);
    plate(ctx,W*.755-65,intentY,130,42,"#272c31","#c2a25f");
    ctx.fillStyle="#f5df9b"; ctx.textAlign="center";ctx.font="bold 23px Microsoft YaHei, sans-serif";
    ctx.fillText(transition?"寻根圣事":airborne?"启圣仪式":"攻击",W*.755,intentY+28);
    const barX=W*.755-180,barY=feet+34;
    plate(ctx,barX,barY,360,29,"#242729","#97805c");
    ctx.fillStyle="#b82c3d";ctx.fillRect(barX+6,barY+5,348,19);
    ctx.fillStyle="#faeed3";ctx.font="bold 18px Microsoft YaHei, sans-serif";
    ctx.fillText("? / ?",W*.755,barY+21);
    ctx.font="bold 26px Microsoft YaHei, sans-serif";
    ctx.fillStyle="#242729";ctx.lineWidth=5;ctx.strokeStyle="#242729";
    ctx.strokeText("“圣徒”",W*.755,barY+62);ctx.fillStyle="#f8edd4";ctx.fillText("“圣徒”",W*.755,barY+62);
    const phaseCount=family==="MT-S-5"?5:2;
    for(let i=0;i<phaseCount;i++) {
      const dx=W*.755+(i-(phaseCount-1)/2)*27;
      diamond(ctx,dx,barY+87,6,i<=stage?"#d9b969":"#32393d",i===4?"#91c7c6":"#c0a674");
    }
    plate(ctx,W*.235-98,feet+30,196,25,"#242729","#97805c");
    ctx.fillStyle="#a92c3e";ctx.fillRect(W*.235-92,feet+35,184,15);
    ctx.font="bold 16px Microsoft YaHei, sans-serif";ctx.fillStyle="#fff1d6";ctx.fillText("? / ?",W*.235,feet+49);
    ctx.font="22px Microsoft YaHei, sans-serif";ctx.fillText("能天使",W*.235,feet+87);
    // Card backs convey the reserved hand area without inventing card values.
    for(let i=0;i<5;i++) {
      ctx.save();ctx.translate(W*.5+(i-2)*113,H-78+Math.abs(i-2)*13);ctx.rotate((i-2)*.035);
      plate(ctx,-57,-90,114,182,"#27343b","#b29d67");
      plate(ctx,-47,-80,94,162,"#33464e","#65757a");
      diamond(ctx,0,-6,22,"#b69d63","#ddd0a0");diamond(ctx,0,-6,12,"#2e4148");ctx.restore();
    }
  }

  function render(ctx, images, assets, options={}) {
    const family=options.family||"MT-10",stage=options.stage||0;
    const progress=clamp(options.progress===undefined?1:options.progress),time=options.time||0;
    const normal=family==="normal",event=family==="event";
    let current;
    ctx.clearRect(0,0,W,H);
    if(normal) {
      current=assets.backgrounds[stage];ctx.drawImage(images[current.path],0,0,W,H);
    } else if(event) {
      current=assets.events[stage];ctx.drawImage(images[current.path],0,0,W,H);
    } else {
      const states=assets.families[family];current=states[stage];
      if(stage>0 && progress<1) {
        ctx.drawImage(images[states[stage-1].path],0,0,W,H);
        transitionLayer(ctx,images[current.layer],progress,stage,family,options.atmosphere!==false);
      } else {ctx.drawImage(images[current.path],0,0,W,H);}
    }
    if(options.hud!==false && !event) hud(ctx,images,assets,stage,time,family,stage>0 && progress<1);
    return current;
  }

  scope.MTArtRenderer={render,transitionLayer,width:W,height:H,uiPrimitives:{plate,diamond}};
  if(typeof module!=="undefined") module.exports=scope.MTArtRenderer;
})(typeof window!=="undefined"?window:globalThis);
