using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    // Choice adjudication and endless-run progression are authoritative here.
    // Presentation may pause between questions or during a boss, but only gate HP
    // reaching zero is allowed to end a run.
    public sealed class HyeopgokRules
    {
        public static readonly Vector3[] Pads={new Vector3(-1.8f,1.23f,2),new Vector3(.6f,1.23f,2),new Vector3(-1.8f,1.23f,-1),new Vector3(.6f,1.23f,-1)};
        public static readonly Vector3 Exit=new Vector3(-.6f,1.24f,-1.3f);
        public const float Speed=6.4f,Hold=.4f,Limit=24f,ConfirmTime=.6f,VisitHold=.35f,InitialPourDelay=1f;
        // Hit geometry starts at the exact visible outside edge of the authored
        // dashed ribbon, then adds the requested eight percent touch allowance.
        public const float PadStrokeHalfX=.87f,PadStrokeHalfZ=.85f,PadStrokeWidth=.085f;
        public const float PadVisualHalfX=PadStrokeHalfX+PadStrokeWidth*.5f,PadVisualHalfZ=PadStrokeHalfZ+PadStrokeWidth*.5f,PadHitPadding=1.08f;
        public const float PadHitHalfX=PadVisualHalfX*PadHitPadding,PadHitHalfZ=PadVisualHalfZ*PadHitPadding;
        public const float MinX=-2.75f,MaxX=1.48f,MinZ=-5.25f,MaxZ=3.55f;
        public const int CarryCapacity=120,CoinPerKill=1,MinimumSpawnCoins=140,AmountBudget=60,FractionBudget=120;

        public float TimeLimit{get;private set;}=Limit;
        public static float LimitForDifficulty(int d)=>d<=1?24:d==2?32:40;
        public QuestionPack Pack{get;private set;}
        public PackItem Current{get;private set;}
        public readonly string[] Choices=new string[4];
        public readonly int[] Poured=new int[2];
        public readonly bool[] Visited=new bool[2];
        public Action<int,int> OnPour;
        public Vector3 King,Target;

        // Wave remains the current 1..10 question number for V3 consumers.
        // Attempts/Correct remain whole-run totals; Stage* counters reset only
        // after a cleared boss and permanent-perk choice.
        public int Wave,Correct,Attempts,Hp,Coins,Score,Hover=-1,LastPad=-1;
        public int Earned,Collected,Spent,Invested,PourCount;
        public int Stage{get;private set;}=1;
        public int Map{get;private set;}=1;
        public int Loop{get;private set;}=1;
        public int StageQuestion{get;private set;}
        public int TotalQuestions=>Attempts;
        public int StageAttempts{get;private set;}
        public int StageCorrect{get;private set;}
        public int MaxHp{get;private set;}=100;
        public bool AwaitingBoss{get;private set;}
        public float DifficultyMultiplier=>Mathf.Pow(1.25f,Mathf.Max(0,Loop-1));
        public float TowerDamageMultiplier=>1f+.15f*PerkDamage;
        public float CoinRadiusMultiplier=>1f+.30f*PerkMagnet;
        public string EndReason{get;private set;}="";
        public int QuestionDifficulty{get;private set;}=1;
        public string[] QuestionHistory{get;private set;}=Array.Empty<string>();
        public int PerkDamage{get;private set;}
        public int PerkMagnet{get;private set;}
        public int PerkHealth{get;private set;}

        public bool Active,Pending,LastCorrect,Ended,Confirming,TutorialBlocked;
        public float Dwell,Elapsed,Confirm;
        public int CommandedPad=>commandedPad;
        public int PadCount=>Current==null?0:4;
        public int TotalPoured=>Poured[0]+Poured[1];
        public bool IsMovingOnPad=>Hover>=0&&(King-Target).sqrMagnitude>=.012f;
        // This engine supports one validated combat profile; accepting a different
        // JSON balance without implementing it would falsely promise solvability.
        public int WalletCapacity=>CarryCapacity;

        System.Random orderRng,placeRng;
        bool[] dealt=Array.Empty<bool>();
        int dealtCount,lastQuestionIndex=-1;
        readonly List<string> questionHistory=new List<string>(64);
        float pourClock;
        int tapBudget=-1,visitedPad=-1,commandedPad=-1;
        bool wasStopped,suppressPadAuto;

        static int StreamSeed(int seed,ulong stream){unchecked{ulong z=(uint)seed+0x9e3779b97f4a7c15UL*(stream+1);z=(z^(z>>30))*0xbf58476d1ce4e5b9UL;z=(z^(z>>27))*0x94d049bb133111ebUL;return(int)(z^(z>>31));}}

        public void Start(QuestionPack pack,int seed)
        {
            Pack=pack;orderRng=new System.Random(StreamSeed(seed,0));placeRng=new System.Random(StreamSeed(seed,1));
            Stage=Map=Loop=1;Wave=StageQuestion=StageAttempts=StageCorrect=0;
            Correct=Attempts=Score=Coins=Earned=Collected=Spent=Invested=PourCount=0;
            PerkDamage=PerkMagnet=PerkHealth=0;MaxHp=Hp=100;
            Active=true;Pending=Ended=AwaitingBoss=false;EndReason="";
            Current=null;QuestionDifficulty=1;lastQuestionIndex=-1;
            int count=Pack!=null&&Pack.items!=null?Pack.items.Length:0;
            dealt=new bool[count];dealtCount=0;questionHistory.Clear();QuestionHistory=Array.Empty<string>();
            Next();
        }

        // Ends question presentation and exposes the boss phase without ending the
        // run. It is idempotent so feedback and battle coordinators can both call it.
        public bool BeginBoss()
        {
            if(Ended||StageAttempts<10)return false;
            AwaitingBoss=true;Active=false;Pending=false;Confirming=false;Dwell=Confirm=0;
            return true;
        }

        // Called only after the current stage boss is defeated and a perk card was
        // chosen. The question deck is global to the run and intentionally survives.
        public bool AdvanceStage(int perkIndex)
        {
            if(Ended||!AwaitingBoss||perkIndex<0||perkIndex>2)return false;
            if(perkIndex==0)PerkDamage++;
            else if(perkIndex==1)PerkMagnet++;
            else {PerkHealth++;MaxHp+=20;}

            Hp=Mathf.Min(MaxHp,Hp+Mathf.CeilToInt(MaxHp*.30f));
            Stage++;Map=(Stage-1)%3+1;Loop=(Stage-1)/3+1;
            Wave=StageQuestion=StageAttempts=StageCorrect=0;Coins=0;
            AwaitingBoss=false;Pending=false;Active=true;Confirming=false;EndReason="";
            Next();
            return Current!=null&&Active;
        }

        // Capture-only fixture. The caller is responsible for guarding its use with
        // a URL flag. It changes progression coordinates but never reloads the pack
        // or resets the whole-run no-repeat deck/history.
        public void DebugSetStage(int stage)
        {
            Stage=Math.Max(1,stage);Map=(Stage-1)%3+1;Loop=(Stage-1)/3+1;
            Wave=StageQuestion=StageAttempts=StageCorrect=0;
            AwaitingBoss=Pending=Ended=Confirming=false;Active=true;EndReason="";
            if(Hp<=0)Hp=MaxHp;
            Next();
        }

        public void Next()
        {
            if(Ended||Hp<=0){if(Hp<=0)SetGateGameOver();return;}
            if(AwaitingBoss||StageAttempts>=10){AwaitingBoss=true;Active=false;return;}
            if(Pack==null||Pack.items==null||Pack.items.Length==0){Active=false;return;}

            int ordinal=StageAttempts+1;
            int at=SelectQuestionIndex(ordinal);
            if(at<0){Active=false;return;}
            dealt[at]=true;dealtCount++;lastQuestionIndex=at;Current=Pack.items[at];
            QuestionDifficulty=Math.Max(1,Current.difficulty);
            StageQuestion=Wave=ordinal;

            // Budget depends on mode / difficulty, never on the hidden answer.
            TimeLimit=Current.Mode=="choice"?LimitForDifficulty(Current.difficulty):Current.Mode=="amount"?60+6*Math.Max(1,Current.difficulty):80+6*Math.Max(1,Current.difficulty);
            if(Attempts==0&&Current.Mode=="amount")TimeLimit=30;
            if(Attempts==0&&Current.Mode=="choice")TimeLimit+=12; // first-ever question: time to find the pads
            for(int i=0;i<4;i++)Choices[i]=Current.Mode=="choice"?Current.choices[i]:"";
            if(Current.Mode=="choice")for(int i=3;i>0;i--){int j=placeRng.Next(i+1);string t=Choices[i];Choices[i]=Choices[j];Choices[j]=t;}

            questionHistory.Add(string.IsNullOrEmpty(Current.id)?at.ToString():Current.id);
            QuestionHistory=questionHistory.ToArray();
            King=Target=new Vector3(-.6f,1.24f,-3.1f);Dwell=Elapsed=Confirm=pourClock=0;Hover=LastPad=visitedPad=commandedPad=-1;suppressPadAuto=false;
            Poured[0]=Poured[1]=0;Visited[0]=Visited[1]=false;Pending=Confirming=TutorialBlocked=wasStopped=false;tapBudget=-1;Active=true;
        }

        int SelectQuestionIndex(int ordinal)
        {
            if(dealtCount>=dealt.Length){Array.Clear(dealt,0,dealt.Length);dealtCount=0;}

            int desired=DesiredDifficulty(Stage,ordinal);
            // Preserve the familiar authored opener only when it also belongs to
            // the stage-one introductory difficulty band.
            if(Stage==1&&ordinal==1&&!dealt[0]&&Math.Max(1,Pack.items[0].difficulty)<=2)return 0;

            int at=PickUndealt(desired,0);
            if(at>=0)return at;
            bool low=Stage<=2||desired<=2;
            at=PickUndealt(0,low?1:2);
            if(at>=0)return at;
            at=PickUndealt(0,3);
            if(at>=0)return at;

            // A category can exhaust on the same draw that exhausts the pack.
            // Start a fresh permutation only after every authored item was seen.
            if(dealtCount>=dealt.Length){Array.Clear(dealt,0,dealt.Length);dealtCount=0;return SelectQuestionIndex(ordinal);}
            return -1;
        }

        // group: 0 exact, 1 difficulty 1-2, 2 difficulty 3-4, 3 any.
        int PickUndealt(int difficulty,int group)
        {
            int at=-1,count=0;
            for(int i=0;i<Pack.items.Length;i++){
                if(dealt[i])continue;
                int d=Math.Max(1,Pack.items[i].difficulty);
                bool match=group==0?d==difficulty:group==1?d<=2:group==2?d>=3:difficulty==0;
                if(!match)continue;
                // Avoid an immediate boundary repeat when another candidate exists.
                if(i==lastQuestionIndex&&dealtCount==0)continue;
                if(orderRng.Next(++count)==0)at=i;
            }
            if(at<0&&dealtCount==0&&lastQuestionIndex>=0&&!dealt[lastQuestionIndex]){
                int d=Math.Max(1,Pack.items[lastQuestionIndex].difficulty);
                bool match=group==0?d==difficulty:group==1?d<=2:group==2?d>=3:difficulty==0;
                if(match)at=lastQuestionIndex;
            }
            return at;
        }

        static int DesiredDifficulty(int stage,int ordinal)
        {
            // Stages one and two reserve the advanced pool. Stage three and later
            // lead with four advanced questions, guaranteeing the stated curve
            // whenever the pack contains that support.
            if(stage==1){int[] schedule={1,2,1,2,1,2,1,2,1,2};return schedule[(ordinal-1)%schedule.Length];}
            if(stage==2){int[] schedule={2,2,1,2,2,1,2,2,1,2};return schedule[(ordinal-1)%schedule.Length];}
            int[] advanced={3,4,3,4,2,3,4,2,3,4};return advanced[(ordinal-1)%advanced.Length];
        }

        public void Move(Vector3 target,bool tap=false,bool suppressChoiceAuto=false)
        {
            if(!Active||Pending)return;
            commandedPad=-1;suppressPadAuto=suppressChoiceAuto;
            Target=new Vector3(Mathf.Clamp(target.x,MinX,MaxX),1.24f,Mathf.Clamp(target.z,MinZ,MaxZ));
            if(tap){tapBudget=1;pourClock=0;wasStopped=false;}else tapBudget=-1;
        }

        public bool CommandChoicePad(int pad)
        {
            if(!Active||Pending||Current==null||Current.Mode!="choice"||pad<0||pad>=4)return false;
            Move(Pads[pad],true);commandedPad=pad;Dwell=0;return true;
        }

        // Pointer-down is not yet a tap or a hold: it may move the king but must
        // not pour until release (one coin) or a held/dragged press (continuous).
        public void Press(Vector3 target){Move(target);if(Active&&!Pending)tapBudget=0;}
        public int AddCoins(int count){if(!Active||count<=0)return 0;int add=Math.Min(count,WalletCapacity-Coins);Coins+=add;Earned+=add;Collected+=add;return add;}
        public int AddRewardCoins(int count){if(!Active||count<=0)return 0;Coins+=count;Earned+=count;return count;}
        public bool SpendUpgradeCoin(){if(!Active||Pending||Coins<=0)return false;Coins--;Spent++;Invested++;return true;}
        public int PadAt(Vector3 p){for(int i=0;i<PadCount;i++){Vector3 d=p-Pads[i];if(Mathf.Abs(d.x)<=PadHitHalfX&&Mathf.Abs(d.z)<=PadHitHalfZ)return i;}return -1;}

        public int Tick(float dt)
        {
            if(!Active||Pending)return -1;dt=Math.Max(0,dt);Elapsed+=dt;King=Vector3.MoveTowards(King,Target,Speed*dt);
            int near=PadAt(King);bool stopped=(King-Target).sqrMagnitude<.012f;
            if(near!=Hover){Hover=near;Dwell=0;pourClock=0;wasStopped=false;}
            // A direct pad tap nominates its pad immediately. A generic move that
            // finishes inside the same visible rectangle auto-nominates it here,
            // so both paths share the exact same 0.4 second arrival ring. Merely
            // crossing a pad while moving can never answer.
            if(commandedPad<0&&!suppressPadAuto&&near>=0&&stopped){commandedPad=near;Dwell=0;}
            if(commandedPad>=0){
                if(near==commandedPad&&stopped){
                    Dwell+=dt;
                    if(Dwell>=Hold){int selected=commandedPad;commandedPad=-1;Commit(selected);return selected;}
                }else Dwell=0;
            }else Dwell=0;
            if(Elapsed>=TimeLimit){Commit(-1);return 4;}return -1;
        }

        public bool PourOne(int pad)
        {
            if(!Active||Pending||Current.Mode=="choice"||pad<0||pad>=PadCount||Hover!=pad||Coins<=0||Poured[pad]>=Current.Max)return false;
            Coins--;Spent++;PourCount++;Poured[pad]++;OnPour?.Invoke(pad,Poured[pad]);return true;
        }

        public bool Select(int pad)=>CommandChoicePad(pad);

        bool Commit(int pad)
        {
            if(!Active||Pending)return false;LastPad=pad;
            LastCorrect=pad>=0&&pad<4&&Current.Mode=="choice"&&PackItem.EquivalentChoice(Choices[pad],Current.answer);
            Attempts++;StageAttempts++;Pending=true;Dwell=0;Confirming=false;
            if(LastCorrect){Correct++;StageCorrect++;Score+=100+Math.Max(0,24-(int)Elapsed);Invested+=TotalPoured;}
            else {
                Hp=Math.Max(0,Hp-18);
                if(Hp==0)SetGateGameOver();
            }
            if(!Ended&&StageAttempts>=10){AwaitingBoss=true;Active=false;}
            return true;
        }

        void SetGateGameOver()
        {
            Hp=0;Active=false;Ended=true;AwaitingBoss=false;EndReason="gate_hp_zero";
        }

        public void Damage(int damage)
        {
            if(Ended||damage<=0)return;
            Hp=Math.Max(0,Hp-damage);if(Hp==0)SetGateGameOver();
        }

        public int AnswerPad(){if(Current==null||Current.Mode!="choice")return -1;for(int i=0;i<4;i++)if(PackItem.EquivalentChoice(Choices[i],Current.answer))return i;return -1;}
        public bool Won=>false;
    }
}
