// Original runtime scenery, revision 3. No game state or input dependencies.
import * as THREE from 'three';
export function dressArena(world,stoneGeometry){
 const groups=new Map(),tmp=new THREE.Object3D();
 const add=(kind,x,y,z,sx,sy,sz,color,rx=0,ry=0,rz=0)=>{if(!groups.has(kind))groups.set(kind,[]);groups.get(kind).push({x,y,z,sx,sy,sz,color,rx,ry,rz});};
 // Water-worn capstones: lighter lip, dark wet foot, and occasional moss between joints.
 for(const side of [-1,1])for(let j=0;j<22;j++){
  const x=-5.18+j*.49,z=-.45+side*.86;if(Math.abs(Math.abs(x)-2.45)<1.1)continue;
  add('stone',x,.37,z,.455,.09,.34,j%4===0?0xc7cabb:0xaebcb8,0,Math.sin(j)*.026);
  add('stone',x,.205,z-side*.14,.43,.1,.1,0x537e78);
  if(j%3===0)add('leaf',x+.16,.36,z+.1,.18,.065,.13,0x648754,0,j);
 }
 // The deck is still open: fine wood grain and forged straps sit flush with the planks.
 for(const x of [-2.45,2.45]){
  for(let j=0;j<6;j++)for(let k=0;k<2;k++)add('stone',x+(k-.5)*.72,.502,-1.28+j*.33+.055,.58,.012,.018,j%2?0x9e7647:0xa98851,0,Math.sin(j+k)*.03);
  for(const side of [-1,1]){
   add('stone',x+side*.79,.515,-.46,.09,.026,1.78,0x667d82);
   for(const z of[-1.24,-.25,.38])add('bolt',x+side*.79,.543,z,.054,.027,.054,0xd5cfae);
   for(const z of[-1.4,.48]){
    add('stone',x+side*1.04,1.065,z,.25,.035,.27,0xe9dcb6);
    add('bolt',x+side*1.04,1.1,z,.078,.07,.078,0xb99555);
   }
  }
 }
 // Low foundation blocks and grass soften the join where each fort meets the lawn.
 for(const x of[-2.45,2.45])for(const side of[-1,1])for(let j=0;j<4;j++){
  add('stone',x+side*1.24,.17,-5.93+j*.48,.24,.23,.435,j%3?0x929b8c:0xb4ba9f,0,side*.018);
  if(j===0||j===3)add('leaf',x+side*1.39,.18,-5.93+j*.48,.25,.11,.19,0x648351,0,j);
 }
 // Chipped edges and tiny lichen patches break perfect repetition without extra silhouettes.
 for(const side of[-1,1])for(let j=0;j<16;j++){
  const z=-7.1+j*.95;
  if(j%3===0)add('stone',side*5.34,.756,z,.15,.014,.26,0x929e94,0,j*.25);
  if(j%4===1)add('leaf',side*5.18,.135,z,.24,.035,.31,0x668251,0,j);
 }
 // Broad leaves and flower heads are grouped away from both troop lanes.
 const beds=[[-4.65,-6.5],[4.55,-5.95],[-4.7,-3.6],[4.75,-2.4],[-4.7,1.45],[4.65,1.55],[-4.65,4.6],[4.65,4.3],[-4.15,6.55],[4.3,6.65],[0,-6.7],[0,-2.4],[0,1.2]];
 beds.forEach(([x,z],i)=>{for(let j=0;j<7;j++){const a=j*2.4+i,dist=.1+(j%3)*.12,px=x+Math.cos(a)*dist,pz=z+Math.sin(a)*dist;
  add('leaf',px,.14+(j%3)*.025,pz,.12+(j%2)*.025,.17+(j%3)*.045,.052,[0x6f963e,0x8fae50,0x567b3d][j%3],0,a,(j%2?1:-1)*.55);
  if(j<3&&i%3===0){add('bolt',px,.3,pz,.06,.06,.06,j%2?0xf2df99:0xf3edc4);}
 }});
 // A few pale stones at the garden edge add scale without spilling onto the paths.
 beds.forEach(([x,z],i)=>{if(i%2===0)add('stone',x+.31,.16,z+.22,.14,.13,.2,0xbbbd9c,0,i*.7,.12);});
 const geometries={stone:stoneGeometry,leaf:new THREE.SphereGeometry(1,5,3),bolt:new THREE.SphereGeometry(1,6,4)};
 for(const[kind,entries]of groups){const mat=new THREE.MeshStandardMaterial({color:0xffffff,roughness:kind==='bolt'?.62:.97});const inst=new THREE.InstancedMesh(geometries[kind],mat,entries.length);entries.forEach((s,i)=>{tmp.position.set(s.x,s.y,s.z);tmp.rotation.set(s.rx,s.ry,s.rz);tmp.scale.set(s.sx,s.sy,s.sz);tmp.updateMatrix();inst.setMatrixAt(i,tmp.matrix);inst.setColorAt(i,new THREE.Color(s.color));});inst.castShadow=kind!=='leaf';inst.receiveShadow=true;world.add(inst);}
 // Paired curved glints follow the open center and outer channels only.
 const curves=new THREE.Group();world.add(curves);
 const glintMat=new THREE.MeshBasicMaterial({color:0xb6e6df,transparent:true,opacity:.38,depthWrite:false});
 for(let i=0;i<12;i++){const x=[-4.7,-.72,.37,4.47][i%4],z=-.95+Math.floor(i/4)*.45;const pts=[];for(let j=0;j<6;j++)pts.push(new THREE.Vector3(x+j*.08,.225,z+Math.sin(j/5*Math.PI)*.035));const line=new THREE.Mesh(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(pts),5,.012,3,false),glintMat);curves.add(line);}
 return{update(t){curves.position.x=Math.sin(t*.55)*.11;glintMat.opacity=.27+Math.sin(t*.8)*.08;}};
}
