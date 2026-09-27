// Fixed-size diagnostic counters, separate from __GAME_TEST__.
mergeInto(LibraryManager.library, {
 HYEOPGOK_ArtAllocation: function(scope,bytes) {
  var p=window.__HYEOPGOK_ART_PROBE__;
  if(!p)p=window.__HYEOPGOK_ART_PROBE__={counts:new Float64Array(4),zero:new Float64Array(4),totals:new Float64Array(4),max:new Float64Array(4),positive:new Float64Array(4),frames:new Float64Array(8192),at:0};
  p.counts[scope]++;p.totals[scope]+=bytes;p.max[scope]=Math.max(p.max[scope],bytes);
  if(bytes===0)p.zero[scope]++;else p.positive[scope]++;
  if(scope===0){p.frames[p.at]=bytes;p.at=(p.at+1)%8192;}
 }
});
