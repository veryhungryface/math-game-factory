// V3 choice-only blind-bot and reward-stream contract probe.
//
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
        const int Seed=20260927,Games=200,Waves=10,Layers=4;
        const float Dt=.025f,ChanceLimit=.25f;

        enum Bot { Repeat, Cycle, Random, Idle, Nearest }

        [Serializable]
        public sealed class Result
        {
            public string id,pack,bot,strategy;
            public int games=Games,presented,resolved,submitted,commands,correct,timeouts,rejectedCommands,pathErrors;
            public double firstAttemptRate,firstAttemptObservedRate;
            public bool blind=true,progress;
            public bool chanceGate,stratificationGate,pathGate;
        }

        [Serializable]
        public sealed class Aggregate
        {
            public string bot;
            public int packs,games,presented,resolved,commands,correct,timeouts,pathErrors;
            public double firstAttemptObservedRate;
            public bool chanceGate,pathGate;
        }

        [Serializable]
        public sealed class RewardChecks
        {
            public bool orthogonality,wrongConsumesZeroDraws,thirdCorrectGuaranteesTower,pass;
            public int testedSeeds,comparisons,answerMismatches,questionOrderMismatches,rewardOutcomeVariants;
            public int comparedWaves,wrongCases,wrongDrawsBefore,wrongDrawsAfter,guaranteeSeeds,guaranteedTowers;
            public int quietRewardDraws,noisyRewardDraws;
        }

        [Serializable]
        public sealed class Report
        {
            public int schemaVersion=3,schema_version=3,seed=Seed,gamesPerBot=Games,gamesPerBotPerPack=Games,wavesPerGame=Waves,choiceCount=4;
            public string inputContract="Blind bots choose only a physical pad index, call CommandChoicePad once, then resolve only through Tick. They never call Move, Select, AnswerPad, Accepts, or inspect choices/answer.";
            public string stratification="Every four games share one production seed and differ only by a cyclic rotation of each authored choice array. The production shuffle is therefore the same permutation of all four answer positions; an answer-independent physical command is correct exactly once per quartet.";
            public string survivalFixture="After recording each first attempt, the probe restores HP before Next so every deterministic layer observes all ten production order/placement draws. This affects neither the submitted pad nor its adjudication.";
            public int packCount,skippedNonChoiceItems,modelErrors;
            public bool eachBotAtOrBelowChance,allBotsAtOrBelowChance,activeBotsAtOrBelowChance,rewardChecksPassed,pass;
            public double allBotsFirstAttemptObservedRate,activeBotsFirstAttemptObservedRate;
            public Result[] results;
            public Aggregate[] botAggregates;
            public RewardChecks rewardOrthogonality;
            public string[] failures;
        }

        static readonly List<string> Failures=new List<string>();
        static int errorCount;

        static void Fail(string text)
        {
            errorCount++;
            if(Failures.Count<160)Failures.Add(text);
        }

        static string Label(Bot bot)
        {
            switch(bot){
                case Bot.Repeat:return "repeat/fixed";
                case Bot.Cycle:return "cycle";
                case Bot.Random:return "random";
                case Bot.Idle:return "idle";
                default:return "nearest";
            }
        }

        static string Id(Bot bot)
        {
            switch(bot){case Bot.Repeat:return "fixed";case Bot.Cycle:return "cycle";case Bot.Random:return "random";case Bot.Idle:return "idle";default:return "nearest";}
        }

        static string Strategy(Bot bot)
        {
            switch(bot){
                case Bot.Repeat:return "한 게임의 모든 문항에서 0번 물리 패드 반복";
                case Bot.Cycle:return "문항 번호에 따라 0→1→2→3 물리 패드 순환";
                case Bot.Random:return "정답과 독립된 고정 입력 스트림으로 물리 패드 균등 추첨";
                case Bot.Idle:return "패드 명령 없이 Tick만 진행하여 시간초과";
                default:return "왕의 현재 위치에서 수평거리 제곱이 가장 작은 패드(동률은 낮은 인덱스)";
            }
        }

        static int InputSeed(int group,int wave)
        {
            unchecked{return Seed^group*73856093^wave*19349663^unchecked((int)0x9e3779b9);}
        }

        // This method is the entire blind decision surface. In particular it never
        // reads Current.answer, Choices, LastCorrect, or AnswerPad().
        static int ChoosePad(Bot bot,HyeopgokRules rules,int group,int wave)
        {
            switch(bot){
                case Bot.Repeat:return 0;
                case Bot.Cycle:return wave&3;
                case Bot.Random:return new System.Random(InputSeed(group,wave)).Next(4);
                case Bot.Idle:return -1;
                default:
                    int nearest=0;float distance=float.MaxValue;
                    for(int i=0;i<4;i++){
                        Vector3 delta=HyeopgokRules.Pads[i]-rules.King;delta.y=0;
                        float candidate=delta.sqrMagnitude;
                        if(candidate<distance){distance=candidate;nearest=i;}
                    }
                    return nearest;
            }
        }

        // A submitted answer has one route in both production and this probe:
        // CommandChoicePad nominates the pad, and Tick performs arrival/commit.
        static int DriveFirstAttempt(HyeopgokRules rules,int pad,out bool commandAccepted)
        {
            commandAccepted=pad<0;
            if(pad>=0)commandAccepted=rules.CommandChoicePad(pad);
            int maxTicks=Mathf.CeilToInt(rules.TimeLimit/Dt)+160;
            for(int tick=0;tick<maxTicks;tick++){
                rules.Tick(Dt);
                if(rules.Pending||rules.Ended)return tick+1;
            }
            return maxTicks;
        }

        static PackItem CloneItem(PackItem source,int layer)
        {
            var item=new PackItem{
                id=source.id,prompt=source.prompt,answer=source.answer,format=source.format,explain=source.explain,unitConcept=source.unitConcept,
                answer_mode=source.answer_mode,accept=source.accept,num_label=source.num_label,den_label=source.den_label,answer_type=source.answer_type,
                answerNumeric=source.answerNumeric,difficulty=source.difficulty,max=source.max,answerValue=source.answerValue,coin_budget=source.coin_budget,
                has_coin_budget=source.has_coin_budget,answerParts=source.answerParts,distractor_tags=source.distractor_tags==null?null:(string[])source.distractor_tags.Clone()
            };
            if(source.choices!=null){
                item.choices=new string[source.choices.Length];
                if(source.choices.Length==4)for(int i=0;i<4;i++)item.choices[(i+layer)&3]=source.choices[i];
                else Array.Copy(source.choices,item.choices,source.choices.Length);
            }
            return item;
        }

        static QuestionPack RotatePack(QuestionPack source,int layer)
        {
            var items=new PackItem[source.items.Length];
            for(int i=0;i<items.Length;i++)items[i]=CloneItem(source.items[i],layer);
            return new QuestionPack{
                pack_id=source.pack_id,title=source.title,school=source.school,unit_id=source.unit_id,schema_version=source.schema_version,
                grade=source.grade,semester=source.semester,unit_order=source.unit_order,has_economy=source.has_economy,
                standards=source.standards,economy=source.economy,items=items
            };
        }

        static Result RunBot(QuestionPack source,Bot bot)
        {
            var result=new Result{id=Id(bot),pack=source.pack_id,bot=Label(bot),strategy=Strategy(bot)};
            var layers=new QuestionPack[Layers];
            for(int i=0;i<Layers;i++)layers[i]=RotatePack(source,i);
            var stratumCorrect=new int[(Games/Layers)*Waves];

            for(int game=0;game<Games;game++){
                int group=game/Layers,layer=game%Layers;
                var rules=new HyeopgokRules();rules.Start(layers[layer],Seed+group);
                for(int wave=0;wave<Waves;wave++){
                    if(rules.Ended||rules.Current==null){result.pathErrors++;Fail(source.pack_id+" "+Label(bot)+" ended before wave "+wave);break;}
                    if(rules.Current.Mode!="choice"||rules.PadCount!=4){result.pathErrors++;Fail(source.pack_id+" exposed non-choice item "+rules.Current.id);break;}
                    result.presented++;
                    int pad=ChoosePad(bot,rules,group,wave);
                    int attemptsBefore=rules.Attempts;
                    DriveFirstAttempt(rules,pad,out bool accepted);
                    if(pad>=0){result.commands++;if(!accepted)result.rejectedCommands++;}
                    if(!rules.Pending||rules.Attempts!=attemptsBefore+1){
                        result.pathErrors++;Fail(source.pack_id+" "+Label(bot)+" unresolved actual path at game "+game+", wave "+wave);break;
                    }
                    result.resolved++;
                    if(rules.LastPad>=0)result.submitted++;
                    if(rules.LastCorrect){result.correct++;stratumCorrect[group*Waves+wave]++;}
                    if(rules.LastPad<0)result.timeouts++;
                    if(pad>=0&&rules.LastPad!=pad){result.pathErrors++;Fail(source.pack_id+" "+Label(bot)+" committed a different pad");}
                    if(pad<0&&rules.LastPad!=-1){result.pathErrors++;Fail(source.pack_id+" idle committed a pad");}

                    // Blind runs would often die before wave ten, which censors later
                    // answer positions toward lucky survivors. Restore only HP after
                    // the first-attempt observation; order/place RNG remains untouched.
                    if(!rules.LastCorrect)rules.Hp=100;
                    rules.Next();
                }
            }

            int expected=bot==Bot.Idle?0:1;
            for(int i=0;i<stratumCorrect.Length;i++)if(stratumCorrect[i]!=expected){
                result.pathErrors++;Fail(source.pack_id+" "+Label(bot)+" stratum "+i+" had "+stratumCorrect[i]+" correct, expected "+expected);
            }
            result.firstAttemptObservedRate=result.presented==0?0:result.correct/(double)result.presented;
            result.firstAttemptRate=result.firstAttemptObservedRate;result.progress=result.correct>0;
            result.chanceGate=result.firstAttemptObservedRate<=ChanceLimit+1e-12;
            result.stratificationGate=result.pathErrors==0&&result.correct==expected*(Games/Layers)*Waves;
            result.pathGate=result.pathErrors==0&&result.rejectedCommands==0&&result.resolved==result.presented&&
                (bot==Bot.Idle?result.timeouts==result.presented:result.timeouts==0);
            if(!result.chanceGate)Fail(source.pack_id+" "+Label(bot)+" exceeded 25%: "+P(result.firstAttemptObservedRate));
            if(!result.stratificationGate)Fail(source.pack_id+" "+Label(bot)+" deterministic stratification failed");
            if(!result.pathGate)Fail(source.pack_id+" "+Label(bot)+" actual command/tick path failed");
            return result;
        }

        static bool SameChoices(HyeopgokRules a,HyeopgokRules b)
        {
            for(int i=0;i<4;i++)if(a.Choices[i]!=b.Choices[i])return false;
            return true;
        }

        static bool DriveCompared(HyeopgokRules a,HyeopgokRules b,int pad)
        {
            if(!a.CommandChoicePad(pad)||!b.CommandChoicePad(pad))return false;
            int guard=0;
            while((!a.Pending||!b.Pending)&&guard++<2400){if(!a.Pending)a.Tick(Dt);if(!b.Pending)b.Tick(Dt);}
            return a.Pending&&b.Pending&&a.LastPad==pad&&b.LastPad==pad;
        }

        static RewardChecks CheckRewards(QuestionPack pack)
        {
            var check=new RewardChecks();bool wrongZero=true,guaranteed=true;var outcomes=new HashSet<HyeopgokRewardKind>();
            const int orthogonalSeeds=20;
            for(int seedAt=0;seedAt<orthogonalSeeds;seedAt++){
                var quietRules=new HyeopgokRules();var noisyRules=new HyeopgokRules();int rulesSeed=Seed+seedAt*101;
                quietRules.Start(pack,rulesSeed);noisyRules.Start(pack,rulesSeed);
                var quietReward=new HyeopgokRewardRng(rulesSeed^0x24680);var noisyReward=new HyeopgokRewardRng(rulesSeed^0x64208);
                int quietStreak=0,noisyStreak=0;check.testedSeeds++;
                for(int wave=0;wave<Waves;wave++){
                    bool sameQuestion=quietRules.Current!=null&&noisyRules.Current!=null&&quietRules.Current.id==noisyRules.Current.id&&SameChoices(quietRules,noisyRules);
                    if(!sameQuestion){check.questionOrderMismatches++;break;}
                    int pad=(seedAt+wave)&3;
                    // Consume unrelated reward entropy on only one side before the
                    // identical physical input. These outcomes also prove that the
                    // exercised reward stream can produce both coin and tower loot.
                    for(int i=0;i<5;i++){
                        var extra=noisyReward.Roll(true,1,true,.5f);
                        if(extra.kind==HyeopgokRewardKind.Coin||extra.kind==HyeopgokRewardKind.Tower)outcomes.Add(extra.kind);
                    }
                    bool paths=DriveCompared(quietRules,noisyRules,pad);
                    if(!paths||quietRules.LastCorrect!=noisyRules.LastCorrect){check.answerMismatches++;break;}
                    quietStreak=quietRules.LastCorrect?quietStreak+1:0;noisyStreak=noisyRules.LastCorrect?noisyStreak+1:0;
                    var quietOutcome=quietReward.Roll(quietRules.LastCorrect,quietStreak,true,.5f);
                    var noisyOutcome=noisyReward.Roll(noisyRules.LastCorrect,noisyStreak,true,.5f);
                    if(quietOutcome.kind==HyeopgokRewardKind.Coin||quietOutcome.kind==HyeopgokRewardKind.Tower)outcomes.Add(quietOutcome.kind);
                    if(noisyOutcome.kind==HyeopgokRewardKind.Coin||noisyOutcome.kind==HyeopgokRewardKind.Tower)outcomes.Add(noisyOutcome.kind);
                    check.comparisons++;check.comparedWaves++;
                    if(!quietRules.LastCorrect){quietRules.Hp=100;noisyRules.Hp=100;}
                    quietRules.Next();noisyRules.Next();
                }
                check.quietRewardDraws+=quietReward.DrawCount;check.noisyRewardDraws+=noisyReward.DrawCount;
            }
            check.rewardOutcomeVariants=outcomes.Count;
            check.orthogonality=check.testedSeeds==orthogonalSeeds&&check.comparisons==orthogonalSeeds*Waves&&
                check.answerMismatches==0&&check.questionOrderMismatches==0&&check.rewardOutcomeVariants>=2;
            if(!check.orthogonality)Fail("Reward entropy changed question/adjudication or did not exercise both outcomes");

            var wrongReward=new HyeopgokRewardRng(Seed^0x55aa55aa);
            check.wrongDrawsBefore=wrongReward.DrawCount;
            for(int i=0;i<64;i++){
                var reward=wrongReward.Roll(false,i%5,i%2==0,i/63f,forceCoin:i%3==0,forceTower:i%7==0);
                check.wrongCases++;
                if(reward.kind!=HyeopgokRewardKind.None||wrongReward.DrawCount!=check.wrongDrawsBefore)wrongZero=false;
            }
            check.wrongDrawsAfter=wrongReward.DrawCount;check.wrongConsumesZeroDraws=wrongZero&&check.wrongDrawsAfter==check.wrongDrawsBefore;
            if(!check.wrongConsumesZeroDraws)Fail("Wrong reward consumed entropy or produced loot");

            for(int seed=0;seed<256;seed++){
                var rewardRng=new HyeopgokRewardRng(seed);
                var reward=rewardRng.Roll(true,3,true,(seed&15)/15f);
                check.guaranteeSeeds++;
                if(reward.kind==HyeopgokRewardKind.Tower)check.guaranteedTowers++;else guaranteed=false;
            }
            check.thirdCorrectGuaranteesTower=guaranteed&&check.guaranteedTowers==check.guaranteeSeeds;
            if(!check.thirdCorrectGuaranteesTower)Fail("Third consecutive correct answer did not guarantee a tower on an empty plot");
            check.pass=check.orthogonality&&check.wrongConsumesZeroDraws&&check.thirdCorrectGuaranteesTower;
            return check;
        }

        static Aggregate AggregateBot(List<Result> rows,Bot bot)
        {
            string label=Label(bot);var a=new Aggregate{bot=label,pathGate=true};
            foreach(var row in rows)if(row.bot==label){
                a.packs++;a.games+=row.games;a.presented+=row.presented;a.resolved+=row.resolved;a.commands+=row.commands;
                a.correct+=row.correct;a.timeouts+=row.timeouts;a.pathErrors+=row.pathErrors;a.pathGate&=row.pathGate&&row.stratificationGate;
            }
            a.firstAttemptObservedRate=a.presented==0?0:a.correct/(double)a.presented;
            a.chanceGate=a.firstAttemptObservedRate<=ChanceLimit+1e-12;
            return a;
        }

        static string P(double value)=>(value*100).ToString("0.000",CultureInfo.InvariantCulture)+"%";

        static void WriteReport(string output,Report report)
        {
            Directory.CreateDirectory(output);
            string json=JsonUtility.ToJson(report,true)+"\n";
            File.WriteAllText(Path.Combine(output,"bot-results.json"),json);
            File.WriteAllText(Path.Combine(output,"bots.json"),json);
            var md=new StringBuilder("# 협곡 사수 v3 — 4지선다 무뇌 봇\n\n");
            md.AppendLine("- 13팩(현재 index 기준) × 봇 5종 × 각 200게임 × 10문항을 `CommandChoicePad → Tick` 경로로 측정한다.");
            md.AppendLine("- 네 입력 봇은 동일 시드의 보기 배열 4회전 층화로 각 물리 패드의 정답 위치를 한 번씩 만나므로 정확히 25%, idle은 0%가 기대값이다.");
            md.AppendLine("- 보상 난수는 문항 순서/보기 배치/정오 난수와 분리하며, 오답은 보상 draw 0, 빈 부지의 3연속 정답은 새 타워를 보장해야 한다.\n");
            md.AppendLine("| 팩 | 봇 | 정답/첫 시도 | 관측률 | 시간초과 | 층화 | 실제 경로 | ≤25% |");
            md.AppendLine("|---|---|---:|---:|---:|---|---|---|");
            foreach(var row in report.results)md.AppendLine("| "+row.pack+" | "+row.bot+" | "+row.correct+"/"+row.presented+" | "+P(row.firstAttemptObservedRate)+" | "+row.timeouts+" | "+Pass(row.stratificationGate)+" | "+Pass(row.pathGate)+" | "+Pass(row.chanceGate)+" |");
            md.AppendLine("\n## 봇별 전체\n");
            md.AppendLine("| 봇 | 팩 | 정답/첫 시도 | 관측률 | ≤25% | 경로 |");
            md.AppendLine("|---|---:|---:|---:|---|---|");
            foreach(var row in report.botAggregates)md.AppendLine("| "+row.bot+" | "+row.packs+" | "+row.correct+"/"+row.presented+" | "+P(row.firstAttemptObservedRate)+" | "+Pass(row.chanceGate)+" | "+Pass(row.pathGate)+" |");
            md.AppendLine("\n## 보상 RNG\n");
            md.AppendLine("- 문항/보기/정오 직교성: **"+Pass(report.rewardOrthogonality.orthogonality)+"** ("+report.rewardOrthogonality.comparedWaves+"문항, quiet "+report.rewardOrthogonality.quietRewardDraws+" draws / noisy "+report.rewardOrthogonality.noisyRewardDraws+" draws)");
            md.AppendLine("- 오답 draw 0: **"+Pass(report.rewardOrthogonality.wrongConsumesZeroDraws)+"** ("+report.rewardOrthogonality.wrongCases+"건, "+report.rewardOrthogonality.wrongDrawsBefore+"→"+report.rewardOrthogonality.wrongDrawsAfter+")");
            md.AppendLine("- 3연속 정답 타워 보장: **"+Pass(report.rewardOrthogonality.thirdCorrectGuaranteesTower)+"** ("+report.rewardOrthogonality.guaranteedTowers+"/"+report.rewardOrthogonality.guaranteeSeeds+")");
            md.AppendLine("\n## 판정\n\n- 각 팩·봇 ≤25%: **"+Pass(report.eachBotAtOrBelowChance)+"**, 전체: **"+Pass(report.allBotsAtOrBelowChance)+"**, 최종: **"+Pass(report.pass)+"**.");
            foreach(string failure in report.failures)md.AppendLine("- 오류: "+failure);
            File.WriteAllText(Path.Combine(output,"report.md"),md.ToString());
        }

        static string Pass(bool value)=>value?"pass":"fail";

        public static void Run()
        {
            Failures.Clear();errorCount=0;
            string repo=Environment.GetEnvironmentVariable("MGF_HYEOPGOK_REPO");var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-hyeopgokRepo")repo=args[i+1];
            if(string.IsNullOrEmpty(repo))repo="/Users/sitpo/math-game-factory";
            string packDir=Path.Combine(repo,"public/g/hyeopgok-sasu/packs");
            var index=JsonUtility.FromJson<PackIndex>(File.ReadAllText(Path.Combine(packDir,"index.json")));
            if(index==null||index.packs==null||index.packs.Length==0)throw new Exception("No pack index entries");

            int skipped=0;var packs=new List<QuestionPack>();
            foreach(var entry in index.packs){
                string file=string.IsNullOrEmpty(entry.file)?entry.pack_id+".json":entry.file;
                var authored=HyeopgokPackJson.Parse(File.ReadAllText(Path.Combine(packDir,file)));
                var eligible=HyeopgokGame.FilterEligibleChoices(authored,out int rejected);skipped+=rejected;
                if(eligible==null||eligible.items==null||eligible.items.Length<10){Fail(entry.pack_id+" has fewer than ten eligible choice items");continue;}
                packs.Add(eligible);
            }

            var rows=new List<Result>();
            foreach(var pack in packs)foreach(Bot bot in Enum.GetValues(typeof(Bot)))rows.Add(RunBot(pack,bot));
            var aggregates=new List<Aggregate>();foreach(Bot bot in Enum.GetValues(typeof(Bot)))aggregates.Add(AggregateBot(rows,bot));
            var reward=packs.Count>0?CheckRewards(packs[0]):new RewardChecks();

            bool each=true,allPaths=true;long allPresented=0,allCorrect=0,activePresented=0,activeCorrect=0;
            foreach(var row in rows){each&=row.chanceGate&&row.stratificationGate;allPaths&=row.pathGate;allPresented+=row.presented;allCorrect+=row.correct;if(row.bot!=Label(Bot.Idle)){activePresented+=row.presented;activeCorrect+=row.correct;}}
            double allRate=allPresented==0?0:allCorrect/(double)allPresented,activeRate=activePresented==0?0:activeCorrect/(double)activePresented;
            bool allChance=allRate<=ChanceLimit+1e-12,activeChance=activeRate<=ChanceLimit+1e-12;
            bool rewardPass=reward.orthogonality&&reward.wrongConsumesZeroDraws&&reward.thirdCorrectGuaranteesTower;
            var report=new Report{
                packCount=packs.Count,skippedNonChoiceItems=skipped,eachBotAtOrBelowChance=each,allBotsAtOrBelowChance=allChance,
                activeBotsAtOrBelowChance=activeChance,allBotsFirstAttemptObservedRate=allRate,activeBotsFirstAttemptObservedRate=activeRate,
                rewardChecksPassed=rewardPass,modelErrors=errorCount,results=rows.ToArray(),botAggregates=aggregates.ToArray(),rewardOrthogonality=reward,
                failures=Failures.ToArray()
            };
            report.pass=report.packCount==index.packs.Length&&report.eachBotAtOrBelowChance&&report.allBotsAtOrBelowChance&&
                report.activeBotsAtOrBelowChance&&rewardPass&&allPaths&&report.modelErrors==0;

            string source=Path.Combine(repo,"factory/unity-src/hyeopgok-sasu");
            string output=Environment.GetEnvironmentVariable("MGF_HYEOPGOK_VALIDATION_OUT");
            if(string.IsNullOrEmpty(output))output=Path.Combine(source,"ArtSource/validation/v3");
            WriteReport(Path.GetFullPath(output),report);
            Debug.Log("HYEOPGOK_V3_BOTS "+JsonUtility.ToJson(report));
            EditorApplication.Exit(report.pass?0:1);
        }
    }
}
#endif
