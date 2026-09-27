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
        RectTransform timerBadge,scrollCurlLeft,scrollCurlRight; TextMeshProUGUI gateLossText,feedbackSeal;
        float questionShownAt,gateLossLife; int previousActualHp=100; bool questionAutoCollapsed,questionUserOpened;
        readonly RectTransform[] padPaint=new RectTransform[4]; Transform tutorialArrow;
        readonly RectTransform[] resultCoins=new RectTransform[12]; float resultStarted;
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
            t.font=MgfText.Font;t.fontSharedMaterial=MathFontMaterial;t.fontStyle=FontStyles.Normal;
            t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;t.raycastTarget=false;return t;
        }
        void BuildUi(){
            var canvas=MgfText.Canvas;canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=1;
            MgfText.Font.TryAddCharacters("∠△°²∥⊥∽≡×÷①②③확정돌아가면취소먼저밖으로나오기코인부족전투에서모으기모든사건한번더탭패드잠깐처치지원군투자시간초과압박남음지켜라");
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
            questionPanel=Box("Quest parchment outline",playRoot,new Vector2(.5f,1),new Vector2(0,-106),new Vector2(372,92),MgfLook.Hex("#5a3a1e"));
            questionFace=Box("Cream quest parchment",questionPanel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(366,86),MgfLook.Hex("#f6e7c8"));
            scrollCurlLeft=Box("Left parchment curl",questionPanel,new Vector2(0,.5f),new Vector2(3,0),new Vector2(11,72),MgfLook.Hex("#d7b982"));
            scrollCurlRight=Box("Right parchment curl",questionPanel,new Vector2(1,.5f),new Vector2(-3,0),new Vector2(11,72),MgfLook.Hex("#d7b982"));
            questionLip=Box("Parchment bottom edge",questionFace,new Vector2(.5f,0),new Vector2(0,4),new Vector2(340,3),MgfLook.Hex("#d6bb83"));
            var q=Text("",questionFace,new Vector2(0,1),Vector2.zero,new Vector2(290,52),17.5f,MgfLook.Hex("#392b1d"));q.alignment=TextAlignmentOptions.TopLeft;q.lineSpacing=2;q.fontSharedMaterial=RoundBodyMaterial;
            q.overflowMode=TextOverflowModes.Ellipsis;questionText=new HyeopgokMathText(q);
            questionExpand=Text("",questionFace,new Vector2(1,0),new Vector2(-77,14),new Vector2(104,24),12,MgfLook.Hex("#805329"));
            timerBadge=Box("Hourglass medal outline",questionPanel,new Vector2(1,1),new Vector2(-27,-30),new Vector2(48,48),MgfLook.Hex("#5a3a1e"));
            Box("Hourglass medal gold",timerBadge,new Vector2(.5f,.5f),Vector2.zero,new Vector2(42,42),MgfLook.Hex("#ecc668"));
            BuildHourglass(timerBadge);
            pressureText=Text("30",timerBadge,new Vector2(.5f,.5f),new Vector2(9,0),new Vector2(28,34),17,MgfLook.Hex("#49301c"));pressureText.fontSharedMaterial=RoundBodyMaterial;
            timeFill=Box("Quest edge time remaining",questionLip,new Vector2(0,.5f),Vector2.zero,new Vector2(340,3),MgfLook.Hex("#73b578")).GetComponent<Image>();timeFill.rectTransform.pivot=new Vector2(0,.5f);
            pressureMarker=Box("Pressure threshold",questionLip,new Vector2(.25f,.5f),Vector2.zero,new Vector2(2,3),MgfLook.Hex("#d39757"));
            feedbackPanel=Group("Stamped quest outcome",questionFace);
            feedbackSeal=Text("",feedbackPanel,new Vector2(.5f,1),new Vector2(0,-24),new Vector2(270,32),23,MgfLook.Hex("#27895f"));
            var ft=Text("",feedbackPanel,new Vector2(.5f,1),new Vector2(0,-75),new Vector2(320,60),17.5f,MgfLook.Hex("#392b1d"));ft.lineSpacing=3;ft.fontSharedMaterial=RoundBodyMaterial;feedbackText=new HyeopgokMathText(ft);feedbackPanel.gameObject.SetActive(false);
            gateLossText=Text("-3",playRoot,new Vector2(0,1),new Vector2(80,-45),new Vector2(64,44),26,MgfLook.Hex("#ff6b54"));gateLossText.gameObject.SetActive(false);
            commandStrip=Group("World guidance holder",playRoot);commandStrip.gameObject.SetActive(false);
            hintText=Text("",commandStrip,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1,1),14,cream);
            armyText=Text("",playRoot,new Vector2(0,0),new Vector2(105,23),new Vector2(195,30),12,cream);
            depositBubble=Parchment("Pouring speech bubble",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(144,34));
            depositText=Text("",depositBubble,new Vector2(.5f,.5f),Vector2.zero,new Vector2(135,30),15,MgfLook.Hex("#49301c"));depositBubble.gameObject.SetActive(false);
            bonusText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(240,62),24,gold);bonusMath=new HyeopgokMathText(bonusText);
            for(int i=0;i<4;i++){
                var p=Group("Pad answer "+i,playRoot);p.anchorMin=p.anchorMax=new Vector2(.5f,.5f);p.sizeDelta=new Vector2(68,52);padLabelRoots[i]=p;
                var paintGo=new GameObject("Painted pad label "+i,typeof(RectTransform),typeof(Canvas));
                var paintCanvas=paintGo.GetComponent<Canvas>();paintCanvas.renderMode=RenderMode.WorldSpace;paintCanvas.worldCamera=cam;
                padPaint[i]=(RectTransform)paintGo.transform;padPaint[i].position=HyeopgokRules.Pads[i]+new Vector3(0,.047f,.37f);padPaint[i].rotation=Quaternion.Euler(90,0,0);padPaint[i].localScale=Vector3.one*.01f;padPaint[i].sizeDelta=new Vector2(165,96);
                padNamePills[i]=Group("Painted role "+i,padPaint[i]);
                padNames[i]=Text("",padNamePills[i],new Vector2(.5f,.5f),Vector2.zero,new Vector2(155,40),27,Color.white);padNames[i].fontSharedMaterial=MathFontMaterial;
                padPaint[i].gameObject.SetActive(false);
                var pt=Text("",p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(68,56),29,Color.white);padLabels[i]=new HyeopgokMathText(pt);
            }
            assembledText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(94,90),32,gold);assembledMath=new HyeopgokMathText(assembledText);
            confirmText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(235,48),16,cream);
            confirmRing=Text("○",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(132,132),88,gold);confirmRing.gameObject.SetActive(false);
            tutorialTrail=Box("Hidden gesture route",playRoot,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,Color.clear);
            tutorialDot=Box("Outlined translucent hold hand",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(44,56),new Color(1,1,1,.82f));
            tutorialDot.GetComponent<Image>().sprite=HoldHandSprite;tutorialDot.GetComponent<Image>().type=Image.Type.Simple;
            tutorialArrow=BuildTutorialArrow();
            tutorialCoin=Box("Tutorial demo coin",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(16,16),gold);tutorialCoin.gameObject.SetActive(false);
            tutorialBubble=Parchment("First-use king speech bubble",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(145,30));
            tutorialCaption=Text("멈춰 서서 붓기",tutorialBubble,new Vector2(.5f,.5f),Vector2.zero,new Vector2(137,27),14,MgfLook.Hex("#49301c"));tutorialCaption.textWrappingMode=TextWrappingModes.NoWrap;tutorialLabel=tutorialBubble;
            for(int i=0;i<4;i++)tutorialRings[i]=Text("○",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(88,88),57,gold);
            SetTutorial(0);
            answerRing=Text("○",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(96,96),64,gold);answerRing.gameObject.SetActive(false);
            Box("Result scrim",endRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,1800),new Color(.025f,.08f,.1f,.40f));
            var resultCard=RoyalPanel("Victory royal frame",endRoot,new Vector2(.5f,.52f),Vector2.zero,new Vector2(354,370));
            var resultRibbon=Box("Victory gold ribbon",resultCard,new Vector2(.5f,1),new Vector2(0,-45),new Vector2(372,66),gold);
            BuildCrest(resultCard,new Vector2(.5f,1),new Vector2(0,34),.82f);
            endTitle=DisplayText("",resultRibbon,new Vector2(.5f,.5f),Vector2.zero,new Vector2(346,62),32,MgfLook.Hex("#49301c"));
            Text("★  ★  ★",resultCard,new Vector2(.5f,.68f),Vector2.zero,new Vector2(280,42),32,gold);
            endDetail=Text("",resultCard,new Vector2(.5f,.43f),Vector2.zero,new Vector2(314,130),18,MgfLook.Hex("#49301c"));endDetail.fontSharedMaterial=RoundBodyMaterial;
            retryRect=Pill("Retry",resultCard,new Vector2(.5f,.12f),Vector2.zero,new Vector2(260,58),MgfLook.Hex("#0c73d5"));
            DisplayText("다시 출격",retryRect,new Vector2(.5f,.5f),Vector2.zero,new Vector2(242,50),25,cream);
            for(int i=0;i<resultCoins.Length;i++){resultCoins[i]=Group("Reward coin "+i,endRoot);resultCoins[i].anchorMin=resultCoins[i].anchorMax=new Vector2(.5f,.5f);resultCoins[i].sizeDelta=new Vector2(20,20);BuildCoinIcon(resultCoins[i],Vector2.zero,14+i%3*3);}
            playRoot.gameObject.SetActive(false);endRoot.gameObject.SetActive(false);LayoutUi();
        }
        void SetTitleInfo(string s){
            titleInfo.text=loaded&&pack!=null?pack.title:s;
            if(loaded&&pack!=null){MgfText.Font.TryAddCharacters(bankJson);UpdateSelectedPackUi();EnsureCatalogue();}
        }
        void TitleInput(){HandleTitlePointer();}
        bool Hit(RectTransform rt)=>RectTransformUtility.RectangleContainsScreenPoint(rt,MgfPointer.Position);
        void ShowPlaying(){titleRoot.gameObject.SetActive(false);endRoot.gameObject.SetActive(false);playRoot.gameObject.SetActive(true);shownHp=100;previousActualHp=100;gateLossLife=0;HideFeedback();}
        void SetQuestion(PackItem p){
            lastClock=-1;questionExpanded=true;questionAutoCollapsed=false;questionUserOpened=false;questionShownAt=Time.unscaledTime;
            pressureMarker.anchorMin=pressureMarker.anchorMax=new Vector2(1-battle.QuestionGrace/Rules.TimeLimit,.5f);
            for(int i=0;i<4;i++)cracks[i].gameObject.SetActive(false);
            lastPoured[0]=lastPoured[1]=-1;lastVisited[0]=lastVisited[1]=true;lastAssembledDen=lastAssembledNum=-1;assembledMath.Set("");confirmText.text="";confirmRing.gameObject.SetActive(false);
            LayoutUi();
            hintText.text=Rules.Current.Mode=="choice"?"정답 패드 위에 0.8초 서기\n10문제 중 7문제 이상 맞히면 승리":IsCoinIntro?"처치 코인 → 등에 쌓기 → 2닢 붓기\n멈춰서 두 번 탭한 뒤 밖으로 이동":Rules.Current.Mode=="fraction_parts"?p.den_label+" → "+p.num_label+" 순서로 붓기\n두 패드에서 멈춘 뒤 밖으로 나와 확정":"탭 = 1닢 · 길게 서면 빠르게\n0은 패드에서 잠깐 멈춘 뒤 나오기";
            bool firstFraction=Rules.Current.Mode=="fraction_parts"&&!firstFractionTutorialShown;
            if(firstFraction)firstFractionTutorialShown=true;
            SetTutorial(firstFraction?2:Rules.Wave==1&&st.moves==0?(Rules.Current.Mode=="choice"?3:1):0);
        }
        void FitQuestion(string prompt){
            bool wide=(float)Screen.width/Screen.height>1.2f;float font=wide?24:17.5f;
            float available=questionPanel.sizeDelta.x-76;
            string styled=(wide?"<line-height=30>":"<line-height=22>")+prompt+"</line-height>";
            var body=questionText.Text;
            body.fontSize=font;body.lineSpacing=0;body.textWrappingMode=TextWrappingModes.Normal;
            // Keep the layout rectangle tall even while collapsed. A short TMP
            // rect triggers vertical ellipsis BEFORE maxVisibleLines is applied,
            // losing line two. Only maxVisibleLines performs the collapse.
            body.rectTransform.sizeDelta=new Vector2(available,500);
            body.rectTransform.anchoredPosition=new Vector2(available*.5f+14,-257);
            body.maxVisibleLines=100;body.overflowMode=TextOverflowModes.Overflow;
            questionText.Set(styled);body.ForceMeshUpdate(true,true);
            int lines=Mathf.Max(1,body.textInfo.lineCount);
            var firstLine=body.textInfo.lineInfo[0];
            var lastLine=body.textInfo.lineInfo[lines-1];
            questionNaturalHeight=Mathf.Max(font*1.4f,firstLine.ascender-lastLine.descender+7);
            bool fullOverflow=body.isTextOverflowing;
            questionCanExpand=lines>2||fullOverflow;
            bool expanded=wide||questionExpanded;
            int shownLines=expanded?lines:Mathf.Min(2,lines);
            var shownLast=body.textInfo.lineInfo[shownLines-1];
            float shownHeight=Mathf.Max(font*1.4f,firstLine.ascender-shownLast.descender+7);
            SetScrollSize(Mathf.Max(66,Mathf.Min(410,shownHeight)+14));
            body.maxVisibleLines=expanded?100:2;body.overflowMode=TextOverflowModes.Masking;
            questionExpand.text=questionCanExpand&&!wide?(questionExpanded?"접기":"더 읽기"):"";
            // Layout flags are part of the math wrapper's cache identity so the
            // stacked fractions hide/reappear together with their parent lines.
            questionText.Set(styled);body.ForceMeshUpdate(true,true);feedbackText.Text.fontSize=font;
            if(Application.absoluteURL.Contains("artprobe=1"))Debug.Log("[HYEOPGOK_UI] lines="+lines+" shown="+shownLines+" rectangle="+body.rectTransform.rect.size+" panel="+questionPanel.sizeDelta+" expandable="+questionCanExpand);
        }
        void SetScrollSize(float height){
            float width=questionPanel.sizeDelta.x;questionPanel.sizeDelta=new Vector2(width,height);questionPanel.anchoredPosition=new Vector2(0,-60-height*.5f);
            questionFace.sizeDelta=new Vector2(width-6,height-6);questionLip.sizeDelta=new Vector2(width-28,3);
            scrollCurlLeft.sizeDelta=scrollCurlRight.sizeDelta=new Vector2(9,height-17);
        }
        bool HandleBattleUiPointer(){
            if(!questionPanel||!Hit(questionPanel))return false;
            if(questionCanExpand&&!feedbackPanel.gameObject.activeSelf){questionExpanded=!questionExpanded;questionUserOpened=true;FitQuestion(Rules.Current.prompt);MgfSfx.Play("tap",.4f);}
            return true;
        }
        void SetChoices(string[] choices){
            for(int i=0;i<4;i++){
                bool active=i<Rules.PadCount;padLabelRoots[i].gameObject.SetActive(active);padPaint[i].gameObject.SetActive(active&&playStarted);if(!active)continue;
                if(Rules.Current.Mode=="choice"){
                    basePadNames[i]=padNames[i].text="";padNamePills[i].gameObject.SetActive(false);
                    bool fractionOnly=choices[i].StartsWith("{frac:")&&choices[i].EndsWith("}");
                    bool compoundFraction=!fractionOnly&&choices[i].Contains("{frac:");
                    padLabels[i].Text.textWrappingMode=compoundFraction?TextWrappingModes.NoWrap:TextWrappingModes.Normal;
                    padLabels[i].Text.fontSize=fractionOnly||choices[i].Length<=3?24:choices[i].Length<=5?18:13;padLabels[i].Set(choices[i]);
                }else{
                    padNamePills[i].gameObject.SetActive(true);
                    basePadNames[i]=Rules.PadCount==1?"코인":i==0?Rules.Current.den_label:Rules.Current.num_label;padNames[i].text=basePadNames[i];LayoutPadName(i);
                    padLabels[i].Text.fontSize=27;padLabels[i].Set("0");
                }
            }
            RefreshPadAmounts();
        }
        void RefreshPadAmounts(){
            if(Rules.Current==null||Rules.Current.Mode=="choice"||padLabels[0]==null)return;
            for(int i=0;i<Rules.PadCount;i++)if(lastPoured[i]!=Rules.Poured[i]||lastVisited[i]!=Rules.Visited[i]){
                lastPoured[i]=Rules.Poured[i];lastVisited[i]=Rules.Visited[i];padLabels[i].Set(Rules.Poured[i].ToString());
                string caption=basePadNames[i];
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
            timerBadge.gameObject.SetActive(false);questionLip.gameObject.SetActive(false);
            feedbackSeal.text=ok?"정 답 !":"다시 생각해요";feedbackSeal.color=ok?MgfLook.Hex("#238757"):MgfLook.Hex("#bb503b");
            feedbackText.Text.color=MgfLook.Hex("#49301c");feedbackText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-32,420);feedbackText.Set(s);
            float textHeight=Mathf.Min(400,feedbackText.Text.preferredHeight+6);SetScrollSize(Mathf.Max(102,textHeight+59));
            feedbackText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-32,textHeight);feedbackText.Text.rectTransform.anchoredPosition=new Vector2(0,-45-textHeight*.5f);feedbackText.Set(s);
            if(ok){bonusMath.Set(Rules.Current.Mode=="choice"?"지원군 출격":"+"+Rules.TotalPoured+"닢 투자");bonusLife=2.2f;}
            // After a miss, mark where the correct answer stood so the explanation line maps to a pad.
            answerMark=ok?-1:Rules.AnswerPad();answerRing.gameObject.SetActive(answerMark>=0);
            if(Rules.LastPad>=0)padLabelRoots[Rules.LastPad].localScale=Vector3.one*(ok?1.3f:.7f);
            if(!ok&&Rules.LastPad>=0)cracks[Rules.LastPad].gameObject.SetActive(true);
            if(!ok&&Rules.Current.Mode=="fraction_parts")for(int i=0;i<2;i++)cracks[i].gameObject.SetActive(true);
            RefreshPadAmounts();
        }
        void HideFeedback(){timerBadge.gameObject.SetActive(true);questionLip.gameObject.SetActive(true);feedbackPanel.gameObject.SetActive(false);questionText.Text.gameObject.SetActive(true);questionExpand.gameObject.SetActive(true);bonusLife=0;bonusMath.Set("");answerMark=-1;if(answerRing)answerRing.gameObject.SetActive(false);}
        void ShowEnd(bool won,bool fallen){
            endRoot.gameObject.SetActive(true);playRoot.gameObject.SetActive(false);resultStarted=Time.unscaledTime;for(int i=0;i<4;i++)padPaint[i].gameObject.SetActive(false);if(tutorialArrow)tutorialArrow.gameObject.SetActive(false);
            endTitle.text=won?"협곡을 지켰다":fallen?"성문이 무너졌다":"버티기만 한 판";
            string next=won?"판단이 전선을 바꿨습니다.":Rules.Correct>=7?"정답 목표는 달성했습니다. 다음 판에는 성문까지 지켜 보세요.":"목표까지 정답 "+(7-Rules.Correct)+"개가 더 필요합니다.";
            endDetail.text="첫 시도 정답  "+Rules.Correct+" / "+Rules.Attempts+"\n"+"점수  "+Rules.Score+"   ·   격파  "+battle.Kills+"\n\n"+next;
        }
        void ResetTutorialProgress(){firstFractionTutorialShown=false;SetTutorial(0);}
        void SetTutorial(int kind){
            tutorialKind=kind;tutorialVisible=kind>0;tutorialStarted=Time.unscaledTime;
            bool visible=tutorialVisible;
            if(tutorialTrail)tutorialTrail.gameObject.SetActive(false);if(tutorialArrow)tutorialArrow.gameObject.SetActive(visible);
            if(tutorialDot)tutorialDot.gameObject.SetActive(visible);
            if(tutorialLabel)tutorialLabel.gameObject.SetActive(visible);
            if(tutorialCoin)tutorialCoin.gameObject.SetActive(false);
            for(int i=0;i<tutorialRings.Length;i++)if(tutorialRings[i])tutorialRings[i].gameObject.SetActive(false);
        }
        void HideTutorial(){if(tutorialVisible)SetTutorial(0);}
        void LayoutUi(){
            if(!questionPanel)return;float cw=844f*Screen.width/Mathf.Max(1,Screen.height);bool wide=(float)Screen.width/Screen.height>1.2f;
            float width=wide?Mathf.Min(cw*.45f,610):Mathf.Min(cw-16,510);
            questionPanel.anchorMin=questionPanel.anchorMax=new Vector2(.5f,1);questionPanel.sizeDelta=new Vector2(width,136);
            float pill=wide?1.25f:1;hpPill.localScale=wavePill.localScale=correctPill.localScale=treasuryPanel.localScale=Vector3.one*pill;
            hpPill.anchoredPosition=new Vector2(wide?83:62,-29);wavePill.anchoredPosition=new Vector2(wide?201:154,-29);correctPill.anchoredPosition=new Vector2(wide?304:231,-29);
            treasuryPanel.anchoredPosition=new Vector2(wide?-145:-118,-29);
            correctPill.anchorMin=correctPill.anchorMax=wide?new Vector2(0,1):new Vector2(1,0);
            correctPill.anchoredPosition=wide?new Vector2(304,-29):new Vector2(-51,26);
            pressureText.fontSize=wide?18:17;
            questionExpand.fontSize=wide?12:10.5f;questionExpand.rectTransform.anchoredPosition=new Vector2(-27,11);questionExpand.rectTransform.sizeDelta=new Vector2(46,21);
            LayoutTitleUi(cw,wide);
            for(int i=0;i<4;i++)if(padNames[i]!=null)LayoutPadName(i);
            if(Rules.Current!=null)FitQuestion(Rules.Current.prompt);
        }
        void AnimateUi(float dt){
            AnimateTitleUi(dt);
            if(endRoot.gameObject.activeSelf)AnimateRewardCoins();
            if(!loaded)return;
            ctaRect.localScale=Vector3.Lerp(ctaRect.localScale,Vector3.one,dt*10);
            if(!playStarted)return;
            if(!questionAutoCollapsed&&!questionUserOpened&&!feedbackPanel.gameObject.activeSelf&&Time.unscaledTime-questionShownAt>=3&&(float)Screen.width/Screen.height<=1.2f){questionAutoCollapsed=true;questionExpanded=false;FitQuestion(Rules.Current.prompt);}
            if(Rules.Hp<previousActualHp){gateLossLife=1.15f;gateLossText.SetText("−{0}",previousActualHp-Rules.Hp);gateLossText.gameObject.SetActive(true);}previousActualHp=Rules.Hp;
            if(gateLossLife>0){gateLossLife-=dt;float t=1-gateLossLife/1.15f;gateLossText.rectTransform.anchoredPosition=hpPill.anchoredPosition+new Vector2(30,-12+t*45);gateLossText.alpha=Mathf.Clamp01(gateLossLife*2);if(gateLossLife<=0)gateLossText.gameObject.SetActive(false);}
            shownHp=Mathf.MoveTowards(shownHp,Rules.Hp,dt*70);
            int h=Mathf.RoundToInt(shownHp);
            if(h!=previousShownHp){hpText.SetText("성문 {0}",h);previousShownHp=h;}
            hpFill.rectTransform.sizeDelta=new Vector2(80*shownHp/100,4);
            timeFill.rectTransform.sizeDelta=new Vector2((questionPanel.sizeDelta.x-28)*Mathf.Clamp01(1-Rules.Elapsed/Rules.TimeLimit),3);
            int clock=Mathf.CeilToInt(Mathf.Max(0,Rules.TimeLimit-Rules.Elapsed));
            if(clock!=lastClock){lastClock=clock;pressureText.SetText("{0}",clock);pressureText.color=clock<=8?MgfLook.Hex("#b94e34"):MgfLook.Hex("#49301c");}
            if(lastTreasury!=battle.Coins){lastTreasury=battle.Coins;treasuryText.SetText("{0}",lastTreasury);}
            bool pouring=battle.Depositing&&Rules.Hover>=0;
            bool stopPrompt=Rules.Current.Mode!="choice"&&Rules.IsMovingOnPad&&!Rules.Pending;
            bool showDeposit=(pouring||stopPrompt)&&Rules.Wave==1&&Time.unscaledTime-questionShownAt<8&&!tutorialVisible;
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
                Vector3 p=cam.WorldToScreenPoint(HyeopgokRules.Pads[i]+new Vector3(0,.05f,Rules.Current.Mode=="choice"?0:-.43f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,p,null,out Vector2 local);padLabelRoots[i].anchoredPosition=local;padScreenLocals[i]=local;
                padLabelRoots[i].localScale=Vector3.Lerp(padLabelRoots[i].localScale,Vector3.one,dt*8);
                float progress=Rules.Current.Mode=="choice"&&Rules.Hover==i?Rules.Dwell/HyeopgokRules.Hold:0;
                int count=progress>0?Mathf.CeilToInt(progress*32)+1:0;padFill[i].positionCount=count;
                for(int k=0;k<count;k++)padFill[i].SetPosition(k,PadEdge(i,Mathf.Min(progress,k/32f)));
            }
            // Role labels are painted into each pad, so screen-space chips cannot overlap.
            Vector3 mid=Rules.PadCount==2?(HyeopgokRules.Pads[0]+HyeopgokRules.Pads[1])*.5f:HyeopgokRules.Pads[0];
            Vector3 middle=cam.WorldToScreenPoint(mid);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,middle,null,out Vector2 center);
            assembledText.rectTransform.anchoredPosition=center+new Vector2(0,-6);
            if(Rules.Confirming){
                Vector3 ks=cam.WorldToScreenPoint(Rules.King);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,ks,null,out Vector2 kingAt);
                confirmRing.gameObject.SetActive(true);confirmRing.rectTransform.anchoredPosition=kingAt;
                float pulse=1.05f+.1f*Mathf.Sin(Time.unscaledTime*9);confirmRing.rectTransform.localScale=Vector3.one*pulse;
                confirmRing.color=Color.Lerp(MgfLook.Hex("#75ed72"),cream,Mathf.Clamp01(Rules.Confirm/HyeopgokRules.ConfirmTime));
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
                tutorialTrail.gameObject.SetActive(false);if(tutorialArrow)tutorialArrow.gameObject.SetActive(false);tutorialDot.gameObject.SetActive(false);tutorialLabel.gameObject.SetActive(false);tutorialCoin.gameObject.SetActive(false);
                for(int i=0;i<tutorialRings.Length;i++)tutorialRings[i].gameObject.SetActive(false);return;
            }
            tutorialDot.gameObject.SetActive(true);tutorialLabel.gameObject.SetActive(Time.unscaledTime-tutorialStarted<6);if(tutorialArrow)tutorialArrow.gameObject.SetActive(true);
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
                    if(t<1f){fromWorld=Rules.King;toWorld=HyeopgokRules.Pads[0];along=Mathf.SmoothStep(0,1,t);ringPad=0;label=Rules.Current.den_label;}
                    else if(t<1.8f){fromWorld=toWorld=HyeopgokRules.Pads[0];along=1;ringPad=0;demoCoin=true;coinDrop=(t-1f)/.8f;label="멈춰 서서 붓기";}
                    else if(t<2.8f){fromWorld=HyeopgokRules.Pads[0];toWorld=HyeopgokRules.Pads[1];along=Mathf.SmoothStep(0,1,t-1.8f);ringPad=1;label=Rules.Current.num_label;}
                    else if(t<3.6f){fromWorld=toWorld=HyeopgokRules.Pads[1];along=1;ringPad=1;demoCoin=true;coinDrop=(t-2.8f)/.8f;label="여기도 멈춰서 붓기";}
                    else {fromWorld=HyeopgokRules.Pads[1];toWorld=HyeopgokRules.Exit;along=Mathf.SmoothStep(0,1,(t-3.6f)/1.2f);ringPad=-1;label="③ 밖에서 확정";}
                }else{
                    int missing=!Rules.Visited[0]?0:!Rules.Visited[1]?1:-1;
                    toWorld=missing>=0?HyeopgokRules.Pads[missing]:HyeopgokRules.Exit;ringPad=missing;
                    label=missing==0?Rules.Current.den_label:missing==1?Rules.Current.num_label:"밖에서 확정";
                }
            }

            Vector2 start=TutorialPoint(fromWorld,canvas),target=TutorialPoint(toWorld,canvas);
            Vector2 dot=Vector2.Lerp(start,target,along),delta=target-start;
            tutorialDot.anchoredPosition=dot+new Vector2(22,-16);tutorialDot.localScale=Vector3.one*.72f;
            // A choice question uses all four pads. Put its short first-use hint
            // below the king so it cannot cover the lower pair of answer pads.
            Vector2 guideAt=TutorialPoint(Rules.King,canvas)+new Vector2(0,Rules.Current.Mode=="choice"?-55:62);
            if(Rules.Current.Mode!="choice")guideAt.y=Mathf.Max(guideAt.y,-185);
            tutorialLabel.anchoredPosition=ClampWorldBubble(guideAt,145);
            if(tutorialKind!=2)label=Rules.Current.Mode=="choice"?"정답에서 0.8초":"멈춰 서서 붓기";
            tutorialCaption.fontSize=label.Length>12?12:14;
            if(tutorialCaption.text!=label)tutorialCaption.text=label;
            tutorialTrail.gameObject.SetActive(false);
            if(tutorialArrow){tutorialArrow.position=toWorld+new Vector3(0,1.5f+.12f*Mathf.Sin(Time.unscaledTime*5),0);tutorialArrow.rotation=Quaternion.Euler(0,-8,0);}
            tutorialTrail.anchoredPosition=(start+target)*.5f;tutorialTrail.sizeDelta=new Vector2(delta.magnitude,5);
            tutorialTrail.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            tutorialCoin.gameObject.SetActive(demoCoin);
            if(demoCoin){
                Vector2 pad=TutorialPoint(toWorld,canvas);tutorialCoin.anchoredPosition=Vector2.Lerp(pad+new Vector2(0,72),pad+new Vector2(0,8),Mathf.SmoothStep(0,1,coinDrop));
                tutorialCoin.localScale=Vector3.one*(1+.2f*Mathf.Sin(Time.unscaledTime*12));
            }
            for(int i=0;i<4;i++){
                bool active=false;
                tutorialRings[i].gameObject.SetActive(active);if(!active)continue;
                tutorialRings[i].rectTransform.anchoredPosition=TutorialPoint(HyeopgokRules.Pads[i],canvas);
                tutorialRings[i].rectTransform.localScale=Vector3.one*(1.0f+.12f*Mathf.Sin(Time.unscaledTime*6+i*.7f));
            }
        }
    }
}
