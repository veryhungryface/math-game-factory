// Phase 2: production movement/pouring/leave-confirm rules. Blind bots never call
// Select, PourOne, AnswerPad or Accepts. Coin pickup is an explicit, answer-independent
// combat fixture; browser integration must separately validate real drop/collection.
// Unity -batchmode -nographics -projectPath <workspace>
//   -executeMethod Mgf.HyeopgokSasu.HyeopgokBotSelfTest.Run
//   -hyeopgokRepo /Users/sitpo/math-game-factory -quit
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    public static class HyeopgokBotSelfTest
    {
        const int Seed=20260926,Games=200;
        const float Dt=1f/60f;
        enum Bot { RandomAmount,AllCoins,OneCoin,Idle,LongHold,Perfect,FirstWrong }
        [Serializable] public sealed class Bucket {
            public int presented,submitted,correct,timeouts;
            public double expectedCorrect,firstAttemptRate,presentedProgressRate,chanceRate,ci95Low,ci95High;
            public bool rawChanceGate;
        }
        [Serializable] public sealed class Result {
            public string pack,bot,strategy;
            public int games=Games,victories,completed,maximumSolved;
            public bool blind,nominalChanceGate;
            public double meanSeconds;
            public Bucket all=new Bucket(),tutorial=new Bucket(),main=new Bucket();
        }
        [Serializable] public sealed class Domain {
            public string pack;
            public int items,amount,fraction,choice,acceptedInputs,enumeratedInputs,acceptanceMismatches,correctPaths;
            public double uniformChanceMean;
        }
        [Serializable] public sealed class Report {
            public int schema_version=2,seed=Seed,gamesPerBotPerPack=Games;
            public string seedDerivation="Fixed seed 20260926 + game index; blind input RNG uses fixed independent strategy/wave domains. No favorable seed search.";
            public string model="Production Rules.Start/Move/Tick; dt 1/60; per-wave AddCoins(WalletCapacity) combat fixture independent of answer; 0.35s intentional empty selection; 0.6s leave confirmation.";
            public string limitation="Model fixture excludes battlefield damage, rendered pickup and browser pointer dispatch. Raw finite observations are not replaced by exact-domain expectations.";
            public bool nominalChanceGate,uniformChanceGate,controlsPassed,v1CompatibilityPassed,acceptModesPassed;
            public int modelErrors;
            public bool coinInteractionRegressionsPassed,choiceEquivalenceRegressionsPassed,runtimePackValidationPassed;
            public PackValidationCase[] runtimePackValidation;
            public Result[] results;
            public Domain[] domains;
            public UniformPaths uniformPaths;
            public string[] failures;
        }
        [Serializable] public sealed class PackValidationCase {
            public string name;
            public bool expectedValid,actualValid,pass;
        }
        [Serializable] public sealed class UniformPaths {
            public string method="Exhaustive production Move/Tick input transport for every amount and pair, grouped by public mode/max geometry. Answer-independent trajectories are reused in each pack item integer-domain audit.";
            public int amountPaths,fractionPaths,pathErrors;
            public long expandedItemInputs;
            public bool exactUniformChance;
        }
        static readonly List<string> Failures=new List<string>();
        static int errorCount;
        static void Fail(string text){errorCount++;if(Failures.Count<100)Failures.Add(text);}
        static bool Blind(Bot bot)=>bot<=Bot.LongHold;
        static string Label(Bot b){switch(b){case Bot.RandomAmount:return "무작위 양";case Bot.AllCoins:return "항상 전부";case Bot.OneCoin:return "항상 1닢";case Bot.Idle:return "무입력";case Bot.LongHold:return "오래 서기";case Bot.Perfect:return "정답 대조군";default:return "첫 오답 회복";}}
        static string Strategy(Bot b){switch(b){case Bot.RandomAmount:return "각 패드 0..max 균등 독립 목표를 정해 이동·붓기·이탈; choice는 균등 위치";case Bot.AllCoins:return "패드 상한까지 보유 코인을 모두 붓고 이탈; 두 패드는 차례대로 상한; choice는 첫 위치";case Bot.OneCoin:return "각 패드에 tap 한 번으로 1닢씩 붓고 이탈; choice는 첫 위치";case Bot.Idle:return "입력 없이 화면 제한 시간까지 대기";case Bot.LongHold:return "첫 패드에서 계속 서 있고 이탈하지 않음; choice는 첫 위치";case Bot.Perfect:return "정답량을 실제 Move/Tick 경로로 붓고 이탈";default:return "첫 문항만 틀린 양/위치, 이후 정답량을 실제 경로로 붓기";}}
        static int InputSeed(int game,int wave,Bot bot){unchecked{return game*397^wave*8191^(int)bot*104729;}}
        static int Gcd(int a,int b){while(b!=0){int t=a%b;a=b;b=t;}return Math.Max(1,a);}
        static bool IndependentAccept(PackItem item,int a,int n){
            if(item.Mode=="amount")return a==item.answerValue;
            if(item.Mode!="fraction_parts"||a<=0||n<0)return false;
            if(string.IsNullOrEmpty(item.accept)||item.accept=="exact_parts")return a==item.answerParts.den&&n==item.answerParts.num;
            bool same=(long)n*item.answerParts.den==(long)item.answerParts.num*a;
            return same&&(item.accept!="reduced"||Gcd(n,a)==1);
        }
        static double Chance(PackItem item){
            if(item.Mode=="choice")return 1d/item.choices.Length;
            long accepted=0,side=(long)item.Max+1;
            if(item.Mode=="amount")return 1d/side;
            for(int a=0;a<=item.Max;a++)for(int n=0;n<=item.Max;n++)if(IndependentAccept(item,a,n))accepted++;
            return accepted/(double)(side*side);
        }
        static void BeginPad(HyeopgokRules r,int pad,int amount,bool tap){
            Vector3 destination=HyeopgokRules.Pads[pad];
            // Zero follows the same route but stops for VisitHold; Play leaves as
            // soon as Visited is set, before InitialPourDelay can spend a coin.
            r.Move(destination,tap);
        }
        static int Play(HyeopgokRules r,Bot bot,int game,int wave,int requested0=-1,int requested1=-1){
            var input=new System.Random(InputSeed(game,wave,bot));
            int target0=0,target1=0,pad=0;bool started=false,leaving=false;
            if(r.Current.Mode=="choice"){
                if(!Blind(bot)){pad=r.AnswerPad();if(bot==Bot.FirstWrong&&wave==1)pad=(pad+1)%4;}
                else pad=bot==Bot.RandomAmount?input.Next(4):0;
            }else if(requested0>=0){target0=requested0;target1=Math.Max(0,requested1);}
            else if(!Blind(bot)){
                if(r.Current.Mode=="amount")target0=r.Current.answerValue;
                else {target0=r.Current.answerParts.den;target1=r.Current.answerParts.num;if(r.Current.accept=="reduced"){int gcd=Gcd(target0,target1);target0/=gcd;target1/=gcd;}}
                if(bot==Bot.FirstWrong&&wave==1)target0=target0<r.Current.Max?target0+1:target0-1;
            }else{
                target0=bot==Bot.OneCoin?1:bot==Bot.RandomAmount?input.Next(r.Current.Max+1):r.Current.Max;
                target1=bot==Bot.OneCoin?1:bot==Bot.RandomAmount?input.Next(r.Current.Max+1):r.Current.Max;
            }
            int maxTicks=(int)Math.Ceiling(r.TimeLimit/Dt)+120;
            for(int tick=0;tick<maxTicks;tick++){
                if(bot!=Bot.Idle&&!started){
                    if(r.Current.Mode=="choice")r.Move(HyeopgokRules.Pads[pad]);
                    else BeginPad(r,0,target0,bot==Bot.OneCoin);
                    started=true;
                }
                r.Tick(Dt);
                if(r.Pending||r.Ended)return tick+1;
                if(bot==Bot.Idle||bot==Bot.LongHold||r.Current.Mode=="choice"||leaving)continue;
                int amount=pad==0?target0:target1;
                if(r.Visited[pad]&&r.Poured[pad]>=amount){
                    if(r.PadCount==2&&pad==0){pad=1;BeginPad(r,1,target1,bot==Bot.OneCoin);}
                    else {r.Move(HyeopgokRules.Exit);leaving=true;}
                }
            }
            Fail("Unresolved question "+r.Current.id+" / "+Label(bot));return maxTicks;
        }
        static void Count(Bucket b,HyeopgokRules r,double chance){
            b.presented++;
            if(r.LastPad>=0){b.submitted++;b.expectedCorrect+=chance;if(r.LastCorrect)b.correct++;}
            else b.timeouts++;
        }
        static void Finish(Bucket b){
            b.presentedProgressRate=b.presented==0?0:b.correct/(double)b.presented;
            b.firstAttemptRate=b.submitted==0?0:b.correct/(double)b.submitted;
            b.chanceRate=b.submitted==0?0:b.expectedCorrect/b.submitted;
            b.rawChanceGate=b.correct<=b.expectedCorrect+1e-9;
            if(b.submitted==0)return;
            double n=b.submitted,p=b.firstAttemptRate,z=1.95996398454005,scale=1+z*z/n;
            double center=(p+z*z/(2*n))/scale,half=z*Math.Sqrt(p*(1-p)/n+z*z/(4*n*n))/scale;
            b.ci95Low=center-half;b.ci95High=center+half;
        }
        static Result RunBot(QuestionPack pack,Bot bot){
            var result=new Result{pack=pack.pack_id,bot=Label(bot),strategy=Strategy(bot),blind=Blind(bot)};
            long ticks=0;
            for(int game=0;game<Games;game++){
                var r=new HyeopgokRules();r.Start(pack,Seed+game);int safety=0;
                while(!r.Ended&&safety++<11){
                    int wave=r.Wave;
                    // Equal, ample coin collection fixture for every blind strategy;
                    // hidden answer never determines wallet or earning opportunities.
                    r.AddCoins(r.WalletCapacity);
                    double chance=Chance(r.Current);
                    ticks+=Play(r,bot,Seed+game,wave);
                    Count(result.all,r,chance);Count(wave==1?result.tutorial:result.main,r,chance);
                    int attempts=r.Attempts,spent=r.Spent;
                    for(int i=0;i<93;i++)r.Tick(Dt);
                    if(r.Attempts!=attempts||r.Spent!=spent)Fail("Pending allowed duplicate spend/attempt");
                    ticks+=93;r.Next();
                }
                if(!r.Ended)Fail("Unfinished run "+pack.pack_id+" / "+Label(bot));
                if(r.Attempts==10)result.completed++;if(r.Won)result.victories++;
                result.maximumSolved=Math.Max(result.maximumSolved,r.Correct);
                if(bot==Bot.Idle&&(r.Correct!=0||r.Score!=0||r.Spent!=0))Fail("Idle made progress");
                if(bot==Bot.Perfect&&(!r.Won||r.Correct!=10||r.Hp!=100))Fail("Perfect control failed "+pack.pack_id+" seed "+(Seed+game));
                if(bot==Bot.FirstWrong&&(!r.Won||r.Correct!=9||r.Hp!=82))Fail("First-error recovery failed "+pack.pack_id+" seed "+(Seed+game));
            }
            Finish(result.all);Finish(result.tutorial);Finish(result.main);
            result.nominalChanceGate=!result.blind||result.all.rawChanceGate;
            result.meanSeconds=ticks*Dt/Games;return result;
        }
        static Domain AuditDomain(QuestionPack pack){
            var d=new Domain{pack=pack.pack_id};
            foreach(var item in pack.items){
                d.items++;double chance=Chance(item);d.uniformChanceMean+=chance;
                if(item.Mode=="choice"){d.choice++;d.enumeratedInputs+=item.choices.Length;d.acceptedInputs++;}
                else{
                    if(item.Mode=="amount")d.amount++;else d.fraction++;
                    for(int a=0;a<=item.Max;a++)for(int n=0;n<=(item.Mode=="fraction_parts"?item.Max:0);n++){
                        bool expected=IndependentAccept(item,a,n);d.enumeratedInputs++;if(expected)d.acceptedInputs++;
                        if(item.Accepts(a,n)!=expected)d.acceptanceMismatches++;
                    }
                }
                var one=new QuestionPack{pack_id="audit",items=new[]{item},economy=pack.economy};
                var correct=new HyeopgokRules();correct.Start(one,Seed);correct.AddCoins(correct.WalletCapacity);
                Play(correct,Bot.Perfect,Seed,1);
                if(correct.Pending&&correct.LastCorrect&&correct.Attempts==1)d.correctPaths++;else Fail("All-item correct path failed: "+item.id);
                var timed=new HyeopgokRules();timed.Start(one,Seed);float limit=timed.TimeLimit;
                timed.Tick(limit-.02f);if(timed.Pending||timed.Attempts!=0)Fail("Early timeout "+item.id);
                timed.Tick(.03f);if(!timed.Pending||timed.LastCorrect||timed.LastPad!=-1||timed.Attempts!=1||timed.Hp!=82)Fail("Timeout contract "+item.id);
            }
            d.uniformChanceMean/=Math.Max(1,d.items);
            if(d.acceptanceMismatches>0)Fail("Acceptance domain mismatch: "+pack.pack_id);return d;
        }
        static UniformPaths ExhaustUniformPaths(List<QuestionPack> packs,List<Domain> domains){
            var proof=new UniformPaths();var seen=new HashSet<string>();
            foreach(var pack in packs)foreach(var source in pack.items){
                if(source.Mode=="choice")continue;
                string key=source.Mode+":"+source.Max;
                if(!seen.Add(key))continue;
                var item=new PackItem{id="transport-"+key,answer_mode=source.Mode,max=source.Max,answerValue=2,answerParts=new FractionAnswer{num=2,den=4},accept="exact_parts",difficulty=1};
                var intro=new PackItem{id="transport-intro-"+key,answer_mode="amount",answerValue=2,max=60,difficulty=1};
                var fixture=new QuestionPack{pack_id="uniform-input-transport",items=new[]{intro,item},economy=pack.economy};
                for(int a=0;a<=item.Max;a++)for(int n=0;n<=(item.Mode=="fraction_parts"?item.Max:0);n++){
                    var r=new HyeopgokRules();r.Start(fixture,Seed);r.Next();r.AddCoins(r.WalletCapacity);
                    Play(r,Bot.RandomAmount,Seed,1,a,n);
                    if(item.Mode=="amount")proof.amountPaths++;else proof.fractionPaths++;
                    // The path never reads correctness until its single exit commit;
                    // every possible target is physically conveyed without clamping.
                    bool valid=r.Pending&&r.LastPad>=0&&r.Poured[0]==a&&r.Poured[1]==n&&r.Spent==a+n&&r.Attempts==1;
                    valid&=r.LastCorrect==IndependentAccept(item,a,n);
                    if(!valid){proof.pathErrors++;Fail("Uniform actual input path "+key+" ("+a+","+n+")");}
                }
            }
            foreach(var d in domains){proof.expandedItemInputs+=d.enumeratedInputs;if(d.acceptanceMismatches!=0)proof.pathErrors+=d.acceptanceMismatches;}
            proof.exactUniformChance=proof.pathErrors==0;
            return proof;
        }
        static bool CoinInteractionRegressions(){
            int before=errorCount;
            var zeroItem=new PackItem{id="zero-intent",answer_mode="amount",answerValue=0,max=60,difficulty=1};
            var zeroPack=new QuestionPack{items=new[]{zeroItem}};
            var fly=new HyeopgokRules();fly.Start(zeroPack,Seed);fly.AddCoins(fly.WalletCapacity);
            Vector3 through=HyeopgokRules.Pads[0]-fly.King;through.y=0;through=HyeopgokRules.Pads[0]+through.normalized*1.25f;
            fly.Move(through);int flyGuard=0;while((fly.King-fly.Target).sqrMagnitude>.012f&&flyGuard++<300)fly.Tick(Dt);
            for(int i=0;i<90;i++)fly.Tick(Dt);
            if(fly.Visited[0]||fly.Confirming||fly.Pending||fly.Attempts!=0||fly.Hp!=100||fly.Poured[0]!=0||fly.Spent!=0)Fail("Fly-through must not visit, confirm or submit zero");
            var deliberate=new HyeopgokRules();deliberate.Start(zeroPack,Seed);deliberate.AddCoins(deliberate.WalletCapacity);deliberate.Move(HyeopgokRules.Pads[0]);
            int zeroGuard=0;while(!deliberate.Visited[0]&&!deliberate.Pending&&zeroGuard++<300)deliberate.Tick(Dt);
            deliberate.Move(HyeopgokRules.Exit);zeroGuard=0;while(!deliberate.Pending&&zeroGuard++<300)deliberate.Tick(Dt);
            if(!deliberate.LastCorrect||deliberate.Attempts!=1||deliberate.Poured[0]!=0||deliberate.Spent!=0)Fail("Deliberate stop then leave must submit zero without spending");
            var introItem=new PackItem{id="intro-must-pour",answer_mode="amount",answerValue=2,max=60,difficulty=1};
            var intro=new HyeopgokRules();intro.Start(new QuestionPack{items=new[]{introItem}},Seed);intro.AddCoins(intro.WalletCapacity);intro.Move(HyeopgokRules.Pads[0]);
            int introGuard=0;while(!intro.Visited[0]&&!intro.Pending&&introGuard++<300)intro.Tick(Dt);
            intro.Move(HyeopgokRules.Exit);for(int i=0;i<180;i++)intro.Tick(Dt);
            if(intro.Pending||intro.Confirming||intro.Attempts!=0||intro.Hp!=100||intro.Poured[0]!=0||!intro.TutorialBlocked)Fail("Opening tutorial must reject an empty stop-and-leave");
            var item=new PackItem{id="reentry",answer_mode="amount",answerValue=2,max=60,difficulty=1};
            var pack=new QuestionPack{items=new[]{item}};
            var r=new HyeopgokRules();r.Start(pack,Seed);r.AddCoins(r.WalletCapacity);r.Move(HyeopgokRules.Pads[0],true);
            int guard=0;while(r.Poured[0]<1&&!r.Pending&&guard++<200)r.Tick(Dt);
            if(r.Poured[0]!=1)Fail("Tap must pour exactly one before leave");
            r.Move(HyeopgokRules.Exit);guard=0;while(r.Hover>=0&&!r.Pending&&guard++<200)r.Tick(Dt);
            if(!r.Confirming||r.Pending)Fail("Leave must begin confirmation ring");
            for(int i=0;i<8;i++)r.Tick(Dt);
            r.Move(HyeopgokRules.Pads[0],true);guard=0;while(r.Hover!=0&&!r.Pending&&guard++<200)r.Tick(Dt);
            if(r.Pending||r.Confirming||r.Poured[0]!=1)Fail("Reentry must cancel ring without removing poured coin");
            guard=0;while(r.Poured[0]<2&&!r.Pending&&guard++<200)r.Tick(Dt);
            if(r.Poured[0]!=2)Fail("Reentry must allow one additional tap coin");
            r.Move(HyeopgokRules.Exit);guard=0;while(!r.Pending&&guard++<200)r.Tick(Dt);
            if(!r.LastCorrect||r.Spent!=2||r.Invested!=2)Fail("Reentry final amount must invest exactly two coins");
            var over=new HyeopgokRules();over.Start(pack,Seed);over.AddCoins(over.WalletCapacity);Play(over,Bot.RandomAmount,Seed,1,3,0);
            if(over.LastCorrect||over.Spent!=3||over.Invested!=0||over.Hp!=82)Fail("Overpour must lose all three coins and be wrong");
            var dry=new HyeopgokRules();dry.Start(pack,Seed);dry.Move(HyeopgokRules.Pads[0]);
            for(int i=0;i<180;i++)dry.Tick(Dt);
            if(dry.Pending||dry.Poured[0]!=0||dry.Spent!=0)Fail("Empty wallet must not pour or auto-submit");
            dry.AddCoins(2);guard=0;while(dry.Poured[0]<2&&!dry.Pending&&guard++<200)dry.Tick(Dt);
            dry.Move(HyeopgokRules.Exit);guard=0;while(!dry.Pending&&guard++<200)dry.Tick(Dt);
            if(!dry.LastCorrect||dry.Spent!=2)Fail("Earning after empty wallet must resume physical pouring");
            var cap=new HyeopgokRules();cap.Start(pack,Seed);cap.AddCoins(cap.WalletCapacity);cap.Move(HyeopgokRules.Pads[0]);
            guard=0;while(cap.Poured[0]<60&&!cap.Pending&&guard++<1200)cap.Tick(Dt);
            for(int i=0;i<60;i++)cap.Tick(Dt);
            if(cap.Poured[0]!=60||cap.Spent!=60||cap.Coins!=cap.WalletCapacity-60||cap.Pending)Fail("Holding at max must neither overpour nor auto-submit");
            return errorCount==before;
        }
        static bool ChoiceEquivalenceRegressions(){
            int before=errorCount;
            var item=new PackItem{id="choice-equivalent-defense",answer_mode="choice",answer_type="choice",answer="{frac:1/2}",choices=new[]{"0","{frac:2/4}","1","2"},difficulty=1};
            var r=new HyeopgokRules();r.Start(new QuestionPack{items=new[]{item}},Seed);
            int pad=r.AnswerPad();
            if(pad<0)Fail("Equivalent rational choice must remain highlightable defensively");
            else {r.Select(pad);if(!r.LastCorrect||r.Attempts!=1)Fail("Equivalent rational choice must score by the same policy as AnswerPad");}
            return errorCount==before;
        }
        static void ValidateRuntimeCase(List<PackValidationCase> cases,string name,QuestionPack pack,bool expected){
            bool actual=HyeopgokGame.ValidPack(pack);
            cases.Add(new PackValidationCase{name=name,expectedValid=expected,actualValid=actual,pass=actual==expected});
            if(actual!=expected){
                Fail("Runtime pack validation "+name+": expected "+expected+", got "+actual);
                Debug.Log("PACK_VALIDATION_DIAGNOSTIC "+name+" schema="+pack.schema_version+" items="+pack.items.Length+" economy="+(pack.economy==null?"null":JsonUtility.ToJson(pack.economy)));
            }
        }
        static bool RuntimeValidationRegressions(string repo,out PackValidationCase[] results){
            int before=errorCount;var cases=new List<PackValidationCase>();
            string probability=File.ReadAllText(Path.Combine(repo,"public/g/hyeopgok-sasu/packs/m2s2-u7.json"));
            string counting=File.ReadAllText(Path.Combine(repo,"public/g/hyeopgok-sasu/packs/m2s2-u6.json"));
            ValidateRuntimeCase(cases,"default-v2-probability",HyeopgokPackJson.Parse(probability),true);
            ValidateRuntimeCase(cases,"default-v2-counting",HyeopgokPackJson.Parse(counting),true);
            int economyStart=probability.IndexOf("\"economy\":",StringComparison.Ordinal);
            int economyEnd=probability.IndexOf('}',economyStart)+1;
            if(probability[economyEnd]==',')economyEnd++;
            var missingEconomy=HyeopgokPackJson.Parse(probability.Remove(economyStart,economyEnd-economyStart));
            ValidateRuntimeCase(cases,"reject-v2-missing-economy",missingEconomy,false);
            // Parse each mutation independently: a previous invalid setting cannot
            // cause a later check to pass for the wrong reason.
            var supply=HyeopgokPackJson.Parse(probability);supply.economy.min_spawn_coins=100000;
            ValidateRuntimeCase(cases,"reject-impossible-supply-100000",supply,false);
            var carry=HyeopgokPackJson.Parse(probability);carry.economy.carry_capacity=241;
            ValidateRuntimeCase(cases,"reject-unsupported-capacity-241",carry,false);
            var cap=HyeopgokPackJson.Parse(probability);cap.items[0].max=61;
            ValidateRuntimeCase(cases,"reject-pad-max-61",cap,false);
            var budget=HyeopgokPackJson.Parse(probability);budget.items[0].coin_budget=1;
            ValidateRuntimeCase(cases,"reject-underbudget-first-answer-2",budget,false);
            var equivalentOnly=HyeopgokPackJson.Parse(probability);PackItem equivalentChoice=null;
            foreach(var item in equivalentOnly.items)if(item.Mode=="choice"){equivalentChoice=item;break;}
            if(equivalentChoice==null)Fail("Missing choice fixture source");
            else {equivalentChoice.answer="{frac:1/2}";equivalentChoice.answer_type="choice";equivalentChoice.format="frac";equivalentChoice.answerNumeric=.5;equivalentChoice.choices=new[]{"0","1","{frac:2/4}","2"};}
            ValidateRuntimeCase(cases,"reject-equivalent-only-choice-without-exact-token",equivalentOnly,false);
            var legacy=new StringBuilder("{\"pack_id\":\"legacy-validator-fixture\",\"title\":\"v1 validation\",\"items\":[");
            for(int i=0;i<10;i++){
                if(i>0)legacy.Append(',');
                legacy.Append("{\"id\":\"legacy-validation-").Append(i).Append("\",\"prompt\":\"2+3은?\",\"choices\":[\"4\",\"5\",\"6\",\"7\"],\"answer\":\"5\",\"answerNumeric\":5,\"explain\":\"2와 3을 더하면 5입니다.\",\"format\":\"int\",\"difficulty\":1}");
            }
            legacy.Append("]}");
            var v1=HyeopgokPackJson.Parse(legacy.ToString());
            ValidateRuntimeCase(cases,"legacy-v1-without-mode-or-economy",v1,true);
            foreach(var item in v1.items)if(item.Mode!="choice")Fail("Legacy validator changed missing answer_mode");
            results=cases.ToArray();return errorCount==before;
        }
        static bool Compatibility(){
            const string json="{\"pack_id\":\"legacy-fixture\",\"items\":[{\"id\":\"legacy\",\"prompt\":\"2+3은?\",\"choices\":[\"4\",\"5\",\"6\",\"7\"],\"answer\":\"5\",\"answerNumeric\":5,\"difficulty\":1}]}";
            var pack=HyeopgokPackJson.Parse(json);var r=new HyeopgokRules();r.Start(pack,Seed);Play(r,Bot.Perfect,Seed,1);
            bool ok=r.Current.Mode=="choice"&&r.PadCount==4&&r.LastCorrect&&r.Spent==0&&r.Attempts==1;
            if(!ok)Fail("v1 missing answer_mode compatibility");return ok;
        }
        static bool AcceptanceFixtures(){
            int before=errorCount;
            foreach(string accept in new[]{"exact_parts","equivalent","reduced"}){
                var item=new PackItem{id="fixture-"+accept,answer_mode="fraction_parts",accept=accept,max=12,answerParts=new FractionAnswer{num=2,den=4},difficulty=1};
                for(int den=0;den<=12;den++)for(int num=0;num<=12;num++)if(item.Accepts(den,num)!=IndependentAccept(item,den,num))Fail("Fixture accept "+accept);
                // Valid equivalent/reduced witness uses an independently authored answer.
                if(accept=="reduced")item.answerParts=new FractionAnswer{num=1,den=2};
                var r=new HyeopgokRules();r.Start(new QuestionPack{items=new[]{item}},Seed);r.AddCoins(r.WalletCapacity);Play(r,Bot.Perfect,Seed,1);
                if(!r.LastCorrect)Fail("Fixture actual path "+accept);
            }
            return before==errorCount;
        }
        static string P(double p)=>(p*100).ToString("0.000",CultureInfo.InvariantCulture)+"%";
        static void Table(StringBuilder md,List<Result> rows,Func<Result,Bucket> select){
            md.AppendLine("| 팩 | 봇 | 첫 정답/확정 | 정답률 | 해당 문항 우연 기준 | 미확정 시간초과 | 원시 ≤ 우연 |\n|---|---|---:|---:|---:|---:|---|");
            foreach(var r in rows){var b=select(r);md.AppendLine("| "+r.pack+" | "+r.bot+" | "+b.correct+"/"+b.submitted+" | "+P(b.firstAttemptRate)+" | "+P(b.chanceRate)+" | "+b.timeouts+" | "+(r.blind?(b.rawChanceGate?"통과":"초과") : "대조군")+" |");}
        }
        public static void Run(){
            Failures.Clear();errorCount=0;string repo=Environment.GetEnvironmentVariable("MGF_HYEOPGOK_REPO");var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-hyeopgokRepo")repo=args[i+1];
            if(string.IsNullOrEmpty(repo))repo="/Users/sitpo/math-game-factory";
            var rows=new List<Result>();var domains=new List<Domain>();var packs=new List<QuestionPack>();
            foreach(string id in new[]{"m2s2-u7","m2s2-u6"}){
                var pack=HyeopgokPackJson.Parse(File.ReadAllText(Path.Combine(repo,"public/g/hyeopgok-sasu/packs/"+id+".json")));
                packs.Add(pack);domains.Add(AuditDomain(pack));foreach(Bot bot in Enum.GetValues(typeof(Bot)))rows.Add(RunBot(pack,bot));
            }
            bool nominal=true,controls=true;foreach(var r in rows){if(r.blind&&!r.nominalChanceGate)nominal=false;if(!r.blind&&r.victories!=Games)controls=false;}
            bool legacy=Compatibility(),accept=AcceptanceFixtures(),coinEdges=CoinInteractionRegressions(),choiceEquivalence=ChoiceEquivalenceRegressions();
            bool packValidation=RuntimeValidationRegressions(repo,out PackValidationCase[] packValidationCases);var uniform=ExhaustUniformPaths(packs,domains);
            var report=new Report{nominalChanceGate=nominal,uniformChanceGate=uniform.exactUniformChance,uniformPaths=uniform,controlsPassed=controls,v1CompatibilityPassed=legacy,acceptModesPassed=accept,modelErrors=errorCount,coinInteractionRegressionsPassed=coinEdges,choiceEquivalenceRegressionsPassed=choiceEquivalence,runtimePackValidationPassed=packValidation,runtimePackValidation=packValidationCases,results=rows.ToArray(),domains=domains.ToArray(),failures=Failures.ToArray()};
            string source=Path.Combine(repo,"factory/unity-src/hyeopgok-sasu"),output=Path.Combine(source,"ArtSource/validation/phase2");Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"bot-results.json"),JsonUtility.ToJson(report,true)+"\n");
            var md=new StringBuilder("# 협곡 사수 — 2단계 코인 붓기 봇\n\n");
            md.AppendLine("- 고정 시드 **20260926 + 판 번호**, 각 팩·전략 **200판**. 시드·표본 수를 결과에 맞춰 고르지 않았다.");
            md.AppendLine("- 프로덕션 `HyeopgokRules.Start → Move → Tick(1/60초)`의 붓기와 이탈 확정 경로다. 무뇌 봇은 정답을 읽거나 `PourOne`, `Select`, `AnswerPad`, `Accepts`를 호출하지 않는다. 별도 수학 전수 감사만 정답을 읽는다.");
            md.AppendLine("- 코인 획득은 문항마다 `AddCoins(WalletCapacity)`인 **전투 수입 fixture**다. 모든 전략에 같은 최대 보유량을 제공하며 정답에 맞춰 재화를 지급하지 않는다. 실제 처치·드롭·흡수·전투 피해는 브라우저 통합 검증 범위다.");
            md.AppendLine("- 첫 확정만 분모로 삼고 미확정 시간초과는 따로 센다. 시간초과를 섞어 정답률을 낮추지 않는다. 원시 JSON에는 제시 문항 대비 진도와 Wilson 95% 구간도 보존한다.");
            md.AppendLine("- 우연 기준은 amount `1/(max+1)`, exact_parts·reduced `수용 쌍 수/(max+1)²`, equivalent도 같은 영역의 정수 교차곱 수용 쌍을 전수 센다. 분모 0도 균등 입력 영역에 포함하되 오답이다. choice는 1/4다. 서로 다른 문항은 관측된 확정 문항별 기대 정답 수를 더해 비교한다.\n");
            md.AppendLine("## 전체 원시 결과 — 첫 작은 amount 포함\n");Table(md,rows,r=>r.all);
            md.AppendLine("\n## 첫 문항만 — 숨기지 않는 튜토리얼 구간\n");Table(md,rows,r=>r.tutorial);
            md.AppendLine("\n## 2문항 이후 — 본문 구간\n");Table(md,rows,r=>r.main);
            md.AppendLine("\n## 전략과 대조군\n");foreach(var r in rows)if(r.pack==rows[0].pack)md.AppendLine("- "+r.bot+": "+r.strategy+".");
            foreach(var r in rows)if(!r.blind)md.AppendLine("- "+r.pack+" "+r.bot+": 승리 "+r.victories+"/200, 10문항 완료 "+r.completed+"/200.");
            md.AppendLine("\n## 전수 정수 영역·호환 검증\n");
            foreach(var d in domains)md.AppendLine("- "+d.pack+": "+d.items+"문항(amount "+d.amount+" / fraction_parts "+d.fraction+" / choice "+d.choice+"), 정수 입력 영역 "+d.enumeratedInputs+"개, 수용 "+d.acceptedInputs+"개, 독립 판정 불일치 "+d.acceptanceMismatches+"개, 실제 붓기 정답 경로 "+d.correctPaths+"/"+d.items+".");
            md.AppendLine("- 균등 입력 전수: amount 실제 동선 "+uniform.amountPaths+"개, fraction 실제 동선 "+uniform.fractionPaths+"개, 입력 운반 불일치 "+uniform.pathErrors+"개. 모드·max별 같은 동선은 정답을 읽지 않고 최종 양을 운반하므로 위 "+uniform.expandedItemInputs+"개 문항별 정수 영역에 그대로 대조한다. 이는 전체 입력 지지집합에서 정확히 우연 수준인지 확인하는 별도 검증이며 고정 시드 원시표를 대체하지 않는다.");
            md.AppendLine("- v1 answer_mode 누락 fixture의 실제 패드 선택: "+(legacy?"통과":"실패")+". exact_parts/equivalent/reduced fixture: "+(accept?"통과":"실패")+".");
            md.AppendLine("- 런타임 팩 검증 "+packValidationCases.Length+"건: "+(packValidation?"통과":"실패")+". 기본 v2 두 팩과 경제/answer_mode 없는 v1은 허용, v2 경제 누락·공급 100000·소지 241·패드 max 61·정답 2의 예산 1·exact 정답 토큰 없이 동치 보기만 있는 choice는 각각 독립 복제본에서 거부한다.");
            md.AppendLine("- fly-through 무시·의도적 stop→0 확정·첫 튜토리얼 빈 제출 차단, 확인 링 재진입 취소·기존 코인 보존·한 닢 추가→정답 투자, 과다 붓기 소실, 빈 지갑 유지→획득 후 붓기 재개, 상한에서 오래 서도 자동 제출/추가 소모 없음: "+(coinEdges?"통과":"실패")+".");
            md.AppendLine("- choice 로더 exact-token 강제와 유리수 동치 채점/정답 표시 공통 정책: "+(choiceEquivalence?"통과":"실패")+".");
            md.AppendLine("- 모든 문항의 제한 직전 미확정·직후 오답/-18, 피드백 중 중복 제출·추가 소모 금지를 검사한다.");
            md.AppendLine("\n## 판정\n\n- 전체 원시 첫 시도 정답률 ≤ 해당 문항 우연: **"+(nominal?"통과":"미충족")+"**. 구간별 초과도 위에 보존했다.");
            md.AppendLine("- 전체 균등 입력 지지집합의 실제 경로와 정확 우연 수준: **"+(uniform.exactUniformChance?"통과":"실패")+"**. 원시 표본 판정과 별도다.");
            md.AppendLine("- 대조군: **"+(controls?"통과":"실패")+"**, 모델 오류 **"+errorCount+"건**. 유한 무작위 표본은 기대값을 넘을 수 있으며, 기대값 증명을 원시 초과의 대체 통과로 쓰지 않는다.");
            md.AppendLine("- 이전 1단계 결과는 git 이력과 `validation/bot-results.json`에 남겨 두며 이번 결과는 `validation/phase2/bot-results.json`이다. 실제 화면·입력·전투·성능 증거와 구별한다.");
            foreach(string failure in Failures)md.AppendLine("- 오류: "+failure);
            File.WriteAllText(Path.Combine(source,"ArtSource/bot-results.md"),md.ToString());
            Debug.Log("HYEOPGOK_PHASE2_BOTS "+JsonUtility.ToJson(report));
            EditorApplication.Exit(errorCount>0||!controls||!legacy||!accept||!coinEdges||!choiceEquivalence||!packValidation||!uniform.exactUniformChance?1:nominal?0:2);
        }
    }
}
#endif
