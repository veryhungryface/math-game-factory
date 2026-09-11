/* 창 닦기 — geometry and state contract, no DOM and no automatic alignment.
 * Workbook register: ~해 보세요. 수선 names its reference 직선 ㄱㄴ.
 * Expression traps copied from textbook-structure: 사다리꼴 has at least one
 * parallel pair; square belongs to all five families (display only, lessons 1–4
 * assess no exclusive classification). 평행선 사이 거리는 수선의 길이.
 * Use ㄱㄴㄷㄹ/ㅍ labels; integer cm with a space before cm. No rounding,
 * fraction, decimal, range or pi questions are generated in this lesson scope.
 * Numeric facts d/target are integers; floating vectors are measured geometry,
 * compared with an explicit angular/endpoint tolerance, never numeric equality.
 */
(function (root) {
  'use strict';
  const C = Object.freeze({width:320,height:420,unit:26,endpointTolerance:24,spanTolerance:14,angleTolerance:4,straightTolerance:10,stainStartRadius:24,timeLimit:90,target:8});
  const DEG=Math.PI/180, SIN4=Math.sin(4*DEG), EPS=1e-9;
  // Uniform 0.1° midpoint lattice. The measured bots' integer-degree
  // directions accept 80/1800 orientations = 4.444…%. An arbitrary direction
  // aligned to both inclusive endpoints may accept 81/1800 = 4.5%; do not
  // mistake the selected policies' baseline for a universal upper bound.
  const ORIENTATIONS=Array.from({length:1800},(_,i)=>(i+.5)/10);
  // Uniform source positions cover the entire usable gasket. Broad uniform
  // coverage prevents a fixed fraction of the opposite gasket from exploiting
  // a concentrated source prior. Midpoints avoid an exact centered source,
  // while near-center sources remain legitimate (no arbitrary dead band).
  // 닫힌 창(사다리꼴) 전용 손잡이 격자. 예전 offMid 는 |o|<18 을 ±18 로 접어, 한가운데가 +20 인
  // 윗변에서 「맞은편 변 한가운데로 곧장」이 2px 차로 수선이 됐다(4차 검수: 7장 d=4 25.76 %).
  // 이제 윗변[−70,110] 위에서 · 한가운데(+20)와 18px 이상 · 뒤집은 비율 자리(+10.5)와 17px 이상 ·
  // 꼭짓점(−70)과 16px 이상 · 창 한가운데를 지나는 선과 27px 이상 떨어진 정수만 쓴다.
  // d=4·d=6 이 같은 격자를 써서 손잡이 자리로 거리 주문의 답을 알 수 없다.
  const TRAP_OFFSETS=Object.freeze([...Array.from({length:28},(_,k)=>-54+k),...Array.from({length:19},(_,k)=>38+k)]);
  function sourceOffset(i,count,phase=0){return (i*73+Math.floor(i/count)*37+phase)%count-(count-1)/2;}
  // These cues state only geometry that is already visible: the labelled
  // direction of the first gasket and where the point/tool starts. They never
  // expose d, delta, the perpendicular angle, or whether a pane must be
  // discarded. Unlike a serial-number suffix, each sentence therefore helps a
  // learner locate and read the actual figure used by the live judge.
  const DIRECTION_CUES=Object.freeze([
    'ㄱ에서 ㄴ으로 보면 거의 오른쪽으로 뻗어요.',
    'ㄱ에서 ㄴ으로 보면 오른쪽 아래로 조금 기울었어요.',
    'ㄱ에서 ㄴ으로 보면 오른쪽 아래로 많이 기울었어요.',
    'ㄱ에서 ㄴ으로 보면 거의 아래로 뻗으며 오른쪽으로 조금 기울었어요.',
    'ㄱ에서 ㄴ으로 보면 거의 아래로 뻗으며 왼쪽으로 조금 기울었어요.',
    'ㄱ에서 ㄴ으로 보면 왼쪽 아래로 많이 기울었어요.',
    'ㄱ에서 ㄴ으로 보면 왼쪽 아래로 조금 기울었어요.',
    'ㄱ에서 ㄴ으로 보면 거의 왼쪽으로 뻗어요.'
  ]);
  const POINT_CUES=Object.freeze([
    '점 ㅍ은 ㄱ 가까이에 있어요.',
    '점 ㅍ은 ㄱ과 가운데 사이에 있어요.',
    '점 ㅍ은 직선 가운데 맞은편에 있어요.',
    '점 ㅍ은 가운데와 ㄴ 사이에 있어요.',
    '점 ㅍ은 ㄴ 가까이에 있어요.'
  ]);
  const TOOL_CUES=Object.freeze([
    '밀대는 ㄷ 가까이에 있어요.',
    '밀대는 ㄷ과 가운데 사이에 있어요.',
    '밀대는 직선 ㄷㄹ의 가운데에 있어요.',
    '밀대는 가운데와 ㄹ 사이에 있어요.',
    '밀대는 ㄹ 가까이에 있어요.'
  ]);
  const STAIN_CUES=Object.freeze([
    '서연의 자국은 ㄷ 가까이에서 시작해요.',
    '서연의 자국은 ㄷ과 가운데 사이에서 시작해요.',
    '서연의 자국은 직선 ㄷㄹ의 가운데에서 시작해요.',
    '서연의 자국은 가운데와 ㄹ 사이에서 시작해요.',
    '서연의 자국은 ㄹ 가까이에서 시작해요.'
  ]);
  // 고무를 창틀 밖까지 늘린 뒤로 「ㄱ 가까이」 같은 선분 기준 단서는 그림과 어긋났다
  // (보이는 끝이 창틀 모서리이기 때문이다). 그래서 단서를 **손잡이의 실제 화면 좌표**에서
  // 만든다 — 학습자가 보는 것과 같은 기준이다.
  function sceneCue(mode,theta,d,offset,delta,shape){
    const id=[mode,theta,d,offset,delta,shape].join('-');
    const ph=hashString(id+'|placement');
    const center=mode==='tutorial'?{x:160,y:210}
      :{x:160+[-6,0,6][ph%3],y:210+[-10,0,10][Math.floor(ph/3)%3]};
    const u=unit(theta),n={x:-u.y,y:u.x},gap=d*C.unit;
    const lower=add(center,mul(n,gap/2));
    const h=add(lower,mul(unit(theta+delta),offset));
    const hx=h.x-160,hy=h.y-210;
    const lr=hx<-72?'왼쪽 끝':hx<-26?'왼쪽':hx<=26?'가운데':hx<=72?'오른쪽':'오른쪽 끝';
    const tb=hy<-66?'위쪽':hy<-22?'조금 위':hy<=22?'':hy<=66?'조금 아래':'아래쪽';
    const where=tb?(lr==='가운데'?tb:lr+' '+tb):lr;
    const who=mode==='point'?'점 ㅍ은':(mode==='error'||mode==='mixed')?'서연의 자국은':'밀대는';
    const tail=(mode==='error'||mode==='mixed')?'에서 시작해요.':'에 있어요.';
    return who+' 창의 '+where+tail;
  }
  function promptParts(mode,theta,d,offset,delta,shape){
    const directionCue=DIRECTION_CUES[Math.min(7,Math.floor((((theta%180)+180)%180)/22.5))];
    const sourceCue=sceneCue(mode,theta,d,offset,delta,shape);
    // The two stain panes deliberately share one neutral instruction. Previously
    // `error` always said "draw a perpendicular" while nonparallel `mixed` alone
    // said "wipe or sort"; reading the sentence, rather than the two gaskets,
    // therefore revealed the answer with certainty.
    const task=mode==='point'?'점 ㅍ에서 직선 ㄱㄴ에 대한 수선을 그어 보세요.':mode==='target'?'평행한 두 고무 사이의 거리가 4 cm인 창만 닦아 보세요.':mode==='error'||mode==='mixed'?'서연의 비스듬한 자국에 속지 말고, 직선 ㄱㄴ과 직선 ㄷㄹ이 평행인지 보고 닦거나 옆으로 쓸어 내 보세요.':shape==='square'?'기울어진 마름모에서 직선 ㄱㄴ에 대한 수선을 그어 보세요.':'직선 ㄱㄴ에 대한 수선을 그어 두 평행선 사이의 거리를 알아보세요.';
    return {directionCue,sourceCue,task,prompt:directionCue+' '+sourceCue+' '+task};
  }
  const add=(a,b)=>({x:a.x+b.x,y:a.y+b.y}), sub=(a,b)=>({x:a.x-b.x,y:a.y-b.y});
  const mul=(v,k)=>({x:v.x*k,y:v.y*k}), dot=(a,b)=>a.x*b.x+a.y*b.y;
  const length=v=>Math.hypot(v.x,v.y), distance=(a,b)=>length(sub(a,b));
  const unit=theta=>({x:Math.cos(theta*DEG),y:Math.sin(theta*DEG)});
  function rng(seed){let a=(Number(seed)||1)>>>0;return()=>{a+=0x6D2B79F5;let t=a;t=Math.imul(t^t>>>15,t|1);t^=t+Math.imul(t^t>>>7,t|61);return((t^t>>>14)>>>0)/4294967296;};}
  function shuffle(a,random){const b=a.slice();for(let i=b.length-1;i>0;i--){const j=Math.floor(random()*(i+1));[b[i],b[j]]=[b[j],b[i]];}return b;}
  function segmentDistance(p,a,b){const ab=sub(b,a),d=dot(ab,ab);const t=d?Math.max(0,Math.min(1,dot(sub(p,a),ab)/d)):0;return distance(p,add(a,mul(ab,t)));}
  function finitePoint(p){return p&&Number.isFinite(p.x)&&Number.isFinite(p.y);}
  function makeProblem(mode,theta,d,offset,delta=0,shape='open'){
    const target=mode==='target'?4:null;
    const requiresDiscard=delta!==0||(target!==null&&d!==target);
    const words=promptParts(mode,theta,d,offset,delta,shape);
    return Object.freeze({id:[mode,theta,d,offset,delta,shape].join('-'),mode,theta,d,offset,delta,shape,target,requiresDiscard,...words,unitConcept:mode==='point'?'수직과 수선':delta?'평행과 평행선':shape==='square'?'마름모에서 평행선 사이의 수선':shape!=='open'?'사다리꼴과 평행선 사이의 거리':'평행선 사이의 거리',level:mode==='point'?1:mode==='pair'?2:mode==='error'||mode==='mixed'?3:4});
  }
  const tutorial=makeProblem('tutorial',0,4,0,0,'open');
  function geometry(p){
    // Source positions are uniform across the full gasket. Independent small
    // scene shifts prevent a fixed screen center from identifying the pane's
    // geometry. Near-center sources are kept; chance alignment is legitimate.
    // The tutorial alone is exactly centered for its first-action demonstration.
    const placementHash=hashString(p.id+'|placement');
    const center=p.mode==='tutorial'?{x:160,y:210}:{x:160+[-6,0,6][placementHash%3],y:210+[-10,0,10][Math.floor(placementHash/3)%3]};
    const u=unit(p.theta),n={x:-u.y,y:u.x},gap=p.d*C.unit;
    const upper=add(center,mul(n,-gap/2)),lower=add(center,mul(n,gap/2));
    // 고무를 창틀보다 길게 뻗어 두고 창틀이 잘라 내게 한다.
    // 짧은 선분이면 양 끝이 보여서 「내 고무 한가운데 → 맞은편 고무 한가운데」로만 밀어도
    // 수선이 됐다(2026-09-11 2차 검수 재현: 첫 시도 정답률 56.5 % / 우연 4.44 %).
    // 보이는 끝이 창틀 모서리가 되면 두 고무의 '보이는 한가운데'는 서로 수선 짝이 아니다.
    //
    // 닫힌 창(사다리꼴·마름모)은 고무를 자를 수 없다(도형이 열린다). 대신 **도형을 기울여**
    // 「같은 자리로 곧장 건너기」가 수선이 되지 않게 한다(3차 검수: 사다리꼴 35 % · 정사각형 100 %).
    //  · 사다리꼴: 윗변을 축 방향으로 20px 밀어 좌우 비대칭으로 만든다(평행 한 쌍 유지).
    //  · 마름모: 네 변이 같고 두 쌍이 평행하되 직각이 아니라, t↔t 는 옆변 방향이지 수선이 아니다.
    //    (정사각형 창을 마름모로 바꾸면서 이름표는 사다리꼴·평행사변형·마름모 셋이 된다.)
    // 열린 창도 두 고무의 선분 구간을 서로 어긋나게 둔다. 구간이 정확히 평행이동이면
    // 「같은 상대 위치(t)로 곧장 건너기」가 그대로 수선이 된다(3차 검수 B 정책).
    // 어긋남을 손잡이 범위(±98.5)보다 크게 둔다. 그래야 「맞은편 고무 선분의 한가운데」
    // 를 겨냥하는 정책도 어떤 손잡이 위치에서든 최소 21° 어긋난다(3차 검수 재감사 6.69 %).
    let topFrom=-250,topTo=570,botFrom=-250,botTo=250,vTopFrom=-250,vTopTo=570;
    if(p.shape==='trapezoid'){
      topFrom=-70;topTo=110;botFrom=-99;botTo=99;vTopFrom=topFrom;vTopTo=topTo;
    }
    if(p.shape==='square'){
      // 한 변 = gap/sin60°, 윗변 밀림 = 변×cos60° = gap/√3. 손잡이 오프셋 o 의 수선의 발은 윗변의
      // 매개 o 에 떨어지므로, 윗변 [lean−half, lean+half] 안에 o 가 들어와야 한다(4차 검수: o=−22·−8
      // 이면 발이 선분 밖이라 ±4° 안의 정답 획이 end-offside 로 거부됐다). 오프셋이 음수인 창은
      // 윗변을 반대로 민 거울상 마름모라 발이 [−2half, 0] 에 떨어진다.
      const half=gap/Math.sqrt(3),lean=(p.offset<0?-1:1)*gap/Math.sqrt(3);
      topFrom=lean-half;topTo=lean+half;botFrom=-half;botTo=half;
      vTopFrom=topFrom;vTopTo=topTo;
    }
    const u2=unit(p.theta+p.delta);
    const line0={a:add(upper,mul(u,topFrom)),b:add(upper,mul(u,topTo)),u};
    const line1={a:add(lower,mul(u2,botFrom)),b:add(lower,mul(u2,botTo)),u:u2};
    const handle=add(lower,mul(u2,p.offset)),foot=add(handle,mul(n,-dot(sub(handle,upper),n)));
    // The wrong trace leans toward the pane interior, so even the most offset
    // tool still leaves a genuinely gasket-to-gasket oblique misconception.
    const slant=add(foot,mul(u,(p.offset>0?-1:1)*gap*Math.tan(25*DEG)));
    return {center,unit:C.unit,u,n,gap,lines:[line0,line1],handle,foot,vertices:[add(upper,mul(u,vTopFrom)),add(upper,mul(u,vTopTo)),line1.b,line1.a],wrongStroke:{type:'stroke',a:handle,b:slant,points:[handle,slant]},bounds:{x:12,y:42,width:296,height:336}};
  }
  function correctAction(p){if(p.requiresDiscard)return {type:'discard'};const g=geometry(p);return {type:'stroke',a:{...g.handle},b:{...g.foot},points:[{...g.handle},{...g.foot}]};}
  function judge(p,a){
    if(!a||typeof a.type!=='string')return {ok:false,reason:'invalid',message:'밀대를 잡고 맞은편 고무까지 밀어 보세요.'};
    // UI-only practice misses still enter the engine so the frozen tutorial can
    // release after three meaningful attempts. They never enter a scored pane.
    if(a.type==='invalid')return {ok:false,reason:'invalid',message:typeof a.uiMessage==='string'&&a.uiMessage?a.uiMessage:'밀대를 잡고 맞은편 고무까지 밀어 보세요.'};
    if(a.type==='discard')return p.requiresDiscard?{ok:true,reason:'sorted',message:p.delta?'평행이 아니어서 두 곳에 모두 직각이 될 수 없어요.':'이 창은 '+p.d+' cm예요. 4 cm 창을 기다려요.'}:{ok:false,reason:'discard-valid',message:'이 창은 닦을 수 있어요. 밀대로 먼저 닦아 보세요.'};
    if(a.type!=='stroke'||!finitePoint(a.a)||!finitePoint(a.b))return {ok:false,reason:'invalid',message:'밀대를 잡고 맞은편 고무까지 밀어 보세요.'};
    const g=geometry(p),v=sub(a.b,a.a),len=length(v);
    if(len<25)return {ok:false,reason:'short',message:'맞은편 고무까지 밀어 보세요.'};
    if(p.mode==='point'&&distance(a.a,g.handle)>C.endpointTolerance+EPS)return {ok:false,reason:'start-point',message:'물방울 점 ㅍ에서 출발해 보세요.'};
    if(p.mode!=='point'&&segmentDistance(a.a,g.lines[1].a,g.lines[1].b)>C.endpointTolerance+EPS)return {ok:false,reason:'start-gasket',message:'밀대를 시작 고무 위에서 출발해 보세요.'};
    // Landing off the far gasket has three physically different causes and the
    // correction is opposite in two of them. Telling a learner who pushed PAST
    // the gasket to "push all the way to it" sends them further from the answer,
    // so the signed normal position decides the wording. The tolerance itself is
    // unchanged: 평행선 사이의 거리 is the perpendicular segment BETWEEN the two
    // gaskets, so a stroke that does not end on the far one has not measured it.
    if(segmentDistance(a.b,g.lines[0].a,g.lines[0].b)>C.endpointTolerance+EPS){
      const side=dot(sub(a.a,g.lines[0].a),g.n)>=0?1:-1,endN=dot(sub(a.b,g.lines[0].a),g.n)*side;
      if(endN<-C.endpointTolerance)return {ok:false,reason:'end-overshoot',message:'고무를 지나쳤어요. 맞은편 고무 위에서 멈춰 보세요.'};
      if(endN>C.endpointTolerance)return {ok:false,reason:'end-gasket',message:'맞은편 고무까지 밀어 보세요.'};
      return {ok:false,reason:'end-offside',message:'고무 밖으로 나갔어요. 맞은편 고무 위에서 멈춰 보세요.'};
    }
    if((a.points||[]).some(q=>!finitePoint(q)||segmentDistance(q,a.a,a.b)>C.straightTolerance+EPS))return {ok:false,reason:'curved',message:'수선은 곧은 선이에요. 한 번에 곧게 밀어 보세요.'};
    const errors=g.lines.map(l=>Math.abs(dot(l.u,v))/len);
    // δ is structurally 12° or 15°; both ±4° cones are disjoint. Do not
    // generate δ=8°: a bisector would pass at the exact shared boundary.
    if(errors[0]>SIN4+EPS||(p.mode!=='point'&&errors[1]>SIN4+EPS)){
      // 손 뗀 뒤 실제로 잰 각을 보여 준다(엄지 오차 94°와 개념 오답 60°를 아이가 구별하게).
      // 두 직선이 만나는 각 중 90° 이하 쪽을 내림한다 — 허용 4° 를 넘었으면 늘 「직각에서 5° 이상」이다.
      const gi=errors[0]>SIN4+EPS?0:1,measuredAngle=Math.floor(Math.acos(Math.min(1,errors[gi]))/DEG+EPS),off=90-measuredAngle;
      const angleText=measuredAngle+'° — 직각에서 '+off+'° 어긋났어요.';
      return {ok:false,reason:p.delta?'nonparallel':'oblique',misconceptionId:p.delta?'visible-noncrossing-is-parallel':'oblique-distance',measuredAngle,angleOff:off,angleLine:gi,angleText,message:angleText+' '+(p.delta?'두 고무가 평행이 아니에요. 창을 옆으로 쓸어 내 보세요.':p.mode==='point'?'직선 ㄱㄴ과 직각으로 만나도록 밀어 보세요.':'비스듬한 길이는 두 고무 사이의 거리가 아니에요.')};
    }
    if(p.requiresDiscard)return {ok:false,reason:'distance-order',misconceptionId:'ignore-distance-order',message:p.d+' cm예요. 이번 주문은 4 cm인 창이에요.'};
    // 평행선 사이의 거리는 두 고무를 잇는 수선 「전체」의 길이다. 양 끝을 각각
    // 24px 로만 재면 두 오차가 더해져 사이에 그은 짧은 토막이 통과한다 — 4 cm
    // 창을 2.23 cm 획으로, 3 cm 창을 1.15 cm 획으로 정답 처리한 실측 사례가 있다.
    // 그래서 획이 실제로 건넌 폭(법선 성분)을 gap 과 직접 비교한다. 못 닿은 쪽은
    // 거리를 잰 것이 아니므로 좁게(14px), 지나친 쪽은 이미 건넜으므로 기존
    // 끝점 허용오차(24px)를 그대로 쓴다. 고무를 따라 옆으로 어긋난 것은
    // 여기서 재지 않는다 — 그건 위의 segmentDistance 검사가 맡는다.
    const span=Math.abs(dot(sub(a.b,a.a),g.n));
    if(g.gap-span>C.spanTolerance+EPS)return {ok:false,reason:'end-gasket',misconceptionId:'stub-is-distance',message:'맞은편 고무까지 이어야 두 고무 사이의 거리예요.'};
    if(span-g.gap>C.endpointTolerance+EPS)return {ok:false,reason:'end-overshoot',message:'고무를 지나쳤어요. 맞은편 고무 위에서 멈춰 보세요.'};
    return {ok:true,reason:'perpendicular',distanceCm:p.d,message:'직선 ㄱㄴ에 대한 수선 · '+p.d+' cm'};
  }
  function strokeDirection(p,angle,len){const g=geometry(p),a=g.handle,b=add(a,mul(unit(angle),len||p.d*C.unit));return {type:'stroke',a:{...a},b,points:[{...a},b]};}
  function wrongActions(p){
    const g=geometry(p);
    const candidates=[
      {applicable:Math.abs(g.u.y)>SIN4+EPS,misconceptionId:'screen-vertical-is-perpendicular',action:strokeDirection(p,-90),explanation:'화면의 세로 방향만 따라 밀면 기울어진 고무와 직각이 아니에요.'},
      {misconceptionId:'oblique-distance',action:g.wrongStroke,explanation:'비스듬히 이은 선분은 고무에 대한 수선이 아니에요.'},
      {applicable:p.delta!==0,misconceptionId:'visible-noncrossing-is-parallel',action:{type:'stroke',a:g.handle,b:g.foot,points:[g.handle,g.foot]},explanation:'화면 안에서 만나지 않아 보여도 방향이 다르면 평행이 아니에요.'},
      {applicable:p.target!==null&&p.d!==p.target,misconceptionId:'ignore-distance-order',action:{type:'stroke',a:g.handle,b:g.foot,points:[g.handle,g.foot]},explanation:'이 창은 '+p.d+' cm이므로 4 cm인 창만 닦는 주문에 맞지 않아요.'},
      {misconceptionId:'discard-cleanable-pane',action:{type:'discard'},explanation:'조건을 만족하는 창은 수선으로 닦아야 해요.'},
      {applicable:Math.abs(g.u.x)>SIN4+EPS,misconceptionId:'screen-horizontal-is-perpendicular',action:strokeDirection(p,0),explanation:'화면의 가로 방향만 따라 밀면 고무와 직각이 아니에요.'}
    ];
    // Required order: remove actual correct actions → range check → deduplicate
    // → supplement → shuffle. Fallback angles are finite, no retry-generation.
    const out=candidates.filter(c=>c.applicable!==false).filter(c=>!judge(p,c.action).ok).filter(c=>c.action.type==='discard'||[c.action.a,c.action.b].every(q=>finitePoint(q)&&q.x>=-180&&q.x<=500&&q.y>=-180&&q.y<=600));
    const unique=[],seen=new Set();for(const c of out){const k=serializeAction(c.action);if(!seen.has(k)){seen.add(k);unique.push(c);}}
    for(const angle of [p.theta+25,p.theta+45,p.theta+65]){if(unique.length>=3)break;const c={misconceptionId:'oblique-distance',action:strokeDirection(p,angle),explanation:'고무에 대한 수선 방향이 아니에요.'};const k=serializeAction(c.action);if(!judge(p,c.action).ok&&!seen.has(k)){seen.add(k);unique.push(c);}}
    return shuffle(unique,rng(hashString(p.id))).slice(0,3);
  }
  function serializeAction(a){if(a.type==='discard')return '창을 옆으로 쓸어 내기';const angle=(Math.atan2(a.b.y-a.a.y,a.b.x-a.a.x)/DEG+360)%360;return '방향 '+angle.toFixed(3)+'° · 한 획 '+length(sub(a.b,a.a)).toFixed(3)+' px';}
  function hashString(s){let h=2166136261;for(let i=0;i<s.length;i++)h=Math.imul(h^s.charCodeAt(i),16777619);return h>>>0;}
  // Constraint satisfaction pool: each of the 1800 orientations maps through
  // a finite balanced recipe to an integer distance and a tangential offset;
  // modes and nonparallel deltas are then enumerated. This is a compact recipe,
  // not the full Cartesian product. No generate-then-reject random loop.
  const pool=[];
  for(let i=0;i<ORIENTATIONS.length;i++){
    const theta=ORIENTATIONS[i],d=3+i%4,offset=sourceOffset(i,198);
    pool.push(makeProblem('point',theta,d,offset));
    pool.push(makeProblem('pair',theta,d,offset));
    pool.push(makeProblem('error',theta,d,offset));
    pool.push(makeProblem('mixed',theta,d,offset));
    for(const delta of [12,15])pool.push(makeProblem('mixed',theta,d,offset,delta));
    // 닫힌 창은 고무를 잘라 낼 수 없으니(도형이 열린다) 손잡이를 윗변 한가운데·꼭짓점에서 떼어 놓는다
    // (TRAP_OFFSETS 참조). 발이 윗변 한가운데와 가까우면 '맞은편 변 한가운데로 밀기'가 그대로 수선이 된다.
    pool.push(makeProblem('target',theta,i%2?6:4,TRAP_OFFSETS[(i*37+11)%TRAP_OFFSETS.length],0,'trapezoid'));
  }
  // The square pane was exactly two panes, both tilted 45°, so its answer
  // direction was memorisable after one play. Six tilts x three offsets keep the
  // side length at 4 cm (half-diagonal 73.5 px fits the 296x336 bounds) while the
  // perpendicular still has to be read off the drawn gasket every time.
  // 오프셋 ±37·41·45 (음수 = 거울상 마름모). 발이 언제나 그려진 윗변 안쪽(끝에서 15px 이상)에 있고,
  // 윗변 한가운데(±60)와 15px · 뒤집은 비율 자리(±(60−o))와 14px · 창 한가운데를 지나는 선과
  // 16px 이상 떨어져 있다.
  for(const th of [33.5,56.5,78.5,101.5,123.5,146.5])for(const offset of [-45,-41,-37,37,41,45])pool.push(makeProblem('wow',th,4,offset,0,'square'));
  const byMode={};for(const p of pool)(byMode[p.mode]||(byMode[p.mode]=[])).push(p);
  function deck(random){
    const chosen=[],used=new Set();
    function add(p){chosen.push(p);used.add(p.id);}
    // 축(0°·90°)에서 10° 안쪽인 창은 채점 덱에 넣지 않는다. 화면 가로·세로로만 미는
    // 정책(그리고 '보이는 한가운데 → 한가운데' 정책)이 그런 창에서는 그대로 통과한다.
    // 앞의 두 장은 band() 로 이미 보장돼 있었지만 pick() 은 풀 전체에서 뽑고 있었다.
    function offAxis(t){const m=((t%90)+90)%90;return m>=10&&m<=80;}
    function pick(mode,condition){const options=byMode[mode].filter(p=>!used.has(p.id)&&offAxis(p.theta)&&(!condition||condition(p)));const p=options[Math.floor(random()*options.length)];chosen.push(p);used.add(p.id);}
    // Orientation drawn on the same 0.1 deg lattice as the pool.
    function band(lo,hi){return Math.round((lo+random()*(hi-lo))*10)/10;}
    // The MODE ramp below is the authored one (수선 -> 평행선 사이의 거리 ->
    // 자국·불량 창 -> 주문·정사각형). The ORIENTATIONS are deliberately not:
    // scored panes 1-2 used to be exactly 0° and 90°, so a learner who only ever
    // pushes screen-vertical cleared the horizontal pane without reading it —
    // the very misconception this unit targets ("가로선과 세로선처럼 보일 때만
    // 수직"). Measured: a fixed-direction bot scored 14.9% first-try against a
    // 4.4% chance level. Every scored pane now sits at least 14° off both screen
    // axes, so the perpendicular must be read off the drawn gasket. The authored
    // 수평 패킹 reference is not lost — it is the (unscored) tutorial pane.
    const early=shuffle([band(20,70),band(110,160)],random);
    // 초반 4장의 출발점에 정확한 가운데(0)가 들어 있으면, 「맞은편 고무의 가운데로 밀기」가
    // 수선 판단 없이 정답이 된다(2026-09-11 검수 재현: 첫 시도 정답률 16.8 % / 우연 4.44 %).
    // 그래서 초반 출발점도 본풀과 같은 균등 격자에서 뽑되 가운데 띠(|offset| < 30)를 뺀다.
    const earlyOffsets=shuffle([-86,-66,-49,-33,33,49,66,86],random).slice(0,4);
    add(makeProblem('point',early[0],3+Math.floor(random()*4),earlyOffsets[0]));
    add(makeProblem('point',early[1],3+Math.floor(random()*4),earlyOffsets[1]));
    const pairBands=shuffle([band(14,26),band(24,36),band(39,51),band(54,66)],random);
    add(makeProblem('pair',pairBands[0],3+Math.floor(random()*4),earlyOffsets[2]));
    add(makeProblem('pair',pairBands[1],3+Math.floor(random()*4),earlyOffsets[3]));
    if(random()<.5){pick('error');pick('mixed',p=>p.delta!==0);}else{pick('mixed',p=>p.delta!==0);pick('error');}
    pick('target');pick('wow');return chosen;
  }
  let sampleSeed=49173;
  function sampleProblems(n){
    n=Math.max(0,Math.floor(Number(n)||0));const random=rng(++sampleSeed),output=[];
    // Each sample cycle is the same eight-job deck used in play. This preserves
    // point/pair/error/nonparallel/target/square coverage instead of weighting
    // rows by the implementation size of each mode's backing pool.
    while(output.length<n){for(const p of deck(random)){
      if(output.length>=n)break;const ca=correctAction(p),bad=wrongActions(p),answer=serializeAction(ca);
      const answerActions=(ca.type==='stroke'&&(p.mode==='error'||p.mode==='mixed'))?[{type:'lift'},ca]:[ca];
      output.push({id:p.id,prompt:p.prompt,choices:null,answer,answerNumeric:p.requiresDiscard?0:p.d,unitConcept:p.unitConcept,problem:p,answerAction:ca,answerActions,geometryFacts:{theta:p.theta,secondTheta:p.theta+p.delta,distanceCm:p.d,unitPx:C.unit},distractors:bad});
    }}return output;
  }
  function create(seed){
    const random=rng(seed===undefined?Date.now():seed);let runDeck=[],index=0,currentProblem=tutorial,feedbackLeft=0,resumePhase='playing',eventSerial=0;
    let s;
    function reset(){s={score:0,lives:3,level:0,phase:'intro',solved:0,cleaned:0,discarded:0,combo:0,maxCombo:0,timeLeft:90,frozen:true,tutorial:true,attempts:0,tutorialAttempts:0,tutorialElapsed:0,blockedAttempts:0,firstAttempts:0,firstCorrect:0,retryReady:false,lifted:false,retryUsed:false,retryCredit:false,marks:[],labels:[],elapsed:0,lastEvent:null,eventSerial:0,questionAttempts:0};}
    reset();
    function event(kind,message,extras){s.eventSerial=++eventSerial;s.lastEvent={kind,message,serial:eventSerial,...extras};return s.lastEvent;}
    function next(){currentProblem=runDeck[index];s.level=currentProblem.level;s.questionAttempts=0;s.retryReady=false;s.retryUsed=false;s.retryCredit=false;s.lifted=false;s.marks=[];if(currentProblem.mode==='error'||currentProblem.mode==='mixed'){const m=geometry(currentProblem).wrongStroke;s.marks.push({a:{...m.a},b:{...m.b},source:'prefilled',misconceptionId:'oblique-distance'});}s.labels=[];s.phase='playing';s.frozen=false;s.tutorial=false;return event('next',currentProblem.prompt,{problem:currentProblem});}
    function finish(won){s.phase=won?'won':'lost';s.frozen=true;event('end',won?'8장을 모두 정리했어요.':'호스가 멈췄어요. 다시 도전해 보세요.',{won});}
    function start(options){reset();runDeck=deck(random);index=0;feedbackLeft=0;eventSerial=0;if(options&&options.tutorial===false){next();}else{currentProblem=tutorial;s.phase='tutorial';event('next',tutorial.prompt,{problem:tutorial});}return getState();}
    function submit(a){
      if(s.phase==='won'||s.phase==='lost'||s.lives<=0)return {kind:'ended',ok:false};
      if(s.phase==='intro')return event('invalid','시작 손잡이를 눌러 보세요.');
      if(s.phase==='feedback')return {kind:'busy',ok:false};
      if(a&&a.type==='lift'){
        const can=s.retryReady&&!s.retryUsed;
        s.lifted=true;if(can){s.retryUsed=true;s.retryReady=false;}s.retryCredit=can||s.retryCredit;
        if(!s.frozen){s.timeLeft=Math.max(0,s.timeLeft-.8);s.elapsed+=.8;if(s.timeLeft===0){finish(false);return s.lastEvent;}}
        return event('lift',can?'밀대를 들었어요. 한 번은 마개 손실 없이 다시 밀 수 있어요.':'밀대를 들었어요. 새 방향으로 밀어 보세요.',{retryCredit:can});
      }
      // Material obstruction is checked BEFORE mathematical judgement. A tool
      // cannot engage on a wet stain start; this is not an incorrect answer and
      // cannot reveal whether the attempted angle was correct. The clock keeps
      // running. Lifting (visible 0.8 s cost) or placing the tool on a genuinely
      // different clear point on the gasket gives the player two real routes.
      const stainMode=currentProblem.mode==='error'||currentProblem.mode==='mixed';
      if(stainMode&&!s.lifted&&a&&a.type==='stroke'&&finitePoint(a.a)&&finitePoint(a.b)&&distance(a.a,a.b)>=25&&s.marks.some(m=>distance(a.a,m.a)<C.stainStartRadius)){
        s.blockedAttempts++;
        return event('blocked','자국에서 밀대가 미끄러져요. 짧게 눌러 들거나, 고무의 빈 곳에서 밀어 보세요.',{ok:false,lifeLost:false,legalCost:true,action:a,problem:currentProblem});
      }
      // A lift bypasses the wet-start obstruction for exactly one stroke. The
      // stains remain in state and on screen; lifting never solves the maths.
      if(a&&a.type==='stroke')s.lifted=false;
      const result=judge(currentProblem,a),wasTutorial=s.tutorial;
      s.attempts++;
      if(!wasTutorial){if(s.questionAttempts===0){s.firstAttempts++;if(result.ok)s.firstCorrect++;}s.questionAttempts++;}
      if(result.ok){
        if(!wasTutorial){s.score+=100+Math.min(s.combo,8)*15+(currentProblem.shape==='square'?50:0);s.solved++;if(a.type==='discard')s.discarded++;else s.cleaned++;s.combo++;s.maxCombo=Math.max(s.maxCombo,s.combo);}
        s.labels=currentProblem.shape==='square'?['사다리꼴','평행사변형','마름모']:currentProblem.shape==='trapezoid'?['사다리꼴']:[];
        s.phase='feedback';feedbackLeft=wasTutorial?1.15:Math.max(1.15,2.4-Math.min(s.combo,8)*.10-(s.elapsed>=55?.35:s.elapsed>=20?.15:0));resumePhase=wasTutorial?'begin':'advance';
        // `...result` carries judge()'s own `message`, so any override has to sit
        // after the spread — before this, the practice pane's success line never
        // reached the screen and neither did the release line below.
        const message=wasTutorial?'두 곳이 모두 직각! 수선의 길이는 4 cm예요.':currentProblem.shape==='trapezoid'?result.message+'. 평행한 변이 한 쌍이라도 있어서 사다리꼴이에요.':result.message;
        return event('correct',result.message,{...result,message,action:a,correctAction:correctAction(currentProblem),problem:currentProblem,tutorial:wasTutorial,lifeLost:false});
      }
      const lifeLost=!wasTutorial&&!s.retryCredit;
      if(lifeLost)s.lives=Math.max(0,s.lives-1);
      s.retryCredit=false;s.lifted=false;s.combo=0;
      if(!wasTutorial&&a?.type==='stroke'&&!s.retryUsed)s.retryReady=true;
      if(a&&a.type==='stroke'&&finitePoint(a.a)&&finitePoint(a.b)){s.marks.push({a:{...a.a},b:{...a.b},misconceptionId:result.misconceptionId||'incomplete-perpendicular'});if(s.marks.length>4)s.marks.shift();}
      // The practice pane must not be a wall. It froze the clock and repeated
      // itself forever, so a learner who could not hit ±4° never reached a single
      // scored pane (measured: 20 straight misses, 31 s, still phase 'tutorial').
      // Practice is unscored, so releasing it cannot inflate the first-attempt
      // correct rate — the eight real panes still start at 0/0.
      // 빈 곳 탭(type 'invalid')은 '밀어 본 것'이 아니다. 그것까지 세면 한 번도 밀지 않은
      // 학습자가 탭 세 번에 연습을 통과한다(2026-09-11 2차 검수 첫 플레이 프레임). 실제로 민
      // 시도(stroke)만 세고, 아무것도 못 하는 사람을 위해 35초 시간 탈출을 따로 둔다.
      if(wasTutorial&&a&&a.type==='stroke')s.tutorialAttempts++;
      const releaseTutorial=wasTutorial&&s.tutorialAttempts>=3;
      s.phase='feedback';feedbackLeft=releaseTutorial?1.9:1.05;resumePhase=releaseTutorial?'begin':wasTutorial?'tutorial':'repeat';
      return event('wrong',result.message,{...result,message:releaseTutorial?'연습은 여기까지 할게요. 이제 진짜 창이 내려와요. 고무와 직각이 되게 밀어 보세요.':result.message,action:a,correctAction:correctAction(currentProblem),problem:currentProblem,tutorial:wasTutorial,releaseTutorial,lifeLost});
    }
    function tick(dt){
      dt=Number.isFinite(dt)?Math.max(0,dt):0;
      if(s.phase==='won'||s.phase==='lost'||s.phase==='intro')return;
      // The tutorial freezes all gameplay progression and costs, including
      // feedback time accounting, until the user has drawn the fixed stroke.
      if(!s.tutorial){s.elapsed+=dt;s.timeLeft=Math.max(0,s.timeLeft-dt);}
      else{
        // 연습에는 별도 시계가 돈다(점수·마개는 그대로 정지). 26초가 지나면 실제로
        // 밀어 보지 않았더라도 실전으로 내보낸다 — 연습이 벽이 되면 안 되기 때문이다.
        // 빈 곳 탭만으로는 풀리지 않는다(2026-09-11 2차 검수: 탭 3번에 연습이 끝났다).
        s.tutorialElapsed=(s.tutorialElapsed||0)+dt;
        if(s.tutorialElapsed>=26&&s.phase==='tutorial'){
          s.tutorialElapsed=0;
          event('wrong','연습은 여기까지 할게요. 이제 진짜 창이 내려와요. 빨간 밀대를 잡고 고무와 직각이 되게 밀어 보세요.',
            {reason:'tutorial-timeout',problem:currentProblem,tutorial:true,releaseTutorial:true,lifeLost:false});
          s.phase='feedback';feedbackLeft=1.9;resumePhase='begin';return;
        }
      }
      if(s.phase==='feedback'){
        feedbackLeft-=dt;if(feedbackLeft>0)return;
        if(s.lives<=0){finish(false);return;}
        if(resumePhase==='begin'){next();return;}
        if(resumePhase==='advance'){if(s.solved>=C.target){finish(true);return;}if(s.timeLeft<=0){finish(false);return;}index++;next();return;}
        if(!s.tutorial&&s.timeLeft<=0){finish(false);return;}
        s.phase=resumePhase==='tutorial'?'tutorial':'playing';event('ready',currentProblem.prompt,{problem:currentProblem});return;
      }
      if(!s.tutorial&&s.timeLeft<=0)finish(false);
    }
    function getState(){return {...s,marks:s.marks.map(m=>({...m,a:{...m.a},b:{...m.b}})),labels:s.labels.slice(),problemId:currentProblem.id,index,remaining:C.target-s.solved};}
    return {start,submit,tick,getState,current:()=>currentProblem,geometry:()=>geometry(currentProblem),sampleProblems};
  }
  root.PaneWipeEngine=Object.freeze({create,geometry,judge,correctAction,wrongActions,pool:Object.freeze(pool),tutorial,sampleProblems,constants:C,segmentDistance,strokeDirection});
})(typeof window!=='undefined'?window:globalThis);
