import puppeteer from 'puppeteer';
import assert from 'node:assert/strict';
import {mkdir,readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {homedir} from 'node:os';
import {serveStatic} from '../../../factory/lib/static-server.mjs';
import {visibleProblem,state,actualClick,precisionHeight,keyboardHeight,touchPath,screenshotAndRecord} from './ui-helpers.mjs';
import {oracle} from './math-oracle.mjs';
if(process.env.SUNBASKET_BROWSER_SLOT!=='granted')throw new Error('Explicit parent browser-slot grant required');
const root=process.cwd(),base=`${root}/logs/manual-20260907-sunbasket-farm/validation`;
const output=process.env.SUNBASKET_UI_OUT||`${base}/revision-1/ui`;
if(!output.startsWith(base+'/'))throw new Error('Output must stay in owned validation directory');
await mkdir(output,{recursive:true});
const sha=buffer=>createHash('sha256').update(buffer).digest('hex');
const artifactFiles=['index.html','game.js','style.css','math.mjs'];
async function hashes(){const pairs=await Promise.all(artifactFiles.map(async name=>[name,sha(await readFile(`${root}/public/g/sunbasket-farm/${name}`))]));return Object.fromEntries(pairs);}
const sourceHashes=await hashes(),log=[],issues=[],errors=[],failedRequests=[];
const server=await serveStatic(`${root}/public`);
const browser=await puppeteer.launch({headless:true,executablePath:process.env.REVIEW_CHROME||`${homedir()}/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing`,args:['--disable-background-timer-throttling','--disable-renderer-backgrounding']});
let page;
const results={scope:'Real pointer/touch/button/keyboard paths. Test hook is read-only (state and projected rendering coordinates). The one normal campaign has an explicitly authorized random-stream fixture for maximum 144-cell coverage. Firstplay and random-policy checks are separate.',sourceHashes,fixture:null,normal:null,oneError:null,failure:null,bots:[],errors,failedRequests,issues};
const snap=(name,details={})=>screenshotAndRecord(page,output,name,log,details);
const waitPhase=phases=>page.waitForFunction(ps=>ps.includes(window.__GAME_TEST__?.getState?.().phase),{timeout:15000},phases);
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
async function newGamePage(viewport={width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true}){
 const next=await browser.newPage();
 next.on('pageerror',e=>errors.push(e.message));next.on('console',m=>{if(m.type()==='error')errors.push(m.text());});next.on('response',r=>{if(r.status()>=400)failedRequests.push({url:r.url(),status:r.status()});});
 await next.setViewport(viewport);await next.goto(`${server.url}/g/sunbasket-farm/`,{waitUntil:'networkidle0'});await next.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});await next.bringToFront();return next;
}
async function correctAllocation(mode){
 const problem=await visibleProblem(page),target=problem.calculated.expectedHeight;
 if(mode==='touch'){
  const layout=await page.evaluate(()=>window.__GAME_TEST__.getLayout());
  const corners=layout.actualFieldCorners,perRow={x:(corners[2].x-corners[1].x)/problem.visible.chosenHeight,y:(corners[2].y-corners[1].y)/problem.visible.chosenHeight};
  const h=layout.handle,from={x:h.x+h.width/2,y:h.y+h.height/2},to={x:from.x+perRow.x*(target-problem.visible.chosenHeight),y:from.y+perRow.y*(target-problem.visible.chosenHeight)};
  const path=Array.from({length:13},(_,i)=>({x:from.x+(to.x-from.x)*i/12,y:from.y+(to.y-from.y)*i/12}));
  await touchPath(page,path);const afterDrag=(await visibleProblem(page)).visible;
  if(target!==problem.visible.chosenHeight)assert.notEqual(afterDrag.chosenHeight,problem.visible.chosenHeight,'real handle touch drag must change height');
  await precisionHeight(page,target,{touch:true});
  return {...problem,input:'touch handle drag with visible-geometry mapping, optional one-metre button correction',afterDrag,actual:(await visibleProblem(page)).visible};
 }
 if(mode==='keyboard')await keyboardHeight(page,target);else await precisionHeight(page,target);
 return {...problem,input:mode,actual:(await visibleProblem(page)).visible};
}
async function inspectCropCoverage(){
 const layout=await page.evaluate(()=>window.__GAME_TEST__.getLayout());
 const coverage=await page.evaluate(points=>({width:innerWidth,height:innerHeight,total:points.length,outside:points.filter(p=>p.x<0||p.x>=innerWidth||p.y<0||p.y>=innerHeight),blocked:points.map(p=>({index:p.index,x:p.x,y:p.y,element:document.elementFromPoint(p.x,p.y)?.id??null})).filter(p=>p.element!=='farm-canvas')}),layout.crops);
 assert.equal(coverage.outside.length,0,'all unharvested crop centres must be inside the screen');
 assert.equal(coverage.blocked.length,0,'all crop centres must receive actual canvas input');
 const diagnostic=await state(page);assert.equal(layout.crops.length,diagnostic.harvestTotal-diagnostic.harvested);
 return {layout,coverage};
}
async function harvest(mode,name,{detailed=false,capacity=false}={}){
 await waitPhase(['harvest']);const before=await state(page),coverage=await inspectCropCoverage();
 if(detailed)await snap(`${name}-ripe`,{coverage:coverage.coverage});
 let duplicate=null,firstRow=null;
 if(mode==='touch'){
  if(capacity){
   const p=coverage.layout.crops[0];await page.touchscreen.tap(p.x,p.y);const once=await state(page);
   assert.ok(once.harvested>0&&once.harvested<once.harvestTotal,'capacity crop tap should yield a partial harvest');
   await page.touchscreen.tap(p.x,p.y);const twice=await state(page);assert.equal(twice.harvested,once.harvested,'same crop area must not yield twice');
   duplicate={point:p,once,twice};await snap(`${name}-partial-duplicate-safe`,{duplicate});
  }
  const remaining=await inspectCropCoverage();
  const rows=new Map();for(const p of remaining.layout.crops){if(!rows.has(p.row))rows.set(p.row,[]);rows.get(p.row).push(p);}
  const snake=[...rows].sort((a,b)=>a[0]-b[0]).flatMap(([row,points])=>points.sort((a,b)=>row%2?b.col-a.col:a.col-b.col));
  await touchPath(page,snake);
 }else{
  for(let row=0;row<13;row++){
   const old=await state(page);if(old.phase!=='harvest')break;
   await page.keyboard.press('Space');const next=await state(page);
   assert.equal(next.harvested-old.harvested,old.width,'SPACE harvests one previously unharvested row');
   if(row===0){firstRow={before:old,after:next};if(detailed)await snap(`${name}-space-row`,{firstRow});}
  }
 }
 const afterHarvest=await state(page);assert.equal(afterHarvest.harvested,before.harvestTotal);assert.equal(afterHarvest.firstTry,before.firstTry);assert.equal(afterHarvest.firstAttempts,before.firstAttempts);assert.equal(afterHarvest.allAttempts,before.allAttempts);assert.equal(afterHarvest.score,before.score);
 if(detailed)await snap(`${name}-cart-loaded`);
 await waitPhase(['success']);const delivered=await state(page);
 assert.equal(delivered.solved,before.solved+1);assert.equal(delivered.coins-before.coins,before.harvestTotal);assert.equal(delivered.score,before.score);
 return {input:mode,before,coverage:coverage.coverage,duplicate,firstRow,afterHarvest,delivered};
}
async function runCampaign(name,{wrongRound=null,allocation='touch',harvestInput='touch'}={}){
 const rounds=[];
 for(let round=1;round<=9;round++){
  await waitPhase(['playing']);assert.equal((await state(page)).round,round);
  let mistake=null;
  if(round===wrongRound){
   const p=await visibleProblem(page),wrong=p.calculated.expectedHeight===12?11:p.calculated.expectedHeight+1;
   await precisionHeight(page,wrong);const before=await state(page);await actualClick(page,'#plant');await waitPhase(['retry']);const after=await state(page);
   assert.equal(after.lives,before.lives-1);assert.equal(after.firstTry,before.firstTry);assert.equal(after.firstAttempts,before.firstAttempts+1);
   await snap(`${name}-r${round}-wrong`,{visible:p.visible,oracle:oracle(p.visible,wrong),before,after});mistake={before,after,wrong};
  }
  const input=await correctAllocation(allocation),prepared=await state(page);
  assert.equal(input.actual.chosenHeight,input.calculated.expectedHeight);
  if(round===1||round===8||mistake)await snap(`${name}-r${round}-allocated`,{input});
  await actualClick(page,'#plant',{touch:allocation==='touch'});await waitPhase(['growing','harvest']);
  const afterPlant=await state(page);assert.equal(afterPlant.allAttempts,prepared.allAttempts+1);
  const capacity=afterPlant.harvestTotal===144;
  if(round===1)await snap(`${name}-r1-growing`);
  const harvestResult=await harvest(harvestInput,`${name}-r${round}`,{detailed:round===1||capacity,capacity});
  const after=await state(page);await snap(`${name}-r${round}-delivered`);
  rounds.push({round,input,mistake,prepared,afterPlant,harvest:harvestResult,after});
  await actualClick(page,'#plant',{touch:allocation==='touch'});
 }
 await waitPhase(['won']);const result=await state(page);
 assert.equal(result.solved,9);assert.equal(result.firstAttempts,8);assert.equal(result.firstTry,wrongRound?7:8);assert.equal(result.lives,wrongRound?2:3);assert.equal(result.score,wrongRound?810:840);assert.equal(result.allAttempts,wrongRound?10:9);assert.equal(result.visibleDecorations,3);
 await snap(`${name}-won`);console.log(JSON.stringify({event:'campaign-complete',name,result}));
 return {rounds,result};
}
async function completeTutorial(mode='keyboard'){
 await waitPhase(['playing']);await correctAllocation(mode);await actualClick(page,'#plant');await harvest('keyboard','guided');await actualClick(page,'#plant');await waitPhase(['playing']);assert.equal((await state(page)).tutorial,false);
}
async function blurCancelsHold(){
 const problem=await visibleProblem(page);await precisionHeight(page,3);
 const b=await(await page.$('#height-plus')).boundingBox();await page.mouse.move(b.x+b.width/2,b.y+b.height/2);await page.mouse.down();await delay(430);
 const other=await browser.newPage();await other.goto('about:blank');await other.bringToFront();await delay(100);const blurred=await state(page);await delay(420);const stable=await state(page);assert.equal(stable.height,blurred.height);assert.equal(stable.inputCount,blurred.inputCount);
 await page.bringToFront();await page.mouse.up();await other.close();await snap('blur-cancels-hold',{problem,blurred,stable});return {blurred,stable};
}
try{
 page=await newGamePage();await snap('title');
 const fixture=JSON.parse(await readFile(`${base}/normal-capacity-fixture.json`,'utf8'));results.fixture={seed:fixture.seed,capacityOrder:fixture.capacityOrder,scope:fixture.scope,file:`${base}/normal-capacity-fixture.json`};
 // Authorized test fixture controls only future shuffle draws. It does not access or write game state or answers.
 await page.evaluate(values=>{let cursor=0;window.__reviewOriginalRandom=Math.random;Math.random=()=>values[cursor++%values.length];},fixture.stream);
 await actualClick(page,'#start',{touch:true});await waitPhase(['playing']);await snap('started');
 const idleBefore=await state(page);await delay(2000);const idleAfter=await state(page);assert.equal(idleAfter.allAttempts,0);assert.equal(idleAfter.solved,0);assert.equal(idleAfter.height,2);results.noInput={before:idleBefore,after:idleAfter,dwellMs:2000};
 results.normal=await runCampaign('normal',{allocation:'touch',harvestInput:'touch'});
 assert.ok(results.normal.rounds.some(r=>r.harvest.before.harvestTotal===144),'normal fixture must actually reach capacity');
 results.storageBeforeReload=await state(page);await page.reload({waitUntil:'networkidle0'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});results.storageAfterReload=await state(page);assert.equal(results.storageAfterReload.wins,1);assert.equal(results.storageAfterReload.visibleDecorations,3);await snap('saved-farm-reloaded');
 // Reload removes the controlled random stream; all following days use the game's normal Math.random.
 await page.setViewport({width:1280,height:800,deviceScaleFactor:1,isMobile:false,hasTouch:false});await page.bringToFront();await actualClick(page,'#start');await waitPhase(['playing']);results.blur=await blurCancelsHold();
 results.reusedRegression={report:'logs/manual-20260907-sunbasket-farm/build-proof/full-ui-report.json',script:'logs/manual-20260907-sunbasket-farm/build-proof/full-ui.mjs',scope:'Builder evidence inspected independently: one ordinary error then full 9-order completion, keyboard row harvest, storage, and three tutorial errors followed by restart. Synthetic blur in builder evidence is supplemented above by a real browser-tab focus transfer. No repeat of the second full campaign in this independent run.'};
 const policies=[['initial-two',()=>2],['fixed-six',()=>6],['maximum-twelve',()=>12],['uniform-integer-1-to-12',i=>1+(createHash('sha256').update(`sunbasket-ui-policy:${i}`).digest().readUInt32BE(0)%12)]];
 for(const [policy,choose]of policies){
  // Fresh real page/start keeps tutorial setup separate and uses unmodified production randomness.
  await page.close();page=await newGamePage({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});await actualClick(page,'#start',{touch:true});await completeTutorial();
  const firstAttempts=[];
  for(let i=0;i<3;i++){
   const p=await visibleProblem(page),height=choose(i),expected=oracle(p.visible,height);await precisionHeight(page,height);const before=await state(page);await actualClick(page,'#plant');const after=await state(page);
   assert.equal(after.firstAttempts,before.firstAttempts+1);assert.equal(after.firstTry-before.firstTry,expected.correct?1:0);
   firstAttempts.push({ordinaryAttempt:i+1,visible:p.visible,height,expected:expected.correct,before,after});
   if(after.phase==='lost')break;
   if(!expected.correct){await correctAllocation('keyboard');await actualClick(page,'#plant');}
   await harvest('keyboard',`bot-${policy}-${i+1}`);if(i<2){await actualClick(page,'#plant');await waitPhase(['playing']);}
  }
  results.bots.push({policy,firstAttempts,actualCorrect:firstAttempts.filter(a=>a.after.firstTry>a.before.firstTry).length,expectedCorrect:firstAttempts.filter(a=>a.expected).length,interpretation:'Small real-input agreement probe only; tutorial and repair submissions excluded. Compare with policy-specific source distribution; no population accuracy estimate from these samples.'});await snap(`policy-${policy}-end`);
 }
 results.finalSourceHashes=await hashes();assert.deepEqual(results.finalSourceHashes,sourceHashes,'production files changed during UI run');assert.equal(errors.length,0);assert.equal(failedRequests.length,0);results.verdict='pass';
}catch(error){results.verdict='fail';issues.push({message:error.message,stack:error.stack});try{await snap('failure-diagnostic');}catch{}console.error(error.stack);process.exitCode=1;}
finally{results.completedAt=new Date().toISOString();await writeFile(`${output}/results.json`,JSON.stringify(results,null,2)+'\n');await browser.close();await server.close();}
console.log(JSON.stringify({verdict:results.verdict,issues,errors,failedRequests,output}));
