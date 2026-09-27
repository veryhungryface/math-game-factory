using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.HyeopgokSasu
{
    /// <summary>
    /// Visual-only, bounded pools. Owns its RNG so changing an impact can never
    /// change the battle's lane recycling, bolt targets or mathematical rules.
    /// </summary>
    sealed class HyeopgokImpact
    {
        const int ParticleCap = 192, FlashCap = 64, NumberCap=16;
        struct Particle
        {
            public Vector3 position, velocity;
            public float age, duration, size, spin;
            public byte kind;
        }
        struct Flash
        {
            public Vector3 position;
            public float age, duration, size, angle;
        }
        struct Popup { public Vector3 position; public float age; public bool heavy,live; }

        readonly Particle[] particles = new Particle[ParticleCap];
        readonly Flash[] flashes = new Flash[FlashCap];
        readonly Matrix4x4[] smokeMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] coinMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] fleckMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] flashMatrices = new Matrix4x4[FlashCap];
        readonly Matrix4x4[] fireMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] darkMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] smallNumbers = new Matrix4x4[NumberCap];
        readonly Matrix4x4[] bigNumbers = new Matrix4x4[NumberCap];
        readonly Popup[] numbers=new Popup[NumberCap];
        readonly Camera camera;
        readonly Mesh cloud, coin, fleck, star,smallNumber,bigNumber;
        readonly Material smokeMaterial, coinMaterial, flashMaterial,starMaterial,fireMaterial,darkMaterial,numberMaterial;
        readonly Texture2D numberAtlas;
        int particleCursor, flashCursor,numberCursor;
        float visualClock,lastStar=-1;
        uint rng = 0x63b1874du;

        public HyeopgokImpact(Camera renderCamera, Shader shader)
        {
            camera = renderCamera;
            cloud = MakeCloud();
            coin = MakeCoin();
            fleck = MakeFleck();
            star = MakeStar();
            numberAtlas=MakeNumberAtlas();
            smallNumber=MakeNumberQuad(false);
            bigNumber=MakeNumberQuad(true);
            numberMaterial=new Material(Resources.Load<Shader>("HyeopgokSasu/Shaders/DamageNumber"));
            numberMaterial.mainTexture=numberAtlas;numberMaterial.enableInstancing=true;
            smokeMaterial = new Material(Resources.Load<Shader>("HyeopgokSasu/Shaders/Smoke"));smokeMaterial.SetColor("_Color",new Color(.93f,.96f,1,.30f));smokeMaterial.enableInstancing=true;
            coinMaterial = CreateMaterial(shader,Color.white);
            flashMaterial = CreateMaterial(shader,new Color(1,1,.98f));
            starMaterial = new Material(Resources.Load<Shader>("HyeopgokSasu/Shaders/Spark"));starMaterial.SetColor("_Color",Color.white);starMaterial.enableInstancing=true;
            fireMaterial = CreateMaterial(shader,new Color(1,.31f,.018f));
            darkMaterial = CreateMaterial(shader,new Color(.69f,.73f,.73f));
        }

        public void Clear()
        {
            for(int i=0;i<ParticleCap;i++) particles[i].duration = 0;
            for(int i=0;i<FlashCap;i++) flashes[i].duration = 0;
            for(int i=0;i<NumberCap;i++) numbers[i].live=false;
            particleCursor = flashCursor = numberCursor=0;
            visualClock=0;lastStar=-1;
            rng = 0x63b1874du;
        }

        public void DamageNumber(Vector3 position,bool heavy)
        {
            // Dense infantry contacts share a readable popup cadence. This only
            // suppresses duplicate decoration; damage and rewards are resolved
            // by the battle before this visual callback runs.
            for(int i=0;i<NumberCap;i++)
                if(numbers[i].live&&numbers[i].age<.18f&&
                   (numbers[i].position-position-Vector3.up*.95f).sqrMagnitude<2.25f)
                    return;
            numbers[numberCursor++%NumberCap]=new Popup {position=position+Vector3.up*.95f,heavy=heavy,live=true};
        }

        public void Fire(Vector3 position,float intensity)
        {
            // Call at a fixed 0.12 s cadence, independent of battle randomness.
            for(int i=0;i<3;i++)
                particles[particleCursor++%ParticleCap]=new Particle {
                    position=position+new Vector3(Range(-.22f,.22f),Range(0,.28f),Range(-.18f,.18f)),
                    velocity=new Vector3(Range(-.12f,.12f),Range(.6f,1.2f),Range(-.1f,.1f)),
                    duration=Range(.38f,.72f),size=Range(.17f,.34f)*intensity,spin=Range(0,360),kind=(byte)(i==2?4:3)};
        }

        public void Explosion(Vector3 position)
        {
            Contact(position,true);
            for(int i=0;i<18;i++)
            {
                float a=Range(0,Mathf.PI*2),speed=Range(.7f,3.1f);
                particles[particleCursor++%ParticleCap]=new Particle {
                    position=position+Vector3.up*.2f,velocity=new Vector3(Mathf.Sin(a)*speed,Range(.8f,3),Mathf.Cos(a)*speed),
                    duration=i<9?Range(.22f,.48f):Range(.55f,.95f),size=Range(.19f,.36f),spin=Range(0,360),kind=(byte)(i<9?3:4)};
            }
            DamageNumber(position,true);
        }

        public void Contact(Vector3 position, bool heavy)
        {
            if(visualClock-lastStar>=.084f){
            lastStar=visualClock;
            flashes[flashCursor++ % FlashCap] = new Flash {
                // Lift the star above the helmet line and slightly toward the
                // viewer, so its white rays survive the opaque smoke/depth test.
                position = position+Vector3.up*.56f-camera.transform.forward*.15f,
                age = 0, duration = .18f,
                size = Range(heavy ? 1.12f : .78f,heavy ? 1.34f : .98f),
                angle = Range(-18,18)
            };
            }
            int count = heavy ? 14 : 6;
            for(int i=0;i<count;i++)
            {
                byte kind = (byte)(i < (heavy?2:1) ? 0 : i < (heavy?5:3) ? 2 : i < (heavy?10:4) ? 1 : 2);
                float angle = Range(0,Mathf.PI*2), speed = Range(.28f,heavy?2.7f:1.6f);
                Vector3 velocity = new Vector3(Mathf.Cos(angle)*speed,Range(.8f,heavy?3.7f:2.3f),Mathf.Sin(angle)*speed);
                if(kind == 0) velocity *= .34f;
                particles[particleCursor++ % ParticleCap] = new Particle {
                    position = position+new Vector3(Range(-.16f,.16f),Range(0,.14f),Range(-.16f,.16f)),
                    velocity = velocity, age = 0,
                    duration = kind == 0 ? Range(.16f,.25f) : kind == 1 ? Range(.38f,.68f) : Range(.16f,.30f),
                    size = kind == 0 ? Range(.13f,.22f) : kind == 1 ? Range(.12f,.20f) : Range(.04f,.08f),
                    spin = Range(-180,180), kind = kind
                };
            }
        }

        public void UpdateAndDraw(float dt)
        {
            visualClock+=dt;
            int smokeCount=0, coinCount=0, fleckCount=0, flashCount=0,fireCount=0,darkCount=0;
            for(int i=0;i<ParticleCap;i++)
            {
                Particle p=particles[i];
                if(p.duration<=0) continue;
                p.age+=dt;
                if(p.age>=p.duration){p.duration=0;particles[i]=p;continue;}
                p.position+=p.velocity*dt;
                if(p.kind==0||p.kind>=3) p.velocity*=1-dt*1.4f;
                else p.velocity.y-=dt*8.5f;
                float t=p.age/p.duration;
                float size=p.size*Mathf.Min(1,(1-t)*4);
                if(p.kind==0)
                {
                    size*=.74f+t*.88f;
                    smokeMatrices[smokeCount++]=Matrix4x4.TRS(p.position,Quaternion.Euler(0,p.spin,0),new Vector3(size,size*.88f,size));
                }
                else if(p.kind==1)
                    coinMatrices[coinCount++]=Matrix4x4.TRS(p.position,Quaternion.Euler(65+t*240,p.spin+t*330,p.spin),Vector3.one*size);
                else if(p.kind==2)
                    fleckMatrices[fleckCount++]=Matrix4x4.TRS(p.position,Quaternion.Euler(t*160,p.spin,t*230),new Vector3(size,size*2.6f,size));
                else if(p.kind==3)
                    fireMatrices[fireCount++]=Matrix4x4.TRS(p.position,Quaternion.Euler(0,p.spin,0),new Vector3(size,size*(1.8f+t),size));
                else
                    darkMatrices[darkCount++]=Matrix4x4.TRS(p.position,Quaternion.Euler(0,p.spin,0),Vector3.one*size*(1+t));
                particles[i]=p;
            }
            Quaternion facing=camera.transform.rotation;
            for(int i=0;i<FlashCap;i++)
            {
                Flash f=flashes[i];
                if(f.duration<=0) continue;
                f.age+=dt;
                if(f.age>=f.duration){f.duration=0;flashes[i]=f;continue;}
                float t=f.age/f.duration;
                float size=f.size*(1-t*t*.65f)*Mathf.Min(1,(1-t)*3.8f);
                // Stars sit on the actual contact, facing the player, not on a HUD.
                flashMatrices[flashCount++]=Matrix4x4.TRS(f.position,facing*Quaternion.Euler(0,0,f.angle+t*18),new Vector3(size,size,1));
                flashes[i]=f;
            }
            Draw(cloud,smokeMaterial,smokeMatrices,smokeCount);
            Draw(coin,coinMaterial,coinMatrices,coinCount);
            Draw(fleck,coinMaterial,fleckMatrices,fleckCount);
            Draw(star,starMaterial,flashMatrices,flashCount);
            Draw(cloud,fireMaterial,fireMatrices,fireCount);
            Draw(cloud,darkMaterial,darkMatrices,darkCount);
            int small=0,big=0;
            for(int i=0;i<NumberCap;i++)
            {
                Popup p=numbers[i];if(!p.live)continue;p.age+=dt;
                if(p.age>.82f){p.live=false;numbers[i]=p;continue;}
                float size=(p.heavy?.60f:.48f)*Mathf.Min(1,(.82f-p.age)*7);
                Matrix4x4 m=Matrix4x4.TRS(p.position+Vector3.up*p.age*.95f-camera.transform.forward*.13f,facing,Vector3.one*size);
                if(p.heavy)bigNumbers[big++]=m;else smallNumbers[small++]=m;
                numbers[i]=p;
            }
            Draw(smallNumber,numberMaterial,smallNumbers,small);
            Draw(bigNumber,numberMaterial,bigNumbers,big);
        }

        void Draw(Mesh mesh,Material material,Matrix4x4[] matrices,int count)
        {
            if(count>0) Graphics.DrawMeshInstanced(mesh,0,material,matrices,count,null,
                ShadowCastingMode.Off,false,0,camera,LightProbeUsage.Off);
        }

        static Material CreateMaterial(Shader shader,Color color)
        {
            Material material=new Material(shader);
            material.SetColor("_Color",color);
            material.SetFloat("_Unlit",1);
            material.enableInstancing=true;
            return material;
        }

        static Mesh MakeStar()
        {
            // Four broad white points plus six blue radial rays, one additive draw.
            Vector3[] vertices=new Vector3[42];Color[] colors=new Color[42];int[] triangles=new int[84];
            colors[0]=Color.white;
            for(int i=0;i<8;i++){
                float a=i*Mathf.PI*.25f,r=i%2==0?.64f:.08f;
                vertices[i+1]=new Vector3(Mathf.Sin(a)*r,Mathf.Cos(a)*r,0);
                colors[i+1]=i%2==0?new Color(.20f,.68f,1,.72f):new Color(1,1,1,1);
                triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%8+1;
            }
            for(int i=0;i<6;i++){
                float a=i*Mathf.PI/3+.22f;Vector3 d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),r=new Vector3(-d.y,d.x,0);
                int v=9+i*4,t=24+i*6;float length=i%2==0?1.12f:.89f;
                vertices[v]=d*.23f-r*.047f;vertices[v+1]=d*.23f+r*.047f;
                vertices[v+2]=d*length+r*.004f;vertices[v+3]=d*length-r*.004f;
                colors[v]=colors[v+1]=new Color(.25f,.78f,1,.95f);colors[v+2]=colors[v+3]=new Color(.08f,.44f,1,0);
                triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;
            }
            // Soft cyan halo is part of the same instanced mesh; no bloom pass.
            colors[33]=new Color(.08f,.48f,1,.65f);
            for(int i=0;i<8;i++){
                float a=i*Mathf.PI*.25f;vertices[34+i]=new Vector3(Mathf.Cos(a)*.38f,Mathf.Sin(a)*.38f,0);colors[34+i]=new Color(.04f,.36f,1,0);
                int t=60+i*3;triangles[t]=33;triangles[t+1]=34+i;triangles[t+2]=34+(i+1)%8;
            }
            return CreateMesh("Four-point white star and six sky rays",vertices,colors,triangles);
        }

        static Texture2D MakeNumberAtlas()
        {
            // Hand-authored five-by-seven bitmap glyphs; baked once at startup.
            // No text objects, dynamic font rebuilds or per-frame allocations.
            const int width=128,height=32;bool[] ink=new bool[width*height];
            string[] glyphs={"00000000000000011111000000000000000","00100011000010000100001000010001110","11111100001000011110000010000111110"};
            int[][] words={new[]{0,2},new[]{0,1,2}};
            for(int word=0;word<2;word++)for(int g=0;g<words[word].Length;g++){
                int start=word*64+(64-words[word].Length*18+3)/2+g*18;string glyph=glyphs[words[word][g]];
                for(int y=0;y<7;y++)for(int x=0;x<5;x++)if(glyph[y*5+x]=='1')
                    for(int yy=0;yy<3;yy++)for(int xx=0;xx<3;xx++)ink[(5+(6-y)*3+yy)*width+start+x*3+xx]=true;
            }
            Color[] pixels=new Color[width*height];
            for(int y=1;y<height-1;y++)for(int x=1;x<width-1;x++){
                int k=y*width+x;if(ink[k])pixels[k]=Color.white;
                else if(ink[k-1]||ink[k+1]||ink[k-width]||ink[k+width]||ink[k-width-1]||ink[k-width+1]||ink[k+width-1]||ink[k+width+1])pixels[k]=new Color(.045f,.12f,.16f,1);
            }
            Texture2D texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Combat damage bitmap atlas",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }

        static Mesh MakeNumberQuad(bool heavy)
        {
            Mesh mesh=new Mesh{name=heavy?"Damage -15 bitmap quad":"Damage -5 bitmap quad",
                vertices=new[]{new Vector3(-1,-.5f,0),new Vector3(1,-.5f,0),new Vector3(1,.5f,0),new Vector3(-1,.5f,0)},
                uv=new[]{new Vector2(heavy?.5f:0,0),new Vector2(heavy?1:.5f,0),new Vector2(heavy?1:.5f,1),new Vector2(heavy?.5f:0,1)},
                triangles=new[]{0,2,1,0,3,2}};mesh.RecalculateBounds();return mesh;
        }

        static Mesh MakeCloud()
        {
            // Rounded, faceted smoke: 8 sides, 4 latitude strips, 48 triangles.
            Vector3[] vertices=new Vector3[48*3];
            Color[] colors=new Color[vertices.Length];
            int[] triangles=new int[vertices.Length];
            int n=0;
            for(int band=0;band<4;band++)
                for(int side=0;side<8;side++)
                {
                    Vector3 a=SpherePoint(band,side),b=SpherePoint(band,side+1);
                    Vector3 c=SpherePoint(band+1,side),d=SpherePoint(band+1,side+1);
                    if(band>0) Face(vertices,colors,triangles,ref n,a,b,c,true);
                    if(band<3) Face(vertices,colors,triangles,ref n,b,d,c,true);
                }
            return CreateMesh("흰 연기 구름",vertices,colors,triangles);
        }

        static Vector3 SpherePoint(int band,int side)
        {
            float latitude=band*Mathf.PI*.25f,longitude=side*Mathf.PI*.25f;
            return new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude))*.5f;
        }

        internal static Mesh MakeCoin()
        {
            // Broad flat minted face, pale bevel and orange-gold rim. Coins are
            // opaque discs, not orange point particles or a solid backpack tube.
            Vector3[] vertices=new Vector3[216];Color[] colors=new Color[216];int[] triangles=new int[216];int n=0;
            Color face=MgfLook.Hex("#F2B705"),rim=MgfLook.Hex("#DD9A1E"),bevel=MgfLook.Hex("#FFE58C");face.a=rim.a=bevel.a=0;
            for(int i=0;i<12;i++){
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
                Vector3 p=new Vector3(Mathf.Cos(a)*.43f,Mathf.Sin(a)*.43f,.075f),q=new Vector3(Mathf.Cos(b)*.43f,Mathf.Sin(b)*.43f,.075f);
                Vector3 r=new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,.040f),t=new Vector3(Mathf.Cos(b)*.5f,Mathf.Sin(b)*.5f,.040f);
                Vector3 u=new Vector3(r.x,r.y,-.06f),v=new Vector3(t.x,t.y,-.06f);
                CoinFace(vertices,colors,triangles,ref n,new Vector3(0,0,.075f),p,q,face);
                CoinFace(vertices,colors,triangles,ref n,new Vector3(0,0,-.06f),v,u,rim);
                CoinFace(vertices,colors,triangles,ref n,p,r,q,bevel);CoinFace(vertices,colors,triangles,ref n,q,r,t,bevel);
                CoinFace(vertices,colors,triangles,ref n,r,u,t,rim);CoinFace(vertices,colors,triangles,ref n,t,u,v,rim);
            }
            return CreateMesh("Minted gold disc",vertices,colors,triangles);
        }
        static void CoinFace(Vector3[] vertices,Color[] colors,int[] triangles,ref int n,Vector3 a,Vector3 b,Vector3 c,Color color){
            vertices[n]=a;colors[n]=color;triangles[n]=n;n++;vertices[n]=b;colors[n]=color;triangles[n]=n;n++;vertices[n]=c;colors[n]=color;triangles[n]=n;n++;
        }

        static Mesh MakeFleck()
        {
            Vector3[] vertices={Vector3.up*.5f,Vector3.down*.5f,Vector3.left*.5f,Vector3.right*.5f,Vector3.forward*.5f,Vector3.back*.5f};
            Color[] colors=new Color[6];
            for(int i=0;i<6;i++)colors[i]=new Color(1,.80f,.22f,0);
            return CreateMesh("금빛 충돌 파편",vertices,colors,new[]{0,4,3,0,3,5,0,5,2,0,2,4,1,3,4,1,5,3,1,2,5,1,4,2});
        }

        static void Face(Vector3[] vertices,Color[] colors,int[] triangles,ref int n,Vector3 a,Vector3 b,Vector3 c,bool shade)
        {
            Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
            float light=shade ? .82f+Mathf.Clamp01(normal.y*.65f-normal.x*.25f-normal.z*.10f)*.18f : 1;
            Color color=new Color(light,light,light,0);
            vertices[n]=a;colors[n]=color;triangles[n]=n;n++;
            vertices[n]=b;colors[n]=color;triangles[n]=n;n++;
            vertices[n]=c;colors[n]=color;triangles[n]=n;n++;
        }

        static Mesh CreateMesh(string name,Vector3[] vertices,Color[] colors,int[] triangles)
        {
            Mesh mesh=new Mesh {name=name,vertices=vertices,colors=colors,triangles=triangles};
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        float Range(float min,float max)
        {
            rng^=rng<<13;rng^=rng>>17;rng^=rng<<5;
            return min+(max-min)*(rng&0x00ffffffu)/16777216f;
        }

        public void Dispose()
        {
            Object.Destroy(cloud);Object.Destroy(coin);Object.Destroy(fleck);Object.Destroy(star);
            Object.Destroy(smokeMaterial);Object.Destroy(coinMaterial);Object.Destroy(flashMaterial);Object.Destroy(starMaterial);
            Object.Destroy(fireMaterial);Object.Destroy(darkMaterial);Object.Destroy(smallNumber);Object.Destroy(bigNumber);Object.Destroy(numberMaterial);Object.Destroy(numberAtlas);
        }
    }
}
