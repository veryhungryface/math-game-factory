using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.HyeopgokSasu
{
    // Phase 1: coins are visible support resources only. They never decide an
    // answer, construction reward, HP, wave completion or victory condition.
    sealed class HyeopgokEconomyFx
    {
        const int Cap=150, StackCap=20;
        struct Coin { public Vector3 position,velocity,origin; public float age,flight,spin; public byte mode; }
        readonly Coin[] coins=new Coin[Cap];
        readonly Matrix4x4[] matrices=new Matrix4x4[Cap+StackCap];
        readonly Mesh coin;
        readonly Material gold;
        readonly Camera camera;
        Transform king;
        int cursor,wallet=15,remaining;
        float pourClock,labelTime;
        Vector3 pad;
        uint rng=0x843aad3u;
        public int Coins { get { return wallet; } }
        public int Remaining { get { return remaining; } }
        public bool Depositing { get { return labelTime>0; } }

        public HyeopgokEconomyFx(Camera cam,Shader shader)
        {
            camera=cam;coin=HyeopgokImpact.MakeCoin();
            gold=new Material(shader);gold.SetColor("_Color",new Color(.95f,.69f,.035f));gold.SetFloat("_Unlit",.65f);gold.enableInstancing=true;
        }
        public void SetKing(Transform value){king=value;}
        public void Clear()
        {
            for(int i=0;i<Cap;i++)coins[i].mode=0;
            cursor=0;wallet=15;remaining=0;pourClock=labelTime=0;rng=0x843aad3u;
        }
        public void Drop(Vector3 position)
        {
            float a=Range(0,6.283185f);
            coins[cursor++%Cap]=new Coin {position=position+Vector3.up*.35f,
                velocity=new Vector3(Mathf.Sin(a)*Range(.5f,1.8f),Range(1.5f,2.8f),Mathf.Cos(a)*Range(.5f,1.8f)),
                spin=Range(0,360),mode=1};
        }
        public void Deposit(Vector3 position)
        {
            pad=position;remaining=Mathf.Min(15,wallet);pourClock=0;labelTime=1.8f;
        }
        public void UpdateAndDraw(float dt)
        {
            if(!king)return;
            Vector3 backpack=king.position-king.forward*.24f+Vector3.up*.68f;
            labelTime=Mathf.Max(0,labelTime-dt);
            if(remaining>0)
            {
                pourClock-=dt;
                if(pourClock<=0)
                {
                    pourClock=.065f;wallet=Mathf.Max(0,wallet-1);remaining--;
                    Vector3 start=backpack+Vector3.up*Mathf.Min(wallet,StackCap)*.052f;
                    coins[cursor++%Cap]=new Coin {position=start,origin=start,mode=4,spin=Range(0,360)};
                }
            }
            int count=0;
            for(int i=0;i<Cap;i++)
            {
                Coin c=coins[i];if(c.mode==0)continue;c.age+=dt;
                if(c.mode==1)
                {
                    c.position+=c.velocity*dt;c.velocity.y-=dt*7;
                    if(c.position.y<=.31f){c.position.y=.31f;c.velocity=Vector3.zero;c.mode=2;}
                }
                if(c.mode==2)
                {
                    Vector3 delta=king.position-c.position;delta.y=0;
                    // A generous support magnet bridges the height of the plateau.
                    // Every drop rests visibly first; no answer interaction is needed.
                    if(c.age>1.4f&&(delta.sqrMagnitude<64||c.age>8))
                    {c.origin=c.position;c.flight=0;c.mode=3;}
                }
                if(c.mode==3)
                {
                    c.flight+=dt*1.9f;float t=Mathf.Clamp01(c.flight);
                    c.position=Vector3.Lerp(c.origin,backpack,t*t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.15f;
                    if(t>=1){wallet=Mathf.Min(999,wallet+1);c.mode=0;coins[i]=c;continue;}
                }
                else if(c.mode==4)
                {
                    float t=Mathf.Clamp01(c.age/.40f);
                    c.position=Vector3.Lerp(c.origin,pad+Vector3.up*.16f,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.8f;
                    if(t>=1){c.mode=0;coins[i]=c;continue;}
                }
                float tilt=c.mode==2?90:c.age*390;
                matrices[count++]=Matrix4x4.TRS(c.position,Quaternion.Euler(tilt,c.spin,c.spin),Vector3.one*.21f);
                coins[i]=c;
            }
            int stack=Mathf.Min(StackCap,wallet);
            for(int i=0;i<stack;i++)
                matrices[count++]=Matrix4x4.TRS(backpack+Vector3.up*(i*.055f),Quaternion.Euler(90,0,0),Vector3.one*.29f);
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
