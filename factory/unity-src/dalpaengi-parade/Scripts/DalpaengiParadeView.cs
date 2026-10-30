using System;
using TMPro;
using Mgf;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.DalpaengiParade
{
    public partial class DalpaengiParadeGame
    {
        static readonly Color Void = MgfLook.Hex("#171427");
        static readonly Color Magenta = MgfLook.Hex("#B93B7A");
        static readonly Color Ivory = MgfLook.Hex("#F2E9D8");
        static readonly Color Cyan = MgfLook.Hex("#47D7D1");
        static readonly Color Brass = MgfLook.Hex("#B98A4A");
        static readonly Color Ink = MgfLook.Hex("#21192F");

        Camera cam;
        Vector3 camBase;
        Transform worldRoot;
        readonly Transform[] ringRoots = new Transform[4];
        readonly Transform[] groupRoots = new Transform[4];
        readonly Transform[] groupKeys = new Transform[4];
        readonly Transform[] ambientSnails = new Transform[8];
        readonly Vector3[] groupTargets = new Vector3[4];
        readonly float[] dropPunch = new float[4];
        Transform titleKey;
        Transform ribbonRoot;
        LineRenderer dragTrail;
        LineRenderer guideTrail;
        Transform guideFinger;
        Material railMat, railDarkMat, ivoryMat, resinMat, brassMat, cyanMat, inkMat;
        bool lastLand;
        float feedbackClock;
        bool feedbackWasCorrect;
        float guidePulse;

        RectTransform uiRoot, titleRoot, gameRoot, endRoot;
        CanvasGroup titleGroup, gameGroup, endGroup;
        TextMeshProUGUI titleLogo, titleCta, titleMeta;
        TextMeshProUGUI goalText, promptText, bandText, timerText, progressText, messageText;
        TextMeshProUGUI revealText, endTitle, endStats, endCta;
        RectTransform messagePlate, revealPlate, ripple;
        readonly RectTransform[] groupBoards = new RectTransform[4];
        readonly TextMeshProUGUI[] groupTexts = new TextMeshProUGUI[4];
        readonly RectTransform[] slotBoards = new RectTransform[4];
        readonly TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[4];
        readonly RectTransform[] ribbonIcons = new RectTransform[3];
        Sprite roundedSprite, ringSprite;
        float messageClock, rippleClock, uiPulse;

        void BuildWorld()
        {
            railMat = MgfLook.Lit(Magenta, .82f, .12f, Magenta * .10f);
            railDarkMat = MgfLook.Lit(MgfLook.Hex("#76234F"), .74f, .10f, Magenta * .025f);
            ivoryMat = MgfLook.Lit(Ivory, .88f, .02f, Ivory * .018f);
            resinMat = MgfLook.Alpha(new Color(.83f, .10f, .42f, .68f));
            brassMat = MgfLook.Lit(Brass, .64f, .72f, Brass * .035f);
            cyanMat = MgfLook.Lit(Cyan, .72f, .15f, Cyan * .20f);
            inkMat = MgfLook.Lit(Ink, .22f, .02f);

            MgfLook.Sky(MgfLook.Hex("#0B0918"), Void, MgfLook.Hex("#241536"), .58f);
            MgfLook.Sun(new Vector3(42f, -32f, -18f), MgfLook.Hex("#D6FFFF"), 1.2f, .52f);
            cam = MgfLook.Camera(new Vector3(0f, 14f, -18f), new Vector3(0f, .2f, .5f), 34f);
            cam.orthographic = true;
            cam.orthographicSize = 12f;
            camBase = cam.transform.position;
            cam.backgroundColor = Void;

            worldRoot = new GameObject("Suspended ultraviolet contour theatre").transform;
            for (int i = 0; i < ringRoots.Length; i++)
            {
                var root = new GameObject("Enamel contour orbit " + (i + 1)).transform;
                root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(0f, -.35f + i * .22f, .4f + i * .24f);
                root.localRotation = Quaternion.Euler(0f, i * 17f, 0f);
                var mesh = NewMesh("Magenta baked enamel rail", TorusXZ(3.0f + i * 1.55f, .13f + i * .015f, 72, 8),
                    i % 2 == 0 ? railMat : railDarkMat, root);
                mesh.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
                ringRoots[i] = root;
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI * .25f + i * .24f;
                    var tick = MgfLook.Prim(PrimitiveType.Cube, "Cyan orbit tick", new Vector3(Mathf.Cos(a)*(3f+i*1.55f), .08f, Mathf.Sin(a)*(3f+i*1.55f)),
                        new Vector3(.06f,.13f,.42f), cyanMat, root, false);
                    tick.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                }
            }

            // 황동 현수 암은 바닥 대신 공중 궤도가 세계라는 점을 강조한다.
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                var arm = MgfLook.Prim(PrimitiveType.Cube, "Brass suspension arm", new Vector3(Mathf.Cos(a)*7.2f, 3.4f, Mathf.Sin(a)*7.2f),
                    new Vector3(.07f, 3.3f, .07f), brassMat, worldRoot, false);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, 8f * Mathf.Sin(a));
            }

            titleKey = BuildWindingKey("Central winding key", worldRoot, 2.2f);
            titleKey.localPosition = new Vector3(0f, 1.2f, -.8f);

            ribbonRoot = new GameObject("Three visible wound ribbons").transform;
            ribbonRoot.SetParent(worldRoot, false);
            ribbonRoot.localPosition = new Vector3(0f, .45f, 4.4f);
            for (int i = 0; i < 3; i++)
            {
                var r = NewMesh("Wound brass ribbon " + (i+1), TorusXZ(.42f + i*.28f, .055f, 44, 6), i==2?cyanMat:brassMat, ribbonRoot);
                r.transform.localPosition = new Vector3((i-1)*1.3f, 0f, 0f);
            }

            for (int i = 0; i < groupRoots.Length; i++)
            {
                var g = new GameObject("Clockwork snail troupe " + (i + 1)).transform;
                g.SetParent(worldRoot, false);
                for (int s = 0; s < 4; s++)
                {
                    var snail = BuildSnail("Troupe snail " + (s + 1), g, .72f);
                    snail.localPosition = new Vector3((s%2-.5f)*.92f, 0f, (s/2-.5f)*.85f);
                }
                var key = BuildWindingKey("Troupe key", g, .42f);
                key.localPosition = new Vector3(0f, .88f, .15f);
                groupKeys[i] = key;
                groupRoots[i] = g;
            }

            for (int i = 0; i < ambientSnails.Length; i++)
            {
                var a = BuildSnail("Orbit attendant " + (i + 1), worldRoot, .55f);
                float angle = i * Mathf.PI * 2f / ambientSnails.Length;
                a.localPosition = new Vector3(Mathf.Cos(angle)*8.3f, -.1f + (i%3)*.13f, Mathf.Sin(angle)*6.4f);
                a.localRotation = Quaternion.Euler(0f, -angle*Mathf.Rad2Deg+90f, 0f);
                ambientSnails[i] = a;
            }

            dragTrail = NewLine("Dragged cyan afterimage", Cyan, .10f, 12);
            dragTrail.enabled = false;
            guideTrail = NewLine("Onboarding dotted trajectory", new Color(.29f,.84f,.82f,.78f), .075f, 18);
            guideTrail.enabled = false;
            guideFinger = SafeSphere("Guiding fingertip", Vector3.zero, new Vector3(.42f,.12f,.42f), cyanMat, worldRoot).transform;
            guideFinger.gameObject.SetActive(false);
        }

        Transform BuildSnail(string name, Transform parent, float scale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one * scale;
            SafeSphere("Milky enamel body", new Vector3(0f,.22f,0f), new Vector3(.72f,.40f,1.08f), ivoryMat, root);
            var shell = SafeSphere("Raspberry resin spiral shell", new Vector3(0f,.68f,.18f), new Vector3(.92f,.28f,.92f), resinMat, root);
            shell.transform.localRotation = Quaternion.Euler(90f,0f,0f);
            for (int r = 0; r < 3; r++)
            {
                var spiral = NewMesh("Resin shell spiral", TorusXZ(.18f+r*.17f,.035f,32,5), r==2?brassMat:railMat, root);
                spiral.transform.localPosition = new Vector3(0f,.72f,-.12f-r*.01f);
                spiral.transform.localRotation = Quaternion.Euler(90f,0f,0f);
            }
            for (int side=-1;side<=1;side+=2)
            {
                var stalk=MgfLook.Prim(PrimitiveType.Cube,"Short eyestalk",new Vector3(side*.22f,.66f,-.42f),new Vector3(.045f,.26f,.045f),ivoryMat,root,false);
                stalk.transform.localRotation=Quaternion.Euler(18f,0f,side*8f);
                SafeSphere("Ink eye",new Vector3(side*.28f,.91f,-.50f),Vector3.one*.11f,inkMat,root);
            }
            var key=BuildWindingKey("Tiny brass key",root,.22f);
            key.localPosition=new Vector3(.52f,.82f,.18f);
            key.localRotation=Quaternion.Euler(0f,0f,90f);
            return root;
        }

        Transform BuildWindingKey(string name, Transform parent, float scale)
        {
            var root=new GameObject(name).transform; root.SetParent(parent,false); root.localScale=Vector3.one*scale;
            MgfLook.Prim(PrimitiveType.Cube,"Key stem",Vector3.zero,new Vector3(.16f,.62f,.16f),brassMat,root,false);
            for(int side=-1;side<=1;side+=2)
                MgfLook.Block("Key wing",new Vector3(side*.48f,.48f,0f),new Vector3(.64f,.34f,.15f),.12f,brassMat,root);
            SafeSphere("Cyan key rivet",new Vector3(0f,.48f,-.1f),Vector3.one*.18f,cyanMat,root);
            return root;
        }

        void BuildUi()
        {
            roundedSprite=MakeRoundedSprite(72,18f,false);
            ringSprite=MakeRoundedSprite(72,18f,true);
            uiRoot=Full("Dalpaengi Parade UI",MgfText.Canvas.transform);

            titleRoot=Full("Orbital title",uiRoot); titleGroup=titleRoot.gameObject.AddComponent<CanvasGroup>();
            titleLogo=Txt("Orbit logo",titleRoot,"달팽이\n퍼레이드",new Vector2(.5f,.82f),Vector2.zero,52f,Ivory,360f);
            titleLogo.fontStyle=FontStyles.Bold; titleLogo.outlineColor=Magenta; titleLogo.outlineWidth=.22f; titleLogo.lineSpacing=-22f;
            titleCta=Txt("Key CTA",titleRoot,"태엽 키를 눌러 감아 시작",new Vector2(.5f,.42f),Vector2.zero,21f,Ivory,350f);
            titleCta.outlineColor=Void; titleCta.outlineWidth=.18f;
            titleMeta=Txt("Title meta",titleRoot,"중3 · 산포도     최고 첫 제출 0/6",new Vector2(.5f,.075f),Vector2.zero,13f,Ivory,365f);

            gameRoot=Full("Game HUD",uiRoot); gameGroup=gameRoot.gameObject.AddComponent<CanvasGroup>();
            var goal=Img("Ivory goal plate",gameRoot,new Vector2(.5f,.955f),Vector2.zero,new Vector2(360f,44f),Ivory,roundedSprite);
            goalText=Txt("Goal",goal,"첫 제출 0/6 · 목표 5",new Vector2(.5f,.5f),Vector2.zero,15f,Ink,335f);
            var prompt=Img("Precision record heading",gameRoot,new Vector2(.5f,.855f),Vector2.zero,new Vector2(366f,108f),new Color(.95f,.91f,.85f,.96f),roundedSprite);
            bandText=Txt("Band",prompt,"편차",new Vector2(.5f,.78f),Vector2.zero,14f,Magenta,340f);
            promptText=Txt("Prompt",prompt,"",new Vector2(.5f,.42f),Vector2.zero,15f,Ink,340f);
            promptText.textWrappingMode=TextWrappingModes.Normal;
            progressText=Txt("Progress",gameRoot,"행진 0/6",new Vector2(.17f,.765f),Vector2.zero,13f,Ivory,120f);
            timerText=Txt("Timer",gameRoot,"3:00",new Vector2(.83f,.765f),Vector2.zero,15f,Cyan,100f);
            for(int i=0;i<3;i++)
            {
                ribbonIcons[i]=Img("Ribbon "+(i+1),gameRoot,new Vector2(.5f,.765f),new Vector2((i-1)*27f,0f),new Vector2(19f,34f),Brass,roundedSprite);
                ribbonIcons[i].localRotation=Quaternion.Euler(0f,0f,(i-1)*8f);
            }

            for(int i=0;i<4;i++)
            {
                groupBoards[i]=Img("Enamel record plate "+(i+1),gameRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(132f,62f),Ivory,roundedSprite);
                groupTexts[i]=Txt("Record text "+(i+1),groupBoards[i],"",new Vector2(.5f,.5f),Vector2.zero,13f,Ink,122f);
                groupTexts[i].textWrappingMode=TextWrappingModes.Normal;
                slotBoards[i]=Img("Departure slot "+(i+1),gameRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(112f,48f),new Color(.29f,.84f,.82f,.22f),ringSprite);
                slotTexts[i]=Txt("Slot label "+(i+1),slotBoards[i],(i+1)+"",new Vector2(.5f,.5f),Vector2.zero,13f,Cyan,104f);
            }

            messagePlate=Img("Live instruction ribbon",gameRoot,new Vector2(.5f,.16f),Vector2.zero,new Vector2(352f,48f),new Color(.73f,.23f,.48f,.95f),roundedSprite);
            messageText=Txt("Live instruction",messagePlate,"",new Vector2(.5f,.5f),Vector2.zero,14f,Ivory,332f);
            messagePlate.gameObject.SetActive(false);
            revealPlate=Img("Turnover evidence",gameRoot,new Vector2(.5f,.50f),Vector2.zero,new Vector2(360f,104f),Ivory,roundedSprite);
            revealText=Txt("Evidence",revealPlate,"",new Vector2(.5f,.5f),Vector2.zero,18f,Ink,336f);
            revealText.textWrappingMode=TextWrappingModes.Normal; revealPlate.gameObject.SetActive(false);
            ripple=Img("Ultraviolet input ripple",gameRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(70f,70f),new Color(.29f,.84f,.82f,.72f),ringSprite);
            ripple.gameObject.SetActive(false);

            endRoot=Full("Result theatre",uiRoot); endGroup=endRoot.gameObject.AddComponent<CanvasGroup>();
            var endPanel=Img("Result enamel medallion",endRoot,new Vector2(.5f,.54f),Vector2.zero,new Vector2(366f,330f),new Color(.95f,.91f,.85f,.97f),roundedSprite);
            endTitle=Txt("Result title",endPanel,"",new Vector2(.5f,.76f),Vector2.zero,36f,Magenta,340f); endTitle.fontStyle=FontStyles.Bold;
            endStats=Txt("Result stats",endPanel,"",new Vector2(.5f,.45f),Vector2.zero,19f,Ink,330f); endStats.textWrappingMode=TextWrappingModes.Normal;
            endCta=Txt("Restart key",endPanel,"태엽을 다시 감기",new Vector2(.5f,.16f),Vector2.zero,20f,Magenta,330f);
            SetEndVisible(false); SetGameVisible(false); SetTitleVisible(true);
            UpdateLayoutIfNeeded(true);
        }

        void ShowTitle()
        {
            localPhase=LocalPhase.Title; st.phase="title"; st.lives=ParadeRules.StartLives; st.level=1; st.solved=0;
            SetTitleVisible(true); SetGameVisible(false); SetEndVisible(false);
            for(int i=0;i<groupRoots.Length;i++) groupRoots[i].gameObject.SetActive(false);
            titleKey.gameObject.SetActive(true); ribbonRoot.gameObject.SetActive(true);
        }

        void PrepareProblemVisuals(bool practice)
        {
            titleKey.gameObject.SetActive(false); ribbonRoot.gameObject.SetActive(true);
            for(int i=0;i<4;i++)
            {
                bool on=i<current.groups.Length;
                groupRoots[i].gameObject.SetActive(on); groupBoards[i].gameObject.SetActive(on); slotBoards[i].gameObject.SetActive(on);
                if(!on) continue;
                var g=current.groups[i];
                if(current.band==ParadeBand.Deviation)
                    groupTexts[i].text="<b>"+g.label+"</b>  기록 "+g.record+" 초\n평균 "+g.mean+" 초";
                else if(current.band==ParadeBand.Variance)
                    groupTexts[i].text="<b>"+g.label+"</b>  "+Join(g.values)+"\n평균 "+g.mean;
                else groupTexts[i].text="<b>"+g.label+"</b>  "+(g.showStandardDeviation?"표준편차 "+ParadeRules.SqrtText(g.key)+" 초":"분산 "+g.key+" 초²");
                slotTexts[i].text=(i==0?"작음  ":"")+(i+1)+(i==current.groups.Length-1?"  큼":"");
            }
            bandText.text=practice?"연습 · 편차=변량−평균":current.band==ParadeBand.Deviation?"1단계 · 편차":current.band==ParadeBand.Variance?"2단계 · 분산":"3단계 · 표준편차";
            promptText.text=current.band==ParadeBand.Deviation?"표시한 기록의 편차가 작은 순으로 무리를 놓으시오.":current.band==ParadeBand.Variance?"네 기록의 분산이 작은 순으로 무리를 놓으시오.":"분산과 표준편차를 같은 척도로 바꾸어 놓으시오.";
            ResetGroupTargets(); RefreshHud(); UpdateUiPositions();
            guideTrail.enabled=practice; guideFinger.gameObject.SetActive(practice);
        }

        void RefreshHud()
        {
            goalText.text="첫 제출 "+st.firstCorrect+"/6 · 목표 5";
            progressText.text="행진 "+st.solved+"/6";
            int sec=Mathf.Max(0,Mathf.CeilToInt(sessionLeft)); timerText.text=(sec/60)+":"+(sec%60).ToString("D2");
            for(int i=0;i<3;i++) ribbonIcons[i].gameObject.SetActive(i<st.lives);
            uiPulse=1f;
        }

        void ShowReveal(bool correct,string evidence)
        {
            revealPlate.gameObject.SetActive(true); revealText.text=(correct?"✓  ":"↯  ")+evidence;
            revealText.color=correct?MgfLook.Hex("#166D67"):MgfLook.Hex("#7E244C");
            feedbackClock=correct?1.65f:1.1f; feedbackWasCorrect=correct;
            if(correct) MgfSfx.Play("correct"); else MgfSfx.Play("wrong");
        }

        void HideReveal(){revealPlate.gameObject.SetActive(false);}

        void ShowEnd(bool won,string reason)
        {
            SetEndVisible(true); endTitle.text=won?"행진 성공":"행진 중단";
            endTitle.color=won?Cyan:Magenta; endStats.text=reason; endCta.text="태엽을 다시 감기";
            titleKey.gameObject.SetActive(true);
        }

        void RefreshEndCount()
        {
            if(localPhase!=LocalPhase.End)return;
            float t=Mathf.Clamp01(endClock/1.1f);
            int shownFirst=Mathf.RoundToInt(st.firstCorrect*t); int shownScore=Mathf.RoundToInt(st.score*t);
            endStats.text=endReason+"\n\n첫 제출 "+shownFirst+"/6 · 점수 "+shownScore+"\n리본 "+st.lives+"가닥";
        }

        void ShowPracticeMath(int group,int slot)
        {
            var g=current.groups[group]; ShowMessage(g.record+" 초 − 평균 "+g.mean+" 초 = 편차 "+Signed(g.key)+" 초",1.1f);
        }

        void ShowConceptBridge(string text){ShowMessage(text,2f);}
        void ShowMessage(string text,float seconds){messageText.text=text;messagePlate.gameObject.SetActive(true);messageClock=seconds<=0f?999f:seconds;}

        void AnimateWorld(float dt)
        {
            for(int i=0;i<ringRoots.Length;i++)
            {
                float speed=(i%2==0?1f:-1f)*(2.2f+i*.55f);
                if(feedbackClock>0f&&feedbackWasCorrect)speed*=10f;
                ringRoots[i].Rotate(0f,speed*dt,0f,Space.Self);
                float breathe=1f+Mathf.Sin(idleClock*(.55f+i*.08f)+i)*.008f;
                ringRoots[i].localScale=Vector3.one*breathe;
            }
            if(titleKey&&titleKey.gameObject.activeSelf)
            {
                titleKey.localRotation=Quaternion.Euler(0f,Mathf.Sin(titleClock*.9f)*8f,titleClock*22f);
                titleKey.localScale=Vector3.one*(2.2f+Mathf.Sin(titleClock*1.7f)*.08f);
            }
            for(int i=0;i<ambientSnails.Length;i++)
            {
                if(!ambientSnails[i])continue;
                var p=ambientSnails[i].localPosition; p.y=-.1f+(i%3)*.13f+Mathf.Sin(idleClock*1.3f+i)*.10f; ambientSnails[i].localPosition=p;
            }
            for(int i=0;i<groupRoots.Length;i++)
            {
                if(!groupRoots[i].gameObject.activeSelf||i==dragGroup)continue;
                groupRoots[i].localPosition=Vector3.Lerp(groupRoots[i].localPosition,groupTargets[i],1f-Mathf.Exp(-dt*12f));
                dropPunch[i]=Mathf.Max(0f,dropPunch[i]-dt*3.8f);
                float punch=Mathf.Sin(dropPunch[i]*Mathf.PI)*.15f;
                groupRoots[i].localScale=new Vector3(1f+punch,1f-punch*.45f,1f+punch);
                groupKeys[i].Rotate(0f,0f,(18f+i*3f)*dt);
            }
            if(shakeClock>0f){shakeClock-=dt;float a=shakeClock/.34f;cam.transform.position=camBase+new Vector3(Mathf.Sin(shakeClock*95f),0f,Mathf.Cos(shakeClock*83f))*.16f*a;}
            else cam.transform.position=camBase;
            feedbackClock=Mathf.Max(0f,feedbackClock-dt);
            UpdatePracticeGuide(); UpdateUiPositions();
        }

        void AnimateUi(float dt)
        {
            if(messagePlate.gameObject.activeSelf&&messageClock<900f){messageClock-=dt;if(messageClock<=0f)messagePlate.gameObject.SetActive(false);}
            if(ripple.gameObject.activeSelf){rippleClock-=dt;float t=1f-Mathf.Clamp01(rippleClock/.45f);ripple.localScale=Vector3.one*Mathf.Lerp(.35f,1.8f,t);var im=ripple.GetComponent<Image>();var c=im.color;c.a=(1f-t)*.72f;im.color=c;if(rippleClock<=0f)ripple.gameObject.SetActive(false);}
            if(uiPulse>0f){uiPulse=Mathf.Max(0f,uiPulse-dt*3f);float s=1f+Mathf.Sin(uiPulse*Mathf.PI)*.08f;goalText.rectTransform.localScale=Vector3.one*s;}
            titleLogo.rectTransform.localScale=Vector3.one*(1f+Mathf.Sin(titleClock*.8f)*.012f);
            titleCta.alpha=.76f+.24f*(.5f+.5f*Mathf.Sin(titleClock*2.1f));
        }

        bool UpdateLayoutIfNeeded(bool force=false)
        {
            bool land=Screen.width>=1024;
            if(!force&&land==lastLand)return false;
            lastLand=land; st.layoutLand=land;
            cam.orthographicSize=land?4.8f:12f;
            camBase=land?new Vector3(0f,10.2f,-16.2f):new Vector3(0f,14f,-18f);
            cam.transform.position=camBase; cam.transform.LookAt(new Vector3(0f,.2f,.5f));
            if(current!=null)ResetGroupTargets();
            return true;
        }

        Vector3 HomePosition(int i)
        {
            int n=current==null?4:current.groups.Length;
            if(lastLand){float spacing=n==3?5.5f:3.9f;return new Vector3((i-(n-1)*.5f)*spacing,.72f,-3.55f+(i%2)*.16f);}
            if(n==3)return new Vector3((i-1)*3.75f,.72f,-5.35f);
            return new Vector3((i%2-.5f)*5.6f,.72f,-4.15f-(i/2)*2.4f);
        }

        Vector3 SlotPosition(int i)
        {
            int n=current==null?4:current.groups.Length;
            if(lastLand){float spacing=n==3?5.5f:3.9f;return new Vector3((i-(n-1)*.5f)*spacing,.80f,1.05f);}
            return new Vector3(0f,.80f,3.7f-i*(n==4?1.72f:2.15f));
        }

        void ResetGroupTargets()
        {
            if(current==null)return;
            for(int i=0;i<current.groups.Length;i++)
            {
                groupTargets[i]=groupToSlot[i]>=0?SlotPosition(groupToSlot[i]):HomePosition(i);
                if(!groupRoots[i].gameObject.activeSelf)continue;
                if(groupRoots[i].localPosition==Vector3.zero||localPhase==LocalPhase.Practice||localPhase==LocalPhase.Playing)groupRoots[i].localPosition=groupTargets[i];
            }
        }

        void SetGroupTarget(int group,Vector3 target){groupTargets[group]=target;}
        void SetDraggedPosition(int group,Vector3 p){groupRoots[group].localPosition=p;groupRoots[group].localScale=new Vector3(1.08f,.92f,1.08f);}
        void SetGroupSelected(int group,bool selected){if(group>=0)groupRoots[group].localScale=selected?new Vector3(1.08f,.92f,1.08f):Vector3.one;}
        void TriggerDropJuice(int group){dropPunch[group]=1f;uiPulse=1f;}
        void WobbleGroup(int group){dropPunch[group]=.75f;}
        void WobbleSlot(int slot){slotBoards[slot].localRotation=Quaternion.Euler(0f,0f,6f);}

        int NearestGroup(Vector2 screen)
        {
            if(current==null)return-1;float best=Mathf.Max(90f,Screen.width*.14f),dist=best;int found=-1;
            for(int i=0;i<current.groups.Length;i++)
            {
                Vector3 sp=cam.WorldToScreenPoint(groupRoots[i].position);float d=Vector2.Distance(screen,new Vector2(sp.x,sp.y));
                if(d<dist){dist=d;found=i;}
            }
            return found;
        }

        int NearestSlot(Vector2 screen)
        {
            if(current==null)return-1;float dist=Mathf.Max(105f,Screen.width*.16f);int found=-1;
            for(int i=0;i<current.groups.Length;i++)
            {
                Vector3 sp=cam.WorldToScreenPoint(SlotPosition(i));float d=Vector2.Distance(screen,new Vector2(sp.x,sp.y));
                if(d<dist){dist=d;found=i;}
            }
            return found;
        }

        bool PointerWorld(out Vector3 p)
        {
            var plane=new Plane(Vector3.up,new Vector3(0f,.80f,0f));var ray=cam.ScreenPointToRay(MgfPointer.Position);float d;
            if(plane.Raycast(ray,out d)){p=ray.GetPoint(d);return true;}p=Vector3.zero;return false;
        }

        void UpdateUiPositions()
        {
            if(current==null||uiRoot==null)return;
            for(int i=0;i<current.groups.Length;i++)
            {
                Vector2 local;Vector3 sp=cam.WorldToScreenPoint(groupRoots[i].position+new Vector3(0f,1.6f,0f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot,new Vector2(sp.x,sp.y),null,out local);groupBoards[i].anchoredPosition=local;
                Vector3 ss=cam.WorldToScreenPoint(SlotPosition(i)+new Vector3(0f,.08f,0f));RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot,new Vector2(ss.x,ss.y),null,out local);slotBoards[i].anchoredPosition=local;
                slotBoards[i].localRotation=Quaternion.Lerp(slotBoards[i].localRotation,Quaternion.identity,.18f);
            }
        }

        void UpdatePracticeGuide()
        {
            if(localPhase!=LocalPhase.Practice||current==null||dragGroup>=0){if(guideTrail)guideTrail.enabled=false;if(guideFinger)guideFinger.gameObject.SetActive(false);return;}
            int next=-1;for(int slot=0;slot<current.groups.Length;slot++)if(slotToGroup[slot]<0){next=current.answerOrder[slot];break;}
            if(next<0)return;int target=Array.IndexOf(current.answerOrder,next);Vector3 a=groupRoots[next].localPosition+Vector3.up*.35f,b=SlotPosition(target)+Vector3.up*.35f;
            guideTrail.enabled=true;guideFinger.gameObject.SetActive(true);guideTrail.positionCount=18;
            for(int i=0;i<18;i++){float t=i/17f;Vector3 p=Vector3.Lerp(a,b,t);p.y+=Mathf.Sin(t*Mathf.PI)*.8f;guideTrail.SetPosition(i,p);}
            float travel=(idleClock*.55f)%1f;Vector3 fp=Vector3.Lerp(a,b,travel);fp.y+=Mathf.Sin(travel*Mathf.PI)*.8f;guideFinger.localPosition=fp;
            float pulse=1f+.22f*Mathf.Sin(idleClock*5f+guidePulse);guideFinger.localScale=Vector3.one*pulse;
        }

        void UpdateGuideTrail(int group,Vector3 world)
        {
            dragTrail.enabled=true;dragTrail.positionCount=12;Vector3 start=HomePosition(group);
            for(int i=0;i<12;i++){float t=i/11f;dragTrail.SetPosition(i,Vector3.Lerp(start,world,t));}
        }
        void HideDragTrail(){if(dragTrail)dragTrail.enabled=false;}
        void PulseGuide(){guidePulse+=2.4f;}

        void ShowRipple(Vector2 screen)
        {
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot,screen,null,out local);ripple.anchoredPosition=local;ripple.localScale=Vector3.one*.35f;
            var im=ripple.GetComponent<Image>();var c=im.color;c.a=.72f;im.color=c;rippleClock=.45f;ripple.gameObject.SetActive(true);
        }

        void SetTitleVisible(bool on){titleGroup.alpha=on?1f:0f;titleGroup.blocksRaycasts=false;titleRoot.gameObject.SetActive(on);}
        void SetGameVisible(bool on){gameGroup.alpha=on?1f:0f;gameGroup.blocksRaycasts=false;gameRoot.gameObject.SetActive(on);}
        void SetEndVisible(bool on){endGroup.alpha=on?1f:0f;endGroup.blocksRaycasts=false;endRoot.gameObject.SetActive(on);}

        LineRenderer NewLine(string name,Color color,float width,int count)
        {
            var go=new GameObject(name);go.transform.SetParent(worldRoot,false);var l=go.AddComponent<LineRenderer>();l.useWorldSpace=false;l.sharedMaterial=MgfLook.Additive(color);l.startColor=l.endColor=color;l.startWidth=l.endWidth=width;l.positionCount=count;l.numCapVertices=4;return l;
        }

        static string Join(int[] a){var s="";for(int i=0;i<a.Length;i++){if(i>0)s+=", ";s+=a[i];}return s;}

        GameObject NewMesh(string name,Mesh mesh,Material mat,Transform parent)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go;
        }

        static Mesh safeSphereMesh;
        GameObject SafeSphere(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent)
        {
            if(!safeSphereMesh)safeSphereMesh=UvSphere(16,10);
            var go=NewMesh(name,safeSphereMesh,mat,parent);go.transform.localPosition=pos;go.transform.localScale=scale;
            return go;
        }

        Mesh UvSphere(int lon,int lat)
        {
            var verts=new Vector3[(lon+1)*(lat+1)];var norms=new Vector3[verts.Length];var uv=new Vector2[verts.Length];var tris=new int[lon*lat*6];
            int vi=0;for(int y=0;y<=lat;y++){float v=(float)y/lat;float p=(v-.5f)*Mathf.PI;float cp=Mathf.Cos(p),sp=Mathf.Sin(p);for(int x=0;x<=lon;x++){float u=(float)x/lon;float a=u*Mathf.PI*2f;Vector3 n=new Vector3(Mathf.Cos(a)*cp,sp,Mathf.Sin(a)*cp);verts[vi]=n*.5f;norms[vi]=n;uv[vi++]=new Vector2(u,v);}}
            int ti=0;for(int y=0;y<lat;y++)for(int x=0;x<lon;x++){int a=y*(lon+1)+x,b=a+1,c=a+lon+1,d=c+1;tris[ti++]=a;tris[ti++]=c;tris[ti++]=b;tris[ti++]=b;tris[ti++]=c;tris[ti++]=d;}
            var m=new Mesh{name="Colliderless enamel sphere"};m.vertices=verts;m.normals=norms;m.uv=uv;m.triangles=tris;m.RecalculateBounds();return m;
        }

        Mesh TorusXZ(float radius,float tube,int major,int minor)
        {
            var verts=new Vector3[major*minor];var norms=new Vector3[verts.Length];var uv=new Vector2[verts.Length];var tris=new int[major*minor*6];
            for(int i=0;i<major;i++){float a=i*Mathf.PI*2f/major;Vector3 center=new Vector3(Mathf.Cos(a)*radius,0f,Mathf.Sin(a)*radius);
                for(int j=0;j<minor;j++){float b=j*Mathf.PI*2f/minor;Vector3 n=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(b),Mathf.Sin(a)*Mathf.Cos(b));int k=i*minor+j;verts[k]=center+n*tube;norms[k]=n;uv[k]=new Vector2((float)i/major,(float)j/minor);}}
            int ti=0;for(int i=0;i<major;i++)for(int j=0;j<minor;j++){int a=i*minor+j,b=i*minor+(j+1)%minor,c=((i+1)%major)*minor+j,d=((i+1)%major)*minor+(j+1)%minor;tris[ti++]=a;tris[ti++]=b;tris[ti++]=c;tris[ti++]=b;tris[ti++]=d;tris[ti++]=c;}
            var m=new Mesh{name="ContourTorus"};m.vertices=verts;m.normals=norms;m.uv=uv;m.triangles=tris;m.RecalculateBounds();return m;
        }

        RectTransform Full(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;return rt;
        }

        RectTransform Img(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,Color color,Sprite sprite)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=offset;rt.sizeDelta=size;var im=go.GetComponent<Image>();im.color=color;im.sprite=sprite;im.type=Image.Type.Sliced;im.raycastTarget=false;return rt;
        }

        TextMeshProUGUI Txt(string name,Transform parent,string text,Vector2 anchor,Vector2 offset,float size,Color color,float width)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=offset;rt.sizeDelta=new Vector2(width,Mathf.Max(44f,size*2.6f));var t=go.AddComponent<TextMeshProUGUI>();t.font=MgfText.Font;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;t.text=text;return t;
        }

        Sprite MakeRoundedSprite(int size,float radius,bool hollow)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name=hollow?"Ring plate":"Rounded plate",wrapMode=TextureWrapMode.Clamp};var px=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=Mathf.Max(Mathf.Abs(x-(size-1)*.5f)-(size*.5f-radius),0f);float dy=Mathf.Max(Mathf.Abs(y-(size-1)*.5f)-(size*.5f-radius),0f);float d=Mathf.Sqrt(dx*dx+dy*dy);byte a=(byte)(Mathf.Clamp01(radius+1f-d)*255f);if(hollow){float inner=Mathf.Max(0f,radius-5f);float ai=Mathf.Clamp01(inner+1f-d);a=(byte)(Mathf.Clamp01(a/255f-ai)*255f);}px[y*size+x]=new Color32(255,255,255,a);}
            tex.SetPixels32(px);tex.Apply(false,true);return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
        }
    }
}
