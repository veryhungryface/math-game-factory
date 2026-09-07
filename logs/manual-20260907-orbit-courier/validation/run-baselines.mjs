import fs from 'node:fs';
import {POOL} from '../../../public/g/orbit-courier/math.mjs';
import {openGame,start,state,clickVisible,waitState,solveCurrent,loadCargo,submit} from './browser-bot.mjs';
const report={method:'Blind policies choose a cargo target before reading the current problem. Trusted pointer input only. Tutorial excluded. A mathematical solver repairs a wrong submission only after recording its first-attempt result, to reach the next independent problem. No-movement / no-submission is reported separately, never as first-try accuracy.',policies:[],started_at:new Date().toISOString()};
const out=new URL('baseline-results.json',import.meta.url),write=()=>fs.writeFileSync(out,JSON.stringify(report,null,2));
let seed=917341;const random=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
const env=await openGame();
try{
  const trialsPerPolicy=Number(process.env.ORBIT_BASELINE_TRIALS||4);
  for(const policy of (process.env.ORBIT_POLICIES||'uniform-cargo,fixed-336,cycle-111,repeat-hundreds').split(',')){
    const r={policy,trials:[],trace:[]};report.policies.push(r);
    while(r.trials.length<trialsPerPolicy){
      await env.page.reload({waitUntil:'networkidle0'});await start(env.page);await solveCurrent(env.page,r.trace);await clickVisible(env.page,'#next');await waitState(env.page,s=>s.phase==='playing'&&s.level===2,'first random mission');
      const used=new Set(['112:3']);
      for(let round=0;round<2&&r.trials.length<trialsPerPolicy;round++){
        // Guess is chosen without observing operands or answer.
        const guess=policy==='uniform-cargo'?Math.floor(random()*1000):policy==='fixed-336'?336:policy==='cycle-111'?111:900;
        const before=await state(env.page);
        const eligible=POOL.filter(p=>p.a<170&&p.b<=4&&!used.has(`${p.a}:${p.b}`));
        const chance=policy==='uniform-cargo'?1/1000:eligible.filter(p=>p.answerNumeric===guess).length/eligible.length;
        await loadCargo(env.page,guess,r.trace);const after=await submit(env.page,r.trace);
        const correct=after.solved>before.solved;
        r.trials.push({level:before.level,guess,operands:before.operands,correct,conditional_chance:chance,firstTryAttempts_delta:after.firstTryAttempts-before.firstTryAttempts,firstTryCorrect_delta:after.firstTryCorrect-before.firstTryCorrect,firstTryFlags:after.firstTryFlags});
        if(after.firstTryAttempts-before.firstTryAttempts!==1||after.firstTryCorrect-before.firstTryCorrect!==(correct?1:0))throw new Error('First-attempt instrumentation differs from observed submission');
        used.add(`${before.operands.a}:${before.operands.b}`);write();
        if(!correct)await solveCurrent(env.page,r.trace);
        await clickVisible(env.page,'#next');await waitState(env.page,s=>s.phase==='playing','advance after measurement');
      }
    }
    r.first_try_correct=r.trials.filter(t=>t.correct).length;r.first_try_attempts=r.trials.length;r.accuracy=r.first_try_correct/r.first_try_attempts;r.mean_chance=r.trials.reduce((n,t)=>n+t.conditional_chance,0)/r.trials.length;write();
  }
  await env.page.reload({waitUntil:'networkidle0'});await start(env.page);const before=await state(env.page);await new Promise(r=>setTimeout(r,15000));const after=await state(env.page);report.no_input={seconds:15,submitted:after.attempts-before.attempts,solved:after.solved-before.solved,first_try_accuracy:null};
  report.failure_path={trace:[],initial:await state(env.page)};
  for(let i=0;i<3;i++)await submit(env.page,report.failure_path.trace);
  report.failure_path.gameover=await state(env.page);
  if(report.failure_path.gameover.phase!=='gameover'||report.failure_path.gameover.lives!==0||report.failure_path.gameover.attempts!==3)throw new Error('Three real wrong submissions did not enter game over');
  await env.page.screenshot({path:new URL('three-wrong-gameover.png',import.meta.url).pathname});
  await clickVisible(env.page,'#next');await waitState(env.page,s=>s.phase==='playing'&&s.level===1&&s.lives===3,'restart after game over');
  report.failure_path.restarted=await state(env.page);
  await env.page.screenshot({path:new URL('restart-after-gameover.png',import.meta.url).pathname});
  report.keyboard={before:await state(env.page)};
  await env.page.keyboard.down('d');await new Promise(r=>setTimeout(r,500));await env.page.keyboard.up('d');
  report.keyboard.after_d=await state(env.page);
  await env.page.keyboard.down('w');await new Promise(r=>setTimeout(r,500));await env.page.keyboard.up('w');
  report.keyboard.after_w=await state(env.page);
  if(report.keyboard.after_d.position.x-report.keyboard.before.position.x<2||report.keyboard.after_d.position.z-report.keyboard.after_w.position.z<2)throw new Error('WASD movement did not follow expected axes');
  await loadCargo(env.page,100);
  report.keyboard.before_space=await state(env.page);
  await env.page.keyboard.down(' ');await new Promise(r=>setTimeout(r,60));await env.page.keyboard.up(' ');
  report.keyboard.after_space=await state(env.page);
  await env.page.keyboard.press('z');report.keyboard.after_z=await state(env.page);
  if(report.keyboard.after_space.cargo!==report.keyboard.before_space.cargo+100||report.keyboard.after_z.cargo!==report.keyboard.before_space.cargo)throw new Error('SPACE pickup / Z undo failed');
  const supply=await env.page.$('[data-place="100"]'),bounds=await supply.boundingBox();
  report.hold={before:await state(env.page)};
  await env.page.mouse.move(bounds.x+bounds.width/2,bounds.y+bounds.height/2);await env.page.mouse.down();await new Promise(r=>setTimeout(r,1050));await env.page.mouse.up();
  report.hold.after=await state(env.page);
  if(report.hold.after.cargo-report.hold.before.cargo<200||report.hold.after.input.holding!==null)throw new Error('Hold-to-repeat or pointer release failed');
  await env.page.keyboard.down('d');await new Promise(r=>setTimeout(r,150));
  const blank=await env.browser.newPage();await blank.goto('about:blank');await blank.bringToFront();await new Promise(r=>setTimeout(r,180));
  report.blur={blurred_state:await state(env.page),document_focused:await env.page.evaluate(()=>document.hasFocus())};
  await new Promise(r=>setTimeout(r,500));report.blur.after_wait=await state(env.page);
  await blank.close();await env.page.bringToFront();await env.page.keyboard.up('d');
  if(report.blur.blurred_state.input.keys.length||Math.abs(report.blur.after_wait.position.x-report.blur.blurred_state.position.x)>.1)throw new Error('Browser focus loss did not clear movement keys');
  report.errors=env.errors;report.completed_at=new Date().toISOString();write();
}catch(e){report.failure=e.stack;write();throw e;}finally{await env.close();}
