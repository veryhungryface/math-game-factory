using System;
using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    // The same movement, wallet and irreversible pouring state machine serves play
    // and tests. Correct answers never mint coins: combat pickups are the only source.
    public sealed class HyeopgokRules
    {
        public static readonly Vector3[] Pads={new Vector3(-1.8f,1.23f,2),new Vector3(.6f,1.23f,2),new Vector3(-1.8f,1.23f,-1),new Vector3(.6f,1.23f,-1)};
        public static readonly Vector3 Exit=new Vector3(-.6f,1.24f,-1.3f);
        public const float Speed=6.4f,Hold=.8f,Radius=.69f,Limit=24f,ConfirmTime=.6f,VisitHold=.35f,InitialPourDelay=1f;
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
        public int Wave,Correct,Attempts,Hp,Coins,Score,Hover=-1,LastPad=-1;
        public int Earned,Collected,Spent,Invested,PourCount;
        public bool Active,Pending,LastCorrect,Ended,Confirming,TutorialBlocked;
        public float Dwell,Elapsed,Confirm;
        public int PadCount=>Current==null?0:Current.Mode=="choice"?4:Current.Mode=="fraction_parts"?2:1;
        public int TotalPoured=>Poured[0]+Poured[1];
        public bool IsMovingOnPad=>Hover>=0&&(King-Target).sqrMagnitude>=.012f;
        // This engine supports one validated combat profile; accepting a different
        // JSON balance without implementing it would falsely promise solvability.
        public int WalletCapacity=>CarryCapacity;
        System.Random orderRng,placeRng;
        readonly int[] used=new int[10];
        float pourClock;
        int tapBudget=-1,visitedPad=-1;
        bool wasStopped;
        static int StreamSeed(int seed,ulong stream){unchecked{ulong z=(uint)seed+0x9e3779b97f4a7c15UL*(stream+1);z=(z^(z>>30))*0xbf58476d1ce4e5b9UL;z=(z^(z>>27))*0x94d049bb133111ebUL;return(int)(z^(z>>31));}}
        public void Start(QuestionPack pack,int seed){
            Pack=pack;orderRng=new System.Random(StreamSeed(seed,0));placeRng=new System.Random(StreamSeed(seed,1));
            Wave=Correct=Attempts=Score=Coins=Earned=Collected=Spent=Invested=PourCount=0;Hp=100;Active=true;Pending=Ended=false;
            for(int i=0;i<10;i++)used[i]=-1;Next();
        }
        public void Next(){
            if(Hp<=0||Wave>=10){Active=false;Ended=true;Pending=false;return;}
            int band=Wave<3?1:Wave<6?2:Wave<8?3:4,at=0,count=0;bool hasBand=false;
            for(int i=0;i<Pack.items.Length;i++)if(Math.Max(1,Pack.items[i].difficulty)==band){bool seen=false;for(int k=0;k<Wave;k++)if(used[k]==i)seen=true;if(!seen)hasBand=true;}
            for(int i=0;i<Pack.items.Length;i++){
                int difficulty=Math.Max(1,Pack.items[i].difficulty);if(hasBand?difficulty!=band:difficulty>band)continue;
                bool seen=false;for(int k=0;k<Wave;k++)if(used[k]==i)seen=true;if(!seen&&orderRng.Next(++count)==0)at=i;
            }
            if(Wave==0)at=0;used[Wave]=at;Current=Pack.items[at];
            // Budget depends on mode / difficulty, never on the hidden answer.
            TimeLimit=Current.Mode=="choice"?LimitForDifficulty(Current.difficulty):Current.Mode=="amount"?60+6*Math.Max(1,Current.difficulty):80+6*Math.Max(1,Current.difficulty);
            if(Wave==0&&Current.Mode=="amount")TimeLimit=30;
            for(int i=0;i<4;i++)Choices[i]=Current.Mode=="choice"?Current.choices[i]:"";
            if(Current.Mode=="choice")for(int i=3;i>0;i--){int j=placeRng.Next(i+1);string t=Choices[i];Choices[i]=Choices[j];Choices[j]=t;}
            King=Target=new Vector3(-.6f,1.24f,-3.1f);Dwell=Elapsed=Confirm=pourClock=0;Hover=LastPad=visitedPad=-1;
            Poured[0]=Poured[1]=0;Visited[0]=Visited[1]=false;Pending=Confirming=TutorialBlocked=wasStopped=false;tapBudget=-1;Wave++;
        }
        public void Move(Vector3 target,bool tap=false){
            if(!Active||Pending)return;
            Target=new Vector3(Mathf.Clamp(target.x,-2.75f,1.48f),1.24f,Mathf.Clamp(target.z,-4.2f,3.55f));
            if(tap){tapBudget=1;pourClock=0;wasStopped=false;}else tapBudget=-1;
        }
        // Pointer-down is not yet a tap or a hold: it may move the king but must
        // not pour until release (one coin) or a held/dragged press (continuous).
        public void Press(Vector3 target){Move(target);if(Active&&!Pending)tapBudget=0;}
        public int AddCoins(int count){if(!Active||count<=0)return 0;int add=Math.Min(count,WalletCapacity-Coins);Coins+=add;Earned+=add;Collected+=add;return add;}
        public int PadAt(Vector3 p){for(int i=0;i<PadCount;i++){Vector3 d=p-Pads[i];d.y=0;if(d.sqrMagnitude<Radius*Radius)return i;}return -1;}
        public int Tick(float dt){
            if(!Active||Pending)return -1;dt=Math.Max(0,dt);Elapsed+=dt;King=Vector3.MoveTowards(King,Target,Speed*dt);
            int near=PadAt(King);bool stopped=(King-Target).sqrMagnitude<.012f;
            if(near!=Hover){
                if(Hover>=0&&Current.Mode!="choice"&&Visited[Hover])visitedPad=Hover;
                Hover=near;Dwell=0;pourClock=0;wasStopped=false;
                if(near>=0){Confirming=false;Confirm=0;TutorialBlocked=false;}
                else if(Current.Mode!="choice"&&Visited[0]&&(PadCount==1||Visited[1])){
                    // The opening tutorial must teach pouring instead of letting
                    // an empty accidental visit consume the question. Later zero
                    // answers remain possible through a deliberate stop and leave.
                    if(Wave==1&&Current.Mode=="amount"&&Current.answerValue>0&&TotalPoured<1){TutorialBlocked=true;Confirming=false;Confirm=0;}
                    else {Confirming=true;Confirm=0;}
                }
            }
            if(Current.Mode=="choice"){
                if(near>=0&&stopped){Dwell+=dt;if(Dwell>=Hold){Select(near);return near;}}else Dwell=0;
            }else{
                if(near>=0&&stopped){
                    Dwell+=dt;pourClock-=dt;
                    if(!wasStopped){pourClock=tapBudget>0?0:InitialPourDelay;wasStopped=true;}
                    if(!Visited[near]&&(tapBudget>0||Dwell>=VisitHold)){Visited[near]=true;visitedPad=near;}
                    if(tapBudget!=0&&pourClock<=0){
                        if(PourOne(near)){if(tapBudget>0)tapBudget--;pourClock=Dwell<1.2f?.42f:Dwell<2.4f?.19f:.09f;}
                    }
                }else{Dwell=0;wasStopped=false;}
                // Walking out commits; going onto either pad during the ring cancels
                // the confirmation and keeps every coin already poured.
                if(Confirming&&near<0){Confirm+=dt;if(Confirm>=ConfirmTime){Commit(visitedPad);return Math.Max(0,visitedPad);}}
            }
            if(Elapsed>=TimeLimit){Commit(-1);return 4;}return -1;
        }
        public bool PourOne(int pad){
            if(!Active||Pending||Current.Mode=="choice"||pad<0||pad>=PadCount||Hover!=pad||Coins<=0||Poured[pad]>=Current.Max)return false;
            Coins--;Spent++;PourCount++;Poured[pad]++;OnPour?.Invoke(pad,Poured[pad]);return true;
        }
        public bool Select(int pad){if(Current==null||Current.Mode!="choice")return false;return Commit(pad);}
        bool Commit(int pad){
            if(!Active||Pending)return false;LastPad=pad;
            LastCorrect=pad>=0&&(Current.Mode=="choice"?pad<4&&PackItem.EquivalentChoice(Choices[pad],Current.answer):Current.Accepts(Poured[0],Poured[1]));
            Attempts++;Pending=true;Dwell=0;Confirming=false;
            if(LastCorrect){Correct++;Score+=100+Math.Max(0,24-(int)Elapsed);Invested+=TotalPoured;}
            else Hp=Math.Max(0,Hp-18);
            return true;
        }
        public void Damage(int damage){if(Active){Hp=Math.Max(0,Hp-damage);if(Hp==0){Active=false;Ended=true;}}}
        public int AnswerPad(){if(Current==null||Current.Mode!="choice")return -1;for(int i=0;i<4;i++)if(PackItem.EquivalentChoice(Choices[i],Current.answer))return i;return -1;}
        public bool Won=>Ended&&Hp>0&&Attempts==10&&Correct>=7;
    }
}
