// Reuses the exact browser renderer for static PNGs and portable transition clips.
const fs=require('node:fs'),path=require('node:path'),{spawn}=require('node:child_process');
const {once}=require('node:events');
const {createCanvas,loadImage,GlobalFonts}=require('@napi-rs/canvas');
const root=path.resolve(__dirname,'..');
const assets=require('../05_preview/assets.js');
const renderer=require('../05_preview/scene_renderer.js');
const exportsDir=path.join(root,'05_preview/exports');
fs.mkdirSync(exportsDir,{recursive:true});
if(process.env.WINDIR)GlobalFonts.registerFromPath(path.join(process.env.WINDIR,'Fonts','msyh.ttc'),'Microsoft YaHei');

async function main(){
  const paths=new Set([assets.player,...Object.values(assets.boss),...assets.backgrounds.map(x=>x.path),...assets.events.map(x=>x.path)]);
  for(const states of Object.values(assets.families))for(const s of states){paths.add(s.path);if(s.layer)paths.add(s.layer);}
  const images={};await Promise.all([...paths].map(async p=>{images[p]=await loadImage(path.join(root,p));}));
  const canvas=createCanvas(1920,1080),ctx=canvas.getContext('2d');
  const png=(name,options)=>{renderer.render(ctx,images,assets,options);fs.writeFileSync(name,canvas.toBuffer('image/png'));};
  png(path.join(root,'02_created/boss/battle_ui_concept.png'),{family:'MT-10',stage:1,hud:true});
  png(path.join(exportsDir,'MT-10_dream_UI.png'),{family:'MT-10',stage:0,hud:true});
  png(path.join(exportsDir,'MT-S-5_final_UI.png'),{family:'MT-S-5',stage:4,hud:true});
  const uiDir=path.join(root,'02_created/boss/ui');fs.mkdirSync(uiDir,{recursive:true});
  for(const [name,width,height,draw] of [
    ['health_frame.png',368,37,c=>renderer.uiPrimitives.plate(c,4,4,360,29,'#242729','#97805c')],
    ['intent_plate.png',138,50,c=>renderer.uiPrimitives.plate(c,4,4,130,42,'#272c31','#c2a25f')],
    ['phase_active.png',20,20,c=>renderer.uiPrimitives.diamond(c,10,10,6,'#d9b969','#c0a674')],
    ['phase_inactive.png',20,20,c=>renderer.uiPrimitives.diamond(c,10,10,6,'#32393d','#c0a674')],
    ['phase_conditional.png',20,20,c=>renderer.uiPrimitives.diamond(c,10,10,6,'#32393d','#91c7c6')]
  ]){const item=createCanvas(width,height);draw(item.getContext('2d'));fs.writeFileSync(path.join(uiDir,name),item.toBuffer('image/png'));}
  const proof=[];
  for(const family of ['MT-10','MT-S-5']){
    const states=assets.families[family];
    for(let stage=1;stage<states.length;stage++){
      for(const progress of [0,.5,1]){
        renderer.render(ctx,images,assets,{family,stage,progress,hud:false,atmosphere:false});
        const center=ctx.getImageData(960,500,1,1).data;
        proof.push({family,stage,progress,center_pixel:Array.from(center)});
      }
    }
  }
  fs.writeFileSync(path.join(root,'00_index/render_checks.json'),JSON.stringify(proof,null,2));
  console.log('Static UI PNGs rendered; all assets loaded.');
  if(!process.env.MT_FFMPEG)return;
  const videoCanvas=createCanvas(1280,720),videoCtx=videoCanvas.getContext('2d');
  const fps=24,frames=120;
  for(const family of ['MT-10','MT-S-5']){
    for(let stage=1;stage<assets.families[family].length;stage++){
      const name=family+'_transition_'+stage+(stage===4?'_conditional':'')+'.mp4';
      const output=path.join(exportsDir,name);
      const encoder=spawn(process.env.MT_FFMPEG,['-y','-loglevel','error','-f','rawvideo','-pixel_format','rgba','-video_size','1280x720','-framerate',String(fps),'-i','pipe:0','-an','-c:v','libx264','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',output],{stdio:['pipe','ignore','pipe'],windowsHide:true});
      let errors='';encoder.stderr.on('data',chunk=>errors+=chunk);
      const done=once(encoder,'close');
      for(let i=0;i<frames;i++){
        const seconds=i/fps,progress=Math.max(0,Math.min(1,(seconds-.7)/3.2));
        videoCtx.setTransform(2/3,0,0,2/3,0,0);
        renderer.render(videoCtx,images,assets,{family,stage,progress,time:seconds,hud:false});
        const raw=Buffer.from(videoCtx.getImageData(0,0,1280,720).data.buffer);
        if(!encoder.stdin.write(raw))await once(encoder.stdin,'drain');
      }
      encoder.stdin.end();const [code]=await done;if(code!==0)throw new Error(errors||'Video encoding failed');
      console.log('Encoded '+name);
    }
  }
}
main().catch(error=>{console.error(error);process.exitCode=1;});
