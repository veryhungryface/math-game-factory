# 한 통 — 독립 수학·계약 감사

- 검사일: 2026-09-08. 대상: `public/g/one-can/engine.js`와 `meta.json`.
- 엔진 SHA-256: `86cfb5e7af562dab097c9522a2777867c4d57671d0ff5cb04c505cd4e18515a7`.
- 메타 SHA-256: `2734610f86b6f5fcc48d9347ed6a95e28982497ff357c61da42e06ad91fb8ad7`.
- **판정: 정수 수학·상태 전이 감사 통과. 브라우저·터치·화면·FPS 검증을 뜻하지 않는다.** 아래 실행은 Node VM에서 실제 게임이 사용하는 엔진의 `tap`/`tick` 경로를 검사했다. 렌더러의 입력 경로는 부모 작업의 별도 QA 대상이다.

## 교육과정과 기획 모순 정리

`curriculum/2022-elementary-math.json`의 `g3s2-u5` 및 `[4수03-17]`, `[4수03-18]`, `[4수03-19]`가 실재하고 단원 소속도 일치한다. `textbook-structure.json`의 3-2 권 5단원에서 차시 1·2·4와 caveats를 직접 확인했다. `textbook-crosswalk.json`에는 이 3학년 단원 항목이 없으므로 권·단원명으로 직접 대조했다. 발문은 지시형, 단위는 수와 띄운 `L`·`mL`, 받아올림/받아내림은 1000이다. 무게·어림·6학년 용어는 출제하지 않는다.

기획서의 수치 오류 두 개를 구현에 옮기지 않았다. `1 L 1100 mL`는 비정규 표기여도 `2100 mL`와 같은 양이며, `800 mL + 300 mL`는 `1100 mL`다. 뺄셈 발문은 “덜어 낸 뒤 남은 양”으로 고쳐 제거한 양 자체를 묻는 것으로 읽히지 않게 했다.

입력 경로는 레일 컵 드롭 하나로 통일했다. 독립 비교 통 선택과 저장 컵 꺼내기 대신 채움·역추적·단위 변환·두 양의 합·뺄셈 뒤 남은 양 채움의 다섯 유형을 쓴다. 실제 일반 주문 순서는 채움 → 역추적 → 혼합 단위 2장 → 뺄셈 → 덧셈이며, 그 뒤도 덧셈·뺄셈 풀을 순서대로 사용한다. 메타의 `type_mix_plan`도 이 실제 경로에 맞췄다.

항상 작은 컵을 넣으면 오류 없이 진행하는 우회를 막기 위해 통마다 **컵 3개까지**라는 공개 자원을 두었다. 작은 컵의 합법적인 덧셈은 점수가 증가하고 `mathematicalCorrect: true`다. 세 컵을 다 쓴 뒤 부족하면 별도의 `allowance` 실패이며, 넘침(`overflow`)과 원인·부족한 양을 구별한다. 오답 뒤 생명은 돌아오지 않는다.

`shipped`에는 정확히 1000 mL를 채운 통만 더한다. 주문의 부분 통 완료는 `solved`만 증가시킨다. 500+500의 안내 통은 실제 한 통으로 세션 출고에 포함하되 일반 첫 시도 정답률에서는 제외한다. 두 번째 안내 드롭이 끝난 뒤 90초 타이머가 시작된다. 승리는 주문을 완성했을 때 출고 **8통 이상**이다. 안내를 생략하는 QA 경로는 한 주문에서 8통을 넘어 9통이 될 수 있으므로 정확히 8에서 즉시 끝난다고 주장하지 않는다.

## 전수 결과

| 검사 | 결과 |
|---|---:|
| 유한 문제 풀 | 164문항 |
| 실제 도달 수학 상태 | 2163상태 |
| 실제 `tap` 분기 판정 | 6489입력 |
| 주문 완료 분기 | 455건 |
| 넘침 분기 | 2384건 |
| 공개 컵 예산 소진·부족 분기 | 866건 |
| 생성된 오답 | 492개, 정답과 동치 0개 |
| 오개념 계산식 대조 | 492개, 불일치 0개 |
| 정확한 무작위 주문 성공확률 전수 재계산 | 164문항, 불일치 0개 |
| 각 문항 정상 경로 | 164/164 성공 |
| 각 문항 첫 실수 뒤 경로 | 164/164 성공, 생명 2 유지 |
| 표본 길이 | 40→40, 17→17, 63→63, 1000→1000 |
| 안내 동결/첫500/두번째500/통계 제외 | 통과 |
| 세 번 실패 후 영구 종료·추가 입력 무효 | 통과 |

덧셈 85문항은 두 mL 부분의 합이 1000 이상인 풀에서 만들었고, 뺄셈 52문항은 빼는 mL 부분이 원래 mL 부분보다 큰 받아내림 풀이다. 모든 용량과 답은 정수다. 분기 전수 검사는 계산을 직접 다시 한 뒤 실제 `tap`의 반환값·점수·생명·완료 상태와 비교했다.

표본 오답은 실제 잘못된 계산을 따른다. 예를 들어 L를 100 mL로 환산하거나, mL 합의 받아올린 1 L를 버리거나, 받아내릴 때 1000 대신 100을 더한다. 뺄셈의 후자는 정답보다 900 mL 작다. 역추적 8문항의 앞 두 오답은 기획서의 `1 L를 100 mL로 바꿈`과 `기준을 10으로 축소`를 각각 `100+start`, `1000-start/10`으로 역산했고, 전부 정수·양수·정답과 비동치다. 같은 오답이 겹치는 8문항은 정답 제거 → 범위 → 중복 제거 후 일반 보충 오답을 넣었다. 그 보충값을 특정 교과서 오개념이라고 주장하지 않는다. 표본의 `prompt`는 실제 화면 주문서의 `p.prompt`와 동일하다. 합법적이지만 마지막 컵으로 작다는 이유만으로 수치 오답이라고 표시하지도 않는다.

안내 포함 200시드(1…200)를 정상 선택과 첫 일반 입력의 넘침 뒤 정상 선택으로 각각 완주했다. 각각 200/200 성공, 정확히 8통, 생명 3/2였다. 최대 33번 일반 드롭이며 일반 드롭 0.8초·뚜껑 1.5초라는 명시적 모형에서는 최대 30.5/32.1초다. 이는 프레임 실측이 아니라 시간 비용을 둔 엔진 시뮬레이션이다.

## 독립 무뇌 봇

각 정책은 200판, 게임 시드 1…200으로 고정했다. 안내는 건너뛰고 매 드롭 뒤 1.6초를 진행했다. 연타는 인덱스0, 순환은 0→1→2, 무작위는 게임 난수와 분리한 LCG다. 재시도는 첫 시도 분모에 새로 더하지 않는다. 측정 단위는 **처음 시도하는 주문 1장**이며, 원래 기획의 근삿값 21%를 쓰지 않는다. 각 주문의 실제 모든 3방향 분기에서 성공 확률을 계산한 뒤 경험한 주문들에 대해 평균냈다.

| 정책 | 첫 시도 정답 | 첫 시도 정답률 | 주문별 정확 우연 기준 평균 | 완주율 |
|---|---:|---:|---:|---:|
| 연타 | 45/308 | 14.610% | 15.704% | 0/200 |
| 순환 | 55/314 | 17.516% | 15.736% | 0/200 |
| 무작위 | 38/309 | 12.298% | 15.678% | 0/200 |
| 무입력 | 0/200 | 0% | 0% | 0/200 |
| 추가: 최솟값 컵 | 0/200 | 0% | 14.815% | 0/200 |
| 추가: 가장 높은 컵 | 45/306 | 14.706% | 15.443% | 0/200 |

순환의 관측값은 평균 우연 기준보다 1.78%p 높다. `z = 0.883`으로 유의한 초과는 관측하지 않았지만, “관측치가 모두 우연 이하”라고 쓰면 안 된다. 원시 비율과 유의성 해석을 분리한다. 이 결과를 낮추려고 시드를 골라 다시 실행하지 않았다. 무입력의 `0/200`은 열린 주문이 타임아웃으로 끝난 200개이며 실제 학습 진도(`solved`, `shipped`)는 둘 다 0이다. 키 선택은 표준화된 용량과 관계없는 외형값만 따라갔다. 실제 모바일 하드웨어와 아동 학습효과는 이 시험의 범위가 아니다.

## 재현

저장소 루트에서 다음 코드 블록을 Node로 실행한다. 실제 엔진을 VM에 로드하고 독립 산술 오라클과 비교한다.

```js
const fs = require('fs'), vm = require('vm'), assert = require('assert');
const context = {}; vm.createContext(context);
vm.runInContext(fs.readFileSync('public/g/one-can/engine.js', 'utf8'), context);
const E = context.OneCan;
const totals = {pool:E.pool.length,states:0,edges:0,complete:0,overflow:0,allowance:0,candidates:0};
function make(p) {
  const s=E.create(991,{tutorial:false});
  Object.assign(s,{problem:p,level:p.level,canMl:p.canStartMl,orderShipped:0,dropsLeft:3,
    shipped:0,score:0,solved:0,currentFirstRecorded:false,firstChance:E.chanceOfProblem(p),firstAttempts:[],random:()=>.37});
  s.rail=E.railValues(Math.min(1000,p.targetMl)-p.canStartMl,3,p.pattern).map(c=>({...c,shapeSeed:.4}));
  return s;
}
function copy(s) { return {...s,rail:s.rail.map(c=>({...c})),firstAttempts:s.firstAttempts.slice(),decks:{...s.decks},deckPositions:{...s.deckPositions},random:()=>.37}; }
for(const p of E.pool) {
  if(p.kind==='add_two') {assert.equal(p.targetMl,p.addends[0]+p.addends[1]);assert(p.addends[0]%1000+p.addends[1]%1000>=1000);}
  if(p.kind==='subtract_fill') {assert.equal(p.targetMl,p.addends[0]-p.addends[1]);assert(p.addends[0]%1000<p.addends[1]%1000);}
  const answer=p.kind==='reverse_complement'?1000-p.canStartMl:p.targetMl;
  assert.equal(E.answerValue(p),answer);
  const candidates=E.candidates(p);assert.equal(candidates.length,3);assert.equal(new Set(candidates.map(c=>c.value)).size,3);
  for(const c of candidates) {assert(Number.isInteger(c.value)&&c.value>0&&c.value!==answer);assert(c.misconceptionId);totals.candidates++;}
  const visited=new Map();
  function visit(s) {
    const key=[s.orderShipped,s.canMl,s.dropsLeft].join(':');if(visited.has(key))return visited.get(key);
    totals.states++;const capacity=Math.min(1000,p.targetMl-s.orderShipped*1000),need=capacity-s.canMl;
    assert.equal(s.rail.length,3);assert.equal(new Set(s.rail.map(c=>c.ml)).size,3);let chance=0;
    for(let i=0;i<3;i++) {
      totals.edges++;const n=copy(s),before=n.canMl,amount=n.rail[i].ml,drops=n.dropsLeft,score=n.score,lives=n.lives;
      assert(Number.isInteger(amount)&&amount>0);const ev=E.tap(n,i);
      if(amount>need) {assert.equal(ev.type,'wrong');assert.equal(ev.reason,'overflow');assert.equal(ev.overflow,amount-need);assert.equal(n.lives,lives-1);totals.overflow++;}
      else if(before+amount<capacity&&drops===1) {assert.equal(ev.reason,'allowance');assert.equal(ev.mathematicalCorrect,true);assert.equal(ev.missing,capacity-before-amount);assert.equal(n.lives,lives-1);assert.equal(n.score,score+10);totals.allowance++;}
      else {assert.equal(ev.mathematicalCorrect,true);assert.equal(n.lives,lives);assert(n.score>score);
        if(ev.orderComplete) {assert.equal(ev.totalMl,p.targetMl);assert.equal(n.solved,s.solved+1);assert.equal(n.firstAttempts.length,1);assert.equal(n.firstAttempts[0].correct,true);chance++;totals.complete++;}
        else chance+=visit(n);
      }
    }
    chance/=3;visited.set(key,chance);return chance;
  }
  assert(Math.abs(visit(make(p))-E.chanceOfProblem(p))<1e-12);
  for(const mistake of [false,true]) {
    const s=make(p);if(mistake){E.tap(s,s.rail.findIndex(c=>c.ml>E.need(s)));assert.equal(s.lives,2);assert.equal(s.firstAttempts[0].correct,false);}
    let done=false;for(let k=0;k<15;k++){const i=E.correctIndex(s);assert(i>=0);if(E.tap(s,i).orderComplete){done=true;break;}}
    assert(done);assert.equal(s.solved,1);assert.equal(s.lives,mistake?2:3);
  }
}
for(const n of [40,17,63,1000]) {const ps=E.sampleProblems(n);assert.equal(ps.length,n);for(const p of ps){assert(p.choices.includes(p.answer));assert.equal(new Set(p.choices).size,p.choices.length);assert.equal(p.answerNumeric,E.answerValue(p));}}
const tutorial=E.create(13);E.tick(tutorial,1000);assert(tutorial.frozen&&tutorial.elapsed===0&&tutorial.lives===3);E.tap(tutorial,0);assert(tutorial.canMl===500&&tutorial.frozen);E.tap(tutorial,0);assert(!tutorial.frozen&&tutorial.shipped===1&&tutorial.firstAttempts.length===0);
const failure=E.create(777,{tutorial:false});for(let i=0;i<3;i++)E.tap(failure,failure.rail.findIndex(c=>c.ml>E.need(failure)));const score=failure.score;assert(failure.lives===0&&failure.phase==='gameover');E.tap(failure,0);E.tick(failure,1000);assert(failure.lives===0&&failure.phase==='gameover'&&failure.score===score);
console.log(totals);
function random(seed){let a=seed>>>0;return()=>{a=(Math.imul(a,1664525)+1013904223)>>>0;return a/4294967296;};}
for(const policy of ['same','cycle','random','idle','smallest','tallest']) {
  let attempted=0,correct=0,expected=0,variance=0,cleared=0,solved=0,shipped=0;
  for(let seed=1;seed<=200;seed++) {
    const s=E.create(seed,{tutorial:false}),r=random(seed^0x6a09e667);let k=0;
    while(s.phase==='playing'&&k<100) {
      if(policy==='idle'){E.tick(s,90);break;}
      const i=policy==='same'?0:policy==='cycle'?k%3:policy==='random'?Math.floor(r()*3):policy==='smallest'?s.rail.reduce((a,b,j)=>b.ml<s.rail[a].ml?j:a,0):s.rail.reduce((a,b,j)=>b.shapeSeed>s.rail[a].shapeSeed?j:a,0);
      E.tap(s,i);E.tick(s,1.6);k++;
    }
    for(const a of s.firstAttempts){attempted++;correct+=+a.correct;const p=policy==='idle'?0:a.chance;expected+=p;variance+=p*(1-p);}
    cleared+=+(s.phase==='clear');solved+=s.solved;shipped+=s.shipped;
  }
  console.log({policy,games:200,correct,attempted,rate:correct/attempted,chance:expected/attempted,z:variance?(correct-expected)/Math.sqrt(variance):0,cleared,solved,shipped});
}
```
