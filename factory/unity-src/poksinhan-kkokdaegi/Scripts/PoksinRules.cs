// 폭신한 꼭대기 — 정수 편차 제약 풀과 정수 판정.
// 표현 함정: 편차는 x−평균, 분산은 편차 제곱의 평균, 표준편차는 음이 아닌 √분산이다.
// 분산 단위는 칸², 편차·표준편차 단위는 칸이며 표준편차에 ±를 붙이지 않는다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.PoksinhanKkokdaegi
{
    [Serializable]
    public sealed class CushionProblem
    {
        public string id;
        public int band;
        public int side;
        public int mean;
        public int[] raw;
        public int[] deviations;
        public int sumSquares;
        public int variance;
        public string prompt;
        public string concept;
        // 마지막 두 층은 같은 평균·최솟값·최댓값을 공유하는 비교 짝이다.
        public string pairId="";
        public string pairRole="";
        public int[] pairedRaw;
        public int pairedSide;
        public int pairedVariance;
    }

    public static class PoksinRules
    {
        public const int TargetLayers = 6;
        public const int MasteryNeeded = 5;
        public const int StartThreads = 3;
        public const float SessionSeconds = 120f;

        // 각 행은 합 0인 정수 편차 벡터다. 행의 제곱합은 정확히 6*s*s.
        // 먼저 s를 고른 뒤 이 제약 만족 풀에서만 뽑으므로 무작위 생성 후 필터링하지 않는다.
        static readonly int[][][] Patterns =
        {
            null,
            new[] {
                new[]{-2,0,0,0,1,1}, new[]{-1,-1,0,0,0,2}
            },
            new[] {
                new[]{-4,-1,1,1,1,2}, new[]{-4,0,0,0,2,2}, new[]{-3,-2,0,1,1,3},
                new[]{-3,-1,-1,0,2,3}, new[]{-2,-2,0,0,0,4}, new[]{-2,-1,-1,-1,1,4}
            },
            new[] {
                new[]{-6,-1,0,2,2,3}, new[]{-6,0,0,0,3,3}, new[]{-6,0,0,1,1,4},
                new[]{-5,-3,1,1,3,3}, new[]{-5,-2,-1,2,2,4}, new[]{-5,-2,0,0,3,4},
                new[]{-5,-1,-1,1,1,5}, new[]{-4,-4,0,2,3,3}, new[]{-4,-4,1,1,2,4},
                new[]{-4,-3,0,0,2,5}, new[]{-4,-2,-2,1,2,5}, new[]{-4,-2,-1,-1,4,4}
            },
            new[] {
                new[]{-8,-2,1,3,3,3}, new[]{-8,-2,2,2,2,4}, new[]{-8,-1,1,1,2,5},
                new[]{-8,0,0,0,4,4}, new[]{-7,-4,2,3,3,3}, new[]{-7,-3,0,2,3,5},
                new[]{-7,-2,-1,1,4,5}, new[]{-7,-1,-1,0,3,6}, new[]{-6,-5,1,3,3,4},
                new[]{-6,-4,-1,3,3,5}, new[]{-6,-4,0,2,2,6}, new[]{-6,-3,-1,0,5,5}
            },
            new[] {
                new[]{-11,1,1,3,3,3}, new[]{-11,1,2,2,2,4}, new[]{-10,-2,1,2,4,5},
                new[]{-10,-1,-1,4,4,4}, new[]{-10,-1,0,2,3,6}, new[]{-10,0,0,0,5,5},
                new[]{-9,-4,2,2,3,6}, new[]{-9,-3,-1,3,5,5}, new[]{-9,-3,1,1,3,7},
                new[]{-9,-2,-2,3,4,6}, new[]{-9,-2,0,0,4,7}, new[]{-9,-1,0,0,2,8}
            },
            new[] {
                new[]{-12,-3,2,3,5,5}, new[]{-12,-3,3,3,3,6}, new[]{-12,-2,0,4,4,6},
                new[]{-12,-2,1,3,3,7}, new[]{-12,-1,-1,3,5,6}, new[]{-12,0,0,0,6,6},
                new[]{-12,0,0,2,2,8}, new[]{-11,-5,2,4,5,5}, new[]{-11,-5,3,3,4,6},
                new[]{-11,-4,1,2,5,7}, new[]{-11,-3,0,1,6,7}, new[]{-11,-2,-1,1,5,8}
            },
            new[] {
                new[]{-12,-7,2,5,6,6}, new[]{-12,-6,0,4,7,7}, new[]{-12,-6,0,5,5,8},
                new[]{-12,-6,1,4,4,9}, new[]{-12,-6,2,2,5,9}, new[]{-12,-5,-2,6,6,7},
                new[]{-12,-5,0,3,4,10}, new[]{-12,-4,-3,5,6,8}, new[]{-12,-4,0,2,3,11},
                new[]{-12,-3,-3,2,8,8}, new[]{-12,-3,-3,4,4,10}, new[]{-12,-3,-2,1,6,10}
            },
            new[] {
                new[]{-12,-10,2,6,6,8}, new[]{-12,-10,3,5,5,9}, new[]{-12,-9,1,3,7,10},
                new[]{-12,-9,2,3,5,11}, new[]{-12,-8,-2,6,6,10}, new[]{-12,-8,0,4,4,12},
                new[]{-12,-7,-3,5,6,11}, new[]{-12,-6,-5,7,7,9}, new[]{-12,-6,-2,0,10,10},
                new[]{-12,-6,-1,-1,9,11}, new[]{-12,-5,-5,3,9,10}, new[]{-12,-4,-4,0,8,12}
            }
        };

        // 자료 4개 편차. Σd²=4s² 이므로 나누기를 빠뜨린 √Σd²=2s 가 눈금 안의 정수가 되어 그 오개념을 실제로 진단할 수 있다.
        static readonly int[][][] FourPatterns =
        {
            null,
            new[]{new[]{-1,-1,1,1}},
            new[]{new[]{-2,-2,2,2}},
            new[]{new[]{-5,1,1,3},new[]{-3,-3,3,3},new[]{-3,-1,-1,5}},
            new[]{new[]{-4,-4,4,4}}
        };

        // 같은 최솟값·최댓값(평균을 더하면 같은 평균)인데 표준편차가 다른 비교 짝. 정답 칸이 매 판 달라진다.
        static readonly int[][][] Pairs =
        {
            new[]{new[]{-3,-2,0,1,1,3},new[]{-3,-3,-3,3,3,3}},
            new[]{new[]{-6,0,0,1,1,4},new[]{-6,-5,1,3,3,4}},
            new[]{new[]{-4,-1,-1,0,0,6},new[]{-4,-3,-3,-1,5,6}},
            new[]{new[]{-8,-2,2,2,2,4},new[]{-8,-6,3,3,4,4}},
            new[]{new[]{-7,-1,-1,0,3,6},new[]{-7,-6,0,2,5,6}},
            new[]{new[]{-6,-4,0,2,2,6},new[]{-6,-6,-6,6,6,6}},
            new[]{new[]{-10,-1,0,2,3,6},new[]{-10,-6,2,2,6,6}},
            new[]{new[]{-9,-3,1,1,3,7},new[]{-9,-6,0,1,7,7}},
            new[]{new[]{-8,-5,0,3,4,6},new[]{-8,-8,0,4,6,6}},
            new[]{new[]{-7,-5,-1,1,5,7},new[]{-7,-7,-7,7,7,7}},
            new[]{new[]{-12,0,0,2,2,8},new[]{-12,-6,0,5,5,8}},
            new[]{new[]{-11,-2,-1,1,5,8},new[]{-11,-8,2,4,5,8}},
            new[]{new[]{-8,-8,2,2,4,8},new[]{-8,-8,-8,8,8,8}},
            new[]{new[]{-12,-6,0,5,5,8},new[]{-12,-10,2,6,6,8}},
            new[]{new[]{-12,-4,0,2,3,11},new[]{-12,-9,2,3,5,11}}
        };

        static int SideOf(int[] d)
        {
            int sum=0,ss=0;for(int i=0;i<d.Length;i++){sum+=d[i];ss+=d[i]*d[i];}
            if(sum!=0||ss%d.Length!=0)throw new Exception("편차 풀 불변식 위반");
            int v=ss/d.Length;for(int t=1;t<=8;t++)if(t*t==v)return t;
            throw new Exception("표준편차 눈금 불변식 위반");
        }

        static CushionProblem Make(ref int serial,int band,int mean,int[] d,string concept)
        {
            var raw=new int[d.Length];int ss=0;
            for(int k=0;k<d.Length;k++){raw[k]=mean+d[k];ss+=d[k]*d[k];if(raw[k]<0||raw[k]>40)return null;}
            int side=SideOf(d);
            return new CushionProblem{id="pk"+(++serial),band=band,side=side,mean=mean,raw=raw,deviations=(int[])d.Clone(),sumSquares=ss,variance=side*side,concept=concept};
        }

        public static CushionProblem Practice()
        {
            return new CushionProblem {
                id="practice", band=0, side=3, mean=6,
                raw=new[]{3,3,9,9}, deviations=new[]{-3,-3,3,3}, sumSquares=36, variance=9,
                prompt="분산이 9인 기록의 표준편차와 같게 한 변의 길이를 정하시오.", concept="분산과 표준편차의 관계"
            };
        }

        public static List<CushionProblem> BuildModels()
        {
            var result = new List<CushionProblem>(900);
            int serial = 0;
            for (int band = 1; band <= 3; band++)
            for (int side = 1; side <= 8; side++)
            {
                var pool = Patterns[side];
                for (int pi = 0; pi < pool.Length; pi++)
                for (int mean = 12; mean <= 28; mean++)
                {
                    var d = pool[pi];
                    bool fits = true;
                    var raw = new int[d.Length];
                    for (int k = 0; k < d.Length; k++) { raw[k] = mean + d[k]; if (raw[k] < 0 || raw[k] > 40) fits = false; }
                    if (!fits) continue;
                    // 같은 다중집합을 순서만 바꾼 값을 다양성으로 세지 않는다. mean/pattern/band가 실제 고유 조건이다.
                    int ss = 0, sum = 0;
                    for (int k = 0; k < d.Length; k++) { sum += d[k]; ss += d[k] * d[k]; }
                    if (sum != 0 || ss != d.Length * side * side) throw new Exception("편차 풀 불변식 위반");
                    var p = new CushionProblem {
                        id = "pk" + (++serial), band=band, side=side, mean=mean, raw=raw,
                        deviations=(int[])d.Clone(), sumSquares=ss, variance=side*side,
                        concept = band==1 ? "분산과 표준편차" : band==2 ? "편차 제곱의 평균" : "원자료의 산포도"
                    };
                    p.prompt = MakePrompt(p);
                    result.Add(p);
                }
            }
            // 나누기 누락을 진단하는 자료 4개 문항(편차의 제곱을 주는 2밴드).
            for(int side=1;side<=4;side++)
            for(int pi=0;pi<FourPatterns[side].Length;pi++)
            for(int mean=12;mean<=28;mean++)
            {
                var p=Make(ref serial,2,mean,FourPatterns[side][pi],"편차 제곱의 평균");
                if(p==null)continue;p.prompt=MakePrompt(p);result.Add(p);
            }
            // 핵심 학습 사건: 같은 평균·최솟값·최댓값인데 산포도만 다른 두 자료. 어느 쪽이 먼저 나오는지도 섞는다.
            for(int row=0;row<Pairs.Length;row++)
            for(int mean=12;mean<=24;mean+=2)
            for(int order=0;order<2;order++)
            {
                var a=Make(ref serial,3,mean,Pairs[row][order],"같은 대푯값의 산포도 비교");
                var b=Make(ref serial,3,mean,Pairs[row][1-order],"같은 대푯값의 산포도 비교");
                if(a==null||b==null||a.side==b.side)throw new Exception("짝 자료 불변식 위반");
                string pairId="pair-"+row+"-"+mean+"-"+order;
                a.pairId=b.pairId=pairId;a.pairRole="A";b.pairRole="B";
                a.pairedRaw=b.raw;a.pairedSide=b.side;a.pairedVariance=b.variance;
                b.pairedRaw=a.raw;b.pairedSide=a.side;b.pairedVariance=a.variance;
                a.prompt=MakePrompt(a);b.prompt=MakePrompt(b);result.Add(a);result.Add(b);
            }
            if (result.Count < 300) throw new Exception("문제 은행 300개 미만");
            return result;
        }

        static string MakePrompt(CushionProblem p)
        {
            string raw = "[" + ArrayText(p.raw) + "]";
            string label = RecordLabel(p);
            if (p.band == 1) return label + " " + raw + "의 평균은 " + p.mean + ", 분산은 " + p.variance + "이다. 한 변의 길이를 표준편차와 같게 정하시오.";
            if (p.band == 2)
            {
                var sq = new int[p.deviations.Length];
                for (int i=0;i<sq.Length;i++) sq[i]=p.deviations[i]*p.deviations[i];
                return label + " " + raw + "의 편차의 제곱은 [" + ArrayText(sq) + "]이다. 한 변의 길이를 표준편차와 같게 정하시오.";
            }
            if(!string.IsNullOrEmpty(p.pairId))
                return "비교 짝 "+p.pairRole+" "+raw+"의 평균은 "+p.mean+", 최솟값은 "+p.raw[0]+", 최댓값은 "+p.raw[p.raw.Length-1]+"이다. 한 변의 길이를 표준편차와 같게 정하시오.";
            return "평균이 " + p.mean + "인 " + label + " " + raw + "의 표준편차와 같게 한 변의 길이를 정하시오.";
        }

        static string RecordLabel(CushionProblem p)
        {
            // 숫자가 지워진 QA 핵심 문장에서도 실제 원단 기록끼리 구별되는 짧은 표제다.
            string[] color={"진홍","석류","산호","자주","장미","벽돌","버건디","연지","동백","루비","다홍","앵두","포도","노을","홍차","복분자","라즈베리"};
            string[] stitch={"격자 누비","사선 누비","물결 누비","십자 누비","세로 누비","마름모 누비","고리 누비","점선 누비","겹솔기","지그재그 솔기","테두리 솔기","단추 누비"};
            int h=17;
            for(int i=0;i<p.deviations.Length;i++)h=unchecked(h*31+p.deviations[i]*7+i);
            return color[Math.Abs(p.mean-12)%color.Length]+" "+stitch[Math.Abs(h)%stitch.Length]+" 기록";
        }

        public static bool IsCorrect(CushionProblem p, int selectedSide)
        {
            // sqrt 근삿값 비교 금지: n*t² == Σd²만 사용한다.
            return selectedSide >= 0 && p.deviations.Length * selectedSide * selectedSide == p.sumSquares;
        }

        public static string MisconceptionId(CushionProblem p, int t)
        {
            if (t == 0) return "mean_of_deviations";
            bool varianceSide = p.variance <= 8 && t == p.variance && t != p.side;
            bool noDivision = t * t == p.sumSquares && t != p.side;
            // 분산을 눈앞에 준 1밴드에서는 분산 복사가, 제곱만 준 2·3밴드에서는 나누기 누락이 더 그럴듯한 원인이다.
            if (p.band == 1 && varianceSide) return "variance_as_side";
            if (noDivision) return "no_division";
            if (varianceSide) return "variance_as_side";
            int min=p.raw[0],max=p.raw[0],abs=0;
            for(int i=0;i<p.raw.Length;i++){min=Math.Min(min,p.raw[i]);max=Math.Max(max,p.raw[i]);abs+=Math.Abs(p.deviations[i]);}
            int range=max-min;
            if(range%2==0){int half=range/2;if(half>=1&&half<=8&&t==half&&t!=p.side)return "half_range_as_side";}
            if(abs%p.raw.Length==0){int mad=abs/p.raw.Length;if(mad>=1&&mad<=8&&t==mad&&t!=p.side)return "mean_absolute_deviation";}
            return t*t > p.variance ? "area_too_large" : "area_too_small";
        }

        public static List<CushionProblem> BuildDeck(List<CushionProblem> models, int seed)
        {
            // 각 층마다 답 s를 먼저 독립 균등 추출한 뒤 해당 밴드·s의 제약 풀에서 샘플링한다.
            var rng = new System.Random(seed);
            var deck = new List<CushionProblem>(6);
            for (int round=0; round<4; round++)
            {
                int band = round/2 + 1;
                int side = rng.Next(1,9);
                // 2밴드에서 자료 4개 문항이 있는 답이면 1/3 확률로 그쪽에서 뽑는다(답 분포는 그대로 균등).
                int wantLength = band==2 && side<=4 && rng.Next(3)==0 ? 4 : 6;
                int count = 0;
                for (int i=0;i<models.Count;i++) if(Eligible(models[i],band,side,wantLength)) count++;
                int pick = rng.Next(count);
                for (int i=0;i<models.Count;i++) if(Eligible(models[i],band,side,wantLength) && pick--==0) { deck.Add(models[i]); break; }
            }
            int row=rng.Next(Pairs.Length),pairMean=12+2*rng.Next(7),order=rng.Next(2);
            string wanted="pair-"+row+"-"+pairMean+"-"+order;
            for(int role=0;role<2;role++)
                for(int i=0;i<models.Count;i++)
                    if(models[i].pairId==wanted&&models[i].pairRole==(role==0?"A":"B")){deck.Add(models[i]);break;}
            if(deck.Count!=6||deck[4].pairId!=deck[5].pairId)throw new Exception("짝 자료 덱 불변식 위반");
            return deck;
        }

        static bool Eligible(CushionProblem p,int band,int side,int length)
        {
            return p.band==band&&p.side==side&&p.raw.Length==length&&string.IsNullOrEmpty(p.pairId);
        }

        public static string ArrayText(int[] a)
        {
            var sb = new StringBuilder();
            for (int i=0;i<a.Length;i++) { if(i>0) sb.Append(", "); sb.Append(a[i]); }
            return sb.ToString();
        }

        [Serializable] sealed class BankWrap { public Sample[] items; }
        [Serializable] sealed class Sample
        {
            public string id, prompt, answer, unitConcept;
            public string[] choices;
            public double answerNumeric;
            public int[] rawData, deviations;
            public int mean, sumSquaredDeviations, variance, standardDeviation, band;
            public string pairId, pairRole;
            public int[] pairedRawData;
            public int pairedVariance, pairedStandardDeviation;
        }

        public static string BankJson(List<CushionProblem> models)
        {
            var items = new Sample[models.Count];
            for (int i=0;i<models.Count;i++)
            {
                var p=models[i];
                items[i]=new Sample {
                    id=p.id, prompt=p.prompt, choices=null, answer=p.side+"칸", answerNumeric=p.side,
                    unitConcept=p.concept, rawData=p.raw, mean=p.mean, deviations=p.deviations,
                    sumSquaredDeviations=p.sumSquares, variance=p.variance, standardDeviation=p.side, band=p.band,
                    pairId=p.pairId,pairRole=p.pairRole,pairedRawData=p.pairedRaw,
                    pairedVariance=p.pairedVariance,pairedStandardDeviation=p.pairedSide
                };
            }
            return JsonUtility.ToJson(new BankWrap{items=items});
        }
    }
}
