using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        readonly Color cream=MgfLook.Hex("#fff1ce"),navy=MgfLook.Hex("#142e35"),gold=MgfLook.Hex("#ffd05a");
        RectTransform commandStrip,pressureMarker,treasuryPanel,hpPill,wavePill,correctPill,questionFace,questionLip,questionShadow,questionBackdrop,assembledHud;
        RectTransform timerBadge,scrollCurlLeft,scrollCurlRight,feedbackRewardIcon; TextMeshProUGUI gateLossText,feedbackSeal,feedbackExpand;
        float questionShownAt,gateLossLife,questionReadTime=3; int previousActualHp=100; bool questionAutoCollapsed,questionUserOpened,questionShowsAll;
        readonly RectTransform[] padPaint=new RectTransform[4]; Transform tutorialArrow;
        readonly RectTransform[] resultCoins=new RectTransform[12]; float resultStarted; TextMeshProUGUI resultStars;
        readonly RectTransform[] padNamePills=new RectTransform[4];
        readonly Vector2[] padScreenLocals=new Vector2[4];
        TextMeshProUGUI questionExpand; bool questionExpanded,questionCanExpand; float questionNaturalHeight;
        HyeopgokMathText collapsedQuestion; string feedbackFull="",feedbackSummary=""; bool feedbackExpanded,feedbackCanExpand,feedbackWasCorrect;
        RectTransform tutorialBubble,depositBubble; TextMeshProUGUI tutorialCaption;
        RectTransform titleRoot,playRoot,endRoot,ctaRect,packRect,retryRect,questionPanel,feedbackPanel;
        RectTransform tutorialDot,tutorialTrail,tutorialLabel,tutorialCoin;
        TextMeshProUGUI titleInfo,hpText,coinsText,waveText,armyText,hintText,endTitle,endDetail,bonusText,pressureText,treasuryText,depositText;
        HyeopgokMathText questionText,feedbackText,bonusMath,assembledMath;
        TextMeshProUGUI assembledText,assembledCaption,confirmText;
        readonly TextMeshProUGUI[] padNames=new TextMeshProUGUI[4];
        readonly int[] lastPoured={-1,-1};
        readonly bool[] lastVisited={false,false};
        readonly string[] basePadNames=new string[4];
        int lastAssembledDen=-1,lastAssembledNum=-1;
        HyeopgokMathText[] padLabels=new HyeopgokMathText[4];
        RectTransform[] padLabelRoots=new RectTransform[4];
        TextMeshProUGUI[] tutorialRings=new TextMeshProUGUI[4];
        readonly RectTransform[] upgradeLabelRoots=new RectTransform[HyeopgokBattle.TowerCount];
        readonly TextMeshProUGUI[] upgradeLabels=new TextMeshProUGUI[HyeopgokBattle.TowerCount];
        static readonly Vector2[] UpgradeLabelOffsets={
            new Vector2(0,58),new Vector2(76,30),new Vector2(-76,30),
            new Vector2(82,-34),new Vector2(-82,-34),new Vector2(0,-62)
        };
        readonly int[] shownUpgradeLevel={-99,-99,-99},shownUpgradeRemaining={-99,-99,-99},shownUpgradeType={-99,-99,-99};
        TextMeshProUGUI answerRing,confirmRing;int answerMark=-1;
        Image hpFill,timeFill;
        float shownHp=100,bonusLife,fractionRevealLife,fractionRevealReduceAt;
        int fractionRevealDen=-1,fractionRevealNum=-1;bool fractionRevealReduced;
        int previousShownHp=-1,lastReds=-1,lastBlues=-1,lastKills=-1,lastClock=-1,lastTreasury=-1;
        bool tutorialVisible,firstFractionTutorialShown;
        bool choiceProbePending;
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
            // Korean wraps between words (spaces), never inside a word such as 구|하시오.
            if(TMP_Settings.instance!=null)TMP_Settings.useModernHangulLineBreakingRules=true;
            MgfText.Font.TryAddCharacters("∠△°²∥⊥∽≡×÷①②③확정돌아가면취소먼저밖으로나오기코인부족전투에서모으기모든사건한번더탭패드하세요잠깐처치지원군투자시간초과압박남음지켜라보스웨이브지역수복영구강화타워공격력흡수반경성문최대체력초원협곡사막강다리설원요새연사부채꼴폭발연쇄발광룬최고기록버틴문제도달맵첫시도정답률원정끝났습니다");
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
            questionBackdrop=Box("Question safe-area backdrop",playRoot,new Vector2(.5f,1),new Vector2(0,-90),new Vector2(4000,180),MgfLook.Hex("#18333f"));
            questionBackdrop.SetAsFirstSibling();
            questionShadow=Box("Quest parchment soft shadow",playRoot,new Vector2(.5f,1),new Vector2(3,-110),new Vector2(374,94),new Color(.05f,.14f,.14f,.42f));
            questionPanel=Box("Quest parchment outline",playRoot,new Vector2(.5f,1),new Vector2(0,-106),new Vector2(372,92),MgfLook.Hex("#5a3a1e"));
            questionFace=Box("Cream quest parchment",questionPanel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(366,86),MgfLook.Hex("#f6e7c8"));
            Box("Parchment warm top glaze",questionFace,new Vector2(.5f,1),new Vector2(0,-4),new Vector2(330,4),MgfLook.Hex("#fff5d9"));
            scrollCurlLeft=Box("Left parchment curl",questionPanel,new Vector2(0,.5f),new Vector2(3,0),new Vector2(11,72),MgfLook.Hex("#d7b982"));
            scrollCurlRight=Box("Right parchment curl",questionPanel,new Vector2(1,.5f),new Vector2(-3,0),new Vector2(11,72),MgfLook.Hex("#d7b982"));
            questionLip=Box("Parchment bottom edge",questionFace,new Vector2(.5f,0),new Vector2(0,4),new Vector2(340,3),MgfLook.Hex("#d6bb83"));
            var q=Text("",questionFace,new Vector2(0,1),Vector2.zero,new Vector2(290,52),22f,MgfLook.Hex("#392b1d"));q.alignment=TextAlignmentOptions.TopLeft;q.lineSpacing=2;q.fontSharedMaterial=RoundBodyMaterial;
            q.overflowMode=TextOverflowModes.Overflow;questionText=new HyeopgokMathText(q);
            var cq=Text("",questionFace,new Vector2(0,1),Vector2.zero,new Vector2(290,30),17.5f,MgfLook.Hex("#392b1d"));cq.alignment=TextAlignmentOptions.MidlineLeft;cq.fontSharedMaterial=RoundBodyMaterial;
            cq.textWrappingMode=TextWrappingModes.NoWrap;cq.overflowMode=TextOverflowModes.Ellipsis;cq.maxVisibleLines=1;collapsedQuestion=new HyeopgokMathText(cq);cq.gameObject.SetActive(false);
            questionExpand=Text("",questionFace,new Vector2(1,0),new Vector2(-77,14),new Vector2(104,24),12,MgfLook.Hex("#805329"));
            timerBadge=Box("Hourglass medal outline",questionPanel,new Vector2(1,1),new Vector2(-27,-30),new Vector2(48,48),MgfLook.Hex("#5a3a1e"));
            Box("Hourglass medal gold",timerBadge,new Vector2(.5f,.5f),Vector2.zero,new Vector2(42,42),MgfLook.Hex("#ecc668"));
            BuildHourglass(timerBadge);
            pressureText=Text("30",timerBadge,new Vector2(.5f,.5f),new Vector2(9,0),new Vector2(30,34),17,MgfLook.Hex("#49301c"));pressureText.fontSharedMaterial=RoundBodyMaterial;pressureText.textWrappingMode=TextWrappingModes.NoWrap;pressureText.alignment=TextAlignmentOptions.Center;
            timeFill=Box("Quest edge time remaining",questionLip,new Vector2(0,.5f),Vector2.zero,new Vector2(340,3),MgfLook.Hex("#73b578")).GetComponent<Image>();timeFill.rectTransform.pivot=new Vector2(0,.5f);
            pressureMarker=Box("Pressure threshold",questionLip,new Vector2(.25f,.5f),Vector2.zero,new Vector2(2,3),MgfLook.Hex("#d39757"));
            feedbackPanel=Group("Stamped quest outcome",questionFace);
            feedbackSeal=Text("",feedbackPanel,new Vector2(.5f,1),new Vector2(0,-24),new Vector2(270,32),23,MgfLook.Hex("#27895f"));
            feedbackRewardIcon=Group("Reward coin and growing tower",feedbackPanel);feedbackRewardIcon.anchorMin=feedbackRewardIcon.anchorMax=new Vector2(.5f,1);feedbackRewardIcon.anchoredPosition=new Vector2(0,-67);feedbackRewardIcon.sizeDelta=new Vector2(112,44);
            BuildCoinIcon(feedbackRewardIcon,new Vector2(-36,0),34);BuildRewardTower(feedbackRewardIcon,new Vector2(22,-2));
            var ft=Text("",feedbackPanel,new Vector2(.5f,1),new Vector2(0,-75),new Vector2(320,60),17.5f,MgfLook.Hex("#392b1d"));ft.lineSpacing=3;ft.fontSharedMaterial=RoundBodyMaterial;feedbackText=new HyeopgokMathText(ft);
            feedbackExpand=Text("",feedbackPanel,new Vector2(1,0),new Vector2(-55,12),new Vector2(100,22),12,MgfLook.Hex("#805329"));feedbackExpand.alignment=TextAlignmentOptions.Right;
            feedbackPanel.gameObject.SetActive(false);
            gateLossText=Text("-3",playRoot,new Vector2(0,1),new Vector2(80,-45),new Vector2(64,44),26,MgfLook.Hex("#ff6b54"));gateLossText.gameObject.SetActive(false);
            commandStrip=Group("World guidance holder",playRoot);commandStrip.gameObject.SetActive(false);
            hintText=Text("",commandStrip,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1,1),14,cream);
            armyText=Text("",playRoot,new Vector2(0,0),new Vector2(105,23),new Vector2(195,30),12,cream);
            depositBubble=Parchment("Pouring speech bubble",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(144,34));
            depositText=Text("",depositBubble,new Vector2(.5f,.5f),Vector2.zero,new Vector2(135,30),15,MgfLook.Hex("#49301c"));depositBubble.gameObject.SetActive(false);
            bonusText=Text("",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(200,42),22,gold);bonusText.textWrappingMode=TextWrappingModes.NoWrap;bonusMath=new HyeopgokMathText(bonusText);
            for(int i=0;i<4;i++){
                var p=Group("Pad answer "+i,playRoot);p.anchorMin=p.anchorMax=new Vector2(.5f,.5f);p.sizeDelta=new Vector2(52,58);padLabelRoots[i]=p;
                var paintGo=new GameObject("Painted pad label "+i,typeof(RectTransform),typeof(Canvas));
                var paintCanvas=paintGo.GetComponent<Canvas>();paintCanvas.renderMode=RenderMode.WorldSpace;paintCanvas.worldCamera=cam;paintCanvas.sortingOrder=20;
                padPaint[i]=(RectTransform)paintGo.transform;padPaint[i].position=HyeopgokRules.Pads[i]+new Vector3(0,.047f,.37f);padPaint[i].rotation=Quaternion.Euler(90,0,0);padPaint[i].localScale=Vector3.one*.01f;padPaint[i].sizeDelta=new Vector2(165,96);
                padNamePills[i]=Group("Painted role "+i,padPaint[i]);
                padNames[i]=Text("",padNamePills[i],new Vector2(.5f,.5f),Vector2.zero,new Vector2(155,40),27,Color.white);padNames[i].fontSharedMaterial=MathFontMaterial;
                padPaint[i].gameObject.SetActive(false);
                var pt=Text("",p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(52,58),29,Color.white);pt.maxVisibleLines=5;pt.lineSpacing=-5;padLabels[i]=new HyeopgokMathText(pt);
            }
            for(int i=0;i<HyeopgokBattle.TowerCount;i++){
                var root=Group("Upgrade pad label "+i,playRoot);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(130,30);upgradeLabelRoots[i]=root;
                var label=Text("",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(130,28),14,gold);label.fontSharedMaterial=MathFontMaterial;label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;upgradeLabels[i]=label;root.gameObject.SetActive(false);
            }
            assembledHud=Pill("Assembled probability HUD",playRoot,new Vector2(1,1),new Vector2(-64,-298),new Vector2(112,88),navy);
            assembledCaption=Text("사건 ÷ 전체",assembledHud,new Vector2(.5f,1),new Vector2(0,-17),new Vector2(102,24),13,cream);assembledCaption.textWrappingMode=TextWrappingModes.NoWrap;
            assembledText=Text("",assembledHud,new Vector2(.5f,.5f),new Vector2(0,-10),new Vector2(94,62),36,gold);assembledMath=new HyeopgokMathText(assembledText);
            assembledHud.gameObject.SetActive(false);assembledText.gameObject.SetActive(false);
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
            // Keep the victory illustration in the star field so it never masks
            // the score/explanation copy on the narrow result card.
            BuildKeepVignette(resultCard,new Vector2(.5f,.5f),new Vector2(0,88),.27f,true);
            var resultRibbon=Box("Victory gold ribbon",resultCard,new Vector2(.5f,1),new Vector2(0,-45),new Vector2(372,66),gold);
            BuildCrest(resultCard,new Vector2(.5f,1),new Vector2(0,34),.82f);
            endTitle=DisplayText("",resultRibbon,new Vector2(.5f,.5f),Vector2.zero,new Vector2(346,62),32,MgfLook.Hex("#49301c"));
            resultStars=DisplayText("최고 기록",resultCard,new Vector2(.5f,.63f),Vector2.zero,new Vector2(300,38),24,gold);
            endDetail=Text("",resultCard,new Vector2(.5f,.43f),Vector2.zero,new Vector2(314,130),18,MgfLook.Hex("#49301c"));endDetail.fontSharedMaterial=RoundBodyMaterial;
            retryRect=GameButton("Retry",resultCard,new Vector2(.5f,.12f),Vector2.zero,new Vector2(260,62),MgfLook.Hex("#0c73d5"),MgfLook.Hex("#074f9c"));
            DisplayText("다시 출격",retryRect,new Vector2(.5f,.5f),new Vector2(0,2),new Vector2(242,50),27,Color.white);
            for(int i=0;i<resultCoins.Length;i++){resultCoins[i]=Group("Reward coin "+i,endRoot);resultCoins[i].SetSiblingIndex(1);resultCoins[i].anchorMin=resultCoins[i].anchorMax=new Vector2(.5f,.5f);resultCoins[i].sizeDelta=new Vector2(26,26);BuildCoinIcon(resultCoins[i],Vector2.zero,18+i%3*4);}
            BuildV32Ui();
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
            questionReadTime=Mathf.Clamp(2.5f+p.prompt.Length*.03f,3,6.5f);
            pressureMarker.anchorMin=pressureMarker.anchorMax=new Vector2(1-battle.QuestionGrace/Rules.TimeLimit,.5f);
            for(int i=0;i<4;i++)cracks[i].gameObject.SetActive(false);
            lastPoured[0]=lastPoured[1]=-1;lastVisited[0]=lastVisited[1]=true;lastAssembledDen=lastAssembledNum=-1;assembledMath.Set("");assembledHud.gameObject.SetActive(false);assembledText.gameObject.SetActive(false);confirmText.text="";confirmRing.gameObject.SetActive(false);
            LayoutUi();
            hintText.text="보기 패드를 탭하면 왕이 달려가 답을 확정해요\n전투 코인은 금빛 타워 패드에서만 사용";
            SetTutorial(Rules.Wave==1&&st.moves==0?3:0);
        }
        void FitQuestion(string prompt){
            bool wide=(float)Screen.width/Screen.height>1.2f;float font=wide?36:24;
            float available=questionPanel.sizeDelta.x-78;
            string styled=(wide?"<line-height=45>":"<line-height=32>")+prompt+"</line-height>";
            var body=questionText.Text;
            body.gameObject.SetActive(true);collapsedQuestion.Text.gameObject.SetActive(false);
            body.fontSize=font;body.lineSpacing=0;body.textWrappingMode=TextWrappingModes.Normal;
            body.rectTransform.sizeDelta=new Vector2(available,720);
            body.rectTransform.anchoredPosition=new Vector2(available*.5f+14,-370);
            body.maxVisibleLines=100;body.overflowMode=TextOverflowModes.Overflow;
            questionText.Set(styled);body.ForceMeshUpdate(true,true);
            questionNaturalHeight=Mathf.Max(font*1.35f,body.preferredHeight+6);
            questionCanExpand=false;questionShowsAll=true;questionExpanded=true;
            SetScrollSize(Mathf.Max(wide?86:78,questionNaturalHeight+24));
            body.rectTransform.sizeDelta=new Vector2(available,questionNaturalHeight+4);
            body.rectTransform.anchoredPosition=new Vector2(available*.5f+14,-14-questionNaturalHeight*.5f);
            body.maxVisibleLines=100;body.overflowMode=TextOverflowModes.Overflow;
            questionExpand.text="";questionExpand.gameObject.SetActive(false);
            questionText.Set(styled);body.ForceMeshUpdate(true,true);feedbackText.Text.fontSize=font;
            PushV3QuestionProbe(prompt);
        }
        void SetScrollSize(float height){
            float width=questionPanel.sizeDelta.x;questionPanel.sizeDelta=new Vector2(width,height);questionPanel.anchoredPosition=new Vector2(0,-60-height*.5f);
            questionShadow.sizeDelta=new Vector2(width+2,height+2);questionShadow.anchoredPosition=questionPanel.anchoredPosition+new Vector2(3,-4);
            questionFace.sizeDelta=new Vector2(width-6,height-6);questionLip.sizeDelta=new Vector2(width-28,3);
            scrollCurlLeft.sizeDelta=scrollCurlRight.sizeDelta=new Vector2(9,height-17);
            questionBackdrop.sizeDelta=new Vector2(4000,height+70);questionBackdrop.anchoredPosition=new Vector2(0,-(height+70)*.5f);
            ApplyWorldViewport(height);
        }
        bool HandleBattleUiPointer(){
            if(HandleV32BattleUiPointer())return true;
            if(!questionPanel||!Hit(questionPanel))return false;
            return true;
        }
        void SetChoices(string[] choices){
            for(int i=0;i<4;i++){
                bool active=i<Rules.PadCount;padLabelRoots[i].gameObject.SetActive(active);padPaint[i].gameObject.SetActive(active&&playStarted);if(!active)continue;
                if(Rules.Current.Mode=="choice"){
                    basePadNames[i]=padNames[i].text="";padNamePills[i].gameObject.SetActive(false);
                    bool fractionOnly=choices[i].StartsWith("{frac:")&&choices[i].EndsWith("}");
                    int length=ChoiceDisplayLength(choices[i]);
                    padLabels[i].Text.textWrappingMode=TextWrappingModes.Normal;
                    padLabels[i].Text.fontSize=fractionOnly?24:length<=3?23:length<=6?16:length<=9?12:11;padLabels[i].Set(choices[i]);
                }else{
                    padNamePills[i].gameObject.SetActive(true);
                    basePadNames[i]=Rules.PadCount==1?"코인":i==0?Rules.Current.den_label:Rules.Current.num_label;padNames[i].text=basePadNames[i];LayoutPadName(i);
                    padLabels[i].Text.fontSize=27;padLabels[i].Set("0");
                }
            }
            if(Rules.PadCount==2)assembledCaption.text=Rules.Current.num_label+" ÷ "+Rules.Current.den_label;
            RefreshPadAmounts();
            choiceProbePending=Application.absoluteURL.Contains("v3choices=1");
        }
        static int ChoiceDisplayLength(string value){
            int length=0;for(int i=0;i<value.Length;){
                if(i+6<value.Length&&value[i]=='{'&&value.Substring(i).StartsWith("{frac:")){
                    int end=value.IndexOf('}',i+6);if(end>i){int slash=value.IndexOf('/',i+6,end-i-6);length+=slash>i?Mathf.Max(slash-i-6,end-slash-1)+1:1;i=end+1;continue;}
                }
                length++;i++;
            }return length;
        }
        void RefreshPadAmounts(){
            if(Rules.Current==null||Rules.Current.Mode=="choice"||padLabels[0]==null)return;
            // During feedback the pad numbers are the authored case counts, not the
            // learner's last (possibly wrong or already-reduced) pour.
            if(fractionRevealLife>0&&Rules.Current.Mode=="fraction_parts")return;
            for(int i=0;i<Rules.PadCount;i++)if(lastPoured[i]!=Rules.Poured[i]||lastVisited[i]!=Rules.Visited[i]){
                lastPoured[i]=Rules.Poured[i];lastVisited[i]=Rules.Visited[i];padLabels[i].Set(Rules.Poured[i].ToString());
                string caption=basePadNames[i];
                if(padNames[i].text!=caption){padNames[i].text=caption;LayoutPadName(i);}padLabelRoots[i].localScale=Vector3.one*1.17f;
            }
            // A fraction with denominator 0 is not a meaningful intermediate
            // result. Keep the assembled fraction hidden until the learner has
            // actually poured a positive number of "all cases" coins.
            bool show=Rules.PadCount==2&&Rules.Poured[0]>0;
            assembledHud.gameObject.SetActive(show);
            assembledText.gameObject.SetActive(show);
            if(show&&(lastAssembledDen!=Rules.Poured[0]||lastAssembledNum!=Rules.Poured[1])){
                lastAssembledDen=Rules.Poured[0];lastAssembledNum=Rules.Poured[1];assembledMath.Set("{frac:"+Rules.Poured[1]+"/"+Rules.Poured[0]+"}");
            }
        }
        void RefreshHud(){
            waveText.text="S"+Rules.Stage+" · "+Rules.StageQuestion+"/10";
            coinsText.text="정답 "+Rules.Correct;
            coinsText.color=gold;
        }
        void UpdateBattleHud(){
            if(lastReds!=battle.Reds||lastBlues!=battle.Blues||lastKills!=battle.Kills){
                lastReds=battle.Reds;lastBlues=battle.Blues;lastKills=battle.Kills;
                armyText.SetText("격파 {0}   ·   수비대 {1}",lastKills,lastBlues);
            }
        }
        void ShowFeedback(bool ok,string s){
            feedbackPanel.gameObject.SetActive(true);questionText.Text.gameObject.SetActive(false);collapsedQuestion.Text.gameObject.SetActive(false);questionExpand.gameObject.SetActive(false);
            timerBadge.gameObject.SetActive(false);questionLip.gameObject.SetActive(false);
            feedbackSeal.text=ok?"정 답 !":"다시 생각해요";feedbackSeal.color=ok?MgfLook.Hex("#238757"):MgfLook.Hex("#bb503b");
            feedbackFull=s;feedbackSummary=s;feedbackCanExpand=false;feedbackExpanded=true;feedbackWasCorrect=ok;
            BeginFractionReveal();RenderFeedback();
            if(ok){bonusMath.Set(lastReward.kind==HyeopgokRewardKind.Tower?TowerKorean(lastReward.towerType)+" 낙하!":"코인 +"+lastReward.coinBonus);bonusLife=2.2f;}
            // After a miss, mark where the correct answer stood so the explanation line maps to a pad.
            answerMark=ok?-1:Rules.AnswerPad();answerRing.gameObject.SetActive(answerMark>=0);
            if(Rules.LastPad>=0)padLabelRoots[Rules.LastPad].localScale=Vector3.one*(ok?1.3f:.7f);
            if(!ok&&Rules.LastPad>=0)cracks[Rules.LastPad].gameObject.SetActive(true);
            if(!ok&&Rules.Current.Mode=="fraction_parts")for(int i=0;i<2;i++)cracks[i].gameObject.SetActive(true);
            RefreshPadAmounts();
        }
        void BeginFractionReveal(){
            EndFractionReveal(false);
            if(Rules.Current==null||Rules.Current.Mode!="fraction_parts"||Rules.Current.answerParts==null)return;
            fractionRevealDen=Rules.Current.answerParts.den;fractionRevealNum=Rules.Current.answerParts.num;
            fractionRevealLife=Mathf.Max(1.5f,feedbackLeft);fractionRevealReduceAt=fractionRevealLife-.72f;fractionRevealReduced=false;
            ApplyFractionReveal(false);
        }
        void ApplyFractionReveal(bool reduced){
            int g=PackItem.Gcd(fractionRevealNum,fractionRevealDen),n=reduced?fractionRevealNum/g:fractionRevealNum,d=reduced?fractionRevealDen/g:fractionRevealDen;
            for(int i=0;i<2;i++){
                padLabels[i].Text.color=gold;padLabels[i].Set((i==0?fractionRevealDen:fractionRevealNum).ToString());
                padLabelRoots[i].localScale=Vector3.one*1.25f;
            }
            assembledHud.gameObject.SetActive(true);assembledText.gameObject.SetActive(true);
            assembledCaption.text=reduced?(g>1?"약분한 확률":"이미 기약분수"):Rules.Current.num_label+" ÷ "+Rules.Current.den_label;
            assembledMath.Set("{frac:"+n+"/"+d+"}");
        }
        void EndFractionReveal(bool restore){
            fractionRevealLife=0;fractionRevealDen=fractionRevealNum=-1;fractionRevealReduced=false;
            for(int i=0;i<2;i++)if(padLabels[i]!=null){padLabels[i].Text.color=Color.white;lastPoured[i]=-1;}
            lastAssembledDen=lastAssembledNum=-1;
            if(Rules.Current!=null&&Rules.Current.Mode=="fraction_parts")assembledCaption.text=Rules.Current.num_label+" ÷ "+Rules.Current.den_label;
            if(restore)RefreshPadAmounts();
        }
        void RenderFeedback(){
            string shown=feedbackFull;feedbackRewardIcon.gameObject.SetActive(false);
            feedbackExpand.text="";
            feedbackText.Text.color=MgfLook.Hex("#49301c");feedbackText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-34,420);feedbackText.Set(shown);
            float textHeight=feedbackText.Text.preferredHeight+7;float iconSpace=0;
            SetScrollSize(Mathf.Max(feedbackWasCorrect?134:112,textHeight+61+iconSpace));
            feedbackText.Text.rectTransform.sizeDelta=new Vector2(questionPanel.sizeDelta.x-34,textHeight);
            feedbackText.Text.rectTransform.anchoredPosition=new Vector2(0,-47-iconSpace-textHeight*.5f);feedbackText.Set(shown);
        }
        void HideFeedback(){EndFractionReveal(false);timerBadge.gameObject.SetActive(true);questionLip.gameObject.SetActive(true);feedbackPanel.gameObject.SetActive(false);questionExpand.gameObject.SetActive(false);bonusLife=0;bonusMath.Set("");answerMark=-1;if(answerRing)answerRing.gameObject.SetActive(false);if(Rules.Current!=null)FitQuestion(Rules.Current.prompt);}
        void ShowEnd(bool won,bool fallen){
            cam.rect=new Rect(0,0,1,1);
            endRoot.gameObject.SetActive(true);playRoot.gameObject.SetActive(false);resultStarted=Time.unscaledTime;for(int i=0;i<4;i++)padPaint[i].gameObject.SetActive(false);if(tutorialArrow)tutorialArrow.gameObject.SetActive(false);
            endTitle.text="성문이 무너졌다";
            resultStars.text="최고 기록  "+v32BestQuestions+"문제";
            int rate=Rules.Attempts<=0?0:Mathf.RoundToInt(Rules.Correct*100f/Rules.Attempts);
            endDetail.text="버틴 문제  "+Rules.Attempts+"   ·   스테이지 "+Rules.Stage+"\n도달 맵  "+V32MapName(Rules.Map)+"   ·   격파 "+battle.Kills+"\n첫 시도 정답률  "+rate+"%\n\n성문 HP가 0이 되어 원정이 끝났습니다.";
        }
        void ResetTutorialProgress(){firstFractionTutorialShown=false;SetTutorial(0);}
        void SetTutorial(int kind){
            tutorialKind=kind;tutorialVisible=kind>0;tutorialStarted=Time.unscaledTime;
            bool visible=tutorialVisible,choiceHint=kind==3;
            if(tutorialTrail)tutorialTrail.gameObject.SetActive(false);if(tutorialArrow)tutorialArrow.gameObject.SetActive(visible&&!choiceHint);
            if(tutorialDot)tutorialDot.gameObject.SetActive(visible&&!choiceHint);
            if(tutorialLabel)tutorialLabel.gameObject.SetActive(visible);
            if(tutorialCoin)tutorialCoin.gameObject.SetActive(false);
            for(int i=0;i<tutorialRings.Length;i++)if(tutorialRings[i])tutorialRings[i].gameObject.SetActive(false);
        }
        void HideTutorial(){if(tutorialVisible)SetTutorial(0);}
        void LayoutUi(){
            if(!questionPanel)return;float cw=844f*Screen.width/Mathf.Max(1,Screen.height);bool wide=(float)Screen.width/Screen.height>1.2f;
            float width=wide?Mathf.Min(cw*.76f,1060):Mathf.Min(cw-16,520);
            questionPanel.anchorMin=questionPanel.anchorMax=new Vector2(.5f,1);questionPanel.sizeDelta=new Vector2(width,136);
            float pill=wide?1.25f:1;hpPill.localScale=wavePill.localScale=correctPill.localScale=treasuryPanel.localScale=Vector3.one*pill;
            hpPill.anchoredPosition=new Vector2(wide?83:62,-29);wavePill.anchoredPosition=new Vector2(wide?201:154,-29);correctPill.anchoredPosition=new Vector2(wide?304:231,-29);
            treasuryPanel.anchoredPosition=new Vector2(wide?-145:-118,-29);
            correctPill.anchorMin=correctPill.anchorMax=wide?new Vector2(0,1):new Vector2(1,0);
            correctPill.anchoredPosition=wide?new Vector2(304,-29):new Vector2(-51,26);
            pressureText.fontSize=wide?18:17;
            assembledHud.anchoredPosition=wide?new Vector2(-72,-230):new Vector2(-64,-298);
            assembledHud.localScale=Vector3.one*(wide?1.08f:1);
            questionExpand.gameObject.SetActive(false);
            LayoutTitleUi(cw,wide);
            for(int i=0;i<4;i++)if(padNames[i]!=null)LayoutPadName(i);
            if(Rules.Current!=null)FitQuestion(Rules.Current.prompt);
        }
        void AnimateUi(float dt){
            AnimateTitleUi(dt);
            AnimateV32Ui(dt);
            if(endRoot.gameObject.activeSelf)AnimateRewardCoins();
            if(!loaded)return;
            ctaRect.localScale=Vector3.Lerp(ctaRect.localScale,Vector3.one,dt*10);
            if(!playStarted)return;
            if(feedbackPanel.gameObject.activeSelf&&feedbackRewardIcon.gameObject.activeSelf){float pulse=1+.08f*Mathf.Sin(Time.unscaledTime*12);feedbackRewardIcon.localScale=Vector3.one*pulse;}
            if(Rules.Hp<previousActualHp){gateLossLife=1.15f;gateLossText.SetText("−{0}",previousActualHp-Rules.Hp);gateLossText.gameObject.SetActive(true);}previousActualHp=Rules.Hp;
            if(gateLossLife>0){gateLossLife-=dt;float t=1-gateLossLife/1.15f;gateLossText.rectTransform.anchoredPosition=hpPill.anchoredPosition+new Vector2(30,-12+t*45);gateLossText.alpha=Mathf.Clamp01(gateLossLife*2);if(gateLossLife<=0)gateLossText.gameObject.SetActive(false);}
            shownHp=Mathf.MoveTowards(shownHp,Rules.Hp,dt*70);
            int h=Mathf.RoundToInt(shownHp);
            if(h!=previousShownHp){hpText.SetText("성문 {0}",h);previousShownHp=h;}
            hpFill.rectTransform.sizeDelta=new Vector2(80*shownHp/Mathf.Max(1,Rules.MaxHp),4);
            timeFill.rectTransform.sizeDelta=new Vector2((questionPanel.sizeDelta.x-28)*Mathf.Clamp01(1-Rules.Elapsed/Rules.TimeLimit),3);
            int clock=Mathf.CeilToInt(Mathf.Max(0,Rules.TimeLimit-Rules.Elapsed));
            if(clock!=lastClock){lastClock=clock;pressureText.fontSize=clock>=100?13:(float)Screen.width/Screen.height>1.2f?18:17;pressureText.SetText("{0}",clock);pressureText.color=clock<=8?MgfLook.Hex("#b94e34"):MgfLook.Hex("#49301c");}
            if(lastTreasury!=battle.Coins){lastTreasury=battle.Coins;treasuryText.SetText("{0}",lastTreasury);}
            if(fractionRevealLife>0){
                fractionRevealLife=Mathf.Max(0,fractionRevealLife-dt);
                if(!fractionRevealReduced&&fractionRevealLife<=fractionRevealReduceAt){fractionRevealReduced=true;ApplyFractionReveal(true);}
                if(fractionRevealLife<=0)EndFractionReveal(true);
            }
            if(depositBubble.gameObject.activeSelf)depositBubble.gameObject.SetActive(false);
            RectTransform canvas=(RectTransform)MgfText.Canvas.transform;
            RefreshPadAmounts();
            bool rewardLabelActive=feedbackPanel.gameObject.activeSelf&&feedbackWasCorrect&&bonusLife>0;
            for(int i=0;i<4;i++){
                if(i>=Rules.PadCount)continue;
                padLabelRoots[i].gameObject.SetActive(!rewardLabelActive);
                Vector3 p=cam.WorldToScreenPoint(HyeopgokRules.Pads[i]+new Vector3(0,.05f,Rules.Current.Mode=="choice"?0:-.43f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,p,null,out Vector2 local);padLabelRoots[i].anchoredPosition=local;padScreenLocals[i]=local;
                padLabelRoots[i].localScale=Vector3.Lerp(padLabelRoots[i].localScale,Vector3.one,dt*8);
                float progress=Rules.CommandedPad==i?Rules.Dwell/HyeopgokRules.Hold:0;
                int count=progress>0?Mathf.CeilToInt(progress*32)+1:0;padFill[i].positionCount=count;
                for(int k=0;k<count;k++)padFill[i].SetPosition(k,PadEdge(i,Mathf.Min(progress,k/32f)));
            }
            RefreshUpgradePads();
            // Short role labels stay painted into each pad; the assembled value is
            // a fixed HUD badge so enemies and perspective cannot hide the fraction.
            Vector3 mid=Rules.PadCount==2?(HyeopgokRules.Pads[0]+HyeopgokRules.Pads[1])*.5f:HyeopgokRules.Pads[0];
            Vector3 middle=cam.WorldToScreenPoint(mid);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,middle,null,out Vector2 center);
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
            if(bonusLife>0){
                bonusLife-=dt;
                Vector2 rewardScreen=new Vector2(cam.pixelRect.center.x,cam.pixelRect.yMax-Mathf.Max(28,Screen.height*.025f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,rewardScreen,null,out Vector2 rewardLocal);bonusText.rectTransform.anchoredPosition=rewardLocal;
                if(bonusLife<=0)bonusMath.Set("");
            }
            if(choiceProbePending){choiceProbePending=false;PushV3QuestionProbe(Rules.Current.prompt);}
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
            bool choiceHint=tutorialKind==3;
            tutorialDot.gameObject.SetActive(!choiceHint);tutorialLabel.gameObject.SetActive(Time.unscaledTime-tutorialStarted<(choiceHint?2.8f:6f));if(tutorialArrow)tutorialArrow.gameObject.SetActive(!choiceHint);
            RectTransform canvas=(RectTransform)MgfText.Canvas.transform;
            Vector3 fromWorld=Rules.King,toWorld=HyeopgokRules.Pads[0];
            float along=Mathf.SmoothStep(0,1,Mathf.PingPong(Time.unscaledTime*1.15f,1));
            int ringPad=0;bool demoCoin=false;float coinDrop=0;
            string label="멈춰 서서 붓기";

            if(tutorialKind==3){
                fromWorld=toWorld=Rules.King;
                ringPad=-2;along=0;label="보기 패드를 탭하세요";
            }else if(tutorialKind==1){
                if(Rules.Coins==0&&Rules.Poured[0]==0){
                    toWorld=battle.CollectionPoint;ringPad=-1;label="전선 가까이에서 코인 모으기";
                }else{
                bool exit=Rules.Visited[0]&&Rules.Poured[0]>=Rules.Current.answerValue;
                toWorld=exit?HyeopgokRules.Exit:HyeopgokRules.Pads[0];ringPad=exit?-1:0;
                label=Rules.TutorialBlocked?"1닢 붓고 나오기":exit?"③ 밖에서 확정":Rules.Poured[0]>0?"② 한 번 더 탭":"① 멈춰 서기";
                }
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
            // The first choice question uses only a neutral speech bubble above
            // the king: no arrow, hand, sequence or pad-specific answer cue.
            Vector2 guideAt=TutorialPoint(Rules.King,canvas)+new Vector2(0,62);
            if(Rules.Current.Mode!="choice")guideAt.y=Mathf.Max(guideAt.y,-185);
            tutorialLabel.anchoredPosition=ClampWorldBubble(guideAt,145);
            if(tutorialKind!=2&&tutorialKind!=1)label="보기 패드를 탭하세요";
            tutorialCaption.fontSize=label.Length>12?12:14;
            if(tutorialCaption.text!=label)tutorialCaption.text=label;
            tutorialTrail.gameObject.SetActive(false);
            if(tutorialArrow&&!choiceHint){tutorialArrow.position=toWorld+new Vector3(0,1.5f+.12f*Mathf.Sin(Time.unscaledTime*5),0);tutorialArrow.rotation=Quaternion.Euler(0,-8,0);}
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

        void ApplyWorldViewport(float panelHeight){
            if(!cam||!playStarted)return;
            float reserved=Mathf.Clamp((70+panelHeight)/844f,.14f,.82f);
            cam.rect=new Rect(0,0,1,1-reserved);
        }

        void RefreshUpgradePads(){
            if(battle==null)return;RectTransform canvas=(RectTransform)MgfText.Canvas.transform;
            bool suppressLabels=feedbackPanel.gameObject.activeSelf&&feedbackWasCorrect&&bonusLife>0;
            for(int i=0;i<HyeopgokBattle.TowerCount;i++){
                int level=battle.TowerLevelAt(i);bool active=level>=0&&playStarted&&v32Phase=="question";
                if(upgradePadRoots[i])upgradePadRoots[i].gameObject.SetActive(active);
                if(upgradeLabelRoots[i])upgradeLabelRoots[i].gameObject.SetActive(active&&!suppressLabels);
                if(!active)continue;
                int left=battle.UpgradeRemainingAt(i),towerType=battle.TowerTypeAt(i);
                if(shownUpgradeLevel[i]!=level||shownUpgradeRemaining[i]!=left||shownUpgradeType[i]!=towerType){
                    string type=towerType==1?"대포":towerType==2?"마법":"석궁";upgradeLabels[i].text=level>=2?type+" Lv3 · MAX":type+" Lv"+(level+1)+" · 코인 "+left;
                    shownUpgradeLevel[i]=level;shownUpgradeRemaining[i]=left;shownUpgradeType[i]=towerType;
                }
                Vector3 screen=cam.WorldToScreenPoint(HyeopgokBattle.UpgradePads[i]);RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,null,out Vector2 local);
                if(!suppressLabels)PlaceUpgradeLabel(i,local);
                int total=level>=2?1:level==0?15:30;float progress=level>=2?1:1-left/(float)total;
                if(battle.ActiveUpgradePad==i&&battle.UpgradeDwell<HyeopgokRules.Hold)progress=battle.UpgradeDwell/HyeopgokRules.Hold;
                int count=progress>0?Mathf.CeilToInt(progress*24)+1:0;
                upgradePadFill[i].positionCount=count;for(int k=0;k<count;k++)upgradePadFill[i].SetPosition(k,UpgradePadEdge(i,Mathf.Min(progress,k/24f)));
            }
        }

        void PlaceUpgradeLabel(int index,Vector2 origin){
            RectTransform root=upgradeLabelRoots[index];bool placed=false;
            for(int candidate=0;candidate<UpgradeLabelOffsets.Length;candidate++){
                root.anchoredPosition=origin+UpgradeLabelOffsets[candidate];Rect rect=WorldLabelScreenRect(root);bool blocked=!ContainsRect(cam.pixelRect,rect,2);
                for(int i=0;i<4&&!blocked;i++)if(padLabelRoots[i].gameObject.activeInHierarchy)blocked=Expanded(WorldLabelScreenRect(padLabelRoots[i]),3).Overlaps(rect);
                for(int i=0;i<index&&!blocked;i++)if(upgradeLabelRoots[i].gameObject.activeInHierarchy)blocked=Expanded(WorldLabelScreenRect(upgradeLabelRoots[i]),3).Overlaps(rect);
                if(!blocked){placed=true;break;}
            }
            root.gameObject.SetActive(placed);
        }
        Rect WorldLabelScreenRect(RectTransform root){
            root.GetWorldCorners(debugCorners);float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
            for(int i=0;i<4;i++){Vector2 point=RectTransformUtility.WorldToScreenPoint(null,debugCorners[i]);minX=Mathf.Min(minX,point.x);minY=Mathf.Min(minY,point.y);maxX=Mathf.Max(maxX,point.x);maxY=Mathf.Max(maxY,point.y);}
            return Rect.MinMaxRect(minX,minY,maxX,maxY);
        }
        static Rect Expanded(Rect rect,float amount)=>new Rect(rect.xMin-amount,rect.yMin-amount,rect.width+amount*2,rect.height+amount*2);
        static bool ContainsRect(Rect outer,Rect inner,float margin)=>inner.xMin>=outer.xMin+margin&&inner.xMax<=outer.xMax-margin&&inner.yMin>=outer.yMin+margin&&inner.yMax<=outer.yMax-margin;

        void CaptureWorldLabelRects(float[] output,out int mask){
            mask=0;if(output==null||output.Length<32)return;
            for(int i=0;i<32;i++)output[i]=0;
            for(int i=0;i<4;i++)CaptureChoiceWorldLabel(i,output,ref mask);
            for(int i=0;i<HyeopgokBattle.TowerCount;i++)CaptureWorldLabel(upgradeLabelRoots[i],4+i,output,ref mask);
            if(bonusLife>0&&!string.IsNullOrEmpty(bonusText.text))CaptureWorldLabel(bonusText.rectTransform,7,output,ref mask);
        }
        void CaptureWorldLabel(RectTransform root,int index,float[] output,ref int mask){
            if(!root||!root.gameObject.activeInHierarchy)return;Rect rect=WorldLabelScreenRect(root);int at=index*4;
            output[at]=rect.x/Screen.width;output[at+1]=1-rect.yMax/Screen.height;output[at+2]=rect.width/Screen.width;output[at+3]=rect.height/Screen.height;mask|=1<<index;
        }
        void CaptureChoiceWorldLabel(int index,float[] output,ref int mask){
            if(!padLabelRoots[index].gameObject.activeInHierarchy||!padLabels[index].TryScreenBounds(out Rect rect))return;int at=index*4;
            output[at]=rect.x/Screen.width;output[at+1]=1-rect.yMax/Screen.height;output[at+2]=rect.width/Screen.width;output[at+3]=rect.height/Screen.height;mask|=1<<index;
        }

        [Serializable] sealed class V3Rect {public float x,y,width,height;}
        [Serializable] sealed class V3Glyph {public string @char,kind="body";public V3Rect rect;}
        [Serializable] sealed class V3Question {
            public string prompt;public V3Rect panelRect,bodyRect,renderedTextRect;public V3Glyph[] glyphs;
            public bool isFolded,hasEllipsis,isTruncated,isOverflowing;public int visibleCharacters,totalCharacters;
        }
        [Serializable] sealed class V3Choice {
            public string text;public V3Rect bodyRect,renderedTextRect;public V3Glyph[] glyphs;
            public bool isTruncated,isOverflowing,hasMissingGlyph;public int visibleCharacters,totalCharacters;
        }
        [Serializable] sealed class V3Layout {public int version=3,screenWidth,screenHeight;public V3Question question;public V3Choice[] choices;}
        readonly Vector3[] debugCorners=new Vector3[4];
        V3Rect ScreenRect(RectTransform rt){
            rt.GetWorldCorners(debugCorners);float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
            for(int i=0;i<4;i++){Vector2 p=RectTransformUtility.WorldToScreenPoint(null,debugCorners[i]);minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);float top=Screen.height-p.y;minY=Mathf.Min(minY,top);maxY=Mathf.Max(maxY,top);}
            return new V3Rect{x=minX,y=minY,width=maxX-minX,height=maxY-minY};
        }
        void PushV3QuestionProbe(string prompt){
            if(!Application.absoluteURL.Contains("artprobe=1"))return;
            Canvas.ForceUpdateCanvases();var body=questionText.Text;body.ForceMeshUpdate(true,true);var glyphs=new List<V3Glyph>(220);
            float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;int visible=0,expected=0;
            AppendProbeGlyphs(body,glyphs,ref minX,ref minY,ref maxX,ref maxY,ref visible,ref expected,true);
            var fractions=body.GetComponentsInChildren<TextMeshProUGUI>(true);for(int i=0;i<fractions.Length;i++)if(fractions[i]!=body&&fractions[i].gameObject.activeInHierarchy)AppendProbeGlyphs(fractions[i],glyphs,ref minX,ref minY,ref maxX,ref maxY,ref visible,ref expected,false);
            if(visible==0){minX=minY=maxX=maxY=0;}
            var q=new V3Question{prompt=prompt,panelRect=ScreenRect(questionPanel),bodyRect=ScreenRect(body.rectTransform),renderedTextRect=new V3Rect{x=minX,y=minY,width=maxX-minX,height=maxY-minY},glyphs=glyphs.ToArray(),
                isFolded=false,hasEllipsis=false,isTruncated=visible<expected,isOverflowing=body.isTextOverflowing,visibleCharacters=visible,totalCharacters=expected};
            V3Choice[] choices=Application.absoluteURL.Contains("v3choices=1")?BuildV3ChoiceProbes():null;
            HYEOPGOK_PushV3Layout(JsonUtility.ToJson(new V3Layout{screenWidth=Screen.width,screenHeight=Screen.height,question=q,choices=choices}));
        }
        V3Choice[] BuildV3ChoiceProbes(){
            var result=new V3Choice[4];
            for(int i=0;i<4;i++){
                var source=padLabels[i].Text;source.ForceMeshUpdate(true,true);var glyphs=new List<V3Glyph>(32);
                float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;int visible=0,expected=0;bool missing=false;
                AppendProbeGlyphs(source,glyphs,ref minX,ref minY,ref maxX,ref maxY,ref visible,ref expected,true,ref missing);
                var fractions=source.GetComponentsInChildren<TextMeshProUGUI>(true);for(int j=0;j<fractions.Length;j++)if(fractions[j]!=source&&fractions[j].gameObject.activeInHierarchy)AppendProbeGlyphs(fractions[j],glyphs,ref minX,ref minY,ref maxX,ref maxY,ref visible,ref expected,false,ref missing);
                if(visible==0){minX=minY=maxX=maxY=0;}
                result[i]=new V3Choice{text=Rules.Choices[i],bodyRect=ScreenRect(source.rectTransform),renderedTextRect=new V3Rect{x=minX,y=minY,width=maxX-minX,height=maxY-minY},glyphs=glyphs.ToArray(),
                    isTruncated=visible<expected,isOverflowing=source.isTextOverflowing,hasMissingGlyph=missing,visibleCharacters=visible,totalCharacters=expected};
            }return result;
        }
        void AppendProbeGlyphs(TextMeshProUGUI source,List<V3Glyph> glyphs,ref float minX,ref float minY,ref float maxX,ref float maxY,ref int visible,ref int expected,bool skipTransparent){bool missing=false;AppendProbeGlyphs(source,glyphs,ref minX,ref minY,ref maxX,ref maxY,ref visible,ref expected,skipTransparent,ref missing);}
        void AppendProbeGlyphs(TextMeshProUGUI source,List<V3Glyph> glyphs,ref float minX,ref float minY,ref float maxX,ref float maxY,ref int visible,ref int expected,bool skipTransparent,ref bool missing){
            source.ForceMeshUpdate(true,true);var info=source.textInfo;
            for(int i=0;i<info.characterCount;i++){
                var c=info.characterInfo[i];bool ink=!char.IsWhiteSpace(c.character)&&!char.IsControl(c.character)&&(!skipTransparent||c.color.a>0);if(!ink)continue;expected++;if(!c.isVisible)continue;visible++;
                if(c.character=='□'||c.character=='�'||c.textElement==null||c.textElement.unicode!=c.character)missing=true;
                Vector3 bl=source.transform.TransformPoint(c.bottomLeft),tr=source.transform.TransformPoint(c.topRight);Vector2 a=RectTransformUtility.WorldToScreenPoint(null,bl),b=RectTransformUtility.WorldToScreenPoint(null,tr);
                float x=Mathf.Min(a.x,b.x),y=Screen.height-Mathf.Max(a.y,b.y),w=Mathf.Abs(b.x-a.x),h=Mathf.Abs(b.y-a.y);minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x+w);maxY=Mathf.Max(maxY,y+h);
                if(glyphs.Count<220)glyphs.Add(new V3Glyph{@char=c.character.ToString(),rect=new V3Rect{x=x,y=y,width=w,height=h}});
            }
        }
    }
}
