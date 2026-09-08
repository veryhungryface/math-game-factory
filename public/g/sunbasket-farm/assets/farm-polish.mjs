import * as THREE from 'three';

// Reusable scenery only: all dimensions and paths are supplied independently of game state.
// The exclusion rectangle includes the full 12 × 12 field, labels and front drag handle.
export const FARM_LAYOUT=Object.freeze({
 fieldClearance:{minX:-4.85,maxX:4.85,minZ:-5.15,maxZ:5.65},
 paths:[
  {points:[[-7,-8],[-5.45,-5],[-5.3,0],[-5.5,4],[-2.5,6.3],[.5,7.3],[5.5,6.1],[8,3.8]],width:1.28},
  {points:[[4.7,5.2],[3,6.2],[1.4,7.4],[-.5,7.7]],width:1.02}
 ],
 meadowBeds:[[-7.1,-5.2,1.05],[-6.6,-1.5,.9],[-6.6,1.7,1.05],[-6.9,4.35,1.0],[-4.7,6.5,.85],[2.0,6.15,.55],[5.95,3.7,.66],[6.65,.65,1.0],[6.3,-2.4,.95],[5.85,-5.4,.6],[1.2,-6.55,.83],[-2.0,-6.65,.72]],
 yards:[{x:-5.65,z:-5.55,rx:1.55,rz:1.12,color:0xc0ae7d},{x:.1,z:7.3,rx:2.28,rz:1.36,color:0xccb986}],
 orchardBeds:[[-7.2,-3.7,1.12],[-7.2,.3,1.1],[-7,3.5,1.1],[7.5,-3.8,1.2],[7.7,.5,1.05],[6.8,7.1,.9]],
 hills:[[-17,-17,6.5,0x91b77d],[-6,-21,8.5,0x9fc68d],[9,-20,7.7,0x88b282],[20,-14,7.0,0x99bf8b]]
});
const tmp=new THREE.Object3D();
function batch(parent,geometry,rows,options={}){if(!rows.length)return;const m=new THREE.InstancedMesh(geometry,new THREE.MeshStandardMaterial({color:0xffffff,roughness:options.roughness??.96,side:options.doubleSide?THREE.DoubleSide:THREE.FrontSide,vertexColors:!!geometry.attributes.color}),rows.length);rows.forEach((r,i)=>{tmp.position.set(r.x??0,r.y??0,r.z??0);tmp.rotation.set(r.rx??0,r.ry??0,r.rz??0);tmp.scale.set(r.sx??r.s??1,r.sy??r.s??1,r.sz??r.s??1);tmp.updateMatrix();m.setMatrixAt(i,tmp.matrix);m.setColorAt(i,new THREE.Color(r.color??0xffffff));});m.castShadow=options.castShadow??false;m.receiveShadow=true;parent.add(m);return m;}
function leafGeometry(){
 const positions=[],colors=[],indices=[],segments=10,sides=4;
 for(let row=0;row<=segments;row++){
  const t=row/segments,width=Math.sin(Math.PI*t)**.85*.067,arc=Math.sin(t*Math.PI*.91)*.135;
  for(let col=0;col<=sides;col++){
   const across=col/sides*2-1,ridge=(1-across*across)*Math.sin(t*Math.PI)*.022;
   positions.push(t*.38,arc+ridge,across*width+Math.sin(t*Math.PI)*.018);
   const light=.79+(1-Math.abs(across))*.16+t*.045;colors.push(light,light,light*.93);
   if(row<segments&&col<sides){const i=row*(sides+1)+col;indices.push(i,i+1,i+sides+1,i+1,i+sides+2,i+sides+1);}
  }
 }
 const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(positions,3));g.setAttribute('color',new THREE.Float32BufferAttribute(colors,3));g.setIndex(indices);g.computeVertexNormals();return g;
}
let featherMap;
function groundFeather(){if(featherMap)return featherMap;const c=document.createElement('canvas');c.width=c.height=96;const ctx=c.getContext('2d'),g=ctx.createRadialGradient(48,48,28,48,48,47);g.addColorStop(0,'white');g.addColorStop(.55,'#ddd');g.addColorStop(1,'black');ctx.fillStyle=g;ctx.fillRect(0,0,96,96);featherMap=new THREE.CanvasTexture(c);return featherMap;}

function surface(parent,geometry,color){const m=new THREE.Mesh(geometry,new THREE.MeshStandardMaterial({color,roughness:1,vertexColors:!!geometry.attributes.color,...(geometry.userData.feather?{alphaMap:groundFeather(),transparent:true,depthWrite:false}:{} )}));m.receiveShadow=true;parent.add(m);return m;}
function island(x,z,rx,rz,y,seed=0){const shape=new THREE.Shape();for(let i=0;i<=32;i++){const a=i/32*Math.PI*2,r=1+Math.sin(i*2.17+seed)*.05+Math.cos(i*.61+seed)*.035,px=x+Math.cos(a)*rx*r,pz=z+Math.sin(a)*rz*r;i?shape.lineTo(px,-pz):shape.moveTo(px,-pz);}const g=new THREE.ShapeGeometry(shape,32);g.rotateX(-Math.PI/2);g.translate(0,y,0);const pos=g.attributes.position,uv=[];for(let i=0;i<pos.count;i++)uv.push((pos.getX(i)-x)/(rx*2.14)+.5,(pos.getZ(i)-z)/(rz*2.14)+.5);g.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));g.userData.feather=true;return g;}
function ribbon(curve,width,y,offset=0){const p=[],c=[],idx=[],up=new THREE.Vector3(0,1,0);for(let i=0;i<=100;i++){const at=i/100,q=curve.getPoint(at),n=new THREE.Vector3().crossVectors(curve.getTangent(at),up).normalize(),wave=1+Math.sin(i*1.13)*.035;for(const side of[-1,1]){const d=offset+side*width*.5*wave;p.push(q.x+n.x*d,y,q.z+n.z*d);const v=.95+Math.sin(i*.7)*.045;c.push(v,v,v);}if(i<100){const j=i*2;idx.push(j,j+1,j+2,j+1,j+3,j+2);}}const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(p,3));g.setAttribute('color',new THREE.Float32BufferAttribute(c,3));g.setIndex(idx);g.computeVertexNormals();return g;}
function seeded(seed){return()=>((seed=seed*16807%2147483647)/2147483647);}
let shadowMap;
function shadowTexture(){if(shadowMap)return shadowMap;const c=document.createElement('canvas');c.width=c.height=96;const ctx=c.getContext('2d'),g=ctx.createRadialGradient(48,48,2,48,48,47);g.addColorStop(0,'rgba(49,62,36,.58)');g.addColorStop(.42,'rgba(49,62,36,.3)');g.addColorStop(1,'rgba(49,62,36,0)');ctx.fillStyle=g;ctx.fillRect(0,0,96,96);shadowMap=new THREE.CanvasTexture(c);return shadowMap;}
export function contactShadow(parent,{x=0,z=0,width=3,depth=3,opacity=.25,y=-.021}={}){const m=new THREE.Mesh(new THREE.PlaneGeometry(width,depth),new THREE.MeshBasicMaterial({map:shadowTexture(),transparent:true,opacity,depthWrite:false}));m.rotation.x=-Math.PI/2;m.position.set(x,y,z);parent.add(m);return m;}

export function decorateMeadow(scene,options={}){
 const layout={...FARM_LAYOUT,...options},root=new THREE.Group();root.name='farm-v3-landscape';scene.add(root);
 const random=seeded(options.seed??7303),leaves=[],flowers=[],stones=[],grass=[],clear=layout.fieldClearance;
 const outside=(x,z)=>x<clear.minX||x>clear.maxX||z<clear.minZ||z>clear.maxZ;
 const paths=layout.paths.map(p=>({...p,curve:new THREE.CatmullRomCurve3(p.points.map(([x,z])=>new THREE.Vector3(x,0,z)))}));
 const samples=paths.flatMap(p=>Array.from({length:120},(_,i)=>({v:p.curve.getPoint(i/119),r:p.width*.53})));
 const clearOfPath=(x,z)=>samples.every(p=>(x-p.v.x)**2+(z-p.v.z)**2>p.r*p.r);
 // A soil shoulder, warmer wheel-worn center and broken edge make the road part of the ground.
 for(const path of paths){surface(root,ribbon(path.curve,path.width+.35,-.065),0x9e9967);surface(root,ribbon(path.curve,path.width+.19,-.055),0xc0ad7a);surface(root,ribbon(path.curve,path.width,-.043),0xd9c393);for(const offset of[-path.width*.23,path.width*.23])surface(root,ribbon(path.curve,.10,-.038,offset),0xcbb583);
  for(let i=2;i<104;i++){const t=i/104,q=path.curve.getPoint(t),n=new THREE.Vector3().crossVectors(path.curve.getTangent(t),new THREE.Vector3(0,1,0)).normalize();for(const side of[-1,1]){const d=path.width*.56+random()*.18,x=q.x+n.x*side*d,z=q.z+n.z*side*d;if(!outside(x,z))continue;
   if(i%2===0)stones.push({x,y:-.022,z,sx:.045+random()*.10,sy:.025,sz:.04+random()*.06,ry:random()*6,color:i%3?0xb7ad83:0xd9c99f});
   if(i%3!==0)for(let k=0;k<3;k++)leaves.push({x,y:-.037,z,ry:random()*6,rz:random()*.12,s:.46+random()*.23,color:k%2?0x70984a:0x819f52});
  }}
 }
 for(const [i,yard]of layout.yards.entries()){surface(root,island(yard.x,yard.z,yard.rx+.18,yard.rz+.12,-.07,i),0xa6a16f);surface(root,island(yard.x,yard.z,yard.rx,yard.rz,-.057,i),yard.color);}
 // Orchard mulch and broad planting islands give the large lawn a deliberate shape.
 for(const[x,z,r]of layout.orchardBeds){surface(root,island(x,z,r*1.2,r*.94,-.073,x),0x76974e);surface(root,island(x,z,r*.45,r*.38,-.055,x),0x9d8f5a);contactShadow(root,{x,z,width:r*3,depth:r*2.4,opacity:.23});}
 for(const[a,[cx,cz,r]]of layout.meadowBeds.entries()){
  surface(root,island(cx,cz,r,r*.7,-.071,a),a%2?0x759447:0x819d51);
  for(let j=0;j<19;j++){const angle=j*2.399+a*.71,spread=Math.sqrt((j+.4)/19)*r*.84,x=cx+Math.cos(angle)*spread,z=cz+Math.sin(angle)*spread*.68;if(!outside(x,z)||!clearOfPath(x,z))continue;
   for(let k=0;k<4;k++)leaves.push({x,y:-.025,z,ry:angle+k*1.73,rz:.06+(k%2)*.16,s:.58+random()*.37,color:[0x60853d,0x83a653,0x769c43,0x9eb864][k]});
   if(j%5===0&&a%3!==1){const y=.14+random()*.05;for(let k=0;k<5;k++){const ang=k*6.283/5;flowers.push({x:x+Math.cos(ang)*.035,y,z:z+Math.sin(ang)*.035,sx:.04,sy:.018,sz:.025,ry:-ang,color:a%2?0xffe9b5:0xf0d7bd});}flowers.push({x,y:y+.009,z,s:.018,color:0xd9ad4f});}
   if(j%4===0)for(let k=0;k<3;k++)grass.push({x:x+(random()-.5)*.3,y:.02,z:z+(random()-.5)*.3,sx:.017,sy:.12+random()*.08,sz:.027,rz:(random()-.5)*.45,ry:random()*6,color:0x9dad64});
  }
 }
 // Gentle hills stay behind the orchard and never encroach on the field plane.
 for(const[x,z,r,color]of layout.hills){const h=surface(root,new THREE.SphereGeometry(1,20,12),color);h.position.set(x,-.18,z);h.scale.set(r,r*.11,r*.73);}
 batch(root,leafGeometry(),leaves,{doubleSide:true});batch(root,new THREE.SphereGeometry(1,6,4),flowers);batch(root,new THREE.IcosahedronGeometry(1,1),stones);batch(root,new THREE.SphereGeometry(1,5,3),grass);
 return{root,paths,stats:{leaves:leaves.length,flowerParts:flowers.length,pebbles:stones.length,beds:layout.meadowBeds.length},layout};
}

// Rebuilt buildings own their planters and awning. Runtime attachments use optional named anchors.
export function decorateBuildings(scene,house,market,options={}){
 const attachments=[];
 for(const[group,size]of[[house,[4.6,3.9]],[market,[4.1,2.8]]]){contactShadow(group,{width:size[0],depth:size[1],opacity:.28});group.updateMatrixWorld(true);}
 if(options.doorstep!==false){const anchor=house.getObjectByName('anchor-doorstep');if(anchor){const position=anchor.getWorldPosition(new THREE.Vector3());scene.worldToLocal(position);const direction=new THREE.Vector3(0,0,1).applyQuaternion(house.getWorldQuaternion(new THREE.Quaternion()));const rows=[];for(let i=1;i<4;i++)rows.push({x:position.x+direction.x*i*.37,y:-.022,z:position.z+direction.z*i*.37,sx:.28-i*.025,sy:.05,sz:.17,ry:house.rotation.y+(i%2?.15:-.14),color:i%2?0xc9c19f:0xb9b799});attachments.push(batch(scene,new THREE.IcosahedronGeometry(1,1),rows));}}
 return{attachments,usesModelPlanters:true};
}
