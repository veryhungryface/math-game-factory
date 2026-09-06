(() => {
'use strict';
const $=s=>document.querySelector(s), $$=s=>[...document.querySelectorAll(s)];
const canvas=$('#world'),ctx=canvas.getContext('2d',{alpha:false,willReadFrequently:true});
const names=['솜바람 해안','바늘숲 고개','노을 리본교'];
const courseWords=['해안','숲길','리본교'];
const palette=[{sky:'#eee9df',fog:'#c5c3d1',land:'#dddac1',water:'#75b9b1',tree:'#77a78b'},{sky:'#e5e8d9',fog:'#c0c4cf',land:'#d2d9b9',water:'#98b9aa',tree:'#549183'},{sky:'#f2e2cb',fog:'#d2bcc5',land:'#e6d4ad',water:'#94b4b2',tree:'#b2a374'}];
let W=390,H=844,dpr=1,tick=0,last=0,roadTravel=0,playerX=0,steer=0,phase='title',resumePhase='planning';
let level=0,score=0,lives=3,solved=0,coins=0,errors=0,selected=[null,null],problem,seed=(Date.now()&0xfffffff)||19;
let elapsed=0,energy=0,operationsApplied=0,attemptSum=0,gotCoins=new Set(),particles=[],dialogAction=()=>{},lastSound=0,muted=true,audio;
let captureStill=false,needsDraw=true,lastDrawPhase="",autoFork=-1,drawCount=0,lastSpeed=-1;const treeCache=new Map();
const images={};for(const [name,file] of Object.entries({jaei:'jaei/think',tak:'tak/think',joy:'jaei/joy',mungchi:'mungchi/pose-collect',mok:'mok/think'})){const im=new Image();im.src='../../vendor/cast/'+file+'.png';images[name]=im;}
const fmt=n=>{const sign=n<0?'-':'';n=Math.abs(n);return sign+Math.floor(n/100)+(n%100?'.'+String(n%100).padStart(2,'0').replace(/0$/,''):'');};
const op=n=>(n>=0?'+':'−')+' '+fmt(Math.abs(n));
function rng(){seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;}
function generate(index,rand=rng){
 const tier=Math.min(2,Math.floor(index/3)),step=tier===0?10:1;
 const start=(tier===0?100:170)+Math.floor(rand()*220/step)*step;
 const a=(15+Math.floor(rand()*60))*step/(tier===0?10:1);
 const aa=tier===0?Math.round(a/10)*10+30:a;
 const b=(tier===0?Math.floor(rand()*6)*10+20:15+Math.floor(rand()*75));
 const delta1=tier===0?20:11+Math.floor(rand()*14);
 const delta2=tier===0?70:57+Math.floor(rand()*32);
 let first=[aa,aa+delta1,aa+delta1*2],second=tier===0?[b,b+delta2,b+delta2*2]:[-b,-b-delta2,-b-delta2*2];
 if(tier===2&&index%2===0) first=[-aa,-aa-delta1,-aa-delta1*2],second=[b,b+delta2,b+delta2*2];
 const route=[Math.floor(rand()*3),Math.floor(rand()*3)];
 // Keep every possible path in the nonnegative, two-decimal curriculum domain.
 const safeStart=Math.max(start,1-Math.min(...first)-Math.min(0,...second));
 const target=safeStart+first[route[0]]+second[route[1]];
 return {start:safeStart,target,options:[first,second],solution:route,tier};
}
function routeSum(p,s){return p.start+p.options[0][s[0]]+p.options[1][s[1]];}
function playTone(freq=400,duration=.12,type='sine',volume=.035){if(muted||!audio)return;const o=audio.createOscillator(),g=audio.createGain();o.type=type;o.frequency.value=freq;g.gain.setValueAtTime(volume,audio.currentTime);g.gain.exponentialRampToValueAtTime(.001,audio.currentTime+duration);o.connect(g);g.connect(audio.destination);o.start();o.stop(audio.currentTime+duration);}
function unlock(){if(!audio){try{audio=new (window.AudioContext||window.webkitAudioContext)();}catch{}}if(audio?.state==='suspended')audio.resume();}
function show(id,on=true){$(id).hidden=!on;}
function updateHUD(){ needsDraw=true; $('#lap').textContent=String(Math.min(3,Math.floor(level/3)+1)).padStart(2,'0')+' / 03';$('#course').textContent=names[Math.min(2,Math.floor(level/3))];$('#score').textContent=score+' 점 · 실뭉치 '+coins;$('#lives').textContent='수리 '+'●'.repeat(lives)+'○'.repeat(3-lives);$('#energy').textContent=fmt(energy);$('#target').textContent=fmt(problem?.target||0);$('#progress i').style.width=(solved/9*100)+'%';$('#sector').textContent='구간 '+(level%3+1)+' / 3';}
function updatePlan(){needsDraw=true;
 $$('.route').forEach(b=>{const row=+b.dataset.row,side=+b.dataset.side,n=problem.options[row][side];b.innerHTML='<small>'+(['왼쪽','가운데','오른쪽'][side])+' · '+(n>=0?'충전':'사용')+'</small>'+op(n)+' <small style="display:inline">L</small>';b.classList.toggle('selected',selected[row]===side);b.setAttribute('aria-pressed',selected[row]===side?'true':'false');b.setAttribute('aria-label',(row+1)+'번째 '+(['왼쪽','가운데','오른쪽'][side])+' 길, '+(n>=0?'충전 ':'사용 ')+fmt(Math.abs(n))+' 리터');});
 const complete=selected.every(v=>v!==null);$('#go').disabled=!complete;
 $('#formula').textContent=complete?fmt(problem.start)+' '+op(problem.options[0][selected[0]])+' '+op(problem.options[1][selected[1]])+' = ? L':'각 갈림길에서 한 길씩 골라요.';
}
function choose(row,side){if(phase!=='planning')return;selected[row]=side;playTone(row?560:430,.07);updatePlan();}
function setupLevel(){autoFork=-1;$('#left').disabled=false;$('#right').disabled=false;problem=generate(level);selected=[null,null];energy=problem.start;phase='planning';playerX=0;steer=0;elapsed=0;$('#banner').textContent='';show('#planner');show('#driveControls',false);show('#dash');updateHUD();updatePlan();}
function startGame(){unlock();show('#title',false);show('#result',false);show('#dialog',false);show('#hud');level=0;score=0;lives=3;solved=0;coins=0;errors=0;particles=[];setupLevel();}
function openDialog(title,text,button,fn,extra='',character='jaei',step=''){resumePhase=phase;phase='dialog';show('#dialog');$('#dialogTitle').textContent=title;$('#dialogText').textContent=text;$('#dialogNext').textContent=button;$('#dialogExtra').innerHTML=extra;$('#dialogStep').textContent=step;$('#speaker').src='../../vendor/cast/'+(character==='tak'?'tak/pose-challenge':character==='mok'?'mok/pose-record':'jaei/think')+'.png';dialogAction=()=>{$('#game').classList.remove('teaching','teach-meter');show('#dialog',false);phase=resumePhase;fn();};}
const tutorial=[['길을 골라 달려요','첫 길, 둘째 길을 하나씩 골라요.\n충전은 더하고, 사용은 빼요.','<div class="example">1.2 + 0.3 + 0.4 = 1.9 L</div>'],['딱 맞으면 다리가 열려요','도착 연료가 다리의 목표와 같아야 해요.\n길을 고르는 동안에는 시간이 멈춰요.','<div class="example">1.9 L = 1.90 L<br><small>끝의 0이 있어도 같은 양이에요.</small></div>'],['출발하면 드리프트!','갈림길은 고른 길로 자동으로 달려요.\n그 사이에는 ← →로 실뭉치를 모아요.\n실뭉치를 놓쳐도 완주할 수 있어요.','']];
function teach(i=0,after=()=>{}){const t=tutorial[i];$('#game').classList.add('teaching');$('#game').classList.toggle('teach-meter',i===1);openDialog(t[0],t[1],i===2?'해볼게요 →':'다음 →',()=>{if(i<2)teach(i+1,after);else{try{localStorage.setItem('onboardingSeen:decimal-drift','1');}catch{}after();}},t[2],'jaei',(i+1)+' / 3 · 놀이 방법');}
function drive(){if(phase!=='planning'||selected.some(v=>v===null))return;unlock();$('#left').disabled=false;$('#right').disabled=false;$('#driveControls p').textContent='좌우로 움직여 실뭉치를 모아요!';phase='driving';elapsed=0;autoFork=-1;operationsApplied=0;attemptSum=routeSum(problem,selected);gotCoins=new Set();show('#planner',false);show('#driveControls');$('#banner').textContent='출발!';playTone(620,.23);}
function burst(x,y,color,n=18){for(let i=0;i<n;i++)particles.push({x,y,vx:(rng()-.5)*170,vy:-70-rng()*130,life:.8+rng()*.5,color,size:3+rng()*5});}
function resolveRace(forceFast=false){
 if(!problem)return;
 const sum=forceFast?routeSum(problem,selected):attemptSum;
 energy=sum;updateHUD();show('#driveControls',false);steer=0;
 if(sum===problem.target){solved++;score+=100+(level%3===2?50:0);burst(W*.5,H*.45,'#f0c054',35);playTone(740,.16);setTimeout(()=>playTone(990,.25),140);level++;updateHUD();
  if(level>=9){finish(true);return;}
  if(forceFast){setupLevel();return;}
  const courseDone=level%3===0;
  openDialog(courseDone?names[Math.floor(level/3)-1]+' 완주!':'기록했다.',courseDone?'좋아. 코스 하나 끝.\n다음은 사용 길이다. 계속 가자.':'연료 '+fmt(sum)+' L. 목표와 같다.\n고른 길을 장부에 올린다.',courseDone?'다음 코스로 →':'계속 달리기 →',setupLevel,'<div class="example">'+fmt(problem.start)+' '+op(problem.options[0][selected[0]])+' '+op(problem.options[1][selected[1]])+' = '+fmt(sum)+'</div>',courseDone?'tak':'mok','기록 '+solved+' / 9');
 }else{lives--;errors++;if(lives<=0){finish(false);return;}
  const diff=Math.abs(sum-problem.target);updateHUD();if(forceFast){phase='planning';energy=problem.start;selected=[null,null];updatePlan();updateHUD();return;}
  openDialog('피트에서 다시 재요',fmt(sum)+' L는 목표보다 '+fmt(diff)+' L '+(sum>problem.target?'많아요.':'적어요.')+'\n소수점 위치를 맞춰 다시 골라 볼까요?', '길 다시 고르기 →',()=>{phase='planning';energy=problem.start;selected=[null,null];show('#planner');updatePlan();updateHUD();},'<div class="example">'+fmt(problem.start)+' '+op(problem.options[0][selected[0]])+' '+op(problem.options[1][selected[1]])+' = '+fmt(sum)+'<br><small>목표 '+fmt(problem.target)+' L</small></div>','jaei','수리 기회 '+lives+'번');playTone(210,.2,'triangle');
 }
}
function finish(won){phase=won?'won':'lost';show('#planner',false);show('#dialog',false);show('#driveControls',false);show('#dash',false);show('#result');$('#banner').textContent='';$('#resultTitle').textContent=won?'세 코스 완주!':'잠깐, 피트에 들러요';$('#resultCast').src='../../vendor/cast/'+(won?'jaei/pose-cheer':'jaei/sad')+'.png';$('#resultText').textContent=won?'정확한 연료로 아홉 다리를 열었어요.\n탁 반장: “좋아. 끝까지 달렸네.”':'완주한 다리 '+solved+'개.\n재이: “괜찮아요. 저도 세 번은 재요.”';$('#resultScore').textContent=score+' 점';$('#badges').innerHTML=courseWords.slice(0,Math.floor(solved/3)).map((s,i)=>'<span>'+(i+1)+'<small>'+s+' 완주</small></span>').join('');try{const old=Number(localStorage.getItem('decimal-drift:best')||0);if(score>old)localStorage.setItem('decimal-drift:best',String(score));}catch{}updateHUD();}
$('#start').onclick=()=>{startGame();let seen=false;try{seen=!!localStorage.getItem('onboardingSeen:decimal-drift');}catch{}if(!seen)teach();};
$('#titleHelp').onclick=()=>{startGame();teach();};$('#help').onclick=()=>{if(phase!=='dialog'&&phase!=='driving')teach();};$('#dialogNext').onclick=()=>{unlock();dialogAction();};$('#go').onclick=drive;
$$('.route').forEach(b=>b.onclick=()=>choose(+b.dataset.row,+b.dataset.side));$('#retry').onclick=startGame;$('#home').onclick=()=>{phase='title';show('#result',false);show('#title');show('#hud',false);show('#dash',false);best();};
$('#sound').onclick=()=>{unlock();muted=!muted;$('#sound').textContent=muted?'♪':'♫';$('#sound').setAttribute('aria-label',muted?'소리 켜기':'소리 끄기');playTone(520,.15);};
for(const [id,dir]of [['#left',-1],['#right',1]]){const b=$(id);b.onpointerdown=e=>{e.preventDefault();b.setPointerCapture(e.pointerId);if(autoFork<0)steer=dir;unlock();};b.onpointerup=b.onpointercancel=b.onlostpointercapture=()=>{steer=0;};}
window.addEventListener('keydown',e=>{if(['ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Enter',' '].includes(e.key))e.preventDefault();if(e.repeat&&phase!=='driving')return;if(phase==='driving'&&autoFork<0){if(e.key==='ArrowLeft'||e.key==='a')steer=-1;if(e.key==='ArrowRight'||e.key==='d')steer=1;}else if(phase==='planning'){if(e.key==='1')choose(0,0);if(e.key==='2')choose(0,1);if(e.key==='3')choose(0,2);if(e.key==='4')choose(1,0);if(e.key==='5')choose(1,1);if(e.key==='6')choose(1,2);if(e.key==='Enter'||e.key===' ')drive();}else if(phase==='dialog'&&(e.key==='Enter'||e.key===' '))dialogAction();});window.addEventListener('keyup',e=>{if(['ArrowLeft','ArrowRight','a','d'].includes(e.key))steer=0;});window.addEventListener('blur',()=>steer=0);
function best(){try{const b=Number(localStorage.getItem('decimal-drift:best')||0);$('#best').textContent=b?'최고 기록'+' · '+b+' 점':'';}catch{}}
function resize(){needsDraw=true;W=innerWidth;H=innerHeight;dpr=Math.min(1,Math.sqrt(240000/(W*H)));canvas.width=Math.round(W*dpr);canvas.height=Math.round(H*dpr);ctx.setTransform(dpr,0,0,dpr,0,0);}
addEventListener('resize',resize);resize();best();
function rr(x,y,w,h,r,fill){ctx.fillStyle=fill;ctx.beginPath();ctx.roundRect(x,y,w,h,r);ctx.fill();}
function ellipse(x,y,rx,ry,fill){ctx.fillStyle=fill;ctx.beginPath();ctx.ellipse(x,y,rx,ry,0,0,Math.PI*2);ctx.fill();}
function poly(points,color){ctx.fillStyle=color;ctx.beginPath();points.forEach((p,i)=>i?ctx.lineTo(...p):ctx.moveTo(...p));ctx.closePath();ctx.fill();}
function project(z,lane=0){const horizon=H*(W>800?.27:.29),scale=z*z;const curve=Math.sin(roadTravel*.0008+z*3.1+Math.floor(level/3)*1.2);const center=W*.5+curve*W*.24*(1-z)+Math.sin(z*5+roadTravel*.00055)*W*.08*(1-z);return {x:center+lane*W*.40*scale,y:horizon+(H-horizon)*scale,w:W*.44*scale,s:scale};}
function tree(x,y,size,color){if(size<2)return;let sprite=treeCache.get(color);if(!sprite){sprite=document.createElement('canvas');sprite.width=144;sprite.height=240;const g=sprite.getContext('2d');g.fillStyle='#526d6421';g.beginPath();g.ellipse(72,220,55,17,0,0,Math.PI*2);g.fill();g.fillStyle='#ac9275';g.beginPath();g.roundRect(64,119,17,96,8);g.fill();const leaf=g.createRadialGradient(46,69,5,72,105,88);leaf.addColorStop(0,color);leaf.addColorStop(.65,color);leaf.addColorStop(1,'#6e8a75');g.fillStyle=leaf;g.beginPath();g.ellipse(72,99,57,76,0,0,Math.PI*2);g.fill();g.fillStyle='#ffffff20';g.beginPath();g.ellipse(55,80,38,46,0,0,Math.PI*2);g.fill();g.strokeStyle='#e7efd345';g.lineWidth=1.5;g.setLineDash([4,5]);g.beginPath();g.ellipse(72,99,47,66,0,0,Math.PI*2);g.stroke();treeCache.set(color,sprite);}ctx.drawImage(sprite,x-size*.6,y-size*1.76,size*1.2,size*2);}
function cart(x,y,s,color,face,tilt=0){ctx.save();ctx.translate(x,y);ctx.rotate(tilt);ellipse(0,s*.24,s*.58,s*.14,'#293d4835');rr(-s*.5,-s*.03,s*.2,s*.32,s*.08,'#344753');rr(s*.3,-s*.03,s*.2,s*.32,s*.08,'#344753');rr(-s*.4,-s*.28,s*.8,s*.49,s*.2,color);rr(-s*.33,-s*.27,s*.66,s*.18,s*.09,'#ffffff32');rr(-s*.38,s*.07,s*.76,s*.14,s*.07,'#f6e8c8');rr(-s*.36,-s*.34,s*.72,s*.12,s*.06,color);if(face?.complete&&face.naturalWidth)ctx.drawImage(face,-s*.38,-s*.99,s*.76,s*.78);else ellipse(0,-s*.6,s*.25,s*.3,color);rr(-s*.2,-s*.03,s*.4,s*.12,s*.06,'#fff5d6');ctx.fillStyle='#33404b';ctx.font='900 '+s*.15+'px sans-serif';ctx.textAlign='center';ctx.fillText(color==='#f0641e'?'04':'01',0,s*.07);ctx.restore();}
function island(x,y,size,kind,pal){
 if(size<7)return;
 ellipse(x,y+size*.1,size*.58,size*.21,'#3f77742a');ellipse(x,y,size*.57,size*.20,'#b7b08e');ellipse(x,y-size*.045,size*.56,size*.17,'#d7d6b2');
 if(kind===0){
  rr(x-size*.13,y-size*.78,size*.26,size*.78,size*.08,'#f5ecd9');rr(x-size*.14,y-size*.47,size*.28,size*.13,size*.04,'#f08a3c');rr(x-size*.16,y-size*.75,size*.32,size*.10,size*.025,'#f08a3c');
  rr(x-size*.09,y-size*.90,size*.18,size*.18,size*.03,'#7db4aa');poly([[x-size*.23,y-size*.9],[x,y-size*1.08],[x+size*.23,y-size*.9]],'#f08a3c');rr(x-size*.035,y-size*.22,size*.07,size*.16,size*.035,'#819b93');
  ellipse(x+size*.32,y-size*.09,size*.14,size*.1,'#95a88b');
 }else if(kind===1){tree(x,y,size*1.05,pal.tree);tree(x+size*.34,y+size*.06,size*.65,'#90ad8b');ellipse(x-size*.29,y-size*.1,size*.14,size*.13,'#a5b480');}
 else{
  rr(x-size*.32,y-size*.46,size*.64,size*.40,size*.16,'#c0a8cc');ellipse(x,y-size*.46,size*.37,size*.13,'#e5d5b9');ellipse(x,y-size*.09,size*.38,size*.14,'#d0bfa1');
  ctx.strokeStyle='#a78bb9';ctx.lineWidth=Math.max(1,size*.035);for(let j=0;j<6;j++){ctx.beginPath();ctx.moveTo(x-size*.23+j*size*.09,y-size*.40);ctx.lineTo(x-size*.23+j*size*.09,y-size*.13);ctx.stroke();}
  rr(x-size*.03,y-size*.86,size*.06,size*.47,size*.03,'#af9578');poly([[x,y-size*.86],[x+size*.48,y-size*.76+Math.sin(tick*2)*size*.05],[x,y-size*.65]],'#f0641e');
 }
}
function drawWorld(dt){drawCount++;
 const pal=palette[Math.min(2,Math.floor(level/3))];const sky=ctx.createLinearGradient(0,0,0,H*.5);sky.addColorStop(0,pal.sky);sky.addColorStop(.7,pal.sky);sky.addColorStop(1,pal.fog);ctx.fillStyle=sky;ctx.fillRect(0,0,W,H);
 // A layered felt diorama: distant islands dissolve in lavender.
 for(let i=0;i<7;i++){const x=(i/6)*W+Math.sin(i*9)*W*.08;ellipse(x,H*.34+Math.sin(i)*8,W*.17,H*.036,pal.fog);}
 ctx.fillStyle=pal.water;ctx.fillRect(0,H*.34,W,H*.66);
 for(let i=0;i<9;i++){const y=H*.4+i*H*.068;rr((Math.sin(i*37+tick*.2)+1)*W*.39,y,W*.09,3,2,'#f8f1df45');}
 // Segment ribbon: rounded scenery, sewn shoulders, ivory road.
 for(let i=1;i<=32;i++){const z=i/32,z0=(i-1)/32,p=project(z),q=project(z0);poly([[q.x-q.w*1.55,q.y],[q.x+q.w*1.55,q.y],[p.x+p.w*1.55,p.y],[p.x-p.w*1.55,p.y]],pal.land);poly([[q.x-q.w*1.07,q.y],[q.x+q.w*1.07,q.y],[p.x+p.w*1.07,p.y],[p.x-p.w*1.07,p.y]],Math.floor(i/3+roadTravel/60)%2?'#c4b694':'#f5edda');poly([[q.x-q.w,q.y],[q.x+q.w,q.y],[p.x+p.w,p.y],[p.x-p.w,p.y]],'#e9e4d1');if(Math.floor(i/4+roadTravel/70)%2){for(const lane of[-.34,.34])poly([[q.x+q.w*lane-1,q.y],[q.x+q.w*lane+1,q.y],[p.x+p.w*lane+2*z,p.y],[p.x+p.w*lane-2*z,p.y]],'#fff8e6');}}
 const scenery=Array.from({length:18},(_,i)=>({i,z:(i/18+roadTravel*.00017)%1})).sort((a,b)=>a.z-b.z);
 for(const {i,z} of scenery){if(z<.1)continue;const p=project(z),side=i%2?-1:1;const x=p.x+side*p.w*(1.35+((i*13)%7)*.13);tree(x,p.y,Math.max(3,W*.15*p.s),i%3===0?'#b6b68d':pal.tree);
 if(i%4===0){const size=W*.23*p.s;island(p.x-side*p.w*1.83,p.y+8*p.s,size,Math.floor(level/3),pal);}
 if(i%3===1){const xx=p.x-side*p.w*1.12,hh=40*p.s+3;rr(xx,p.y-hh,Math.max(2,4*p.s),hh,2,'#ac987b');poly([[xx,p.y-hh],[xx+side*hh*.55,p.y-hh+hh*.12],[xx,p.y-hh+hh*.37]],i%2?'#f0641e':'#128490');}
 }
 // Stitches sit on the track's bound edge; the driving surface stays calm.
 for(let i=0;i<20;i++){const z=(i/20+roadTravel*.00028)%1;if(z<.15)continue;const p=project(z);for(const side of[-1,1]){ctx.strokeStyle='#fff8e5';ctx.lineWidth=1+2*p.s;ctx.beginPath();ctx.moveTo(p.x+side*p.w*1.03-3*p.s,p.y);ctx.lineTo(p.x+side*p.w*1.03+3*p.s,p.y+2*p.s);ctx.stroke();}}

 if(phase==='planning'){
  for(let row=1;row>=0;row--){if(selected[row]===null)continue;const sp=project(row?.39:.52,(selected[row]-1)*.70),sw=35+60*sp.s;rr(sp.x-sw*.5,sp.y-sw*.58,sw,sw*.48,sw*.13,row?'#128490':'#f08a3c');ctx.fillStyle='#fff8e6';ctx.font='900 '+Math.max(10,sw*.19)+'px sans-serif';ctx.textAlign='center';ctx.fillText(op(problem.options[row][selected[row]])+' L',sp.x,sp.y-sw*.26);}
 }
 // Track pennants and the finish arch.
 const gateZ=phase==='driving'?Math.min(.92,.28+elapsed*.10):.46;const gate=project(gateZ),gw=gate.w*1.65,gh=60*gate.s+30;
 rr(gate.x-gw*.5-6,gate.y-gh,8+gate.s*8,gh,4,'#128490');rr(gate.x+gw*.5-6,gate.y-gh,8+gate.s*8,gh,4,'#128490');rr(gate.x-gw*.57,gate.y-gh-14,gw*1.14,24+gate.s*25,8,'#f6f0df');ctx.fillStyle='#0c626d';ctx.font='900 '+Math.max(10,gate.s*26+9)+'px sans-serif';ctx.textAlign='center';ctx.fillText(problem?fmt(problem.target)+' L':'GRAND PRIX',gate.x,gate.y-gh+5+gate.s*10);
 if(phase==='driving'){
  for(let routeIndex=0;routeIndex<2;routeIndex++){
   const arrival=routeIndex?3.8:2;
   if(elapsed>arrival+.35)continue;
   const z=Math.max(.08,Math.min(.92,.83-(arrival-elapsed)*.2));
   const lane=(selected[routeIndex]-1)*.70,fp=project(z,lane),amount=problem.options[routeIndex][selected[routeIndex]],fw=25+70*fp.s;
   rr(fp.x-fw*.53,fp.y-fw*.52,fw*1.06,fw*.48,fw*.15,amount>0?'#f08a3c':'#a38bb7');
   ctx.fillStyle='#fff7e6';ctx.font='900 '+Math.max(10,fw*.22)+'px sans-serif';ctx.fillText(op(amount)+' L',fp.x,fp.y-fw*.21);ctx.font='800 '+Math.max(9,fw*.13)+'px sans-serif';ctx.fillStyle='#233d48';ctx.fillText((routeIndex+1)+' · '+['왼쪽','가운데','오른쪽'][selected[routeIndex]],fp.x,fp.y-fw*.68);
   ellipse(fp.x,fp.y+fw*.04,fw*.35,fw*.075,'#2f55541c');
  }
  if(elapsed>4.6){const open=energy===problem.target;ctx.save();ctx.translate(gate.x-gw*.5,gate.y-gh*.25);ctx.rotate(open?-Math.min(1,(elapsed-4.6))*1.3:0);rr(0,-5,gw,10,4,open?'#128490':'#c0a8cc');for(let j=0;j<8;j++)rr(j*gw/8, -5,gw/16,10,2,'#fff6df');ctx.restore();}
  const rival=project(.63+Math.sin(elapsed)*.025);cart(rival.x-rival.w*.36,rival.y,Math.min(W*.14,105)*.56,'#12309c',images.tak,.08);
  for(let i=0;i<8;i++){if(gotCoins.has(i))continue;const progress=(elapsed/6.4+i*.16)%1;const z=.18+progress*.72;if(progress>.9)continue;const lane=Math.sin(i*2.6)*.72,p=project(z,lane);const size=6+22*p.s;ctx.save();ctx.translate(p.x,p.y-7);ctx.rotate(Math.PI*.25+tick*.5);rr(-size*.5,-size*.5,size,size,size*.26,'#e9b741');rr(-size*.32,-size*.32,size*.25,size*.25,size*.07,'#fff2b7');ctx.restore();if(z>.79&&z<.9&&Math.abs(playerX-lane)<.24){gotCoins.add(i);coins++;score+=10;burst(p.x,p.y,'#f0c054',8);playTone(900+i*60,.07);updateHUD();}}
 }
 const carY=phase==='driving'?H*.77:H*.59;const carZ=Math.sqrt((carY-H*.29)/(H*.71)),p=project(carZ);const carX=p.x+playerX*p.w*.85;
 if(phase!=='title'){
  if(phase==='driving'&&Math.abs(steer)>.1){for(let i=0;i<3;i++)ellipse(carX-steer*15+Math.sin(tick*20+i)*8,carY+22+i*7,10+i*3,5+i*2,'#faf3df88');}
  cart(carX,carY,Math.min(W*.19,128),'#f0641e',images.jaei,steer*.10+Math.sin(tick*4)*.008);
 }
 for(const p of particles){p.x+=p.vx*dt;p.y+=p.vy*dt;p.vy+=240*dt;p.life-=dt;ctx.globalAlpha=Math.max(0,Math.min(1,p.life*2));rr(p.x,p.y,p.size,p.size,2,p.color);}ctx.globalAlpha=1;particles=particles.filter(p=>p.life>0);
}
function frame(now){const dt=Math.min(.04,(now-last)/1000||.016);last=now;if(!captureStill){tick+=dt;if(phase==='driving'){
 elapsed+=dt;roadTravel+=dt*540;if(tick-lastSound>.32){lastSound=tick;playTone([262,330,392,440,392,330,294,392][Math.floor(elapsed/.32)%8],.12,'triangle',.012);}const guideIndex=elapsed>=1.1&&elapsed<2.25?0:elapsed>=2.9&&elapsed<4.05?1:-1;
 if(guideIndex!==autoFork){autoFork=guideIndex;steer=0;$('#left').disabled=autoFork>=0;$('#right').disabled=autoFork>=0;$('#driveControls p').textContent=autoFork>=0?(autoFork+1)+'번째 · 고른 '+['왼쪽','가운데','오른쪽'][selected[autoFork]]+' 길로 달려요':'좌우로 움직여 실뭉치를 모아요!';}
 if(autoFork>=0)playerX+=((selected[autoFork]-1)*.75-playerX)*Math.min(1,dt*8);else playerX=Math.max(-.95,Math.min(.95,playerX+steer*dt*1.65));const speedNow=Math.round(80+Math.sin(elapsed*3)*5);if(speedNow!==lastSpeed){lastSpeed=speedNow;$('#speed').textContent=speedNow+' km/h';}
 if(elapsed>1&&elapsed<1.2)$('#banner').textContent='';
 const thresholds=[2,3.8];for(let i=0;i<2;i++){if(elapsed>=thresholds[i]&&operationsApplied===i){energy+=problem.options[i][selected[i]];operationsApplied++;$('#banner').textContent=op(problem.options[i][selected[i]])+' L';playTone(430+i*100,.16);updateHUD();}}
 if(elapsed>4.8)$('#banner').textContent=energy===problem.target?'딱 맞아요!':'다리에서 확인해요';
 if(elapsed>=6.4)resolveRace();
 }else if(phase==='title')roadTravel+=dt*35;}
 if(phase==='driving'||particles.length||needsDraw||lastDrawPhase!==phase){drawWorld(dt);needsDraw=false;lastDrawPhase=phase;}requestAnimationFrame(frame);
}
requestAnimationFrame(frame);
window.__GAME_TEST__={ready:true,renderStats(){return {drawCount,elapsed,phase};},getLayout(){return{land:W>=800,playW:W,playH:H,kind:"panoramic-pseudo3d"};},start:startGame,getState(){return {score,lives,level:Math.min(9,level+1),phase:phase==='planning'?'playing':phase,solved,coins,energy,target:problem?.target,selected:[...selected],errors,playerX,autoFork};},answerCorrect(){if(['title','won','lost'].includes(phase))startGame();show('#dialog',false);selected=[...problem.solution];attemptSum=routeSum(problem,selected);resolveRace(true);},answerWrong(){if(['title','won','lost'].includes(phase))startGame();show('#dialog',false);selected=[(problem.solution[0]+1)%3,problem.solution[1]];attemptSum=routeSum(problem,selected);resolveRace(true);},sampleProblems(n){let local=0x45679;const rand=()=>{local=(Math.imul(local,1664525)+1013904223)>>>0;return local/4294967296;};return Array.from({length:n},(_,i)=>{const p=generate(i,rand),choices=[];for(let a=0;a<3;a++)for(let b=0;b<3;b++)choices.push(op(p.options[0][a])+' → '+op(p.options[1][b]));return {id:'route-'+i,prompt:'출발 연료 '+fmt(p.start)+' L. 첫 길 '+p.options[0].map(op).join(' 또는 ')+', 둘째 길 '+p.options[1].map(op).join(' 또는 ')+'. 도착 연료를 '+fmt(p.target)+' L로 만드는 두 길을 골라 보세요.',choices,answer:op(p.options[0][p.solution[0]])+' → '+op(p.options[1][p.solution[1]]),answerNumeric:p.target/100,unitConcept:'소수 두 자리 수의 덧셈과 뺄셈',model:p};});},solutionHint(){if(!problem)return null;return {options:problem.options.map(v=>[...v]),start:problem.start,target:problem.target,route:[...problem.solution],selectors:problem.solution.map((side,row)=>'[data-row="'+row+'"][data-side="'+side+'"]'),commit:'#go',phase,units:'integer hundredths'};}};
})();
