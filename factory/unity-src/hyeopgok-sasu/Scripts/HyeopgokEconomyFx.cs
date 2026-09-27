using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.HyeopgokSasu
{
    // Pooled coins physically bridge battlefield pickups, the king's wallet and
    // actual answer deposits. The rules own the balance; this class cannot mint it.
    sealed class HyeopgokEconomyFx
    {
        const int Cap=192,StackCap=24,DecorCap=96;
        const float PickupRadius=2.25f;
        struct Coin { public Vector3 position,velocity,origin,target; public float age,flight,spin; public byte mode; }
        readonly Coin[] coins=new Coin[Cap];
        readonly Coin[] decorations=new Coin[DecorCap];
        readonly Matrix4x4[] matrices=new Matrix4x4[Cap+DecorCap+StackCap];
        readonly Mesh coin;readonly Material gold;readonly Camera camera;
        Transform king;HyeopgokRules rules;
        int cursor,decorCursor;float labelTime;uint rng=0x843aad3u;
        public int Coins=>rules==null?0:rules.Coins;
        public int Remaining=>rules==null?0:rules.TotalPoured;
        public bool Depositing=>labelTime>0;
        public HyeopgokEconomyFx(Camera cam,Shader shader){camera=cam;coin=HyeopgokImpact.MakeCoin();gold=new Material(shader);gold.SetColor("_Color",Color.white);gold.SetFloat("_Unlit",.28f);gold.SetFloat("_Metallic",1);gold.enableInstancing=true;}
        public void SetKing(Transform value){king=value;}
        public void SetRules(HyeopgokRules value){rules=value;}
        public void Clear(){for(int i=0;i<Cap;i++)coins[i].mode=0;for(int i=0;i<DecorCap;i++)decorations[i].mode=0;cursor=decorCursor=0;labelTime=0;rng=0x843aad3u;}
        int Slot(){for(int k=0;k<Cap;k++){int i=(cursor+k)%Cap;if(coins[i].mode==0){cursor=(i+1)%Cap;return i;}}return -1;}
        public void Drop(Vector3 position){int i=Slot();if(i<0)return;float a=Range(0,6.283185f);coins[i]=new Coin{position=position+Vector3.up*.35f,velocity=new Vector3(Mathf.Sin(a)*Range(.7f,1.7f),Range(1.5f,2.8f),Mathf.Cos(a)*Range(.7f,1.7f)),age=-.07f,spin=Range(0,360),mode=1};
            // The spendable pickup retains its original timing. These visual
            // spill discs never call AddCoins. A fixed 96-slot pool throws four
            // flat discs across a 2-3 m band after the impact's 70 ms beat.
            for(int k=0;k<4;k++){float angle=a+k*1.5708f+Range(-.16f,.16f),speed=Range(1.6f,3.0f);decorations[decorCursor++%DecorCap]=new Coin{position=position+Vector3.up*.38f,velocity=new Vector3(Mathf.Sin(angle)*speed,Range(1.8f,2.8f),Mathf.Cos(angle)*speed),age=-.07f-k*.012f,spin=a*57.3f+k*35,mode=1};}
        }
        public void Pour(Vector3 target){
            if(!king)return;labelTime=.42f;int i=Slot();if(i<0)return;
            Vector3 start=Backpack()+Vector3.up*.32f;
            coins[i]=new Coin{position=start,origin=start,target=target+Vector3.up*.20f,mode=4,spin=Range(0,360)};
            // Eight discrete discs per actual deposit: no beam or extra currency.
            // Eight discs fan out over the pad (biased toward the camera) so the
            // arcs stay visible in front of the king instead of behind him.
            for(int k=0;k<8;k++){float a=k*2.4f;decorations[decorCursor++%DecorCap]=new Coin{position=start,origin=start,target=target+new Vector3(Mathf.Sin(a)*.72f,.12f,Mathf.Cos(a)*.42f-.36f),age=-k*.065f,flight=.72f,mode=4,spin=k*43};}
        }
        Vector3 Backpack()=>king.position-king.forward*.36f+Vector3.up*.52f;
        public void Simulate(float dt){Step(dt,false);}
        public void UpdateAndDraw(float dt){Step(dt,true);}
        void Step(float dt,bool draw){
            if(!king)return;Vector3 backpack=Backpack();labelTime=Mathf.Max(0,labelTime-dt);int count=0;
            for(int i=0;i<Cap;i++){
                Coin c=coins[i];if(c.mode==0)continue;c.age+=dt;
                if(c.age<0){coins[i]=c;continue;}
                if(c.mode==1){c.position+=c.velocity*dt;c.velocity.y-=dt*7;if(c.position.y<=.31f){c.position.y=.31f;c.velocity=Vector3.zero;c.mode=2;}}
                if(c.mode==2){Vector3 delta=king.position-c.position;delta.y=0;
                    // Ground coins only magnetise after the king approaches the
                    // battle line. The former 10 m radius filled a 120-coin wallet
                    // while the king stood still on the answer side of the plateau.
                    if(rules!=null&&rules.Active&&Coins<rules.WalletCapacity&&c.age>.8f&&delta.sqrMagnitude<PickupRadius*PickupRadius){c.origin=c.position;c.flight=0;c.mode=3;}
                }
                if(c.mode==3){c.flight+=dt*2.8f;float t=Mathf.Clamp01(c.flight);c.position=Vector3.Lerp(c.origin,backpack,t*t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.15f;
                    if(t>=1){if(rules!=null&&rules.AddCoins(HyeopgokRules.CoinPerKill)>0)c.mode=0;else{c.mode=2;c.position=c.origin;}coins[i]=c;continue;}}
                else if(c.mode==4){float t=Mathf.Clamp01(c.age/.38f);c.position=Vector3.Lerp(c.origin,c.target,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.9f;if(t>=1){c.mode=0;coins[i]=c;continue;}}
                if(draw)matrices[count++]=Matrix4x4.TRS(c.position,c.mode==2?Quaternion.Euler(-90,c.spin,0):Quaternion.Euler(c.age*390,c.spin,c.spin),Vector3.one*.25f);coins[i]=c;
            }
            for(int i=0;i<DecorCap;i++){
                Coin c=decorations[i];if(c.mode==0)continue;c.age+=dt;
                if(c.age<0){decorations[i]=c;continue;}
                if(c.mode==1){c.position+=c.velocity*dt;c.velocity.y-=dt*7;if(c.position.y<=.318f){if(c.position.z<-8.0f){c.mode=0;decorations[i]=c;continue;}c.position.y=.318f;c.velocity=Vector3.zero;c.mode=2;c.flight=0;}}
                if(c.mode==2){c.flight+=dt;if(c.flight>=6){c.origin=c.position;c.flight=0;c.mode=3;}}
                if(c.mode==3){c.flight+=dt*1.8f;float t=Mathf.Clamp01(c.flight);c.position=Vector3.Lerp(c.origin,backpack,t*t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.1f;if(t>=1)c.mode=0;}
                if(c.mode==4){float t=Mathf.Clamp01(c.age/c.flight);c.position=Vector3.Lerp(c.origin,c.target,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*2.1f;if(t>=1)c.mode=0;}
                if(draw&&c.mode!=0)matrices[count++]=Matrix4x4.TRS(c.position,c.mode==2?Quaternion.Euler(-90,c.spin,0):Quaternion.Euler(70+c.age*300,c.spin,c.spin),Vector3.one*(c.mode==4?.36f:.28f));
                decorations[i]=c;
            }
            if(!draw)return;
            // Compact two-column coin satchel below the crown line; it grows with the
            // wallet (one disc per coin up to 24) without hiding the hero's head.
            int stack=Mathf.Min(StackCap,Coins);
            for(int i=0;i<stack;i++)matrices[count++]=Matrix4x4.TRS(backpack+king.right*((i%2==0?-1:1)*.095f)+Vector3.up*((i/2)*.036f),Quaternion.Euler(-90,i*23,0),Vector3.one*.2f);
            if(count>0)Graphics.DrawMeshInstanced(coin,0,gold,matrices,count,null,ShadowCastingMode.Off,false,0,camera,LightProbeUsage.Off);
        }
        // Seven-segment geometry keeps combat numbers in instanced mesh batches,
        // avoiding one text object/material per popup and any font atlas texture.
        internal static Mesh NumberMesh(int number,bool minus)
        {
            int tens=number>=10?number/10:-1,ones=number%10;
            int slots=(minus?1:0)+(tens>=0?1:0)+1;
            var vertices=new System.Collections.Generic.List<Vector3>(84);
            var triangles=new System.Collections.Generic.List<int>(126);
            var colors=new System.Collections.Generic.List<Color>(84);
            int[] masks={63,6,91,79,102,109,125,7,127,111};
            for(int s=0;s<slots;s++)
            {
                bool negative=minus&&s==0;
                int n=negative?0:s==slots-1?ones:tens;
                int mask=negative?64:masks[n];float x=(s-(slots-1)*.5f)*.72f;
                for(int segment=0;segment<7;segment++)
                {
                    if((mask&(1<<segment))==0)continue;
                    bool h=segment==0||segment==3||segment==6;
                    float cx=x+(h?0:segment==1||segment==2?.25f:-.25f);
                    float cy=segment==0?.48f:segment==3?-.48f:segment==6?0:segment==1||segment==5?.24f:-.24f;
                    float w=h?.48f:.10f,height=h?.10f:.41f;
                    int b=vertices.Count;
                    vertices.Add(new Vector3(cx-w*.5f,cy-height*.5f,0));vertices.Add(new Vector3(cx+w*.5f,cy-height*.5f,0));
                    vertices.Add(new Vector3(cx+w*.5f,cy+height*.5f,0));vertices.Add(new Vector3(cx-w*.5f,cy+height*.5f,0));
                    for(int k=0;k<4;k++)colors.Add(new Color(1,1,1,0));
                    triangles.Add(b);triangles.Add(b+2);triangles.Add(b+1);triangles.Add(b);triangles.Add(b+3);triangles.Add(b+2);
                }
            }
            Mesh mesh=new Mesh {name="전장 숫자"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        float Range(float min,float max){rng^=rng<<13;rng^=rng>>17;rng^=rng<<5;return min+(max-min)*(rng&0xffffffu)/16777216f;}
        public void Dispose(){Object.Destroy(coin);Object.Destroy(gold);}
    }
}
