using System;
using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    // This is the production movement/answer model, also used by the headless bots.
    // Math is decided by canonical exact strings (reduced integer fractions in the verified pack).
    public sealed class HyeopgokRules
    {
        public static readonly Vector3[] Pads = {
            new Vector3(-1.8f,1.23f,2.0f), new Vector3(.6f,1.23f,2.0f),
            new Vector3(-1.8f,1.23f,-1.0f), new Vector3(.6f,1.23f,-1.0f)
        };
        public const float Speed=4.8f, Hold=.8f, Radius=.69f, Limit=24f;
        // Keep pack JSON unchanged. Difficulty already describes calculation load:
        // elementary single events 24s, counting two dice 32s, compound probability 40s.
        public float TimeLimit {get; private set;}=Limit;
        public static float LimitForDifficulty(int difficulty)=>difficulty<=1?24f:difficulty==2?32f:40f;
        public QuestionPack Pack {get; private set;}
        public PackItem Current {get; private set;}
        public readonly string[] Choices=new string[4];
        public Vector3 King,Target;
        public int Wave, Correct, Attempts, Hp, Coins, Score, Hover=-1, LastPad=-1;
        public bool Active, Pending, LastCorrect, Ended;
        public float Dwell, Elapsed;
        System.Random orderRng, placeRng;
        readonly int[] used=new int[10];
        // Domain-separated seed expansion using the published SplitMix64 mix.
        // Fixed standard constants, never chosen from bot outcomes.
        // Sebastiano Vigna, public domain: https://prng.di.unimi.it/splitmix64.c
        static int StreamSeed(int seed,ulong stream)
        {
            unchecked {
                ulong z=(uint)seed+0x9e3779b97f4a7c15UL*(stream+1);
                z=(z^(z>>30))*0xbf58476d1ce4e5b9UL;
                z=(z^(z>>27))*0x94d049bb133111ebUL;
                return (int)(z^(z>>31));
            }
        }
        public void Start(QuestionPack pack,int seed)
        {
            Pack=pack; orderRng=new System.Random(StreamSeed(seed,0)); placeRng=new System.Random(StreamSeed(seed,1));
            Wave=Correct=Attempts=Score=0; Hp=100; Coins=0; Active=true; Pending=Ended=false;
            for(int i=0;i<10;i++)used[i]=-1;
            Next();
        }
        public void Next()
        {
            if(Hp<=0 || Wave>=10){ Active=false; Ended=true; Pending=false; return; }
            int band=Wave<3?1:Wave<6?2:Wave<8?3:4;
            int at=0,count=0;
            bool hasBand=false;
            for(int i=0;i<Pack.items.Length;i++)if(Math.Max(1,Pack.items[i].difficulty)==band){
                bool seen=false;for(int k=0;k<Wave;k++)if(used[k]==i)seen=true;
                if(!seen)hasBand=true;
            }
            // Reservoir selection from the specified difficulty band, without retries or infinite loops.
            for(int i=0;i<Pack.items.Length;i++){
                var p=Pack.items[i];int difficulty=Math.Max(1,p.difficulty);
                if(hasBand?difficulty!=band:difficulty>band)continue;
                bool seen=false;for(int k=0;k<Wave;k++)if(used[k]==i)seen=true;
                if(!seen && orderRng.Next(++count)==0)at=i;
            }
            if(Wave==0)at=0;
            used[Wave]=at; Current=Pack.items[at];
            TimeLimit=LimitForDifficulty(Current.difficulty);
            for(int i=0;i<4;i++)Choices[i]=Current.choices[i];
            for(int i=3;i>0;i--){int j=placeRng.Next(i+1);string t=Choices[i];Choices[i]=Choices[j];Choices[j]=t;}
            King=Target=new Vector3(-.6f,1.24f,-3.1f);
            Dwell=Elapsed=0; Hover=LastPad=-1; Pending=false; Wave++;
        }
        public void Move(Vector3 target){if(!Active||Pending)return;Target=new Vector3(Mathf.Clamp(target.x,-2.75f,1.48f),1.24f,Mathf.Clamp(target.z,-4.2f,3.55f));}
        public int Tick(float dt)
        {
            if(!Active||Pending)return -1;
            Elapsed+=dt; King=Vector3.MoveTowards(King,Target,Speed*dt);
            int near=-1;
            for(int i=0;i<4;i++)if((King-Pads[i]).sqrMagnitude<Radius*Radius)near=i;
            // Standing is required: passing through a pad does not count.
            if(near!=Hover){Hover=near;Dwell=0;}
            if(near>=0 && (King-Target).sqrMagnitude<.03f){Dwell+=dt;if(Dwell>=Hold){Select(near);return near;}}
            else Dwell=0;
            if(Elapsed>=TimeLimit){Select(-1);return 4;}
            return -1;
        }
        public bool Select(int pad)
        {
            if(!Active||Pending)return false;
            LastPad=pad;LastCorrect=pad>=0&&pad<4&&Choices[pad]==Current.answer;
            Attempts++; Pending=true; Dwell=0;
            if(LastCorrect){Correct++;int bonus=Math.Max(0,24-(int)Elapsed);Score+=100+bonus;Coins+=35+bonus;}
            else{Hp=Math.Max(0,Hp-18);Coins=Math.Max(0,Coins-15);}
            return true;
        }
        public void Damage(int damage){if(Active){Hp=Math.Max(0,Hp-damage);if(Hp==0){Active=false;Ended=true;}}}
        public int AnswerPad(){for(int i=0;i<4;i++)if(Choices[i]==Current.answer)return i;return -1;}
        public bool Won=>Ended && Hp>0 && Attempts==10 && Correct>=7;
    }
}
