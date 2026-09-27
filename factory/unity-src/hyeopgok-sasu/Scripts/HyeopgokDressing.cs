using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        static bool PadClearForDressing(float x,float z,float radius)
        {
            for(int i=0;i<HyeopgokRules.Pads.Length;i++){
                Vector3 p=HyeopgokRules.Pads[i];float dx=x-p.x,dz=z-p.z;
                if(dx*dx+dz*dz<radius*radius)return false;
            }
            return true;
        }

        void BuildVillagePathsAndYard()
        {
            // Round 2b: a purposeful dirt network instead of an oval loop. The spine
            // runs from the south timber yard between the two pad columns to the
            // northern well; branches reach the barracks edge and both tower bases.
            // Ribbons sit below all pads and change no collider or walk bound.
            var paths=new DecorationMesh();
            Color earth=Hex("#C8B59C").linear;earth.a=.84f;
            Color rut=Hex("#F3DDB8").linear;rut.a=.30f;
            Vector3[][] routes={
                new[]{new Vector3(-.45f,1.205f,-4.55f),new Vector3(-.6f,1.205f,-2.7f),new Vector3(-.6f,1.205f,.45f),new Vector3(-.62f,1.205f,3.3f),new Vector3(-1.05f,1.205f,4.75f)},
                new[]{new Vector3(-.6f,1.204f,.45f),new Vector3(-1.9f,1.204f,.42f),new Vector3(-3.05f,1.204f,.2f)},
                new[]{new Vector3(-.6f,1.204f,-2.7f),new Vector3(.55f,1.204f,-3.05f),new Vector3(1.45f,1.204f,-3.45f)},
                new[]{new Vector3(-.62f,1.204f,3.3f),new Vector3(.55f,1.204f,3.95f),new Vector3(1.45f,1.204f,4.55f)}};
            for(int r=0;r<routes.Length;r++){
                Vector3[] knots=routes[r];var points=new Vector3[(knots.Length-1)*10+1];
                for(int i=0;i<points.Length;i++){
                    float u=i/10f;int k=Mathf.Min(knots.Length-2,(int)u);float t=u-k;
                    Vector3 a0=knots[Mathf.Max(0,k-1)],b0=knots[k],c0=knots[k+1],d0=knots[Mathf.Min(knots.Length-1,k+2)];
                    points[i]=.5f*((2*b0)+(-a0+c0)*t+(2*a0-5*b0+4*c0-d0)*t*t+(-a0+3*b0-3*c0+d0)*t*t*t);
                }
                float width=r==0?.78f:.62f;
                paths.CurvedFeatheredRibbon(points,width,.20f,earth,false);
                var leftRut=new Vector3[points.Length];var rightRut=new Vector3[points.Length];
                for(int i=0;i<points.Length;i++){
                    Vector3 a1=points[Mathf.Max(0,i-1)],b1=points[Mathf.Min(points.Length-1,i+1)];
                    Vector3 side=Vector3.Cross(Vector3.up,b1-a1).normalized;
                    leftRut[i]=points[i]+side*width*.24f+Vector3.up*.002f;rightRut[i]=points[i]-side*width*.24f+Vector3.up*.002f;
                }
                paths.CurvedFeatheredRibbon(leftRut,.060f,.020f,rut,false);
                paths.CurvedFeatheredRibbon(rightRut,.060f,.020f,rut,false);
                // Soft worn junction discs hide ribbon joins where branches meet.
                if(r>0)paths.SoftDisc(knots[0]+Vector3.up*.001f,.42f,.9f,earth);
            }
            // Feet and carts have worn the entry of each workshop, with soft edges.
            Color apron=earth;apron.a=.20f;
            paths.SoftDisc(new Vector3(-.50f,1.207f,-4.63f),1.42f,.55f,apron);
            paths.SoftDisc(new Vector3(-1.05f,1.207f,5.17f),1.49f,.65f,apron);
            CreateDecoration("Soft courtyard loop and two cart ruts",paths,Mgf.MgfLook.Alpha(Color.white),false);

            var rails=new DecorationMesh();Color blue=Hex("#167DE0"),wood=Hex("#B78C50");
            for(int side=0;side<2;side++){
                float x=side==0?-5.65f:-3.03f;
                for(int i=0;i<5;i++){
                    float z=-.79f+i*.67f;
                    if(side==1&&i<2)continue; // Barracks doorway remains open.
                    rails.Box(new Vector3(x,.69f,z),new Vector3(.10f,.88f,.10f),wood);
                    rails.Box(new Vector3(x,1.13f,z),new Vector3(.14f,.11f,.14f),blue);
                    if(i<4){
                        rails.Box(new Vector3(x,.92f,z+.335f),new Vector3(.08f,.09f,.70f),blue);
                        rails.Box(new Vector3(x,.60f,z+.335f),new Vector3(.08f,.075f,.70f),wood);
                    }
                }
            }
            CreateDecoration("Barracks yard blue painted rails",rails,worldMat,false);
            SpawnModel("torch",new Vector3(-5.28f,.25f,-.85f),.95f);
            SpawnModel("torch",new Vector3(-3.07f,.25f,-.84f),.95f);
        }

        // Art-only seeds never consume a rule, question or combat random number.
        // Every authored mesh below is merged once by CombineScenery, including
        // the small baked-AO grass/flower/pebble assets. Nothing is spawned in play.
        void BuildMeadowDressing(DecorationMesh detail)
        {
            var rng=new System.Random(713947);
            Vector2[] patches={
                new Vector2(-10.1f,-6.2f),new Vector2(-9.3f,-1.8f),new Vector2(-8.8f,2.6f),
                new Vector2(-8.8f,7.2f),new Vector2(-5.2f,9.4f),new Vector2(9.2f,6.8f),
                new Vector2(9.7f,1.2f),new Vector2(9.4f,-5.6f),new Vector2(6.4f,-13.6f),
                new Vector2(-6.0f,-13.3f),new Vector2(-17.1f,-11.8f),new Vector2(16.8f,-12.6f),
                new Vector2(-20.8f,1.2f),new Vector2(21.0f,3.7f),new Vector2(-14.4f,19.9f),
                new Vector2(14.5f,19.5f),new Vector2(-1.45f,5.1f),new Vector2(1.40f,4.8f),
                new Vector2(-2.57f,-3.91f),new Vector2(1.69f,-3.8f),new Vector2(2.11f,.24f)
            };
            for(int cluster=0;cluster<patches.Length;cluster++){
                bool inner=cluster>=16;int count=inner?12:16;
                for(int i=0;i<count;i++){
                    float angle=(float)rng.NextDouble()*Mathf.PI*2;
                    float radius=Mathf.Sqrt((float)rng.NextDouble())*(inner?.66f:2.0f);
                    float x=patches[cluster].x+Mathf.Sin(angle)*radius,z=patches[cluster].y+Mathf.Cos(angle)*radius;
                    bool plateau=InPolygon(PlateauOutline,x,z);
                    if(inner&&!plateau)continue;
                    bool pad=!PadClearForDressing(x,z,2.0f);
                    if(pad||z>-11.9f&&z<-8.1f)continue;
                    float y=plateau?1.202f:SceneryGround(x,z)+.003f;
                    string id=i%8==0?"flowers":i%5==0?"pebbles":"grass_tuft";
                    float scale=id=="grass_tuft"?.62f+(float)rng.NextDouble()*.58f:.50f+(float)rng.NextDouble()*.5f;
                    var go=SpawnModel(id,new Vector3(x,y,z),scale);go.transform.Rotate(0,rng.Next(360),0);
                }
            }
            // The village joins the surrounding field with short, worn stepping stones.
            // These are purely visual; pad centres, walk bounds and the army road are unchanged.
            for(int i=0;i<6;i++){
                float x=-2.7f+i*.39f,z=4.23f+Mathf.Sin(i*.8f)*.08f;
                detail.Disc(new Vector3(x,1.209f,z),.115f,.62f,Hex(i%2==0?"#b5b4a0":"#a0aa98"),7);
            }
        }

        void BuildGroundToneVariation()
        {
            // Large, low-alpha patches break the broad single-colour lawns without
            // textures. They are authored once and remain one transparent draw.
            var tones=new DecorationMesh();
            Color cool=Hex("#0D7461").linear;cool.a=.075f;
            Color warm=Hex("#73C892").linear;warm.a=.050f;
            Vector4[] plateau={
                new Vector4(-2.78f,1.203f,5.13f,1.46f),new Vector4(1.48f,1.203f,5.42f,1.18f),
                new Vector4(-.18f,1.203f,-4.72f,1.34f),new Vector4(2.18f,1.203f,-3.63f,.92f),
                new Vector4(-3.05f,1.203f,3.85f,.86f),new Vector4(2.48f,1.203f,3.63f,.70f)};
            for(int i=0;i<plateau.Length;i++){
                Vector4 p=plateau[i];tones.SoftDisc(new Vector3(p.x,p.y,p.z),p.w,.66f,i%2==0?cool:warm);
            }
            Vector4[] plain={
                new Vector4(-9.4f,-1.514f,-5.1f,3.2f),new Vector4(8.6f,-1.514f,2.4f,3.0f),
                new Vector4(-7.8f,-1.514f,-13.2f,2.8f),new Vector4(7.3f,-1.514f,-13.0f,2.5f),
                new Vector4(-9.1f,-1.514f,8.2f,2.7f),new Vector4(9.0f,-1.514f,8.8f,2.6f)};
            for(int i=0;i<plain.Length;i++){
                Vector4 p=plain[i];tones.SoftDisc(new Vector3(p.x,p.y,p.z),p.w,.72f,i%2==0?cool:warm);
            }
            CreateDecoration("Low frequency meadow tone bake",tones,Mgf.MgfLook.Alpha(Color.white),false);
        }

        void PlaceSettlementProps()
        {
            string[] ids={
                "barrel","sack","crate","barrel","torch","torch","bell","logpile",
                "well","barrel","sack","crate","stump","logpile","crate","barrel",
                "sack","torch","bell","torch","crate","sack","barrel","crate",
                "torch","torch","logpile","barrel","sack","crate","stump","logpile"
            };
            Vector4[] points={
                new Vector4(-5.13f,.25f,-.22f,.59f),new Vector4(-5.08f,.25f,-.61f,.61f),
                new Vector4(-5.11f,.25f,1.34f,.54f),new Vector4(-3.38f,.25f,2.56f,.49f),
                new Vector4(-5.05f,.25f,2.53f,.69f),new Vector4(-3.44f,.25f,.53f,.68f),
                new Vector4(-3.34f,.25f,1.23f,.55f),new Vector4(-5.22f,.25f,2.0f,.52f),
                new Vector4(-1.24f,1.2f,4.71f,.68f),new Vector4(-2.44f,1.2f,4.00f,.48f),
                new Vector4(-2.29f,1.2f,4.14f,.49f),new Vector4(-1.91f,1.2f,5.24f,.47f),
                new Vector4(-.45f,1.2f,5.69f,.43f),new Vector4(.26f,1.2f,5.72f,.58f),
                new Vector4(-2.84f,1.2f,-3.63f,.45f),new Vector4(-2.54f,1.2f,-4.12f,.46f),
                new Vector4(-2.14f,1.2f,-4.46f,.51f),new Vector4(-2.77f,1.2f,3.29f,.57f),
                new Vector4(1.81f,1.2f,5.22f,.49f),new Vector4(2.06f,1.2f,3.91f,.57f),
                new Vector4(2.03f,.25f,10.92f,.65f),new Vector4(2.39f,.25f,10.70f,.62f),
                new Vector4(6.27f,.25f,8.76f,.62f),new Vector4(6.22f,.25f,9.55f,.57f),
                new Vector4(2.54f,.25f,7.05f,.68f),new Vector4(6.13f,.25f,7.08f,.68f),
                new Vector4(6.02f,.25f,10.74f,.48f),new Vector4(2.43f,.25f,8.03f,.46f),
                new Vector4(2.21f,.25f,8.47f,.48f),new Vector4(1.89f,1.2f,-2.72f,.44f),
                new Vector4(7.28f,-1.52f,-2.04f,.34f),new Vector4(7.93f,-1.52f,-1.65f,.57f)
            };
            for(int i=0;i<points.Length;i++){
                Vector4 p=points[i];if(p.y>1&&!PadClearForDressing(p.x,p.z,2))continue;
                float scale=p.w*(p.y>1?1.25f:1.1f);var go=SpawnModel(ids[i],new Vector3(p.x,p.y,p.z),scale);go.transform.Rotate(0,i*79+17,0);
            }
            for(int i=0;i<7;i++){
                SpawnModel("palisade",new Vector3(-5.58f,.25f,2.0f-i*.58f),.55f).transform.Rotate(0,90,0);
            }
            // A compact timber enclosure makes the plateau's northern workshop read as
            // a lived-in settlement, rather than scattered props on an empty test plane.
            for(int i=0;i<4;i++)SpawnModel("palisade",new Vector3(-1.72f+i*.70f,1.2f,6.01f),.48f);
        }

        void DressWorkshopVignettes()
        {
            // Below the walking area's z=-4.2 boundary: a timber yard with goods
            // for the next upgrade. Tall props stay outside all four pad footprints.
            string[] ids={"logpile","crate","barrel","sack","sack","torch","stump","crate","barrel","sack","logpile","torch","barrel","crate"};
            Vector4[] p={
                new Vector4(-.45f,1.2f,-4.98f,.82f),new Vector4(-1.16f,1.2f,-4.72f,.84f),
                new Vector4(.32f,1.2f,-4.54f,.81f),new Vector4(.74f,1.2f,-4.80f,.80f),
                new Vector4(-1.73f,1.2f,-4.54f,.71f),new Vector4(1.67f,1.2f,-3.67f,.72f),
                new Vector4(-2.82f,1.2f,.40f,.88f),new Vector4(-2.97f,1.2f,.92f,.66f),
                new Vector4(1.87f,1.2f,.33f,.76f),new Vector4(2.24f,1.2f,.65f,.71f),
                new Vector4(-2.82f,1.2f,3.90f,.75f),new Vector4(-.16f,1.2f,4.37f,.72f),
                new Vector4(-.14f,1.2f,5.15f,.75f),new Vector4(.78f,1.2f,5.29f,.74f)
            };
            for(int i=0;i<p.Length;i++){
                Vector4 v=p[i];if(!PadClearForDressing(v.x,v.z,2))continue;
                SpawnModel(ids[i],new Vector3(v.x,v.y,v.z),v.w).transform.Rotate(0,i*73+31,0);
            }
            var apron=new DecorationMesh();
            Color earth=Hex("#C8B59C").linear;earth.a=.18f;
            apron.SoftDisc(new Vector3(-.42f,1.209f,-4.66f),1.28f,.53f,earth);
            apron.SoftDisc(new Vector3(-1.25f,1.211f,4.67f),1.03f,.86f,earth);
            apron.SoftDisc(new Vector3(1.94f,1.211f,.45f),.63f,.86f,earth);
            CreateDecoration("Workshop worn earth aprons",apron,Mgf.MgfLook.Alpha(Color.white),false);
        }

        void DressPlateauClusters()
        {
            // Three readable edge clusters fill the empty lawn while preserving a
            // strict two-metre clear circle around every answer pad.
            string[] ids={
                "crate","barrel","sack","torch","palisade","palisade",
                "crate","barrel","sack","torch","palisade",
                "logpile","crate","barrel","sack","torch","palisade"
            };
            Vector4[] points={
                new Vector4(2.28f,1.2f,4.18f,.66f),new Vector4(2.07f,1.2f,4.62f,.60f),new Vector4(1.70f,1.2f,5.02f,.58f),
                new Vector4(2.52f,1.2f,4.77f,.67f),new Vector4(2.44f,1.2f,5.27f,.42f),new Vector4(1.91f,1.2f,5.60f,.42f),
                new Vector4(2.54f,1.2f,.73f,.55f),new Vector4(2.52f,1.2f,.30f,.52f),new Vector4(2.56f,1.2f,-.10f,.50f),
                new Vector4(2.57f,1.2f,1.18f,.61f),new Vector4(2.64f,1.2f,.93f,.36f),
                new Vector4(-3.10f,1.2f,4.37f,.61f),new Vector4(-3.03f,1.2f,3.92f,.53f),new Vector4(-3.18f,1.2f,4.82f,.53f),
                new Vector4(-2.72f,1.2f,5.23f,.49f),new Vector4(-2.92f,1.2f,5.60f,.59f),new Vector4(-3.24f,1.2f,5.38f,.38f)};
            for(int i=0;i<points.Length;i++){
                Vector4 p=points[i];if(!PadClearForDressing(p.x,p.z,2.0f))continue;
                var prop=SpawnModel(ids[i],new Vector3(p.x,p.y,p.z),p.w);
                prop.transform.Rotate(0,i*67+19,0);
            }
        }

        void DressMesaToes()
        {
            // Round 2b: rim shoulders and wall/foot boulders are authored as clustered
            // flat-top sandstone blocks in art_r2_environment.py (terrain.fbx).
            Vector4[] ledge={
                new Vector4(-2.92f,.23f,-4.45f,.87f),new Vector4(-1.66f,.22f,-5.25f,.84f),
                new Vector4(-.28f,.22f,-5.56f,.83f),new Vector4(1.05f,.22f,-5.15f,.81f),
                new Vector4(2.18f,.23f,-3.83f,.79f)
            };
            for(int i=0;i<ledge.Length;i++){
                Vector4 p=ledge[i];var rock=SpawnModel("rock_medium",new Vector3(p.x,p.y,p.z),1);
                rock.transform.localScale=new Vector3(.70f,p.w*1.35f,.47f);rock.transform.Rotate(0,i*71+23,0);
            }
        }

        void PlantSandstoneRim()
        {
            var rng=new System.Random(942817);
            Vector2[] groves={
                new Vector2(-8.45f,-5.25f),new Vector2(-8.35f,-.45f),new Vector2(-8.6f,2.1f),
                new Vector2(-8.9f,8.45f),new Vector2(-3.9f,10.6f),new Vector2(8.8f,9.4f),
                new Vector2(8.2f,2.3f),new Vector2(8.4f,-2.35f),new Vector2(7.1f,-13.25f),
                new Vector2(-6.15f,-13.3f),new Vector2(-17.2f,-13.8f),new Vector2(17.1f,-13.4f),
                new Vector2(-22.1f,-3.2f),new Vector2(-22.3f,8.1f),new Vector2(-15.7f,20.5f),
                new Vector2(-5.9f,25.1f),new Vector2(6.3f,25.4f),new Vector2(17.4f,21.1f),
                new Vector2(23.1f,8.9f),new Vector2(23.4f,-3.2f),
                // Round 2b: fill the empty portrait foreground around the creek.
                new Vector2(6.6f,-6.3f),new Vector2(1.3f,-11.3f),new Vector2(-3.6f,-12.0f),new Vector2(4.4f,-12.6f)
            };
            var placed=new Vector2[9];
            for(int group=0;group<groves.Length;group++){
                int count=3+(group%7);
                for(int i=0;i<count;i++){
                    Vector2 best=groves[group];float bestDistance=-1;
                    for(int trial=0;trial<12;trial++){
                        float angle=(float)rng.NextDouble()*Mathf.PI*2;
                        float radius=Mathf.Sqrt((float)rng.NextDouble())*1.72f;
                        Vector2 candidate=groves[group]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                        float distance=99;
                        for(int k=0;k<i;k++)distance=Mathf.Min(distance,(candidate-placed[k]).sqrMagnitude);
                        if(distance>bestDistance){best=candidate;bestDistance=distance;}
                    }
                    placed[i]=best;
                    float x=best.x,z=best.y,scale=.64f+(float)rng.NextDouble()*.34f;
                    var tree=SpawnModel((i+group)%3==0?"tree_broadleaf":"tree",new Vector3(x,SceneryGround(x,z),z),scale);
                    tree.transform.Rotate(0,rng.Next(360),0);
                    // Each grove gets one or two nested mineral shapes, never an
                    // isolated evenly-spaced boulder necklace.
                    if(i==0||(i==count-1&&group%2==0)){
                        float a=(float)rng.NextDouble()*Mathf.PI*2;
                        float rx=x+Mathf.Sin(a)*(.34f+scale*.26f),rz=z+Mathf.Cos(a)*(.34f+scale*.26f);
                        string id=(group+i)%5==0?"rock_medium":"rock_small";
                        float rs=id=="rock_medium"?.32f+(float)rng.NextDouble()*.18f:.42f+(float)rng.NextDouble()*.24f;
                        var rock=SpawnModel(id,new Vector3(rx,SceneryGround(rx,rz),rz),rs);rock.transform.Rotate(0,rng.Next(360),0);
                    }
                }
            }
            // Far mineral ridges are now authored in art_r2_environment.py and
            // merged into terrain.fbx. No isolated large boulder scatters remain.
        }
    }
}
