// Pure helpers: importing this module never starts a browser.
import {mkdir,writeFile} from 'node:fs/promises';
import {oracle} from './math-oracle.mjs';
export async function visibleProblem(page){
  const visible=await page.evaluate(()=>{
    const text=id=>document.getElementById(id)?.innerText??'';
    const number=text=>Number(text.match(/\d+/)?.[0]);
    return {
      width:number(text('order-width')),
      targetBoxes:number(text('order-request')),
      chosenHeight:number(text('height-readout')),
      request:text('order-request'),rule:text('farm-rule'),
      heightLabel:text('height-label'),widthLabel:text('width-label'),
      feedback:text('feedback'),harvest:text('harvest-count'),
      action:text('plant'),actionDisabled:document.getElementById('plant')?.disabled,
    };
  });
  const readable=Number.isInteger(visible.width)&&Number.isInteger(visible.targetBoxes)&&Number.isInteger(visible.chosenHeight);
  return {visible,calculated:readable?oracle(visible,visible.chosenHeight):null};
}
export async function state(page){return page.evaluate(()=>window.__GAME_TEST__?.getState?.()??null);}
export async function actualClick(page,selector,{touch=false}={}){
  const handle=await page.$(selector);
  if(!handle)throw new Error(`Missing control ${selector}`);
  const b=await handle.boundingBox();
  if(!b||b.width<1||b.height<1)throw new Error(`Hidden control ${selector}`);
  const point={x:b.x+b.width/2,y:b.y+b.height/2};
  if(touch)await page.touchscreen.tap(point.x,point.y);else await page.mouse.click(point.x,point.y);
  return point;
}
export async function precisionHeight(page,target,{touch=false}={}){
  for(let i=0;i<15;i++){
    const {visible}=await visibleProblem(page);
    if(visible.chosenHeight===target)return visible;
    await actualClick(page,visible.chosenHeight<target?'#height-plus':'#height-minus',{touch});
    const after=(await visibleProblem(page)).visible;
    if(after.chosenHeight===visible.chosenHeight)throw new Error(`Height button did not change ${visible.chosenHeight}`);
  }
  throw new Error(`Height buttons failed to reach ${target}`);
}
export async function keyboardHeight(page,target){
  // A real pointer click focuses the visible control; adjustment uses real keyboard events.
  await actualClick(page,'#field-handle');
  for(let i=0;i<15;i++){
    const {visible}=await visibleProblem(page);
    if(visible.chosenHeight===target)return visible;
    await page.keyboard.press(visible.chosenHeight<target?'ArrowUp':'ArrowDown');
    const after=(await visibleProblem(page)).visible;
    if(after.chosenHeight===visible.chosenHeight)throw new Error(`Keyboard did not change height ${visible.chosenHeight}`);
  }
  throw new Error(`Keyboard failed to reach ${target}`);
}
export async function touchPath(page,points){
  if(!points.length)throw new Error('Empty touch path');
  const session=await page.createCDPSession();
  const p=point=>({x:point.x,y:point.y,id:1,radiusX:3,radiusY:3,force:1});
  try{
    await session.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[p(points[0])]});
    for(const point of points.slice(1))await session.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[p(point)]});
    await session.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
  }finally{await session.detach();}
}
export async function mousePath(page,points){
  if(!points.length)throw new Error('Empty mouse path');
  await page.mouse.move(points[0].x,points[0].y);await page.mouse.down();
  for(const point of points.slice(1))await page.mouse.move(point.x,point.y);
  await page.mouse.up();
}
export async function screenshotAndRecord(page,directory,name,log,details={}){
  await mkdir(directory,{recursive:true});const file=`${directory}/${name}.png`;
  await page.screenshot({path:file});
  const item={at:new Date().toISOString(),name,screenshot:file,...await visibleProblem(page),diagnostic:await state(page),...details};
  log.push(item);await writeFile(`${directory}/input-log.json`,JSON.stringify(log,null,2)+'\n');return item;
}
