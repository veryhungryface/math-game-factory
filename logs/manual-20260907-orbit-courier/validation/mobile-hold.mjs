import fs from 'node:fs';
import {openGame,state,waitState} from './browser-bot.mjs';
const env=await openGame(),report={kind:'Real touchscreen hold / release on 390x844 viewport'};
try{
  await env.page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
  await env.page.reload({waitUntil:'networkidle0'});await env.page.waitForFunction(()=>window.__GAME_TEST__?.ready);
  const center=async s=>{const b=await(await env.page.$(s)).boundingBox();return [b.x+b.width/2,b.y+b.height/2];};
  await env.page.touchscreen.tap(...await center('#start'));await waitState(env.page,s=>s.phase==='playing','touch start');
  await new Promise(r=>setTimeout(r,100));
  await env.page.touchscreen.tap(...await center('[data-place="100"]'));await waitState(env.page,s=>s.cargo===100,'touch initial arrival');
  report.before=await state(env.page);
  const handle=await env.page.touchscreen.touchStart(...await center('[data-place="100"]'));
  await new Promise(r=>setTimeout(r,1100));await handle.end();
  report.after=await state(env.page);await new Promise(r=>setTimeout(r,500));report.after_release_wait=await state(env.page);
  if(report.after.cargo-report.before.cargo<200||report.after.input.holding!==null||report.after_release_wait.cargo!==report.after.cargo)throw new Error('Mobile hold / release failed');
  await env.page.touchscreen.tap(...await center('#undo'));report.after_undo=await state(env.page);
  if(report.after_undo.cargo!==report.after.cargo-100)throw new Error('Mobile undo failed');
  await env.page.screenshot({path:new URL('mobile-touch-hold.png',import.meta.url).pathname});
  report.errors=env.errors;
}catch(e){report.failure=e.stack;throw e;}finally{fs.writeFileSync(new URL('mobile-touch-hold.json',import.meta.url),JSON.stringify(report,null,2));await env.close();}
