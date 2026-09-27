using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        readonly Color cream=MgfLook.Hex("#fff1ce"),navy=MgfLook.Hex("#142e35"),gold=MgfLook.Hex("#ffd05a");
        RectTransform commandStrip,pressureMarker,treasuryPanel,hpPill,wavePill,correctPill,questionFace,questionLip;
        readonly RectTransform[] padNamePills=new RectTransform[4];
        readonly Vector2[] padScreenLocals=new Vector2[4];
        TextMeshProUGUI questionExpand; bool questionExpanded,questionCanExpand; float questionNaturalHeight;
        RectTransform tutorialBubble,depositBubble; TextMeshProUGUI tutorialCaption;
        RectTransform titleRoot,playRoot,endRoot,ctaRect,packRect,retryRect,questionPanel,feedbackPanel;
        RectTransform tutorialDot,tutorialTrail,tutorialLabel,tutorialCoin;
        TextMeshProUGUI titleInfo,hpText,coinsText,waveText,armyText,hintText,endTitle,endDetail,bonusText,pressureText,treasuryText,depositText;
        HyeopgokMathText questionText,feedbackText,bonusMath,assembledMath;
        TextMeshProUGUI assembledText,confirmText;
        readonly TextMeshProUGUI[] padNames=new TextMeshProUGUI[4];
        readonly int[] lastPoured={-1,-1};
        readonly bool[] lastVisited={false,false};
        readonly string[] basePadNames=new string[4];
        int lastAssembledDen=-1,lastAssembledNum=-1;
        HyeopgokMathText[] padLabels=new HyeopgokMathText[4];
        RectTransform[] padLabelRoots=new RectTransform[4];
        TextMeshProUGUI[] tutorialRings=new TextMeshProUGUI[4];
        TextMeshProUGUI answerRing,confirmRing;int answerMark=-1;
        Image hpFill,timeFill;
        float shownHp=100,bonusLife;
        int previousShownHp=-1,lastReds=-1,lastBlues=-1,lastKills=-1,lastClock=-1,lastTreasury=-1;
        bool tutorialVisible,firstFractionTutorialShown;
        int tutorialKind;
        float tutorialStarted;
        bool IsCoinIntro=>Rules.Wave==1&&Rules.Current!=null&&Rules.Current.Mode=="amount"&&Rules.Current.answerValue==2&&(Rules.Current.id=="m2s2-u6-001"||Rules.Current.id=="m2s2-u7-001");
        RectTransform Group(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        RectTransform Box(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,Color color){
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));var r=(RectTransform)go.transform;r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=offset;r.sizeDelta=size;
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;image.sprite=RoundSprite;image.type=Image.Type.Sliced;return r;
        }
        TextMeshProUGUI Text(string content,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,float font,Color color){
            var t=MgfText.Ui(content,anchor,offset,font,color,size.x);t.transform.SetParent(parent,false);t.rectTransform.sizeDelta=size;
            t.font=RoundFont;t.fontSharedMaterial=RoundFontMaterial;t.fontStyle=FontStyles.Normal;
            t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;t.raycastTarget=false;return t;
        }
        void BuildUi(){
            var canvas=MgfText.Canvas;canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=1;
            RoundFont.TryAddCharacters("①②③확정돌아가면취소먼저밖으로나오기코인부족전투에서모으기모든사건한번더탭패드잠깐처치지원군투자시간초과압박남음지켜라");
            titleRoot=Group("Title",canvas.transform);playRoot=Group("Battle HUD",canvas.transform);endRoot=Group("Results",canvas.transform);
            BuildTitleUi();
            hpPill=Pill("Gate health pill",playRoot,new Vector2(0,1),new Vector2(62,-28),new Vector2(108,42),navy);
            hpText=Text("성문 100",hpPill,new Vector2(.5f,.5f),new Vector2(0,2),new Vector2(102,29),17,cream);
            var hpBg=Box("Gate health",hpPill,new Vector2(.5f,0),new Vector2(0,7),new Vector2(80,4),MgfLook.Hex("#395054"));
            hpFill=Box("health remaining",hpBg,new Vector2(0,.5f),Vector2.zero,new Vector2(80,4),MgfLook.Hex("#77e3a5")).GetComponent<Image>();hpFill.rectTransform.pivot=new Vector2(0,.5f);
            wavePill=Pill("Wave pill",playRoot,new Vector2(0,1),new Vector2(154,-28),new Vector2(69,42),navy);
            waveText=Text("1 / 10",wavePill,new Vector2(.5f,.5f),Vector2.zero,new Vector2(65,30),17,cream);
            correctPill=Pill("Correct answers pill",playRoot,new Vector2(0,1),new Vector2(231,-28),new Vector2(76,42),navy);
            coinsText=Text("정답 0/7",correctPill,new Vector2(.5f,.5f),Vector2.zero,new Vector2(73,30),15,cream);
            treasuryPanel=Pill("Coin treasury",playRoot,new Vector2(1,1),new Vector2(-58,-28),new Vector2(101,42),navy);
            BuildCoinIcon(treasuryPanel,new Vector2(-33,0),23);
            treasuryText=Text("0",treasuryPanel,new Vector2(.5f,.5f),new Vector2(11,1),new Vector2(62,34),24,Color.white);
            questionPanel=Box("Question banner rim",playRoot,new Vector2(.5f,1),new Vector2(0,-125),new Vector2(372,134),MgfLook.Hex("#c99b48"));
            questionFace=Box("Blue campaign plaque",questionPanel,new Vector2(.5f,.5f),new Vector2(0,2),new Vector2(366,128),MgfLook.Hex("#163945"));
            Box("Banner warm highlight",questionFace,new Vector2(.5f,1),new Vector2(0,-5),new Vector2(325,2),MgfLook.Hex("#54737b"));
            questionLip=Box("Banner gold lip",questionFace,new Vector2(.5f,0),new Vector2(0,2),new Vector2(330,4),gold);
            var q=Text("",questionFace,new Vector2(.5f,1),new Vector2(0,-48),new Vector2(340,78),17,cream);q.alignment=TextAlignmentOptions.TopLeft;q.lineSpacing=2;q.fontSharedMaterial=RoundBodyMaterial;
            q.overflowMode=TextOverflowModes.Ellipsis;q.gameObject.AddComponent<RectMask2D>();questionText=new HyeopgokMathText(q);
            questionExpand=Text("",questionFace,new Vector2(1,0),new Vector2(-51,15),new Vector2(92,24),12,gold);
            pressureText=Text("",questionFace,new Vector2(0,0),new Vector2(123,16),new Vector2(230,24),12,MgfLook.Hex("#c1d8d5"));pressureText.alignment=TextAlignmentOptions.MidlineLeft;pressureText.fontSharedMaterial=RoundBodyMaterial;
            var timeBg=Box("Pressure clock",questionFace,new Vector2(.5f,0),new Vector2(0,4),new Vector2(354,5),MgfLook.Hex("#0c2631"));
            timeFill=Box("Time remaining",timeBg,new Vector2(0,.5f),Vector2.zero,new Vector2(354,5),MgfLook.Hex("#48ad79")).GetComponent<Image>();timeFill.rectTransform.pivot=new Vector2(0,.5f);
            pressureMarker=Box("Pressure threshold",timeBg,new Vector2(.25f,.5f),Vector2.zero,new Vector2(3,9),MgfLook.Hex("#dc734c"));
            // Feedback replaces the question in this same banner; no second explanation box.
            feedbackPanel=Group("Answer in the same banner",questionFace);
            var ft=Text("",feedbackPanel,new Vector2(.5f,.5f),new Vector2(0,7),new Vector2(342,102),17,cream);ft.lineSpacing=4;ft.fontSharedMaterial=RoundBodyMaterial;feedbackText=new HyeopgokMathText(ft);feedbackPanel.gameObject.SetActive(false);
            commandStrip=Group("World guidance holder",playRoot);commandStrip.gameObject.SetActive(false);
            hintText=Text("",commandStrip,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1,1),14,cream);
            armyText=Text("",playRoot,new Vector2(0,0),new Vector2(105,23),new Vector2(195,30),12,cream);
            depositBubble=Pill("Pouring speech bubble",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(144,34),navy);
            depositText=Text("",depositBubble,new Vector2(.5f,.5f),Vector2.zero,new Vector2(135,30),15,gold);depositBubble.gameObject.SetActive(false);
            bonusText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(240,62),24,gold);bonusMath=new HyeopgokMathText(bonusText);
            for(int i=0;i<4;i++){
                var p=Group("Pad answer "+i,playRoot);p.anchorMin=p.anchorMax=new Vector2(.5f,.5f);p.sizeDelta=new Vector2(68,52);padLabelRoots[i]=p;
                padNamePills[i]=Pill("Floating label "+i,p,new Vector2(.5f,.5f),new Vector2(0,44),new Vector2(142,40),navy);
                padNames[i]=Text("",padNamePills[i],new Vector2(.5f,.5f),Vector2.zero,new Vector2(130,35),14,cream);
                var pt=Text("",p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(68,56),29,Color.white);padLabels[i]=new HyeopgokMathText(pt);
            }
            assembledText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(94,90),32,gold);assembledMath=new HyeopgokMathText(assembledText);
            confirmText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(235,48),16,cream);
            confirmRing=Text("○",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(132,132),88,gold);confirmRing.gameObject.SetActive(false);
            tutorialTrail=Box("First drag route",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,4),new Color(1,.86f,.38f,.7f));
            tutorialDot=Box("Drag ghost",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(22,21),cream);tutorialDot.localRotation=Quaternion.Euler(0,0,-24);
            Box("Ghost index finger",tutorialDot,new Vector2(.5f,.5f),new Vector2(5,15),new Vector2(8,24),cream);
            Box("Ghost thumb",tutorialDot,new Vector2(.5f,.5f),new Vector2(-13,2),new Vector2(9,10),cream);
            Box("Ghost wrist",tutorialDot,new Vector2(.5f,.5f),new Vector2(2,-14),new Vector2(16,11),gold);
            tutorialCoin=Box("Tutorial demo coin",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(16,16),gold);tutorialCoin.gameObject.SetActive(false);
            tutorialBubble=Pill("World tutorial speech bubble",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(145,30),navy);
            tutorialCaption=Text("멈춰 서서 붓기",tutorialBubble,new Vector2(.5f,.5f),Vector2.zero,new Vector2(137,27),14,cream);tutorialCaption.textWrappingMode=TextWrappingModes.NoWrap;tutorialLabel=tutorialBubble;
            for(int i=0;i<4;i++)tutorialRings[i]=Text("○",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(88,88),57,gold);
            SetTutorial(0);
            answerRing=Text("○",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(96,96),64,gold);answerRing.gameObject.SetActive(false);
            var resultCard=Pill("Victory standard",endRoot,new Vector2(.5f,.52f),Vector2.zero,new Vector2(354,336),navy);
            Box("Result gold rail",resultCard,new Vector2(.5f,1),new Vector2(0,-9),new Vector2(312,5),gold);
            BuildCrest(resultCard,new Vector2(.5f,1),new Vector2(0,37),.65f);
            endTitle=Text("",resultCard,new Vector2(.5f,.79f),Vector2.zero,new Vector2(340,65),36,gold);
            endDetail=Text("",resultCard,new Vector2(.5f,.44f),Vector2.zero,new Vector2(318,127),20,cream);
            retryRect=Pill("Retry",resultCard,new Vector2(.5f,.105f),Vector2.zero,new Vector2(260,58),gold);
            Text("다시 출격",retryRect,new Vector2(.5f,.5f),Vector2.zero,new Vector2(242,50),25,navy);
            playRoot.gameObject.SetActive(false);endRoot.gameObject.SetActive(false);LayoutUi();
        }
        void SetTitleInfo(string s){
            titleInfo.text=loaded&&pack!=null?pack.title:s;
            if(loaded&&pack!=null){RoundFont.TryAddCharacters(bankJson);UpdateSelectedPackUi();EnsureCatalogue();}
        }
        void TitleInput(){HandleTitlePointer();}
        bool Hit(RectTransform rt)=>RectTransformUtility.RectangleContainsScreenPoint(rt,MgfPointer.Position);
        void ShowPlaying(){titleRoot.gameObject.SetActive(false);endRoot.gameObject.SetActive(false);playRoot.gameObject.SetActive(true);shownHp=100;HideFeedback();}
        void SetQuestion(PackItem p){
            lastClock=-1;questionExpanded=false;
            pressureMarker.anchorMin=pressureMarker.anchorMax=new Vector2(1-battle.QuestionGrace/Rules.TimeLimit,.5f);
            for(int i=0;i<4;i++)cracks[i].gameObject.SetActive(false);
            lastPoured[0]=lastPoured[1]=-1;lastVisited[0]=lastVisited[1]=true;lastAssembledDen=lastAssembledNum=-1;assembledMath.Set("");confirmText.text="";confirmRing.gameObject.SetActive(false);
            LayoutUi();
            hintText.text=Rules.Current.Mode=="choice"?"정답 패드 위에 0.8초 서기\n10문제 중 7문제 이상 맞히면 승리":IsCoinIntro?"처치 코인 → 등에 쌓기 → 2닢 붓기\n멈춰서 두 번 탭한 뒤 밖으로 이동":Rules.Current.Mode=="fraction_parts"?"모든 경우 → 사건의 경우 순서로 붓기\n두 패드에서 멈춘 뒤 밖으로 나와 확정":"탭 = 1닢 · 길게 서면 빠르게\n0은 패드에서 잠깐 멈춘 뒤 나오기";
            bool firstFraction=Rules.Current.Mode=="fraction_parts"&&!firstFractionTutorialShown;
            if(firstFraction)firstFractionTutorialShown=true;
            SetTutorial(firstFraction?2:Rules.Wave==1&&st.moves==0?(Rules.Current.Mode=="choice"?3:1):0);
        }
        void FitQuestion(string prompt){
            bool wide=(float)Screen.width/Screen.height>1.2f;float font=wide?27:17.5f;
            questionText.Text.fontSize=font;questionText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-32,500);
            questionText.Set(prompt);questionNaturalHeight=questionText.Text.preferredHeight;
            float closed=font*3.95f;questionText.Text.maxVisibleLines=questionExpanded?100:3;questionText.Text.overflowMode=questionExpanded?TextOverflowModes.Masking:TextOverflowModes.Ellipsis;questionCanExpand=questionNaturalHeight>closed;
            float textHeight=questionExpanded?Mathf.Min(questionNaturalHeight+4,430):Mathf.Max(font*1.25f,Mathf.Min(questionNaturalHeight+3,closed));
            float height=textHeight+(wide?65:51);questionPanel.sizeDelta=new Vector2(questionPanel.sizeDelta.x,height);
            questionPanel.anchoredPosition=new Vector2(0,-61-height*.5f);
            questionFace.sizeDelta=new Vector2(questionPanel.sizeDelta.x-6,height-6);
            questionText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-32,textHeight);
            questionText.Text.rectTransform.anchoredPosition=new Vector2(0,-8-textHeight*.5f);
            questionText.Text.rectTransform.anchorMin=questionText.Text.rectTransform.anchorMax=new Vector2(.5f,1);
            questionLip.sizeDelta=new Vector2(questionPanel.sizeDelta.x-34,4);
            questionExpand.text=questionCanExpand?(questionExpanded?"접기 -":"더 읽기 +"):"";
            questionText.Set(prompt);feedbackText.Text.fontSize=wide?26:17.5f;
            feedbackText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-32,height-36);
        }
        bool HandleBattleUiPointer(){
            if(!questionPanel||!Hit(questionPanel))return false;
            if(questionCanExpand&&!feedbackPanel.gameObject.activeSelf){questionExpanded=!questionExpanded;FitQuestion(Rules.Current.prompt);MgfSfx.Play("tap",.4f);}
            return true;
        }
        void SetChoices(string[] choices){
            for(int i=0;i<4;i++){
                bool active=i<Rules.PadCount;padLabelRoots[i].gameObject.SetActive(active);if(!active)continue;
                if(Rules.Current.Mode=="choice"){
                    basePadNames[i]=padNames[i].text="";padNamePills[i].gameObject.SetActive(false);padLabels[i].Text.fontSize=choices[i].Contains("{frac:")||choices[i].Length<=3?24:choices[i].Length<=5?18:13;padLabels[i].Set(choices[i]);
                }else{
                    padNamePills[i].gameObject.SetActive(true);
                    basePadNames[i]=Rules.PadCount==1?"답만큼 붓기":i==0?Rules.Current.den_label:Rules.Current.num_label;padNames[i].text=basePadNames[i];LayoutPadName(i);
                    padLabels[i].Text.fontSize=30;padLabels[i].Set("0");
                }
            }
            RefreshPadAmounts();
        }
        void RefreshPadAmounts(){
            if(Rules.Current==null||Rules.Current.Mode=="choice"||padLabels[0]==null)return;
            for(int i=0;i<Rules.PadCount;i++)if(lastPoured[i]!=Rules.Poured[i]||lastVisited[i]!=Rules.Visited[i]){
                lastPoured[i]=Rules.Poured[i];lastVisited[i]=Rules.Visited[i];padLabels[i].Set(Rules.Poured[i].ToString());
                string caption=basePadNames[i]+(Rules.Visited[i]&&Rules.Poured[i]==0?" · 0 선택":"");
                if(padNames[i].text!=caption){padNames[i].text=caption;LayoutPadName(i);}padLabelRoots[i].localScale=Vector3.one*1.17f;
            }
            // A fraction with denominator 0 is not a meaningful intermediate
            // result. Keep the assembled fraction hidden until the learner has
            // actually poured a positive number of "all cases" coins.
            bool show=Rules.PadCount==2&&Rules.Poured[0]>0;
            assembledText.gameObject.SetActive(show);
            if(show&&(lastAssembledDen!=Rules.Poured[0]||lastAssembledNum!=Rules.Poured[1])){
                lastAssembledDen=Rules.Poured[0];lastAssembledNum=Rules.Poured[1];assembledMath.Set("{frac:"+Rules.Poured[1]+"/"+Rules.Poured[0]+"}");
            }
        }
        void RefreshHud(){
            waveText.text=Rules.Wave+" / 10";
            bool reachable=Rules.Correct+(10-Rules.Attempts)>=7;
            coinsText.text="정답 "+Rules.Correct+"/7";
            coinsText.color=reachable?gold:MgfLook.Hex("#ffb27c");
            if(!reachable&&!Rules.Ended)hintText.text="이번 판은 버티기\n남은 문제로 성문을 지켜라";
        }
        void UpdateBattleHud(){
            if(lastReds!=battle.Reds||lastBlues!=battle.Blues||lastKills!=battle.Kills){
                lastReds=battle.Reds;lastBlues=battle.Blues;lastKills=battle.Kills;
                armyText.SetText("격파 {0}   ·   수비대 {1}",lastKills,lastBlues);
            }
        }
        void ShowFeedback(bool ok,string s){
            feedbackPanel.gameObject.SetActive(true);questionText.Text.gameObject.SetActive(false);questionExpand.gameObject.SetActive(false);
            feedbackText.Text.color=ok?cream:MgfLook.Hex("#ffc4b2");feedbackText.Set(s);
            float needed=Mathf.Max(questionPanel.sizeDelta.y,feedbackText.Text.preferredHeight+46);
            questionPanel.sizeDelta=new Vector2(questionPanel.sizeDelta.x,needed);questionPanel.anchoredPosition=new Vector2(0,-61-needed*.5f);
            questionFace.sizeDelta=new Vector2(questionPanel.sizeDelta.x-6,needed-6);
            feedbackText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-32,needed-38);feedbackText.Set(s);
            if(ok){bonusMath.Set(Rules.Current.Mode=="choice"?"지원군 출격":"+"+Rules.TotalPoured+"닢 투자");bonusLife=2.2f;}
            // After a miss, mark where the correct answer stood so the explanation line maps to a pad.
            answerMark=ok?-1:Rules.AnswerPad();answerRing.gameObject.SetActive(answerMark>=0);
            if(Rules.LastPad>=0)padLabelRoots[Rules.LastPad].localScale=Vector3.one*(ok?1.3f:.7f);
            if(!ok&&Rules.LastPad>=0)cracks[Rules.LastPad].gameObject.SetActive(true);
            if(!ok&&Rules.Current.Mode=="fraction_parts")for(int i=0;i<2;i++)cracks[i].gameObject.SetActive(true);
            RefreshPadAmounts();
        }
        void HideFeedback(){feedbackPanel.gameObject.SetActive(false);questionText.Text.gameObject.SetActive(true);questionExpand.gameObject.SetActive(true);bonusLife=0;bonusMath.Set("");answerMark=-1;if(answerRing)answerRing.gameObject.SetActive(false);}
        void ShowEnd(bool won,bool fallen){
            endRoot.gameObject.SetActive(true);playRoot.gameObject.SetActive(false);
            endTitle.text=won?"협곡을 지켰다":fallen?"성문이 무너졌다":"버티기만 한 판";
            string next=won?"판단이 전선을 바꿨습니다.":Rules.Correct>=7?"정답 목표는 달성했습니다. 다음 판에는 성문까지 지켜 보세요.":"목표까지 정답 "+(7-Rules.Correct)+"개가 더 필요합니다.";
            endDetail.text="첫 시도 정답  "+Rules.Correct+" / "+Rules.Attempts+"\n"+"점수  "+Rules.Score+"   ·   격파  "+battle.Kills+"\n\n"+next;
        }
        void ResetTutorialProgress(){firstFractionTutorialShown=false;SetTutorial(0);}
        void SetTutorial(int kind){
            tutorialKind=kind;tutorialVisible=kind>0;tutorialStarted=Time.unscaledTime;
            bool visible=tutorialVisible;
            if(tutorialTrail)tutorialTrail.gameObject.SetActive(visible);
            if(tutorialDot)tutorialDot.gameObject.SetActive(visible);
            if(tutorialLabel)tutorialLabel.gameObject.SetActive(visible);
            if(tutorialCoin)tutorialCoin.gameObject.SetActive(false);
            for(int i=0;i<tutorialRings.Length;i++)if(tutorialRings[i])tutorialRings[i].gameObject.SetActive(false);
        }
        void HideTutorial(){if(tutorialVisible)SetTutorial(0);}
        void LayoutUi(){
            if(!questionPanel)return;float cw=844f*Screen.width/Mathf.Max(1,Screen.height);bool wide=(float)Screen.width/Screen.height>1.2f;
            float width=wide?Mathf.Min(760,cw-480):Mathf.Min(cw-16,510);
            questionPanel.anchorMin=questionPanel.anchorMax=new Vector2(.5f,1);questionPanel.sizeDelta=new Vector2(width,136);
            float pill=wide?1.25f:1;hpPill.localScale=wavePill.localScale=correctPill.localScale=treasuryPanel.localScale=Vector3.one*pill;
            hpPill.anchoredPosition=new Vector2(wide?83:62,-29);wavePill.anchoredPosition=new Vector2(wide?201:154,-29);correctPill.anchoredPosition=new Vector2(wide?304:231,-29);
            treasuryPanel.anchoredPosition=new Vector2(wide?-145:-118,-29);
            correctPill.anchorMin=correctPill.anchorMax=wide?new Vector2(0,1):new Vector2(1,0);
            correctPill.anchoredPosition=wide?new Vector2(304,-29):new Vector2(-51,26);
            ((RectTransform)timeFill.transform.parent).sizeDelta=new Vector2(width-24,5);
            pressureText.fontSize=wide?16:11.5f;pressureText.rectTransform.sizeDelta=new Vector2(width-123,wide?46:35);pressureText.rectTransform.anchoredPosition=new Vector2((width-123)*.5f+10,wide?27:21);
            questionExpand.fontSize=wide?16:12;questionExpand.rectTransform.anchoredPosition=new Vector2(-54,16);
            LayoutTitleUi(cw,wide);
            for(int i=0;i<4;i++)if(padNames[i]!=null)LayoutPadName(i);
            if(Rules.Current!=null)FitQuestion(Rules.Current.prompt);
        }
        void AnimateUi(float dt){
            AnimateTitleUi(dt);
            if(!loaded)return;
            ctaRect.localScale=Vector3.Lerp(ctaRect.localScale,Vector3.one,dt*10);
            if(!playStarted)return;
            shownHp=Mathf.MoveTowards(shownHp,Rules.Hp,dt*70);
            int h=Mathf.RoundToInt(shownHp);
            if(h!=previousShownHp){hpText.SetText("성문 {0}",h);previousShownHp=h;}
            hpFill.rectTransform.sizeDelta=new Vector2(80*shownHp/100,4);
            timeFill.rectTransform.sizeDelta=new Vector2((questionPanel.sizeDelta.x-24)*Mathf.Clamp01(1-Rules.Elapsed/Rules.TimeLimit),5);
            int clock=Mathf.CeilToInt(Mathf.Max(0,Rules.TimeLimit-Rules.Elapsed));
            if(clock!=lastClock){
                lastClock=clock;
                if(Rules.Elapsed<battle.QuestionGrace)pressureText.SetText("{0}초 남음 · 시간 초과는 오답\n{1}초 뒤부터 4초마다 성문 -3",clock,Mathf.CeilToInt(battle.QuestionGrace-Rules.Elapsed));
                else pressureText.SetText("{0}초 남음 · 시간 초과는 오답\n4초마다 성문 -3",clock);
            }
            if(lastTreasury!=battle.Coins){lastTreasury=battle.Coins;treasuryText.SetText("{0}",lastTreasury);}
            bool pouring=battle.Depositing&&Rules.Hover>=0;
            bool stopPrompt=Rules.Current.Mode!="choice"&&Rules.IsMovingOnPad&&!Rules.Pending;
            bool showDeposit=pouring||stopPrompt;
            if(depositBubble.gameObject.activeSelf!=showDeposit)depositBubble.gameObject.SetActive(showDeposit);
            RectTransform canvas=(RectTransform)MgfText.Canvas.transform;
            if(showDeposit){
                string deposit=pouring?"-1닢 → 답":"멈춰 서서 붓기";if(depositText.text!=deposit)depositText.text=deposit;
                Vector3 ds=cam.WorldToScreenPoint(HyeopgokRules.Pads[Rules.Hover]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,ds,null,out Vector2 dat);depositBubble.anchoredPosition=dat+new Vector2(0,-54);
            }
            RefreshPadAmounts();
            for(int i=0;i<4;i++){
                if(i>=Rules.PadCount)continue;
                Vector3 p=cam.WorldToScreenPoint(HyeopgokRules.Pads[i]+new Vector3(0,.05f,0));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,p,null,out Vector2 local);padLabelRoots[i].anchoredPosition=local;padScreenLocals[i]=local;
                padLabelRoots[i].localScale=Vector3.Lerp(padLabelRoots[i].localScale,Vector3.one,dt*8);
                float progress=Rules.Current.Mode=="choice"&&Rules.Hover==i?Rules.Dwell/HyeopgokRules.Hold:0;
                int count=progress>0?Mathf.CeilToInt(progress*32)+1:0;padFill[i].positionCount=count;
                for(int k=0;k<count;k++)padFill[i].SetPosition(k,PadEdge(i,Mathf.Min(progress,k/32f)));
            }
            AvoidPadLabelOverlap(canvas);
            Vector3 mid=Rules.PadCount==2?(HyeopgokRules.Pads[0]+HyeopgokRules.Pads[1])*.5f:HyeopgokRules.Pads[0];
            Vector3 middle=cam.WorldToScreenPoint(mid);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,middle,null,out Vector2 center);
            assembledText.rectTransform.anchoredPosition=center+new Vector2(0,-6);
            if(Rules.Confirming){
                Vector3 ks=cam.WorldToScreenPoint(Rules.King);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,ks,null,out Vector2 kingAt);
                confirmRing.gameObject.SetActive(true);confirmRing.rectTransform.anchoredPosition=kingAt;
                float pulse=1.05f+.1f*Mathf.Sin(Time.unscaledTime*9);confirmRing.rectTransform.localScale=Vector3.one*pulse;
                confirmRing.color=Color.Lerp(gold,cream,Mathf.Clamp01(Rules.Confirm/HyeopgokRules.ConfirmTime));
                confirmText.rectTransform.anchoredPosition=kingAt+new Vector2(0,-66);if(confirmText.text!="이대로 확정 · 돌아가면 취소")confirmText.text="이대로 확정 · 돌아가면 취소";
            }else{
                confirmRing.gameObject.SetActive(false);confirmText.rectTransform.anchoredPosition=center+new Vector2(0,-72);
                string confirm=Rules.TutorialBlocked&&!tutorialVisible?"먼저 코인을 1닢 이상 붓고 나오기":!Rules.Pending&&Rules.Hover>=0&&Rules.Coins==0?"코인이 부족해요 · 전투에서 모으기":"";if(confirmText.text!=confirm)confirmText.text=confirm;
            }
            AnimateTutorial();
            if(answerMark>=0){
                Vector3 rs=cam.WorldToScreenPoint(HyeopgokRules.Pads[answerMark]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,rs,null,out Vector2 at);
                answerRing.rectTransform.anchoredPosition=at;answerRing.rectTransform.localScale=Vector3.one*(1.05f+.08f*Mathf.Sin(Time.unscaledTime*7));
                padLabelRoots[answerMark].localScale=Vector3.one*1.15f;
            }
            if(bonusLife>0){bonusLife-=dt;bonusText.rectTransform.anchoredPosition=new Vector2(0,(2.2f-bonusLife)*18);if(bonusLife<=0)bonusMath.Set("");}
        }
        Vector2 TutorialPoint(Vector3 world,RectTransform canvas){
            Vector3 screen=cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,null,out Vector2 local);return local;
        }
        void AnimateTutorial(){
            if(!tutorialVisible)return;
            if(Rules.Confirming||Rules.Pending){
                tutorialTrail.gameObject.SetActive(false);tutorialDot.gameObject.SetActive(false);tutorialLabel.gameObject.SetActive(false);tutorialCoin.gameObject.SetActive(false);
                for(int i=0;i<tutorialRings.Length;i++)tutorialRings[i].gameObject.SetActive(false);return;
            }
            tutorialDot.gameObject.SetActive(true);tutorialLabel.gameObject.SetActive(true);
            RectTransform canvas=(RectTransform)MgfText.Canvas.transform;
            Vector3 fromWorld=Rules.King,toWorld=HyeopgokRules.Pads[0];
            float along=Mathf.SmoothStep(0,1,Mathf.PingPong(Time.unscaledTime*1.15f,1));
            int ringPad=0;bool demoCoin=false;float coinDrop=0;
            string label="멈춰 서서 붓기";

            if(tutorialKind==3){
                toWorld=(HyeopgokRules.Pads[0]+HyeopgokRules.Pads[1]+HyeopgokRules.Pads[2]+HyeopgokRules.Pads[3])*.25f;
                ringPad=-2;label="정답에서 0.8초";
            }else if(tutorialKind==1){
                bool exit=Rules.Visited[0]&&Rules.Poured[0]>=Rules.Current.answerValue;
                toWorld=exit?HyeopgokRules.Exit:HyeopgokRules.Pads[0];ringPad=exit?-1:0;
                label=Rules.TutorialBlocked?"1닢 붓고 나오기":exit?"③ 밖에서 확정":Rules.Poured[0]>0?"② 한 번 더 탭":"① 멈춰 서기";
            }else if(tutorialKind==2){
                bool untouched=!Rules.Visited[0]&&!Rules.Visited[1];
                if(untouched){
                    // A 4.8 second loop visibly rehearses both semantic pads:
                    // move -> stop -> ghost coin, then repeat for the event pad.
                    float t=Mathf.Repeat(Time.unscaledTime-tutorialStarted,4.8f);
                    if(t<1f){fromWorld=Rules.King;toWorld=HyeopgokRules.Pads[0];along=Mathf.SmoothStep(0,1,t);ringPad=0;label="① 모든 경우";}
                    else if(t<1.8f){fromWorld=toWorld=HyeopgokRules.Pads[0];along=1;ringPad=0;demoCoin=true;coinDrop=(t-1f)/.8f;label="멈춰 서서 붓기";}
                    else if(t<2.8f){fromWorld=HyeopgokRules.Pads[0];toWorld=HyeopgokRules.Pads[1];along=Mathf.SmoothStep(0,1,t-1.8f);ringPad=1;label="② 사건의 경우";}
                    else if(t<3.6f){fromWorld=toWorld=HyeopgokRules.Pads[1];along=1;ringPad=1;demoCoin=true;coinDrop=(t-2.8f)/.8f;label="여기도 멈춰서 붓기";}
                    else {fromWorld=HyeopgokRules.Pads[1];toWorld=HyeopgokRules.Exit;along=Mathf.SmoothStep(0,1,(t-3.6f)/1.2f);ringPad=-1;label="③ 밖에서 확정";}
                }else{
                    int missing=!Rules.Visited[0]?0:!Rules.Visited[1]?1:-1;
                    toWorld=missing>=0?HyeopgokRules.Pads[missing]:HyeopgokRules.Exit;ringPad=missing;
                    label=missing==0?"① 모든 경우":missing==1?"② 사건의 경우":"③ 밖에서 확정";
                }
            }

            Vector2 start=TutorialPoint(fromWorld,canvas),target=TutorialPoint(toWorld,canvas);
            Vector2 dot=Vector2.Lerp(start,target,along),delta=target-start;
            tutorialDot.anchoredPosition=dot+new Vector2(22,-16);tutorialDot.localScale=Vector3.one*.72f;Vector2 guideAt=TutorialPoint(Rules.King,canvas)+new Vector2(0,-43);
            guideAt.y=Mathf.Max(guideAt.y,-185);tutorialLabel.anchoredPosition=ClampWorldBubble(guideAt,145);
            if(tutorialCaption.text!=label)tutorialCaption.text=label;
            tutorialTrail.gameObject.SetActive(delta.sqrMagnitude>16);
            tutorialTrail.anchoredPosition=(start+target)*.5f;tutorialTrail.sizeDelta=new Vector2(delta.magnitude,5);
            tutorialTrail.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            tutorialCoin.gameObject.SetActive(demoCoin);
            if(demoCoin){
                Vector2 pad=TutorialPoint(toWorld,canvas);tutorialCoin.anchoredPosition=Vector2.Lerp(pad+new Vector2(0,72),pad+new Vector2(0,8),Mathf.SmoothStep(0,1,coinDrop));
                tutorialCoin.localScale=Vector3.one*(1+.2f*Mathf.Sin(Time.unscaledTime*12));
            }
            for(int i=0;i<4;i++){
                bool active=ringPad==-2&&i<Rules.PadCount||ringPad==i;
                tutorialRings[i].gameObject.SetActive(active);if(!active)continue;
                tutorialRings[i].rectTransform.anchoredPosition=TutorialPoint(HyeopgokRules.Pads[i],canvas);
                tutorialRings[i].rectTransform.localScale=Vector3.one*(1.0f+.12f*Mathf.Sin(Time.unscaledTime*6+i*.7f));
            }
        }
    }
}
