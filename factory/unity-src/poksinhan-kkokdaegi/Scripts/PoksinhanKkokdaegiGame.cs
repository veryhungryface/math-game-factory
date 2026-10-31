// 폭신한 꼭대기 — 실제 모서리 드래그, 첫 제출 장부, 2.5D 진홍 누비 절벽.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.PoksinhanKkokdaegi
{
    public sealed class PoksinhanKkokdaegiGame : MonoBehaviour, IMgfGame
    {
        // IL2CPP 관리 코드 스트리핑이 런타임 생성 프리미티브의 콜라이더 타입을 제거하지 않게 보존한다.
        static readonly Type PreserveSphereColliderType=typeof(SphereCollider);
        static readonly Type PreserveCapsuleColliderType=typeof(CapsuleCollider);
        [Serializable]
        sealed class State : MgfState
        {
            public int band, round, firstCorrect, firstAttempts, firstMask, selectedSide, inputCount;
            public string feedbackKind="", misconceptionId="", activeProblemId="";
            public bool masteryImpossible, onboarding, repairMode, dragging, layoutLand;
            public int handleXPermille, handleYPermille, trackEndXPermille, trackEndYPermille;
        }

        enum Flow { Title, Opening, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<CushionProblem> models = new List<CushionProblem>(900);
        List<CushionProblem> deck;
        CushionProblem current;
        Flow flow = Flow.Title;
        Camera cam;
        int runSerial;
        bool firstAttempt=true, revealCorrect, revealPractice, failAfterReveal;
        float timeLeft, openingClock, revealClock, idleClock, animClock, scoreShown;
        bool dragging, moved;
        Vector2 pointerDown;
        string endReason="";

        static readonly Color Raspberry=MgfLook.Hex("#C44B68"), RaspberryDeep=MgfLook.Hex("#8C2F49");
        static readonly Color Ivory=MgfLook.Hex("#F5E7CC"), Ink=MgfLook.Hex("#352A32");
        static readonly Color Lime=MgfLook.Hex("#B9CC45"), Warm=MgfLook.Hex("#F5B75B");
        Material clothMat, deepMat, ivoryMat, inkMat, limeMat, glowMat;
        Transform titleRoot, gameRoot, titleStack, activeCushion, activeTop, handle, gecko, geckoBody, tail;
        Transform[] feet=new Transform[4];
        Transform[] stack=new Transform[6];
        Transform[] buttons=new Transform[12];
        Transform seamLoose;
        LineRenderer dragTrail, ghostTrail;
        Vector3 cushionBase=new Vector3(0,.65f,1.2f);

        CanvasGroup titleUi, gameUi, revealUi, endUi, toastUi;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleStart, goalTxt, recordTxt, formulaTxt;
        TextMeshProUGUI progressTxt, livesTxt, timeTxt, sideTxt, guideTxt, revealTitle, revealMath;
        TextMeshProUGUI endTitle, endStats, endTap, toastTxt;
        Image handleRing, ripple, scoreStamp, topBg, recordBg;
        readonly Image[] firstStamps=new Image[6];
        readonly Image[] threadIcons=new Image[3];
        readonly Image[] ticks=new Image[9];
        Sprite roundSprite, ringSprite;
        int lastSecond=-1;
        bool lastLayoutLand;

        void Awake()
        {
            GC.KeepAlive(PreserveSphereColliderType);GC.KeepAlive(PreserveCapsuleColliderType);
            MgfLook.Quality(32f);
            models.AddRange(PoksinRules.BuildModels());
            Prewarm(); BuildWorld(); BuildUi(); ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var b=new StringBuilder(30000);
            b.Append("폭신한꼭대기늘려라네모방석고리를당겨시작중학교3학년산포도변을표준편차에맞추면도마뱀이오른다분산편차제곱의합자료개수표준편차평균기록칸실전연습첫제출봉합실남음완성실패다시쌓기수리중고리를끌고놓으시오놓은넓이필요한분산제곱들의평균이분산변량평균이편차더고르게모였다");
            for(int i=0;i<Mathf.Min(models.Count,400);i++) b.Append(models[i].prompt).Append(PoksinRules.ArrayText(models[i].raw));
            MgfText.Prewarm(b.ToString());
        }

        void Update()
        {
            float dt=Mathf.Min(.05f,Time.unscaledDeltaTime);
            Relayout();
            HandlePointer();
            if(flow==Flow.Opening){openingClock+=dt;if(openingClock>=.36f)BeginPractice();}
            else if(flow==Flow.Practice){idleClock+=dt;if(idleClock>=8f){idleClock=0;PulseGuide(true);}}
            else if(flow==Flow.Playing)
            {
                timeLeft-=dt;
                if(timeLeft<=0){timeLeft=0;EndRun("time");}
                else {idleClock+=dt;if(idleClock>=8f){idleClock=0;PulseGuide(false);}}
            }
            else if(flow==Flow.Reveal){revealClock+=dt;if(revealClock>=(revealCorrect?1.15f:1.35f))FinishReveal();}
            TickWorld(dt); RefreshDynamic();
            MgfLook.FitWidth(cam,36f,.72f);
        }

        void ResetState()
        {
            st.score=0;st.lives=PoksinRules.StartThreads;st.level=1;st.phase="title";st.solved=0;
            st.band=0;st.round=0;st.firstCorrect=0;st.firstAttempts=0;st.firstMask=0;st.selectedSide=0;st.inputCount=0;
            st.feedbackKind="";st.misconceptionId="";st.activeProblemId="";st.masteryImpossible=false;
            st.onboarding=false;st.repairMode=false;st.dragging=false;
            timeLeft=PoksinRules.SessionSeconds;firstAttempt=true;dragging=false;moved=false;endReason="";
            openingClock=revealClock=idleClock=animClock=0;scoreShown=0;lastSecond=-1;
        }

        void ShowTitle()
        {
            ResetState();flow=Flow.Title;current=null;deck=null;st.phase="title";
            titleRoot.gameObject.SetActive(true);gameRoot.gameObject.SetActive(false);
            SetGroup(titleUi,true);SetGroup(gameUi,false);SetGroup(revealUi,false);SetGroup(endUi,false);SetGroup(toastUi,false);
            ResetStack();Notify();
        }

        void StartOpening()
        {
            if(flow!=Flow.Title&&flow!=Flow.End)return;
            ResetState();flow=Flow.Opening;st.phase="playing";openingClock=0;
            SetGroup(titleUi,false);SetGroup(endUi,false);titleRoot.gameObject.SetActive(true);
            animClock=0;MgfSfx.Play("whoosh",.45f);Notify();
        }

        void BeginPractice()
        {
            flow=Flow.Practice;st.phase="playing";st.onboarding=true;current=PoksinRules.Practice();
            st.activeProblemId=current.id;st.band=0;st.level=1;st.round=0;st.selectedSide=0;firstAttempt=true;
            titleRoot.gameObject.SetActive(false);gameRoot.gameObject.SetActive(true);SetGroup(gameUi,true);
            LoadProblem(true);PulseGuide(true);Notify();
        }

        void BeginRun(bool deterministic)
        {
            runSerial++;ResetState();flow=Flow.Playing;st.phase="playing";st.onboarding=false;
            int seed=deterministic?20261031:unchecked(Environment.TickCount+runSerial*7919);
            deck=PoksinRules.BuildDeck(models,seed);timeLeft=PoksinRules.SessionSeconds;
            titleRoot.gameObject.SetActive(false);gameRoot.gameObject.SetActive(true);
            SetGroup(titleUi,false);SetGroup(gameUi,true);SetGroup(revealUi,false);SetGroup(endUi,false);
            ResetStack();LoadRound(0);Notify();
        }

        void LoadRound(int index)
        {
            if(index>=PoksinRules.TargetLayers){EndRun(st.firstCorrect>=PoksinRules.MasteryNeeded?"clear":"mastery");return;}
            current=deck[index];flow=Flow.Playing;st.phase="playing";st.round=index+1;st.band=current.band;st.level=current.band;
            st.activeProblemId=current.id;st.selectedSide=0;st.feedbackKind="";st.misconceptionId="";st.repairMode=false;
            firstAttempt=true;dragging=false;moved=false;LoadProblem(false);Notify();
        }

        void LoadProblem(bool practice)
        {
            SetCushionSide(0,true);activeCushion.gameObject.SetActive(true);seamLoose.gameObject.SetActive(false);
            gecko.localPosition=new Vector3(0,1.45f,-.15f);gecko.localRotation=Quaternion.identity;
            dragTrail.enabled=false;ghostTrail.enabled=practice;handleRing.gameObject.SetActive(practice);
            recordTxt.text=RecordText(current);goalTxt.text=practice?"변을 표준편차에 맞추면 도마뱀이 오른다":DisplayPrompt(current);
            formulaTxt.text="분산 = 편차 제곱의 합 ÷ 자료 개수\n표준편차 = √분산";
            guideTxt.text=practice?"고리를 3칸까지 끌고 놓으시오":"모서리 고리를 끌어 한 변을 정하시오";
            SetGroup(revealUi,false);SetGroup(toastUi,false);RefreshHud();
        }

        string RecordText(CushionProblem p)
        {
            if(p==null)return "";
            if(p.band==0)return "기록  3, 3, 9, 9     평균 6\n분산 9";
            string raw="기록  "+PoksinRules.ArrayText(p.raw)+"     평균 "+p.mean;
            if(p.band==1)return raw+"\n분산 "+p.variance+" 칸²";
            if(p.band==2){var sq=new int[p.deviations.Length];for(int i=0;i<sq.Length;i++)sq[i]=p.deviations[i]*p.deviations[i];return raw+"\n편차²  "+PoksinRules.ArrayText(sq);}
            return raw+"\n자료 개수 6";
        }

        string DisplayPrompt(CushionProblem p)
        {
            if(p.band==1)return "분산 "+p.variance+" → 표준편차만큼 한 변을 늘리시오";
            if(p.band==2)return "편차 제곱의 평균 → 표준편차만큼 늘리시오";
            return "원자료 → 표준편차만큼 한 변을 늘리시오";
        }

        void Submit(int side)
        {
            if((flow!=Flow.Practice&&flow!=Flow.Playing)||current==null)return;
            side=Mathf.Clamp(side,0,8);st.selectedSide=side;revealCorrect=PoksinRules.IsCorrect(current,side);revealPractice=flow==Flow.Practice;
            if(!revealPractice&&firstAttempt)
            {
                int stamp=st.firstAttempts;st.firstAttempts++;
                if(revealCorrect){st.firstCorrect++;st.firstMask|=1<<stamp;}
            }
            if(revealCorrect)
            {
                st.feedbackKind="correct";st.misconceptionId="";
                if(!revealPractice){st.solved++;st.score+=firstAttempt?100:35;st.repairMode=false;}
            }
            else
            {
                st.feedbackKind="wrong";st.misconceptionId=PoksinRules.MisconceptionId(current,side);
                if(!revealPractice){st.lives=Math.Max(0,st.lives-1);st.repairMode=true;}
                firstAttempt=false;
            }
            int remaining=PoksinRules.TargetLayers-st.firstAttempts;
            st.masteryImpossible=!revealPractice&&st.firstCorrect+remaining<PoksinRules.MasteryNeeded;
            failAfterReveal=!revealPractice&&!revealCorrect&&(st.lives<=0||st.masteryImpossible);
            flow=Flow.Reveal;st.phase="playing";revealClock=0;dragging=false;st.dragging=false;
            BeginReveal(side,revealCorrect,revealPractice);RefreshHud();Notify();
        }

        void BeginReveal(int side,bool ok,bool practice)
        {
            SetGroup(revealUi,true);handleRing.gameObject.SetActive(false);ghostTrail.enabled=false;dragTrail.enabled=false;
            if(ok)
            {
                revealTitle.text=practice?"분산 9 → 표준편차 3":"정확한 한 변 "+side+"칸";
                revealMath.text=practice?"편차 −3, −3, 3, 3\n제곱 넓이의 평균 9  →  √9 = 3":
                    "놓은 변 "+side+"칸  →  넓이 "+(side*side)+"칸²\n필요한 분산 "+current.variance+"칸²";
                MgfSfx.Play("correct",.50f);animClock=0;
            }
            else
            {
                revealTitle.text=practice?"고리를 다시 맞추시오":"옆솔기가 풀렸습니다";
                revealMath.text="놓은 넓이 "+(side*side)+"칸² / 필요한 분산 "+current.variance+"칸²\n"+WrongReason(st.misconceptionId);
                seamLoose.gameObject.SetActive(true);MgfSfx.Play("wrong",.30f);MgfFx.Shake(cam,.11f);animClock=0;
            }
        }

        string WrongReason(string id)
        {
            if(id=="deviation_average_zero")return "편차의 합은 0, 제곱의 평균이 분산입니다.";
            if(id=="variance_as_side")return "분산은 넓이, 표준편차는 그 정사각형의 변입니다.";
            if(id=="forgot_divide_by_n")return "편차 제곱의 합을 자료 개수로 나누시오.";
            return id=="area_too_large"?"만든 정사각형의 넓이가 더 큽니다.":"만든 정사각형의 넓이가 더 작습니다.";
        }

        void FinishReveal()
        {
            if(flow!=Flow.Reveal)return;
            SetGroup(revealUi,false);
            if(revealPractice){if(revealCorrect)BeginRun(false);else{flow=Flow.Practice;st.phase="playing";st.selectedSide=0;LoadProblem(true);PulseGuide(true);Notify();}return;}
            if(failAfterReveal){EndRun(st.lives<=0?"thread":"mastery");return;}
            if(revealCorrect)
            {
                LockLayer(st.solved-1,current.side,firstAttempt);
                if(st.solved>=PoksinRules.TargetLayers)EndRun(st.firstCorrect>=PoksinRules.MasteryNeeded?"clear":"mastery");
                else LoadRound(st.solved);
            }
            else
            {
                flow=Flow.Playing;st.phase="playing";st.selectedSide=0;SetCushionSide(0,true);seamLoose.gameObject.SetActive(true);
                guideTxt.text="찢어진 방석을 직접 고쳐 다시 놓으시오";Notify();
            }
        }

        void EndRun(string reason)
        {
            if(flow==Flow.End)return;
            bool clear=reason=="clear"&&st.solved==6&&st.firstCorrect>=5;
            flow=Flow.End;st.phase=clear?"clear":"gameover";endReason=reason;dragging=false;st.dragging=false;
            SetGroup(gameUi,false);SetGroup(revealUi,false);SetGroup(endUi,true);
            endTitle.text=clear?"폭신한 꼭대기 완성":"탑이 여기서 멈췄습니다";
            string why=reason=="time"?"활성 풀이 시간 120초가 끝났습니다.":reason=="thread"?"봉합실 3가닥을 모두 썼습니다.":reason=="mastery"?"첫 제출 5/6에 도달할 수 없습니다.":"두 방석의 폭으로 흩어진 정도가 보입니다.";
            endStats.text="완성 "+st.solved+"/6층     첫 제출 "+st.firstCorrect+"/6\n점수 "+st.score+"\n"+why;
            endTap.text="큰 천 고리를 눌러 다시 쌓기";MgfSfx.Play(clear?"win":"lose",.55f);Notify();
        }

        void HandlePointer()
        {
            if(MgfPointer.Down)
            {
                st.inputCount++;pointerDown=MgfPointer.Position;idleClock=0;RippleAt(pointerDown);
                if(flow==Flow.Title){StartOpening();return;}
                if(flow==Flow.End){StartOpening();return;}
                if(flow==Flow.Opening||flow==Flow.Reveal){Toast(flow==Flow.Reveal?"수치 근거를 확인하시오":"천 자락이 열리는 중입니다");Notify();return;}
                if(flow!=Flow.Practice&&flow!=Flow.Playing)return;
                if(InCushionZone(pointerDown))
                {
                    dragging=true;moved=false;st.dragging=true;st.selectedSide=0;dragTrail.enabled=true;
                    dragTrail.positionCount=2;SetTrail(pointerDown,pointerDown);MgfSfx.Play("tap",.18f);Notify();
                }
                else Refuse("방석 모서리 고리를 끌고 놓으시오");
            }
            if(dragging&&MgfPointer.Held)
            {
                Vector2 p=MgfPointer.Position;float dist=Vector2.Distance(p,pointerDown);if(dist>=18f)moved=true;
                int side=SideFromDrag(pointerDown,p);
                if(side!=st.selectedSide){st.selectedSide=side;SetCushionSide(side,false);st.inputCount++;Notify();}
                SetTrail(pointerDown,p);
            }
            if(dragging&&MgfPointer.Up)
            {
                dragging=false;st.dragging=false;dragTrail.enabled=false;
                if(!moved||st.selectedSide<=0){st.selectedSide=0;SetCushionSide(0,false);Refuse("고리를 18px 이상 끌고 놓으시오");}
                else Submit(st.selectedSide);
            }
        }

        bool InCushionZone(Vector2 p)
        {
            float x=p.x/Mathf.Max(1,Screen.width),y=p.y/Mathf.Max(1,Screen.height);
            return MgfBridge.IsLandLayout ? x>.22f&&x<.80f&&y>.10f&&y<.76f : x>.10f&&x<.94f&&y>.10f&&y<.61f;
        }

        int SideFromDrag(Vector2 a,Vector2 b)
        {
            Vector2 d=b-a;Vector2 dir=new Vector2(.72f,.69f);float projection=Vector2.Dot(d,dir);
            if(projection<18f)return 0;
            float step=Mathf.Max(20f,Mathf.Min(Screen.width,Screen.height)*.058f);
            return Mathf.Clamp(Mathf.RoundToInt(projection/step),1,8);
        }

        void Refuse(string msg){Toast(msg);PulseGuide(flow==Flow.Practice);MgfSfx.Play("wrong",.12f);Notify();}
        void Toast(string msg){toastTxt.text=msg;toastUi.alpha=1;toastUi.gameObject.SetActive(true);animClock=-.65f;}
        void Notify()=>MgfBridge.NotifyChanged();

        // ── IMgfGame
        public void TestStart()=>BeginRun(true);
        void EnsureTestPlaying(){if(flow==Flow.Reveal)FinishReveal();if(flow!=Flow.Playing)BeginRun(true);}
        public void TestAnswerCorrect(){EnsureTestPlaying();Submit(current.side);}
        public void TestAnswerWrong(){EnsureTestPlaying();Submit(current.side==8?7:current.side+1);}
        public string StateJson()
        {
            st.layoutLand=MgfBridge.IsLandLayout;
            st.handleXPermille=MgfBridge.IsLandLayout?700:720;st.handleYPermille=MgfBridge.IsLandLayout?360:300;
            st.trackEndXPermille=MgfBridge.IsLandLayout?880:900;st.trackEndYPermille=MgfBridge.IsLandLayout?700:570;
            return JsonUtility.ToJson(st);
        }
        public string ProblemBankJson()=>PoksinRules.BankJson(models);

        // ── 3D world
        void BuildWorld()
        {
            MgfLook.Sky(RaspberryDeep,Raspberry,MgfLook.Hex("#6E243A"),.92f);
            MgfLook.Sun(new Vector3(48,-32,18),MgfLook.Hex("#FFE8C0"),1.28f,.72f);
            cam=MgfLook.Camera(new Vector3(10.8f,10.5f,-15.8f),new Vector3(0,2.2f,1.4f),36f);
            clothMat=MgfLook.Lit(Raspberry,.08f,0);deepMat=MgfLook.Lit(RaspberryDeep,.04f,0);
            ivoryMat=MgfLook.Lit(Ivory,.12f,0);inkMat=MgfLook.Lit(Ink,.26f,0);limeMat=MgfLook.Lit(Lime,.22f,0,Lime*.06f);
            glowMat=MgfLook.Additive(new Color(1f,.83f,.42f,.76f),MgfLook.SoftDot);
            titleRoot=new GameObject("CrimsonQuiltTitle").transform;gameRoot=new GameObject("CushionCliff").transform;
            BuildQuiltWorld(titleRoot,true);BuildQuiltWorld(gameRoot,false);BuildTitleWorld();BuildGameWorld();
        }

        void BuildQuiltWorld(Transform root,bool title)
        {
            MgfLook.Block("QuiltBack",new Vector3(0,3.3f,6.8f),new Vector3(25,10,1.4f),.55f,clothMat,root);
            for(int i=0;i<12;i++)
            {
                float x=-10.8f+i*1.95f;var channel=MgfLook.Block("StitchedChannel"+i,new Vector3(x,3.3f,5.95f),new Vector3(.11f,9.0f,.12f),.04f,ivoryMat,root).transform;
                channel.localRotation=Quaternion.Euler(0,0,(i%2==0?1:-1)*5f);
            }
            for(int i=0;i<5;i++)MgfLook.Block("HorizontalQuilt"+i,new Vector3(0,-.1f+i*2f,5.82f),new Vector3(24,.10f,.16f),.04f,deepMat,root);
            for(int i=0;i<buttons.Length;i++)
            {
                float x=-9.2f+(i%6)*3.7f,y=.4f+(i/6)*5.1f;
                buttons[i]=MgfLook.Prim(PrimitiveType.Sphere,"Button"+i,new Vector3(x,y,5.48f),new Vector3(.34f,.34f,.18f),i%3==0?limeMat:ivoryMat,root,false).transform;
            }
            MgfLook.Block("NearFoldL",new Vector3(-9,-.8f,1.0f),new Vector3(7,2.2f,9),1.0f,deepMat,root);
            MgfLook.Block("NearFoldR",new Vector3(9,-.6f,1.5f),new Vector3(7,2.0f,8),1.0f,clothMat,root);
        }

        void BuildTitleWorld()
        {
            titleStack=new GameObject("TitleCushionStack").transform;titleStack.SetParent(titleRoot,false);
            for(int i=0;i<3;i++)
            {
                var c=MgfLook.Block("TitleCushion"+i,new Vector3(0,.15f+i*1.75f,.5f+i*.55f),new Vector3(9-i*.8f,1.45f,7.3f-i*.5f),.55f,i==1?deepMat:clothMat,titleStack);
                c.transform.localRotation=Quaternion.Euler(0,(i-1)*6,(i%2==0?-2:3));
                MgfLook.Block("TitleSeam"+i,new Vector3(0,-.76f,-.05f),new Vector3(7.2f,.16f,.20f),.05f,ivoryMat,c.transform);
            }
            var label=MgfLook.Block("SewnTitleLabel",new Vector3(0,2.15f,-3.2f),new Vector3(7.2f,2.1f,.20f),.18f,ivoryMat,titleRoot);
            MakeLoop("TitleLoop",new Vector3(4.1f,1.55f,-3.35f),1.05f,.20f,limeMat,titleRoot);
            BuildGecko(titleRoot,new Vector3(-3.0f,4.7f,-1.3f),1.18f,true);
        }

        void BuildGameWorld()
        {
            for(int i=0;i<stack.Length;i++)
            {
                stack[i]=MgfLook.Block("StackLayer"+i,new Vector3(0,-.65f+i*.44f,2.9f+i*.15f),new Vector3(5.5f,.48f,4.6f),.28f,i%2==0?deepMat:clothMat,gameRoot).transform;
                stack[i].gameObject.SetActive(false);
            }
            activeCushion=new GameObject("ActiveCushion").transform;activeCushion.SetParent(gameRoot,false);activeCushion.localPosition=cushionBase;
            activeTop=MgfLook.Block("StuffedSquare",Vector3.zero,new Vector3(6.7f,.82f,6.7f),.44f,clothMat,activeCushion).transform;
            MgfLook.Block("TopInset",new Vector3(0,.45f,0),new Vector3(5.8f,.10f,5.8f),.22f,deepMat,activeCushion);
            for(int e=0;e<4;e++)
            {
                Vector3 p=e<2?new Vector3(0,.47f,e==0?-3.15f:3.15f):new Vector3(e==2?-3.15f:3.15f,.47f,0);
                Vector3 s=e<2?new Vector3(5.8f,.10f,.13f):new Vector3(.13f,.10f,5.8f);
                MgfLook.Block("IvorySeam"+e,p,s,.04f,ivoryMat,activeCushion);
            }
            handle=MakeLoop("CornerLoop",new Vector3(3.45f,.35f,-3.35f),.58f,.13f,limeMat,activeCushion);
            seamLoose=MgfLook.Block("LooseSeam",new Vector3(3.0f,.2f,-2.9f),new Vector3(.16f,.16f,2.8f),.05f,ivoryMat,activeCushion).transform;
            dragTrail=MakeLine("FabricDragTrail",gameRoot,Lime,.10f);dragTrail.enabled=false;
            ghostTrail=MakeLine("GhostStitchPath",gameRoot,Ivory,.075f);ghostTrail.positionCount=18;
            for(int i=0;i<18;i++){float u=i/17f;ghostTrail.SetPosition(i,new Vector3(3.5f+u*2.6f,1.0f+u*2.2f,-2.25f));}
            BuildGecko(gameRoot,new Vector3(0,2.1f,.4f),1f,false);
        }

        LineRenderer MakeLine(string name,Transform parent,Color c,float width)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);var l=g.AddComponent<LineRenderer>();
            l.sharedMaterial=MgfLook.Unlit(c);l.startColor=l.endColor=c;l.startWidth=l.endWidth=width;l.numCapVertices=4;l.useWorldSpace=true;return l;
        }

        Transform MakeLoop(string name,Vector3 pos,float radius,float thickness,Material mat,Transform parent)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=pos;
            const int pieces=12;
            for(int i=0;i<pieces;i++)
            {
                float a=(i+.5f)/pieces*Mathf.PI*2f;
                var p=MgfLook.Block(name+"Seg"+i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0),
                    new Vector3(radius*Mathf.PI*2f/pieces*1.10f,thickness,thickness),thickness*.45f,mat,root).transform;
                p.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg+90f);
            }
            return root;
        }

        void BuildGecko(Transform parent,Vector3 pos,float scale,bool title)
        {
            gecko=new GameObject(title?"TitleCottonGecko":"CottonGecko").transform;gecko.SetParent(parent,false);gecko.localPosition=pos;gecko.localScale=Vector3.one*scale;
            geckoBody=MgfLook.Prim(PrimitiveType.Capsule,"FabricBody",Vector3.zero,new Vector3(1.3f,.48f,.55f),ivoryMat,gecko,false).transform;
            geckoBody.localRotation=Quaternion.Euler(0,0,90);
            for(int i=0;i<4;i++)
            {
                float x=i<2?-.55f:.55f,z=i%2==0?-.48f:.48f;
                feet[i]=MgfLook.Block("FlatFoot"+i,new Vector3(x,-.28f,z),new Vector3(.62f,.13f,.40f),.11f,ivoryMat,gecko).transform;
                for(int toe=-1;toe<=1;toe++)MgfLook.Block("Toe"+i+"_"+toe,new Vector3(.12f*toe,-.09f,-.25f),new Vector3(.08f,.07f,.30f),.035f,Ink==Color.clear?ivoryMat:inkMat,feet[i]);
            }
            for(int e=-1;e<=1;e+=2)MgfLook.Prim(PrimitiveType.Sphere,"Eye"+e,new Vector3(-.55f,.18f,e*.24f),new Vector3(.20f,.20f,.15f),inkMat,gecko,false);
            tail=new GameObject("ZipperTail").transform;tail.SetParent(gecko,false);tail.localPosition=new Vector3(.75f,.02f,0);
            for(int i=0;i<5;i++)
            {
                var seg=MgfLook.Block("Zipper"+i,new Vector3(i*.34f,Mathf.Sin(i*.8f)*.15f,Mathf.Sin(i*.55f)*.18f),new Vector3(.42f,.16f,.24f),.07f,i==4?limeMat:ivoryMat,tail);
                seg.transform.localRotation=Quaternion.Euler(0,i*12f,i*13f);
            }
        }

        void SetCushionSide(int side,bool immediate)
        {
            float s=side<=0?.24f:Mathf.Lerp(.34f,1.0f,(side-1)/7f);
            activeCushion.localScale=new Vector3(s,1,s);sideTxt.text=side<=0?"0칸 · 아직 제출되지 않음":side+"칸";
        }

        void LockLayer(int index,int side,bool first)
        {
            if(index<0||index>=stack.Length)return;var t=stack[index];t.gameObject.SetActive(true);
            float s=Mathf.Lerp(.58f,1.0f,(side-1)/7f);t.localScale=new Vector3(s,1,s);t.localPosition=new Vector3(0,-.65f+index*.44f,2.9f+index*.15f);
            if(first)MgfFx.Glow(t.position+Vector3.up*.3f,Lime,8);
        }

        void ResetStack(){for(int i=0;i<stack.Length;i++)if(stack[i])stack[i].gameObject.SetActive(false);}
        void SetTrail(Vector2 a,Vector2 b)
        {
            Ray ra=cam.ScreenPointToRay(a),rb=cam.ScreenPointToRay(b);Plane p=new Plane(Vector3.up,new Vector3(0,1.25f,0));
            if(p.Raycast(ra,out float da)&&p.Raycast(rb,out float db)){dragTrail.SetPosition(0,ra.GetPoint(da));dragTrail.SetPosition(1,rb.GetPoint(db));}
        }

        void TickWorld(float dt)
        {
            animClock+=dt;float t=Time.unscaledTime;
            if(titleRoot.gameObject.activeSelf)
            {
                titleStack.localPosition=new Vector3(0,Mathf.Sin(t*.8f)*.10f,0);titleStack.localRotation=Quaternion.Euler(0,Mathf.Sin(t*.35f)*2.2f,0);
            }
            for(int i=0;i<buttons.Length;i++)buttons[i].localScale=Vector3.one*(1+Mathf.Sin(t*1.2f+i)*.06f);
            if(gecko)
            {
                float breath=1+Mathf.Sin(t*2.1f)*.045f;geckoBody.localScale=new Vector3(1.3f,breath*.48f,.55f);
                tail.localRotation=Quaternion.Euler(0,Mathf.Sin(t*1.7f)*9f,Mathf.Sin(t*1.1f)*7f);
            }
            if(flow==Flow.Reveal&&revealCorrect)
            {
                float u=Mathf.Clamp01(revealClock/.82f);float squash=Mathf.Sin(u*Mathf.PI);
                activeCushion.localScale=new Vector3(activeCushion.localScale.x,1-squash*.28f,activeCushion.localScale.z);
                gecko.localPosition=new Vector3(0,1.45f+Mathf.Sin(u*Mathf.PI)*2.0f,-.15f);
                for(int i=0;i<4;i++)feet[i].localPosition+=new Vector3((i<2?-1:1)*squash*.002f,0,(i%2==0?-1:1)*squash*.002f);
            }
            else if(flow==Flow.Reveal&&!revealCorrect){gecko.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*19f)*4f);gecko.localPosition=Vector3.Lerp(gecko.localPosition,new Vector3(2.8f,.55f,-1.9f),dt*5f);}
            if(ghostTrail.enabled)
            {
                float pulse=.65f+.35f*Mathf.Sin(t*4f);ghostTrail.startColor=ghostTrail.endColor=new Color(Ivory.r,Ivory.g,Ivory.b,pulse);
            }
            if(toastUi.gameObject.activeSelf&&animClock>0&&animClock>.85f){toastUi.alpha=Mathf.MoveTowards(toastUi.alpha,0,dt*4);if(toastUi.alpha<=0)toastUi.gameObject.SetActive(false);}
        }

        // ── UI
        RectTransform R(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
        {
            var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;
            r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        Image Img(Transform p,string n,Vector2 a,Vector2 o,Vector2 s,Color c,Sprite sp=null)
        {var r=R(n,p,a,o,s);var im=r.gameObject.AddComponent<Image>();im.color=c;im.sprite=sp;im.raycastTarget=false;if(sp&&sp.border!=Vector4.zero)im.type=Image.Type.Sliced;return im;}
        TextMeshProUGUI Txt(Transform p,string str,Vector2 a,Vector2 o,float size,Color c,float w=360,TextAlignmentOptions al=TextAlignmentOptions.Center)
        {var r=R("Text",p,a,o,new Vector2(w,size*2.7f));var x=r.gameObject.AddComponent<TextMeshProUGUI>();x.font=MgfText.Font;x.fontSize=size;x.color=c;x.alignment=al;x.raycastTarget=false;x.textWrappingMode=TextWrappingModes.Normal;x.text=str;return x;}
        CanvasGroup Group(string n){var g=new GameObject(n,typeof(RectTransform));g.transform.SetParent(MgfText.Canvas.transform,false);var r=(RectTransform)g.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return g.AddComponent<CanvasGroup>();}
        static void SetGroup(CanvasGroup g,bool on){g.gameObject.SetActive(on);g.alpha=on?1:0;}
        static Sprite MakeSprite(int size,float radius,bool ring)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};var px=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {float fx=x+.5f,fy=y+.5f,a;if(ring){float dx=fx-size*.5f,dy=fy-size*.5f,d=Mathf.Sqrt(dx*dx+dy*dy);a=Mathf.Clamp01(1-Mathf.Abs(d-size*.39f)/(size*.065f));}
             else{float qx=Mathf.Max(Mathf.Abs(fx-size*.5f)-(size*.5f-radius),0),qy=Mathf.Max(Mathf.Abs(fy-size*.5f)-(size*.5f-radius),0);a=Mathf.Clamp01(radius-Mathf.Sqrt(qx*qx+qy*qy)+.8f);}px[y*size+x]=new Color32(255,255,255,(byte)(a*255));}
            tex.SetPixels32(px);tex.Apply(false,true);float b=ring?0:radius+2;return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(b,b,b,b));
        }

        void BuildUi()
        {
            roundSprite=MakeSprite(64,15,false);ringSprite=MakeSprite(64,15,true);
            titleUi=Group("TitleUI");gameUi=Group("GameUI");revealUi=Group("RevealUI");endUi=Group("EndUI");toastUi=Group("ToastUI");
            titleLogo=Txt(titleUi.transform,"폭신한\n꼭대기",new Vector2(.5f,.67f),Vector2.zero,48,Ink,330);titleLogo.outlineWidth=.18f;titleLogo.outlineColor=Ivory;
            titleTag=Txt(titleUi.transform,"늘려라 네모 방석",new Vector2(.5f,.50f),Vector2.zero,23,Ivory,330);
            titleMeta=Txt(titleUi.transform,"중학교 3학년 · 산포도     최고 첫 제출 6/6",new Vector2(.5f,.12f),Vector2.zero,16,Ivory,360);
            Img(titleUi.transform,"StartLoopPlate",new Vector2(.5f,.27f),Vector2.zero,new Vector2(255,66),Lime,roundSprite);
            titleStart=Txt(titleUi.transform,"고리를 당겨 시작",new Vector2(.5f,.27f),Vector2.zero,23,Ink,250);

            topBg=Img(gameUi.transform,"TopSeam",new Vector2(.5f,1),new Vector2(0,-47),new Vector2(370,76),Ivory,roundSprite);
            progressTxt=Txt(gameUi.transform,"",new Vector2(.18f,1),new Vector2(0,-47),17,Ink,125);
            livesTxt=Txt(gameUi.transform,"",new Vector2(.50f,1),new Vector2(0,-47),17,RaspberryDeep,120);
            timeTxt=Txt(gameUi.transform,"",new Vector2(.82f,1),new Vector2(0,-47),17,Ink,110);
            recordBg=Img(gameUi.transform,"RecordRibbon",new Vector2(.5f,.80f),Vector2.zero,new Vector2(366,145),Ivory,roundSprite);
            recordTxt=Txt(gameUi.transform,"",new Vector2(.5f,.825f),Vector2.zero,18,Ink,340);
            formulaTxt=Txt(gameUi.transform,"",new Vector2(.5f,.745f),Vector2.zero,15,RaspberryDeep,340);
            goalTxt=Txt(gameUi.transform,"",new Vector2(.5f,.665f),Vector2.zero,18,Ivory,370);goalTxt.outlineWidth=.15f;goalTxt.outlineColor=RaspberryDeep;
            sideTxt=Txt(gameUi.transform,"0칸 · 아직 제출되지 않음",new Vector2(.5f,.17f),Vector2.zero,25,Ivory,350);sideTxt.outlineWidth=.18f;sideTxt.outlineColor=Ink;
            guideTxt=Txt(gameUi.transform,"",new Vector2(.5f,.08f),Vector2.zero,17,Ivory,360);
            handleRing=Img(gameUi.transform,"GuideRing",new Vector2(.74f,.27f),Vector2.zero,new Vector2(88,88),Lime,ringSprite);
            ripple=Img(gameUi.transform,"TouchRipple",new Vector2(.5f,.5f),Vector2.zero,new Vector2(70,70),new Color(Ivory.r,Ivory.g,Ivory.b,0),ringSprite);
            for(int i=1;i<=8;i++)ticks[i]=Img(gameUi.transform,"Tick"+i,new Vector2(.25f+i*.07f,.25f),Vector2.zero,new Vector2(i%2==0?7:5,i%2==0?24:15),Ivory,roundSprite);
            for(int i=0;i<6;i++)firstStamps[i]=Img(gameUi.transform,"FirstStamp"+i,new Vector2(.27f+i*.092f,.92f),Vector2.zero,new Vector2(24,24),new Color(RaspberryDeep.r,RaspberryDeep.g,RaspberryDeep.b,.35f),roundSprite);
            for(int i=0;i<3;i++)threadIcons[i]=Img(gameUi.transform,"Thread"+i,new Vector2(.455f+i*.045f,.955f),Vector2.zero,new Vector2(13,28),RaspberryDeep,roundSprite);

            Img(revealUi.transform,"RevealCloth",new Vector2(.5f,.48f),Vector2.zero,new Vector2(350,185),Ivory,roundSprite);
            revealTitle=Txt(revealUi.transform,"",new Vector2(.5f,.52f),Vector2.zero,25,RaspberryDeep,330);
            revealMath=Txt(revealUi.transform,"",new Vector2(.5f,.44f),Vector2.zero,17,Ink,330);
            Img(endUi.transform,"EndLabel",new Vector2(.5f,.52f),Vector2.zero,new Vector2(354,360),Ivory,roundSprite);
            endTitle=Txt(endUi.transform,"",new Vector2(.5f,.64f),Vector2.zero,34,RaspberryDeep,330);
            endStats=Txt(endUi.transform,"",new Vector2(.5f,.49f),Vector2.zero,20,Ink,330);
            Img(endUi.transform,"RetryLoop",new Vector2(.5f,.33f),Vector2.zero,new Vector2(280,66),Lime,roundSprite);
            endTap=Txt(endUi.transform,"",new Vector2(.5f,.33f),Vector2.zero,20,Ink,270);
            Img(toastUi.transform,"ToastCloth",new Vector2(.5f,.18f),Vector2.zero,new Vector2(340,60),Ink,roundSprite);
            toastTxt=Txt(toastUi.transform,"",new Vector2(.5f,.18f),Vector2.zero,16,Ivory,325);
            scoreStamp=Img(gameUi.transform,"ScoreStamp",new Vector2(.08f,.955f),Vector2.zero,new Vector2(50,34),Lime,roundSprite);
            SetGroup(titleUi,false);SetGroup(gameUi,false);SetGroup(revealUi,false);SetGroup(endUi,false);SetGroup(toastUi,false);
        }

        void Relayout()
        {
            bool land=MgfBridge.IsLandLayout;
            if(land==lastLayoutLand&&recordBg!=null&&recordBg.rectTransform.anchorMin.x==(land ? .18f : .5f))return;
            lastLayoutLand=land;
            if(land)
            {
                Anchor(topBg,new Vector2(.50f,.97f),Vector2.zero,new Vector2(760,64));
                Anchor(recordBg,new Vector2(.23f,.55f),Vector2.zero,new Vector2(250,265));
                Anchor(recordTxt,new Vector2(.23f,.61f),Vector2.zero);recordTxt.fontSize=15;recordTxt.rectTransform.sizeDelta=new Vector2(232,112);
                Anchor(formulaTxt,new Vector2(.23f,.47f),Vector2.zero);formulaTxt.fontSize=12;formulaTxt.rectTransform.sizeDelta=new Vector2(232,76);
                Anchor(goalTxt,new Vector2(.68f,.78f),Vector2.zero);goalTxt.fontSize=14;goalTxt.rectTransform.sizeDelta=new Vector2(360,84);
                Anchor(progressTxt,new Vector2(.17f,.94f),Vector2.zero);Anchor(livesTxt,new Vector2(.50f,.94f),Vector2.zero);Anchor(timeTxt,new Vector2(.84f,.94f),Vector2.zero);
                Anchor(sideTxt,new Vector2(.63f,.16f),Vector2.zero);Anchor(guideTxt,new Vector2(.63f,.08f),Vector2.zero);
                Anchor(handleRing,new Vector2(.73f,.31f),Vector2.zero);
            }
            else
            {
                Anchor(topBg,new Vector2(.5f,1),new Vector2(0,-47),new Vector2(370,76));
                Anchor(recordBg,new Vector2(.5f,.80f),Vector2.zero,new Vector2(366,145));
                Anchor(recordTxt,new Vector2(.5f,.825f),Vector2.zero);recordTxt.fontSize=18;
                Anchor(formulaTxt,new Vector2(.5f,.745f),Vector2.zero);formulaTxt.fontSize=15;
                Anchor(goalTxt,new Vector2(.5f,.665f),Vector2.zero);goalTxt.fontSize=18;goalTxt.rectTransform.sizeDelta=new Vector2(370,72);
                Anchor(progressTxt,new Vector2(.18f,1),new Vector2(0,-47));Anchor(livesTxt,new Vector2(.50f,1),new Vector2(0,-47));Anchor(timeTxt,new Vector2(.82f,1),new Vector2(0,-47));
                Anchor(sideTxt,new Vector2(.5f,.17f),Vector2.zero);Anchor(guideTxt,new Vector2(.5f,.08f),Vector2.zero);
                Anchor(handleRing,new Vector2(.74f,.27f),Vector2.zero);
            }
        }

        void Anchor(Component c,Vector2 anchor,Vector2 pos){var r=(RectTransform)c.transform;r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=pos;}
        void Anchor(Component c,Vector2 anchor,Vector2 pos,Vector2 size){Anchor(c,anchor,pos);((RectTransform)c.transform).sizeDelta=size;}

        void PulseGuide(bool strong){handleRing.gameObject.SetActive(true);handleRing.rectTransform.sizeDelta=Vector2.one*(strong?116:92);guideTxt.text=strong?"맥동하는 고리에서 오른쪽 위 눈금까지 끌고 놓으시오":"모서리 고리를 끌고 놓으시오";}
        void RippleAt(Vector2 screen)
        {
            ripple.gameObject.SetActive(true);Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)gameUi.transform,screen,null,out local);
            ripple.rectTransform.anchoredPosition=local;ripple.color=Ivory;ripple.rectTransform.localScale=Vector3.one*.35f;animClock=0;
        }

        void RefreshHud()
        {
            progressTxt.text=st.onboarding?"연습":"방석 "+st.solved+"/6";
            livesTxt.text="봉합실 "+st.lives+"/3";
            timeTxt.text=st.onboarding?"시간 정지":Mathf.CeilToInt(timeLeft)+"초";
            for(int i=0;i<6;i++)firstStamps[i].color=i<st.firstAttempts?(((st.firstMask&(1<<i))!=0)?Lime:RaspberryDeep):new Color(RaspberryDeep.r,RaspberryDeep.g,RaspberryDeep.b,.28f);
            for(int i=0;i<3;i++)threadIcons[i].color=i<st.lives?RaspberryDeep:new Color(RaspberryDeep.r,RaspberryDeep.g,RaspberryDeep.b,.18f);
        }

        void RefreshDynamic()
        {
            int sec=Mathf.CeilToInt(timeLeft);if(sec!=lastSecond){lastSecond=sec;if(gameUi.gameObject.activeSelf)RefreshHud();}
            scoreShown=Mathf.MoveTowards(scoreShown,st.score,Time.unscaledDeltaTime*420f);
            scoreStamp.rectTransform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01((st.score-scoreShown)/100f)*Mathf.PI)*.15f);
            var sc=scoreStamp.GetComponentInChildren<TextMeshProUGUI>();
            if(!sc){sc=Txt(scoreStamp.transform,"0",new Vector2(.5f,.5f),Vector2.zero,15,Ink,48);}
            sc.text=Mathf.RoundToInt(scoreShown).ToString();
            if(handleRing.gameObject.activeSelf){float p=1+Mathf.Sin(Time.unscaledTime*4f)*.10f;handleRing.rectTransform.localScale=Vector3.one*p;}
            if(ripple.gameObject.activeSelf){ripple.rectTransform.localScale=Vector3.MoveTowards(ripple.rectTransform.localScale,Vector3.one*1.8f,Time.unscaledDeltaTime*4);var c=ripple.color;c.a=Mathf.MoveTowards(c.a,0,Time.unscaledDeltaTime*2.6f);ripple.color=c;if(c.a<=0)ripple.gameObject.SetActive(false);}
        }
    }
}
