import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRuntime} from './runtime-vm.mjs';
const root=process.cwd(),dir=path.join(root,'public/g/star-out'),html=fs.readFileSync(path.join(dir,'index.html'),'utf8'),meta=JSON.parse(fs.readFileSync(path.join(dir,'meta.json'))),curr=JSON.parse(fs.readFileSync(path.join(root,'curriculum/2022-elementary-math.json')));
const checks=[];const add=(name,ok,detail)=>checks.push({name,pass:!!ok,detail});
for(const [name,w,h]of [['thumb.png',1200,630],['square.png',1080,1080]]){const b=fs.readFileSync(path.join(dir,name));add(name,b.readUInt32BE(16)===w&&b.readUInt32BE(20)===h,{width:b.readUInt32BE(16),height:b.readUInt32BE(20)})}
add('unit',curr.units.some(u=>u.id===meta.unit.id),meta.unit.id);
const unit=curr.units.find(u=>u.id===meta.unit.id);add('standards',meta.standards.every(s=>unit.standards.includes(s)),meta.standards);
add('qa_unreviewed',meta.qa.score===0&&meta.qa.gate===80&&meta.qa.passed===false,meta.qa);
const refs=[...html.matchAll(/(?:src|href)=["']([^"']+)["']/g)].map(m=>m[1]);add('local_asset_references',refs.every(r=>r.startsWith('./')&&fs.existsSync(path.join(dir,r))),refs);
add('no_remote_requests',!/(?:https?:)?\/\//.test(html.replace(/\/\/[^\n]*/g,'')),null);
add('no_interval_loop',!html.includes('setInterval'),null);
const buttons=[...html.matchAll(/<button\b([^>]*)>/gi)].map(m=>({tag:m[0],id:m[1].match(/\bid=["']([^"']+)["']/i)?.[1]||''}));
const chromeButtonIds=new Set(['begin','again','sound','helpOpen','helpClose']);
const unexpectedButtons=buttons.filter(b=>!chromeButtonIds.has(b.id));
add('controls.no_answer_buttons',unexpectedButtons.length===0&&['begin','again','sound'].every(id=>buttons.some(b=>b.id===id)),{ids:buttons.map(b=>b.id),unexpected:unexpectedButtons});
add('motion_preference',html.includes('prefers-reduced-motion'),null);
add('onboarding.help_revisit_markup',buttons.some(b=>b.id==='helpOpen')&&buttons.some(b=>b.id==='helpClose')&&/<section\s+id=["']help["'][^>]*\brole=["']dialog["'][^>]*\bhidden\b/i.test(html),{ids:buttons.map(b=>b.id)});

const model=createRuntime({sourceHtml:html,storage:{'onboardingSeen:star-out':'1'}});
const structure=model.expose(`(()=>{const square=POOLS['polygon_diag-4']||[],sample=square.length?sampleProblem(square[0],0):null;return {squareCount:square.length,firstMain:orderFor(1),fixed:fixedPlate(),sample:sample&&{n:sample.n,kind:sample.kind,figureFacts:sample.figureFacts}}})()`);
add('progression.square_in_main_pool',structure.squareCount>0&&structure.firstMain==='polygon_diag-4'&&structure.fixed.id==='practice-square'&&structure.sample?.n===4&&structure.sample?.kind==='polygon_diag'&&structure.sample?.figureFacts?.vertexLabels?.length===4,structure);
const sizes=[[320,720],[390,844],[820,1180],[1280,800],[1280,1400],[1920,1080]];const layouts=[];
for(const [width,height]of sizes){const rt=createRuntime({width,height,sourceHtml:html});rt.click('begin');const r=rt.expose(`(()=>{let minX=Infinity,maxX=-Infinity,minY=Infinity,maxY=-Infinity,minDistance=Infinity;for(const p of Object.values(POOLS).flat()){install(p);for(const elapsed of [0,p.duration||8]){S.plate.elapsed=elapsed;const g=geometry();for(let i=0;i<g.vertices.length;i++){const v=g.vertices[i];minX=Math.min(minX,v.x-27);maxX=Math.max(maxX,v.x+27);minY=Math.min(minY,v.y-27);maxY=Math.max(maxY,v.y+27);for(let j=i+1;j<g.vertices.length;j++)minDistance=Math.min(minDistance,Math.hypot(v.x-g.vertices[j].x,v.y-g.vertices[j].y));}}}return{W,H,land,play,minX,maxX,minY,maxY,minDistance}})()`);const pass=r.minX>=0&&r.maxX<=r.W&&r.minY>=0&&r.maxY<=r.H&&r.minDistance>=56;layouts.push({viewport:[width,height],...r,pass});}
add('canvas_geometry_contained',layouts.every(x=>x.pass),layouts);
const output={at:new Date().toISOString(),mode:'static and offline canvas geometry only',browserVerified:false,sourceSha256:crypto.createHash('sha256').update(html).digest('hex'),checks,pass:checks.every(x=>x.pass),limitations:'Does not validate actual CSS layout, pixels, trusted browser input, network responses, or FPS. Official QA/firstplay failed to start their localhost server (EPERM).'};
fs.writeFileSync('logs/star-out-build/static-audit.json',JSON.stringify(output,null,2)+'\n');console.log(JSON.stringify({pass:output.pass,checks:checks.length,failed:checks.filter(x=>!x.pass),geometry:layouts.map(x=>({viewport:x.viewport,pass:x.pass,minSpacing:x.minDistance}))}));if(!output.pass)process.exitCode=1;
