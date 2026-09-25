import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import assert from 'node:assert/strict';
import {createRuntime} from './runtime-vm.mjs';
const HERE=path.dirname(fileURLToPath(import.meta.url));
const sourceHtml=fs.readFileSync(path.resolve(HERE,'../../public/g/star-out/index.html'),'utf8');
const games=[];let metadata;
function random(seed){let s=seed>>>0;return()=>{s=(Math.imul(s,1664525)+1013904223)>>>0;return s/4294967296;};}
for(let i=0;i<200;i++){
 const seed=(0x91e10da5+Math.imul(i+1,2654435761))>>>0,rand=random(seed^0x79cc4519);
 const rt=createRuntime({seed,sourceHtml});metadata??=rt.metadata;rt.click('begin');
 while(rt.state().phase==='playing'&&rt.now<180000){
  // Exempt only the explicitly displayed action tutorials. No classification or
  // correctness information is used by the main coordinate policy below.
  const training=rt.expose('S.tutorial||!!S.plate?.guided');
  if(training){
   const t=rt.expose('({practice:S.tutorial,locked:S.plate.locked,g:geometry(),play})');
   if(!t.locked){
    if(t.practice)rt.dragVertices(0,2);
    else rt.drag({x:t.g.cx,y:t.g.cy},{x:t.play.x+t.play.w-5,y:t.g.cy});
   }
   rt.advance(1.55,{frameMs:50});continue;
  }
  const from={x:rand()*390,y:rand()*844},to={x:rand()*390,y:rand()*844};
  rt.drag(from,to,{duration:.08,steps:2});rt.advance(.12,{frameMs:50});
 }
 const state=rt.state();assert.notEqual(state.phase,'playing');assert.equal(rt.errors.filter(e=>e.type==='error').length,0);
 games.push({i,seed,phase:state.phase,solved:state.solved,firstAttempts:state.firstAttempts,events:rt.events.length,seconds:rt.now/1000});
}
const trials=games.flatMap(g=>g.firstAttempts),correct=trials.filter(t=>t.firstCorrect).length,wins=games.filter(g=>g.phase==='clear').length;
const result={metadata,policy:'uniform full-canvas coordinate strokes',separateStressTest:true,games:200,trials:trials.length,firstCorrect:correct,firstCorrectRate:correct/trials.length,completions:wins,completionRate:wins/200,
 method:'Both endpoints are sampled independently and uniformly from the 390×844 canvas on every stroke. The policy does not inspect current shape, marked vertex, lines, answer, kind, or success feedback. Coordinate events are delivered to actual registered canvas handlers in the unchanged game VM; DOM hit targeting, trusted browser input, pixels and performance remain unverified.',
 chanceRate:null,chanceNote:'No analytic baseline is asserted for this mixed continuous-coordinate drawing/discard policy. Finite board lifetime, moving hit regions, multi-stroke completion, locked-window behavior and penalties interact. This stress result is kept separate from B3 with its explicit discrete random-endpoint baseline.',gameDetails:games};
fs.writeFileSync(path.join(HERE,'coordinate-stress-vm.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(Object.fromEntries(Object.entries(result).filter(([k])=>k!=='gameDetails')),null,2));
