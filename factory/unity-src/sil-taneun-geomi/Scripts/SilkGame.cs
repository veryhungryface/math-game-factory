// 실 타는 거미 — 타이틀/무손실 연습/두 배달 길/실패 상태와 유일한 답 입력.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.SilTaneunGeomi
{
    public partial class SilkGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int selectedLength=1;
            public int routeIndex;
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int firstAttemptFailures;
            public int attemptIndex;
            public int pointerVersion;
            public int activeLane;
            public int remainingSec=120;
            public string problemId="";
            public string prompt="";
            public string targetSegment="";
            public string misconceptionId="";
            public string lane0Problem="";
            public string lane1Problem="";
            public bool lane0First=true;
            public bool lane1First=true;
            public bool onboarding;
            public bool frozen;
        }

        sealed class Lane
        {
            public SilkProblem problem;
            public int selected=1;
            public bool first=true;
            public float waitLeft=SilkRules.QueueSeconds;
            public bool present;
        }

        enum Phase { Title, Practice, Playing, Reveal, End }

        readonly State st=new State();
        readonly List<MgfProblem> bank=new List<MgfProblem>();
        readonly Lane[] lanes={new Lane(),new Lane()};
        Phase phase=Phase.Title;
        List<SilkProblem> deck;
        SilkProblem current;
        System.Random rng;
        int runSerial,nextDeck,activeLane,firstAnswerOffset,submittedLength;
        bool dragging,dragMoved,dragFromTrack,titlePressed,revealCorrect,revealWasPractice,revealFirst;
        Vector2 dragStart;
        int dragStartLength;
        float revealClock,idleGuide,runLeft;
        string endReason="";

        void Awake()
        {
            MgfLook.Quality(36f);
            firstAnswerOffset=((Environment.TickCount&0x7fffffff)%6)*4;
            bank.AddRange(SilkRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb=new StringBuilder("실타는거미실을늘려길을이어라중학교2학년평행선과선분길이의비라일락온실꽃가루배달안전고리부분전체중점연결정리거미줄길이는숫자를기준으로실끝을잡아눈금까지끌고놓으시오첫시도성공꽃이피었다다시계산게임종료재도전");
            sb.Append("△ABC에서DE∥BCl∥m∥nADDBAEECDMNBCCM실전연습오른쪽위아래구간서로다른두직선잘못세운비례식고쳐계산하시오");
            sb.Append("구할선분좌우로당겨맞추시오진주실은아래눈금바뀐뒤놓으면제출된다필요한보다짧았다길었다냈다?AM=MBAN=NC→");
            for(int i=0;i<bank.Count;i++){sb.Append(bank[i].prompt);sb.Append(bank[i].answer);sb.Append(bank[i].unitConcept);}
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt=Mathf.Min(.05f,Time.deltaTime);
            HandleInput();
            if(phase==Phase.Practice)
            {
                idleGuide+=dt;
                if(idleGuide>=8f){idleGuide=0;ReplayGuide(true);}
            }
            else if(phase==Phase.Playing)
            {
                runLeft=Mathf.Max(0,runLeft-dt);
                // 본판에서도 손이 멈추면 실 끝 유령 손가락을 다시 띄운다(연습과 같은 안내).
                idleGuide+=dt;if(idleGuide>=7f){idleGuide=0;ShowGuide(2.6f);}
                if(runLeft<=0){EndRun("time");}
                else UpdateQueueTimers(dt);
            }
            else if(phase==Phase.Reveal)
            {
                revealClock+=dt;
                if(revealClock>=(revealCorrect?1.72f:1.48f))FinishReveal();
            }
            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase=Phase.Title;endReason="";deck=null;current=null;dragging=false;titlePressed=false;
            st.score=0;st.lives=SilkRules.StartLives;st.level=1;st.solved=0;st.routeIndex=0;st.combo=0;
            st.firstAttemptTotal=0;st.firstAttemptCorrect=0;st.firstAttemptFailures=0;st.attemptIndex=0;
            st.selectedLength=1;st.activeLane=0;st.remainingSec=120;st.problemId="";st.prompt="";
            st.targetSegment="";st.misconceptionId="";st.onboarding=false;st.frozen=false;
            for(int i=0;i<2;i++)ClearLane(i);
            ResetWorldForRun();SetScreen();MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            rng=new System.Random(unchecked(Environment.TickCount^runSerial*104729^firstAnswerOffset*7919));
            int firstAnswer=SilkRules.BalancedFirstAnswer(runSerial,firstAnswerOffset);
            deck=SilkRules.RunDeck(rng,firstAnswer);nextDeck=0;activeLane=0;runLeft=SilkRules.RunSeconds;
            st.score=0;st.lives=SilkRules.StartLives;st.level=1;st.solved=0;st.routeIndex=0;st.combo=0;
            st.firstAttemptTotal=0;st.firstAttemptCorrect=0;st.firstAttemptFailures=0;st.attemptIndex=0;
            st.misconceptionId="";st.remainingSec=120;endReason="";dragging=false;revealClock=0;
            for(int i=0;i<2;i++)ClearLane(i);
            ResetWorldForRun();
        }

        void ClearLane(int index)
        {
            lanes[index].problem=null;lanes[index].selected=1;lanes[index].first=true;
            lanes[index].waitLeft=SilkRules.QueueSeconds+(index*6);lanes[index].present=false;
        }

        void FillLane(int index)
        {
            if(nextDeck>=deck.Count){ClearLane(index);return;}
            var l=lanes[index];l.problem=deck[nextDeck++];l.present=true;l.first=true;
            l.waitLeft=SilkRules.QueueSeconds+(index*6);
            int start=1+rng.Next(SilkRules.MaxLength);
            if(start==l.problem.answer)start=start==SilkRules.MaxLength?start-1:start+1;
            l.selected=start;
        }

        void StartPractice()
        {
            ResetRun();phase=Phase.Practice;st.onboarding=true;st.frozen=true;
            var l=lanes[0];l.problem=SilkRules.Practice();l.present=true;l.first=true;l.selected=1;
            activeLane=0;current=l.problem;SetSelected(1,false);idleGuide=0;
            BindCurrent();ResetWorldForProblem(current);RefreshProblemUi();SetScreen();ReplayGuide(false);
            MgfSfx.Play("whoosh",.35f);MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();phase=Phase.Playing;st.onboarding=false;st.frozen=false;
            FillLane(0);SelectLane(0,false);SetScreen();MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score=0;st.solved=0;st.routeIndex=0;st.combo=0;st.firstAttemptTotal=0;st.firstAttemptCorrect=0;
            st.firstAttemptFailures=0;st.attemptIndex=0;st.lives=SilkRules.StartLives;st.misconceptionId="";
            st.onboarding=false;st.frozen=false;phase=Phase.Playing;nextDeck=0;idleGuide=0;
            for(int i=0;i<2;i++)ClearLane(i);
            FillLane(0);SelectLane(0,false);SetScreen();
            ShowGuide(4.5f);ShowToast("실전 · 구할 선분의 실을 좌우로 당겨 길이를 맞추시오");
        }

        void BindCurrent()
        {
            current=lanes[activeLane].problem;
            st.activeLane=activeLane;st.selectedLength=lanes[activeLane].selected;
            st.problemId=current==null?"":current.id;st.prompt=current==null?"":current.prompt;
            st.targetSegment=current==null?"":current.target;st.level=current==null?1:Math.Max(1,current.band);
            st.routeIndex=st.solved+1;
            st.lane0Problem=lanes[0].present?lanes[0].problem.id:"";
            st.lane1Problem=lanes[1].present?lanes[1].problem.id:"";
            st.lane0First=lanes[0].first;st.lane1First=lanes[1].first;
        }

        void SelectLane(int index,bool notify)
        {
            if(index<0||index>1||!lanes[index].present)return;
            activeLane=index;BindCurrent();SetSelected(lanes[index].selected,false);
            ResetWorldForProblem(current);RefreshProblemUi();RefreshLaneUi();
            if(notify){SpiderWave(index);MgfSfx.Play("tap",.18f);MgfBridge.NotifyChanged();}
        }

        void SetSelected(int value,bool notify)
        {
            value=Math.Max(SilkRules.MinLength,Math.Min(SilkRules.MaxLength,value));
            int before=st.selectedLength;st.selectedLength=value;
            if(lanes[activeLane].present)lanes[activeLane].selected=value;
            SetSilkLength(value);RefreshLengthUi(before!=value);
            if(notify)MgfBridge.NotifyChanged();
        }

        void UpdateQueueTimers(float dt)
        {
            for(int i=0;i<2;i++)
            {
                if(!lanes[i].present||i==activeLane||st.solved<3)continue;
                lanes[i].waitLeft-=dt;
                if(lanes[i].waitLeft<=0&&lanes[i].first)
                {
                    lanes[i].first=false;lanes[i].waitLeft=SilkRules.QueueSeconds;
                    st.firstAttemptTotal++;st.firstAttemptFailures++;st.lives=Math.Max(0,st.lives-1);
                    st.misconceptionId="queue_timeout";DropSafetyHook();ShowToast("기다리던 거미의 안전 고리가 떨어졌다");
                    BindCurrent();MgfBridge.NotifyChanged();
                    if(st.lives<=0||st.firstAttemptFailures>=3){EndRun("mastery");return;}
                }
            }
        }

        void SubmitSilk()
        {
            if((phase!=Phase.Practice&&phase!=Phase.Playing)||current==null)return;
            st.remainingSec=Mathf.CeilToInt(runLeft);submittedLength=st.selectedLength;
            revealWasPractice=phase==Phase.Practice;revealCorrect=SilkRules.IsCorrect(current,st.selectedLength);
            revealFirst=lanes[activeLane].first;
            if(revealWasPractice)
            {
                if(revealCorrect){st.score=100;st.solved=1;}
                else st.misconceptionId=SilkRules.MisconceptionId(current,st.selectedLength);
            }
            else
            {
                if(lanes[activeLane].first)
                {
                    st.firstAttemptTotal++;
                    if(revealCorrect)st.firstAttemptCorrect++;
                    else st.firstAttemptFailures++;
                    lanes[activeLane].first=false;
                }
                if(revealCorrect)
                {
                    st.combo++;st.score+=revealFirst?150+st.combo*15:65;st.solved++;
                }
                else
                {
                    st.combo=0;st.lives=Math.Max(0,st.lives-1);st.attemptIndex++;
                    st.misconceptionId=SilkRules.MisconceptionId(current,st.selectedLength);
                }
            }
            phase=Phase.Reveal;revealClock=0;dragging=false;BindCurrent();
            BeginRevealVisual(revealCorrect,revealFirst);SetScreen();MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if(phase!=Phase.Reveal)return;
            CompleteRevealVisual(revealCorrect);
            if(revealWasPractice)
            {
                if(revealCorrect)BeginMainAfterPractice();
                else
                {
                    phase=Phase.Practice;lanes[0].selected=1;SetSelected(1,false);
                    ResetWorldForProblem(current);RefreshProblemUi();ReplayGuide(true);
                }
            }
            else if(!revealCorrect)
            {
                if(st.lives<=0||st.firstAttemptFailures>=3)EndRun(st.lives<=0?"hooks":"mastery");
                else
                {
                    phase=Phase.Playing;int start=1+rng.Next(SilkRules.MaxLength);
                    if(start==current.answer)start=start==24?23:start+1;
                    SetSelected(start,false);ResetWorldForProblem(current);RefreshProblemUi();
                    ShowWrongReason(st.misconceptionId);
                }
            }
            else
            {
                lanes[activeLane].present=false;
                if(st.solved>=SilkRules.TargetRoutes)
                {
                    if(st.firstAttemptCorrect>=7)EndRun("clear"); else EndRun("mastery");
                }
                else
                {
                    phase=Phase.Playing;
                    if(st.solved<3){FillLane(0);SelectLane(0,false);}
                    else
                    {
                        FillLane(activeLane);
                        int other=1-activeLane;
                        if(!lanes[other].present)FillLane(other);
                        if(lanes[activeLane].present)SelectLane(activeLane,false);else SelectLane(other,false);
                    }
                }
            }
            SetScreen();MgfBridge.NotifyChanged();
        }

        void FinishPendingReveal(){if(phase==Phase.Reveal){revealClock=99;FinishReveal();}}

        void EndRun(string reason)
        {
            if(phase==Phase.End)return;
            phase=Phase.End;dragging=false;endReason=reason;st.frozen=true;
            if(reason=="time")st.remainingSec=0;else st.remainingSec=Mathf.CeilToInt(runLeft);
            int best=PlayerPrefs.GetInt("sil-taneun-geomi.best",0);
            if(st.firstAttemptCorrect>best){PlayerPrefs.SetInt("sil-taneun-geomi.best",st.firstAttemptCorrect);PlayerPrefs.Save();}
            ShowEnd(reason);SetScreen();MgfSfx.Play(reason=="clear"?"win":"lose",.55f);MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            if(MgfPointer.Down)
            {
                st.pointerVersion++;SpawnTapRipple(MgfPointer.Position);
                if(phase==Phase.Title)
                {
                    titlePressed=IsTitleStartZone(MgfPointer.Position);
                    if(titlePressed){BeginTitlePress();MgfSfx.Play("tap",.24f);}else RefuseInput("꽃가루 주머니를 눌러 배달을 시작하시오");
                    MgfBridge.NotifyChanged();return;
                }
                if(phase==Phase.End){StartPractice();return;}
                if(phase==Phase.Reveal){RefuseInput("거미들이 꽃에 도착할 때까지 잠깐 기다리시오");MgfBridge.NotifyChanged();return;}
                if(phase!=Phase.Practice&&phase!=Phase.Playing)return;
                int lane=LaneFromPointer(MgfPointer.Position);
                if(lane>=0&&lanes[lane].present){SelectLane(lane,true);return;}
                // 본판 입력 규칙(연습도 같은 띠·같은 실 끝을 쓴다).
                // ① 계량 실 띠(눈금 카드와 그 위아래 여백): 누른 x 가 곧 눈금 → 놓으면 제출.
                // ② 그 밖(문제 카드·모식도·온실): 좌우로 당기면 실 끝이 상대적으로 움직이고,
                //    움직이지 않은 탭은 제출하지 않고 실 끝에 유령 손가락을 다시 띄운다.
                // 연습만은 첫 탭을 관대하게 받아 화면 어디를 눌러도 길이가 바뀐다(무손실).
                dragging=true;dragMoved=false;idleGuide=0;dragStart=MgfPointer.Position;dragStartLength=st.selectedLength;
                dragFromTrack=phase==Phase.Practice||IsTrackZone(MgfPointer.Position);
                if(dragFromTrack)
                {
                    int before=st.selectedLength;SetSelected(LengthFromPointer(MgfPointer.Position),true);dragMoved=before!=st.selectedLength;
                }
                HandleGrab(true);MgfSfx.Play("tap",.18f);
            }
            if(MgfPointer.Held&&titlePressed){UpdateTitlePress(MgfPointer.Position);}
            if(MgfPointer.Up&&titlePressed){titlePressed=false;EndTitlePress();StartPractice();return;}
            if(MgfPointer.Held&&dragging)
            {
                int before=st.selectedLength,next;
                if(dragFromTrack)
                {
                    next=LengthFromPointer(MgfPointer.Position);
                    if(Vector2.Distance(MgfPointer.Position,dragStart)>6f&&next==dragStartLength)
                        next=Mathf.Clamp(dragStartLength+(MgfPointer.Position.x>=dragStart.x?1:-1),1,24);
                }
                else next=dragStartLength+TicksBetween(dragStart,MgfPointer.Position);
                SetSelected(next,before!=next);if(before!=next){dragMoved=true;TickSilk();}
                if(dragMoved)UpdateSilkTrail();
            }
            if(MgfPointer.Up&&dragging)
            {
                dragging=false;HandleGrab(false);
                if(dragFromTrack&&phase!=Phase.Practice&&IsReleaseCancelled(MgfPointer.Position)){RefuseInput("눈금 가까이에서 놓으면 길이가 제출된다");ResetSilkTrail();MgfBridge.NotifyChanged();return;}
                if(!dragMoved)
                {
                    RefuseInput(dragFromTrack?"실 끝을 다른 눈금까지 끈 뒤 놓으시오":"진주 실 끝을 좌우로 당겨 "+(current!=null?current.target:"선분")+" 길이를 맞추시오");
                    MgfBridge.NotifyChanged();return;
                }
                SubmitSilk();
            }
        }

        public void TestStart(){FinishPendingReveal();StartRunDirect();}
        public void TestAnswerCorrect()
        {
            FinishPendingReveal();if(phase!=Phase.Playing||current==null)return;
            SetSelected(current.answer,false);SubmitSilk();
        }
        public void TestAnswerWrong()
        {
            FinishPendingReveal();if(phase!=Phase.Playing||current==null)return;
            int wrong=current.wrongA>0?current.wrongA:(current.answer==24?23:current.answer+1);
            if(wrong==current.answer)wrong=current.answer==1?2:1;
            SetSelected(wrong,false);SubmitSilk();
        }
        public string StateJson()
        {
            st.phase=phase==Phase.Title?"title":phase==Phase.End?(endReason=="clear"?"clear":"gameover"):"playing";
            st.onboarding=phase==Phase.Practice;st.frozen=phase==Phase.Practice||phase==Phase.Reveal||phase==Phase.End;
            // remainingSec는 마지막 학습 사건의 스냅샷이다. 매 프레임 변하는 값을 상태에
            // 넣으면 QA input.real의 무입력 기준선을 오염시키므로 UI 타이머는 runLeft를 직접 그린다.
            st.lives=Math.Max(0,st.lives);
            BindCurrent();return JsonUtility.ToJson(st);
        }
        public string ProblemBankJson()=>MgfJson.Bank(bank);
    }
}
