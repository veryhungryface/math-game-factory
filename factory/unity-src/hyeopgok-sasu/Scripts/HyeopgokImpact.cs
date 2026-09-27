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
        readonly Material smokeMaterial, coinMaterial,starMaterial,fireMaterial,darkMaterial,numberMaterial;
        readonly Texture2D numberAtlas;
        int particleCursor, flashCursor,numberCursor;
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
            coinMaterial = CreateMaterial(shader,Color.white);coinMaterial.SetFloat("_Unlit",.32f);coinMaterial.SetFloat("_Metallic",.95f);
            starMaterial = new Material(Resources.Load<Shader>("HyeopgokSasu/Shaders/Spark"));starMaterial.SetColor("_Color",Color.white);starMaterial.enableInstancing=true;
            fireMaterial = CreateMaterial(shader,new Color(1,.31f,.018f));
            darkMaterial = CreateMaterial(shader,new Color(1f,.90f,.88f));
        }

        public void Clear()
        {
            for(int i=0;i<ParticleCap;i++) particles[i].duration = 0;
            for(int i=0;i<FlashCap;i++) flashes[i].duration = 0;
            for(int i=0;i<NumberCap;i++) numbers[i].live=false;
            particleCursor = flashCursor = numberCursor=0;
            rng = 0x63b1874du;
        }

        public void DamageNumber(Vector3 position,bool heavy)
        {
            // Dense infantry contacts share a readable popup cadence. This only
            // suppresses duplicate decoration; damage and rewards are resolved
            // by the battle before this visual callback runs.
            for(int i=0;i<NumberCap;i++)
                if(numbers[i].live&&numbers[i].age<.30f&&
                   (numbers[i].position-position-Vector3.up*.95f).sqrMagnitude<4.0f)
                    return;
            numbers[numberCursor++%NumberCap]=new Popup {position=position+Vector3.up*.95f,heavy=heavy,live=true};
        }

        public void DamageNumberPerTarget(Vector3 position,bool heavy)
        {
            // Tower patterns call this once per enemy they actually remove. A
            // Lv3 chain visibly promises four hits, so spatial de-duplication is
            // deliberately disabled for this bounded (16-slot) effect pool.
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

        public void Explosion(Vector3 position,bool showDamageNumber=true)
        {
            Contact(position,true);
            for(int i=0;i<18;i++)
            {
                float a=Range(0,Mathf.PI*2),speed=Range(.7f,3.1f);
                particles[particleCursor++%ParticleCap]=new Particle {
                    position=position+Vector3.up*.2f,velocity=new Vector3(Mathf.Sin(a)*speed,Range(.8f,3),Mathf.Cos(a)*speed),
                    duration=i<9?Range(.22f,.48f):Range(.55f,.95f),size=Range(.19f,.36f),spin=Range(0,360),kind=(byte)(i<9?3:4)};
            }
            if(showDamageNumber)DamageNumber(position,true);
        }

        public void Contact(Vector3 position, bool heavy)
        {
            flashes[flashCursor++ % FlashCap] = new Flash {
                // Lift the star above the helmet line and slightly toward the
                // viewer, so its white rays survive the opaque smoke/depth test.
                position = position+Vector3.up*.56f-camera.transform.forward*.15f,
                age = 0, duration = heavy?.24f:.21f,
                size = Range(heavy ? 1.65f : 1.22f,heavy ? 2.0f : 1.52f),
                angle = Range(-18,18)
            };
            int count = heavy ? 18 : 8;
            for(int i=0;i<count;i++)
            {
                byte kind = (byte)(i < (heavy?3:1) ? 0 : i < (heavy?7:3) ? 2 : i < (heavy?14:6) ? 1 : 2);
                float angle = Range(0,Mathf.PI*2), speed = Range(.28f,heavy?2.7f:1.6f);
                Vector3 velocity = new Vector3(Mathf.Cos(angle)*speed,Range(.8f,heavy?3.7f:2.3f),Mathf.Sin(angle)*speed);
                if(kind == 0) velocity *= .34f;
                particles[particleCursor++ % ParticleCap] = new Particle {
                    position = position+new Vector3(Range(-.16f,.16f),Range(0,.14f),Range(-.16f,.16f)),
                    velocity = velocity, age = kind==0?-.035f:kind==1?-.070f:0,
                    duration = kind == 0 ? Range(.16f,.25f) : kind == 1 ? Range(.38f,.68f) : Range(.16f,.30f),
                    size = kind == 0 ? Range(.13f,.22f) : kind == 1 ? Range(.12f,.20f) : Range(.04f,.08f),
                    spin = Range(-180,180), kind = kind
                };
            }
        }

        public void UpdateAndDraw(float dt)
        {
            int smokeCount=0, coinCount=0, fleckCount=0, flashCount=0,fireCount=0,darkCount=0;
            for(int i=0;i<ParticleCap;i++)
            {
                Particle p=particles[i];
                if(p.duration<=0) continue;
                p.age+=dt;
                if(p.age<0){particles[i]=p;continue;}
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
                float lensScale=Mathf.Clamp((f.position-camera.transform.position).magnitude/38f,.62f,1f);
                float size=f.size*lensScale*(1-t*t*.65f)*Mathf.Min(1,(1-t)*3.8f);
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
                float pop=p.age<.09f?1+(.09f-p.age)*4.5f:1;float size=(p.heavy?.82f:.64f)*pop*Mathf.Min(1,(.82f-p.age)*7);
                // Keep popups a similar on-screen size at the close landscape lens.
                size*=Mathf.Clamp((p.position-camera.transform.position).magnitude/44f,.6f,1f);
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
                float a=i*Mathf.PI*.25f,r=i%2==0?.78f:.105f;
                vertices[i+1]=new Vector3(Mathf.Sin(a)*r,Mathf.Cos(a)*r,0);
                colors[i+1]=i%2==0?new Color(.20f,.68f,1,.72f):new Color(1,1,1,1);
                triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%8+1;
            }
            for(int i=0;i<6;i++){
                float a=i*Mathf.PI/3+.22f;Vector3 d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),r=new Vector3(-d.y,d.x,0);
                int v=9+i*4,t=24+i*6;float length=i%2==0?1.30f:1.02f;
                vertices[v]=d*.25f-r*.062f;vertices[v+1]=d*.25f+r*.062f;
                vertices[v+2]=d*length+r*.004f;vertices[v+3]=d*length-r*.004f;
                colors[v]=colors[v+1]=new Color(.25f,.78f,1,.95f);colors[v+2]=colors[v+3]=new Color(.08f,.44f,1,0);
                triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;
            }
            // Soft cyan halo is part of the same instanced mesh; no bloom pass.
            colors[33]=new Color(.12f,.62f,1,.78f);
            for(int i=0;i<8;i++){
                float a=i*Mathf.PI*.25f;vertices[34+i]=new Vector3(Mathf.Cos(a)*.48f,Mathf.Sin(a)*.48f,0);colors[34+i]=new Color(.04f,.36f,1,0);
                int t=60+i*3;triangles[t]=33;triangles[t+1]=34+i;triangles[t+2]=34+(i+1)%8;
            }
            return CreateMesh("Four-point white star and six sky rays",vertices,colors,triangles);
        }

        static Texture2D MakeNumberAtlas()
        {
            // Rounded stroke glyphs rasterised once at startup (distance to line
            // segments): chunky white numerals with a dark outline, bilinear
            // filtered. Left half "-5", right half "-15". No per-frame work.
            const int width=256,height=64;const float unit=3.55f,face=1.45f,outline=1.05f;
            Vector2[][] minus={new[]{new Vector2(0,7),new Vector2(5.2f,7)}};
            Vector2[][] one={new[]{new Vector2(2.6f,1.2f),new Vector2(2.6f,12.8f),new Vector2(.4f,10.9f)}};
            Vector2[][] five={new[]{new Vector2(7.2f,12.8f),new Vector2(1.9f,12.8f),new Vector2(1.5f,7.9f),new Vector2(3.7f,8.7f),new Vector2(6.0f,8.4f),new Vector2(7.5f,6.6f),new Vector2(7.6f,4.0f),new Vector2(6.2f,1.8f),new Vector2(3.9f,1.1f),new Vector2(1.2f,2.1f)}};
            var strokes=new System.Collections.Generic.List<Vector2>(64);
            void Add(Vector2[][] glyph,float x0,float cx){foreach(var line in glyph)for(int i=0;i+1<line.Length;i++){strokes.Add(new Vector2(cx+(x0+line[i].x)*unit,4+line[i].y*unit));strokes.Add(new Vector2(cx+(x0+line[i+1].x)*unit,4+line[i+1].y*unit));}}
            // Word widths in glyph units: "-5" = 5.2+1.8+7.6, "-15" = 5.2+1.6+2.6+1.9+7.6
            Add(minus,0,64-14.6f*unit*.5f);Add(five,7.0f,64-14.6f*unit*.5f);
            float left=192-18.9f*unit*.5f;Add(minus,0,left);Add(one,6.8f,left);Add(five,11.3f,left);
            Color[] pixels=new Color[width*height];Color ink=Color.white,rim=new Color(.043f,.13f,.17f,1);
            float faceR=face*unit,rimR=(face+outline)*unit;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                Vector2 p=new Vector2(x+.5f,y+.5f);float d=999;
                for(int k=0;k<strokes.Count;k+=2){Vector2 a=strokes[k],b=strokes[k+1],ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(1e-4f,ab.sqrMagnitude));d=Mathf.Min(d,(p-(a+ab*t)).magnitude);}
                float inkA=Mathf.Clamp01(faceR-d+.5f),rimA=Mathf.Clamp01(rimR-d+.5f);
                Color c=Color.Lerp(rim,ink,inkA);c.a=rimA;pixels[y*width+x]=c;
            }
            Texture2D texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Combat damage stroke atlas",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
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
            Object.Destroy(smokeMaterial);Object.Destroy(coinMaterial);Object.Destroy(starMaterial);
            Object.Destroy(fireMaterial);Object.Destroy(darkMaterial);Object.Destroy(smallNumber);Object.Destroy(bigNumber);Object.Destroy(numberMaterial);Object.Destroy(numberAtlas);
        }
    }
}
