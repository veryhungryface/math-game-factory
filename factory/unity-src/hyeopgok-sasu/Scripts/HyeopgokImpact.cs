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
        const int ParticleCap = 192, FlashCap = 20;
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

        readonly Particle[] particles = new Particle[ParticleCap];
        readonly Flash[] flashes = new Flash[FlashCap];
        readonly Matrix4x4[] smokeMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] coinMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] fleckMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] flashMatrices = new Matrix4x4[FlashCap];
        readonly Camera camera;
        readonly Mesh cloud, coin, fleck, star;
        readonly Material smokeMaterial, coinMaterial, flashMaterial;
        int particleCursor, flashCursor;
        uint rng = 0x63b1874du;

        public HyeopgokImpact(Camera renderCamera, Shader shader)
        {
            camera = renderCamera;
            cloud = MakeCloud();
            coin = MakeCoin();
            fleck = MakeFleck();
            star = MakeStar();
            smokeMaterial = CreateMaterial(shader,new Color(.98f,.975f,.90f));
            coinMaterial = CreateMaterial(shader,new Color(1,.70f,.035f));
            flashMaterial = CreateMaterial(shader,new Color(1,1,.98f));
        }

        public void Clear()
        {
            for(int i=0;i<ParticleCap;i++) particles[i].duration = 0;
            for(int i=0;i<FlashCap;i++) flashes[i].duration = 0;
            particleCursor = flashCursor = 0;
            rng = 0x63b1874du;
        }

        public void Contact(Vector3 position, bool heavy)
        {
            flashes[flashCursor++ % FlashCap] = new Flash {
                // Lift the star above the helmet line and slightly toward the
                // viewer, so its white rays survive the opaque smoke/depth test.
                position = position+Vector3.up*.46f-camera.transform.forward*.10f,
                age = 0, duration = heavy ? .26f : .20f,
                size = Range(heavy ? 1.06f : .68f,heavy ? 1.48f : 1.00f),
                angle = Range(0,180)
            };
            int count = heavy ? 14 : 6;
            for(int i=0;i<count;i++)
            {
                byte kind = (byte)(i < (heavy?5:3) ? 0 : i < (heavy?10:4) ? 1 : 2);
                float angle = Range(0,Mathf.PI*2), speed = Range(.28f,heavy?2.7f:1.6f);
                Vector3 velocity = new Vector3(Mathf.Cos(angle)*speed,Range(.8f,heavy?3.7f:2.3f),Mathf.Sin(angle)*speed);
                if(kind == 0) velocity *= .34f;
                particles[particleCursor++ % ParticleCap] = new Particle {
                    position = position+new Vector3(Range(-.16f,.16f),Range(0,.14f),Range(-.16f,.16f)),
                    velocity = velocity, age = 0,
                    duration = kind == 0 ? Range(.48f,.83f) : kind == 1 ? Range(.38f,.68f) : Range(.16f,.30f),
                    size = kind == 0 ? Range(.30f,.52f) : kind == 1 ? Range(.12f,.20f) : Range(.04f,.08f),
                    spin = Range(-180,180), kind = kind
                };
            }
        }

        public void UpdateAndDraw(float dt)
        {
            int smokeCount=0, coinCount=0, fleckCount=0, flashCount=0;
            for(int i=0;i<ParticleCap;i++)
            {
                Particle p=particles[i];
                if(p.duration<=0) continue;
                p.age+=dt;
                if(p.age>=p.duration){p.duration=0;particles[i]=p;continue;}
                p.position+=p.velocity*dt;
                if(p.kind==0) p.velocity*=1-dt*1.4f;
                else p.velocity.y-=dt*8.5f;
                float t=p.age/p.duration;
                float size=p.size*Mathf.Min(1,(1-t)*4);
                if(p.kind==0)
                {
                    size*=.75f+t*1.15f;
                    smokeMatrices[smokeCount++]=Matrix4x4.TRS(p.position,Quaternion.Euler(0,p.spin,0),new Vector3(size,size*.88f,size));
                }
                else if(p.kind==1)
                    coinMatrices[coinCount++]=Matrix4x4.TRS(p.position,Quaternion.Euler(65+t*240,p.spin+t*330,p.spin),Vector3.one*size);
                else
                    fleckMatrices[fleckCount++]=Matrix4x4.TRS(p.position,Quaternion.Euler(t*160,p.spin,t*230),new Vector3(size,size*2.6f,size));
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
            Draw(star,flashMaterial,flashMatrices,flashCount);
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
            Vector3[] vertices=new Vector3[17];
            Color[] colors=new Color[17];
            int[] triangles=new int[96];
            colors[0]=new Color(1,1,1,0);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;
                float r=i%2==1 ? .115f : i%4==0 ? .69f : .42f;
                vertices[i+1]=new Vector3(Mathf.Sin(a)*r,Mathf.Cos(a)*r,0);
                colors[i+1]=i%2==0 ? new Color(.92f,.98f,1,0) : new Color(1,1,1,0);
                int j=(i+1)%16+1, k=i*6;
                triangles[k]=0;triangles[k+1]=i+1;triangles[k+2]=j;
                triangles[k+3]=0;triangles[k+4]=j;triangles[k+5]=i+1;
            }
            return CreateMesh("충돌 별 섬광",vertices,colors,triangles);
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

        static Mesh MakeCoin()
        {
            Vector3[] vertices=new Vector3[32*3];
            Color[] colors=new Color[vertices.Length];
            int[] triangles=new int[vertices.Length];
            int n=0;
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*.25f,b=(i+1)*Mathf.PI*.25f;
                Vector3 p=new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,.10f);
                Vector3 q=new Vector3(Mathf.Cos(b)*.5f,Mathf.Sin(b)*.5f,.10f);
                Vector3 r=new Vector3(p.x,p.y,-.10f),s=new Vector3(q.x,q.y,-.10f);
                Face(vertices,colors,triangles,ref n,new Vector3(0,0,.10f),p,q,false);
                Face(vertices,colors,triangles,ref n,new Vector3(0,0,-.10f),s,r,false);
                Face(vertices,colors,triangles,ref n,p,r,q,true);
                Face(vertices,colors,triangles,ref n,q,r,s,true);
            }
            return CreateMesh("회전하는 금화",vertices,colors,triangles);
        }

        static Mesh MakeFleck()
        {
            Vector3[] vertices={Vector3.up*.5f,Vector3.down*.5f,Vector3.left*.5f,Vector3.right*.5f,Vector3.forward*.5f,Vector3.back*.5f};
            Color[] colors=new Color[6];
            for(int i=0;i<6;i++)colors[i]=new Color(1,1,1,0);
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
            Object.Destroy(smokeMaterial);Object.Destroy(coinMaterial);Object.Destroy(flashMaterial);
        }
    }
}
