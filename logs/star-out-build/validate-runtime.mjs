import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {createRuntime} from './runtime-vm.mjs';
const HERE=path.dirname(fileURLToPath(import.meta.url));
const SOURCE_HTML=fs.readFileSync(path.resolve(HERE,'../../public/g/star-out/index.html'),'utf8');
const runtime=options=>createRuntime({...options,sourceHtml:SOURCE_HTML});
const pairKey=(i,j)=>i<j?`${i}:${j}`:`${j}:${i}`;
const diagonal=(n,i,j)=>i!==j&&Math.min(Math.abs(i-j),n-Math.abs(i-j))>=2;
const scalarRng=seed=>{let s=seed>>>0;return()=>{s=(Math.imul(s,1664525)+1013904223)>>>0;return s/4294967296;};};
function shuffled(a,r){const b=a.slice();for(let i=b.length-1;i>0;i--){const j=Math.floor(r()*(i+1));[b[i],b[j]]=[b[j],b[i]];}return b;}
function full(rt){return rt.expose("({plate:S.plate,tutorial:S.tutorial,guide:S.discardGuide,serial:S.serial,history:S.history,play,geometry:geometry(),phase:S.phase,frozen:S.frozen,lastMisconceptionId:S.lastMisconceptionId,helpHidden:$('help')?.hidden,titleHidden:$('title')?.hidden})");}
function discardGesture(rt){const {geometry:g,play}=full(rt);rt.drag({x:g.cx,y:g.cy},{x:play.x+play.w-5,y:g.cy},{duration:.12});}
function solveCurrent(rt){const d=full(rt);if(d.plate.locked)return;
  if(d.plate.discard){discardGesture(rt);return;}
  const existing=new Set(d.plate.lines.map(l=>pairKey(...l.pair)));
  for(const [i,j]of d.plate.requiredPairs)if(!existing.has(pairKey(i,j)))rt.dragVertices(i,j,{duration:.10});
}
function handleTraining(rt){const d=full(rt);if(!d.tutorial&&!d.plate.guided)return false;
  if(!d.plate.locked)solveCurrent(rt);rt.advance(1.55,{frameMs:50});return true;
}
function installFixture(rt,kind,n,marked=0){rt.evaluate(`S.tutorial=false;S.frozen=false;S.discardGuide=false;S.discardTaught=true;S.lastMisconceptionId=null;install(makeProblem('${kind}',${n},${marked},0,0,0));`);}
function misconception(rt){const d=full(rt);return d.lastMisconceptionId||d.plate.bad?.misconceptionId||null;}

function functionalChecks(){
 const checks=[],runtimes=[];
 const rt=runtime({seed:1937});runtimes.push(rt);rt.click('begin');
 const before=rt.state();rt.advance(12,{frameMs:50});const after=rt.state();
 assert.equal(after.time,before.time);assert.equal(after.lives,before.lives);assert.equal(after.solved,0);assert.equal(after.frozen,true);
 rt.dragVertices(0,1);assert.equal(rt.state().lives,3);assert.equal(rt.state().frozen,true);
 rt.dragVertices(0,2);assert.equal(rt.state().solved,0);assert.equal(rt.storage['onboardingSeen:star-out'],'1');rt.advance(1.55,{frameMs:50});
 const firstMain=full(rt);assert.equal(firstMain.tutorial,false);assert.equal(firstMain.frozen,false);assert.equal(firstMain.plate.kind,'polygon_diag');assert.equal(firstMain.plate.n,4);
 const afterTutorial=rt.state();rt.evaluate('window.__GAME_TEST__.start()');const skipped=full(rt);
 assert.equal(skipped.tutorial,false);assert.equal(skipped.frozen,false);assert.equal(skipped.plate.kind,'polygon_diag');assert.equal(skipped.plate.n,4);assert.equal(rt.state().score,0);
 checks.push({name:'first_visit_tutorial_persists_then_main_square_and_restart_skips',pass:true,before,after,afterTutorial,firstMain:{id:firstMain.plate.id,kind:firstMain.plate.kind,n:firstMain.plate.n},skipped:{id:skipped.plate.id,kind:skipped.plate.kind,n:skipped.plate.n}});

 const qaHook=runtime({seed:1938});runtimes.push(qaHook);qaHook.evaluate('window.__GAME_TEST__.start()');const qaHookBefore=qaHook.state();qaHook.evaluate('window.__GAME_TEST__.answerCorrect()');qaHook.advance(.4,{frameMs:50});const qaHookAfter=qaHook.state();
 assert.ok(qaHookAfter.score>qaHookBefore.score,'__GAME_TEST__.answerCorrect() must increase score even from a clean first visit');
 checks.push({name:'qa_answerCorrect_increases_score_from_clean_origin',pass:true,before:qaHookBefore,after:qaHookAfter});
 const first=full(rt),beforeWrong=rt.state();rt.dragVertices(first.plate.marked,(first.plate.marked+1)%first.plate.n);const wrong=rt.state();
 assert.equal(wrong.lives,2);assert.equal(wrong.solved,0);assert.equal(misconception(rt),'side_is_diagonal');solveCurrent(rt);assert.equal(rt.state().solved,1);
 let safety=0;
 while(rt.state().phase==='playing'&&safety++<50){
   const d=full(rt);if(d.plate.locked){rt.advance(1.8,{frameMs:50});continue;}solveCurrent(rt);rt.advance(1.8,{frameMs:50});
 }
 assert.equal(rt.state().phase,'clear');assert.equal(rt.state().solved,8);assert.equal(rt.state().lives,2);
 assert.equal(rt.state().firstAttempts[0].firstCorrect,false);
 checks.push({name:'first_general_mistake_then_eight_locks_clear',pass:true,beforeWrong,wrong,final:rt.state(),events:rt.events.length});

 const star=runtime({seed:991,storage:{'onboardingSeen:star-out':'1'}});runtimes.push(star);star.click('begin');installFixture(star,'star_bonus',5);solveCurrent(star);
 assert.equal(full(star).plate.kind,'star_bonus');assert.equal(full(star).plate.locked,true);
 const scoreBefore=star.state().score;const p=full(star).plate;const occupied=new Set(p.lines.map(l=>pairKey(...l.pair)));
 for(let i=0;i<5;i++)for(let j=i+1;j<5;j++)if(diagonal(5,i,j)&&!occupied.has(pairKey(i,j)))star.dragVertices(i,j,{duration:.07,steps:2});
 assert.equal(star.state().stars,1);assert.equal(star.state().score,scoreBefore+300);assert.equal(full(star).plate.lines.length,5);
 assert.ok(star.storage['star-out:firstStarAt']);checks.push({name:'star_bonus_five_student_diagonals_make_star',pass:true,state:star.state(),lines:full(star).plate.lines,storage:star.storage,events:star.events.length});

 const ordinary=runtime({seed:992,storage:{'onboardingSeen:star-out':'1'}});runtimes.push(ordinary);ordinary.click('begin');installFixture(ordinary,'polygon_diag',5);solveCurrent(ordinary);
 const ordinaryBefore=ordinary.state(),ordinaryPlate=full(ordinary).plate,ordinaryOccupied=new Set(ordinaryPlate.lines.map(l=>pairKey(...l.pair)));
 for(let i=0;i<5;i++)for(let j=i+1;j<5;j++)if(diagonal(5,i,j)&&!ordinaryOccupied.has(pairKey(i,j)))ordinary.dragVertices(i,j,{duration:.07,steps:2});
 assert.equal(ordinary.state().stars,0);assert.equal(ordinary.state().score,ordinaryBefore.score);assert.equal(full(ordinary).plate.lines.length,ordinaryPlate.lines.length);assert.equal(ordinary.storage['star-out:firstStarAt'],undefined);
 checks.push({name:'ordinary_pentagon_cannot_award_star',pass:true,before:ordinaryBefore,after:ordinary.state(),lineCount:full(ordinary).plate.lines.length});

 // Isolated exact test fixture: game generator and installed plate, actual pointer path.
 const triangle=runtime({seed:714,storage:{'onboardingSeen:star-out':'1'}});runtimes.push(triangle);triangle.click('begin');installFixture(triangle,'triangle_discard',3);
 triangle.dragVertices(0,2);assert.equal(triangle.state().lives,2);const afterBad=triangle.state();discardGesture(triangle);
 assert.equal(full(triangle).plate.completed,true);assert.equal(triangle.state().lives,2);assert.equal(triangle.state().solved,0);
 assert.equal(triangle.state().score,0);assert.equal(triangle.state().combo,0);assert.equal(misconception(triangle),'triangle_has_diagonal');assert.equal(triangle.state().firstAttempts.at(-1).firstCorrect,false);
 checks.push({name:'triangle_stroke_has_misconception_then_discard_has_no_reward',pass:true,fixture:true,afterBad,afterSwipe:triangle.state(),misconceptionId:misconception(triangle)});

 const discardClean=runtime({seed:715,storage:{'onboardingSeen:star-out':'1'}});runtimes.push(discardClean);discardClean.click('begin');installFixture(discardClean,'not_polygon',5);
 const discardBefore=discardClean.state();discardGesture(discardClean);const discardAfter=discardClean.state();
 assert.equal(discardAfter.score,discardBefore.score);assert.equal(discardAfter.solved,discardBefore.solved);assert.equal(discardAfter.combo,discardBefore.combo);assert.equal(full(discardClean).plate.completed,true);
 checks.push({name:'clean_discard_completes_plate_without_score_solved_or_combo',pass:true,before:discardBefore,after:discardAfter});

 const misconceptionCases=[
   ['not_polygon',5,'closed_curve_is_polygon'],
   ['not_regular',5,'equal_sides_only'],
 ];
 for(const [kind,n,expected]of misconceptionCases){const probe=runtime({storage:{'onboardingSeen:star-out':'1'}});runtimes.push(probe);probe.click('begin');installFixture(probe,kind,n);const plate=full(probe).plate;probe.dragVertices(plate.marked,(plate.marked+2)%plate.n);assert.equal(misconception(probe),expected);checks.push({name:`runtime_misconception_${expected}`,pass:true,kind,misconceptionId:misconception(probe)});}

 const guide=runtime({seed:817});runtimes.push(guide);guide.click('begin');handleTraining(guide);solveCurrent(guide);guide.advance(1.5,{frameMs:50});
 assert.equal(full(guide).guide,true);const guideBefore=guide.state();guide.advance(12,{frameMs:50});const guideAfter=guide.state();
 assert.equal(guideAfter.time,guideBefore.time);assert.equal(guideAfter.lives,guideBefore.lives);assert.equal(full(guide).plate.elapsed,0);
 const trialsBefore=guide.state().firstAttempts.length;discardGesture(guide);const guideDiscard=guide.state();
 assert.equal(guideDiscard.score,guideBefore.score);assert.equal(guideDiscard.solved,guideBefore.solved);assert.equal(guideDiscard.combo,guideBefore.combo);assert.equal(guideDiscard.firstAttempts.length,trialsBefore);guide.advance(1.5,{frameMs:50});
 assert.equal(guide.state().frozen,false);
 checks.push({name:'first_discard_training_freezes_and_has_no_reward_or_history',pass:true,before:guideBefore,afterWait:guideAfter,afterDiscard:guideDiscard,afterAdvance:guide.state()});

 const reverse=runtime({seed:281,storage:{'onboardingSeen:star-out':'1'}});runtimes.push(reverse);reverse.click('begin');reverse.evaluate("S.tutorial=false;S.frozen=false;S.discardGuide=false;S.discardTaught=true;install(makeProblem('reverse_partial',6,2,1,0,1,1));");
 const reverseBefore=full(reverse).plate;assert.equal(reverseBefore.lines.length,1);solveCurrent(reverse);
 assert.equal(full(reverse).plate.lines.length,3);assert.equal(reverse.state().solved,1);assert.equal(reverse.state().lives,3);
 checks.push({name:'reverse_partial_keeps_given_line_and_requires_two_new_diagonals',pass:true,fixture:true,before:reverseBefore,after:full(reverse).plate,state:reverse.state()});

 const returning=runtime({storage:{'onboardingSeen:star-out':'1'}});runtimes.push(returning);returning.click('begin');assert.equal(full(returning).tutorial,false);assert.equal(full(returning).plate.n,4);
 checks.push({name:'returning_player_starts_at_main_square',pass:true,state:returning.state(),plate:full(returning).plate.id});
 const revisit=runtime({storage:{'onboardingSeen:star-out':'1'}});runtimes.push(revisit);assert.equal(revisit.dom('help').hidden,true);revisit.click('helpOpen');assert.equal(revisit.dom('help').hidden,false);revisit.click('helpClose');assert.equal(revisit.dom('help').hidden,true);revisit.click('begin');
 assert.equal(full(revisit).tutorial,true);assert.equal(full(revisit).frozen,true);assert.equal(full(revisit).plate.id,'practice-square');
 checks.push({name:'help_reopens_tutorial_for_returning_player',pass:true,state:revisit.state(),storage:revisit.storage});

 const death=runtime();runtimes.push(death);death.click('begin');handleTraining(death);const dp=full(death).plate;
 for(let i=0;i<3;i++)death.dragVertices(dp.marked,(dp.marked+1)%dp.n);
 assert.equal(death.state().phase,'gameover');assert.equal(death.state().lives,0);death.advance(100,{frameMs:50});assert.equal(death.state().phase,'gameover');assert.equal(death.state().lives,0);
 const deathState=death.state();death.evaluate('window.__GAME_TEST__.answerCorrect();window.__GAME_TEST__.answerWrong();');assert.deepEqual(death.state(),deathState);
 death.click('again');assert.equal(death.state().lives,3);assert.equal(death.state().frozen,false);assert.equal(full(death).tutorial,false);assert.equal(full(death).plate.n,4);
 checks.push({name:'zero_lives_terminal_and_single_retry_click',pass:true,afterRetry:death.state()});
 return {metadata:rt.metadata,checks,errors:runtimes.flatMap(x=>x.errors)};
}

// ONLY this visible observation is given to mindless policies. No kind, discard,
// requiredPairs, correctness, firstClean, or answer from the generator is exposed.
function visibleObservation(rt){return rt.expose(`(()=>{const p=S.plate,g=geometry();return {id:p.id,phase:S.phase,locked:p.locked,vertices:g.vertices,marked:p.marked,occupiedPairs:p.lines.map(l=>l.pair)}})()`);}
function chooseEndpoint(policy,obs,memory,rand){
 if(memory.id!==obs.id){memory.id=obs.id;memory.cycle=0;memory.chosen=null;memory.order=[];}
 const candidates=obs.vertices.map((_,i)=>i).filter(i=>i!==obs.marked);
 if(policy==='repeat'){
   // Always repeat the same visible endpoint for this plate: leftmost point.
   memory.chosen??=candidates.slice().sort((a,b)=>obs.vertices[a].x-obs.vertices[b].x)[0];return memory.chosen;
 }
 if(policy==='cycle'){
   // Fixed clockwise endpoint order, starting with the next visible corner.
   const order=candidates.sort((a,b)=>((a-obs.marked+obs.vertices.length)%obs.vertices.length)-((b-obs.marked+obs.vertices.length)%obs.vertices.length));
   return order[memory.cycle++%order.length];
 }
 if(policy==='random'){
   if(!memory.order.length){const occupied=new Set(obs.occupiedPairs.filter(p=>p.includes(obs.marked)).map(p=>p.find(i=>i!==obs.marked)));memory.order=shuffled(candidates.filter(i=>!occupied.has(i)),rand);}
   return memory.order.shift();
 }
 return null;
}
function choose(n,k){let x=1;for(let i=1;i<=k;i++)x=x*(n-k+i)/i;return x;}
function chanceFor(policy,trial){
 if(['not_polygon','not_regular','triangle_discard'].includes(trial.kind))return 0;
 const available=trial.n-1-trial.pre,needed=trial.n-3-trial.pre;
 if(policy==='random')return 1/choose(available,needed);
 // A repeated single endpoint cannot finish multi-line plates. The main-band
 // square now needs exactly one of three visible endpoints, so its opportunity
 // baseline is 1/3 across the enumerated rotations/marked vertices.
 if(policy==='repeat'&&needed===1)return 1/available;
 return 0;
}
function runBots(count=200){
 const rows=[];let metadata=null;
 for(const policy of ['repeat','cycle','random','idle']){
  const games=[],trials=[];const t0=performance.now();
  for(let game=0;game<count;game++){
   const seed=(0x91e10da5+Math.imul(game+1,2654435761))>>>0;
   const rt=runtime({seed});metadata??=rt.metadata;rt.click('begin');
   const rand=scalarRng(seed^0x5bd1e995),memory={};let steps=0;
   while(rt.state().phase==='playing'&&rt.now<180000&&steps++<5000){
    if(handleTraining(rt))continue;
    const obs=visibleObservation(rt);
    if(obs.locked){rt.advance(1.8,{frameMs:50});continue;}
    if(policy==='idle'){rt.advance(.5,{frameMs:50});continue;}
    const endpoint=chooseEndpoint(policy,obs,memory,rand);
    if(endpoint!==undefined&&endpoint!==null)rt.dragVertices(obs.marked,endpoint,{duration:.08,steps:2});
    rt.advance(.10,{frameMs:50});
   }
   const state=rt.state();assert.notEqual(state.phase,'playing',`${policy} seed ${seed}: did not terminate`);
   assert.equal(rt.errors.filter(e=>e.type==='error').length,0);
   const history=state.firstAttempts||full(rt).history;
   assert.ok(history.length>0);history.forEach(t=>trials.push({...t,game,seed,chance:chanceFor(policy,t)}));
   games.push({game,seed,phase:state.phase,solved:state.solved,lives:state.lives,firstTrials:history.length,firstCorrect:history.filter(h=>h.firstCorrect).length,events:rt.events.length,simulatedSeconds:rt.now/1000});
  }
  const wins=games.filter(g=>g.phase==='clear').length,correct=trials.filter(t=>t.firstCorrect).length;
  const expected=trials.reduce((s,t)=>s+t.chance,0),variance=trials.reduce((s,t)=>s+t.chance*(1-t.chance),0);
  const z=variance?(correct-expected)/Math.sqrt(variance):correct?Infinity:0;
  // The requirement rejects statistically significant uplift, not sampling noise.
  const significantUplift=z>1.644854;
  rows.push({policy,games:count,trials:trials.length,firstCorrect:correct,firstCorrectRate:correct/trials.length,chanceRate:expected/trials.length,expectedFirstCorrect:expected,zScore:z,
   significantUpliftOneSided5pct:significantUplift,completions:wins,completionRate:wins/count,pass:!significantUplift&&(policy!=='idle'||games.every(g=>g.solved===0)),runtimeMs:performance.now()-t0,gameDetails:games,trialDetails:trials});
  console.log(JSON.stringify({policy,...Object.fromEntries(Object.entries(rows.at(-1)).filter(([k])=>!['gameDetails','trialDetails'].includes(k)))}));
 }
 return {metadata,countPerPolicy:count,method:'Unchanged game-code in Node VM mocks, registered pointer handlers, queued RAF; not trusted browser events. Each game starts normally. Fixed square and the explicitly guided first discard are completed as training and excluded. Main inputs never inspect kind or answers. All arrived main plates terminate in completion/failure and are in the first-attempt denominator, even if an early error was later corrected.',
 chanceDefinitions:{repeat:'A repeated endpoint can finish only a one-line task. Main-band squares therefore use 1/3 (one diagonal among three endpoints); multi-line and discard plates use 0.',cycle:'0: the fixed clockwise order starts with the adjacent visible corner, so every plate has an error before it could complete. Never swipes discard plates.',random:'Uniform random permutation of visible endpoints excluding the marked start and preoccupied lines; for a drawable n-gon with p preinstalled lines, chance=1 / C(n-1-p,n-3-p). Discard plates receive strokes only so chance=0. This is stronger than random whole-screen coordinates because every stroke reaches a corner.',idle:'0: no main input is sent; plate expiry loses life. Guided actions are excluded.'},
 rows,pass:rows.every(r=>r.pass)};
}
const mode=process.argv[2]||'all';
if(mode==='functional'||mode==='all'){const result=functionalChecks();fs.writeFileSync(path.join(HERE,'functional-vm.json'),JSON.stringify(result,null,2)+'\n');console.log(`Functional checks: ${result.checks.length}, all pass; browserVerified=false`);}
if(mode==='bots'||mode==='all'){const result=runBots(Number(process.argv[3]||200));fs.writeFileSync(path.join(HERE,'bots-vm.json'),JSON.stringify(result,null,2)+'\n');if(!result.pass)process.exitCode=1;}
