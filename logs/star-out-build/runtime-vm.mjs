import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

// Offline behavioral harness only. These mocks do not implement browser layout,
// painting, hit testing, trusted PointerEvents, audio output, or GPU performance.
// The unmodified game-code script registers its actual handlers and RAF callback;
// tests deliver coordinate-shaped events to those handlers and advance queued RAF.
const HERE = path.dirname(fileURLToPath(import.meta.url));
const DEFAULT_HTML = path.resolve(HERE, '../../public/g/star-out/index.html');

export function createRuntime({ htmlPath = DEFAULT_HTML, width = 390, height = 844,
  seed = 0x12345678, reducedMotion = true, storage = {}, sourceHtml } = {}) {
  const html = sourceHtml ?? fs.readFileSync(htmlPath, 'utf8');
  const code = html.match(/<script\b[^>]*\bid=["']game-code["'][^>]*>([\s\S]*?)<\/script>/i)?.[1];
  if (!code) throw new Error('Missing script id="game-code"');
  let now = 0, nextId = 1, rng = seed >>> 0;
  const rafQueue = new Map(), timerQueue = new Map(), errors = [], paintCalls = {};
  const events = [], domById = new Map(), allNodes = [];
  const localStorageData = new Map(Object.entries(storage).map(([k,v]) => [k,String(v)]));
  const clone = value => value === undefined ? undefined : JSON.parse(JSON.stringify(value));
  function eventTarget(target) {
    const listeners = new Map();
    target.addEventListener = (type, fn) => {
      if (!listeners.has(type)) listeners.set(type, new Set());
      listeners.get(type).add(fn);
    };
    target.removeEventListener = (type, fn) => listeners.get(type)?.delete(fn);
    target.dispatchEvent = event => {
      event.target ??= target; event.currentTarget = target;
      event.preventDefault ??= () => { event.defaultPrevented = true; };
      event.stopPropagation ??= () => {};
      for (const fn of listeners.get(event.type) || []) fn.call(target, event);
      if (typeof target['on' + event.type] === 'function') target['on' + event.type](event);
      return !event.defaultPrevented;
    };
    target._listeners = listeners;
    return target;
  }
  const drawing = new Proxy({
    canvas: null,
    measureText: text => ({ width: String(text).length * 12, actualBoundingBoxAscent: 16, actualBoundingBoxDescent: 4 }),
    createLinearGradient: () => ({ addColorStop() {} }),
    createRadialGradient: () => ({ addColorStop() {} }),
    createPattern: () => ({}),
    getImageData: (x,y,w,h) => ({ data: new Uint8ClampedArray(w*h*4), width:w, height:h }),
    createImageData: (w,h) => ({ data: new Uint8ClampedArray(w*h*4), width:w, height:h }),
    isPointInPath: () => false,
  }, { get(target, key) {
    if (key in target) return target[key];
    return (...args) => { paintCalls[key] = (paintCalls[key] || 0) + 1; };
  }});
  function element(tag = 'div', attrs = {}) {
    const classes = new Set((attrs.class || '').split(/\s+/).filter(Boolean));
    const el = eventTarget({ tagName: tag.toUpperCase(), id: attrs.id || '', textContent: '', innerHTML: '',
      hidden: Object.hasOwn(attrs,'hidden'), children: [], parentElement: null, attributes: {...attrs},
      dataset: {}, width:0, height:0, clientWidth:Math.min(width,1280), clientHeight:height,
      offsetWidth:Math.min(width,1280), offsetHeight:height,
      style: { setProperty(k,v){this[k]=v;}, removeProperty(k){delete this[k];} },
      classList: {add(...xs){xs.forEach(x=>classes.add(x));},remove(...xs){xs.forEach(x=>classes.delete(x));},
        contains:x=>classes.has(x),toggle(x,on){const value=on??!classes.has(x);value?classes.add(x):classes.delete(x);return value;}},
      getBoundingClientRect(){const w=this.clientWidth,h=this.clientHeight;return {x:0,y:0,left:0,top:0,right:w,bottom:h,width:w,height:h};},
      setAttribute(k,v){this.attributes[k]=String(v);},getAttribute(k){return this.attributes[k]??null;},
      removeAttribute(k){delete this.attributes[k];},
      appendChild(child){this.children.push(child);child.parentElement=this;return child;},
      append(...children){children.forEach(c=>this.appendChild(c));},remove(){},
      getContext:()=>drawing, setPointerCapture(){},releasePointerCapture(){},hasPointerCapture:()=>true,
      querySelectorAll:selector=>select(selector),querySelector:selector=>select(selector)[0]||null,
      focus(){},blur(){},click(){this.dispatchEvent({type:'click',isTrusted:false});},
    });
    Object.defineProperty(el,'className',{get:()=>[...classes].join(' '),set:value=>{classes.clear();String(value).split(/\s+/).forEach(c=>classes.add(c));}});
    if(el.id)domById.set(el.id,el);allNodes.push(el);return el;
  }
  function select(selector) {
    return selector.split(',').flatMap(s=>{
      s=s.trim();
      if(s.startsWith('#'))return [domById.get(s.slice(1))].filter(Boolean);
      if(s.startsWith('.'))return allNodes.filter(e=>e.classList.contains(s.slice(1)));
      if(s==='body *'||s==='*')return allNodes;
      return allNodes.filter(e=>e.tagName.toLowerCase()===s);
    });
  }
  for(const match of html.matchAll(/<([a-z][\w-]*)\b([^>]*)>/gi)){
    const attrs={};for(const a of match[2].matchAll(/([\w-]+)(?:\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+)))?/g))attrs[a[1]]=a[2]??a[3]??a[4]??'';
    if(attrs.id||attrs.class)element(match[1],attrs);
  }
  const document = eventTarget({ documentElement:element('html'),body:element('body'),visibilityState:'visible',hidden:false,
    getElementById:id=>domById.get(id)||null, querySelector:selector=>select(selector)[0]||null,
    querySelectorAll:select,createElement:tag=>element(tag),createElementNS:(ns,tag)=>element(tag),
  });
  drawing.canvas=domById.get('table');
  const randomMath=Object.create(Math);randomMath.random=()=>{rng=(Math.imul(rng,1664525)+1013904223)>>>0;return rng/4294967296;};
  class MockImage {
    constructor(){this.width=1024;this.height=1024;this.naturalWidth=1024;this.naturalHeight=1024;this.complete=true;this.onload=null;this.onerror=null;}
    set src(value){this._src=value;this.onload?.();}get src(){return this._src;}decode(){return Promise.resolve();}
  }
  class MockAudioContext {
    constructor(){this.currentTime=0;this.state='running';this.destination={};}
    resume(){return Promise.resolve();}createOscillator(){const param={setValueAtTime(){},exponentialRampToValueAtTime(){},linearRampToValueAtTime(){}};return {frequency:param,connect(){},start(){},stop(){}};}
    createGain(){return {gain:{setValueAtTime(){},exponentialRampToValueAtTime(){},linearRampToValueAtTime(){}},connect(){}};}
  }
  const sandbox=eventTarget({document,innerWidth:width,innerHeight:height,devicePixelRatio:1,
    matchMedia:()=>({matches:reducedMotion,addEventListener(){},removeEventListener(){}}),
    localStorage:{getItem:k=>localStorageData.get(k)??null,setItem:(k,v)=>localStorageData.set(k,String(v)),removeItem:k=>localStorageData.delete(k)},
    console:{log(){},warn(...args){errors.push({type:'warn',text:args.join(' ')});},error(...args){errors.push({type:'error',text:args.join(' ')});}},
    performance:{now:()=>now},Math:randomMath,Image:MockImage,AudioContext:MockAudioContext,
    Path2D:class {moveTo(){}lineTo(){}closePath(){}arc(){}quadraticCurveTo(){}},
    requestAnimationFrame:fn=>{const id=nextId++;rafQueue.set(id,fn);return id;},cancelAnimationFrame:id=>rafQueue.delete(id),
    setTimeout:(fn,ms=0)=>{const id=nextId++;timerQueue.set(id,{fn,at:now+ms});return id;},clearTimeout:id=>timerQueue.delete(id),
    getComputedStyle:el=>({display:el.hidden?'none':'block',visibility:'visible',opacity:'1',getPropertyValue:key=>el.style[key]||''}),
  });
  sandbox.window=sandbox;sandbox.self=sandbox;sandbox.globalThis=sandbox;
  const context=vm.createContext(sandbox);
  vm.runInContext(code,context,{filename:htmlPath,timeout:10000});
  const evaluate=source=>vm.runInContext(source,context,{timeout:10000});
  function advance(seconds, {frameMs=1000/60}={}){
    const until=now+seconds*1000;
    while(now<until-1e-7){
      now=Math.min(until,now+frameMs);
      for(const [id,timer] of [...timerQueue])if(timer.at<=now){timerQueue.delete(id);timer.fn();}
      const queue=[...rafQueue.values()];rafQueue.clear();
      for(const callback of queue)callback(now);
    }
    return state();
  }
  function pointer(type,x,y,{pointerId=1,target='table',pointerType='touch'}={}){
    const e={type,clientX:x,clientY:y,pointerId,pointerType,isPrimary:true,isTrusted:false,buttons:type==='pointerup'?0:1,button:0};
    events.push({type,x,y,t:now});domById.get(target).dispatchEvent(e);return state();
  }
  function drag(from,to,{duration=.12,steps=6}={}){
    pointer('pointerdown',from.x,from.y);
    for(let i=1;i<=steps;i++){advance(duration/steps);pointer('pointermove',from.x+(to.x-from.x)*i/steps,from.y+(to.y-from.y)*i/steps);}
    pointer('pointerup',to.x,to.y);return state();
  }
  function dragVertices(i,j,options){const g=clone(evaluate('geometry()'));return drag(g.vertices[i],g.vertices[j],options);}
  function state(){return clone(sandbox.__GAME_TEST__.getState());}
  return {context,evaluate,expose:source=>clone(evaluate(source)),advance,pointer,drag,dragVertices,state,
    click:id=>domById.get(id).click(),dom:id=>domById.get(id),events,errors,paintCalls,
    get now(){return now;},get storage(){return Object.fromEntries(localStorageData);},
    metadata:{mode:'offline-node-vm-mocks',browserVerified:false,htmlPath,width,height,seed,
      sourceSha256:crypto.createHash('sha256').update(html).digest('hex'),scriptSha256:crypto.createHash('sha256').update(code).digest('hex')},
  };
}

function fixedLoopSmoke(){
  const normal=createRuntime();normal.click('begin');normal.advance(.1);
  const initial=normal.state();normal.dragVertices(0,2);normal.advance(.15);const correct=normal.state();
  if(!(correct.score>initial.score))throw new Error('Tutorial actual registered pointer handler did not award points');
  normal.advance(1.5);const mainStart=normal.state();normal.dragVertices(0,2);normal.advance(.15);const mainCorrect=normal.state();
  if(mainCorrect.solved!==1)throw new Error('Fixed main loop failed to progress');
  const recovery=createRuntime();recovery.click('begin');recovery.dragVertices(0,2);recovery.advance(1.5);
  const beforeWrong=recovery.state();recovery.dragVertices(0,1);const wrong=recovery.state();recovery.dragVertices(0,2);recovery.advance(.15);const recovered=recovery.state();
  if(wrong.lives!==beforeWrong.lives-1||recovered.solved!==1||recovered.lives!==wrong.lives)throw new Error('One mistake then recovery path failed');
  const evidence={at:new Date().toISOString(),...normal.metadata,limitations:'Mocks execute the unchanged game script and registered coordinate handlers. This is not a browser PointerEvent, layout, rendering, accessibility, audio, network, or performance test.',
    normal:{initial,correct,mainStart,mainCorrect,eventCount:normal.events.length},recovery:{beforeWrong,wrong,recovered,eventCount:recovery.events.length},errors:[...normal.errors,...recovery.errors]};
  const out=path.join(HERE,'fixed-loop-vm.json');fs.writeFileSync(out,JSON.stringify(evidence,null,2)+'\n');console.log(JSON.stringify(evidence,null,2));
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))fixedLoopSmoke();
