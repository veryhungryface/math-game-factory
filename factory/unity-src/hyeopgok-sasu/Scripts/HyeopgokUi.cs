using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        readonly Color cream=MgfLook.Hex("#fff1ce"),navy=MgfLook.Hex("#142e35"),gold=MgfLook.Hex("#ffd05a");
        RectTransform commandStrip,pressureMarker,treasuryPanel;
        RectTransform titleRoot,playRoot,endRoot,ctaRect,packRect,retryRect,questionPanel,feedbackPanel;
        RectTransform tutorialDot,tutorialTrail,tutorialLabel;
        TextMeshProUGUI titleInfo,hpText,coinsText,waveText,armyText,hintText,endTitle,endDetail,bonusText,pressureText,treasuryText,depositText;
        HyeopgokMathText questionText,feedbackText,bonusMath,assembledMath;
        TextMeshProUGUI assembledText,confirmText;
        readonly TextMeshProUGUI[] padNames=new TextMeshProUGUI[4];
        readonly int[] lastPoured={-1,-1};
        int lastAssembledDen=-1,lastAssembledNum=-1;
        HyeopgokMathText[] padLabels=new HyeopgokMathText[4];
        RectTransform[] padLabelRoots=new RectTransform[4];
        TextMeshProUGUI[] tutorialRings=new TextMeshProUGUI[4];
        TextMeshProUGUI answerRing;int answerMark=-1;
        Image hpFill,timeFill;
        float shownHp=100,bonusLife;
        int previousShownHp=-1,lastReds=-1,lastBlues=-1,lastKills=-1,lastClock=-1,lastTreasury=-1;
        bool tutorialVisible;
        bool IsCoinIntro=>Rules.Wave==1&&Rules.Current!=null&&Rules.Current.Mode=="amount"&&Rules.Current.answerValue==2&&(Rules.Current.id=="m2s2-u6-001"||Rules.Current.id=="m2s2-u7-001");
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
            Text("처치해 코인을 모으고 · 답만큼 부어 건설하라",titleRoot,new Vector2(.5f,.76f),Vector2.zero,new Vector2(390,42),17,cream);
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
            var q=Text("",questionPanel,new Vector2(.5f,.5f),new Vector2(3,14),new Vector2(330,85),17,navy);q.alignment=TextAlignmentOptions.MidlineLeft;q.lineSpacing=5;
            questionText=new HyeopgokMathText(q);
            var timeBg=Box("Pressure clock",questionPanel,new Vector2(.5f,0),new Vector2(0,-3),new Vector2(365,4),MgfLook.Hex("#354e40"));
            timeFill=Box("Time remaining",timeBg,new Vector2(0,.5f),Vector2.zero,new Vector2(365,4),gold).GetComponent<Image>();timeFill.rectTransform.pivot=new Vector2(0,.5f);
            pressureMarker=Box("Pressure begins at 18 seconds",timeBg,new Vector2(1-HyeopgokBattle.PressureGrace/HyeopgokRules.Limit,.5f),Vector2.zero,new Vector2(3,12),MgfLook.Hex("#ff895a"));
            pressureText=Text("24초가 지나면 오답 · 남은 24초\n성문 압박까지 18초",questionPanel,new Vector2(.5f,0),new Vector2(0,15),new Vector2(345,29),10.5f,navy);
            treasuryPanel=Box("Battle coins",playRoot,new Vector2(1,1),new Vector2(-67,-221),new Vector2(108,34),navy);
            treasuryText=Text("코인 0",treasuryPanel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(102,30),17,gold);
            depositText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(140,25),12,gold);depositText.outlineColor=navy;depositText.outlineWidth=.24f;
            depositText.gameObject.SetActive(false);
            armyText=Text("",playRoot,new Vector2(.5f,.035f),Vector2.zero,new Vector2(360,27),14,cream);armyText.outlineColor=navy;armyText.outlineWidth=.22f;
            commandStrip=Box("Command strip",playRoot,new Vector2(.5f,.093f),Vector2.zero,new Vector2(355,56),navy);
            hintText=Text("패드에 답만큼 붓고 밖으로 이동\n탭은 1닢 · 길게 서면 빠르게",playRoot,new Vector2(.5f,.093f),Vector2.zero,new Vector2(340,53),15,cream);
            feedbackPanel=Box("Answer explanation",playRoot,new Vector2(.5f,1),new Vector2(0,-125),new Vector2(365,126),navy);
            var ft=Text("",feedbackPanel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(340,100),16,cream);ft.lineSpacing=8;feedbackText=new HyeopgokMathText(ft);feedbackPanel.gameObject.SetActive(false);
            bonusText=Text("",playRoot,new Vector2(.3f,.64f),Vector2.zero,new Vector2(230,54),21,gold);bonusText.outlineColor=navy;bonusText.outlineWidth=.2f;bonusMath=new HyeopgokMathText(bonusText);
            for(int i=0;i<4;i++){
                var p=Group("Pad answer "+i,playRoot);p.anchorMin=p.anchorMax=new Vector2(.5f,.5f);p.sizeDelta=new Vector2(68,52);padLabelRoots[i]=p;
                padNames[i]=Text("",p,new Vector2(.5f,.5f),new Vector2(0,38),new Vector2(145,41),13,cream);padNames[i].outlineColor=navy;padNames[i].outlineWidth=.26f;
                var pt=Text("",p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(66,52),24,cream);pt.outlineColor=navy;pt.outlineWidth=.16f;padLabels[i]=new HyeopgokMathText(pt);
            }
            assembledText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(140,100),32,gold);assembledText.outlineColor=navy;assembledText.outlineWidth=.22f;assembledMath=new HyeopgokMathText(assembledText);
            confirmText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(230,44),17,gold);confirmText.outlineColor=navy;confirmText.outlineWidth=.25f;
            tutorialTrail=Box("First drag route",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,5),gold);
            tutorialDot=Box("Drag ghost",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(21,20),cream);tutorialDot.localRotation=Quaternion.Euler(0,0,-24);
            Box("Ghost index finger",tutorialDot,new Vector2(.5f,.5f),new Vector2(5,15),new Vector2(7,24),cream);
            Box("Ghost thumb",tutorialDot,new Vector2(.5f,.5f),new Vector2(-13,2),new Vector2(9,9),cream);
            Box("Ghost wrist",tutorialDot,new Vector2(.5f,.5f),new Vector2(2,-14),new Vector2(15,11),gold);
            tutorialLabel=Text("두 번 탭",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(108,28),16,cream).rectTransform;
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
            lastClock=-1;
            pressureMarker.anchorMin=pressureMarker.anchorMax=new Vector2(1-battle.QuestionGrace/Rules.TimeLimit,.5f);
            for(int i=0;i<4;i++)cracks[i].gameObject.SetActive(false);
            lastPoured[0]=lastPoured[1]=-1;lastAssembledDen=lastAssembledNum=-1;assembledMath.Set("");confirmText.text="";
            LayoutUi();
            hintText.text=Rules.Current.Mode=="choice"?"정답 패드 위에 0.8초 서기\n10문제 중 7문제 이상 맞히면 승리":IsCoinIntro?"처치 코인 → 등에 쌓기 → 2닢 붓기\n패드 두 번 탭 후 밖으로 이동":Rules.Current.Mode=="fraction_parts"?"두 패드에 각각 답만큼 붓기\n두 곳을 채우고 밖으로 나와 확정":"탭 = 1닢 · 길게 서면 빠르게\n답만큼 붓고 밖으로 · 0은 지나가기";
            SetTutorial(Rules.Wave==1&&st.moves==0);
        }
        void FitQuestion(string prompt){
            bool wide=(float)Screen.width/Screen.height>1.2f;
            float font=wide?26:17;
            questionText.Text.fontSize=font;questionText.Set(prompt);
            float textHeight=Mathf.Max(wide?98:70,questionText.Text.preferredHeight+10);
            float height=textHeight+(wide?67:49);
            questionPanel.sizeDelta=new Vector2(questionPanel.sizeDelta.x,height);
            questionText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-30,textHeight);
            questionText.Text.rectTransform.anchoredPosition=new Vector2(3,wide?23:18);
            ((RectTransform)questionPanel.GetChild(0)).sizeDelta=new Vector2(6,height);
            if(!wide)questionPanel.anchoredPosition=new Vector2(0,-64-height*.5f);
            feedbackPanel.anchoredPosition=questionPanel.anchoredPosition;
            feedbackPanel.sizeDelta=new Vector2(questionPanel.sizeDelta.x,height+26);
            feedbackText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-28,height+10);
            feedbackText.Text.fontSize=wide?25:17;
            questionText.Set(prompt);
        }
        void SetChoices(string[] choices){
            for(int i=0;i<4;i++){
                bool active=i<Rules.PadCount;padLabelRoots[i].gameObject.SetActive(active);if(!active)continue;
                if(Rules.Current.Mode=="choice"){
                    padNames[i].text="";padLabels[i].Text.fontSize=choices[i].Contains("{frac:")||choices[i].Length<=3?24:choices[i].Length<=5?18:13;padLabels[i].Set(choices[i]);
                }else{
                    padNames[i].text=Rules.PadCount==1?"답만큼 붓기":i==0?Rules.Current.den_label:Rules.Current.num_label;
                    padLabels[i].Text.fontSize=30;padLabels[i].Set("0");
                }
            }
            RefreshPadAmounts();
        }
        void RefreshPadAmounts(){
            if(Rules.Current==null||Rules.Current.Mode=="choice"||padLabels[0]==null)return;
            for(int i=0;i<Rules.PadCount;i++)if(lastPoured[i]!=Rules.Poured[i]){
                lastPoured[i]=Rules.Poured[i];padLabels[i].Set(Rules.Poured[i].ToString());padLabelRoots[i].localScale=Vector3.one*1.17f;
            }
            bool show=Rules.PadCount==2&&Rules.Visited[0]&&Rules.Visited[1]&&(Rules.Confirming||Rules.Pending);
            assembledText.gameObject.SetActive(show);
            if(show&&(lastAssembledDen!=Rules.Poured[0]||lastAssembledNum!=Rules.Poured[1])){
                lastAssembledDen=Rules.Poured[0];lastAssembledNum=Rules.Poured[1];assembledMath.Set("{frac:"+Rules.Poured[1]+"/"+Rules.Poured[0]+"}");
            }
        }
        void RefreshHud(){
            waveText.text=Rules.Wave+" / 10";
            bool reachable=Rules.Correct+(10-Rules.Attempts)>=7;
            coinsText.text=reachable?"정답 "+Rules.Correct+" / 목표 7":"목표: 성문 방어";
            coinsText.color=reachable?gold:MgfLook.Hex("#ffb27c");
            if(!reachable&&!Rules.Ended)hintText.text="이번 판은 버티기\n남은 문제로 성문을 지켜라";
        }
        void UpdateBattleHud(){
            if(lastReds!=battle.Reds||lastBlues!=battle.Blues||lastKills!=battle.Kills){
                lastReds=battle.Reds;lastBlues=battle.Blues;lastKills=battle.Kills;
                armyText.SetText("적 {0}     수비대 {1}     격파 {2}",lastReds,lastBlues,lastKills);
            }
        }
        void ShowFeedback(bool ok,string s){
            feedbackPanel.gameObject.SetActive(true);feedbackText.Text.color=ok?cream:MgfLook.Hex("#ffb7a2");feedbackText.Set(s);
            float needed=feedbackText.Text.preferredHeight+25;
            if(needed>feedbackPanel.sizeDelta.y){
                float top=feedbackPanel.anchoredPosition.y+feedbackPanel.sizeDelta.y*.5f;
                feedbackPanel.sizeDelta=new Vector2(feedbackPanel.sizeDelta.x,needed);
                feedbackText.Text.rectTransform.sizeDelta=new Vector2(feedbackText.Text.rectTransform.sizeDelta.x,needed-16);
                feedbackPanel.anchoredPosition=new Vector2(feedbackPanel.anchoredPosition.x,top-needed*.5f);feedbackText.Set(s);
            }
            if(ok){bonusMath.Set(Rules.Current.Mode=="choice"?"지원군 출격":"+"+Rules.TotalPoured+"닢 투자");bonusLife=2.2f;}
            // After a miss, mark where the correct answer stood so the explanation line maps to a pad.
            answerMark=ok?-1:Rules.AnswerPad();answerRing.gameObject.SetActive(answerMark>=0);
            if(Rules.LastPad>=0)padLabelRoots[Rules.LastPad].localScale=Vector3.one*(ok?1.3f:.7f);
            if(!ok&&Rules.LastPad>=0)cracks[Rules.LastPad].gameObject.SetActive(true);
            if(!ok&&Rules.Current.Mode=="fraction_parts")for(int i=0;i<2;i++)cracks[i].gameObject.SetActive(true);
            RefreshPadAmounts();
        }
        void HideFeedback(){feedbackPanel.gameObject.SetActive(false);bonusLife=0;bonusMath.Set("");answerMark=-1;if(answerRing)answerRing.gameObject.SetActive(false);}
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
            for(int i=0;i<tutorialRings.Length;i++)if(tutorialRings[i])tutorialRings[i].gameObject.SetActive(visible&&i<Rules.PadCount);
        }
        void HideTutorial(){if(tutorialVisible)SetTutorial(false);}
        void LayoutUi(){
            if(!questionPanel)return;
            // CanvasScaler has not necessarily run at Awake/first Update. Its
            // height-matched logical width is stable across device pixel ratios.
            float cw=844f*Screen.width/Mathf.Max(1,Screen.height);
            bool wide=(float)Screen.width/Screen.height>1.2f;
            float width=wide?430:Mathf.Min(cw-26,480);
            Vector2 anchor=wide?new Vector2(.17f,.71f):new Vector2(.5f,1);
            Vector2 offset=wide?Vector2.zero:new Vector2(0,-125);
            questionPanel.anchorMin=questionPanel.anchorMax=feedbackPanel.anchorMin=feedbackPanel.anchorMax=anchor;
            questionPanel.anchoredPosition=feedbackPanel.anchoredPosition=offset;
            feedbackPanel.sizeDelta=new Vector2(width,126);feedbackText.Text.rectTransform.sizeDelta=new Vector2(width-28,113);
            ((RectTransform)timeFill.transform.parent).sizeDelta=new Vector2(width,4);
            Vector2 hintAnchor=wide?new Vector2(.17f,.29f):new Vector2(.5f,.093f);
            commandStrip.anchorMin=commandStrip.anchorMax=hintText.rectTransform.anchorMin=hintText.rectTransform.anchorMax=hintAnchor;
            commandStrip.sizeDelta=new Vector2(wide?430:355,wide?94:60);hintText.rectTransform.sizeDelta=new Vector2(wide?406:340,wide?90:58);
            hintText.fontSize=wide?23:14;
            pressureText.fontSize=wide?16:10.5f;pressureText.rectTransform.anchoredPosition=new Vector2(0,wide?24:15);
            treasuryText.fontSize=wide?23:17;treasuryPanel.sizeDelta=new Vector2(wide?155:108,wide?44:34);treasuryText.rectTransform.sizeDelta=new Vector2(wide?147:102,wide?40:30);
            hpText.fontSize=waveText.fontSize=wide?21:16;coinsText.fontSize=wide?20:15;armyText.fontSize=wide?18:14;
            questionPanel.sizeDelta=new Vector2(width,126);questionText.Text.rectTransform.sizeDelta=new Vector2(width-33,85);pressureText.rectTransform.sizeDelta=new Vector2(width-20,wide?44:29);
            treasuryPanel.anchorMin=treasuryPanel.anchorMax=wide?new Vector2(1,1):new Vector2(0,.175f);
            treasuryPanel.anchoredPosition=wide?new Vector2(-92,-84):new Vector2(67,0);
            if(Rules.Current!=null)FitQuestion(Rules.Current.prompt);
            if(feedbackPanel.gameObject.activeSelf)feedbackText.Set(feedback);
        }
        void AnimateUi(float dt){
            if(!loaded)return;
            ctaRect.localScale=Vector3.Lerp(ctaRect.localScale,Vector3.one,dt*10);
            if(!playStarted)return;
            shownHp=Mathf.MoveTowards(shownHp,Rules.Hp,dt*70);
            int h=Mathf.RoundToInt(shownHp);
            if(h!=previousShownHp){hpText.SetText("성문 {0}",h);previousShownHp=h;}
            hpFill.rectTransform.sizeDelta=new Vector2(110*shownHp/100,4);
            timeFill.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x*Mathf.Clamp01(1-Rules.Elapsed/Rules.TimeLimit),4);
            int clock=Mathf.CeilToInt(Mathf.Max(0,Rules.TimeLimit-Rules.Elapsed));
            if(clock!=lastClock){
                lastClock=clock;
                if(Rules.Elapsed<battle.QuestionGrace)pressureText.SetText("{0}초가 지나면 오답 · 남은 {1}초\n성문 압박까지 {2}초",Rules.TimeLimit,clock,Mathf.CeilToInt(battle.QuestionGrace-Rules.Elapsed));
                else pressureText.SetText("{0}초가 지나면 오답 · 남은 {1}초\n성문 압박 · 4초마다 -3",Rules.TimeLimit,clock);
            }
            if(lastTreasury!=battle.Coins){lastTreasury=battle.Coins;treasuryText.SetText("코인 {0}",lastTreasury);}
            bool pouring=battle.Depositing&&Rules.Hover>=0;
            if(depositText.gameObject.activeSelf!=pouring)depositText.gameObject.SetActive(pouring);
            RectTransform canvas=(RectTransform)MgfText.Canvas.transform;
            if(pouring){
                depositText.text="-1닢 → 답";
                Vector3 ds=cam.WorldToScreenPoint(HyeopgokRules.Pads[Rules.Hover]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,ds,null,out Vector2 dat);depositText.rectTransform.anchoredPosition=dat+new Vector2(0,-36);
            }
            RefreshPadAmounts();
            for(int i=0;i<4;i++){
                if(i>=Rules.PadCount)continue;
                Vector3 p=cam.WorldToScreenPoint(HyeopgokRules.Pads[i]+new Vector3(0,.05f,0));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,p,null,out Vector2 local);padLabelRoots[i].anchoredPosition=local;
                padLabelRoots[i].localScale=Vector3.Lerp(padLabelRoots[i].localScale,Vector3.one,dt*8);
                // Pack labels sit to opposite sides in fraction mode, leaving the
                // middle free for the assembled vertical fraction.
                if(Rules.PadCount==2)padNames[i].rectTransform.anchoredPosition=new Vector2(i==0?-27:27,43);
                else padNames[i].rectTransform.anchoredPosition=new Vector2(0,38);
                float progress=Rules.Confirming?Rules.Confirm/HyeopgokRules.ConfirmTime:Rules.Current.Mode=="choice"&&Rules.Hover==i?Rules.Dwell/HyeopgokRules.Hold:0;
                int count=progress>0?Mathf.CeilToInt(progress*32)+1:0;padFill[i].positionCount=count;
                for(int k=0;k<count;k++)padFill[i].SetPosition(k,PadEdge(i,Mathf.Min(progress,k/32f)));
            }
            Vector3 mid=Rules.PadCount==2?(HyeopgokRules.Pads[0]+HyeopgokRules.Pads[1])*.5f:HyeopgokRules.Pads[0];
            Vector3 middle=cam.WorldToScreenPoint(mid);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,middle,null,out Vector2 center);
            assembledText.rectTransform.anchoredPosition=center+new Vector2(0,-6);
            confirmText.rectTransform.anchoredPosition=center+new Vector2(0,-72);
            confirmText.text=Rules.Confirming?"이 양으로? · 다시 올라서면 추가":!Rules.Pending&&Rules.Hover>=0&&Rules.Coins==0?"코인이 부족해요 · 전투에서 모으기":"";
            AnimateTutorial();
            if(answerMark>=0){
                Vector3 rs=cam.WorldToScreenPoint(HyeopgokRules.Pads[answerMark]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,rs,null,out Vector2 at);
                answerRing.rectTransform.anchoredPosition=at;answerRing.rectTransform.localScale=Vector3.one*(1.05f+.08f*Mathf.Sin(Time.unscaledTime*7));
                padLabelRoots[answerMark].localScale=Vector3.one*1.15f;
            }
            if(bonusLife>0){bonusLife-=dt;bonusText.rectTransform.anchoredPosition=new Vector2(0,(2.2f-bonusLife)*18);if(bonusLife<=0)bonusMath.Set("");}
        }
        void AnimateTutorial(){
            if(!tutorialVisible)return;
            if(!IsCoinIntro&&Rules.PourCount>0){HideTutorial();return;}
            RectTransform canvas=(RectTransform)MgfText.Canvas.transform;
            Vector3 ks=cam.WorldToScreenPoint(Rules.King);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,ks,null,out Vector2 start);
            Vector3 padCenter=Rules.Current.Mode=="choice"?(HyeopgokRules.Pads[0]+HyeopgokRules.Pads[1]+HyeopgokRules.Pads[2]+HyeopgokRules.Pads[3])*.25f:IsCoinIntro&&Rules.Poured[0]>=2?HyeopgokRules.Exit:HyeopgokRules.Pads[0];
            tutorialLabel.GetComponent<TextMeshProUGUI>().text=Rules.Current.Mode=="choice"?"끌어 멈추기":IsCoinIntro?(Rules.Poured[0]>=2?"밖으로":"두 번 탭"):"답만큼 붓기";
            Vector3 ps=cam.WorldToScreenPoint(padCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,ps,null,out Vector2 target);
            float p=Mathf.SmoothStep(0,1,Mathf.PingPong(Time.unscaledTime*1.15f,1));
            Vector2 dot=Vector2.Lerp(start,target,p),delta=target-start;
            tutorialDot.anchoredPosition=dot;tutorialLabel.anchoredPosition=dot+new Vector2(55,-46);
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
