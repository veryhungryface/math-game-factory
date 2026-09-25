import fs from 'node:fs';import vm from 'node:vm';import crypto from 'node:crypto';
const source=fs.readFileSync('public/g/pane-wipe/engine.js','utf8'),box={};vm.createContext(box);vm.runInContext(source,box);const E=box.PaneWipeEngine;
const report={engineSha256:crypto.createHash('sha256').update(source).digest('hex'),reason:'The prespecified 200-game spatial audit flagged fractions 0.6 and 0.75. Retain those results and test these two policies only on 2000 new, non-overlapping seeds; no code or threshold changed.',seedRange:[50001,52000],baseline:'Independent uniform destination on the visible target segment, with the +/-4 degree interval clipped to segment boundaries.',policies:[]};
function endpointChance(p){if(p.requiresDiscard)return 0;const g=E.geometry(p),l=Math.hypot(g.lines[0].b.x-g.lines[0].a.x,g.lines[0].b.y-g.lines[0].a.y),u={x:(g.lines[0].b.x-g.lines[0].a.x)/l,y:(g.lines[0].b.y-g.lines[0].a.y)/l},s=(g.foot.x-g.lines[0].a.x)*u.x+(g.foot.y-g.lines[0].a.y)*u.y,w=g.gap*Math.tan(4*Math.PI/180);return Math.max(0,Math.min(l,s+w)-Math.max(0,s-w))/l;}
for(const fraction of [.6,.75]){
  const r={fraction,games:2000,firstCorrect:0,firstAttempts:0,expectedCorrect:0,variance:0,completed:0,runs:[]};
  for(let seed=50001;seed<=52000;seed++){
    const g=E.create(seed);g.start({tutorial:false});let attempts=0;
    for(let i=0;i<200;i++){
      const before=g.getState();if(['won','lost'].includes(before.phase))break;if(before.phase==='feedback'){g.tick(3);continue;}
      const p=g.current(),geom=E.geometry(p),a=geom.handle,b={x:geom.lines[0].a.x+(geom.lines[0].b.x-geom.lines[0].a.x)*fraction,y:geom.lines[0].a.y+(geom.lines[0].b.y-geom.lines[0].a.y)*fraction};g.submit({type:'stroke',a,b,points:[a,b]});
      const after=g.getState();if(after.firstAttempts>attempts){const chance=endpointChance(p);r.expectedCorrect+=chance;r.variance+=chance*(1-chance);attempts=after.firstAttempts;}g.tick(.6);
    }
    const s=g.getState();r.firstCorrect+=s.firstCorrect;r.firstAttempts+=s.firstAttempts;r.completed+=Number(s.phase==='won');r.runs.push({seed,phase:s.phase,firstCorrect:s.firstCorrect,firstAttempts:s.firstAttempts});
  }
  r.firstRate=r.firstCorrect/r.firstAttempts;r.chanceRate=r.expectedCorrect/r.firstAttempts;r.z=(r.firstCorrect-r.expectedCorrect)/Math.sqrt(r.variance);r.significantExcess=r.z>1.645;r.rawAtOrBelowChance=r.firstRate<=r.chanceRate;report.policies.push(r);
}
report.verdict=report.policies.some(x=>x.significantExcess)?'fail':'no_excess_on_independent_confirmation';fs.writeFileSync('logs/pane-wipe-build/audit-spatial-confirm.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,policies:report.policies.map(({runs,...r})=>r)},null,2));if(report.verdict==='fail')process.exitCode=1;
