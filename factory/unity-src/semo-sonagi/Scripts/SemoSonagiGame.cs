// 세모 소나기 — 상태 머신과 실제 한 획 포인터 입력.
// 답은 네 우산 중 두 중심을 한 번의 연속 스와이프로 통과하고 놓는 경로로만 제출한다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.SemoSonagi
{
    public partial class SemoSonagiGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int attemptIndex;
            public int correctMask;
            public int touchedMask;
            public int pointerVersion;
            public int combo;
            public bool onboarding;
            public bool dragging;
            public bool targetImpossible;
            public string problemId="";
            public string prompt="";
            public string misconceptionId="";
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }
        readonly State st=new State();
        readonly List<MgfProblem> bank=new List<MgfProblem>();
        GamePhase phase=GamePhase.Title;
        List<SonagiProblem> deck;
        SonagiProblem current;
        int runSerial;
        bool firstAttempt;
        bool revealCorrect;
        bool revealPractice;
        int revealMask;
        float sessionLeft,waveLeft,revealClock,idleGuide;
        string endReason="";
        Vector2 lastPointer;
        float swipeLength;

        void Awake()
        {
            MgfLook.Quality(32f);
            string poolError;
            if(!SemoSonagiRules.ValidatePool(out poolError))Debug.LogError("[세모 소나기] 문제 풀 검증 실패: "+poolError);
            string deckError;
            if(!SemoSonagiRules.ValidateRunDecks(out deckError))Debug.LogError("[세모 소나기] 화면 묶음 검증 실패: "+deckError);
            bank.AddRange(SemoSonagiRules.BuildBank());
            BuildWorld();BuildUi();Prewarm();ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb=new StringBuilder("세모소나기직각삼각형우산쓸어펼치기피타고라스정리중학교2학년세변정사각형넓이가장긴변보수천빗물정원달팽이첫판연습정답오답검산");
            for(int i=0;i<bank.Count;i++){sb.Append(bank[i].prompt);sb.Append(bank[i].answer);sb.Append(bank[i].unitConcept);}
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt=Mathf.Min(.05f,Time.deltaTime);
            UpdateLayoutIfNeeded();
            HandleInput();
            if(phase==GamePhase.Practice)
            {
                idleGuide+=dt;if(idleGuide>=8f){idleGuide=0;BoostGuide();}
            }
            else if(phase==GamePhase.Playing)
            {
                sessionLeft-=dt;waveLeft-=dt;
                if(sessionLeft<=0)EndRun("timer");
                else if(waveLeft<=0)ResolveMask(0,true);
            }
            else if(phase==GamePhase.Reveal)
            {
                revealClock+=dt;
                if(revealClock>=(revealCorrect?2.25f:1.85f))FinishReveal();
            }
            UpdateWorld(dt);UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase=GamePhase.Title;deck=null;current=null;endReason="";
            st.score=0;st.lives=SemoSonagiRules.StartLives;st.level=1;st.solved=0;st.phase="title";
            st.firstAttemptTotal=0;st.firstAttemptCorrect=0;st.attemptIndex=0;st.correctMask=0;st.touchedMask=0;
            st.pointerVersion=0;st.combo=0;st.onboarding=false;st.dragging=false;st.targetImpossible=false;
            st.problemId="";st.prompt="";st.misconceptionId="";
            ResetWorldForRun();SetScreen();MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;deck=SemoSonagiRules.RunDeck(runSerial);endReason="";
            shownSecond=-1;
            st.score=0;st.lives=SemoSonagiRules.StartLives;st.level=1;st.solved=0;st.phase="playing";
            st.firstAttemptTotal=0;st.firstAttemptCorrect=0;st.attemptIndex=0;st.correctMask=0;st.touchedMask=0;
            st.combo=0;st.onboarding=false;st.dragging=false;st.targetImpossible=false;st.misconceptionId="";
            sessionLeft=SemoSonagiRules.SessionSeconds;ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun();phase=GamePhase.Practice;st.onboarding=true;current=SemoSonagiRules.Practice();firstAttempt=true;idleGuide=0;
            BeginProblem();SetScreen();BoostGuide();MgfSfx.Play("whoosh",.28f);MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();phase=GamePhase.Playing;st.onboarding=false;NextProblem();SetScreen();MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            shownSecond=-1;
            st.score=0;st.lives=SemoSonagiRules.StartLives;st.level=1;st.solved=0;st.firstAttemptTotal=0;st.firstAttemptCorrect=0;
            st.attemptIndex=0;st.combo=0;st.onboarding=false;st.targetImpossible=false;st.misconceptionId="";
            sessionLeft=SemoSonagiRules.SessionSeconds;phase=GamePhase.Playing;NextProblem();SetScreen();
        }

        void NextProblem()
        {
            if(st.solved>=SemoSonagiRules.TargetSolved){EndRun(st.firstAttemptCorrect>=SemoSonagiRules.RequiredFirstCorrect?"clear":"mastery");return;}
            current=deck[st.solved];firstAttempt=true;st.problemId=current.id;st.prompt=current.prompt;st.correctMask=current.correctMask;
            st.touchedMask=0;st.attemptIndex=0;st.misconceptionId="";st.level=current.band;
            BeginProblem();MgfBridge.NotifyChanged();
        }

        float WaveSeconds(SonagiProblem p){return p!=null&&p.band<=1?18f:20f;}

        void BeginProblem()
        {
            st.dragging=false;st.touchedMask=0;swipeLength=0;waveLeft=phase==GamePhase.Practice?999f:WaveSeconds(current);
            ResetWorldForProblem(current,phase==GamePhase.Practice?2:4);RefreshProblemUi();
        }

        int FirstFailureCount(){return st.firstAttemptTotal-st.firstAttemptCorrect;}

        void ResolveMask(int mask,bool timedOut=false)
        {
            if((phase!=GamePhase.Practice&&phase!=GamePhase.Playing)||current==null)return;
            revealPractice=phase==GamePhase.Practice;revealMask=mask;revealCorrect=SemoSonagiRules.MaskIsCorrect(current,mask);
            st.touchedMask=mask;st.dragging=false;
            if(!revealPractice)
            {
                st.attemptIndex++;
                if(firstAttempt)
                {
                    st.firstAttemptTotal++;if(revealCorrect)st.firstAttemptCorrect++;firstAttempt=false;
                }
                if(revealCorrect)
                {
                    st.solved++;st.combo++;st.score+=100+current.band*25+Mathf.Min(4,st.combo)*15;
                    st.misconceptionId="";
                }
                else
                {
                    st.lives=Math.Max(0,st.lives-1);st.combo=0;
                    st.misconceptionId=timedOut?"timeout":MisconceptionForMask(mask);
                    st.targetImpossible=FirstFailureCount()>=2;
                }
            }
            else st.misconceptionId=revealCorrect?"":"near_square";
            phase=GamePhase.Reveal;revealClock=0;BeginRevealVisual(revealCorrect,revealPractice,mask,st.misconceptionId);
            SetScreen();MgfBridge.NotifyChanged();
        }

        string MisconceptionForMask(int mask)
        {
            if(mask==0)return "empty_sweep";
            for(int i=0;i<4;i++)if((mask&(1<<i))!=0&&!SemoSonagiRules.IsRight(current.mode,current.candidates[i]))
                return string.IsNullOrEmpty(current.candidates[i].misconceptionId)?"silhouette_guess":current.candidates[i].misconceptionId;
            if(BitCount(mask)!=2)return "cardinality";
            return "hypotenuse_by_position";
        }

        static int BitCount(int mask){int n=0;while(mask!=0){n+=mask&1;mask>>=1;}return n;}

        void FinishReveal()
        {
            if(phase!=GamePhase.Reveal)return;
            CompleteRevealVisual(revealCorrect,revealMask);
            if(revealPractice)
            {
                if(revealCorrect)BeginMainAfterPractice();
                else{phase=GamePhase.Practice;BeginProblem();BoostGuide();}
            }
            else if(!revealCorrect)
            {
                if(st.lives<=0)EndRun("cloth");
                else if(st.targetImpossible)EndRun("mastery");
                else{phase=GamePhase.Playing;BeginProblem();ShowWrongReason(st.misconceptionId);}
            }
            else{phase=GamePhase.Playing;NextProblem();}
            SetScreen();MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if(phase==GamePhase.End)return;
            endReason=reason;phase=GamePhase.End;st.onboarding=false;st.dragging=false;
            st.phase=reason=="clear"?"clear":"gameover";ShowEndVisual(reason);SetScreen();MgfBridge.NotifyChanged();
            MgfSfx.Play(reason=="clear"?"win":"lose",.48f);
        }

        void HandleInput()
        {
            if(phase==GamePhase.Title)
            {
                if(MgfPointer.Down){st.pointerVersion++;PulseTitleUmbrella();MgfBridge.NotifyChanged();StartPractice();}
                return;
            }
            if(phase==GamePhase.End)
            {
                if(MgfPointer.Down){st.pointerVersion++;StartPractice();}
                return;
            }
            if(phase==GamePhase.Reveal)
            {
                if(MgfPointer.Down){st.pointerVersion++;RippleAtScreen(MgfPointer.Position,"잠깐, 빗물 식을 확인해요");ReactSnails();MgfBridge.NotifyChanged();}
                return;
            }
            if(phase!=GamePhase.Practice&&phase!=GamePhase.Playing)return;

            if(MgfPointer.Down)
            {
                st.pointerVersion++;st.dragging=true;st.touchedMask=0;swipeLength=0;lastPointer=MgfPointer.Position;idleGuide=0;
                AddTouches(lastPointer,lastPointer);BeginSwipeVisual(lastPointer);MgfSfx.Play("tap",.18f);MgfBridge.NotifyChanged();
            }
            if(st.dragging&&MgfPointer.Held)
            {
                Vector2 now=MgfPointer.Position;swipeLength+=Vector2.Distance(lastPointer,now);AddTouches(lastPointer,now);MoveSwipeVisual(now);lastPointer=now;
            }
            if(st.dragging&&MgfPointer.Up)
            {
                Vector2 now=MgfPointer.Position;swipeLength+=Vector2.Distance(lastPointer,now);AddTouches(lastPointer,now);st.dragging=false;EndSwipeVisual();
                int mask=st.touchedMask;
                if(mask==0||swipeLength<24f)
                {
                    RippleAtScreen(now,mask==0?"우산 천 위를 쓸어라":"톡 누르지 말고 천을 가로질러 쓸어라");
                    BoostGuide();FoldTappedCanopy(mask);MgfSfx.Play("wrong",.10f);MgfBridge.NotifyChanged();
                }
                else ResolveMask(mask,false);
            }
        }

        void AddTouches(Vector2 from,Vector2 to)
        {
            int count=phase==GamePhase.Practice?2:4;float r=TouchRadiusPixels();
            for(int i=0;i<count;i++)if(SegmentCircle(from,to,candidateScreen[i],r))
            {
                int before=st.touchedMask;st.touchedMask|=1<<i;
                if(before!=st.touchedMask){TouchCanopyVisual(i);MgfSfx.Play("pop",.12f);MgfBridge.NotifyChanged();}
            }
        }

        float TouchRadiusPixels(){return Mathf.Clamp(Mathf.Min(Screen.width,Screen.height)*.115f,42f,74f);}

        static bool SegmentCircle(Vector2 a,Vector2 b,Vector2 c,float radius)
        {
            Vector2 ab=b-a;float d=ab.sqrMagnitude;
            float t=d<.001f?0:Mathf.Clamp01(Vector2.Dot(c-a,ab)/d);
            return (a+ab*t-c).sqrMagnitude<=radius*radius;
        }

        public void TestStart(){StartRunDirect();}
        public void TestAnswerCorrect()
        {
            if(phase==GamePhase.Title||phase==GamePhase.End)StartRunDirect();
            if(phase==GamePhase.Reveal)FinishReveal();
            if(phase==GamePhase.Practice||phase==GamePhase.Playing)ResolveMask(current.correctMask,false);
        }
        public void TestAnswerWrong()
        {
            if(phase==GamePhase.Title||phase==GamePhase.End)StartRunDirect();
            if(phase==GamePhase.Reveal)FinishReveal();
            if(phase==GamePhase.Practice||phase==GamePhase.Playing)
            {
                int wrong=1;while(wrong==current.correctMask)wrong<<=1;ResolveMask(wrong,false);
            }
        }
        public string StateJson()
        {
            if(phase==GamePhase.Title)st.phase="title";
            else if(phase==GamePhase.End)st.phase=endReason=="clear"?"clear":"gameover";
            else st.phase="playing";
            return JsonUtility.ToJson(st);
        }
        public string ProblemBankJson()=>MgfJson.Bank(bank);
    }
}
