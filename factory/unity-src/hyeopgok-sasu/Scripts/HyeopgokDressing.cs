using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
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
                bool inner=cluster>=16;int count=inner?9:16;
                for(int i=0;i<count;i++){
                    float angle=(float)rng.NextDouble()*Mathf.PI*2;
                    float radius=Mathf.Sqrt((float)rng.NextDouble())*(inner?.66f:2.0f);
                    float x=patches[cluster].x+Mathf.Sin(angle)*radius,z=patches[cluster].y+Mathf.Cos(angle)*radius;
                    bool plateau=InPolygon(PlateauOutline,x,z);
                    if(inner&&!plateau)continue;
                    bool pad=false;
                    for(int k=0;k<4;k++)if(Mathf.Abs(x-HyeopgokRules.Pads[k].x)<1.06f&&Mathf.Abs(z-HyeopgokRules.Pads[k].z)<1.07f){pad=true;break;}
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
                Vector4 p=points[i];float scale=p.w*(p.y>1?1.25f:1.1f);var go=SpawnModel(ids[i],new Vector3(p.x,p.y,p.z),scale);go.transform.Rotate(0,i*79+17,0);
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
                Vector4 v=p[i];SpawnModel(ids[i],new Vector3(v.x,v.y,v.z),v.w).transform.Rotate(0,i*73+31,0);
            }
            var apron=new DecorationMesh();
            Color earth=Hex("#C8B59C").linear;earth.a=.35f;
            apron.SoftDisc(new Vector3(-.42f,1.209f,-4.66f),1.28f,.53f,earth);
            apron.SoftDisc(new Vector3(-1.25f,1.211f,4.67f),1.03f,.86f,earth);
            apron.SoftDisc(new Vector3(1.94f,1.211f,.45f),.63f,.86f,earth);
            CreateDecoration("Workshop worn earth aprons",apron,Mgf.MgfLook.Alpha(Color.white),false);
            // Flat stepping stones connect the two pad rows without introducing
            // a new obstacle, collision shape or change to mathematical input.
            var steps=new DecorationMesh();
            for(int i=0;i<8;i++){
                float x=-2.3f+i*.43f,z=.46f+Mathf.Sin(i*.85f)*.07f;
                steps.Disc(new Vector3(x,1.217f,z),i%3==0?.13f:.09f,.65f,Hex(i%2==0?"#b6b6a0":"#9eae94"),7);
            }
            CreateDecoration("Footworn stones between answer pads",steps,worldMat,false);
        }

        void DressMesaToes()
        {
            // Freestanding, baked sandstone formations cover portions of the long
            // navy mesa faces. Their irregular shoulders replace the straight beam
            // impression while keeping terrain, road and collision geometry intact.
            Vector4[] wall={
                new Vector4(-9.72f,-1.50f,3.74f,1.14f),new Vector4(-7.77f,-1.50f,3.67f,1.38f),
                new Vector4(-6.11f,-1.50f,4.37f,1.18f),new Vector4(-4.94f,-1.50f,5.55f,.98f),
                new Vector4(-3.88f,-1.50f,6.64f,.82f),new Vector4(8.44f,-1.50f,4.89f,.90f),
                new Vector4(9.94f,-1.50f,5.53f,.96f),new Vector4(11.22f,-1.50f,7.11f,1.08f)
            };
            for(int i=0;i<wall.Length;i++){
                Vector4 p=wall[i];var rock=SpawnModel("rock_large",new Vector3(p.x,p.y,p.z),1);
                rock.transform.localScale=new Vector3(.62f+(i%3)*.09f,p.w*1.38f,.60f+(i%2)*.16f);rock.transform.Rotate(0,i*53+19,0);
                SpawnModel("rock_medium",new Vector3(p.x-.57f,p.y,p.z-.63f),.78f+(i%3)*.14f).transform.Rotate(0,i*83,0);
            }
            Vector4[] ledge={
                new Vector4(-2.92f,.23f,-4.45f,.87f),new Vector4(-1.66f,.22f,-5.25f,.84f),
                new Vector4(-.28f,.22f,-5.56f,.83f),new Vector4(1.05f,.22f,-5.15f,.81f),
                new Vector4(2.18f,.23f,-3.83f,.79f)
            };
            for(int i=0;i<ledge.Length;i++){
                Vector4 p=ledge[i];var rock=SpawnModel("rock_medium",new Vector3(p.x,p.y,p.z),1);
                rock.transform.localScale=new Vector3(.70f,p.w*1.35f,.47f);rock.transform.Rotate(0,i*71+23,0);
            }
            // Low talus outside the army banks adds another depth scale at the foot.
            Vector3[] foot={new Vector3(-6.71f,-1.5f,-4.4f),new Vector3(-6.15f,-1.5f,.30f),new Vector3(6.72f,-1.5f,-3.9f),new Vector3(6.99f,-1.5f,.5f)};
            for(int i=0;i<foot.Length;i++)SpawnModel("rock_medium",foot[i],1.08f+(i%2)*.22f).transform.Rotate(0,i*89+15,0);
        }

        void PlantSandstoneRim()
        {
            var rng=new System.Random(942817);
            Vector2[] groves={
                new Vector2(-10.75f,-5.25f),new Vector2(-10.3f,-.45f),new Vector2(-10.0f,3.8f),
                new Vector2(-8.9f,8.45f),new Vector2(-3.9f,10.6f),new Vector2(8.8f,9.4f),
                new Vector2(10.0f,3.6f),new Vector2(10.3f,-2.35f),new Vector2(7.1f,-13.25f),
                new Vector2(-6.15f,-13.3f),new Vector2(-17.2f,-13.8f),new Vector2(17.1f,-13.4f),
                new Vector2(-22.1f,-3.2f),new Vector2(-22.3f,8.1f),new Vector2(-15.7f,20.5f),
                new Vector2(-5.9f,25.1f),new Vector2(6.3f,25.4f),new Vector2(17.4f,21.1f),
                new Vector2(23.1f,8.9f),new Vector2(23.4f,-3.2f)
            };
            for(int group=0;group<groves.Length;group++){
                int count=group<10?5+(group%3):4+(group%3);
                for(int i=0;i<count;i++){
                    float angle=i*2.39996f+group*.37f;
                    float radius=i==0?0:.65f+Mathf.Sqrt(i)*.55f;
                    float x=groves[group].x+Mathf.Sin(angle)*radius,z=groves[group].y+Mathf.Cos(angle)*radius;
                    float scale=.72f+(float)rng.NextDouble()*.52f;
                    var tree=SpawnModel((i+group)%4==0?"tree_broadleaf":"tree",new Vector3(x,SceneryGround(x,z),z),scale);
                    tree.transform.Rotate(0,rng.Next(360),0);
                }
            }
            // Three deliberately different boulder sizes break up the old box-cliff silhouette.
            // Foreground clusters stay low; the taller masses continue beyond either edge.
            for(int group=0;group<18;group++){
                float angle=group*Mathf.PI*2/18;
                float x=Mathf.Sin(angle)*(24+(group%3)*1.5f),z=2+Mathf.Cos(angle)*(25+(group%2)*1.2f);
                float y=SceneryGround(x,z);
                string id=z<-10?"rock_medium":"rock_large";
                var major=SpawnModel(id,new Vector3(x,y,z),1.05f+(group%3)*.24f);major.transform.Rotate(0,group*83+17,0);
                var shoulder=SpawnModel("rock_medium",new Vector3(x+1.42f,y,z-.60f),.84f+(group%2)*.19f);shoulder.transform.Rotate(0,group*59,0);
                SpawnModel("rock_small",new Vector3(x-.98f,y,z-.91f),.77f+(group%3)*.12f).transform.Rotate(0,group*31,0);
            }
            // Visible inner toes use the same sandstone family, with grass growing between them.
            Vector3[] toes={new Vector3(-10.90f,-1.52f,-6.90f),new Vector3(-11.26f,-1.52f,-1.20f),new Vector3(-9.48f,3.1f,6.14f),new Vector3(11.2f,-1.52f,-4.12f),new Vector3(11.52f,-1.52f,1.2f),new Vector3(8.6f,1.2f,7.18f)};
            for(int i=0;i<toes.Length;i++){
                SpawnModel(i%2==0?"rock_medium":"rock_small",toes[i],.75f+(i%3)*.12f).transform.Rotate(0,i*77,0);
            }
        }
    }
}
