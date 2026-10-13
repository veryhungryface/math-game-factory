// 골렘 합체 — 정오의 백색 염전 채석장, 흑요석 자동인형, 산화동 비례 봉인.
// 정답은 합체→면 타일→부피 기둥으로 상승하고, 오답만 카메라 흔들림과 균열을 쓴다.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.GolemHapche
{
    public partial class GolemHapcheGame
    {
        static readonly Color Salt = MgfLook.Hex("#F3F0DF");
        static readonly Color SaltBright = MgfLook.Hex("#FFFDF2");
        static readonly Color Obsidian = MgfLook.Hex("#111820");
        static readonly Color ObsidianEdge = MgfLook.Hex("#26313B");
        static readonly Color Copper = MgfLook.Hex("#B46A3C");
        static readonly Color CopperDark = MgfLook.Hex("#6F3B27");
        static readonly Color Cobalt = MgfLook.Hex("#1E4D8F");
        static readonly Color Sage = MgfLook.Hex("#98A8A0");
        static readonly Color Ink = MgfLook.Hex("#111820");

        Camera cam;
        float camBaseSize;
        Transform worldRoot, quarryRoot, fissureRoot, brineRoot;
        readonly Transform[] golems = new Transform[GolemRules.ShieldCount];
        readonly Transform[] golemHeads = new Transform[GolemRules.ShieldCount];
        readonly Transform[] golemArms = new Transform[GolemRules.ShieldCount];
        readonly Transform[] triangleShields = new Transform[GolemRules.ShieldCount];
        readonly Transform[] solidModels = new Transform[GolemRules.ShieldCount];
        readonly Vector3[] golemHomes = new Vector3[GolemRules.ShieldCount];
        readonly Transform[] saltFlags = new Transform[10];
        readonly Transform[] quarryCranes = new Transform[4];
        readonly Transform[] nails = new Transform[GolemRules.StartLives];
        readonly List<Transform> sealClusters = new List<Transform>();
        LineRenderer dragTrail3d, fissureLine, measureBeam;
        Material saltMat, saltBrightMat, obsidianMat, edgeMat, copperMat, copperGlowMat, cobaltMat, waterMat, crackMat;

        CanvasGroup titleG, hudG, feedbackG, endG, toastG;
        RectTransform rootRt, titleLogoRt, titleSealRt, conditionRt, goalRt, promptRt, measureRt, feedbackRt, endRt, restartRt, toastRt, rippleRt;
        Image titleSealImg, conditionImg, goalImg, promptImg, measureImg, feedbackImg, toastImg, rippleImg;
        TextMeshProUGUI titleLogoShadow, titleLogo, titleTag, titleMeta, titleBest, titleSealText;
        TextMeshProUGUI scoreText, progressText, livesText, timerText, conditionText, goalText, promptText, measureText;
        TextMeshProUGUI feedbackTitleText, feedbackFormulaText, endTitleText, endStatsText, endScoreText, restartText, toastText;
        readonly RectTransform[] shieldRt = new RectTransform[GolemRules.ShieldCount];
        readonly Image[] shieldOuter = new Image[GolemRules.ShieldCount];
        readonly Image[] shieldInner = new Image[GolemRules.ShieldCount];
        readonly TextMeshProUGUI[] shieldIndex = new TextMeshProUGUI[GolemRules.ShieldCount];
        readonly TextMeshProUGUI[] shieldData = new TextMeshProUGUI[GolemRules.ShieldCount];
        readonly Vector2[] shieldHome = new Vector2[GolemRules.ShieldCount];
        RectTransform guideLineRt, guideHandRt;
        Image guideLineImg, guideHandImg;
        TextMeshProUGUI guideHandText;
        Sprite cutSprite, thinCutSprite, ringSprite, diamondSprite;

        float worldClock, titleClock, rippleClock = 9f, toastClock = 9f, feedbackVisualClock = 9f;
        float guideBoost, cardRefuseClock, visualHitStop, mergeClock = -1f, wrongClock = -1f, titleTransitionClock = -1f;
        float displayedScore, endDisplayedScore;
        int lastShownScore = -1, lastShownEndScore = -1, lastSecond = -1, layoutW = -1, layoutH = -1;
        int mergeA = -1, mergeB = -1, wrongA = -1, wrongB = -1, droppingNail = -1;
        bool wideLayout;
        Vector3 feedbackMidpoint;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color; image.sprite = sprite; image.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color,
            float width = 360f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.35f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        static Sprite MakePolygonSprite(int w, int h, float cut, bool ring)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = ring ? "GolemRing" : "GolemCut", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            float border = Mathf.Max(2f, Mathf.Min(w,h) * .055f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float edge = Mathf.Min(Mathf.Min(x + .5f, w - x - .5f), Mathf.Min(y + .5f, h - y - .5f));
                float diagA = x + y + 1f;
                float diagB = (w - x) + y;
                float diagC = x + (h - y);
                float diagD = (w - x) + (h - y);
                float d = Mathf.Min(edge, Mathf.Min(Mathf.Min(diagA-cut,diagB-cut),Mathf.Min(diagC-cut,diagD-cut)) * .7071f);
                float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d-border) / 1.4f) : Mathf.Clamp01(d + .6f);
                px[y*w+x] = new Color32(255,255,255,(byte)(Mathf.Clamp01(a)*255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float b = ring ? 0 : cut + 2;
            return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(.5f,.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b,b,b,b));
        }

        static Texture2D MakeSaltStrataTexture()
        {
            const int size=64;
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name="SaltStrataTexture",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            var px=new Color32[size*size];
            for(int y=0;y<size;y++)
            for(int x=0;x<size;x++)
            {
                float seam=Mathf.Abs(Mathf.Sin((y+Mathf.Sin(x*.23f)*2.4f)*.48f));
                float grain=(Mathf.Sin(x*1.71f+y*.37f)+1f)*.025f;
                byte v=(byte)Mathf.Clamp(226f+seam*22f+grain*255f,0,255);
                px[y*size+x]=new Color32(v,(byte)Mathf.Min(255,v+2),(byte)Mathf.Min(255,v+5),255);
            }
            tex.SetPixels32(px); tex.Apply(false,true); return tex;
        }

        static Texture2D MakeObsidianFractureTexture()
        {
            const int size=64;
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name="ObsidianFractureTexture",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Point};
            var px=new Color32[size*size];
            for(int y=0;y<size;y++)
            for(int x=0;x<size;x++)
            {
                float f1=Mathf.Abs(((x*3+y*5)%37)-18.5f);
                float f2=Mathf.Abs(((x*7-y*2+128)%43)-21.5f);
                bool crack=f1<1.15f||f2<.85f;
                byte v=(byte)(crack?58:18+((x*11+y*7)%9));
                px[y*size+x]=new Color32(v,(byte)(v+5),(byte)(v+10),255);
            }
            tex.SetPixels32(px); tex.Apply(false,true); return tex;
        }

        void BuildWorld()
        {
            // WebGL IL2CPP가 CreatePrimitive(Cylinder)의 문자열 기반 콜라이더 생성을 제거하지 않게 보존한다.
            var colliderTypes = new GameObject("ColliderTypes");
            colliderTypes.AddComponent<CapsuleCollider>();
            Destroy(colliderTypes);
            MgfLook.Sky(MgfLook.Hex("#BFD9EA"), MgfLook.Hex("#EEF2E8"), MgfLook.Hex("#D9E5EA"), 1.05f);
            MgfLook.Sun(new Vector3(52,-38,22), MgfLook.Hex("#FFF9D9"), 1.34f, .72f);
            cam = MgfLook.Camera(new Vector3(13.8f,15.2f,-18.5f), new Vector3(0,.7f,2.4f), 32f);
            cam.orthographic = true; cam.orthographicSize = 8.2f; camBaseSize = cam.orthographicSize;

            Texture2D saltStrata=MakeSaltStrataTexture();
            Texture2D obsidianFracture=MakeObsidianFractureTexture();
            saltMat = MgfLook.Lit(Salt,.08f,.01f,null,saltStrata); saltMat.mainTextureScale=new Vector2(3f,3f);
            saltBrightMat = MgfLook.Lit(SaltBright,.18f,.01f,SaltBright*.055f,saltStrata); saltBrightMat.mainTextureScale=new Vector2(2f,2f);
            obsidianMat = MgfLook.Lit(Obsidian,.46f,.16f,Obsidian*.035f,obsidianFracture); obsidianMat.mainTextureScale=new Vector2(2f,2f);
            edgeMat = MgfLook.Lit(ObsidianEdge,.34f,.10f);
            copperMat = MgfLook.Lit(Copper,.55f,.58f,Copper*.07f);
            copperGlowMat = MgfLook.Lit(MgfLook.Hex("#D98A55"),.68f,.64f,MgfLook.Hex("#C96D3C")*.35f);
            cobaltMat = MgfLook.Lit(Cobalt,.28f,.12f,Cobalt*.09f);
            waterMat = MgfLook.Lit(new Color(.36f,.63f,.72f,.88f),.88f,.12f,MgfLook.Hex("#1E5E82")*.14f);
            crackMat = MgfLook.Alpha(new Color(.08f,.14f,.20f,.92f));

            worldRoot = new GameObject("NoonSaltQuarry").transform;
            quarryRoot = new GameObject("WhiteTerraces").transform; quarryRoot.SetParent(worldRoot,false);
            brineRoot = new GameObject("ShallowBrine").transform; brineRoot.SetParent(worldRoot,false);
            fissureRoot = new GameObject("TidalFissure").transform; fissureRoot.SetParent(worldRoot,false);

            BuildQuarry(); BuildFissure(); BuildGolems(); BuildNails();
            dragTrail3d = MakeLine("ObsidianDragTrail", MgfLook.Alpha(new Color(Copper.r,Copper.g,Copper.b,.58f)), .10f, 12);
            dragTrail3d.enabled = false;
            measureBeam = MakeLine("MeasureBeam", MgfLook.Alpha(new Color(Cobalt.r,Cobalt.g,Cobalt.b,.38f)), .07f, 2);
            measureBeam.enabled = false;
        }

        void BuildQuarry()
        {
            MgfLook.Block("QuarryFloor",new Vector3(0,-1.15f,1.5f),new Vector3(22f,1.0f,18f),.18f,saltMat,quarryRoot);
            // 층상 단면이 화면 뒤를 채워 단일 백색 플랫폼과 전혀 다른 수직 실루엣을 만든다.
            for(int layer=0;layer<5;layer++)
            {
                float y=.20f+layer*.88f, z=8.2f+layer*.20f;
                MgfLook.Block("RearSaltStratum_"+layer,new Vector3(0,y,z),new Vector3(19.5f-layer*.55f,.82f,1.65f),.08f,layer%2==0?saltMat:saltBrightMat,quarryRoot);
                for(int seam=0;seam<6;seam++)
                    MgfLook.Block("CopperSeam_"+layer+"_"+seam,new Vector3(-7.5f+seam*3.0f,y-.36f,z-.88f),new Vector3(1.5f,.045f,.06f),.01f,copperMat,quarryRoot);
            }
            for(int side=-1;side<=1;side+=2)
            for(int step=0;step<5;step++)
            {
                float x=side*(8.1f-step*.36f), y=-.22f+step*.58f, z=-3.5f+step*2.15f;
                MgfLook.Block("SideCliff_"+side+"_"+step,new Vector3(x,y,z),new Vector3(2.3f,1.05f,3.0f),.08f,step%2==0?saltMat:saltBrightMat,quarryRoot);
            }
            for(int r=0;r<4;r++)
            {
                float z=-4.3f+r*2.72f, w=15.8f-r*.55f;
                MgfLook.Block("WorkingBench_"+r,new Vector3(-.55f,-.40f+r*.20f,z),new Vector3(w,.50f,1.72f),.07f,r%2==0?saltBrightMat:saltMat,quarryRoot);
                MgfLook.Block("CanalBankL_"+r,new Vector3(-w*.30f,-.06f+r*.20f,z+.93f),new Vector3(w*.34f,.17f,.16f),.025f,copperMat,quarryRoot);
                MgfLook.Block("CanalBankR_"+r,new Vector3(w*.30f,-.06f+r*.20f,z+.93f),new Vector3(w*.34f,.17f,.16f),.025f,copperMat,quarryRoot);
                MgfLook.Block("FlowingBrine_"+r,new Vector3(0,-.10f+r*.20f,z+.93f),new Vector3(w*.56f,.055f,.32f),.08f,waterMat,brineRoot);
                for(int k=0;k<6;k++)
                {
                    float x=-6.3f+k*2.45f+(r%2)*.42f;
                    Transform crystal=CreateMeshPart("SaltCrystal_"+r+"_"+k,SharpShardMesh(5),new Vector3(x,.05f+r*.20f,z+.58f),new Vector3(.30f,.65f+(k%3)*.18f,.30f),saltBrightMat,quarryRoot);
                    crystal.localRotation=Quaternion.Euler(k%2==0?-7:8,k*31f,k%2==0?8:-8);
                }
            }
            MgfLook.Block("DeepCobaltCut",new Vector3(3.4f,-.42f,5.2f),new Vector3(7.2f,.16f,3.1f),.04f,cobaltMat,quarryRoot);
            for(int i=0;i<3;i++)
                MgfLook.Block("BrineReservoir_"+i,new Vector3(-5.1f+i*4.8f,-.14f,6.0f+(i%2)*.62f),new Vector3(3.15f,.08f,1.55f),.22f,waterMat,brineRoot);

            for(int i=0;i<saltFlags.Length;i++)
            {
                float x=-7.3f+i*1.62f;
                var pole=MgfLook.Prim(PrimitiveType.Cylinder,"SurveyPole_"+i,new Vector3(x,1.25f,8.0f+(i%2)*.4f),new Vector3(.06f,1.35f,.06f),copperMat,quarryRoot,false);
                saltFlags[i]=MgfLook.Block("SaltFlag_"+i,new Vector3(.32f,.75f,0),new Vector3(.62f,.30f,.05f),.03f,i%2==0?cobaltMat:copperMat,pole.transform).transform;
            }
            for(int i=0;i<quarryCranes.Length;i++)
            {
                var root=new GameObject("QuarryCrane_"+i).transform; root.SetParent(quarryRoot,false); root.localPosition=new Vector3(-7.1f+i*4.6f,.1f,7.5f);
                MgfLook.Prim(PrimitiveType.Cylinder,"Post",new Vector3(0,1.3f,0),new Vector3(.10f,1.35f,.10f),copperMat,root,false);
                quarryCranes[i]=MgfLook.Block("Arm",new Vector3(.7f,2.35f,0),new Vector3(1.5f,.13f,.13f),.04f,copperMat,root).transform;
                Destroy(quarryCranes[i].GetComponent<Collider>());
                var pulley=MgfLook.Prim(PrimitiveType.Cylinder,"Pulley",new Vector3(1.38f,2.31f,0),new Vector3(.28f,.07f,.28f),obsidianMat,root,false); pulley.transform.localRotation=Quaternion.Euler(90,0,0);
                MgfLook.Block("HangingCable",new Vector3(1.38f,1.52f,0),new Vector3(.035f,1.45f,.035f),.01f,edgeMat,root);
                Transform bucket=CreateMeshPart("ObsidianBucket",TaperedPrismMesh(),new Vector3(1.38f,.72f,0),new Vector3(.52f,.36f,.52f),obsidianMat,root);
                bucket.localRotation=Quaternion.Euler(0,i*35f,0);
            }
            for(int rail=-1;rail<=1;rail+=2)
                MgfLook.Block("QuarryRail_"+rail,new Vector3(rail*.32f,-.19f,-5.55f),new Vector3(.10f,.10f,8.3f),.02f,copperMat,quarryRoot);
            for(int i=0;i<7;i++)
            {
                var sleeper=MgfLook.Prim(PrimitiveType.Cylinder,"RailSleeper_"+i,new Vector3(0,-.23f,-8.5f+i*1.25f),new Vector3(.075f,.72f,.075f),edgeMat,quarryRoot,false);
                sleeper.transform.localRotation=Quaternion.Euler(0,0,90);
            }
        }

        void BuildFissure()
        {
            fissureLine = MakeLine("AdvancingFissure", crackMat, .22f, 15);
            fissureLine.transform.SetParent(fissureRoot,false);
            fissureLine.useWorldSpace=false;
            var points=new Vector3[15];
            for(int i=0;i<points.Length;i++)
                points[i]=new Vector3(6.7f+Mathf.Sin(i*2.31f)*.52f,.02f,-5.4f+i*.83f);
            fissureLine.SetPositions(points);
            for(int i=0;i<5;i++)
            {
                var branch=MakeLine("FissureBranch_"+i,crackMat,.10f,3); branch.transform.SetParent(fissureRoot,false); branch.useWorldSpace=false;
                Vector3 p=points[2+i*2]; branch.SetPosition(0,p); branch.SetPosition(1,p+new Vector3(i%2==0?-.65f:.58f,0,.34f)); branch.SetPosition(2,p+new Vector3(i%2==0?-1.05f:.96f,0,.08f));
            }
        }

        void BuildGolems()
        {
            Vector3[] pos = {
                new Vector3(-5.8f,.15f,-3.7f), new Vector3(-2.1f,.15f,-3.7f), new Vector3(1.7f,.15f,-3.7f),
                new Vector3(-4.0f,.30f,.15f), new Vector3(-.25f,.30f,.15f), new Vector3(3.5f,.30f,.15f)
            };
            for(int i=0;i<GolemRules.ShieldCount;i++)
            {
                golemHomes[i]=pos[i]; golems[i]=CreateGolem(i,pos[i]);
            }
        }

        Transform CreateGolem(int index, Vector3 pos)
        {
            var root=new GameObject("ObsidianGolem_"+GolemRules.Letters[index]).transform; root.SetParent(worldRoot,false); root.localPosition=pos;
            CreateMeshPart("FracturedTorso",TaperedPrismMesh(),new Vector3(0,1.22f,0),new Vector3(1.12f,1.42f,.78f),obsidianMat,root);
            Transform chest=CreateMeshPart("ObsidianChestPlate",SharpShardMesh(6),new Vector3(0,1.35f,-.45f),new Vector3(.78f,.70f,.15f),edgeMat,root); chest.localRotation=Quaternion.Euler(0,0,180);
            golemHeads[index]=CreateMeshPart("FacetedHead",TaperedPrismMesh(),new Vector3(0,2.15f,-.02f),new Vector3(.72f,.62f,.62f),obsidianMat,root);
            MgfLook.Block("Eye",new Vector3(0,2.17f,-.35f),new Vector3(.40f,.08f,.06f),.02f,cobaltMat,root);
            for(int s=-1;s<=1;s+=2)
            {
                Transform shoulder=CreateMeshPart("FracturedShoulder",TaperedPrismMesh(),new Vector3(s*.72f,1.62f,0),new Vector3(.50f,.46f,.64f),edgeMat,root); shoulder.localRotation=Quaternion.Euler(0,0,s*13f);
                var arm=CreateMeshPart("EtchedCopperArm",TaperedPrismMesh(),new Vector3(s*.85f,.98f,-.02f),new Vector3(.30f,.92f,.30f),copperMat,root);
                if(s==1) golemArms[index]=arm;
                var joint=MgfLook.Prim(PrimitiveType.Cylinder,"CopperJoint",new Vector3(s*.84f,1.40f,-.02f),new Vector3(.24f,.10f,.24f),copperGlowMat,root,false); joint.transform.localRotation=Quaternion.Euler(0,0,90);
                CreateMeshPart("ObsidianFoot",TaperedPrismMesh(),new Vector3(s*.34f,.25f,.05f),new Vector3(.50f,.48f,.72f),obsidianMat,root);
            }
            var shield=new GameObject("TriangularShield"); shield.transform.SetParent(root,false); shield.transform.localPosition=new Vector3(0,1.20f,-.62f); shield.transform.localRotation=Quaternion.Euler(8,0,0); triangleShields[index]=shield.transform;
            shield.AddComponent<MeshFilter>().sharedMesh=TrianglePrismMesh();
            var sr=shield.AddComponent<MeshRenderer>(); sr.sharedMaterial=obsidianMat; sr.shadowCastingMode=ShadowCastingMode.On; sr.receiveShadows=true;
            var bc=shield.AddComponent<BoxCollider>(); bc.size=new Vector3(1.18f,1.18f,.18f); bc.center=new Vector3(0,-.05f,0);
            var rim=MgfLook.Block("ShieldCopperRim",new Vector3(0,-.09f,-.11f),new Vector3(.78f,.08f,.08f),.02f,copperMat,shield.transform); rim.transform.localRotation=Quaternion.Euler(0,0,30);
            solidModels[index]=MgfLook.Block("SimilaritySolid",new Vector3(0,1.32f,-.70f),Vector3.one,.08f,copperMat,root).transform;
            solidModels[index].gameObject.SetActive(false);
            var label=MgfText.World(GolemRules.Letters[index],new Vector3(0,2.95f,0),1.2f,Cobalt,root); label.fontStyle=FontStyles.Bold; label.transform.localRotation=Quaternion.Euler(42,0,0);
            return root;
        }

        static Mesh TrianglePrismMesh()
        {
            var v=new[]{new Vector3(0,.72f,-.10f),new Vector3(-.70f,-.52f,-.10f),new Vector3(.70f,-.52f,-.10f),new Vector3(0,.72f,.10f),new Vector3(-.70f,-.52f,.10f),new Vector3(.70f,-.52f,.10f)};
            var t=new[]{0,1,2,5,4,3,0,3,4,0,4,1,1,4,5,1,5,2,2,5,3,2,3,0};
            var m=new Mesh{name="ObsidianTriangleShield"}; m.vertices=v; m.triangles=t; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static Mesh TaperedPrismMesh()
        {
            var v=new[]{
                new Vector3(-.55f,-.5f,-.50f),new Vector3(.55f,-.5f,-.50f),new Vector3(.48f,-.5f,.50f),new Vector3(-.48f,-.5f,.50f),
                new Vector3(-.42f,.5f,-.42f),new Vector3(.46f,.5f,-.40f),new Vector3(.38f,.5f,.43f),new Vector3(-.45f,.5f,.40f)};
            var t=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
            var m=new Mesh{name="FracturedTaperedPrism"}; m.vertices=v; m.triangles=t; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static Mesh SharpShardMesh(int sides)
        {
            sides=Mathf.Clamp(sides,4,8);
            var v=new Vector3[sides+2];
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2f/sides;
                v[i]=new Vector3(Mathf.Cos(a)*.48f,-.5f,Mathf.Sin(a)*.48f);
            }
            v[sides]=new Vector3(.10f,.5f,-.07f); v[sides+1]=new Vector3(0,-.5f,0);
            var t=new int[sides*6];
            for(int i=0;i<sides;i++)
            {
                int n=(i+1)%sides, p=i*6;
                t[p]=i;t[p+1]=n;t[p+2]=sides;
                t[p+3]=n;t[p+4]=i;t[p+5]=sides+1;
            }
            var m=new Mesh{name="SharpSaltShard"}; m.vertices=v; m.triangles=t; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static Transform CreateMeshPart(string name,Mesh mesh,Vector3 pos,Vector3 scale,Material mat,Transform parent)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var mr=go.AddComponent<MeshRenderer>(); mr.sharedMaterial=mat; mr.shadowCastingMode=ShadowCastingMode.On; mr.receiveShadows=true;
            return go.transform;
        }

        void BuildNails()
        {
            for(int i=0;i<nails.Length;i++)
            {
                var n=new GameObject("SealNail_"+i).transform; n.SetParent(worldRoot,false); n.localPosition=new Vector3(5.25f+i*.55f,.55f,-1.2f);
                var shaft=MgfLook.Prim(PrimitiveType.Cylinder,"Shaft",new Vector3(0,.45f,0),new Vector3(.11f,.48f,.11f),copperMat,n,false); shaft.transform.localRotation=Quaternion.Euler(0,0,0);
                MgfLook.Block("Head",new Vector3(0,.95f,0),new Vector3(.42f,.16f,.42f),.04f,copperGlowMat,n);
                nails[i]=n;
            }
        }

        LineRenderer MakeLine(string name, Material mat, float width, int count)
        {
            var go=new GameObject(name); var lr=go.AddComponent<LineRenderer>(); lr.sharedMaterial=mat; lr.positionCount=count;
            lr.startWidth=lr.endWidth=width; lr.useWorldSpace=true; lr.numCapVertices=2; lr.numCornerVertices=2; lr.shadowCastingMode=ShadowCastingMode.Off; lr.receiveShadows=false;
            return lr;
        }

        void BuildUi()
        {
            cutSprite=MakePolygonSprite(96,64,15,false); thinCutSprite=MakePolygonSprite(96,64,9,false);
            ringSprite=MakePolygonSprite(72,72,17,true); diamondSprite=MakePolygonSprite(72,72,30,false);
            var canvas=MgfText.Canvas;
            rootRt=R("GolemHapcheUi",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
            rootRt.anchorMin=Vector2.zero; rootRt.anchorMax=Vector2.one; rootRt.sizeDelta=Vector2.zero;
            BuildTitleUi(); BuildHudUi(); BuildFeedbackUi(); BuildEndUi(); BuildOverlayUi(); LayoutUi(true);
        }

        void BuildTitleUi()
        {
            var rt=R("TitleWorldOverlay",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.sizeDelta=Vector2.zero;
            titleG=rt.gameObject.AddComponent<CanvasGroup>();
            titleLogoShadow=Txt(rt,"골렘 합체",new Vector2(.5f,.77f),new Vector2(5,-6),57,Copper,370); titleLogoShadow.fontStyle=FontStyles.Bold; titleLogoRt=titleLogoShadow.rectTransform;
            titleLogo=Txt(rt,"골렘 합체",new Vector2(.5f,.77f),Vector2.zero,57,Obsidian,370); titleLogo.fontStyle=FontStyles.Bold; titleLogo.outlineWidth=.12f; titleLogo.outlineColor=SaltBright;
            titleTag=Txt(rt,"닮은 방패를 포개라",new Vector2(.5f,.68f),Vector2.zero,18,Cobalt,340); titleTag.fontStyle=FontStyles.Bold;
            titleMeta=Txt(rt,"중학교 2학년 · 도형의 닮음",new Vector2(.5f,.62f),Vector2.zero,12,Ink,330);
            int best=PlayerPrefs.GetInt("golem-hapche.best",0);
            titleBest=Txt(rt,best>0?"최고 봉인력 "+best:"",new Vector2(.5f,.54f),Vector2.zero,13,CopperDark,300);
            titleSealImg=Img(rt,"CrackedSealStone",new Vector2(.5f,.20f),Vector2.zero,new Vector2(246,96),Obsidian,cutSprite); titleSealRt=titleSealImg.rectTransform;
            var inner=Img(titleSealRt,"CopperFault",new Vector2(.5f,.5f),Vector2.zero,new Vector2(230,80),Copper,thinCutSprite);
            titleSealText=Txt(inner.transform,"금 간 봉인석 누르기",new Vector2(.5f,.5f),Vector2.zero,22,SaltBright,218); titleSealText.fontStyle=FontStyles.Bold;
            Txt(rt,"90초 · 6계약 · 성공: 첫 시도 5/6 + 마지막 두 계약 2/2",new Vector2(.5f,.10f),Vector2.zero,12,Ink,340);
        }

        void BuildHudUi()
        {
            var rt=R("PlayHud",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.sizeDelta=Vector2.zero;
            hudG=rt.gameObject.AddComponent<CanvasGroup>();
            var scorePlate=Img(rt,"ScorePlate",new Vector2(.13f,.965f),Vector2.zero,new Vector2(96,54),Obsidian,cutSprite);
            scoreText=Txt(scorePlate.transform,"0",new Vector2(.5f,.5f),Vector2.zero,22,SaltBright,88); scoreText.fontStyle=FontStyles.Bold;
            progressText=Txt(rt,"0 / 6",new Vector2(.5f,.965f),Vector2.zero,18,Ink,120); progressText.fontStyle=FontStyles.Bold;
            timerText=Txt(rt,"90초",new Vector2(.87f,.965f),Vector2.zero,18,Cobalt,86); timerText.fontStyle=FontStyles.Bold;
            livesText=Txt(rt,"◆ ◆ ◆",new Vector2(.5f,.915f),Vector2.zero,17,CopperDark,150);
            conditionImg=Img(rt,"ConditionSeal",new Vector2(.16f,.865f),Vector2.zero,new Vector2(122,44),new Color(SaltBright.r,SaltBright.g,SaltBright.b,.97f),thinCutSprite); conditionRt=conditionImg.rectTransform;
            conditionText=Txt(conditionRt,"SSS 닮음",new Vector2(.5f,.5f),Vector2.zero,14,Cobalt,112); conditionText.fontStyle=FontStyles.Bold;
            goalImg=Img(rt,"PersistentGoal",new Vector2(.5f,.86f),Vector2.zero,new Vector2(350,52),SaltBright,thinCutSprite); goalRt=goalImg.rectTransform;
            goalText=Txt(goalRt,"닮은 방패 두 개를 포개어 균열을 막으시오.",new Vector2(.5f,.5f),Vector2.zero,14,Ink,330); goalText.fontStyle=FontStyles.Bold;
            promptImg=Img(rt,"ContractSlate",new Vector2(.5f,.765f),Vector2.zero,new Vector2(354,104),new Color(Obsidian.r,Obsidian.g,Obsidian.b,.96f),cutSprite); promptRt=promptImg.rectTransform;
            promptText=Txt(promptRt,"",new Vector2(.5f,.50f),Vector2.zero,16,SaltBright,330); promptText.fontStyle=FontStyles.Bold;
            measureImg=Img(rt,"OrthographicMeasure",new Vector2(.5f,.655f),Vector2.zero,new Vector2(348,82),new Color(Cobalt.r,Cobalt.g,Cobalt.b,.97f),cutSprite); measureRt=measureImg.rectTransform;
            measureText=Txt(measureRt,"방패를 잡으면\n두 측정판이 펼쳐진다",new Vector2(.5f,.5f),Vector2.zero,14,SaltBright,326); measureText.fontStyle=FontStyles.Bold;

            for(int i=0;i<GolemRules.ShieldCount;i++)
            {
                shieldOuter[i]=Img(rt,"ShieldSlot_"+GolemRules.Letters[i],new Vector2(.5f,.5f),Vector2.zero,new Vector2(158,116),Copper,cutSprite); shieldRt[i]=shieldOuter[i].rectTransform;
                shieldInner[i]=Img(shieldRt[i],"ObsidianShieldFace",new Vector2(.5f,.5f),Vector2.zero,new Vector2(148,106),Obsidian,thinCutSprite);
                shieldIndex[i]=Txt(shieldInner[i].transform,GolemRules.Letters[i],new Vector2(.13f,.78f),Vector2.zero,22,Copper,38); shieldIndex[i].fontStyle=FontStyles.Bold;
                shieldData[i]=Txt(shieldInner[i].transform,"",new Vector2(.56f,.48f),Vector2.zero,17,SaltBright,116); shieldData[i].fontStyle=FontStyles.Bold; shieldData[i].lineSpacing=-8;
            }
        }

        void BuildFeedbackUi()
        {
            feedbackImg=Img(rootRt,"MathReveal",new Vector2(.5f,.51f),Vector2.zero,new Vector2(360,188),Obsidian,cutSprite); feedbackRt=feedbackImg.rectTransform; feedbackG=feedbackRt.gameObject.AddComponent<CanvasGroup>();
            feedbackTitleText=Txt(feedbackRt,"비례 봉인",new Vector2(.5f,.75f),Vector2.zero,24,SaltBright,330); feedbackTitleText.fontStyle=FontStyles.Bold;
            feedbackFormulaText=Txt(feedbackRt,"",new Vector2(.5f,.41f),Vector2.zero,17,SaltBright,330); feedbackFormulaText.fontStyle=FontStyles.Bold;
            feedbackG.alpha=0; feedbackG.blocksRaycasts=false;
        }

        void BuildEndUi()
        {
            var rt=R("EndQuarrySeal",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.sizeDelta=Vector2.zero; endRt=rt; endG=rt.gameObject.AddComponent<CanvasGroup>();
            var resultSlate=Img(rt,"EndResultSlate",new Vector2(.5f,.57f),Vector2.zero,new Vector2(360,276),new Color(Obsidian.r,Obsidian.g,Obsidian.b,.94f),cutSprite);
            endTitleText=Txt(resultSlate.transform,"채석장 봉인 완료",new Vector2(.5f,.80f),Vector2.zero,30,SaltBright,336); endTitleText.fontStyle=FontStyles.Bold;
            endScoreText=Txt(resultSlate.transform,"0",new Vector2(.5f,.53f),Vector2.zero,50,Copper,300); endScoreText.fontStyle=FontStyles.Bold;
            endStatsText=Txt(resultSlate.transform,"",new Vector2(.5f,.21f),Vector2.zero,17,SaltBright,334); endStatsText.fontStyle=FontStyles.Bold;
            var outer=Img(rt,"RestartSeal",new Vector2(.5f,.20f),Vector2.zero,new Vector2(246,96),Obsidian,cutSprite); restartRt=outer.rectTransform;
            var inner=Img(outer.transform,"RestartCopper",new Vector2(.5f,.5f),Vector2.zero,new Vector2(230,80),Copper,thinCutSprite);
            restartText=Txt(inner.transform,"다시 봉인",new Vector2(.5f,.5f),Vector2.zero,24,SaltBright,210); restartText.fontStyle=FontStyles.Bold;
        }

        void BuildOverlayUi()
        {
            toastImg=Img(rootRt,"RefusalToast",new Vector2(.5f,.59f),Vector2.zero,new Vector2(344,66),new Color(CopperDark.r,CopperDark.g,CopperDark.b,.97f),cutSprite); toastRt=toastImg.rectTransform; toastG=toastRt.gameObject.AddComponent<CanvasGroup>();
            toastText=Txt(toastRt,"",new Vector2(.5f,.5f),Vector2.zero,14,SaltBright,324); toastText.fontStyle=FontStyles.Bold; toastG.alpha=0;
            rippleImg=Img(rootRt,"TapRipple",new Vector2(.5f,.5f),Vector2.zero,new Vector2(74,74),new Color(Copper.r,Copper.g,Copper.b,.85f),ringSprite); rippleRt=rippleImg.rectTransform; rippleRt.gameObject.SetActive(false);
            guideLineImg=Img(rootRt,"GuidePath",new Vector2(.5f,.5f),Vector2.zero,new Vector2(12,160),new Color(Copper.r,Copper.g,Copper.b,.72f),thinCutSprite); guideLineRt=guideLineImg.rectTransform;
            guideHandImg=Img(rootRt,"GuideHand",new Vector2(.5f,.5f),Vector2.zero,new Vector2(74,74),Copper,diamondSprite); guideHandRt=guideHandImg.rectTransform;
            guideHandText=Txt(guideHandRt,"끌기",new Vector2(.5f,.5f),Vector2.zero,14,SaltBright,68); guideHandText.fontStyle=FontStyles.Bold;
            guideLineRt.gameObject.SetActive(false); guideHandRt.gameObject.SetActive(false);
        }

        void LayoutUi(bool force)
        {
            if(!force && layoutW==Screen.width && layoutH==Screen.height) return;
            // Unity의 Screen.width는 고DPI 세로 태블릿에서 CSS 폭의 2배가 될 수 있다.
            // 절대 픽셀 수가 아니라 종횡비만으로 가로 3존/세로 스택을 선택한다.
            layoutW=Screen.width; layoutH=Screen.height; wideLayout=Screen.width>Screen.height*1.18f;
            conditionRt.gameObject.SetActive(wideLayout);
            if(current!=null) promptText.text=wideLayout?current.prompt:GolemRules.ConditionName(current.kind)+"\n"+current.prompt;
            if(wideLayout)
            {
                // 가로 화면은 왼쪽 조건 / 가운데 목표·발문 / 오른쪽 측정의 세 독립 열이다.
                timerText.rectTransform.anchorMin=timerText.rectTransform.anchorMax=new Vector2(.90f,.965f); timerText.rectTransform.sizeDelta=new Vector2(76,43);
                conditionRt.anchorMin=conditionRt.anchorMax=new Vector2(.105f,.865f); conditionRt.sizeDelta=new Vector2(112,48);
                goalRt.anchorMin=goalRt.anchorMax=new Vector2(.50f,.895f); goalRt.sizeDelta=new Vector2(300,54); goalText.fontSize=13;
                promptRt.anchorMin=promptRt.anchorMax=new Vector2(.50f,.735f); promptRt.sizeDelta=new Vector2(320,72); promptText.fontSize=15;
                measureRt.anchorMin=measureRt.anchorMax=new Vector2(.885f,.815f); measureRt.sizeDelta=new Vector2(158,132); measureText.fontSize=12;
                goalText.rectTransform.sizeDelta=new Vector2(282,48); promptText.rectTransform.sizeDelta=new Vector2(300,62); measureText.rectTransform.sizeDelta=new Vector2(142,116);
                for(int i=0;i<6;i++)
                {
                    float x=.18f+(i%3)*.32f, y=.45f-(i/3)*.28f;
                    Vector2 size=new Vector2(166,116);
                    if(runPhase==RunPhase.Practice)
                    {
                        if(i<2) { x=i==0?.29f:.71f; y=.39f; size=new Vector2(214,148); }
                        else { x=.20f+(i-2)*.20f; y=.105f; size=new Vector2(104,72); }
                    }
                    shieldRt[i].anchorMin=shieldRt[i].anchorMax=new Vector2(x,y); shieldRt[i].sizeDelta=size; shieldHome[i]=Vector2.zero;
                    shieldInner[i].rectTransform.sizeDelta=size-new Vector2(10,10); shieldData[i].fontSize=runPhase==RunPhase.Practice&&i>=2?11:17;
                }
                titleLogo.fontSize=68; titleLogoShadow.fontSize=68; titleSealRt.sizeDelta=new Vector2(282,102);
            }
            else
            {
                timerText.rectTransform.anchorMin=timerText.rectTransform.anchorMax=new Vector2(.70f,.965f); timerText.rectTransform.sizeDelta=new Vector2(70,43);
                conditionRt.anchorMin=conditionRt.anchorMax=new Vector2(.17f,.865f); conditionRt.sizeDelta=new Vector2(114,44);
                goalRt.anchorMin=goalRt.anchorMax=new Vector2(.5f,.855f); goalRt.sizeDelta=new Vector2(346,54); goalText.fontSize=13;
                promptRt.anchorMin=promptRt.anchorMax=new Vector2(.5f,.745f); promptRt.sizeDelta=new Vector2(350,102); promptText.fontSize=14;
                measureRt.anchorMin=measureRt.anchorMax=new Vector2(.5f,.615f); measureRt.sizeDelta=new Vector2(346,78); measureText.fontSize=13;
                goalText.rectTransform.sizeDelta=new Vector2(326,48); promptText.rectTransform.sizeDelta=new Vector2(330,90); measureText.rectTransform.sizeDelta=new Vector2(326,64);
                for(int i=0;i<6;i++)
                {
                    float x=i%2==0?.27f:.73f, y=.485f-(i/2)*.18f;
                    Vector2 size=new Vector2(154,112);
                    if(runPhase==RunPhase.Practice)
                    {
                        if(i<2) { x=i==0?.27f:.73f; y=.405f; size=new Vector2(166,138); }
                        else { x=.14f+(i-2)*.24f; y=.13f; size=new Vector2(76,62); }
                    }
                    shieldRt[i].anchorMin=shieldRt[i].anchorMax=new Vector2(x,y); shieldRt[i].sizeDelta=size; shieldHome[i]=Vector2.zero;
                    shieldInner[i].rectTransform.sizeDelta=size-new Vector2(10,10); shieldData[i].fontSize=runPhase==RunPhase.Practice&&i>=2?9:16;
                }
                titleLogo.fontSize=57; titleLogoShadow.fontSize=57; titleSealRt.sizeDelta=new Vector2(246,96);
            }
            ApplyPracticeShieldState();
            for(int i=0;i<6;i++) if(!dragging || i!=dragShield) shieldRt[i].anchoredPosition=shieldHome[i];
        }

        void ApplyPracticeShieldState()
        {
            bool practice=runPhase==RunPhase.Practice;
            for(int i=0;i<6;i++)
            {
                bool locked=practice&&i>=2;
                shieldOuter[i].color=locked?new Color(Copper.r,Copper.g,Copper.b,.28f):Copper;
                shieldInner[i].color=locked?new Color(Obsidian.r,Obsidian.g,Obsidian.b,.42f):Obsidian;
                shieldIndex[i].color=locked?new Color(Copper.r,Copper.g,Copper.b,.48f):Copper;
                shieldData[i].color=locked?new Color(SaltBright.r,SaltBright.g,SaltBright.b,.46f):SaltBright;
            }
        }

        void SetScreen()
        {
            bool title=runPhase==RunPhase.Title, end=runPhase==RunPhase.End;
            titleG.alpha=title?1:0; titleG.interactable=title; titleG.blocksRaycasts=title;
            hudG.alpha=!title&&!end?1:0; hudG.interactable=!title&&!end; hudG.blocksRaycasts=!title&&!end;
            endG.alpha=end?1:0; endG.interactable=end; endG.blocksRaycasts=end;
            if(runPhase!=RunPhase.Feedback) { feedbackG.alpha=0; feedbackG.blocksRaycasts=false; }
            UpdateHud(true);
        }

        void ShowTitleVisual()
        {
            titleClock=0; titleTransitionClock=-1f; endDisplayedScore=0; lastShownEndScore=-1;
            titleSealImg.color=Obsidian; titleSealRt.localScale=Vector3.one;
            for(int i=0;i<golems.Length;i++) { golems[i].gameObject.SetActive(true); golems[i].localPosition=golemHomes[i]; golems[i].localScale=Vector3.one; }
        }

        void BeginTitleTransition()
        {
            titleTransitionClock=0f; MgfFx.Punch(titleSealRt,.13f,.28f);
            MgfFx.Glow(new Vector3(0,.25f,-1.2f),SaltBright,16,.28f);
        }

        void RefreshContractUi()
        {
            if(current==null) return;
            conditionText.text=GolemRules.ConditionName(current.kind);
            promptText.text=wideLayout?current.prompt:GolemRules.ConditionName(current.kind)+"\n"+current.prompt;
            for(int i=0;i<6;i++)
            {
                shieldData[i].text=runPhase==RunPhase.Practice&&i>=2?"연습 잠금":current.ShieldLabel(i);
                shieldOuter[i].color=Copper; shieldInner[i].color=Obsidian;
                shieldRt[i].localScale=Vector3.one; shieldRt[i].anchoredPosition=shieldHome[i];
            }
            ApplyPracticeShieldState();
            UpdateContractModels();
            measureText.text="방패를 잡으면\n두 측정판이 펼쳐진다";
            goalText.text=runPhase==RunPhase.Practice?"닮은 방패 두 개를 포개어 균열을 막으시오.":RunGoalLine();
            UpdateHud(true);
        }

        void ResetWorldForRun()
        {
            mergeClock=wrongClock=-1f; mergeA=mergeB=wrongA=wrongB=-1; droppingNail=-1;
            dragTrail3d.enabled=false; measureBeam.enabled=false; fissureRoot.localScale=Vector3.one;
            for(int i=0;i<golems.Length;i++) { if(golems[i]) { golems[i].gameObject.SetActive(true); golems[i].localPosition=golemHomes[i]; golems[i].localScale=Vector3.one; golems[i].localRotation=Quaternion.identity; } }
            for(int i=0;i<nails.Length;i++) { nails[i].gameObject.SetActive(true); nails[i].localPosition=new Vector3(5.25f+i*.55f,.55f,-1.2f); nails[i].localRotation=Quaternion.identity; }
            for(int i=0;i<sealClusters.Count;i++) if(sealClusters[i]) UnityEngine.Object.Destroy(sealClusters[i].gameObject);
            sealClusters.Clear();
        }

        void ResetWorldForContract(GolemContract contract)
        {
            mergeClock=wrongClock=-1f; dragTrail3d.enabled=false; measureBeam.enabled=false;
            for(int i=0;i<golems.Length;i++) { golems[i].localPosition=golemHomes[i]; golems[i].localScale=Vector3.one; golems[i].localRotation=Quaternion.identity; }
            UpdateContractModels();
        }

        void UpdateContractModels()
        {
            bool solids=current!=null&&current.kind==ContractKind.Volume;
            for(int i=0;i<GolemRules.ShieldCount;i++)
            {
                triangleShields[i].gameObject.SetActive(!solids);
                solidModels[i].gameObject.SetActive(solids);
                if(!solids) continue;
                ShieldData s=current.shields[i];
                float max=Mathf.Max(s.s1,Mathf.Max(s.s2,s.s3));
                solidModels[i].localScale=new Vector3(.62f+.58f*s.s1/max,.62f+.58f*s.s3/max,.38f+.52f*s.s2/max);
            }
        }

        void SwapGoalAfterPractice()
        {
            goalText.text=RunGoalLine();
            MgfFx.Punch(goalRt,.10f,.32f);
            HideGuide();
        }

        // 성공 조건 두 개(전체 첫 시도 5/6, 마지막 두 계약 2/2)를 플레이 내내 보이게 한다.
        // 어느 하나가 이미 깨졌으면 그 순간 알려 결과 화면에서야 드러나는 숨은 조건을 없앤다.
        string RunGoalLine()
        {
            int misses=st.firstAttemptTotal-st.firstAttemptCorrect;
            int lastTried=Mathf.Max(0,st.firstAttemptTotal-(GolemRules.TargetContracts-2));
            int lastMisses=lastTried-st.lastTwoFirstTry;
            if(lastMisses>0) return "마지막 두 계약 중 첫 시도 실패 · 봉인 불완전 확정\n남은 계약으로 비례를 익히시오";
            if(misses>=2) return "첫 시도 실수 2개 · 봉인 불완전 확정\n남은 계약으로 비례를 익히시오";
            if(st.contractIndex>=GolemRules.TargetContracts-2)
                return "마지막 두 계약 · 첫 시도에 맞혀야 봉인 ("+st.lastTwoFirstTry+" / 2)";
            return "첫 시도 정답 "+st.firstAttemptCorrect+" / "+st.firstAttemptTotal+"\n성공: 첫 시도 5/6 + 마지막 두 계약 2/2";
        }

        void BeginShieldDrag(int index)
        {
            shieldRt[index].SetAsLastSibling(); shieldRt[index].localScale=Vector3.one*1.12f;
            shieldOuter[index].color=SaltBright; shieldInner[index].color=Obsidian;
            golems[index].localScale=new Vector3(.92f,1.12f,.92f);
            dragTrail3d.enabled=true; dragTrail3d.positionCount=12;
            for(int i=0;i<12;i++) dragTrail3d.SetPosition(i,golems[index].position+Vector3.up*(.7f+i*.055f));
            RefreshMeasureUi(index,-1);
        }

        void MoveShieldDrag(int index, Vector2 screen)
        {
            shieldRt[index].position=screen+Vector2.up*34f;
            Vector3 world;
            if(MgfPointer.OnPlane(cam,.35f,out world))
            {
                Vector3 start=golems[index].position+Vector3.up*1.2f;
                for(int i=0;i<12;i++)
                {
                    float t=i/11f; Vector3 p=Vector3.Lerp(start,world,t); p.y+=Mathf.Sin(t*Mathf.PI)*.55f;
                    dragTrail3d.SetPosition(i,p);
                }
                golems[index].localPosition=Vector3.Lerp(golems[index].localPosition,golemHomes[index]+new Vector3(0,.45f,0),.24f);
            }
        }

        void ReturnShield(int index)
        {
            if(index<0)return;
            shieldRt[index].anchoredPosition=shieldHome[index]; shieldRt[index].localScale=Vector3.one;
            shieldOuter[index].color=Copper; shieldInner[index].color=Obsidian;
            golems[index].localPosition=golemHomes[index]; golems[index].localScale=Vector3.one;
            dragTrail3d.enabled=false; measureBeam.enabled=false;
        }

        void SetHoverShield(int index)
        {
            for(int i=0;i<6;i++)
            {
                if(i==dragShield) continue;
                shieldRt[i].localScale=i==index?Vector3.one*1.09f:Vector3.one;
                shieldOuter[i].color=i==index?SaltBright:Copper;
            }
            if(index>=0 && dragShield>=0)
            {
                measureBeam.enabled=true; measureBeam.SetPosition(0,golems[dragShield].position+Vector3.up*1.5f); measureBeam.SetPosition(1,golems[index].position+Vector3.up*1.5f);
            }
            else measureBeam.enabled=false;
        }

        void RefreshMeasureUi(int from, int to)
        {
            if(current==null||from<0)return;
            string left=GolemRules.Letters[from]+"  "+current.ShieldLabel(from).Replace("\n","  ");
            string right=to>=0?GolemRules.Letters[to]+"  "+current.ShieldLabel(to).Replace("\n","  "):"다른 방패 위로 이동 중";
            measureText.text=left+"\n"+right;
        }

        void FocusShield(int index)
        {
            for(int i=0;i<6;i++) shieldOuter[i].color=i==index?SaltBright:Copper;
            MgfFx.Punch(shieldRt[index],.08f,.18f);
        }

        void BeginFeedbackVisual(GolemContract contract, int a, int b, bool correct, bool practice, int combo)
        {
            feedbackVisualClock=0; feedbackG.alpha=1; feedbackRt.localScale=Vector3.one*.90f;
            feedbackImg.color=correct?new Color(Cobalt.r,Cobalt.g,Cobalt.b,.98f):new Color(CopperDark.r,CopperDark.g,CopperDark.b,.98f);
            feedbackTitleText.text=correct?(practice?"첫 비례 봉인 완성":"흑요석 합체 · 계약 봉인"):("방패 균열 · "+GolemRules.Letters[a]+"와 "+GolemRules.Letters[b]);
            feedbackFormulaText.text=correct?contract.reveal:GolemRules.WrongReason(contract,a,b);
            shieldOuter[a].color=correct?SaltBright:CopperDark; shieldOuter[b].color=correct?SaltBright:CopperDark;
            shieldRt[a].localScale=shieldRt[b].localScale=Vector3.one*(correct?1.12f:.96f);
            dragTrail3d.enabled=false; measureBeam.enabled=false;
            if(correct)
            {
                mergeA=a; mergeB=b; mergeClock=0f; visualHitStop=.085f;
                feedbackMidpoint=(golemHomes[a]+golemHomes[b])*.5f+new Vector3(0,.20f,0);
                MgfSfx.Play("correct",.48f); MgfFx.Glow(feedbackMidpoint+Vector3.up*1.8f,SaltBright,12+Math.Min(4,combo)*3,.26f);
                if(!practice) BuildSealCluster(contract,st.solved-1);
                MgfFx.Punch(scoreText.transform,.18f,.30f);
            }
            else
            {
                wrongA=a; wrongB=b; wrongClock=0f;
                MgfSfx.Play("wrong",.36f); MgfFx.Shake(cam,.075f,.25f);
                MgfFx.Burst((golemHomes[a]+golemHomes[b])*.5f+Vector3.up*1.2f,ObsidianEdge,11,1.8f,.13f);
                if(!practice)
                {
                    droppingNail=Mathf.Clamp(st.lives,0,nails.Length-1);
                    if(droppingNail>=0) MgfFx.Punch(nails[droppingNail],.24f,.24f);
                    fissureRoot.localScale=new Vector3(1f+(GolemRules.StartLives-st.lives)*.055f,1,1f+(GolemRules.StartLives-st.lives)*.075f);
                }
            }
            UpdateHud(true);
        }

        void CompleteFeedbackVisual(bool correct)
        {
            feedbackG.alpha=0; mergeClock=wrongClock=-1f; visualHitStop=0;
            for(int i=0;i<6;i++)
            {
                golems[i].localPosition=golemHomes[i]; golems[i].localScale=Vector3.one; golems[i].localRotation=Quaternion.identity;
                shieldRt[i].anchoredPosition=shieldHome[i]; shieldRt[i].localScale=Vector3.one; shieldOuter[i].color=Copper;
            }
            if(droppingNail>=0 && droppingNail<nails.Length) nails[droppingNail].gameObject.SetActive(false);
            droppingNail=-1; cam.orthographicSize=camBaseSize;
        }

        void BuildSealCluster(GolemContract contract,int index)
        {
            var root=new GameObject("PermanentRatioSeal_"+(index+1)).transform; root.SetParent(worldRoot,false);
            root.localPosition=new Vector3(4.2f+(index%3)*1.25f,.04f,3.8f+(index/3)*1.65f); root.localScale=Vector3.one*.02f;
            int area=Math.Max(1,contract.ratioLarge*contract.ratioLarge);
            int volume=Math.Max(1,contract.ratioLarge*contract.ratioLarge*contract.ratioLarge);
            CreateCombinedCells("AreaTiles",area,false,saltBrightMat,root,new Vector3(-.35f,.10f,0));
            CreateCombinedCells("VolumeColumns",volume,true,copperMat,root,new Vector3(.55f,.10f,0));
            MgfLook.Block("GiantTorso",new Vector3(0,1.15f,.25f),new Vector3(.70f,1.35f,.48f),.04f,obsidianMat,root);
            MgfLook.Block("GiantHead",new Vector3(0,2.02f,.25f),new Vector3(.50f,.46f,.46f),.03f,obsidianMat,root);
            MgfLook.Block("GiantEye",new Vector3(0,2.03f,-.01f),new Vector3(.30f,.06f,.04f),.01f,cobaltMat,root);
            sealClusters.Add(root);
        }

        void CreateCombinedCells(string name,int count,bool volume,Material mat,Transform parent,Vector3 offset)
        {
            var verts=new List<Vector3>(count*24); var tris=new List<int>(count*36); var norms=new List<Vector3>(count*24);
            int side=volume?Mathf.CeilToInt(Mathf.Pow(count,1f/3f)):Mathf.CeilToInt(Mathf.Sqrt(count));
            for(int i=0;i<count;i++)
            {
                int x=i%side, z=(i/side)%side, y=volume?i/(side*side):0;
                Vector3 center=offset+new Vector3((x-(side-1)*.5f)*.24f,.06f+y*.22f,(z-(side-1)*.5f)*.24f);
                AddBox(verts,norms,tris,center,new Vector3(.20f,volume?.20f:.08f,.20f));
            }
            var mesh=new Mesh{name=name+"Mesh"}; mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetTriangles(tris,0); mesh.RecalculateBounds();
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.AddComponent<MeshFilter>().sharedMesh=mesh; var mr=go.AddComponent<MeshRenderer>(); mr.sharedMaterial=mat; mr.shadowCastingMode=ShadowCastingMode.On; mr.receiveShadows=true;
        }

        static void AddBox(List<Vector3> v,List<Vector3> n,List<int> t,Vector3 c,Vector3 s)
        {
            Vector3 h=s*.5f;
            Vector3[] fn={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            for(int f=0;f<6;f++)
            {
                Vector3 normal=fn[f], u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up, w=Vector3.Cross(normal,u); int start=v.Count;
                v.Add(c+Vector3.Scale(normal+u+w,h)); v.Add(c+Vector3.Scale(normal-u+w,h)); v.Add(c+Vector3.Scale(normal-u-w,h)); v.Add(c+Vector3.Scale(normal+u-w,h));
                n.Add(normal);n.Add(normal);n.Add(normal);n.Add(normal);
                t.Add(start);t.Add(start+1);t.Add(start+2);t.Add(start);t.Add(start+2);t.Add(start+3);
            }
        }

        void ShowWrongReason(string misconception)
        {
            if(current==null)return;
            toastText.text="흉터 기록: "+MisconceptionKorean(misconception); toastClock=0; toastG.alpha=1; MgfFx.Punch(toastRt,.08f,.26f);
        }

        static string MisconceptionKorean(string id)
        {
            switch(id)
            {
                case "additive-sides":return "대응변에 같은 수를 더함";
                case "non-included-angle":return "SAS의 끼인각이 아님";
                case "two-side-ratio":return "두 대응변의 비만 확인함";
                case "unequal-included-angle":return "서로 다른 끼인각을 같다고 봄";
                case "one-angle-only":return "대응각 하나만 같다고 봄";
                case "proportional-angles":return "대응각을 같은 크기 대신 비로 봄";
                case "vertex-order":return "꼭짓점 대응 순서를 뒤집음";
                case "linear-area-ratio":return "닮음비를 넓이의 비로 씀";
                case "linear-volume-ratio":return "닮음비를 부피의 비로 씀";
                case "one-axis-area-ratio":return "한 대응변의 비만 2:3임";
                case "one-axis-volume-ratio":return "한 선형 치수의 비만 2:3임";
                default:return "대응하는 비가 일정하지 않음";
            }
        }

        void ShowEnd(string reason)
        {
            // 마지막 정답의 수식 패널이 결과 화면 위로 다시 페이드인되지 않게 완전히 닫는다.
            feedbackVisualClock=99f; feedbackG.alpha=0; feedbackG.blocksRaycasts=false;
            endDisplayedScore=0; lastShownEndScore=-1;
            int accuracy=st.firstAttemptTotal==0?0:Mathf.RoundToInt(st.firstAttemptCorrect*100f/st.firstAttemptTotal);
            if(reason=="clear") endTitleText.text="백색 채석장 봉인 완료";
            else if(reason=="mastery") endTitleText.text="거신은 섰지만\n봉인은 불완전";
            else if(reason=="lives") endTitleText.text="봉인못이 모두 부러졌다";
            else endTitleText.text="파도 균열이 성문에 닿았다";
            endStatsText.text="완성 계약  "+st.solved+" / 6\n첫 시도 정답  "+st.firstAttemptCorrect+" / "+st.firstAttemptTotal+"  ("+accuracy+"%)\n마지막 두 계약  "+st.lastTwoFirstTry+" / 2\n성공 조건  전체 5/6 · 마지막 2/2";
            restartText.text="봉인 다시 시작";
            if(reason=="clear")
            {
                MgfFx.Glow(new Vector3(5.2f,2.4f,5.0f),SaltBright,32,.38f);
                fissureLine.enabled=false;
            }
        }

        void UpdateWorld(float dt)
        {
            worldClock+=dt; titleClock+=dt;
            MgfLook.FitWidth(cam,32f,.72f);
            if(cam.orthographic) cam.orthographicSize=Mathf.Max(cam.orthographicSize,wideLayout?7.3f:8.2f);
            for(int i=0;i<saltFlags.Length;i++) saltFlags[i].localRotation=Quaternion.Euler(0,Mathf.Sin(worldClock*1.1f+i*.7f)*8f,Mathf.Sin(worldClock*1.7f+i)*3f);
            for(int i=0;i<quarryCranes.Length;i++) quarryCranes[i].localRotation=Quaternion.Euler(0,Mathf.Sin(worldClock*.32f+i)*12f,0);
            brineRoot.localScale=new Vector3(1f+Mathf.Sin(worldClock*.8f)*.006f,1,1f+Mathf.Cos(worldClock*.7f)*.01f);

            if(runPhase==RunPhase.Title)
            {
                for(int i=0;i<golems.Length;i++)
                {
                    float bob=Mathf.Sin(worldClock*2.0f+i*.8f)*.06f;
                    if(i==2)
                    {
                        golems[i].localPosition=new Vector3(-.35f,.18f,-1.15f)+Vector3.up*bob;
                        golems[i].localScale=Vector3.one*(1.58f+Mathf.Sin(worldClock*1.4f)*.025f);
                    }
                    else
                    {
                        golems[i].localPosition=golemHomes[i]+Vector3.up*bob;
                        golems[i].localScale=Vector3.one;
                    }
                    golemArms[i].localRotation=Quaternion.Euler(Mathf.Sin(worldClock*3f+i)*18f,0,(i%2==0?1:-1)*12f);
                }
            }
            else if(mergeClock<0f && wrongClock<0f && !dragging)
            {
                for(int i=0;i<golems.Length;i++)
                {
                    golems[i].localPosition=Vector3.Lerp(golems[i].localPosition,golemHomes[i]+Vector3.up*(Mathf.Sin(worldClock*1.7f+i)*.035f),.16f);
                    golemHeads[i].localRotation=Quaternion.Euler(0,Mathf.Sin(worldClock*.9f+i)*5f,0);
                }
            }

            if(visualHitStop>0f) { visualHitStop-=dt; return; }
            if(mergeClock>=0f)
            {
                mergeClock+=dt; float k=Mathf.Clamp01(mergeClock/1.10f);
                float anticipate=Mathf.Clamp01(mergeClock/.15f);
                if(mergeClock<.15f)
                {
                    Vector3 away=(golemHomes[mergeA]-golemHomes[mergeB]).normalized*.22f*Mathf.Sin(anticipate*Mathf.PI);
                    golems[mergeA].localPosition=golemHomes[mergeA]+away; golems[mergeB].localPosition=golemHomes[mergeB]-away;
                }
                else
                {
                    float move=Mathf.Clamp01((mergeClock-.15f)/.48f); float over=move<.82f?move/ .82f:1f+(1f-move)*.12f;
                    golems[mergeA].localPosition=Vector3.Lerp(golemHomes[mergeA],feedbackMidpoint,Mathf.Clamp01(over));
                    golems[mergeB].localPosition=Vector3.Lerp(golemHomes[mergeB],feedbackMidpoint+new Vector3(.08f,.02f,.08f),Mathf.Clamp01(over));
                    float stretch=1f+Mathf.Sin(Mathf.Clamp01((mergeClock-.15f)/.55f)*Mathf.PI)*.30f;
                    golems[mergeA].localScale=new Vector3(1f/stretch,stretch,1f/stretch);
                    golems[mergeB].localScale=new Vector3(1f/stretch,stretch,1f/stretch);
                }
                if(sealClusters.Count>0)
                {
                    Transform cluster=sealClusters[sealClusters.Count-1]; float rise=Mathf.Clamp01((mergeClock-.52f)/.48f); float overshoot=rise+Mathf.Sin(rise*Mathf.PI)*.16f;
                    cluster.localScale=Vector3.one*Mathf.Max(.02f,overshoot);
                }
                cam.orthographicSize=camBaseSize-Mathf.Sin(k*Mathf.PI)*.55f;
            }
            if(wrongClock>=0f)
            {
                wrongClock+=dt; float k=Mathf.Clamp01(wrongClock/.55f);
                if(wrongA>=0) golems[wrongA].localRotation=Quaternion.Euler(0,0,Mathf.Sin(k*24f)*(1f-k)*8f);
                if(wrongB>=0) golems[wrongB].localRotation=Quaternion.Euler(0,0,-Mathf.Sin(k*21f)*(1f-k)*8f);
                if(droppingNail>=0 && droppingNail<nails.Length)
                {
                    nails[droppingNail].localPosition+=Vector3.down*dt*5.2f;
                    nails[droppingNail].Rotate(Vector3.forward,dt*340f);
                }
            }
            if(titleTransitionClock>=0f) titleTransitionClock+=dt;
        }

        void UpdateUi(float dt)
        {
            LayoutUi(false);
            displayedScore=Mathf.MoveTowards(displayedScore,st.score,dt*Mathf.Max(180,Mathf.Abs(st.score-displayedScore)*4));
            int shown=Mathf.RoundToInt(displayedScore); if(shown!=lastShownScore){lastShownScore=shown;scoreText.text=shown.ToString();}
            int sec=Mathf.Max(0,Mathf.CeilToInt(runLeft)); if(sec!=lastSecond){lastSecond=sec;timerText.text=sec+"초";}
            if(runPhase==RunPhase.End)
            {
                endDisplayedScore=Mathf.MoveTowards(endDisplayedScore,st.score,dt*Mathf.Max(220,st.score*1.2f)); int e=Mathf.RoundToInt(endDisplayedScore);
                if(e!=lastShownEndScore){lastShownEndScore=e;endScoreText.text=e.ToString();}
            }
            if(runPhase==RunPhase.Title)
            {
                float pulse=1f+Mathf.Max(0,Mathf.Sin(titleClock*2.2f))*.035f; titleSealRt.localScale=Vector3.one*pulse;
                titleLogoRt.localRotation=Quaternion.Euler(0,0,Mathf.Sin(titleClock*.75f)*.7f);
            }
            if(rippleClock<.48f)
            {
                rippleClock+=dt; float k=rippleClock/.48f; rippleRt.localScale=Vector3.one*(.55f+k*1.9f); rippleImg.color=new Color(Copper.r,Copper.g,Copper.b,1f-k); if(k>=1)rippleRt.gameObject.SetActive(false);
            }
            if(toastClock<2.25f)
            {
                toastClock+=dt; toastG.alpha=Mathf.Clamp01(Mathf.Min(toastClock*7f,(2.25f-toastClock)*5f));
                if(cardRefuseClock>0){cardRefuseClock-=dt;toastRt.anchoredPosition=new Vector2(Mathf.Sin(cardRefuseClock*75f)*7f,0);} else toastRt.anchoredPosition=Vector2.zero;
            }
            if(feedbackVisualClock<2.8f)
            {
                feedbackVisualClock+=dt; feedbackG.alpha=Mathf.Clamp01(Mathf.Min(feedbackVisualClock*8f,(2.8f-feedbackVisualClock)*6f));
                feedbackRt.localScale=Vector3.one*(1f+Mathf.Sin(Mathf.Clamp01(feedbackVisualClock/.38f)*Mathf.PI)*.09f);
            }
            if(guideHandRt.gameObject.activeSelf && current!=null)
            {
                Vector2 start=Center(shieldRt[current.correctA]), end=Center(shieldRt[current.correctB]); float u=(Mathf.Sin(worldClock*3.1f)+1f)*.5f;
                guideHandRt.position=Vector2.Lerp(start,end,u); Vector2 d=end-start; guideLineRt.position=(start+end)*.5f; guideLineRt.sizeDelta=new Vector2(12+guideBoost*9f,d.magnitude);
                guideLineRt.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-90f); guideHandRt.localScale=Vector3.one*(1f+.13f*Mathf.Sin(worldClock*6f)+guideBoost*.35f); guideBoost=Mathf.MoveTowards(guideBoost,0,dt*.55f);
            }
            if(dragging && dragShield>=0) shieldRt[dragShield].localScale=new Vector3(1.08f,1.18f,1f);
        }

        void UpdateHud(bool punch)
        {
            if(progressText==null)return;
            progressText.text=st.solved+" / 6";
            livesText.text=st.lives<=0?"◇ ◇ ◇":st.lives==1?"◆ ◇ ◇":st.lives==2?"◆ ◆ ◇":"◆ ◆ ◆";
            if(punch) MgfFx.Punch(progressText.transform,.08f,.20f);
        }

        bool Hit(RectTransform rt,Vector2 p)=>rt&&rt.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(rt,p);
        bool HitTitleSeal(Vector2 p)
        {
            if(Hit(titleSealRt,p)) return true;
            // 타이틀 중앙의 거신도 세계 안의 시작 오브젝트다. 무작정 첫 탭도 연습으로 이어진다.
            return p.x>=Screen.width*.25f&&p.x<=Screen.width*.75f&&p.y>=Screen.height*.27f&&p.y<=Screen.height*.67f;
        }
        bool HitRestartSeal(Vector2 p)=>Hit(restartRt,p);
        int HitShield(Vector2 p){for(int i=0;i<6;i++)if(Hit(shieldRt[i],p))return i;return-1;}
        int HitShieldExcept(Vector2 p,int except){for(int i=0;i<6;i++)if(i!=except&&Hit(shieldRt[i],p))return i;return-1;}
        int HitPracticeShield(Vector2 p)
        {
            if(current==null) return -1;
            int a=current.correctA,b=current.correctB;
            if(Hit(shieldRt[a],p)) return a;
            if(Hit(shieldRt[b],p)) return b;
            // 연습의 두 활성 방패는 안내판 아래 플레이 영역을 양분하는 넓은 손잡이를 가진다.
            // 첫 탐색 탭이 카드 테두리에서 조금 벗어나도 가까운 활성 방패를 집는다.
            if(p.y<Screen.height*.05f||p.y>Screen.height*.92f) return -1;
            float split=(Center(shieldRt[a]).x+Center(shieldRt[b]).x)*.5f;
            return p.x<split?a:b;
        }
        int ResolveDropTarget(Vector2 p,int from)
        {
            int exact=HitShieldExcept(p,from);
            if(runPhase!=RunPhase.Practice||current==null) return exact;
            int a=current.correctA,b=current.correctB;
            if(from!=a&&from!=b) return -1;
            int target=from==a?b:a;
            Vector2 tc=Center(shieldRt[target]);
            float magneticRadius=Mathf.Max(150f,Screen.width*.36f);
            if(Vector2.Distance(p,tc)<=magneticRadius) return target;
            if(p.y<Screen.height*.05f||p.y>Screen.height*.92f) return -1;
            float split=(Center(shieldRt[a]).x+Center(shieldRt[b]).x)*.5f;
            if((target==b&&p.x>split)||(target==a&&p.x<split)) return target;
            return -1;
        }
        static Vector2 Center(RectTransform rt)=>RectTransformUtility.WorldToScreenPoint(null,rt.position);

        void SpawnTapRipple(Vector2 p)
        {
            rippleRt.gameObject.SetActive(true); rippleRt.position=p; rippleRt.localScale=Vector3.one*.55f; rippleClock=0; rippleImg.color=Copper;
        }

        void RefuseInput(string message)
        {
            toastText.text=message; toastClock=0; toastG.alpha=1; cardRefuseClock=.36f; toastRt.anchoredPosition=Vector2.zero;
            MgfSfx.Play("wrong",.17f); MgfFx.Punch(toastRt,.06f,.22f);
        }

        void PointToTitleSeal(){MgfFx.Punch(titleSealRt,.14f,.28f);}
        void PointToRestartSeal(){MgfFx.Punch(restartRt,.14f,.28f);}
        void PointToActiveShield(){int i=current==null?0:PositiveUi(st.contractIndex%6);MgfFx.Punch(shieldRt[i],.15f,.30f);}
        static int PositiveUi(int x){return x<0?0:x;}
        void StartGuide(){guideHandRt.gameObject.SetActive(true);guideLineRt.gameObject.SetActive(true);guideBoost=1f;}
        void BoostGuide(){StartGuide();guideBoost=1f;MgfSfx.Play("tap",.12f);}
        void HideGuide(){guideHandRt.gameObject.SetActive(false);guideLineRt.gameObject.SetActive(false);}
    }
}
