// Textbook expression traps: ÷ (never /); quotient zero keeps its place;
// remainder must satisfy 0 <= r < divisor. Check with d×q+r=N, never omit r.
// Workbook voice: ~구해 보세요. Only lessons 4, 6, 7; fixed 60÷3 warm-up.
export const shuffle=(a,rng=Math.random)=>{a=[...a];for(let i=a.length-1;i>0;i--){let j=Math.floor(rng()*(i+1));[a[i],a[j]]=[a[j],a[i]];}return a;};
export function problem(N,d,kind='procedural'){const a=Math.floor(N/10),b=N%10,qT=Math.floor(a/d),rT=a%d,p=rT*10+b,qO=Math.floor(p/d),r=p%d;return {id:`${N}d${d}`,N,d,a,b,qT,rT,p,qO,q:qT*10+qO,r,band:r?2:1,kind};}
// Build from quotient digits and remainders, so carry and Euclidean identities
// hold structurally. Bounds below enumerate a finite constraint-satisfying pool.
export const POOL=[];
for(let d=2;d<=9;d++)for(let qt=0;qt*d<10;qt++)for(let rt=1;rt<d && qt*d+rt<10;rt++){
 const a=d*qt+rt;
 for(let qo=0;qo<=9;qo++)for(let r=0;r<d;r++){
  const b=d*qo+r-10*rt;
  if(b>=0 && b<=9 && a>0)POOL.push(problem(a*10+b,d));
 }
}
export const answerText=(q,r)=>`${q} ⋯ ${r}`;
const promptFor=(p,kind)=>{
 if(kind==='representation')return `그림을 보고, ${p.N}개를 ${p.d}줄에 똑같이 나누어 담습니다. 한 줄에 몇 개씩 담고, 남는 구슬은 몇 개인지 구해 보세요.`;
 if(kind==='real_world')return `병뚜껑 ${p.N}개를 상자 ${p.d}개에 똑같이 나누어 담습니다. 한 상자에 몇 개씩 담고, 남는 뚜껑은 몇 개인지 구해 보세요.`;
 if(kind==='check')return `${p.N} ÷ ${p.d}의 값을 계산하고, ${p.d} × 몫 + 나머지 = ${p.N}이 되도록 쟁반을 만들어 확인해 보세요.`;
 return `${p.N} ÷ ${p.d}의 몫과 나머지를 각각 구해 보세요.`;
};
export function sample(p,rng=Math.random,kind=p.kind||'procedural'){
 const answer=answerText(p.q,p.r);
 let wrong=[
  {q:p.qT*10+Math.floor(p.b/p.d),r:p.b%p.d,misconceptionId:'forgotCarry'},
  {q:p.q-1,r:p.r+p.d,misconceptionId:'remainderTooLarge'},
  ...(p.r?[{q:p.q,r:0,misconceptionId:'checkSkipRemainder'}]:[]),
  ...(p.qO===0?[{q:p.qT,r:p.r,misconceptionId:'omittedZeroPlace'}]:[])
 ];
 // Required order: remove answer -> range -> deduplicate -> supplement -> shuffle.
 wrong=wrong.filter(x=>answerText(x.q,x.r)!==answer).filter(x=>x.q>=0&&x.q<=99&&x.r>=0&&x.r<=18);
 wrong=wrong.filter((x,i,a)=>a.findIndex(y=>y.q===x.q&&y.r===x.r)===i);
 for(let k=1;wrong.length<3;k++){const x={q:p.q+k,r:p.r,misconceptionId:'oneGroupTooMany'};if(!wrong.some(y=>y.q===x.q&&y.r===x.r))wrong.push(x);}
 const distractors=wrong.slice(0,3).map(x=>({...x,value:answerText(x.q,x.r)}));
 return {...p,problemType:kind,prompt:promptFor(p,kind),choices:shuffle([answer,...distractors.map(x=>x.value)],rng),answer,answerNumeric:p.q,unitConcept:kind==='check'?'곱셈으로 나눗셈 확인하기':'내림이 있는 나눗셈의 몫과 나머지',distractors};
}
export class Game {
 constructor(rng=Math.random){this.rng=rng;this.start();}
 start(){Object.assign(this,{score:0,lives:3,level:1,phase:'playing',solved:0,board:Array.from({length:10},()=>Array(10).fill(0)),step:'tens',q:0,digits:[],time:90,frozen:true,piece:null,waste:false,combo:0,cleared:[],event:0,last:'',outcome:'idle',hold:0,resumeStep:null});
  const digits=shuffle([0,1,2,3,4],this.rng),kinds=['procedural','representation','procedural','real_world','procedural','check'];this.deck=[problem(60,3,kinds[0])];
  for(let i=1;i<6;i++){const band=i<2?1:i<4?2:i%2+1;const pool=POOL.filter(p=>p.band===band&&p.qT===digits[i-1]&&!this.deck.some(x=>x.id===p.id));this.deck.push({...shuffle(pool,this.rng)[0],kind:kinds[i]});}
  this.p=this.deck[0];this.last='손잡이를 끌어 3줄에 똑같이 채워 보세요.';
 }
 tick(dt){if(this.phase!=='playing')return;if(this.hold>0){this.hold=Math.max(0,this.hold-dt);return;}if(!this.frozen){this.time=Math.max(0,this.time-dt);if(!this.time)this.finish(false,'시간이 끝났어요');}}
 currentN(){return this.step==='tens'?this.p.a:this.p.p;}
 guess(q){if(this.phase!=='playing'||!['tens','ones'].includes(this.step)||this.hold>0)return false;
  this.q=Math.max(0,Math.min(9,Math.round(q)));const n=this.currentN(),r=n-this.p.d*this.q;
  if(r<0||r>=this.p.d){this.wrong(r<0?'구슬이 모자라요':'한 묶음씩 더 채울 수 있어요');return false;}
  this.outcome='correct';this.event++;this.score+=10;this.last=`${n}${this.step==='tens'?'십':''} = ${this.p.d} × ${this.q}${this.step==='tens'?'십':''} + ${r}${this.step==='tens'?'십':''}`;
  if(this.step==='tens'){this.digits=[this.q];this.frozen=false;this.step='carry';}
  else{this.digits.push(this.q);this.piece=[this.digits[0],this.digits[1],r];this.step='place';this.last=`${this.p.d} × ${this.p.q} + ${r} = ${this.p.N}`;this.ensureSpace();}
  this.hold=1;return true;
 }
 carry(){if(this.phase!=='playing'||this.step!=='carry'||this.hold>0)return false;this.step='ones';this.q=this.p.qO===0?1:0;this.event++;this.last=`${this.p.rT}십을 내려 ${this.p.rT*10} + ${this.p.b} = ${this.p.p}개`;return true;}
 wrong(reason='쟁반을 다시 맞춰 보세요',force=false){if(this.phase!=='playing')return;const n=this.currentN(),q=Math.floor(n/this.p.d),r=n%this.p.d;this.outcome='wrong';this.event++;this.last=`${reason} · ${n} = ${this.p.d} × ${q} + ${r}`;this.hold=.85;
  if(this.frozen&&!force)return;
  this.frozen=false;this.lives--;this.combo=0;
  if(this.lives<=0){this.finish(false,'핀 3개가 모두 부러졌어요');return;}
  this.resumeStep=['tens','ones'].includes(this.step)?this.step:'tens';this.step='place';this.waste=true;this.piece=[2,2];this.ensureSpace();
 }
 cells(piece=this.piece){return (piece||[]).flatMap((len,y)=>Array.from({length:len},(_,x)=>[x,y]));}
 canPlace(x,y){return this.cells().length>0&&this.cells().every(([dx,dy])=>x+dx>=0&&x+dx<10&&y+dy>=0&&y+dy<10&&!this.board[y+dy][x+dx]);}
 positions(){const a=[];for(let y=0;y<10;y++)for(let x=0;x<10;x++)if(this.canPlace(x,y))a.push([x,y]);return a;}
 ensureSpace(){if(!this.positions().length)this.finish(false,'조각을 놓을 빈자리가 없어요');}
 place(x,y){if(this.phase!=='playing'||this.step!=='place'||this.hold>0)return false;if(!this.canPlace(x,y)){this.last='빈칸 안에 조각 전체를 놓아 보세요.';this.event++;return false;}
  for(const [dx,dy]of this.cells())this.board[y+dy][x+dx]=this.waste?2:1+this.solved%2;
  const rows=[],cols=[];
  for(let row=0;row<10;row++)if(this.board[row].every(Boolean))rows.push(row);
  for(let col=0;col<10;col++)if(this.board.every(row=>row[col]))cols.push(col);
  this.cleared=[...rows.map(index=>({axis:'row',index})),...cols.map(index=>({axis:'col',index}))];
  const count=this.cleared.length;if(count){this.combo=count>1?this.combo+1:1;this.score+=100*count*(count>1?2:1);if(count>1)this.lives=Math.min(3,this.lives+1);for(const row of rows)this.board[row].fill(0);for(const col of cols)for(let row=0;row<10;row++)this.board[row][col]=0;this.hold=count>1?.77:.6;}
  this.outcome='placed';this.event++;
  if(this.waste){this.waste=false;this.piece=null;this.step=this.resumeStep;this.last='폐기 조각을 놓았어요. 같은 자리를 다시 계산해 보세요.';return true;}
  this.score+=30;this.solved++;this.piece=null;
  if(this.solved===6){this.finish(true,`${this.p.d} × ${this.p.q} + ${this.p.r} = ${this.p.N}`);return true;}
  this.level=this.solved<2?1:this.solved<4?2:3;this.p=this.deck[this.solved];this.step='tens';this.q=this.p.qT===0?1:0;this.digits=[];this.last=count>1?'두 줄 압착! 핀 하나를 고쳤어요.':'다음 주문의 쟁반을 늘려 보세요.';return true;
 }
 finish(win,reason){this.phase=win?'clear':'gameover';this.last=reason;this.frozen=false;this.event++;}
 state(){return {score:this.score,lives:this.lives,level:this.level,phase:this.phase,solved:this.solved,step:this.step,time:this.time,frozen:this.frozen,board:this.board.map(r=>[...r]),pendingPiece:this.piece,problem:this.p.id,selection:this.q,event:this.event};}
}
