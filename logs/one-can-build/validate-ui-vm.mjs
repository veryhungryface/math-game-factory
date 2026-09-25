import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';

const html=fs.readFileSync('public/g/one-can/index.html','utf8');
const engine=fs.readFileSync('public/g/one-can/engine.js','utf8');
const scripts=Array.from(html.matchAll(/<script([^>]*)>([\s\S]*?)<\/script>/g));
const inline=scripts.filter(m=>!m[1].includes('src=')).map(m=>m[2]);
const report={generatedAt:new Date().toISOString(),htmlSha256:crypto.createHash('sha256').update(html).digest('hex'),environment:'Node vm with lightweight DOM/canvas stubs. Synthetic callback invocation only. NOT a browser, pixel test, real pointer input, CSS layout, or FPS validation.',checks:[],viewports:[]};
for(const script of inline)new vm.Script(script);
report.checks.push('All inline scripts parse');
const fileRefs=Array.from(html.matchAll(/(?:src|href)=["']([^"']+)["']/g),m=>m[1]);
for(const ref of fileRefs){assert.ok(!/^https?:|^\/\//.test(ref),ref);assert.ok(!ref.startsWith('/'),ref);assert.ok(fs.existsSync('public/g/one-can/'+ref),ref);}
report.checks.push('Every literal HTML source/href is a relative existing local file');

function setup(width,height){
  const elements=new Map(),raf=[],ctxCalls={count:0},documentListeners={},windowListeners={};
  const context=new Proxy({}, {get(t,k){if(k in t)return t[k];return (...a)=>{ctxCalls.count++;return undefined;};},set(t,k,v){t[k]=v;return true;}});
  const createElement=(tag)=>{
    const styles={setProperty(k,v){this[k]=v;}};
    const e={tagName:tag.toUpperCase(),hidden:false,style:styles,children:[],listeners:{},attributes:{},className:'',textContent:'',innerHTML:'',width:0,height:0,
      classList:{values:new Set(),toggle(k,on){if(on)this.values.add(k);else this.values.delete(k);},add(k){this.values.add(k);},remove(k){this.values.delete(k);}},
      addEventListener(k,fn){this.listeners[k]=fn;},setAttribute(k,v){this.attributes[k]=v;},appendChild(c){this.children.push(c);},getContext(){return context;},
      getBoundingClientRect(){return{x:0,y:0,left:0,top:0,width:this.width,height:this.height};}
    };return e;
  };
  for(const m of html.matchAll(/<([a-z]+)[^>]*\bid="([^"]+)"[^>]*>/g)){const e=createElement(m[1]);e.hidden=/\shidden(?:\s|>)/.test(m[0]);elements.set(m[2],e);}
  const doc={getElementById(id){assert.ok(elements.has(id),'Unknown element '+id);return elements.get(id);},createElement,documentElement:createElement('html'),addEventListener(k,fn){documentListeners[k]=fn;}};
  let time=100;
  const sandbox={console,document:doc,innerWidth:width,innerHeight:height,devicePixelRatio:1,matchMedia:()=>({matches:false}),localStorage:{getItem:()=>null,setItem:()=>{}},requestAnimationFrame:f=>raf.push(f),addEventListener:(k,f)=>windowListeners[k]=f};
  sandbox.window=sandbox;vm.createContext(sandbox);vm.runInContext(engine,sandbox);for(const script of inline)vm.runInContext(script,sandbox);
  const step=seconds=>{for(let f=0;f<Math.ceil(seconds*60);f++){const next=raf.shift();assert.ok(next,'Animation frame loop stopped');time+=1000/60;next(time);}};
  const event=()=>({preventDefault(){},clientX:10,clientY:500,target:{tagName:'CANVAS'}});
  return {sandbox,elements,step,event,ctxCalls,windowListeners,documentListeners};
}
for(const [width,height] of [[390,844],[820,1180],[1280,800],[1920,1080],[640,390]]){
  const {sandbox:c,elements:e,step,event,ctxCalls}=setup(width,height);
  const hook=c.__GAME_TEST__;assert.equal(hook.ready,true);assert.equal(hook.getState().phase,'title');
  e.get('startButton').onclick();step(.1);
  assert.equal(hook.getState().frozen,true);assert.equal(hook.getState().canMl,0);
  const buttons=e.get('cupTargets').children;assert.equal(buttons.length,3);
  e.get('playUI').listeners.pointerdown({clientX:40,clientY:150,target:{closest(){return null;}}});step(.6);const before=e.get('feedback').textContent;e.get('feedback').textContent='';e.get('field').listeners.pointerdown(event());step(.05);assert.notEqual(e.get('feedback').textContent,'','Invalid canvas input had no textual response');
  buttons[0].listeners.pointerdown(event());step(1.6);assert.equal(hook.getState().canMl,500);assert.equal(hook.getState().frozen,true);
  buttons[0].listeners.pointerdown(event());step(1.6);assert.equal(hook.getState().frozen,false);assert.equal(hook.getState().shipped,1);
  const score=hook.getState().score;hook.answerCorrect();assert.ok(hook.getState().score>score,'Hook correct failed to increase score');
  const lives=hook.getState().lives;hook.answerWrong();assert.equal(hook.getState().lives,lives-1);
  const layout=hook.getLayout();assert.ok(layout.canBounds.y+layout.canBounds.h<=height,'Can body extends below viewport');assert.equal(layout.land,(width>=height&&width>=640)||width>=1024);assert.equal(layout.playW,Math.min(width,1280));
  const targets=hook.getInputTargets();
  for(const t of targets){assert.ok(t.x>=0&&t.x<=width&&t.y>=0&&t.y<=height,`Offscreen target ${width}x${height}`);}
  for(const b of buttons.filter(x=>!x.hidden)){assert.ok(parseFloat(b.style.width)>=44&&parseFloat(b.style.height)>=44);}
  hook.answerWrong();hook.answerWrong();assert.equal(hook.getState().phase,'gameover');const terminal=JSON.stringify(hook.getState());
  hook.answerCorrect();hook.answerWrong();assert.equal(JSON.stringify(hook.getState()),terminal,'QA hooks revive a terminal run');
  step(2);assert.equal(e.get('result').hidden,false);assert.ok(ctxCalls.count>100);
  hook.start();step(.05);assert.equal(hook.getState().phase,'playing');assert.equal(hook.getState().frozen,false);
  for(const n of [40,17,63])assert.equal(hook.sampleProblems(n).length,n);
  // Execute whole runs through the canvas hit-test callback. These are synthetic
  // coordinates in a VM, not trusted browser events or a visual play test.
  for(const wrongFirst of [false,true]) {
    hook.start();step(.05);
    if(wrongFirst){const s=hook.getState(),need=Math.min(1000,s.problem.targetMl-s.orderShipped*1000)-s.canMl;const t=hook.getInputTargets().find(t=>t.ml>need);e.get('field').listeners.pointerdown({preventDefault(){},clientX:t.x,clientY:t.y,target:{tagName:'CANVAS'}});step(1.7);assert.equal(hook.getState().lives,2);}
    let count=0;
    while(hook.getState().phase==='playing') {
      const s=hook.getState(),need=Math.min(1000,s.problem.targetMl-s.orderShipped*1000)-s.canMl;
      const ts=hook.getInputTargets().filter(t=>t.ml<=need&&(s.dropsLeft>1||t.ml===need));
      const t=ts.find(t=>t.ml===need)||ts.sort((a,b)=>b.ml-a.ml)[0];assert.ok(t,'No budget-solvable cup');
      e.get('field').listeners.pointerdown({preventDefault(){},clientX:t.x,clientY:t.y,target:{tagName:'CANVAS'}});step(1.7);assert.ok(++count<90);
    }
    assert.equal(hook.getState().phase,'clear');assert.ok(hook.getState().shipped>=8);assert.equal(hook.getState().lives,wrongFirst?2:3);
  }
  report.viewports.push({width,height,land:layout.land,stageWidth:layout.playW,targetsInsideViewport:true,targetSizesAtLeast44:true,tutorialViaSyntheticCallbacks:true,hooks:true,renderCalls:ctxCalls.count});
}
report.checks.push('Five viewport modes calculate rail targets within the viewport, >=44px button dimensions');
report.checks.push('Synthetic canvas refusal and real UI cup handler paths execute, both fixed tutorial drops progress');
report.checks.push('Hooks ready/start/correct/wrong/state/samples work; terminal hook calls cannot revive');
report.checks.push('Normal and one-mistake recovery runs finish through synthetic canvas coordinates in all five viewport modes');
report.checks.push('Frame loop and result rendering execute without VM runtime exceptions');
fs.writeFileSync('logs/one-can-build/ui-vm-report.json',JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
