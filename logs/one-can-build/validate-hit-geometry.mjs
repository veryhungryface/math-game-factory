import http from 'http'; import fs from 'fs'; import path from 'path';
import puppeteer from 'puppeteer';
const root='/Users/sitpo/math-game-factory/public';
const types={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png'};
const srv=http.createServer((q,r)=>{let p=path.join(root,decodeURIComponent(q.url.split('?')[0]));if(p.endsWith('/'))p+='index.html';
 fs.readFile(p,(e,d)=>{if(e){r.writeHead(404);r.end('x');return}r.writeHead(200,{'content-type':types[path.extname(p)]||'application/octet-stream'});r.end(d)})});
await new Promise(r=>srv.listen(0,'127.0.0.1',r));const port=srv.address().port;
const b=await puppeteer.launch({headless:true,executablePath:process.env.PUPPETEER_EXECUTABLE_PATH});
const pg=await b.newPage();
const [W,H]=[Number(process.argv[2]||390),Number(process.argv[3]||844)];
await pg.setViewport({width:W,height:H,deviceScaleFactor:2,isMobile:true,hasTouch:true});
await pg.goto(`http://127.0.0.1:${port}/g/one-can/`,{waitUntil:'networkidle0'});
await pg.waitForFunction('window.__GAME_TEST__&&window.__GAME_TEST__.ready');
await pg.evaluate(()=>window.__GAME_TEST__.start());
await new Promise(r=>setTimeout(r,500));
const res=await pg.evaluate(()=>{
  const tg=window.__GAME_TEST__.getInputTargets();
  const btns=[...document.querySelectorAll('.cupTarget')];
  const rects=btns.map(x=>{const r=x.getBoundingClientRect();return{x:r.x,y:r.y,w:r.width,h:r.height}});
  let wrong=0,tested=0,worst=0;const detail=[];
  tg.forEach((t,i)=>{
    const w=rects[i].w;
    // sample across the cup's own body width (what a child aims at)
    for(let f=-0.42;f<=0.42;f+=0.06){
      const px=t.x+f*w/1.15, py=t.y+10; tested++;
      const el=document.elementFromPoint(px,py);
      const j=btns.indexOf(el);
      if(j!==i){wrong++;detail.push({cup:i,offsetPx:Math.round(f*w/1.15),got:j})}
    }
  });
  let overlap=0;for(let i=1;i<rects.length;i++){const a=[...rects].sort((p,q)=>p.x-q.x)[i-1],c=[...rects].sort((p,q)=>p.x-q.x)[i];overlap=Math.max(overlap,Math.min(a.x+a.w,c.x+c.w)-Math.max(a.x,c.x))}
  // visual cup footprint overlap: body -0.45w .. handle +0.62w
  let vis=0;const cs=[...tg].sort((p,q)=>p.x-q.x);
  for(let i=1;i<cs.length;i++){const cw=rects[0].w/1.15;vis=Math.max(vis,(cs[i-1].x+0.62*cw)-(cs[i].x-0.45*cw))}
  return {overlapPx:+overlap.toFixed(1),visualOverlapPx:+vis.toFixed(1),wrong,tested,detail:detail.slice(0,8),rects};
});
console.log(process.argv[2]+'x'+process.argv[3], JSON.stringify(res,null,1));
await b.close();srv.close();
