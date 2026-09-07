import fs from 'node:fs';
import {openGame,start,state,clickVisible,waitState,solveCurrent,loadCargo,submit,collectSamples} from './browser-bot.mjs';

const report={input_route:'Puppeteer trusted pointer/mouse events on visible controls; read-only __GAME_TEST__.getState; no action or solution hooks',scenarios:[],started_at:new Date().toISOString()};
const out=new URL('input-results.json',import.meta.url);
const write=()=>fs.writeFileSync(out,JSON.stringify(report,null,2));
const env=await openGame();
try{
  report.math=await collectSamples(env.page);
  for(const mode of (process.env.ORBIT_SCENARIOS||'normal,recovery').split(',')){
    await env.page.reload({waitUntil:'networkidle0'});await start(env.page);
    const result={mode,trace:[],initial:await state(env.page)};
    report.scenarios.push(result);write();
    await env.page.screenshot({path:new URL(`${mode}-start.png`,import.meta.url).pathname});
    for(let mission=1;mission<=9;mission++){
      console.log(`${mode}: mission ${mission}`);
      if(mode==='recovery'&&mission===2){
        const s=await state(env.page);
        result.before_mistake=s;
        await loadCargo(env.page,1,result.trace); // Every admissible target is at least 220.
        await submit(env.page,result.trace);
        result.after_mistake=await state(env.page);
        if(result.after_mistake.lives!==s.lives-1||result.after_mistake.firstTryAttempts!==1||result.after_mistake.firstTryCorrect!==0)throw new Error('First-mistake accounting mismatch');
        await env.page.screenshot({path:new URL('recovery-after-mistake.png',import.meta.url).pathname});
      }
      await solveCurrent(env.page,result.trace);
      const s=await state(env.page);
      if(s.phase!=='success'||s.solved!==mission)throw new Error(`Campaign failed at ${mode} ${mission}: ${JSON.stringify(s)}`);
      if(mission===1||mission===5||mission===9)await env.page.screenshot({path:new URL(`${mode}-success-${mission}.png`,import.meta.url).pathname});
      await clickVisible(env.page,'#next');
      await waitState(env.page,s=>mission===9?s.phase==='won':s.phase==='playing'&&s.level===mission+1,'next mission');
      write();
    }
    result.final=await state(env.page);
    if(result.final.phase!=='won'||result.final.solved!==9)throw new Error('Declared campaign goal not reached');
    if(mode==='normal'&&(result.final.firstTryCorrect!==8||result.final.firstTryAttempts!==8||result.final.lives!==3))throw new Error('Normal completion first-try totals invalid');
    if(mode==='recovery'&&(result.final.firstTryCorrect!==7||result.final.firstTryAttempts!==8||result.final.lives!==2))throw new Error('Recovery completion first-try totals invalid');
    await env.page.screenshot({path:new URL(`${mode}-complete.png`,import.meta.url).pathname});write();
  }
  report.errors=env.errors;if(env.errors.length)throw new Error('Console or network errors present');report.completed_at=new Date().toISOString();write();
}catch(e){report.failure=e.stack;write();throw e;}finally{await env.close();}
