// Fixed-size diagnostic counters, separate from __GAME_TEST__.
mergeInto(LibraryManager.library, {
 HYEOPGOK_ArtAllocation: function(scope,bytes) {
  var p=window.__HYEOPGOK_ART_PROBE__;
  if(!p)p=window.__HYEOPGOK_ART_PROBE__={counts:new Float64Array(4),zero:new Float64Array(4),totals:new Float64Array(4),max:new Float64Array(4),positive:new Float64Array(4),frames:new Float64Array(8192),at:0};
  p.counts[scope]++;p.totals[scope]+=bytes;p.max[scope]=Math.max(p.max[scope],bytes);
  if(bytes===0)p.zero[scope]++;else p.positive[scope]++;
  if(scope===0){p.frames[p.at]=bytes;p.at=(p.at+1)%8192;}
 },
 HYEOPGOK_PushV3Layout: function(jsonPtr) {
  var layout=JSON.parse(UTF8ToString(jsonPtr));
  function rect(flat,at,visible,id){return {id:id,visible:!!visible,active:!!visible,rect:{x:(flat[at]||0)*innerWidth,y:(flat[at+1]||0)*innerHeight,width:(flat[at+2]||0)*innerWidth,height:(flat[at+3]||0)*innerHeight}};}
  function snapshot(){
   var state=window.__GAME_TEST__&&window.__GAME_TEST__.getState?window.__GAME_TEST__.getState():{};
   var sx=innerWidth/Math.max(1,layout.screenWidth||innerWidth),sy=innerHeight/Math.max(1,layout.screenHeight||innerHeight);
   var q=JSON.parse(JSON.stringify(layout.question||{}));
   function scaleRect(r){if(!r)return;r.x*=sx;r.width*=sx;r.y*=sy;r.height*=sy;}
   scaleRect(q.panelRect);scaleRect(q.bodyRect);scaleRect(q.renderedTextRect);(q.glyphs||[]).forEach(function(g){scaleRect(g.rect);});
   var choices=[],upgrades=[],levels=state.towerLevels||[],cp=state.choicePadRects||[],up=state.upgradePadRects||[];
   for(var i=0;i<4;i++)choices.push(rect(cp,i*4,state.phase==='playing','choice-'+i));
   for(var j=0;j<3;j++)upgrades.push(rect(up,j*4,state.phase==='playing'&&levels[j]>=0,'upgrade-'+j));
   var fp=state.frontLineScreen||[],points=[];for(var k=0;k<4;k++)points.push({x:(fp[k*3]||0)*innerWidth,y:(fp[k*3+1]||0)*innerHeight,visible:(fp[k*3+2]||0)>0});
   var raw=state.shadowSampleScreen||[],types=['king','soldier','building','tree'],samples=[];
   for(var s=0;s<4;s++){var at=s*11,ring=[];for(var n=0;n<4;n++)ring.push({x:(raw[at+3+n*2]||0)*innerWidth,y:(raw[at+4+n*2]||0)*innerHeight});samples.push({id:types[s]+'-'+s,type:types[s],visible:(raw[at]||0)>0,foot:{x:(raw[at+1]||0)*innerWidth,y:(raw[at+2]||0)*innerHeight},ring:ring});}
   var kr=state.kingRect||[0,0,0,0],kingRect={x:kr[0]*innerWidth,y:kr[1]*innerHeight,width:kr[2]*innerWidth,height:kr[3]*innerHeight};
   return {version:3,question:q,choicePadRects:choices,upgradePadRects:upgrades,frontLine:{visible:points.every(function(p){return p.visible;}),points:points},shadowSamples:samples,kingRect:kingRect,kingScreen:state.kingScreen||null};
  }
  var api={version:3,snapshot:snapshot};window.__HYEOPGOK_V3_DEBUG__=api;window.__HYEOPGOK_ART_DEBUG__=api;
 }
});
