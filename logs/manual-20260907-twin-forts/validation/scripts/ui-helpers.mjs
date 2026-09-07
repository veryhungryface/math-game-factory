// Helpers only; importing this file does not launch a browser.
import {mkdir,writeFile} from 'node:fs/promises';
import {oracle} from './oracle.mjs';

export async function uiQuantities(page) {
  return page.evaluate(()=>({
    total:Number(document.querySelector('#total')?.textContent.match(/\d+/)?.[0]),
    a:Number(document.querySelector('#ratio-left')?.textContent),
    b:Number(document.querySelector('#ratio-right')?.textContent),
    left:Number(document.querySelector('#left-count')?.textContent.match(/\d+/)?.[0]),
    right:Number(document.querySelector('#right-count')?.textContent.match(/\d+/)?.[0]),
    fieldLeft:document.querySelector('#field-left')?.textContent,
    fieldRight:document.querySelector('#field-right')?.textContent,
    prompt:document.querySelector('#command')?.innerText,
    feedback:document.querySelector('#feedback')?.innerText,
    action:document.querySelector('#deploy')?.innerText,
    actionDisabled:document.querySelector('#deploy')?.disabled,
    inputValue:Number(document.querySelector('#split')?.value)
  }));
}

export async function screenshotAndRecord(page,dir,name,log,details={}) {
  await mkdir(dir,{recursive:true});
  const file=`${dir}/${name}.png`;
  await page.screenshot({path:file});
  const ui=await uiQuantities(page);
  const diagnostic=await page.evaluate(()=>window.__GAME_TEST__?.getState?.()??null);
  const item={at:new Date().toISOString(),name,screenshot:file,ui,diagnostic,...details};
  log.push(item);
  await writeFile(`${dir}/input-log.json`,JSON.stringify(log,null,2));
  return item;
}

export async function actualClick(page,selector) {
  const el=await page.$(selector);
  if(!el) throw new Error(`Missing visible action ${selector}`);
  const b=await el.boundingBox();
  if(!b)throw new Error(`Hidden action ${selector}`);
  await page.mouse.click(b.x+b.width/2,b.y+b.height/2);
}

// Move through real keyboard events. Never set DOM .value, dispatch synthetic
// events in JS, call answerCorrect(), or call the internal grading predicate.
export async function keyboardAllocate(page,target) {
  await page.focus('#split');
  let old=await uiQuantities(page);
  for(let i=0;i<110;i++){
    if(old.left===target)return old;
    await page.keyboard.press(old.left<target?'ArrowRight':'ArrowLeft');
    const next=await uiQuantities(page);
    if(next.left===old.left)throw new Error(`Keyboard allocation did not change ${old.left}`);
    old=next;
  }
  throw new Error(`Keyboard allocation did not reach ${target}`);
}

export async function precisionAllocate(page,target) {
  let p=await uiQuantities(page);
  for(let i=0;i<110;i++){
    if(p.left===target)return p;
    await actualClick(page,p.left<target?'#move-left':'#move-right');
    const next=await uiQuantities(page);
    if(next.left===p.left)throw new Error(`Precision allocation did not change ${p.left}`);
    p=next;
  }
  throw new Error(`Precision allocation did not reach ${target}`);
}

export async function mouseDragAllocation(page,ratio) {
  const b=await (await page.$('#split')).boundingBox();
  await page.mouse.move(b.x+b.width/2,b.y+b.height/2);
  await page.mouse.down();
  await page.mouse.move(b.x+Math.max(1,Math.min(b.width-1,b.width*ratio)),b.y+b.height/2,{steps:12});
  await page.mouse.up();
  return uiQuantities(page);
}

export async function touchDragAllocation(page,ratio) {
  const b=await (await page.$('#split')).boundingBox();
  const session=await page.createCDPSession();
  const x0=b.x+b.width/2,y=b.y+b.height/2;
  await session.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:x0,y,id:1,radiusX:2,radiusY:2,force:1}]});
  for(let step=1;step<=12;step++){
    const x=x0+((b.x+b.width*ratio)-x0)*(step/12);
    await session.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x,y,id:1,radiusX:2,radiusY:2,force:1}]});
  }
  await session.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
  await session.detach();
  return uiQuantities(page);
}

export async function visibleAnswer(page) {
  const p=await uiQuantities(page);
  return {visible:p,calculated:oracle(p.total,p.a,p.b)};
}
