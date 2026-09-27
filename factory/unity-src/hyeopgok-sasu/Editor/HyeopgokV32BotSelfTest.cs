// V3.2 endless-run survival probe. Every submitted answer travels through the
// production CommandChoicePad -> Tick path; no bot receives HP restoration.
//
// Unity -batchmode -nographics -projectPath <workspace>
//   -executeMethod Mgf.HyeopgokSasu.HyeopgokV32BotSelfTest.Run
//   -hyeopgokRepo /Users/sitpo/math-game-factory -quit
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    public static class HyeopgokV32BotSelfTest
    {
        const int SeedBase=20260928,SeedsPerPack=12;
        const float Dt=.05f;

        enum Bot { Idle, Random, Fixed, Nearest, Perfect }

        [Serializable]
        public sealed class RunResult
        {
            public string pack,bot,endReason;
            public int seed,questions,correct,commands,rejectedCommands,deathStage,reachedStage,maxStage;
            public int stage1Low,stage1Questions,stage3Advanced,stage3Questions;
            public bool blind,ended,reachedStage3,reachedStage4,actualPadPath,noRepeat,difficultyCurve,pass;
        }

        [Serializable]
        public sealed class BotResult
        {
            public string bot;
            public bool blind,pass;
            public int runs,deaths,deathsBeforeStage3,reachedStage3,reachedStage4,maxStage,totalQuestions,totalCorrect,pathFailures;
        }

        [Serializable]
        public sealed class RuleChecks
        {
            public bool tenQuestionsContinue=true,twentyQuestionsContinue=true,thirtyQuestionsContinue=true;
            public bool gateHpZeroEnds=true,mapCycle=true,perkDamage=true,perkMagnet=true,perkHealth=true,pass;
            public int tenChecks,twentyChecks,thirtyChecks,gateChecks;
        }

        [Serializable]
        public sealed class Report
        {
            public int schemaVersion=4,schema_version=4,seedBase=SeedBase,seedsPerPack=SeedsPerPack,packCount;
            public string inputContract="idle/random/fixed/nearest/perfect all resolve through production CommandChoicePad and Tick; no run restores or fabricates HP";
            public bool blindBotsDieBeforeStage3,perfectReachesStage3,actualPadPath,difficultyCurve,gateHpOnly,pass;
            public BotResult[] bots;
            public RunResult[] runs;
            public RuleChecks rules;
            public string[] failures;
        }

        static readonly List<string> Failures=new List<string>();

        static string BotId(Bot bot)
        {
            switch(bot){case Bot.Idle:return "idle";case Bot.Random:return "random";case Bot.Fixed:return "fixed";case Bot.Nearest:return "nearest";default:return "perfect";}
        }

        static bool IsBlind(Bot bot)=>bot!=Bot.Perfect;

        static int ChoosePad(Bot bot,HyeopgokRules rules,System.Random inputRng)
        {
            switch(bot){
                case Bot.Idle:return -1;
                case Bot.Random:return inputRng.Next(4);
                case Bot.Fixed:return 0;
                case Bot.Perfect:return rules.AnswerPad();
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

        static bool Drive(HyeopgokRules rules,int pad,out bool accepted)
        {
            accepted=pad<0;
            if(pad>=0)accepted=rules.CommandChoicePad(pad);
            int guard=Mathf.CeilToInt(rules.TimeLimit/Dt)+200;
            for(int i=0;i<guard&&!rules.Pending&&!rules.Ended;i++)rules.Tick(Dt);
            return rules.Pending&&rules.Attempts>0;
        }

        static RunResult RunOne(QuestionPack pack,Bot bot,int seed,RuleChecks checks)
        {
            var result=new RunResult{pack=pack.pack_id,bot=BotId(bot),seed=seed,blind=IsBlind(bot),actualPadPath=true,noRepeat=true,difficultyCurve=true};
            var rules=new HyeopgokRules();rules.Start(pack,seed);
            var inputRng=new System.Random(unchecked(seed*486187739+(int)bot*16777619));
            var seen=new HashSet<string>();
            int targetStage=bot==Bot.Perfect?4:3;

            while(!rules.Ended&&rules.Stage<targetStage&&result.questions<40){
                if(!rules.Active||rules.Current==null||rules.Current.Mode!="choice"){
                    result.actualPadPath=false;Failures.Add(result.pack+" "+result.bot+" missing active choice at stage "+rules.Stage);break;
                }
                string questionId=rules.Current.id??"";
                if(!seen.Add(questionId)&&seen.Count<pack.items.Length)result.noRepeat=false;

                int stage=rules.Stage,difficulty=rules.QuestionDifficulty;
                if(stage==1){result.stage1Questions++;if(difficulty<=2)result.stage1Low++;}
                if(stage==3){result.stage3Questions++;if(difficulty>=3)result.stage3Advanced++;}

                int pad=ChoosePad(bot,rules,inputRng),before=rules.Attempts;
                bool resolved=Drive(rules,pad,out bool accepted);
                if(pad>=0){result.commands++;if(!accepted)result.rejectedCommands++;}
                if(!resolved||rules.Attempts!=before+1||(pad>=0&&rules.LastPad!=pad)||(pad<0&&rules.LastPad!=-1))result.actualPadPath=false;
                result.questions++;if(rules.LastCorrect)result.correct++;

                // Reaching a question-count boundary must never end a living run.
                // A blind bot may legitimately lose its final HP on that exact
                // answer, which is the one allowed terminal condition.
                if(rules.Attempts==10){checks.tenChecks++;if(rules.Ended&&rules.Hp>0)checks.tenQuestionsContinue=false;}
                if(rules.Attempts==20){checks.twentyChecks++;if(rules.Ended&&rules.Hp>0)checks.twentyQuestionsContinue=false;}
                if(rules.Attempts==30){checks.thirtyChecks++;if(rules.Ended&&rules.Hp>0)checks.thirtyQuestionsContinue=false;}
                if(rules.Ended)break;

                if(rules.AwaitingBoss){
                    int clearedStage=rules.Stage;
                    if(!rules.BeginBoss()||!rules.AdvanceStage((clearedStage-1)%3)){
                        result.actualPadPath=false;Failures.Add(result.pack+" "+result.bot+" could not advance cleared stage "+clearedStage);break;
                    }
                    int expectedStage=clearedStage+1;
                    if(rules.Stage!=expectedStage||rules.Map!=(expectedStage-1)%3+1||rules.Loop!=(expectedStage-1)/3+1)checks.mapCycle=false;
                    if(clearedStage==1&&Mathf.Abs(rules.TowerDamageMultiplier-1.15f)>.0001f)checks.perkDamage=false;
                    if(clearedStage==2&&Mathf.Abs(rules.CoinRadiusMultiplier-1.30f)>.0001f)checks.perkMagnet=false;
                    if(clearedStage==3&&rules.MaxHp!=120)checks.perkHealth=false;
                }else rules.Next();
            }

            result.ended=rules.Ended;result.endReason=rules.EndReason;result.deathStage=rules.Ended?rules.Stage:0;
            result.reachedStage=rules.Stage;result.maxStage=rules.Stage;result.reachedStage3=rules.Stage>=3;result.reachedStage4=rules.Stage>=4;
            bool supportsLow=CountDifficulty(pack,false)>=8,supportsAdvanced=CountDifficulty(pack,true)>=4;
            bool stage1Pass=!supportsLow||result.stage1Questions<10||result.stage1Low>=8;
            bool stage3Pass=bot!=Bot.Perfect||!supportsAdvanced||result.stage3Questions<10||result.stage3Advanced>=4;
            result.difficultyCurve=stage1Pass&&stage3Pass;
            result.pass=result.actualPadPath&&result.noRepeat&&result.difficultyCurve&&
                (result.blind?result.ended&&result.deathStage<3&&result.endReason=="gate_hp_zero":result.reachedStage3&&!result.ended);
            if(!result.pass)Failures.Add(result.pack+" "+result.bot+" seed "+seed+" failed: ended="+result.ended+", stage="+rules.Stage+", hp="+rules.Hp);
            return result;
        }

        static int CountDifficulty(QuestionPack pack,bool advanced)
        {
            int count=0;
            foreach(var item in pack.items){int d=Math.Max(1,item.difficulty);if(advanced?d>=3:d<=2)count++;}
            return count;
        }

        static BotResult Aggregate(Bot bot,List<RunResult> runs)
        {
            var aggregate=new BotResult{bot=BotId(bot),blind=IsBlind(bot),pass=true};
            foreach(var row in runs){
                if(row.bot!=aggregate.bot)continue;
                aggregate.runs++;aggregate.totalQuestions+=row.questions;aggregate.totalCorrect+=row.correct;aggregate.maxStage=Math.Max(aggregate.maxStage,row.maxStage);
                if(row.ended)aggregate.deaths++;if(row.ended&&row.deathStage<3)aggregate.deathsBeforeStage3++;
                if(row.reachedStage3)aggregate.reachedStage3++;if(row.reachedStage4)aggregate.reachedStage4++;
                if(!row.actualPadPath)aggregate.pathFailures++;aggregate.pass&=row.pass;
            }
            return aggregate;
        }

        static void CheckGateDeath(QuestionPack pack,RuleChecks checks)
        {
            var rules=new HyeopgokRules();rules.Start(pack,SeedBase^0x51f15e);
            rules.Damage(rules.MaxHp-1);
            if(rules.Ended)checks.gateHpZeroEnds=false;
            rules.Damage(1);checks.gateChecks++;
            if(!rules.Ended||rules.Hp!=0||rules.EndReason!="gate_hp_zero"||rules.Won)checks.gateHpZeroEnds=false;
        }

        static QuestionPack FilterEligibleChoices(QuestionPack source,out int skipped)
        {
            skipped=0;if(source==null||source.items==null)return null;
            var ids=new HashSet<string>();var items=new List<PackItem>(source.items.Length);
            foreach(var item in source.items){
                bool valid=item!=null&&item.Mode=="choice"&&!string.IsNullOrEmpty(item.id)&&ids.Add(item.id)&&
                    !string.IsNullOrEmpty(item.prompt)&&!string.IsNullOrEmpty(item.explain)&&item.answer_type=="choice"&&
                    !string.IsNullOrEmpty(item.answer)&&item.choices!=null&&item.choices.Length==4;
                int exact=0,equivalent=0;
                if(valid)for(int i=0;i<4&&valid;i++){
                    valid=!string.IsNullOrEmpty(item.choices[i]);
                    if(item.choices[i]==item.answer)exact++;
                    if(PackItem.EquivalentChoice(item.choices[i],item.answer))equivalent++;
                    for(int j=0;j<i;j++)if(PackItem.EquivalentChoice(item.choices[i],item.choices[j]))valid=false;
                }
                valid&=exact==1&&equivalent==1&&(!(source.schema_version>=3)||item.distractor_tags!=null&&item.distractor_tags.Length==3);
                if(valid)items.Add(item);else skipped++;
            }
            return new QuestionPack{pack_id=source.pack_id,title=source.title,school=source.school,unit_id=source.unit_id,schema_version=source.schema_version,
                grade=source.grade,semester=source.semester,unit_order=source.unit_order,standards=source.standards,economy=source.economy,has_economy=source.has_economy,items=items.ToArray()};
        }

        public static void Run()
        {
            Failures.Clear();
            string repo=Environment.GetEnvironmentVariable("MGF_HYEOPGOK_REPO");var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-hyeopgokRepo")repo=args[i+1];
            if(string.IsNullOrEmpty(repo))repo="/Users/sitpo/math-game-factory";
            string packDir=Path.Combine(repo,"public/g/hyeopgok-sasu/packs");
            var index=JsonUtility.FromJson<PackIndex>(File.ReadAllText(Path.Combine(packDir,"index.json")));
            if(index==null||index.packs==null||index.packs.Length==0)throw new Exception("No pack index entries");

            var packs=new List<QuestionPack>();
            foreach(var entry in index.packs){
                string file=string.IsNullOrEmpty(entry.file)?entry.pack_id+".json":entry.file;
                QuestionPack authored=HyeopgokPackJson.Parse(File.ReadAllText(Path.Combine(packDir,file)));
                QuestionPack eligible=FilterEligibleChoices(authored,out int skipped);
                if(eligible==null||eligible.items==null||eligible.items.Length<30){Failures.Add(entry.pack_id+" has fewer than 30 eligible endless questions");continue;}
                packs.Add(eligible);
            }

            var checks=new RuleChecks();var runs=new List<RunResult>();
            foreach(var pack in packs){
                CheckGateDeath(pack,checks);
                foreach(Bot bot in Enum.GetValues(typeof(Bot)))for(int i=0;i<SeedsPerPack;i++)runs.Add(RunOne(pack,bot,SeedBase+i*7919,checks));
            }

            var bots=new List<BotResult>();
            foreach(Bot bot in Enum.GetValues(typeof(Bot)))bots.Add(Aggregate(bot,runs));
            bool blindPass=true,perfectPass=true,pathPass=true,curvePass=true;
            foreach(var bot in bots){if(bot.blind)blindPass&=bot.pass&&bot.deathsBeforeStage3==bot.runs;else perfectPass&=bot.pass&&bot.reachedStage3==bot.runs;pathPass&=bot.pathFailures==0;}
            foreach(var row in runs)curvePass&=row.difficultyCurve;
            checks.pass=checks.tenQuestionsContinue&&checks.twentyQuestionsContinue&&checks.thirtyQuestionsContinue&&checks.gateHpZeroEnds&&
                checks.mapCycle&&checks.perkDamage&&checks.perkMagnet&&checks.perkHealth&&checks.tenChecks>0&&checks.twentyChecks>0&&checks.thirtyChecks>0&&checks.gateChecks==packs.Count;

            var report=new Report{
                packCount=packs.Count,blindBotsDieBeforeStage3=blindPass,perfectReachesStage3=perfectPass,actualPadPath=pathPass,
                difficultyCurve=curvePass,gateHpOnly=checks.pass,bots=bots.ToArray(),runs=runs.ToArray(),rules=checks,failures=Failures.ToArray()
            };
            report.pass=report.packCount==index.packs.Length&&blindPass&&perfectPass&&pathPass&&curvePass&&checks.pass&&Failures.Count==0;

            string source=Path.Combine(repo,"factory/unity-src/hyeopgok-sasu");
            string output=Environment.GetEnvironmentVariable("MGF_HYEOPGOK_VALIDATION_OUT");
            if(string.IsNullOrEmpty(output))output=Path.Combine(source,"ArtSource/validation/v32");
            Directory.CreateDirectory(output);
            string json=JsonUtility.ToJson(report,true)+"\n";
            File.WriteAllText(Path.Combine(output,"bots.json"),json);
            File.WriteAllText(Path.Combine(output,"bot-results.json"),json);
            Debug.Log("HYEOPGOK_V32_BOTS "+JsonUtility.ToJson(report));
            EditorApplication.Exit(report.pass?0:1);
        }
    }
}
#endif
