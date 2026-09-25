/* 별 딱총 — integer-only, DOM-free game state. Same pools power play and QA.
 * Textbook expression_traps (2022, 3-2 unit 6):
 * - Read a pictograph using its printed title and legend; no vertical scale.
 * - Units are 10/1 or 100/10, never arbitrary 3/7 units. Half of 10 is 5.
 * - Refer to displayed data with '표를 보고' / '그림그래프를 보고';
 *   every table-referencing problem carries the same sourceTable used by the UI.
 * - Workbook instructions use '~해 보세요'; error correction uses '잘못'.
 * - No thousands commas. Integers are compared, never decimal strings.
 * - Rounding, if introduced elsewhere, must say '반올림하여 ○의 자리까지'.
 *   Bounds 이상/이하 include endpoints, 초과/미만 exclude them. No such task
 *   is generated here; fraction/decimal/π concepts are outside lessons 1–3.
 * There is deliberately NO numeric plate total in the live-state HUD API.
 */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.StarCork = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const TYPES = ['big', 'small', 'half'];
  const NAMES = ['민지', '하준', '서윤'];
  function subject(name) { return name + ((name.charCodeAt(name.length - 1) - 44032) % 28 ? '이' : '가'); }
  const GOAL = 6;
  const SESSION_SECONDS = 90;
  function rng(seed) {
    let x = (Number(seed) >>> 0) || 0x51ac27;
    return function () { x += 0x6D2B79F5; let t = x; t = Math.imul(t ^ t >>> 15, t | 1); t ^= t + Math.imul(t ^ t >>> 7, t | 61); return ((t ^ t >>> 14) >>> 0) / 4294967296; };
  }
  function shuffle(a, random) {
    const out = a.slice();
    for (let i = out.length - 1; i > 0; i--) { const j = Math.floor(random() * (i + 1)); [out[i], out[j]] = [out[j], out[i]]; }
    return out;
  }
  function value(type, p) { return type === 'big' ? p.legendBig : type === 'half' ? p.legendBig / 2 : p.legendSmall; }
  function sum(types, p) { return types.reduce((n, type) => n + value(typeof type === 'string' ? type : type.type, p), 0); }
  function evaluate(types, p) { const n = sum(types, p); return n === p.target ? 'correct' : n > p.target ? 'over' : 'under'; }
  function counts(types) { return types.reduce((c, t) => { c[t]++; return c; }, {big: 0, small: 0, half: 0}); }
  function fromCounts(c) { return TYPES.flatMap(t => Array(c[t] || 0).fill(t)); }
  function decompose(n, legendBig, useHalf) {
    const big = Math.floor(n / legendBig), rest = n % legendBig;
    const half = useHalf && legendBig === 10 && rest >= 5 ? 1 : 0;
    return {big, half, small: (rest - half * 5) / (legendBig / 10)};
  }
  function rowValue(row, p) { return row.big * p.legendBig + row.small * p.legendSmall + row.half * p.legendBig / 2; }
  function equation(types, p) { return types.map(t => value(t, p)).join(' + ') + ' = ' + sum(types, p); }
  function graphText(rows, p) {
    return rows.map(r => `${r.name}: 큰 사과 ${r.big}개, 작은 사과 ${r.small}개${r.half ? ', 반쪽 사과 1개' : ''}`).join('; ') + ` (큰 사과 ${p.legendBig}개, 작은 사과 ${p.legendSmall}개${rows.some(r => r.half) ? ', 반쪽 사과 5개' : ''})`;
  }
  function tableText(rows) {
    return `표: ${rows.map(r => `${r.name} | 별사탕 수 ${r.value}개`).join('; ')}`;
  }

  // Stock is independent of the target within each band. Shuffle the finite
  // stock without inspecting any answer or intermediate sum. Unlike the flawed
  // proposal, ordinary orders are judged at the END of the pass: merely passing
  // through the right subtotal while tapping everything cannot earn progress.
  function makeBelt(c, p, variant) {
    // A target must not be recoverable from the conveyor prefix.  The former
    // seed hashed p.id (which embeds the answer), so every answer acquired its
    // own recognisable order even when stock counts were identical.  Seed only
    // from target-independent stock and display variant instead.
    const orderSeed=(
      0x51ac27 ^
      Math.imul((c.big||0)+1,73856093) ^
      Math.imul((c.small||0)+1,19349663) ^
      Math.imul((c.half||0)+1,83492791) ^
      Math.imul((Number(variant)||0)+17,2654435761)
    )>>>0;
    return shuffle(fromCounts(c),rng(orderSeed));
  }

  const pools = {construct: [], read: [], difference: [], scaled: [], repair: [], open: []};
  function finishProblem(p, beltCounts, variant) {
    p.legendSmall = p.legendBig / 10;
    // Data rows do not reserve a location for the answer. The name in a read
    // task remains attached to its data when the printed rows are permuted.
    let rowSeed=0;for(const ch of p.id)rowSeed=(Math.imul(rowSeed,31)+ch.charCodeAt(0))>>>0;
    p.rows = shuffle(p.rows || [],rng(rowSeed));
    p.sourceTable = (p.sourceTable || []).map(row=>({...row}));
    p.initial = p.initial || [];
    p.solution = p.solution || fromCounts(decompose(p.target, p.legendBig, p.allowHalf));
    const beltSize=(beltCounts.big||0)+(beltCounts.small||0)+(beltCounts.half||0);
    const completeCounts={...beltCounts,small:(beltCounts.small||0)+Math.max(0,8-beltSize)};
    p.belt = makeBelt(completeCounts, p, variant || 0);
    p.title = p.rows.length ? '모은 사과 수' : '모은 별사탕 수';
    p.unitConcept = p.type === 'difference' ? '그림그래프 비교' : p.type === 'read' || p.type === 'max' ? '그림그래프 읽기' : p.type === 'error_find' ? '그림그래프 고치기' : '표를 그림그래프로 나타내기';
    p.requiredForm = 'integer';
    return p;
  }
  // Digit-first pools: nonzero small digit prevents icon-count and all-big
  // misconceptions from accidentally equaling the answer. No random rejection.
  for (let b = 2; b <= 3; b++) for (let s = 1; s <= 4; s++) for (let ni = 0; ni < 3; ni++) {
    const target = b * 10 + s;
    pools.construct.push(finishProblem({id:`construct-${b}-${s}-${ni}`, type:'construct', level:1, target, legendBig:10, name:NAMES[ni], sourceTable:[{name:NAMES[ni],value:target}], prompt:`표를 보고 ${subject(NAMES[ni])} 모은 별사탕 ${target}개를 접시에 나타내 보세요.`}, {big:4,small:7,half:0}, ni));
    // Seven prefilled large stars for EVERY repair target: prefilled stock must
    // not reveal the target through the old target-dependent b+s icon count.
    const initial = Array(7).fill('big');
    pools.repair.push(finishProblem({id:`repair-${b}-${s}-${ni}`, type:'error_find', level:3, target, legendBig:10, name:NAMES[ni], initial, prompt:`${subject(NAMES[ni])} ${target}개를 나타내려는데 별을 너무 많이 담았습니다. 접시를 옳게 고쳐 보세요.`}, {big:4,small:7,half:0}, ni));
  }
  for (let b = 2; b <= 3; b++) for (let s = 1; s <= 4; s++) for (let ni = 0; ni < 3; ni++) for (const mode of ['read','max']) {
    const target = b * 10 + 5 + s;
    const p = {id:`${mode}-${b}-${s}-${ni}`,type:mode,level:2,target,legendBig:10,allowHalf:true};
    p.rows = [
      {name:NAMES[ni],big:b,small:s,half:1},
      {name:NAMES[(ni+1)%3],big:1,small:s,half:0},
      {name:NAMES[(ni+2)%3],big:2,small:0,half:0}
    ];
    if(mode==='read'&&ni===0)p.rows[2]={name:NAMES[(ni+2)%3],big:4,small:s,half:0};
    if(mode==='read'&&ni===1){p.rows[1]={name:NAMES[(ni+1)%3],big:4,small:s,half:0};p.rows[2]={name:NAMES[(ni+2)%3],big:5,small:s,half:0};}
    // Half pictures belong to every data row, not only the requested/correct
    // row. Their presence must never act as a visual answer marker.
    p.rows.forEach(row=>{row.half=1;});
    p.prompt = mode === 'read' ? `그림그래프를 보고 ${subject(NAMES[ni])} 모은 사과 수만큼 접시에 나타내 보세요.` : '그림그래프를 보고 가장 많이 모은 사람의 사과 수만큼 접시에 나타내 보세요.';
    pools.read.push(finishProblem(p,{big:4,small:6,half:1},ni));
  }
  for (let b = 1; b <= 2; b++) for (let s = 1; s <= 4; s++) for (let lb = 1; lb <= 2; lb++) for (let ls = 1; ls <= 4; ls++) {
    const target=b*10+s, low=lb*10+ls, high=low+target, middle=low+Math.floor(target/2);
    const rowNames=shuffle(NAMES,rng(b*9173+s*593+lb*31+ls));
    const p={id:`difference-${b}-${s}-${lb}-${ls}`,type:'difference',level:2,target,legendBig:10,allowHalf:true,
      rows:[{name:rowNames[0],...decompose(high,10,true)},{name:rowNames[1],...decompose(low,10,true)},{name:rowNames[2],...decompose(middle,10,true)}],
      prompt:'그림그래프를 보고 가장 많은 사과 수와 가장 적은 사과 수의 차를 접시에 나타내 보세요.'};
    pools.difference.push(finishProblem(p,{big:3,small:7,half:1},lb+ls));
  }
  // Level 3 is the only band with the 100/10 legend, and it must do two jobs.
  // (a) Hundreds have to appear at all: 230/340/410 are the curriculum's
  //     '여러 크기의 그림 단위' case and stay in the lesson.
  // (b) A target whose digits are (hundreds, tens) is satisfied by copying the
  //     gesture learned at 10/1 — 34 and 340 are both 3 big + 4 small — so a
  //     legend-blind digit match would still win. The sub-100 targets break
  //     exactly that: 30 at this legend is three SMALL stars, and the child who
  //     reuses 'first digit → big star' overfills and loses a plate.
  // Eight distinct targets hold the fixed-composition blind chance in this band
  // at 1/8, the same as construct/read/difference. One stock (5 big + 10 small
  // = 600) covers every target and still exceeds each of them, so mashing the
  // whole belt always overfills; no target needs more than 7 of the 10 small
  // stars, which keeps a missed tap recoverable inside one 90s session.
  for (const target of [30,40,50,60,70,230,340,410]) for (let ni = 0; ni < 3; ni++) {
    pools.scaled.push(finishProblem({id:`scaled-${target}-${ni}`,type:'construct',level:3,target,legendBig:100,name:NAMES[ni],sourceTable:[{name:NAMES[ni],value:target}],prompt:`표를 보고 ${subject(NAMES[ni])} 모은 별사탕 ${target}개를 접시에 나타내 보세요.`}, {big:5,small:10,half:0},ni));
  }
  // The chosen open construction explicitly needs twelve small candies, so this
  // final finite belt has 17 objects instead of the ordinary 11–12. Only a
  // screenful is visible at once. Both 3×10 and 2×10+10×1 are accepted for 30.
  for (let b=2;b<=4;b++) for(let ni=0;ni<3;ni++) {
    const target=b*10;
    pools.open.push(finishProblem({id:`open-${b}-${ni}`,type:'open_construct',level:3,target,legendBig:10,name:NAMES[ni],sourceTable:[{name:NAMES[ni],value:target}],prompt:`표를 보고 ${NAMES[ni]}의 별사탕 ${target}개를 큰 별과 작은 별로 자유롭게 나타내 보세요.`,solution:[...Array(b-1).fill('big'),...Array(10).fill('small')]},{big:5,small:12,half:0},ni));
  }
  const allProblems = Object.values(pools).flat();
  const tutorial = {id:'tutorial-21',type:'construct',level:1,target:21,legendBig:10,legendSmall:1,rows:[],title:'모은 별사탕 수',name:'민지',prompt:'별을 맞혀 21개를 접시에 나타내 보세요.',solution:['big','big','small'],belt:['big','big','small'],initial:[],unitConcept:'그림그래프',requiredForm:'integer'};

  function distractors(p) {
    const c = counts(p.solution);
    const candidates = [
      {value:c.big+c.small+c.half,misconceptionId:'count-pictures',reason:'그림 개수만 세고 큰 별과 작은 별의 범례를 놓쳤어요.'},
      {value:(c.big+c.small+c.half)*p.legendBig,misconceptionId:'small-as-big',reason:'작은 별도 큰 별과 같은 수로 세었어요.'},
      {value:c.big*p.legendBig+c.small*p.legendSmall+c.half,misconceptionId:'half-as-one',reason:'반쪽 그림을 1개로 세었어요.'},
      {value:c.big*p.legendBig+c.small*p.legendSmall+c.half*p.legendBig,misconceptionId:'half-as-whole',reason:'반쪽 그림도 큰 그림 하나와 같은 수로 세었어요.'},
      ...(p.legendBig===100?[{value:c.big*10+c.small,misconceptionId:'previous-legend',reason:'새 범례 100과 10을 읽지 않고 10과 1로 세었어요.'}]:[])
    ];
    // Required order: remove correct → range → deduplicate → supplement → shuffle.
    const incorrect=candidates.filter(x=>x.value!==p.target).filter(x=>Number.isInteger(x.value)&&x.value>=0&&x.value<=2000);
    const seen=new Set(), unique=incorrect.filter(x=>seen.has(x.value)?false:(seen.add(x.value),true));
    for(const delta of [p.legendSmall,-p.legendSmall,p.legendBig,-p.legendBig]) {
      if(unique.length>=3)break;
      const n=p.target+delta;
      if(n>=0&&n!==p.target&&!seen.has(n)){seen.add(n);unique.push({value:n,misconceptionId:'one-picture-more-or-less',reason:'별 하나가 나타내는 수만큼 많거나 적어요.'});}
    }
    return unique.slice(0,3);
  }
  let sampleCursor=0;
  const sampleDeck=shuffle(allProblems,rng(170921));
  function sampleProblems(n) {
    n=Math.max(0,Math.floor(Number(n)||0));
    const out=[];
    for(let i=0;i<n;i++) {
      const p=sampleDeck[(sampleCursor++)%sampleDeck.length], wrong=distractors(p);
      const legend=`(큰 별 ${p.legendBig}개, 작은 별 ${p.legendSmall}개)`;
      const extra=p.rows.length?' '+graphText(p.rows,p):p.sourceTable.length?' '+tableText(p.sourceTable)+' '+legend:' '+legend;
      const choices=shuffle([p.target,...wrong.map(d=>d.value)].map(String),rng(sampleCursor*971));
      out.push({id:p.id,prompt:p.prompt+extra,choices,answer:String(p.target),answerNumeric:p.target,unitConcept:p.unitConcept,misconceptions:wrong.map(d=>({...d})),problemId:p.id,type:p.type,legendBig:p.legendBig,legendSmall:p.legendSmall,rows:p.rows.map(r=>({...r})),sourceTable:p.sourceTable.map(r=>({...r})),solution:p.solution.slice(),belt:p.belt.slice(),initial:p.initial.slice(),requiredForm:'integer'});
    }
    return out;
  }

  class Game {
    constructor() { this.width=390;this.sequence=0;this.random=rng(1);this.state={phase:'title',score:0,lives:3,level:1,solved:0,goal:GOAL,elapsed:0,remaining:SESSION_SECONDS,tutorial:true,frozen:true,problem:tutorial,candies:[],plate:[],lastEvent:null,eventSeq:0,orderIndex:-1,attempt:1,attemptLog:[],firstAttemptCount:0,firstAttemptCorrect:0}; }
    emit(type,fields) {
      const s=this.state;
      s.eventSeq++;
      s.lastEvent={type,seq:s.eventSeq,problemId:s.problem.id,orderIndex:s.orderIndex,attempt:s.attempt,tutorial:s.tutorial,...fields};
      return s.lastEvent;
    }
    start(seed,withTutorial=true) {
      this.random=rng(seed===undefined?Date.now():seed);
      this.sequence=0;
      this.decks=Object.fromEntries(Object.entries(pools).map(([k,v])=>[k,shuffle(v,this.random)]));
      const useTutorial=withTutorial!==false;
      this.state={phase:'playing',score:0,lives:3,level:1,solved:0,goal:GOAL,elapsed:0,remaining:SESSION_SECONDS,tutorial:useTutorial,frozen:useTutorial,problem:tutorial,candies:[],plate:[],lastEvent:null,eventSeq:0,orderIndex:useTutorial?-1:0,attempt:1,attemptLog:[],firstAttemptCount:0,firstAttemptCorrect:0,combo:0,inputCount:0,idleTime:0,feedbackRemaining:0,waveElapsed:0,waveDuration:16};
      this.load(useTutorial?tutorial:this.pickProblem(0),useTutorial);return this.state;
    }
    setViewport(width) { this.width=Math.max(320,Number(width)||390); }
    pickProblem(index) {
      const key=['construct','read','difference','scaled','repair','open'][index];
      const deck=this.decks[key];return deck.pop();
    }
    load(p,isTutorial) {
      const s=this.state;
      s.problem=p;s.tutorial=!!isTutorial;s.frozen=!!isTutorial;s.phase='playing';s.level=p.level;
      s.waveElapsed=0;s.idleTime=0;s.feedbackRemaining=0;s.plate=[];
      // Each ordinary order is one finite pass. Wider stages show more objects,
      // preserving a >=64px lane pitch; portrait defaults to a 78px pitch.
      s.spacing=Math.min(.20,Math.max(.055,74/this.width));
      const reading=['read','max','difference'].includes(p.type);
      s.planningRemaining=reading?3:0;
      s.waveDuration=p.type==='open_construct'?12:p.type==='error_find'?11:reading?12:10;
      const travelTime=s.waveDuration-s.planningRemaining-.2;
      s.speed=(1.14+(p.belt.length-1)*s.spacing)/travelTime;
      // A pass always enters from the left. Starting half a belt already past
      // the player makes a fixed tap location see only a misleading suffix.
      s.candies=p.belt.map((type,i)=>({id:`c${++this.sequence}`,type,status:'belt',spawnProgress:isTutorial?.25+i*.25:-.06-i*s.spacing,progress:isTutorial?.25+i*.25:-.06-i*s.spacing}));
      for(const type of p.initial) {const c={id:`c${++this.sequence}`,type,status:'plate',progress:0};s.candies.push(c);s.plate.push(c.id);}
      this.emit('next',{problem:p});
    }
    getState() { return this.state; }
    plateTypes() { const s=this.state;return s.plate.map(id=>s.candies.find(c=>c.id===id).type); }
    shoot(id) {
      const s=this.state;
      if(s.phase!=='playing')return false;
      const c=s.candies.find(c=>c.id===id);
      if(!c||c.status!=='belt'||c.progress<0||c.progress>1)return false;
      c.status='plate';s.plate.push(c.id);s.inputCount++;s.idleTime=0;
      const shot={id:c.id,candyType:c.type,progress:c.progress,plateIndex:s.plate.length-1};
      this.emit('shot',shot);
      const result=evaluate(this.plateTypes(),s.problem);
      if(result==='correct'&&s.frozen)return this.correct(shot);
      if(result==='over'&&!s.frozen)return this.wrong('over',shot);
      return s.lastEvent;
    }
    remove(id) {
      const s=this.state;
      if(s.phase!=='playing')return false;
      const c=s.candies.find(c=>c.id===id);
      if(!c||c.status!=='plate')return false;
      const oldIndex=s.plate.indexOf(id);s.plate.splice(oldIndex,1);c.status='belt';
      // Return the SAME candy into the last free incoming position. No new candy
      // and no silent replacement stock. It must actually be shot again.
      const incoming=s.candies.filter(x=>x!==c&&x.status==='belt').map(x=>x.progress);
      c.progress=s.frozen?c.spawnProgress:Math.min(0,...incoming)-s.spacing;
      s.inputCount++;s.idleTime=0;
      this.emit('removed',{id:c.id,candyType:c.type,plateIndex:oldIndex,progress:c.progress});
      if(s.frozen&&evaluate(this.plateTypes(),s.problem)==='correct')return this.correct();
      return s.lastEvent;
    }
    record(success,reason) {
      const s=this.state;
      s.attemptLog.push({problemId:s.problem.id,orderIndex:s.orderIndex,attempt:s.attempt,tutorial:s.tutorial,success,reason});
      if(!s.tutorial&&s.attempt===1){s.firstAttemptCount++;if(success)s.firstAttemptCorrect++;}
    }
    correct(extra) {
      const s=this.state, plate=this.plateTypes(), eq=equation(plate,s.problem);
      this.record(true,'equal');
      // The frozen practice plate is instruction, not part of the timed record.
      if(!s.tutorial){s.combo++;s.score+=s.problem.target*Math.min(3,s.combo);s.solved++;}
      s.phase='feedback';s.feedbackRemaining=1.25;
      return this.emit('correct',{equation:eq,plate:plate.slice(),target:s.problem.target,duration:1.25,...extra});
    }
    wrong(reason,extra) {
      const s=this.state;
      if(s.phase!=='playing'||s.frozen)return false;
      const plate=this.plateTypes();this.record(false,reason);s.combo=0;
      // A missed (underfilled) pass returns intact and costs time only. Only an
      // overflow cracks one of the three plates, as promised by the fail state.
      if(reason!=='under')s.lives--;
      s.phase='feedback';s.feedbackRemaining=1.15;
      const actual=sum(plate,s.problem);
      // Underfill is a free, same-problem retry.  Revealing the exact icon
      // arrangement here lets a player wait, copy the answer, and pass without
      // doing the reading.  Show only actual-vs-target; costly overflow keeps
      // the designed worked-example reveal.
      const teaching=reason==='under'
        ? {actual,target:s.problem.target}
        : {solution:s.problem.solution.slice(),equation:equation(s.problem.solution,s.problem),actual,target:s.problem.target};
      const event=this.emit('wrong',{reason,plate:plate.slice(),...teaching,duration:1.15,...extra});
      if(s.lives<=0){s.lives=0;event.terminal=true;}
      return event;
    }
    end(reason) {
      const s=this.state;if(s.phase==='won'||s.phase==='lost')return;
      s.phase='lost';s.frozen=false;this.emit('lost',{reason});
    }
    tick(dt) {
      const s=this.state;dt=Math.max(0,Number(dt)||0);
      if(s.phase!=='playing'&&s.phase!=='feedback')return s;
      s.idleTime+=dt;
      if(!s.frozen){s.elapsed+=dt;s.remaining=Math.max(0,SESSION_SECONDS-s.elapsed);}
      if(s.phase==='feedback') {
        s.feedbackRemaining-=dt;
        if(s.feedbackRemaining<=0) {
          if(s.lastEvent.type==='correct') {
            if(s.solved>=GOAL){s.score+=Math.floor(s.remaining);s.phase='won';this.emit('won',{bonus:Math.floor(s.remaining)});return s;}
            s.orderIndex++;s.attempt=1;this.load(this.pickProblem(s.orderIndex),false);
          } else if(s.lives<=0){s.phase='lost';s.frozen=false;this.emit('lost',{reason:'lives'});return s;}
          else {s.attempt++;this.load(s.problem,false);}
        }
      } else if(!s.frozen) {
        s.waveElapsed+=dt;
        const moveTime=Math.max(0,dt-s.planningRemaining);
        s.planningRemaining=Math.max(0,s.planningRemaining-dt);
        for(const c of s.candies)if(c.status==='belt'){c.progress+=s.speed*moveTime;if(c.progress>1.09)c.status='gone';}
        if(s.waveElapsed>=s.waveDuration){
          const result=evaluate(this.plateTypes(),s.problem);
          if(result==='correct')this.correct();else this.wrong(result==='over'?'over':'under');
        }
      }
      // A sixth plate judged in time remains a win while its exit animation
      // finishes; the celebration itself cannot revoke an earned success.
      if(s.remaining<=0&&s.solved<GOAL&&s.phase==='playing')this.end('time');
      return s;
    }
    forceCorrect() {
      const s=this.state;
      if(s.phase==='feedback'){s.feedbackRemaining=0;this.tick(.001);}
      if(s.phase==='won'||s.phase==='lost'||s.phase==='title')return false;
      for(const c of s.candies)c.status='gone';s.plate=[];
      for(const type of s.problem.solution){const c={id:`c${++this.sequence}`,type,status:'plate',progress:0};s.candies.push(c);s.plate.push(c.id);}
      return this.correct();
    }
    forceWrong() {
      const s=this.state;
      if(s.phase==='feedback'){s.feedbackRemaining=0;this.tick(.001);}
      if(s.phase==='won'||s.phase==='lost'||s.phase==='title')return false;
      // QA's explicit forced path must lower lives even in the frozen tutorial.
      const wasFrozen=s.frozen;s.frozen=false;const e=this.wrong('forced');
      if(wasFrozen&&s.phase!=='lost'){s.tutorial=false;s.orderIndex=0;s.problem=this.pickProblem(0);}
      return e;
    }
    sampleProblems(n) { return sampleProblems(n); }
  }

  // Exact null for an independent 50% decision per physical candy in order.
  // Equality is checked only at wave end; overfull NEW SHOTS still fail at once.
  // For repair, first decide independently whether to remove each initial star,
  // then decide whether to shoot each conveyor star. This is one documented
  // null policy, not a substitute for the actual input-policy bot simulation.
  function chanceProbability(p, order) {
    let distribution=new Map([[sum(p.initial,p),1]]);
    function step(v,isRemoval) {
      const next=new Map();
      for(const [n,prob] of distribution) {
        next.set(n,(next.get(n)||0)+prob/2);
        const total=n+(isRemoval?-v:v);
        if(isRemoval||total<=p.target)next.set(total,(next.get(total)||0)+prob/2);
      }
      distribution=next;
    }
    for(const t of p.initial)step(value(t,p),true);
    for(const t of order||p.belt)step(value(t,p),false);
    return distribution.get(p.target)||0;
  }
  return {Game,pools,allProblems,tutorial,value,sum,evaluate,counts,fromCounts,decompose,rowValue,equation,distractors,sampleProblems,chanceProbability,rng,shuffle,GOAL,SESSION_SECONDS};
});
