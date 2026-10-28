using System;
using System.Collections.Generic;
using Mgf;
using UnityEngine;

namespace Mgf.YangmalDaetalchul
{
    public sealed class YangmalDaetalchulGame : MonoBehaviour, IMgfGame
    {
        public enum Flow { Title, Split, Practice, Playing, Reveal, End }
        [Serializable] public sealed class State : MgfState
        {
            public int round, stage, rescued, firstAttempted, firstCorrect, selectedLength, attemptsOnCurrentProblem;
            public bool onboardingComplete, dragging;
            public string currentProblemId = "", lastResult = "", misconceptionId = "", layout = "portrait";
            public int inputCount, pointerVersion, handleX, handleY, rulerMin, rulerMax;
        }

        readonly State st = new State();
        readonly List<SockProblem> bank = new List<SockProblem>();
        List<SockProblem> deck;
        SockProblem current;
        YangmalDaetalchulView view;
        Flow flow;
        System.Random rng;
        int serial;
        float timeLeft, revealClock, splitClock;
        bool revealCorrect, revealPractice, dragMoved;
        Vector2 down;

        public State GameState => st;
        public SockProblem Current => current;
        public Flow CurrentFlow => flow;
        public float TimeLeft => timeLeft;

        void Awake()
        {
            MgfLook.Quality(34f); Physics.simulationMode = SimulationMode.Script;
            if (Time.frameCount < 0) { gameObject.AddComponent<BoxCollider>(); gameObject.AddComponent<SphereCollider>(); gameObject.AddComponent<CapsuleCollider>(); }
            bank.AddRange(SockRules.BuildBank()); Prewarm();
            view = new YangmalDaetalchulView(this); ShowTitle(); MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var s = new System.Text.StringBuilder(18000);
            s.Append("양말대탈출미끄럼틀을늘려라중학교3학년원과직선세탁방구조시작최고기록길이에맞게늘려양말을내보내라");
            s.Append("같은점에서그은두접선의길이는같다내접원접점직각선분길이를구하시오바르게고치시오연습실전안전실첫시도통과");
            s.Append("손잡이를끌어길이를정하시오빛나는손가락을따라끌어보시오도착한뒤늘리시오너무짧아주머니에떨어졌습니다너무길어집게를지났습니다");
            s.Append("△ABCD E F O P cm0123456789=−·");
            foreach (var p in bank) s.Append(p.prompt).Append(p.evidence);
            MgfText.Prewarm(s.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            HandleInput();
            if (flow == Flow.Split) { splitClock += dt; if (splitClock >= .45f) BeginPractice(); }
            else if (flow == Flow.Playing) { timeLeft -= dt; if (timeLeft <= 0) { timeLeft = 0; End(false, "시간 종료"); } }
            else if (flow == Flow.Reveal) { revealClock += dt; if (revealClock >= (revealCorrect ? 1.55f : 1.25f)) FinishReveal(); }
            view.Tick(dt);
        }

        void ResetRunState()
        {
            st.score=0; st.lives=3; st.level=1; st.solved=0; st.round=0; st.stage=1; st.rescued=0;
            st.firstAttempted=0; st.firstCorrect=0; st.selectedLength=0; st.attemptsOnCurrentProblem=0;
            st.lastResult=""; st.misconceptionId=""; st.dragging=false; st.onboardingComplete=false;
        }

        void ShowTitle()
        {
            StopAllCoroutines(); flow=Flow.Title; st.phase="title"; current=null; deck=null; ResetRunState(); view.ShowTitle(); Notify();
        }

        void StartSplit()
        {
            if (flow != Flow.Title) return; flow=Flow.Split; splitClock=0; st.phase="title"; st.inputCount++; st.pointerVersion++;
            view.BeginSplit(); MgfSfx.Play("whoosh",.28f); Notify();
        }

        void BeginPractice()
        {
            flow=Flow.Practice; st.phase="playing"; st.onboardingComplete=false; Load(SockRules.Practice(), true); view.ShowPractice(); Notify();
        }

        void BeginRun(bool test)
        {
            serial++; rng = new System.Random(unchecked(20261029 ^ serial*104729 ^ (test?0:Environment.TickCount)));
            deck=SockRules.BuildDeck(rng,serial); ResetRunState(); st.onboardingComplete=true; timeLeft=SockRules.RunSeconds;
            view.ResetRunWorld();
            // 연속 시드의 첫 Random 값 편향을 없앤다. 17은 24와 서로소라 24판마다 답 1..24가 정확히 한 번씩 나오며,
            // +5 오프셋은 단순 1→24 순환 입력과 위상이 맞지 않는다. 나머지 8문항은 매 판 셔플을 유지한다.
            int firstQ = 1 + ((serial * 17 + 5) % 24);
            deck[0] = SockRules.Make(1, firstQ, 1 + (serial % 5), "run" + serial + "-r1-b1-q" + firstQ.ToString("00") + "-v" + (1 + serial % 5));
            flow=Flow.Playing; st.phase="playing"; LoadRound(0); view.ShowRun(); Notify();
        }

        void LoadRound(int index)
        {
            if(index>=9){End(st.firstCorrect>=7,"완료");return;} st.round=index+1; st.stage=index/3+1; st.level=st.stage; Load(deck[index],false);
        }

        void Load(SockProblem p,bool practice)
        {
            current=p; st.currentProblemId=p.id; st.selectedLength=0; st.attemptsOnCurrentProblem=0; st.lastResult=""; st.misconceptionId=""; st.dragging=false;
            view.LoadProblem(p,practice); Notify();
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                down=MgfPointer.Position; st.inputCount++; st.pointerVersion++;
                if(flow==Flow.Title){StartSplit();return;}
                if(flow==Flow.End){BeginRun(false);return;}
                if(flow==Flow.Split||flow==Flow.Reveal){view.RefuseAt(down,flow==Flow.Reveal?"도착한 뒤 늘리시오":"세탁기 문을 누르시오");Notify();return;}
                if((flow==Flow.Practice||flow==Flow.Playing)&&(flow==Flow.Practice||view.HitHandle(down)||view.HitRulerGrab(down)))
                {
                    st.dragging=true; dragMoved=false; view.HandlePressed(); Notify();
                }
                else { view.RefuseAt(down,"노란 손잡이를 끌어 길이를 정하시오"); Notify(); }
            }
            if(st.dragging&&MgfPointer.Held)
            {
                int cm=view.LengthFromScreen(MgfPointer.Position);
                if(cm!=st.selectedLength){st.selectedLength=cm;dragMoved=true;st.pointerVersion++;view.SetLength(cm);Notify();}
            }
            if(st.dragging&&MgfPointer.Up)
            {
                st.dragging=false; view.HandleReleased();
                if(!dragMoved||st.selectedLength==0){st.selectedLength=0;view.RefuseAt(MgfPointer.Position,"손잡이를 끌어 길이를 정하시오");Notify();return;}
                // 연습도 학생이 실제로 놓은 정수 눈금을 그대로 판정한다.
                // 정답으로 치환하면 아무 길이나 놓아도 6 cm 성공 연출이 나와 잘못된 조작 규칙을 학습시킨다.
                Submit(st.selectedLength,false);
            }
        }

        void Submit(int value,bool forced)
        {
            if(current==null||!(flow==Flow.Practice||flow==Flow.Playing))return;
            bool practice=flow==Flow.Practice, correct=value==current.answerCm, first=st.attemptsOnCurrentProblem==0;
            st.attemptsOnCurrentProblem++; st.lastResult=correct?"correct":"wrong"; st.misconceptionId=SockRules.Diagnose(current,value);
            if(practice)
            {
                if(!correct){view.PracticeMiss(value,current.answerCm);st.selectedLength=0;Notify();return;}
                st.onboardingComplete=true; revealCorrect=true; revealPractice=true; revealClock=0; flow=Flow.Reveal; st.phase="reveal"; view.Reveal(true,value,current,true); MgfSfx.Play("correct",.55f); Notify(); return;
            }
            if(first){st.firstAttempted++;if(correct)st.firstCorrect++;}
            if(correct){st.score+=first?100:20;st.rescued++;st.solved=st.rescued;}
            else st.lives--;
            revealCorrect=correct;revealPractice=false;revealClock=0;flow=Flow.Reveal;st.phase="reveal";view.Reveal(correct,value,current,false);MgfSfx.Play(correct?"correct":"wrong",correct?.55f:.42f);
            if(!correct) MgfFx.Shake(view.Camera,.07f,.22f); Notify();
        }

        void FinishReveal()
        {
            if(revealPractice){revealPractice=false;BeginRun(false);return;}
            if(!revealCorrect)
            {
                if(st.lives<=0){End(false,"안전실 소진");return;}
                int remaining=9-st.firstAttempted; if(st.firstCorrect+remaining<7){End(false,"첫 시도 기준 미달");return;}
                flow=Flow.Playing;st.phase="playing";st.selectedLength=0;view.ResumeRepair();Notify();return;
            }
            flow=Flow.Playing;st.phase="playing";LoadRound(st.round);
        }

        void End(bool clear,string reason)
        {
            flow=Flow.End;st.phase=clear?"clear":"gameover";st.lastResult=reason;view.ShowEnd(clear,reason);MgfSfx.Play(clear?"win":"lose",.6f);Notify();
        }

        void Notify()=>MgfBridge.NotifyChanged();
        public void TestStart()=>BeginRun(true);
        public void TestAnswerCorrect(){if(flow!=Flow.Playing)BeginRun(true);Submit(current.answerCm,true);}
        public void TestAnswerWrong(){if(flow!=Flow.Playing)BeginRun(true);int w=current.answerCm==24?23:current.answerCm+1;Submit(w,true);}
        public string StateJson()=>JsonUtility.ToJson(st);
        public string ProblemBankJson()=>MgfJson.Bank(SockRules.ToMgf(bank));
    }
}
