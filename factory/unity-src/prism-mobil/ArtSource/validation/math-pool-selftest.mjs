// PrismMobilRules.BuildBank의 정수 생성 규칙을 독립 재계산해 전수 검산한다.
let checked = 0, accidentalTruths = 0, invalidCentroids = 0, dynamicMedianCases = 0, oldFixedTwoLineAccepted = 0;
function gcd(a,b){ while(b){const t=a%b;a=b;b=t;} return Math.abs(a)||1; }
for(let k=2;k<=22;k++){
  const g=gcd(k,24-k),p=k/g,q=(24-k)/g;
  for(let scale=1;scale<=8;scale++){
    const ae=p*scale,ec=q*scale;
    if(ae*(24-k)!==ec*k) throw new Error(`parallel invariant failed k=${k}`);
    let mixed=Math.round(24*p/(2*p+q)); if(mixed===k)mixed=Math.max(1,k-1);
    if(mixed===k) accidentalTruths++;
    checked++;
  }
}
for(let u=1;u<=8;u++){
  const ad=3*u,ag=2*u,gd=u;
  if(ag+gd!==ad||ag===ad/2||ag===gd) accidentalTruths++;
  checked++;
}
const templates=[[1,1,10,3,10,8],[1,1,10,5,10,9],[1,2,10,4,10,9],[1,2,10,5,10,8],[1,3,10,4,10,8],[1,3,10,5,10,10],[1,4,10,5,10,9],[2,3,11,1,2,8]];
const dist=(a,b)=>Math.hypot(a[0]-b[0],a[1]-b[1]);
function incenter(A,B,C){const a=dist(B,C),b=dist(A,C),c=dist(A,B),s=a+b+c;return[(a*A[0]+b*B[0]+c*C[0])/s,(a*A[1]+b*B[1]+c*C[1])/s];}
function circumcenter(A,B,C){const[ax,ay]=A,[bx,by]=B,[cx,cy]=C,d=2*(ax*(by-cy)+bx*(cy-ay)+cx*(ay-by));if(Math.abs(d)<1e-6)return null;const aa=ax*ax+ay*ay,bb=bx*bx+by*by,cc=cx*cx+cy*cy;return[(aa*(by-cy)+bb*(cy-ay)+cc*(ay-by))/d,(aa*(cx-bx)+bb*(ax-cx)+cc*(bx-ax))/d];}
for(const v of templates)for(let ox=0;ox<=1;ox++)for(let oy=0;oy<=1;oy++)for(let tag=0;tag<5;tag++){
  let [ax,ay,bx,by,cx,cy]=v;ax+=ox;bx+=ox;cx+=ox;ay+=oy;by+=oy;cy+=oy;
  if(tag===1)[bx,by,cx,cy]=[cx,cy,bx,by];
  else if(tag===2){ax=12-ax;bx=12-bx;cx=12-cx;}
  else if(tag===3){ay=12-ay;by=12-by;cy=12-cy;}
  else if(tag===4){[ax,ay]=[ay,ax];[bx,by]=[by,bx];[cx,cy]=[cy,cx];}
  if((ax+bx+cx)%3||(ay+by+cy)%3)invalidCentroids++;
  const gx=(ax+bx+cx)/3,gy=(ay+by+cy)/3;
  const boxX=(Math.min(ax,bx,cx)+Math.max(ax,bx,cx))/2,boxY=(Math.min(ay,by,cy)+Math.max(ay,by,cy))/2;
  const A=[ax,ay],B=[bx,by],C=[cx,cy],G=[gx,gy],I=incenter(A,B,C),O=circumcenter(A,B,C);
  const Mbc=[(bx+cx)/2,(by+cy)/2],Mac=[(ax+cx)/2,(ay+cy)/2];
  const cross1=[A[0]+(Mbc[0]-A[0])*2/3,A[1]+(Mbc[1]-A[1])*2/3];
  const cross2=[B[0]+(Mac[0]-B[0])*2/3,B[1]+(Mac[1]-B[1])*2/3];
  if(dist(cross1,G)>1e-9||dist(cross2,G)>1e-9)invalidCentroids++;else dynamicMedianCases++;
  const stage=(x,y)=>[.10+.80*x,.27+.45*y],grid=(x,y)=>stage(.05+.90*x/12,.08+.84*y/12);
  const As=grid(ax,ay),Bs=grid(bx,by),Cs=grid(cx,cy),MbcS=[(Bs[0]+Cs[0])/2,(Bs[1]+Cs[1])/2],MacS=[(As[0]+Cs[0])/2,(As[1]+Cs[1])/2];
  const oldFirst=dist([.50,.36],MbcS)<.10&&dist([.50,.70],As)<.16;
  const oldSecond=dist([.31,.53],MacS)<.10&&dist([.79,.34],Bs)<.16;
  if(oldFirst&&oldSecond)oldFixedTwoLineAccepted++;
  if(Math.hypot(gx-boxX,gy-boxY)<1.2||dist(G,I)<1.2||!O||dist(G,O)<1.2) accidentalTruths++;
  checked++;
}
const result={checked,accidentalTruths,invalidCentroids,dynamicMedianCases,oldFixedTwoLineAccepted,passed:accidentalTruths===0&&invalidCentroids===0&&dynamicMedianCases===160&&oldFixedTwoLineAccepted===0};
console.log(JSON.stringify(result,null,2));
if(!result.passed)process.exitCode=1;
