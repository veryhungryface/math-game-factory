// This test helper reads only rendered point locations: no answerCorrect and no
// problem-generator answer fields. Equality tolerance here allows raster rounding;
// production grading separately uses integer model lengths.
export const edgeVertices = [[0, 1], [1, 2], [2, 0]];
export function visibleTriangle(vertices) {
  const lengths = edgeVertices.map(([a,b]) => Math.hypot(vertices[a].x-vertices[b].x, vertices[a].y-vertices[b].y));
  const pairs = [[0,1],[0,2],[1,2]].filter(([a,b]) => Math.abs(lengths[a]-lengths[b]) < 0.75);
  const angleDots = vertices.map((v,i) => {
    const a=vertices[(i+1)%3], b=vertices[(i+2)%3];
    const ax=a.x-v.x, ay=a.y-v.y, bx=b.x-v.x, by=b.y-v.y;
    return (ax*bx+ay*by) / (Math.hypot(ax,ay)*Math.hypot(bx,by));
  });
  const largest = angleDots.indexOf(Math.min(...angleDots));
  const angleClass = angleDots[largest] < -0.0001 ? 'obtuse' : angleDots[largest] > 0.0001 ? 'acute' : 'right';
  return { lengths, equalPairs: pairs, equilateral: pairs.length===3, isosceles:pairs.length>0, angleDots, largest, angleClass };
}
export function pairMotion(pair) {
  const a=edgeVertices[pair[0]], b=edgeVertices[pair[1]];
  const anchor=a.find(i=>b.includes(i));
  return {anchor, first:a.find(i=>i!==anchor), second:b.find(i=>i!==anchor)};
}
export async function pointerDrag(page,a,b,ms=220) {
  await page.mouse.move(a.x,a.y);
  await page.mouse.down();
  const steps=6;
  for(let i=1;i<=steps;i++) {
    await page.mouse.move(a.x+(b.x-a.x)*i/steps,a.y+(b.y-a.y)*i/steps);
    await new Promise(r=>setTimeout(r,ms/steps));
  }
  await page.mouse.up();
}
