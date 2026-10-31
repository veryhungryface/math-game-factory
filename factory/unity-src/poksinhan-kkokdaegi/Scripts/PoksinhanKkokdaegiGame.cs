// 폭신한 꼭대기 — 규칙·상태·입력. 장면은 CushionWorld, 화면 글자는 CushionHud 가 맡는다.
// 한 칸 = 누비 한 칸이라 방석 위 칸 수가 곧 넓이(분산에 대응)이고, 한 변이 표준편차다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.PoksinhanKkokdaegi
{
    public sealed class PoksinhanKkokdaegiGame : MonoBehaviour, IMgfGame
    {
        // IL2CPP 스트리핑이 런타임 프리미티브의 콜라이더 형식을 지우지 않게 보존한다.
        static readonly Type PreserveSphere=typeof(SphereCollider), PreserveCapsule=typeof(CapsuleCollider);
        [Serializable]
        sealed class State : MgfState
        {
            public int band, round, firstCorrect, firstAttempts, firstMask, selectedSide, inputCount;
            public int remainingFirstMistakes;
            public string feedbackKind="", misconceptionId="", activeProblemId="";
            public bool masteryImpossible, onboarding, repairMode, dragging, layoutLand;
            // 눈금 k칸 모서리의 화면 좌표(천분율, 위쪽 원점). 자동 입력 도구용 기하 정보이며 정답과 무관하다.
            public int[] cornerXPermille=new int[9], cornerYPermille=new int[9];
        }

        public enum Flow { Title, Opening, Practice, Playing, Reveal, End }

        const float PracticeSeconds=1.5f;
        readonly State st=new State();
        readonly List<CushionProblem> models=new List<CushionProblem>(5000);
        List<CushionProblem> deck;
        CushionProblem current;
        Flow flow=Flow.Title;
        CushionWorld world;
        CushionHud hud;
        int runSerial, reachedSide;
        bool practiced, firstAttempt=true, revealCorrect, revealPractice, failAfterReveal;
        bool practiceTapArmed, dragging, moved;
        float timeLeft, openingClock, revealClock, idleClock, endClock;
        Vector2 pointerDown;
        int lastSecond=-1;

        void Awake()
        {
            GC.KeepAlive(PreserveSphere);GC.KeepAlive(PreserveCapsule);
            MgfLook.Quality(60f);
            models.AddRange(PoksinRules.BuildModels());
            Prewarm();
            world=new CushionWorld();world.Build();
            hud=new CushionHud();hud.Build();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var b=new StringBuilder(8000);
            b.Append(CushionHud.AllStaticText);
            b.Append("0123456789,.·→−²√÷=()/[]ABn ");
            for(int i=0;i<Mathf.Min(models.Count,200);i+=3)b.Append(models[i].prompt);
            MgfText.Prewarm(b.ToString());
        }

        void Update()
        {
            float dt=Mathf.Min(.05f,Time.unscaledDeltaTime);
            HandlePointer();
            if(flow==Flow.Opening){openingClock+=dt;if(openingClock>=.42f)BeginPractice();}
            else if(flow==Flow.Practice){idleClock+=dt;if(idleClock>=8f){idleClock=0;hud.Guide("초록 고리를 아래로 끌어 3칸 표시에서 손을 떼시오");}}
            else if(flow==Flow.Playing)
            {
                if(!dragging)idleClock+=dt;
                timeLeft-=dt;
                if(timeLeft<=0){timeLeft=0;EndRun("time");}
            }
            else if(flow==Flow.Reveal)
            {
                revealClock+=dt;
                float limit=revealPractice&&revealCorrect?PracticeSeconds+.9f:revealCorrect?2.1f:2.3f;
                if(revealClock>=limit)FinishReveal();
            }
            else if(flow==Flow.End)endClock+=dt;
            world.Tick(dt,flow,revealClock,revealCorrect);
            hud.Tick(dt,world,flow==Flow.Practice&&!dragging,dragging&&st.selectedSide>0,st.selectedSide,current!=null&&!dragging&&(flow==Flow.Practice||flow==Flow.Playing));
            int sec=Mathf.CeilToInt(timeLeft);
            if(sec!=lastSecond&&flow==Flow.Playing){lastSecond=sec;RefreshHud();}
        }

        void ResetState()
        {
            st.score=0;st.lives=PoksinRules.StartThreads;st.level=1;st.phase="title";st.solved=0;
            st.band=0;st.round=0;st.firstCorrect=0;st.firstAttempts=0;st.firstMask=0;st.selectedSide=0;st.inputCount=0;
            st.remainingFirstMistakes=1;st.feedbackKind="";st.misconceptionId="";st.activeProblemId="";st.masteryImpossible=false;
            st.onboarding=false;st.repairMode=false;st.dragging=false;
            timeLeft=PoksinRules.SessionSeconds;firstAttempt=true;dragging=false;moved=false;
            openingClock=revealClock=idleClock=0;lastSecond=-1;practiceTapArmed=false;
        }

        void ShowTitle()
        {
            ResetState();flow=Flow.Title;current=null;deck=null;
            world.ShowTitle();hud.ShowTitle(PlayerPrefs.GetInt("poksin_best",0));Notify();
        }

        void StartOpening()
        {
            ResetState();flow=Flow.Opening;st.phase="playing";openingClock=0;
            hud.HideAll();world.OpenTitle();MgfSfx.Play("whoosh",.45f);Notify();
        }

        void BeginPractice()
        {
            flow=Flow.Practice;st.phase="playing";st.onboarding=true;current=PoksinRules.Practice();
            st.activeProblemId=current.id;firstAttempt=true;
            world.ShowGame();world.ResetTower();world.LoadCushion(true);
            hud.ShowGame();hud.LoadProblem(current,true,0);RefreshHud();Notify();
        }

        void BeginRun(bool deterministic)
        {
            runSerial++;ResetState();practiced=true;flow=Flow.Playing;st.phase="playing";
            int seed=deterministic?20261031:unchecked(Environment.TickCount+runSerial*7919);
            deck=PoksinRules.BuildDeck(models,seed);
            world.ShowGame();world.ResetTower();hud.ShowGame();
            LoadRound(0);
        }

        void LoadRound(int index)
        {
            current=deck[index];flow=Flow.Playing;st.phase="playing";st.round=index+1;st.band=current.band;st.level=current.band;
            st.activeProblemId=current.id;st.selectedSide=0;st.feedbackKind="";st.misconceptionId="";st.repairMode=false;
            firstAttempt=true;dragging=false;moved=false;idleClock=0;
            world.LoadCushion(false);hud.LoadProblem(current,false,index);RefreshHud();Notify();
        }

        void Submit(int side)
        {
            if((flow!=Flow.Practice&&flow!=Flow.Playing)||current==null)return;
            side=Mathf.Clamp(side,0,8);st.selectedSide=side;
            revealCorrect=PoksinRules.IsCorrect(current,side);revealPractice=flow==Flow.Practice;
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
            }
            int remaining=PoksinRules.TargetLayers-st.firstAttempts;
            st.remainingFirstMistakes=Math.Max(0,1-(st.firstAttempts-st.firstCorrect));
            st.masteryImpossible=!revealPractice&&st.firstCorrect+remaining<PoksinRules.MasteryNeeded;
            failAfterReveal=!revealPractice&&!revealCorrect&&(st.lives<=0||st.masteryImpossible);
            flow=Flow.Reveal;revealClock=0;dragging=false;st.dragging=false;
            world.BeginReveal(side,revealCorrect,Math.Abs(side*side-current.variance));
            hud.ShowReveal(RevealTitle(side),RevealMath(side));
            MgfSfx.Play(revealCorrect?"correct":"wrong",revealCorrect?.5f:.3f);
            RefreshHud();Notify();
        }

        string RevealTitle(int side)
        {
            if(revealCorrect)return revealPractice?"분산 9 → 표준편차 3":"한 변 "+side+"칸 · 솔기 완성";
            return revealPractice?"다시 맞추시오 · 연습은 실이 줄지 않습니다":"옆솔기가 풀렸습니다 · 봉합실 −1";
        }

        string RevealMath(int side)
        {
            if(revealCorrect)
            {
                if(revealPractice)return "편차 −3, −3, 3, 3 → 제곱의 평균 9\n방석 9칸(넓이) · 한 변 √9 = 3칸";
                if(!string.IsNullOrEmpty(current.pairId))
                {
                    if(current.pairRole=="A")return "짝 A 표준편차 "+side+"칸. 평균·최솟값·최댓값이 같은 짝 B를 쌓아 비교하시오.";
                    return "짝 A "+current.pairedSide+"칸 · 짝 B "+side+"칸. 표준편차가 작은 짝 "+(current.pairedSide<side?"A":"B")+"가 더 고릅니다.";
                }
                return "놓은 넓이 "+(side*side)+"칸² = 분산 "+current.variance+"칸²";
            }
            return "놓은 넓이 "+(side*side)+"칸² · 분산 "+current.variance+"칸²\n"+WrongReason(st.misconceptionId);
        }

        static string WrongReason(string id)
        {
            switch(id)
            {
                case "mean_of_deviations":return "편차의 평균은 항상 0입니다. 편차를 제곱해 평균을 내시오.";
                case "no_division":return "편차 제곱의 합을 자료 개수로 나누지 않았습니다.";
                case "variance_as_side":return "이 게임의 방석 넓이가 분산입니다. 표준편차는 √분산인 한 변입니다.";
                case "half_range_as_side":return "범위의 절반이 아니라 모든 편차의 제곱을 쓰시오.";
                case "mean_absolute_deviation":return "편차의 절댓값이 아니라 제곱의 평균을 쓰시오.";
                case "area_too_large":return "만든 방석의 넓이가 분산보다 큽니다.";
                default:return "만든 방석의 넓이가 분산보다 작습니다.";
            }
        }

        void FinishReveal()
        {
            if(flow!=Flow.Reveal)return;
            hud.HideReveal();
            if(revealPractice)
            {
                if(revealCorrect)BeginRun(false);
                else{flow=Flow.Practice;st.selectedSide=0;st.feedbackKind="";world.LoadCushion(true);hud.LoadProblem(current,true,0);Notify();}
                return;
            }
            if(failAfterReveal){world.KeepTear();EndRun(st.lives<=0?"thread":"mastery");return;}
            if(revealCorrect)
            {
                world.LockLayer(st.solved-1,current.side,firstAttempt);
                if(st.solved>=PoksinRules.TargetLayers)EndRun(st.firstCorrect>=PoksinRules.MasteryNeeded?"clear":"mastery");
                else LoadRound(st.solved);
            }
            else
            {
                firstAttempt=false;flow=Flow.Playing;st.selectedSide=0;idleClock=0;
                world.KeepTear();world.LoadCushion(false);
                hud.Guide(WrongReason(st.misconceptionId)+"\n같은 기록으로 방석을 다시 만드시오");
                RefreshHud();Notify();
            }
        }

        void EndRun(string reason)
        {
            if(flow==Flow.End)return;
            bool clear=reason=="clear"&&st.solved==6&&st.firstCorrect>=5;
            flow=Flow.End;endClock=0;st.phase=clear?"clear":"gameover";dragging=false;st.dragging=false;
            if(st.firstCorrect>PlayerPrefs.GetInt("poksin_best",0)){PlayerPrefs.SetInt("poksin_best",st.firstCorrect);PlayerPrefs.Save();}
            world.ShowEnd(clear);
            string why=reason=="time"?"풀이 시간 120초가 끝났습니다.":reason=="thread"?"봉합실 3가닥을 모두 썼습니다.":
                reason=="mastery"?"첫 제출 실수가 2회가 되어 5/6에 닿을 수 없습니다.":"";
            string detail=why;
            if(!clear&&!string.IsNullOrEmpty(st.misconceptionId))detail+="\n"+WrongReason(st.misconceptionId);
            if(clear&&deck!=null)
            {
                var a=deck[4];var b=deck[5];
                string better=a.side<b.side?"A":"B";
                detail="짝 A  "+PoksinRules.ArrayText(a.raw)+"  →  "+a.side+"칸\n짝 B  "+PoksinRules.ArrayText(b.raw)+"  →  "+b.side+"칸\n평균·최솟값·최댓값이 같아도 짝 "+better+"가 더 고릅니다.";
            }
            hud.ShowEnd(clear,st.solved,st.firstCorrect,detail);
            MgfSfx.Play(clear?"win":"lose",.55f);Notify();
        }

        void RefreshHud(){hud.RefreshHud(st.onboarding,st.solved,st.lives,timeLeft,st.remainingFirstMistakes,st.firstAttempts,st.firstMask);}

        void HandlePointer()
        {
            if(MgfPointer.Down)
            {
                st.inputCount++;pointerDown=MgfPointer.Position;idleClock=0;hud.Ripple(pointerDown);
                if(flow==Flow.Title){StartOpening();return;}
                if(flow==Flow.End){if(endClock>.45f){if(practiced)BeginRun(false);else StartOpening();}return;}
                if(flow==Flow.Reveal){if(revealClock>.7f)FinishReveal();return;}
                if(flow!=Flow.Practice&&flow!=Flow.Playing)return;
                if(flow==Flow.Practice&&practiceTapArmed)
                {
                    // 드래그를 못 찾은 학생의 연습 전용 두 번째 탭: 3칸 표시까지 함께 놓아 준다. 통계에는 들어가지 않는다.
                    practiceTapArmed=false;world.SetSide(3);Submit(3);return;
                }
                if(world.InFrame(cameraPointer:true))
                {
                    dragging=true;moved=false;reachedSide=0;st.dragging=true;st.selectedSide=0;
                    MgfSfx.Play("tap",.18f);Notify();
                }
                else Refuse("격자 위에서 끌고 놓으시오");
            }
            if(dragging&&MgfPointer.Held)
            {
                if(Vector2.Distance(MgfPointer.Position,pointerDown)>=18f)moved=true;
                int side=moved?world.SideFromPointer():0;
                reachedSide=Mathf.Max(reachedSide,side);
                if(side!=st.selectedSide){st.selectedSide=side;world.SetSide(side);st.inputCount++;MgfSfx.Play("tap",.10f);Notify();}
            }
            if(dragging&&MgfPointer.Up)
            {
                dragging=false;st.dragging=false;
                bool zeroSubmit=moved&&st.selectedSide==0&&reachedSide>0&&flow==Flow.Playing;
                if(moved&&st.selectedSide>0||zeroSubmit)Submit(st.selectedSide);
                else{st.selectedSide=0;world.SetSide(0);Refuse("고리를 끌어 길이를 만든 뒤 손을 떼시오");}
            }
        }

        void Refuse(string msg)
        {
            if(flow==Flow.Practice){practiceTapArmed=true;msg="한 번 더 누르면 3칸까지 놓습니다";}
            hud.Toast(msg);world.Nudge();MgfSfx.Play("pop",.14f);Notify();
        }

        void Notify()=>MgfBridge.NotifyChanged();

        // ── IMgfGame
        public void TestStart()=>BeginRun(true);
        void EnsureTestPlaying(){if(flow==Flow.Reveal)FinishReveal();if(flow!=Flow.Playing)BeginRun(true);}
        public void TestAnswerCorrect(){EnsureTestPlaying();world.SetSide(current.side);Submit(current.side);}
        public void TestAnswerWrong(){EnsureTestPlaying();int t=current.side==8?7:current.side+1;world.SetSide(t);Submit(t);}
        public string StateJson()
        {
            st.layoutLand=MgfBridge.IsLandLayout;
            world.CornerPermille(st.cornerXPermille,st.cornerYPermille);
            return JsonUtility.ToJson(st);
        }
        public string ProblemBankJson()=>PoksinRules.BankJson(models);
    }
}
