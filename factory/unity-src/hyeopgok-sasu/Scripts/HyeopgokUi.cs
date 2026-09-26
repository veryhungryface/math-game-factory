using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        readonly Color cream=MgfLook.Hex("#fff1ce"),navy=MgfLook.Hex("#142e35"),gold=MgfLook.Hex("#ffd05a");
        RectTransform commandStrip;
        RectTransform titleRoot,playRoot,endRoot,ctaRect,packRect,retryRect,questionPanel,feedbackPanel;
        RectTransform tutorialDot,tutorialTrail,tutorialLabel;
        TextMeshProUGUI titleInfo,hpText,coinsText,waveText,armyText,hintText,endTitle,endDetail,bonusText,pressureText;
        HyeopgokMathText questionText,feedbackText,bonusMath;
        HyeopgokMathText[] padLabels=new HyeopgokMathText[4];
        RectTransform[] padLabelRoots=new RectTransform[4];
        TextMeshProUGUI[] tutorialRings=new TextMeshProUGUI[4];
        TextMeshProUGUI answerRing;int answerMark=-1;
        Image hpFill,timeFill;
        float shownHp=100,bonusLife;
        string lastArmy="";int previousShownHp=-1;
        bool tutorialVisible;
        RectTransform Group(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        RectTransform Box(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,Color color){
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));var r=(RectTransform)go.transform;r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=offset;r.sizeDelta=size;go.GetComponent<Image>().color=color;go.GetComponent<Image>().raycastTarget=false;return r;
        }
        TextMeshProUGUI Text(string content,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,float font,Color color){
            var t=MgfText.Ui(content,anchor,offset,font,color,size.x);t.transform.SetParent(parent,false);t.rectTransform.sizeDelta=size;
            t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;t.raycastTarget=false;return t;
        }
        void BuildUi(){
            var canvas=MgfText.Canvas;
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=1;
            titleRoot=Group("Title",canvas.transform);playRoot=Group("Battle HUD",canvas.transform);endRoot=Group("Results",canvas.transform);
            var titleShadow=Text("협곡 사수",titleRoot,new Vector2(.5f,.83f),new Vector2(2,-5),new Vector2(380,100),61,navy);
            var title=Text("협곡 사수",titleRoot,new Vector2(.5f,.83f),Vector2.zero,new Vector2(380,100),61,cream);
            title.fontStyle=FontStyles.Bold;title.outlineColor=navy;title.outlineWidth=.18f;
            Text("화면을 눌러 출격 · 전투에서 왕을 답 패드로 끌기",titleRoot,new Vector2(.5f,.76f),Vector2.zero,new Vector2(390,42),17,cream);
            Box("title rule",titleRoot,new Vector2(.5f,.724f),Vector2.zero,new Vector2(46,3),gold);
            packRect=Box("Select problem pack",titleRoot,new Vector2(.5f,.22f),Vector2.zero,new Vector2(280,48),navy);
            titleInfo=Text("문제 팩을 불러오는 중…",packRect,new Vector2(.5f,.5f),Vector2.zero,new Vector2(268,44),17,cream);
            ctaRect=Box("Deploy shadow",titleRoot,new Vector2(.5f,.135f),new Vector2(0,-5),new Vector2(240,61),MgfLook.Hex("#805329"));
            var ctaFace=Box("Deploy",ctaRect,new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(240,61),gold);
            Text("출  격",ctaFace,new Vector2(.5f,.5f),Vector2.zero,new Vector2(220,54),27,navy);
            Text("어디든 누르면 바로 시작",titleRoot,new Vector2(.5f,.067f),Vector2.zero,new Vector2(370,30),15,cream);
            // Battle HUD: compact tactical strip, then a legible question parchment.
            Box("HUD backdrop",playRoot,new Vector2(.5f,1),new Vector2(0,-29),new Vector2(2000,58),navy);
            hpText=Text("성문 100",playRoot,new Vector2(.5f,1),new Vector2(-130,-24),new Vector2(100,32),16,cream);
            waveText=Text("1 / 10",playRoot,new Vector2(.5f,1),new Vector2(-35,-24),new Vector2(74,32),16,cream);
            coinsText=Text("정답 0 / 목표 7",playRoot,new Vector2(.5f,1),new Vector2(81,-24),new Vector2(158,32),15,gold);
            var hpBg=Box("Gate health",playRoot,new Vector2(.5f,1),new Vector2(-115,-46),new Vector2(110,4),MgfLook.Hex("#395054"));
            hpFill=Box("health remaining",hpBg,new Vector2(0,.5f),Vector2.zero,new Vector2(110,4),MgfLook.Hex("#66d69f")).GetComponent<Image>();hpFill.rectTransform.pivot=new Vector2(0,.5f);
            questionPanel=Box("Question",playRoot,new Vector2(.5f,1),new Vector2(0,-125),new Vector2(365,126),cream);
            Box("question gold edge",questionPanel,new Vector2(0,.5f),new Vector2(3,0),new Vector2(6,126),gold);
            var q=Text("",questionPanel,new Vector2(.5f,.5f),new Vector2(3,9),new Vector2(330,94),17,navy);q.alignment=TextAlignmentOptions.MidlineLeft;q.lineSpacing=5;
            questionText=new HyeopgokMathText(q);
            var timeBg=Box("Pressure clock",questionPanel,new Vector2(.5f,0),new Vector2(0,-3),new Vector2(365,4),MgfLook.Hex("#354e40"));
            timeFill=Box("Time remaining",timeBg,new Vector2(0,.5f),Vector2.zero,new Vector2(365,4),gold).GetComponent<Image>();timeFill.rectTransform.pivot=new Vector2(0,.5f);
            Box("Pressure begins at 18 seconds",timeBg,new Vector2(1-HyeopgokBattle.PressureGrace/HyeopgokRules.Limit,.5f),Vector2.zero,new Vector2(3,12),MgfLook.Hex("#ff895a"));
            pressureText=Text("성문 압박까지 18초",questionPanel,new Vector2(.5f,0),new Vector2(0,9),new Vector2(345,18),11,navy);
            armyText=Text("",playRoot,new Vector2(.5f,.035f),Vector2.zero,new Vector2(360,27),14,cream);armyText.outlineColor=navy;armyText.outlineWidth=.22f;
            commandStrip=Box("Command strip",playRoot,new Vector2(.5f,.093f),Vector2.zero,new Vector2(355,56),navy);
            hintText=Text("왕을 끌어 답 위에서 멈추세요\n0.8초 서 있으면 확정",playRoot,new Vector2(.5f,.093f),Vector2.zero,new Vector2(340,53),15,cream);
            feedbackPanel=Box("Answer explanation",playRoot,new Vector2(.5f,1),new Vector2(0,-125),new Vector2(365,126),navy);
            var ft=Text("",feedbackPanel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(340,100),16,cream);ft.lineSpacing=8;feedbackText=new HyeopgokMathText(ft);feedbackPanel.gameObject.SetActive(false);
            bonusText=Text("",playRoot,new Vector2(.3f,.64f),Vector2.zero,new Vector2(230,54),21,gold);bonusText.outlineColor=navy;bonusText.outlineWidth=.2f;bonusMath=new HyeopgokMathText(bonusText);
            for(int i=0;i<4;i++){
                var p=Group("Pad answer "+i,playRoot);p.anchorMin=p.anchorMax=new Vector2(.5f,.5f);p.sizeDelta=new Vector2(68,52);padLabelRoots[i]=p;
                var pt=Text("",p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(66,52),24,cream);pt.outlineColor=navy;pt.outlineWidth=.16f;padLabels[i]=new HyeopgokMathText(pt);
            }
            tutorialTrail=Box("First drag route",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,5),gold);
            tutorialDot=Box("Drag ghost",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(21,21),gold);tutorialDot.localRotation=Quaternion.Euler(0,0,45);
            tutorialLabel=Text("끌기",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(68,28),16,cream).rectTransform;
            for(int i=0;i<4;i++){
                tutorialRings[i]=Text("○",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(88,88),57,gold);
                tutorialRings[i].outlineColor=navy;tutorialRings[i].outlineWidth=.14f;
            }
            SetTutorial(false);
            answerRing=Text("○",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(96,96),64,gold);answerRing.outlineColor=navy;answerRing.outlineWidth=.14f;answerRing.gameObject.SetActive(false);
            var resultCard=Box("Results banner",endRoot,new Vector2(.5f,.53f),Vector2.zero,new Vector2(352,312),navy);
            Box("Result edge",resultCard,new Vector2(.5f,1),Vector2.zero,new Vector2(352,5),gold);
            endTitle=Text("",resultCard,new Vector2(.5f,.79f),Vector2.zero,new Vector2(332,70),38,cream);
            endDetail=Text("",resultCard,new Vector2(.5f,.43f),Vector2.zero,new Vector2(326,124),19,cream);
            retryRect=Box("Retry",resultCard,new Vector2(.5f,.09f),Vector2.zero,new Vector2(252,56),gold);
            Text("다시 출격",retryRect,new Vector2(.5f,.5f),Vector2.zero,new Vector2(240,50),23,navy);
            playRoot.gameObject.SetActive(false);endRoot.gameObject.SetActive(false);
            LayoutUi();
        }
        void SetTitleInfo(string s){titleInfo.text=s+(loaded?"  >":"");}
        void TitleInput(){
            if(Hit(packRect)&&loaded&&!loading){MgfSfx.Play("tap");StartCoroutine(LoadPack((packAt+1)%index.packs.Length));}
            else if(loaded)TestStart();
            else if(!loaded&&!loading)StartCoroutine(BootPacks());
        }
        bool Hit(RectTransform rt)=>RectTransformUtility.RectangleContainsScreenPoint(rt,MgfPointer.Position);
        void ShowPlaying(){titleRoot.gameObject.SetActive(false);endRoot.gameObject.SetActive(false);playRoot.gameObject.SetActive(true);shownHp=100;HideFeedback();}
        void SetQuestion(PackItem p){
            for(int i=0;i<4;i++)cracks[i].gameObject.SetActive(false);
            questionText.Text.fontSize=p.prompt.Length>100?15:p.prompt.Length>75?16:17;
            questionText.Set(p.prompt);
            hintText.text=Rules.Wave==1?"왕을 끌어 답 위에서 멈추세요\n0.8초 서 있으면 확정":"정답 패드로 이동 · 0.8초 정지\n10문제 중 7문제 이상 맞히면 승리";
            SetTutorial(Rules.Wave==1&&st.moves==0);
        }
        void SetChoices(string[] choices){for(int i=0;i<4;i++){
            padLabelRoots[i].gameObject.SetActive(true);
            padLabels[i].Text.fontSize=choices[i].Contains("{frac:")||choices[i].Length<=3?24:choices[i].Length<=5?18:13;
            padLabels[i].Set(choices[i]);
        }}
        void RefreshHud(){
            waveText.text=Rules.Wave+" / 10";
            bool reachable=Rules.Correct+(10-Rules.Attempts)>=7;
            coinsText.text=reachable?"정답 "+Rules.Correct+" / 목표 7":"목표: 성문 방어";
            coinsText.color=reachable?gold:MgfLook.Hex("#ffb27c");
            if(!reachable&&!Rules.Ended)hintText.text="이번 판은 버티기\n남은 문제로 성문을 지켜라";
        }
        void UpdateBattleHud(){
            string s="적 "+battle.Reds+"     수비대 "+battle.Blues+"     격파 "+battle.Kills;
            if(s!=lastArmy){lastArmy=s;armyText.text=s;}
        }
        void ShowFeedback(bool ok,string s){
            feedbackPanel.gameObject.SetActive(true);feedbackText.Text.color=ok?cream:MgfLook.Hex("#ffb7a2");feedbackText.Set(s);
            if(ok){bonusMath.Set(rewardIsRatio?Rules.Current.answer+" 명중":Rules.Current.format=="frac"?"판단 적중":"경우의 수 지원");bonusLife=2.2f;}
            // After a miss, mark where the correct answer stood so the explanation line maps to a pad.
            answerMark=ok?-1:Rules.AnswerPad();answerRing.gameObject.SetActive(answerMark>=0);
            if(Rules.LastPad>=0)padLabelRoots[Rules.LastPad].localScale=Vector3.one*(ok?1.3f:.7f);
            if(!ok&&Rules.LastPad>=0)cracks[Rules.LastPad].gameObject.SetActive(true);
        }
        void HideFeedback(){feedbackPanel.gameObject.SetActive(false);bonusText.text="";answerMark=-1;if(answerRing)answerRing.gameObject.SetActive(false);}
        void ShowEnd(bool won,bool fallen){
            endRoot.gameObject.SetActive(true);playRoot.gameObject.SetActive(false);
            endTitle.text=won?"협곡을 지켰다":fallen?"성문이 무너졌다":"버티기만 한 판";
            string next=won?"판단이 전선을 바꿨습니다.":Rules.Correct>=7?"정답 목표는 달성했습니다. 다음 판에는 성문까지 지켜 보세요.":"목표까지 정답 "+(7-Rules.Correct)+"개가 더 필요합니다.";
            endDetail.text="첫 시도 정답  "+Rules.Correct+" / "+Rules.Attempts+"\n"+"점수  "+Rules.Score+"   ·   격파  "+battle.Kills+"\n\n"+next;
        }
        void SetTutorial(bool visible){
            tutorialVisible=visible;
            if(tutorialTrail)tutorialTrail.gameObject.SetActive(visible);
            if(tutorialDot)tutorialDot.gameObject.SetActive(visible);
            if(tutorialLabel)tutorialLabel.gameObject.SetActive(visible);
            for(int i=0;i<tutorialRings.Length;i++)if(tutorialRings[i])tutorialRings[i].gameObject.SetActive(visible);
        }
        void HideTutorial(){if(tutorialVisible)SetTutorial(false);}
        void LayoutUi(){
            if(!questionPanel)return;
            // CanvasScaler has not necessarily run at Awake/first Update. Its
            // height-matched logical width is stable across device pixel ratios.
            float cw=844f*Screen.width/Mathf.Max(1,Screen.height);
            bool wide=(float)Screen.width/Screen.height>1.2f;
            float width=wide?300:Mathf.Min(cw-26,480);
            Vector2 anchor=wide?new Vector2(.16f,.76f):new Vector2(.5f,1);
            Vector2 offset=wide?Vector2.zero:new Vector2(0,-125);
            questionPanel.anchorMin=questionPanel.anchorMax=feedbackPanel.anchorMin=feedbackPanel.anchorMax=anchor;
            questionPanel.anchoredPosition=feedbackPanel.anchoredPosition=offset;
            feedbackPanel.sizeDelta=new Vector2(width,126);feedbackText.Text.rectTransform.sizeDelta=new Vector2(width-28,113);
            ((RectTransform)timeFill.transform.parent).sizeDelta=new Vector2(width,4);
            Vector2 hintAnchor=wide?new Vector2(.16f,.53f):new Vector2(.5f,.093f);
            commandStrip.anchorMin=commandStrip.anchorMax=hintText.rectTransform.anchorMin=hintText.rectTransform.anchorMax=hintAnchor;
            commandStrip.sizeDelta=new Vector2(wide?300:355,56);hintText.rectTransform.sizeDelta=new Vector2(wide?285:340,53);
            questionPanel.sizeDelta=new Vector2(width,126);questionText.Text.rectTransform.sizeDelta=new Vector2(width-33,94);pressureText.rectTransform.sizeDelta=new Vector2(width-20,18);
            if(Rules.Current!=null)questionText.Set(Rules.Current.prompt);
            if(feedbackPanel.gameObject.activeSelf)feedbackText.Set(feedback);
        }
        void AnimateUi(float dt){
            if(!loaded)return;
            ctaRect.localScale=Vector3.Lerp(ctaRect.localScale,Vector3.one,dt*10);
            if(!playStarted)return;
            shownHp=Mathf.MoveTowards(shownHp,Rules.Hp,dt*70);
            int h=Mathf.RoundToInt(shownHp);
            if(h!=previousShownHp){hpText.text="성문 "+h;previousShownHp=h;}
            hpFill.rectTransform.sizeDelta=new Vector2(110*shownHp/100,4);
            timeFill.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x*Mathf.Clamp01(1-Rules.Elapsed/HyeopgokRules.Limit),4);
            pressureText.text=Rules.Elapsed<HyeopgokBattle.PressureGrace?"성문 압박까지 "+Mathf.CeilToInt(HyeopgokBattle.PressureGrace-Rules.Elapsed)+"초":"성문 압박 · 4초마다 -3";
            RectTransform canvas=(RectTransform)MgfText.Canvas.transform;
            for(int i=0;i<4;i++){
                Vector3 p=cam.WorldToScreenPoint(HyeopgokRules.Pads[i]+new Vector3(0,.05f,0));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,p,null,out Vector2 local);padLabelRoots[i].anchoredPosition=local;
                padLabelRoots[i].localScale=Vector3.Lerp(padLabelRoots[i].localScale,Vector3.one,dt*8);
                float progress=Rules.Hover==i?Rules.Dwell/HyeopgokRules.Hold:0;
                int count=progress>0?Mathf.CeilToInt(progress*32)+1:0;padFill[i].positionCount=count;
                for(int k=0;k<count;k++)padFill[i].SetPosition(k,PadEdge(i,Mathf.Min(progress,k/32f)));
            }
            AnimateTutorial();
            if(answerMark>=0){
                Vector3 rs=cam.WorldToScreenPoint(HyeopgokRules.Pads[answerMark]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,rs,null,out Vector2 at);
                answerRing.rectTransform.anchoredPosition=at;answerRing.rectTransform.localScale=Vector3.one*(1.05f+.08f*Mathf.Sin(Time.unscaledTime*7));
                padLabelRoots[answerMark].localScale=Vector3.one*1.15f;
            }
            if(bonusLife>0){bonusLife-=dt;bonusText.rectTransform.anchoredPosition=new Vector2(0,(2.2f-bonusLife)*18);if(bonusLife<=0)bonusText.text="";}
        }
        void AnimateTutorial(){
            if(!tutorialVisible)return;
            RectTransform canvas=(RectTransform)MgfText.Canvas.transform;
            Vector3 ks=cam.WorldToScreenPoint(Rules.King);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,ks,null,out Vector2 start);
            Vector3 padCenter=(HyeopgokRules.Pads[0]+HyeopgokRules.Pads[1]+HyeopgokRules.Pads[2]+HyeopgokRules.Pads[3])*.25f;
            Vector3 ps=cam.WorldToScreenPoint(padCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,ps,null,out Vector2 target);
            float p=Mathf.SmoothStep(0,1,Mathf.PingPong(Time.unscaledTime*1.15f,1));
            Vector2 dot=Vector2.Lerp(start,target,p),delta=target-start;
            tutorialDot.anchoredPosition=dot;tutorialLabel.anchoredPosition=dot+new Vector2(0,27);
            tutorialTrail.anchoredPosition=(start+target)*.5f;tutorialTrail.sizeDelta=new Vector2(delta.magnitude,5);
            tutorialTrail.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            for(int i=0;i<4;i++){
                Vector3 rs=cam.WorldToScreenPoint(HyeopgokRules.Pads[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,rs,null,out Vector2 local);
                tutorialRings[i].rectTransform.anchoredPosition=local;
                tutorialRings[i].rectTransform.localScale=Vector3.one*(1.0f+.12f*Mathf.Sin(Time.unscaledTime*6+i*.7f));
            }
        }
    }
}
