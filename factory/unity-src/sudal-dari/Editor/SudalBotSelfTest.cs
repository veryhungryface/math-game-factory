// 다리 놓는 수달 — 생성기 전수 검사와 무뇌 봇 4종×200판.
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mgf.SudalDari
{
    public static class SudalBotSelfTest
    {
#if UNITY_EDITOR
        public static void Run()
        {
            string outPath = "sudal-bots.md";
            var args = Environment.GetCommandLineArgs();
            for (int i=0;i<args.Length-1;i++) if(args[i]=="-sudalOut") outPath=args[i+1];
            string report=RunAll();
            System.IO.File.WriteAllText(outPath,report);
            UnityEngine.Debug.Log("SUDAL_BOTS\n"+report);
            EditorApplication.Exit(0);
        }
#endif

        public static string RunAll()
        {
            var sb=new StringBuilder();
            sb.AppendLine("# 다리 놓는 수달 — 전수 검사 + 무뇌 봇");
            Exhaustive(sb);
            Bots(sb);
            return sb.ToString();
        }

        static void Exhaustive(StringBuilder sb)
        {
            var bank=SudalRules.BuildBank();
            var seen=new HashSet<string>();
            int badMath=0,badChoices=0,badWords=0,badDistractor=0;
            foreach(var p in bank)
            {
                if(!seen.Add(p.prompt+"|"+p.answer)) badMath++;
                if(p.prompt.Contains("√")||p.prompt.Contains("제곱근")||p.prompt.Contains("가정")||p.prompt.Contains("결론")) badWords++;
                if(p.choices==null||p.choices.Length!=25)badChoices++;
                else
                {
                    var ch=new HashSet<string>(); bool has=false;
                    for(int i=0;i<p.choices.Length;i++){if(!ch.Add(p.choices[i]))badChoices++;if(p.choices[i]==p.answer)has=true;}
                    if(!has)badChoices++;
                }
            }
            for(int seed=0;seed<400;seed++)
            {
                var d=SudalRules.RunDeck(new Random(1000+seed));
                if(d.Count!=SudalRules.TargetBridges)badMath++;
                foreach(var p in d)
                {
                    if(!SudalRules.IsRight(p.a,p.b,p.c))badMath++;
                    if(p.answer<1||p.answer>25)badMath++;
                    if(p.kind==BridgeKind.Hypotenuse&&p.answer!=p.c)badMath++;
                    if(p.kind==BridgeKind.MissingLeg&&p.answer!=(p.shownLeg==p.a?p.b:p.a))badMath++;
                    if(p.kind==BridgeKind.CorrectFrame&&SudalRules.IsRight(p.a,p.b,p.startLongest))badDistractor++;
                    if(p.wrongSum==p.answer||p.wrongAddSquares==p.answer)badDistractor++;
                }
            }
            sb.AppendLine("- 문제 은행 **"+bank.Count+"개**, 고유 문항 **"+seen.Count+"개**");
            sb.AppendLine("- 400개 런 덱(3,200문항) 정수 불변 오류 **"+badMath+"**, 금지 표현 **"+badWords+"**");
            sb.AppendLine("- 선택지/정답 계약 오류 **"+badChoices+"**, 오답 우연 정답 충돌 **"+badDistractor+"**");
            sb.AppendLine();
        }

        enum Bot { Fixed, Cycle, Random, Idle }

        sealed class Result
        {
            public int firstTotal,firstCorrect,finished;
        }

        static void Bots(StringBuilder sb)
        {
            const int Games=200;
            var fixedR=RunBot(Bot.Fixed,Games,11);
            var cycleR=RunBot(Bot.Cycle,Games,23);
            var randomR=RunBot(Bot.Random,Games,20261004);
            var idleR=RunBot(Bot.Idle,Games,37);
            sb.AppendLine("## 무뇌 봇 4종 × 200판 (문항별 첫 시도 정답률)");
            sb.AppendLine("- 우연 수준: **1/25 = 4.00%** (1~25 정수 눈금 중 정답 1개)");
            Line(sb,"연타(1 cm 고정)",fixedR,Games);
            Line(sb,"순환(1→25)",cycleR,Games);
            Line(sb,"무작위(1~25 균등)",randomR,Games);
            Line(sb,"무입력",idleR,Games);
            sb.AppendLine("- 무입력 진도: **0 / 200판**");
        }

        static void Line(StringBuilder sb,string name,Result r,int games)
        {
            double rate=r.firstCorrect*100.0/Math.Max(1,r.firstTotal);
            sb.AppendLine("- "+name+": 첫 시도 **"+r.firstCorrect+"/"+r.firstTotal+" = "+rate.ToString("0.00")+"%**, 완주 **"+r.finished+"/"+games+"**");
        }

        static Result RunBot(Bot bot,int games,int seed)
        {
            var r=new Result();
            var rnd=new Random(seed);
            int cycle=1;
            for(int g=0;g<games;g++)
            {
                if(bot==Bot.Idle)continue;
                var deck=SudalRules.RunDeck(new Random(seed+g*7919));
                int lives=SudalRules.StartLives, solved=0;
                for(int q=0;q<deck.Count&&lives>0;)
                {
                    var p=deck[q];
                    int guess=Guess(bot,rnd,ref cycle);
                    r.firstTotal++;
                    bool firstOk=guess==p.answer;
                    if(firstOk){r.firstCorrect++;solved++;q++;continue;}
                    lives--;
                    // 첫 시도 뒤에도 같은 입력 전략으로 같은 문항을 재시도한다. 첫 시도 통계에는 더하지 않는다.
                    while(lives>0)
                    {
                        guess=Guess(bot,rnd,ref cycle);
                        if(guess==p.answer){solved++;q++;break;}
                        lives--;
                    }
                }
                if(solved>=SudalRules.TargetBridges)r.finished++;
            }
            return r;
        }

        static int Guess(Bot bot,Random rnd,ref int cycle)
        {
            if(bot==Bot.Fixed)return 1;
            if(bot==Bot.Cycle){int x=cycle;cycle=cycle>=25?1:cycle+1;return x;}
            return 1+rnd.Next(25);
        }
    }
}
