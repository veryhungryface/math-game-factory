// 점프 수리 — 자주색 고무 경기장, 수리 망, 스프링 토끼, 반응형 UI.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.JeompeuSuri
{
    public partial class JeompeuSuriGame
    {
        static readonly Color Plum = MgfLook.Hex("#643B70");
        static readonly Color PlumDark = MgfLook.Hex("#35243D");
        static readonly Color Apricot = MgfLook.Hex("#F3AD72");
        static readonly Color Cream = MgfLook.Hex("#F8F2E7");
        static readonly Color Lime = MgfLook.Hex("#B8E074");
        static readonly Color Coral = MgfLook.Hex("#E55873");
        static readonly Color Ink = MgfLook.Hex("#38233D");
        const float BoardY = .74f;
        const float GridStep = .82f;

        Camera cam;
        Transform arenaRoot, boardRoot, rabbitRoot, rabbitBody, rabbitHead, rabbitEarL, rabbitEarR, rabbitFeet;
        Transform guideGhost, guideBead, landingRingA, landingRingB;
        readonly Transform[] handles = new Transform[4];
        readonly Transform[] straps = new Transform[3];
        readonly Transform[] ribbons = new Transform[7];
        readonly Transform[] bunnyTrail = new Transform[5];
        readonly LineRenderer[] edges = new LineRenderer[4];
        LineRenderer diagAC, diagBD, midCrossA, midCrossB, guideLine;
        Mesh boardMesh;
        MeshFilter boardFilter;
        readonly Vector3[] visualPoints = new Vector3[4];
        Vector3 boardHome, rabbitHome;
        Material plumMat, plumDarkMat, apricotMat, creamMat, limeMat, coralMat, inkMat, meshMat, lineMat, diagMat, ghostMat;
        readonly Vector3 rabbitBaseScale = Vector3.one * 1.18f;
        float worldClock, revealWorldClock, wrongBoardShake, titlePress, guideBoost, handlePulse;
        bool worldReveal, worldCorrect;

        CanvasGroup titleG, hudG, revealG, endG, toastG;
        RectTransform rootRt, titleRt, hudRt, revealRt, endRt, toastRt, restartRt;
        RectTransform guideRingRt, guideTargetRt, guidePathRt, guideFingerRt, rippleRt;
        TextMeshProUGUI logoShadow, logoMain, titleTag, titleMeta, titleCta, titleHint;
        TextMeshProUGUI scoreTxt, progressTxt, livesTxt, timerTxt, goalTxt, promptTxt, measureTxt;
        readonly TextMeshProUGUI[] vertexLabelTxt = new TextMeshProUGUI[4];
        TextMeshProUGUI revealTitleTxt, revealReasonTxt, revealProofTxt;
        TextMeshProUGUI endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt, toastTxt;
        Image guideRingImg, guideTargetImg, guidePathImg, guideFingerImg, rippleImg, revealPanelImg;
        Sprite roundSprite, ringSprite;
        float displayedScore, endDisplayedScore, toastClock, rippleClock, guideClock, revealUiClock;
        int shownSecond = -1, shownScore = -1, layoutW = -1, layoutH = -1;

        Vector3 BoardWorld(Vector2 p)
        {
            return new Vector3((p.x - 3f) * GridStep, BoardY, (p.y - 3f) * GridStep + 1.15f);
        }

        Vector3 BoardWorld(Vector2Int p) => BoardWorld(new Vector2(p.x, p.y));

        LineRenderer Line(string name, Material mat, float width, Transform parent, int count = 2)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat; lr.positionCount = count; lr.startWidth = lr.endWidth = width;
            lr.useWorldSpace = true; lr.numCapVertices = 4; lr.numCornerVertices = 3;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#80518A"), Plum, MgfLook.Hex("#3D2947"), .92f);
            MgfLook.Sun(new Vector3(48, -28, 16), MgfLook.Hex("#FFE3BD"), 1.25f, .62f);
            cam = MgfLook.Camera(new Vector3(8.8f, 11.8f, -11.8f), new Vector3(0, .1f, 1.35f), 32f);
            cam.orthographic = true; cam.orthographicSize = 6.4f;

            plumMat = MgfLook.Lit(Plum, .28f, .01f);
            plumDarkMat = MgfLook.Lit(PlumDark, .34f, .02f);
            apricotMat = MgfLook.Lit(Apricot, .42f, .02f, Apricot * .05f);
            creamMat = MgfLook.Lit(Cream, .28f, 0);
            limeMat = MgfLook.Lit(Lime, .38f, 0, Lime * .15f);
            coralMat = MgfLook.Lit(Coral, .35f, 0, Coral * .08f);
            inkMat = MgfLook.Lit(Ink, .20f, .01f);
            meshMat = MgfLook.Lit(MgfLook.Hex("#EFE5D5"), .55f, 0);
            lineMat = MgfLook.Unlit(Apricot);
            diagMat = MgfLook.Unlit(MgfLook.Hex("#5B4771"));
            ghostMat = MgfLook.Alpha(new Color(Lime.r, Lime.g, Lime.b, .52f));

            arenaRoot = new GameObject("PlumRubberArena").transform;
            MgfLook.Block("RubberFloor", new Vector3(0, -.45f, 2f), new Vector3(18f, .8f, 17f), .85f, plumMat, arenaRoot);
            MgfLook.Block("BackWall", new Vector3(0, 2.6f, 9.3f), new Vector3(18f, 6f, 1.15f), .65f, plumDarkMat, arenaRoot);
            MgfLook.Block("LeftTube", new Vector3(-8.1f, 1.2f, 2f), new Vector3(1.15f, 3.1f, 15f), .55f, apricotMat, arenaRoot);
            MgfLook.Block("RightTube", new Vector3(8.1f, 1.2f, 2f), new Vector3(1.15f, 3.1f, 15f), .55f, apricotMat, arenaRoot);

            // 배경 점프대와 스포츠 소품 — 회화 배경 없이 프리미티브만 쓴다.
            BuildBackgroundTrampoline(-5.5f, 6.4f, 2.4f, .18f);
            BuildBackgroundTrampoline(5.7f, 6.9f, 2.0f, -.20f);
            BuildBackgroundTrampoline(-5.9f, -2.7f, 1.8f, -.12f);
            for (int i = 0; i < ribbons.Length; i++)
            {
                float x = -6.4f + i * 2.1f;
                ribbons[i] = MgfLook.Block("CeilingRibbon" + i, new Vector3(x, 5.0f + (i % 2) * .25f, 8.35f),
                    new Vector3(.18f, 1.1f + (i % 3) * .2f, .12f), .07f, i % 2 == 0 ? apricotMat : limeMat, arenaRoot).transform;
            }
            MgfLook.Block("WaterBottle", new Vector3(6.5f, .3f, -.6f), new Vector3(.55f, 1.5f, .55f), .24f, creamMat, arenaRoot);
            MgfLook.Block("Towel", new Vector3(5.7f, .0f, -.8f), new Vector3(1.2f, .18f, .72f), .09f, apricotMat, arenaRoot);
            for (int i = 0; i < 2; i++)
            {
                MgfLook.Block("Shoe" + i, new Vector3(-6.5f + i * .72f, .05f, -.7f + i * .2f), new Vector3(.82f, .38f, 1.2f), .19f, coralMat, arenaRoot);
            }

            BuildBoard();
            BuildRabbit();
        }

        void BuildBackgroundTrampoline(float x, float z, float size, float rot)
        {
            var root = new GameObject("BackgroundTrampoline").transform;
            root.SetParent(arenaRoot, false); root.localPosition = new Vector3(x, .0f, z); root.localRotation = Quaternion.Euler(0, rot * 40f, 0);
            MgfLook.Block("Mesh", new Vector3(0, .4f, 0), new Vector3(size, .13f, size * .72f), .25f, meshMat, root);
            for (int i = 0; i < 4; i++)
            {
                float sx = (i & 1) == 0 ? -1 : 1, sz = i < 2 ? -1 : 1;
                MgfLook.Prim(PrimitiveType.Cylinder, "Pad", new Vector3(sx * size * .48f, .42f, sz * size * .34f),
                    new Vector3(.22f, .28f, .22f), apricotMat, root, false);
            }
        }

        void BuildBoard()
        {
            boardRoot = new GameObject("RepairTrampoline").transform;
            boardRoot.SetParent(arenaRoot, false); boardHome = boardRoot.position;

            // 7×7 정방 모눈. 선은 채점 색과 무관하게 항상 같은 크림색이다.
            Material gridMat = MgfLook.Alpha(new Color(Plum.r, Plum.g, Plum.b, .32f));
            for (int i = 0; i < 7; i++)
            {
                float q = (i - 3) * GridStep;
                var a = Line("GridX" + i, gridMat, .035f, boardRoot);
                a.SetPosition(0, new Vector3(q, BoardY + .16f, -1.31f)); a.SetPosition(1, new Vector3(q, BoardY + .16f, 3.61f));
                var b = Line("GridY" + i, gridMat, .035f, boardRoot);
                b.SetPosition(0, new Vector3(-2.46f, BoardY + .16f, q + 1.15f)); b.SetPosition(1, new Vector3(2.46f, BoardY + .16f, q + 1.15f));
            }
            MgfLook.Block("ApricotFrame", new Vector3(0, BoardY - .08f, 1.15f), new Vector3(5.75f, .26f, 5.75f), .40f, apricotMat, boardRoot);
            MgfLook.Block("CreamMeshBed", new Vector3(0, BoardY, 1.15f), new Vector3(5.2f, .13f, 5.2f), .32f, meshMat, boardRoot);

            var meshGo = new GameObject("ElasticQuadrilateral"); meshGo.transform.SetParent(boardRoot, false);
            boardFilter = meshGo.AddComponent<MeshFilter>();
            var mr = meshGo.AddComponent<MeshRenderer>(); mr.sharedMaterial = MgfLook.Lit(new Color(.96f, .84f, .70f, 1f), .46f, 0);
            boardMesh = new Mesh { name = "RepairMesh" };
            boardMesh.vertices = new Vector3[4]; boardMesh.triangles = new[]{0,1,2,0,2,3};
            boardMesh.normals = new[]{Vector3.up,Vector3.up,Vector3.up,Vector3.up};
            boardMesh.uv = new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            boardFilter.sharedMesh = boardMesh;

            for (int i = 0; i < 4; i++)
            {
                edges[i] = Line("StitchedEdge" + i, lineMat, .13f, boardRoot);
                handles[i] = MgfLook.Prim(PrimitiveType.Sphere, "Vertex" + RepairProblem.VertexName(i), Vector3.zero,
                    new Vector3(.52f,.32f,.52f), apricotMat, boardRoot, true).transform;
            }
            diagAC = Line("DiagonalAC", diagMat, .065f, boardRoot);
            diagBD = Line("DiagonalBD", diagMat, .065f, boardRoot);
            midCrossA = Line("MidpointAC", lineMat, .095f, boardRoot, 2);
            midCrossB = Line("MidpointBD", diagMat, .095f, boardRoot, 2);

            guideGhost = MgfLook.Prim(PrimitiveType.Sphere, "PracticeDestination", Vector3.zero,
                new Vector3(.58f,.12f,.58f), ghostMat, boardRoot, false).transform;
            guideLine = Line("PracticeGuidePath", MgfLook.Alpha(new Color(Lime.r,Lime.g,Lime.b,.75f)), .075f, boardRoot);
            guideBead = MgfLook.Prim(PrimitiveType.Sphere, "MovingGuideBead", Vector3.zero,
                new Vector3(.24f,.16f,.24f), limeMat, boardRoot, false).transform;

            for (int i = 0; i < 3; i++)
                straps[i] = MgfLook.Block("SafetyStrap" + i, new Vector3(-3.7f + i * .46f, .65f, 3.95f),
                    new Vector3(.24f,.25f,2.0f), .10f, coralMat, boardRoot).transform;

            landingRingA = MakeRing("LandingWaveA", limeMat, boardRoot);
            landingRingB = MakeRing("LandingWaveB", apricotMat, boardRoot);
            landingRingA.gameObject.SetActive(false); landingRingB.gameObject.SetActive(false);
        }

        Transform MakeRing(string name, Material mat, Transform parent)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2f / 20f;
                var o = MgfLook.Prim(PrimitiveType.Sphere, "WaveDot", new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)),
                    new Vector3(.10f,.04f,.10f), mat, root, false);
                o.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            return root;
        }

        void BuildRabbit()
        {
            rabbitRoot = new GameObject("SpringRabbit").transform; rabbitRoot.SetParent(arenaRoot, false);
            rabbitHome = new Vector3(3.0f, .72f, 0.0f); rabbitRoot.position = rabbitHome; rabbitRoot.localScale=rabbitBaseScale;
            rabbitBody = MgfLook.Prim(PrimitiveType.Cylinder, "RubberBody", new Vector3(0,.86f,0), new Vector3(.68f,.72f,.68f), creamMat, rabbitRoot, false).transform;
            rabbitHead = MgfLook.Prim(PrimitiveType.Sphere, "RabbitHead", new Vector3(0,1.72f,0), new Vector3(.86f,.72f,.72f), creamMat, rabbitRoot, false).transform;
            rabbitEarL = MgfLook.Prim(PrimitiveType.Capsule, "RubberEarL", new Vector3(-.28f,2.45f,.02f), new Vector3(.22f,.72f,.22f), apricotMat, rabbitRoot, false).transform;
            rabbitEarR = MgfLook.Prim(PrimitiveType.Capsule, "RubberEarR", new Vector3(.28f,2.45f,.02f), new Vector3(.22f,.72f,.22f), apricotMat, rabbitRoot, false).transform;
            rabbitEarL.localRotation = Quaternion.Euler(0,0,-12); rabbitEarR.localRotation = Quaternion.Euler(0,0,12);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.22f,1.83f,-.58f), new Vector3(.10f,.13f,.08f), inkMat, rabbitRoot, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.22f,1.83f,-.58f), new Vector3(.10f,.13f,.08f), inkMat, rabbitRoot, false);
            rabbitFeet = new GameObject("CoilFeet").transform; rabbitFeet.SetParent(rabbitRoot, false);
            for (int foot = 0; foot < 2; foot++)
            {
                float x = foot == 0 ? -.28f : .28f;
                for (int c = 0; c < 4; c++) MgfLook.Prim(PrimitiveType.Cylinder, "Coil", new Vector3(x,.15f+c*.14f,0), new Vector3(.26f,.055f,.26f), plumDarkMat, rabbitFeet, false);
                MgfLook.Block("ApricotShoe", new Vector3(x,.02f,-.16f), new Vector3(.55f,.22f,.78f), .16f, apricotMat, rabbitFeet);
            }
            for (int i = 0; i < bunnyTrail.Length; i++)
            {
                bunnyTrail[i] = MgfLook.Prim(PrimitiveType.Sphere, "RubberAfterimage" + i, Vector3.zero,
                    new Vector3(.18f,.08f,.18f), i%2==0?apricotMat:limeMat, arenaRoot, false).transform;
                bunnyTrail[i].gameObject.SetActive(false);
            }
        }

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f,.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        CanvasGroup Group(string name, Transform parent)
        {
            var rt = R(name,parent,new Vector2(.5f,.5f),Vector2.zero,new Vector2(390,844));
            return rt.gameObject.AddComponent<CanvasGroup>();
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite=null)
        {
            var rt=R(name,parent,anchor,pos,size); var im=rt.gameObject.AddComponent<Image>();
            im.color=color; im.sprite=sprite; im.raycastTarget=false;
            if(sprite && sprite.border!=Vector4.zero) im.type=Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent,string text,Vector2 anchor,Vector2 pos,float size,Color color,float width=360,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {
            var rt=R("Text",parent,anchor,pos,new Vector2(width,size*2.15f)); var t=rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=MgfText.Font; t.fontSize=size; t.color=color; t.alignment=align; t.raycastTarget=false;
            t.textWrappingMode=TextWrappingModes.Normal; t.text=text; return t;
        }

        Sprite MakeSprite(int size,float radius,bool ring)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name=ring?"JSRing":"JSRound",wrapMode=TextureWrapMode.Clamp};
            var px=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float fx=x+.5f,fy=y+.5f,a;
                if(ring){float dx=fx-size*.5f,dy=fy-size*.5f,d=Mathf.Sqrt(dx*dx+dy*dy);a=Mathf.Clamp01(1-Mathf.Abs(d-size*.39f)/(size*.07f));}
                else{float qx=Mathf.Max(Mathf.Abs(fx-size*.5f)-(size*.5f-radius),0),qy=Mathf.Max(Mathf.Abs(fy-size*.5f)-(size*.5f-radius),0);a=Mathf.Clamp01(radius-Mathf.Sqrt(qx*qx+qy*qy)+.8f);}
                px[y*size+x]=new Color32(255,255,255,(byte)(a*255));
            }
            tex.SetPixels32(px);tex.Apply(false,true);float b=ring?0:radius+2;
            return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(b,b,b,b));
        }

        void BuildUi()
        {
            roundSprite=MakeSprite(96,25,false); ringSprite=MakeSprite(96,25,true);
            var canvas=MgfText.Canvas; rootRt=(RectTransform)canvas.transform;
            titleG=Group("TitleUi",canvas.transform); titleRt=(RectTransform)titleG.transform;
            hudG=Group("HudUi",canvas.transform); hudRt=(RectTransform)hudG.transform;
            revealG=Group("RevealUi",canvas.transform); revealRt=(RectTransform)revealG.transform;
            endG=Group("EndUi",canvas.transform); endRt=(RectTransform)endG.transform;
            toastG=Group("ToastUi",canvas.transform); toastRt=(RectTransform)toastG.transform;

            // 매달린 망 간판 같은 비대칭 로고: 텍스트를 단순 중앙 카드로 가두지 않는다.
            Img(titleG.transform,"HangingNet",new Vector2(.5f,1),new Vector2(0,-145),new Vector2(344,160),new Color(Apricot.r,Apricot.g,Apricot.b,.90f),roundSprite);
            Img(titleG.transform,"NetKnotL",new Vector2(.5f,1),new Vector2(-151,-68),new Vector2(10,145),Cream);
            Img(titleG.transform,"NetKnotR",new Vector2(.5f,1),new Vector2(151,-68),new Vector2(10,145),Cream);
            logoShadow=Txt(titleG.transform,"점프  수리",new Vector2(.5f,1),new Vector2(4,-141),58,PlumDark,350);
            logoMain=Txt(titleG.transform,"점프  수리",new Vector2(.5f,1),new Vector2(0,-135),58,Cream,350);
            logoMain.fontStyle=FontStyles.Bold; logoMain.characterSpacing=-2;
            titleTag=Txt(titleG.transform,"꼭짓점을 끌어 점프대를 고쳐라",new Vector2(.5f,1),new Vector2(0,-215),19,Cream,350);
            titleMeta=Txt(titleG.transform,"중학교 2학년 · 사각형의 성질",new Vector2(.5f,0),new Vector2(0,134),14,Cream,350);
            Img(titleG.transform,"StartMesh",new Vector2(.5f,0),new Vector2(0,76),new Vector2(270,82),new Color(Cream.r,Cream.g,Cream.b,.96f),roundSprite);
            titleCta=Txt(titleG.transform,"망을 눌러 출발",new Vector2(.5f,0),new Vector2(0,78),24,Ink,270); titleCta.fontStyle=FontStyles.Bold;
            titleHint=Txt(titleG.transform,"대각선의 중점을 맞추면 토끼가 뛴다",new Vector2(.5f,0),new Vector2(0,28),13,Cream,350);

            Img(hudG.transform,"TopRubber",new Vector2(.5f,1),new Vector2(0,-55),new Vector2(370,92),new Color(Cream.r,Cream.g,Cream.b,.96f),roundSprite);
            scoreTxt=Txt(hudG.transform,"0",new Vector2(0,1),new Vector2(64,-50),21,Ink,110);
            progressTxt=Txt(hudG.transform,"1 / 10",new Vector2(.5f,1),new Vector2(0,-50),18,Ink,130);
            livesTxt=Txt(hudG.transform,"끈 3",new Vector2(1,1),new Vector2(-70,-50),18,Coral,100);
            timerTxt=Txt(hudG.transform,"90초",new Vector2(.5f,1),new Vector2(0,-87),15,Plum,140);
            goalTxt=Txt(hudG.transform,"꼭짓점을 맞추면 토끼가 건넌다",new Vector2(.5f,1),new Vector2(0,-123),18,Cream,370); goalTxt.fontStyle=FontStyles.Bold;
            Img(hudG.transform,"PromptPad",new Vector2(.5f,1),new Vector2(0,-171),new Vector2(360,62),new Color(Apricot.r,Apricot.g,Apricot.b,.96f),roundSprite);
            promptTxt=Txt(hudG.transform,"",new Vector2(.5f,1),new Vector2(0,-171),17,Ink,335); promptTxt.fontStyle=FontStyles.Bold;
            Img(hudG.transform,"MeasurePad",new Vector2(.5f,0),new Vector2(0,66),new Vector2(362,82),new Color(Cream.r,Cream.g,Cream.b,.94f),roundSprite);
            measureTxt=Txt(hudG.transform,"",new Vector2(.5f,0),new Vector2(0,67),15,Ink,338);
            for(int i=0;i<4;i++)
            {
                vertexLabelTxt[i]=Txt(hudG.transform,RepairProblem.VertexName(i),new Vector2(.5f,.5f),Vector2.zero,20,PlumDark,34);
                vertexLabelTxt[i].fontStyle=FontStyles.Bold;
            }

            guideRingImg=Img(hudG.transform,"GuideRing",new Vector2(.5f,.5f),Vector2.zero,new Vector2(70,70),Lime,ringSprite);
            guideTargetImg=Img(hudG.transform,"GuideTarget",new Vector2(.5f,.5f),Vector2.zero,new Vector2(64,64),new Color(Lime.r,Lime.g,Lime.b,.46f),ringSprite);
            guidePathImg=Img(hudG.transform,"GuidePath",new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,5),new Color(Lime.r,Lime.g,Lime.b,.82f),roundSprite);
            guideFingerImg=Img(hudG.transform,"GuideFinger",new Vector2(.5f,.5f),Vector2.zero,new Vector2(28,28),Cream,roundSprite);
            guideRingRt=guideRingImg.rectTransform; guideTargetRt=guideTargetImg.rectTransform; guidePathRt=guidePathImg.rectTransform; guideFingerRt=guideFingerImg.rectTransform;

            revealPanelImg=Img(revealG.transform,"Evidence",new Vector2(.5f,.5f),new Vector2(0,20),new Vector2(354,228),new Color(Cream.r,Cream.g,Cream.b,.98f),roundSprite);
            revealTitleTxt=Txt(revealG.transform,"",new Vector2(.5f,.5f),new Vector2(0,74),31,Ink,320); revealTitleTxt.fontStyle=FontStyles.Bold;
            revealReasonTxt=Txt(revealG.transform,"",new Vector2(.5f,.5f),new Vector2(0,18),17,Ink,320);
            revealProofTxt=Txt(revealG.transform,"",new Vector2(.5f,.5f),new Vector2(0,-46),16,Plum,320);

            Img(endG.transform,"EndBanner",new Vector2(.5f,.5f),new Vector2(0,20),new Vector2(360,420),new Color(Cream.r,Cream.g,Cream.b,.98f),roundSprite);
            endTitleTxt=Txt(endG.transform,"",new Vector2(.5f,.5f),new Vector2(0,150),34,Ink,330); endTitleTxt.fontStyle=FontStyles.Bold;
            endScoreTxt=Txt(endG.transform,"0점",new Vector2(.5f,.5f),new Vector2(0,80),38,Plum,320); endScoreTxt.fontStyle=FontStyles.Bold;
            endStatsTxt=Txt(endG.transform,"",new Vector2(.5f,.5f),new Vector2(0,-5),17,Ink,320);
            restartRt=Img(endG.transform,"Restart",new Vector2(.5f,.5f),new Vector2(0,-112),new Vector2(280,72),Apricot,roundSprite).rectTransform;
            endTapTxt=Txt(endG.transform,"다시 수리하기",new Vector2(.5f,.5f),new Vector2(0,-111),22,Ink,270); endTapTxt.fontStyle=FontStyles.Bold;

            Img(toastG.transform,"Toast",new Vector2(.5f,0),new Vector2(0,142),new Vector2(356,68),new Color(PlumDark.r,PlumDark.g,PlumDark.b,.94f),roundSprite);
            toastTxt=Txt(toastG.transform,"",new Vector2(.5f,0),new Vector2(0,143),14,Cream,330);
            rippleImg=Img(canvas.transform,"TapRipple",new Vector2(.5f,.5f),Vector2.zero,new Vector2(52,52),Apricot,ringSprite);
            rippleRt=rippleImg.rectTransform; rippleImg.enabled=false;
        }

        void SetGroup(CanvasGroup g,bool on)
        {
            g.alpha=on?1:0;g.blocksRaycasts=false;g.interactable=false;
        }

        void ShowPhaseUi()
        {
            SetGroup(titleG,phase==Phase.Title);
            SetGroup(hudG,phase==Phase.Practice||phase==Phase.Playing||phase==Phase.Reveal);
            SetGroup(revealG,phase==Phase.Reveal);
            SetGroup(endG,phase==Phase.End);
            if(phase!=Phase.Practice) SetGuide(false);
        }

        void ResetTitleWorld()
        {
            worldReveal=false;wrongBoardShake=0;titlePress=0;
            boardRoot.gameObject.SetActive(true);boardRoot.localScale=Vector3.one*1.04f;
            rabbitRoot.position=rabbitHome+new Vector3(0,0,1.0f);rabbitRoot.localRotation=Quaternion.identity;rabbitRoot.localScale=rabbitBaseScale;
            var p=JeompeuRules.Practice();for(int i=0;i<4;i++){vertices[i]=p.target[i];SetVertexVisual(i,vertices[i]);}
            for(int i=0;i<3;i++)straps[i].gameObject.SetActive(true);
            guideGhost.gameObject.SetActive(false);guideLine.gameObject.SetActive(false);guideBead.gameObject.SetActive(false);
        }

        void ResetArenaForRun()
        {
            worldReveal=false;wrongBoardShake=0;boardRoot.localScale=Vector3.one;boardRoot.position=boardHome;
            rabbitRoot.position=rabbitHome;rabbitRoot.localRotation=Quaternion.identity;rabbitRoot.localScale=rabbitBaseScale;
            for(int i=0;i<3;i++)straps[i].gameObject.SetActive(true);
            landingRingA.gameObject.SetActive(false);landingRingB.gameObject.SetActive(false);
            for(int i=0;i<bunnyTrail.Length;i++)bunnyTrail[i].gameObject.SetActive(false);
        }

        void ResetBoardVisual(bool practice)
        {
            worldReveal=false;wrongBoardShake=0;boardRoot.position=boardHome;
            for(int i=0;i<4;i++)SetVertexVisual(i,vertices[i]);
            for(int i=0;i<3;i++)straps[i].gameObject.SetActive(i<st.lives);
            guideGhost.gameObject.SetActive(practice);
            guideLine.gameObject.SetActive(practice);
            guideBead.gameObject.SetActive(practice);
            if(practice)
            {
                guideGhost.position=BoardWorld(current.AnswerPoint)+Vector3.up*.09f;
                guideLine.SetPosition(0,BoardWorld(vertices[3])+Vector3.up*.16f);
                guideLine.SetPosition(1,BoardWorld(current.AnswerPoint)+Vector3.up*.16f);
            }
            SetGuide(practice);
        }

        void RefreshBoardGeometry()
        {
            var mv=new Vector3[4];
            for(int i=0;i<4;i++)mv[i]=boardRoot.InverseTransformPoint(visualPoints[i]+Vector3.down*.055f);
            boardMesh.vertices=mv;boardMesh.RecalculateBounds();boardMesh.RecalculateNormals();
            for(int i=0;i<4;i++){edges[i].SetPosition(0,visualPoints[i]+Vector3.up*.08f);edges[i].SetPosition(1,visualPoints[(i+1)&3]+Vector3.up*.08f);}
            diagAC.SetPosition(0,visualPoints[0]+Vector3.up*.10f);diagAC.SetPosition(1,visualPoints[2]+Vector3.up*.10f);
            diagBD.SetPosition(0,visualPoints[1]+Vector3.up*.10f);diagBD.SetPosition(1,visualPoints[3]+Vector3.up*.10f);
            SetCross(midCrossA,(visualPoints[0]+visualPoints[2])*.5f+Vector3.up*.13f,.18f);
            SetCross(midCrossB,(visualPoints[1]+visualPoints[3])*.5f+Vector3.up*.15f,.13f);
        }

        void SetCross(LineRenderer lr,Vector3 c,float s)
        {
            bool ac=lr==midCrossA;
            lr.SetPosition(0,c+(ac?new Vector3(-s,0,0):new Vector3(0,0,-s)));
            lr.SetPosition(1,c+(ac?new Vector3(s,0,0):new Vector3(0,0,s)));
        }

        void SetVertexVisual(int index,Vector2Int p)
        {
            Vector3 w=BoardWorld(p)+Vector3.up*.17f;handles[index].position=w;visualPoints[index]=w;RefreshBoardGeometry();
        }

        void BeginVertexDrag(int index){handlePulse=.35f;MgfFx.Punch(handles[index],.16f,.20f);}

        void UpdateVertexDrag(int index,Vector2 raw)
        {
            raw.x=Mathf.Clamp(raw.x,0,6);raw.y=Mathf.Clamp(raw.y,0,6);
            Vector3 w=BoardWorld(raw)+Vector3.up*.42f;handles[index].position=w;visualPoints[index]=w;RefreshBoardGeometry();
        }

        void EndVertexDrag(int index){handles[index].localScale=new Vector3(.52f,.32f,.52f);}

        bool PointerGrid(Vector2 screen,out Vector2 grid)
        {
            Ray r=cam.ScreenPointToRay(screen);var plane=new Plane(Vector3.up,new Vector3(0,BoardY,0));
            if(plane.Raycast(r,out float d))
            {
                Vector3 p=r.GetPoint(d);grid=new Vector2(p.x/GridStep+3f,(p.z-1.15f)/GridStep+3f);return true;
            }
            grid=Vector2.zero;return false;
        }

        Vector2Int NearestGrid(Vector2 screen)
        {
            if(!PointerGrid(screen,out Vector2 p))return dragStartGrid;
            return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(p.x),0,6),Mathf.Clamp(Mathf.RoundToInt(p.y),0,6));
        }

        int NearestHandle(Vector2 screen,out float distance)
        {
            int best=-1;distance=float.MaxValue;
            for(int i=0;i<4;i++)
            {
                Vector3 s=cam.WorldToScreenPoint(handles[i].position);float d=Vector2.Distance(screen,new Vector2(s.x,s.y));
                if(d<distance){distance=d;best=i;}
            }
            return best;
        }

        void BindMeasure()
        {
            if(current==null){measureTxt.text="";return;}
            bool p=JeompeuRules.Bisects(vertices);int ac=JeompeuRules.DiagonalSquare(vertices,true),bd=JeompeuRules.DiagonalSquare(vertices,false),dot=JeompeuRules.DiagonalDot(vertices);
            if(current.band<=1)measureTxt.text="AC 중점  ×   BD 중점  +\n두 십자가를 한 점에 겹치시오";
            else if(current.band==2)measureTxt.text="AC² "+ac+"   BD² "+bd+"   AC·BD "+dot+"\n중점 "+(p?"같음":"다름")+" · 같은 축척으로 비교";
            else measureTxt.text="자 없이 모눈으로 읽기\n중점 십자 · 대각선의 방향과 길이를 확인하시오";
        }

        void RefreshProblemUi()
        {
            if(promptTxt==null)return;
            if(current==null){promptTxt.text="";return;}
            promptTxt.text=JeompeuRules.KindName(current.kind)+"이 되도록 꼭짓점을 옮기시오";
            progressTxt.text=(phase==Phase.Practice?"연습":(courseIndex+1)+" / 10");
            livesTxt.text="끈 "+st.lives;
            BindMeasure();
        }

        void ReplayGuide(bool strong)
        {
            if(phase!=Phase.Practice)return;guideClock=0;guideBoost=strong?1f:.35f;SetGuide(true);PulseHandle(3);
            ShowToast(strong?"D를 잡아 흐르는 선 끝의 유령 꼭짓점으로 끌어오시오":"D를 끌어 초록 유령 꼭짓점에 놓으시오",2.8f);
        }

        void SetGuide(bool on)
        {
            if(guideRingImg==null)return;
            guideRingImg.enabled=on;guideTargetImg.enabled=on;guidePathImg.enabled=on;guideFingerImg.enabled=on;
        }

        Vector2 UiPoint(Vector3 world)
        {
            Vector3 s=cam.WorldToScreenPoint(world);
            return new Vector2(s.x/Mathf.Max(1,Screen.width)*390f-195f,s.y/Mathf.Max(1,Screen.height)*844f-422f);
        }

        void PositionGuide()
        {
            PositionVertexLabels();
            if(phase!=Phase.Practice||current==null)return;
            Vector2 a=UiPoint(handles[3].position),b=UiPoint(BoardWorld(current.AnswerPoint)+Vector3.up*.2f);
            float pulse=.5f+.5f*Mathf.Sin(worldClock*5.5f);float boost=1f+guideBoost*.28f;
            guideRingRt.anchoredPosition=a;guideRingRt.sizeDelta=Vector2.one*(64+18*pulse)*boost;
            guideTargetRt.anchoredPosition=b;
            Vector2 d=b-a;guidePathRt.anchoredPosition=(a+b)*.5f;guidePathRt.sizeDelta=new Vector2(d.magnitude,5+3*pulse);guidePathRt.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            float t=0.5f-0.5f*Mathf.Cos((guideClock%1.2f)/1.2f*Mathf.PI*2f);guideFingerRt.anchoredPosition=Vector2.Lerp(a,b,t);
            guideBead.position=Vector3.Lerp(BoardWorld(vertices[3]),BoardWorld(current.AnswerPoint),t)+Vector3.up*.28f;
        }

        void PositionVertexLabels()
        {
            bool on=phase==Phase.Practice||phase==Phase.Playing||phase==Phase.Reveal;
            for(int i=0;i<4;i++)
            {
                vertexLabelTxt[i].enabled=on;
                if(!on)continue;
                Vector2 p=UiPoint(handles[i].position+new Vector3(0,.28f,0));
                vertexLabelTxt[i].rectTransform.anchoredPosition=p+new Vector2(i%2==0?-15:15,10);
            }
        }

        void BeginRevealVisual(bool correct,string reason)
        {
            worldReveal=true;worldCorrect=correct;revealWorldClock=0;revealUiClock=0;
            revealPanelImg.color=correct?new Color(Lime.r,Lime.g,Lime.b,.96f):new Color(1f,.88f,.88f,.98f);
            revealTitleTxt.text=correct?"망이 팽팽해졌다":"안전 끈이 풀렸다";
            revealReasonTxt.text=correct?JeompeuRules.KindName(current.kind)+"의 조건을 만족했다":reason;
            bool p=JeompeuRules.Bisects(vertices);bool eq=JeompeuRules.DiagonalSquare(vertices,true)==JeompeuRules.DiagonalSquare(vertices,false);bool perp=JeompeuRules.DiagonalDot(vertices)==0;
            if(correct)
            {
                string proof="AO=OC, BO=OD";
                if(current.kind==QuadKind.Rectangle)proof+=" · AC=BD";
                else if(current.kind==QuadKind.Rhombus)proof+=" · AC⊥BD";
                else if(current.kind==QuadKind.Square)proof+=" · AC=BD · AC⊥BD";
                revealProofTxt.text=proof+(eq&&perp?"\n정사각형은 직사각형이면서 마름모이다":"");
                MgfSfx.Play("correct",.46f);
                landingRingA.gameObject.SetActive(true);landingRingB.gameObject.SetActive(true);
            }
            else
            {
                revealProofTxt.text="중점 "+(p?"같음":"다름")+" · 길이 "+(eq?"같음":"다름")+" · "+(perp?"수직":"수직 아님");
                wrongBoardShake=.42f;MgfSfx.Play("wrong",.42f);
                if(!revealPractice&&st.lives>=0&&st.lives<3)straps[st.lives].gameObject.SetActive(false);
            }
        }

        void CompleteRevealVisual(bool correct)
        {
            worldReveal=false;landingRingA.gameObject.SetActive(false);landingRingB.gameObject.SetActive(false);
            rabbitRoot.localScale=rabbitBaseScale;rabbitRoot.localRotation=Quaternion.identity;
            for(int i=0;i<bunnyTrail.Length;i++)bunnyTrail[i].gameObject.SetActive(false);
        }

        void AnimateWorld(float dt)
        {
            worldClock+=dt;guideClock+=dt;guideBoost=Mathf.Max(0,guideBoost-dt*.35f);
            for(int i=0;i<ribbons.Length;i++)ribbons[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(worldClock*.8f+i*.7f)*(5+i%3));
            if(phase==Phase.Title)
            {
                float hop=Mathf.Max(0,Mathf.Sin(worldClock*2.15f));rabbitRoot.position=rabbitHome+new Vector3(0,hop*1.15f,1f);
                rabbitRoot.localScale=Vector3.Scale(rabbitBaseScale,new Vector3(1-.08f*hop,1+.15f*hop,1-.08f*hop));
                boardRoot.localScale=Vector3.one*(1.04f-titlePress*.04f)+new Vector3(.01f,Mathf.Sin(worldClock*2f)*.025f,.01f);
            }
            else if(!worldReveal)
            {
                float idle=Mathf.Sin(worldClock*3.2f)*.04f;rabbitRoot.position=rabbitHome+Vector3.up*Mathf.Abs(idle);
                rabbitEarL.localRotation=Quaternion.Euler(0,0,-12+Mathf.Sin(worldClock*2.1f)*5);rabbitEarR.localRotation=Quaternion.Euler(0,0,12+Mathf.Sin(worldClock*2.1f+.5f)*5);
            }
            if(worldReveal)
            {
                revealWorldClock+=dt;
                if(worldCorrect)
                {
                    float t=Mathf.Clamp01(revealWorldClock/1.25f);float y=Mathf.Sin(t*Mathf.PI)*3.4f;
                    float forward=t*4.0f;rabbitRoot.position=rabbitHome+new Vector3(forward,y,0);
                    rabbitRoot.localRotation=Quaternion.Euler(0,0,-t*360f);
                    float squash=t<.12f?Mathf.Lerp(1,.62f,t/.12f):Mathf.Lerp(.62f,1,Mathf.Clamp01((t-.12f)/.22f));
                    rabbitRoot.localScale=Vector3.Scale(rabbitBaseScale,new Vector3(1.1f/squash,squash,1.1f/squash));
                    for(int i=0;i<bunnyTrail.Length;i++)
                    {
                        float tt=t-i*.055f;if(tt<=0){bunnyTrail[i].gameObject.SetActive(false);continue;}
                        bunnyTrail[i].gameObject.SetActive(true);bunnyTrail[i].position=rabbitHome+new Vector3(tt*4f,Mathf.Sin(Mathf.Clamp01(tt)*Mathf.PI)*3.35f,.05f*i);
                        bunnyTrail[i].localScale=Vector3.one*(.6f-i*.07f);
                    }
                    float wave=Mathf.Clamp01((t-.58f)/.42f);
                    landingRingA.position=new Vector3(0,BoardY+.15f,1.15f);landingRingB.position=landingRingA.position;
                    landingRingA.localScale=Vector3.one*(.2f+wave*2.1f);landingRingB.localScale=Vector3.one*(.2f+Mathf.Max(0,wave-.18f)*2.2f);
                }
                else
                {
                    rabbitRoot.position=rabbitHome+new Vector3(0,-Mathf.Sin(Mathf.Clamp01(revealWorldClock/.7f)*Mathf.PI)*.35f,0);
                    rabbitEarL.localRotation=Quaternion.Euler(0,0,-55);rabbitEarR.localRotation=Quaternion.Euler(0,0,18);
                }
            }
            if(wrongBoardShake>0)
            {
                wrongBoardShake=Mathf.Max(0,wrongBoardShake-dt);float amp=wrongBoardShake/.42f*.10f;
                boardRoot.position=boardHome+Vector3.right*Mathf.Sin(worldClock*48f)*amp;
                if(wrongBoardShake<=0)boardRoot.position=boardHome;
            }
            if(handlePulse>0)handlePulse-=dt;
            PositionGuide();
        }

        void UpdateUi(float dt)
        {
            if(layoutW!=Screen.width||layoutH!=Screen.height)ApplyLayout();
            int targetScore=st.score;
            displayedScore=Mathf.MoveTowards(displayedScore,targetScore,Mathf.Max(45f,Mathf.Abs(targetScore-displayedScore)*7f)*dt);
            int ds=Mathf.RoundToInt(displayedScore);
            if(ds!=shownScore){shownScore=ds;scoreTxt.text=ds.ToString("N0")+"점";}
            int sec=Mathf.CeilToInt(timeLeft);
            if(sec!=shownSecond){shownSecond=sec;timerTxt.text=sec+"초";timerTxt.color=sec<=15?Coral:Plum;}
            if(toastClock>0){toastClock-=dt;toastG.alpha=Mathf.Clamp01(toastClock*4f);}else toastG.alpha=0;
            if(rippleClock>0)
            {
                rippleClock-=dt;float t=1-Mathf.Clamp01(rippleClock/.28f);rippleRt.localScale=Vector3.one*(.55f+t*1.25f);rippleImg.color=new Color(Apricot.r,Apricot.g,Apricot.b,1-t);if(rippleClock<=0)rippleImg.enabled=false;
            }
            if(phase==Phase.Reveal)
            {
                revealUiClock+=dt;float k=1+Mathf.Sin(Mathf.Clamp01(revealUiClock/.32f)*Mathf.PI)*.10f;
                revealPanelImg.rectTransform.localScale=Vector3.one*k;
            }
            if(phase==Phase.End)
            {
                endDisplayedScore=Mathf.MoveTowards(endDisplayedScore,st.score,Mathf.Max(80,st.score*2)*dt);
                endScoreTxt.text=Mathf.RoundToInt(endDisplayedScore).ToString("N0")+"점";
            }
        }

        void ApplyLayout()
        {
            layoutW=Screen.width;layoutH=Screen.height;float aspect=(float)Screen.width/Mathf.Max(1,Screen.height);
            cam.orthographicSize=aspect<.72f?7.7f:aspect>1.3f?6.0f:6.65f;
            cam.transform.position=aspect<.72f?new Vector3(8.8f,12.3f,-12.7f):new Vector3(8.8f,11.2f,-11.4f);
            cam.transform.LookAt(new Vector3(0,.15f,1.35f));
            if(aspect>1.25f){titleTag.rectTransform.anchoredPosition=new Vector2(0,-205);measureTxt.rectTransform.parent.GetComponent<RectTransform>();}
        }

        void BeginTitlePress(){titlePress=1;MgfFx.Punch(titleCta.transform,.10f,.22f);}
        void EndTitlePress(){titlePress=0;}
        void PunchRestart(){MgfFx.Punch(restartRt,.10f,.20f);MgfSfx.Play("tap",.18f);}

        void SpawnTapRipple(Vector2 screen)
        {
            rippleImg.enabled=true;rippleClock=.28f;rippleRt.localScale=Vector3.one*.55f;
            rippleRt.anchoredPosition=new Vector2(screen.x/Mathf.Max(1,Screen.width)*390f-195f,screen.y/Mathf.Max(1,Screen.height)*844f-422f);
        }

        void PulseHandle(int index){if(index>=0&&index<4){handlePulse=.35f;MgfFx.Punch(handles[index],.20f,.30f);}}

        void RefuseInput(string message)
        {
            st.lastAction="refuse";ShowToast(message,2.4f);MgfSfx.Play("wrong",.13f);
            if(current!=null)PulseHandle(phase==Phase.Practice?3:keyboardVertex);
            MgfBridge.NotifyChanged();
        }

        void ShowToast(string message,float seconds)
        {
            toastTxt.text=message;toastClock=seconds;toastG.alpha=1;MgfFx.Punch(toastRt,.04f,.20f);
        }

        void ShowEnd(string reason)
        {
            bool clear=reason=="clear";endTitleTxt.text=clear?"코스 통과":"수리 연습 종료";
            endStatsTxt.text="완성 "+st.solved+" / 10\n첫 시도 "+st.firstCorrect+" / "+st.firstAttempts+" · 마지막 단계 "+st.band3FirstCorrect+" / 4\n"+
                (clear?"두 대각선의 성질을 정확히 읽었다":"통과 조건: 첫 시도 8/10 · 마지막 단계 3/4");
            endDisplayedScore=0;endScoreTxt.text="0점";rabbitRoot.position=rabbitHome;
        }

        void FillScreenProbe()
        {
            st.screenW=Screen.width;st.screenH=Screen.height;
            for(int i=0;i<4;i++)
            {
                Vector3 p=cam.WorldToScreenPoint(handles[i].position);st.handlePx[i*2]=Mathf.RoundToInt(p.x);st.handlePx[i*2+1]=Mathf.RoundToInt(Screen.height-p.y);
            }
            Vector2Int[] basis={new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(0,1)};
            for(int i=0;i<3;i++)
            {
                Vector3 p=cam.WorldToScreenPoint(BoardWorld(basis[i])+Vector3.up*.18f);
                st.gridPx[i*2]=Mathf.RoundToInt(p.x);st.gridPx[i*2+1]=Mathf.RoundToInt(Screen.height-p.y);
            }
        }
    }
}
