// 화물 잼 — 중2 도형의 닮음 문제 모델.
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 중학교 지시형 「~하시오」를 사용하고 길이는 `6 cm`, 각은 `60°`로 쓴다.
// - △ABC∽△DEF의 꼭짓점은 correspondence 배열 순서를 지킨다.
// - 중2 범위이므로 √, 무리수, 삼각비를 만들지 않는다.
// - 답은 SSS/SAS/AA의 정수 교차곱·정수 각 비교로만 판정한다.
// 오개념 역산(misconceptionId): additive_reasoning, correspondence_order,
// non_included_angle, one_angle_only. 모든 후보 풀은 ValidateBank에서 전수 검사한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.HwamulJam
{
    public enum SimilarityKind { SSS, SAS, AA }

    [Serializable]
    public sealed class CargoData
    {
        public int[] v = new int[3];
        public string label = "";
        public string misconceptionId = "";
    }

    [Serializable]
    public sealed class SimilarityOrder
    {
        public string id = "";
        public SimilarityKind kind;
        public int band;
        public int serial;
        public int[] target = new int[3];
        public CargoData[] cargo = new CargoData[8];
        public int correctIndex;
        public string prompt = "";
        public string targetLabel = "";
        public string reveal = "";
        public string concept = "";
    }

    public static class HwamulRules
    {
        public const int CandidateCount = 8;
        public const int Goal = 8;
        public const int StartLives = 3;
        public const float RunSeconds = 90f;

        static readonly int[][] SssBases = {
            new[]{3,4,5}, new[]{4,5,6}, new[]{5,6,8}, new[]{5,7,9},
            new[]{6,7,10}, new[]{6,8,11}, new[]{7,9,12}, new[]{8,10,13}
        };
        static readonly int[][] SasBases = {
            new[]{3,5,40}, new[]{4,6,60}, new[]{5,7,50}, new[]{6,8,70},
            new[]{4,7,45}, new[]{5,8,80}, new[]{6,9,55}, new[]{7,10,65}
        };
        static readonly int[][] AaBases = {
            new[]{40,60,80}, new[]{45,55,80}, new[]{50,60,70}, new[]{35,65,80},
            new[]{30,70,80}, new[]{45,65,70}, new[]{40,55,85}, new[]{50,55,75}
        };

        static int Pos(int x) { return x & 0x7fffffff; }
        public static int CorrectSlot(int serial)
        {
            // 8칸을 균등하게 순회하되 인접 주문에서는 멀리 이동한다.
            return (serial * 5 + 3) & 7;
        }

        static CargoData C(int a, int b, int c, string label, string misconception)
        {
            return new CargoData { v = new[] { a, b, c }, label = label, misconceptionId = misconception };
        }

        // SSS도 배열 위치가 곧 대응변이라는 사실을 화면에 명시한다. 변 이름을 빼면
        // 순열 후보가 모두 같은 세 변의 삼각형으로 읽혀 단일 정답이 아니게 된다.
        static string SssLabel(int[] x) { return "△DEF  DE " + x[0] + " cm · EF " + x[1] + " cm · FD " + x[2] + " cm"; }
        static string SasLabel(int[] x) { return "DE " + x[0] + " cm · DF " + x[1] + " cm · ∠D " + x[2] + "°"; }
        static string AaLabel(int[] x) { return "∠D " + x[0] + "° · ∠E " + x[1] + "° · ∠F " + x[2] + "°"; }

        static SimilarityOrder MakeSss(int serial, int band)
        {
            int[] b = SssBases[Pos(serial * 3 + 1) % SssBases.Length];
            int scale = 2 + (Pos(serial / 7) % 2);
            int k = 2 + Pos(serial) % 4;
            int[] good = { b[0] * scale, b[1] * scale, b[2] * scale };
            var raw = new List<CargoData> {
                C(good[0],good[1],good[2],SssLabel(good),""),
                C(b[0]+k,b[1]+k,b[2]+k,SssLabel(new[]{b[0]+k,b[1]+k,b[2]+k}),"additive_reasoning"),
                C(good[1],good[0],good[2],SssLabel(new[]{good[1],good[0],good[2]}),"correspondence_order"),
                C(good[0],good[1]+1,good[2],SssLabel(new[]{good[0],good[1]+1,good[2]}),"single_side_ratio"),
                C(b[0]*scale,b[1]*(scale+1),b[2]*scale,SssLabel(new[]{b[0]*scale,b[1]*(scale+1),b[2]*scale}),"single_side_ratio"),
                C(good[0]+2,good[1]+2,good[2]+1,SssLabel(new[]{good[0]+2,good[1]+2,good[2]+1}),"additive_reasoning"),
                C(good[2],good[1],good[0],SssLabel(new[]{good[2],good[1],good[0]}),"correspondence_order"),
                C(good[0]-1,good[1],good[2]+1,SssLabel(new[]{good[0]-1,good[1],good[2]+1}),"single_side_ratio")
            };
            var p = new SimilarityOrder {
                id="sss-"+serial.ToString("D3"), kind=SimilarityKind.SSS, band=band, serial=serial,
                target=(int[])b.Clone(), concept="SSS 닮음과 대응변의 비",
                prompt="주문 "+serial.ToString("D3")+": △ABC∽△DEF의 순서를 지키는 화물을 도크로 보내시오.",
                targetLabel="목표 △ABC  AB "+b[0]+" cm · BC "+b[1]+" cm · CA "+b[2]+" cm",
                reveal=b[0]+":"+good[0]+" = "+b[1]+":"+good[1]+" = "+b[2]+":"+good[2]+" · SSS 닮음"
            };
            Place(p, raw); return p;
        }

        static SimilarityOrder MakeSas(int serial, int band)
        {
            int[] b = SasBases[Pos(serial * 5 + 2) % SasBases.Length];
            int scale = 2 + Pos(serial / 5) % 2;
            int[] good = { b[0] * scale, b[1] * scale, b[2] };
            var raw = new List<CargoData> {
                C(good[0],good[1],good[2],SasLabel(good),""),
                C(good[0],good[1],good[2]+10,SasLabel(new[]{good[0],good[1],good[2]+10}),"non_included_angle"),
                C(good[0],good[1]+1,good[2],SasLabel(new[]{good[0],good[1]+1,good[2]}),"single_side_ratio"),
                C(good[1],good[0],good[2],SasLabel(new[]{good[1],good[0],good[2]}),"correspondence_order"),
                C(b[0]+scale,b[1]+scale,b[2],SasLabel(new[]{b[0]+scale,b[1]+scale,b[2]}),"additive_reasoning"),
                C(good[0],good[1],Math.Max(20,good[2]-5),SasLabel(new[]{good[0],good[1],Math.Max(20,good[2]-5)}),"non_included_angle"),
                C(good[0]+2,good[1]+2,good[2],SasLabel(new[]{good[0]+2,good[1]+2,good[2]}),"additive_reasoning"),
                C(good[0]-1,good[1],good[2],SasLabel(new[]{good[0]-1,good[1],good[2]}),"single_side_ratio")
            };
            var p = new SimilarityOrder {
                id="sas-"+serial.ToString("D3"), kind=SimilarityKind.SAS, band=band, serial=serial,
                target=(int[])b.Clone(), concept="SAS 닮음과 끼인각",
                prompt="주문 "+serial.ToString("D3")+": 두 대응변의 비와 끼인각을 확인하여 닮은 화물을 보내시오.",
                targetLabel="목표 △ABC  AB "+b[0]+" cm · AC "+b[1]+" cm · ∠A "+b[2]+"°",
                reveal=b[0]+":"+good[0]+" = "+b[1]+":"+good[1]+", 끼인각 "+b[2]+"° · SAS 닮음"
            };
            Place(p, raw); return p;
        }

        static SimilarityOrder MakeAa(int serial, int band)
        {
            int[] b = AaBases[Pos(serial * 7 + 4) % AaBases.Length];
            int d1 = 5 + Pos(serial) % 2 * 5;
            // 한 각만 같은 두 오개념 후보가 같은 수치가 되지 않도록 변화량을 상보로 고정한다.
            int d2 = d1 == 5 ? 10 : 5;
            var raw = new List<CargoData> {
                C(b[0],b[1],b[2],AaLabel(b),""),
                C(b[0],b[1]+d1,b[2]-d1,AaLabel(new[]{b[0],b[1]+d1,b[2]-d1}),"one_angle_only"),
                C(b[0]+d1,b[1],b[2]-d1,AaLabel(new[]{b[0]+d1,b[1],b[2]-d1}),"one_angle_only"),
                C(b[0]+d1,b[1]-d1,b[2],AaLabel(new[]{b[0]+d1,b[1]-d1,b[2]}),"correspondence_order"),
                C(b[0],b[1]-d2,b[2]+d2,AaLabel(new[]{b[0],b[1]-d2,b[2]+d2}),"one_angle_only"),
                C(b[0]-d1,b[1]+d1,b[2],AaLabel(new[]{b[0]-d1,b[1]+d1,b[2]}),"correspondence_order"),
                C(b[0]+d2,b[1],b[2]-d2,AaLabel(new[]{b[0]+d2,b[1],b[2]-d2}),"one_angle_only"),
                C(b[0],b[1]+d2,b[2]-d2,AaLabel(new[]{b[0],b[1]+d2,b[2]-d2}),"one_angle_only")
            };
            var p = new SimilarityOrder {
                id="aa-"+serial.ToString("D3"), kind=SimilarityKind.AA, band=band, serial=serial,
                target=(int[])b.Clone(), concept="AA 닮음과 대응각",
                prompt="주문 "+serial.ToString("D3")+": 두 대응각을 확인하여 닮은 화물을 도크로 보내시오.",
                targetLabel="목표 △ABC  ∠A "+b[0]+"° · ∠B "+b[1]+"° · ∠C "+b[2]+"°",
                reveal="∠A=∠D="+b[0]+"°, ∠B=∠E="+b[1]+"° · AA 닮음"
            };
            Place(p, raw); return p;
        }

        static void Place(SimilarityOrder p, List<CargoData> raw)
        {
            p.correctIndex = CorrectSlot(p.serial);
            int w = 1;
            for (int i=0;i<CandidateCount;i++) p.cargo[i] = i==p.correctIndex ? raw[0] : raw[w++];
        }

        public static SimilarityOrder FixedPractice()
        {
            var p=MakeSss(0,0);
            p.id="practice-fixed";p.prompt="연습: 목표 △ABC와 닮은 화물을 찾아 길을 열어 도크로 보내시오.";
            p.target=new[]{3,4,5};p.targetLabel="목표 △ABC  AB 3 cm · BC 4 cm · CA 5 cm";
            var raw=new List<CargoData>{
                C(6,8,10,SssLabel(new[]{6,8,10}),""),
                C(5,6,7,SssLabel(new[]{5,6,7}),"additive_reasoning"),
                C(6,9,10,SssLabel(new[]{6,9,10}),"single_side_ratio"),
                C(8,6,10,SssLabel(new[]{8,6,10}),"correspondence_order"),
                C(9,12,14,SssLabel(new[]{9,12,14}),"single_side_ratio"),
                C(4,5,6,SssLabel(new[]{4,5,6}),"additive_reasoning"),
                C(7,8,10,SssLabel(new[]{7,8,10}),"single_side_ratio"),
                C(10,8,6,SssLabel(new[]{10,8,6}),"correspondence_order")
            };
            p.correctIndex=1; p.cargo=new CargoData[8]; p.cargo[0]=raw[1];p.cargo[1]=raw[0];
            for(int i=2;i<8;i++)p.cargo[i]=raw[i];
            p.reveal="3:6 = 4:8 = 5:10 · 세 대응변의 비가 같으므로 SSS 닮음";
            return p;
        }

        public static List<SimilarityOrder> BuildBank()
        {
            var list=new List<SimilarityOrder>(384);
            for(int i=1;i<=384;i++)
            {
                int kind=(i-1)%3; int band=i<=128?1:i<=256?2:3;
                list.Add(kind==0?MakeSss(i,band):kind==1?MakeSas(i,band):MakeAa(i,band));
            }
            ValidateBank(list); return list;
        }

        public static List<SimilarityOrder> RunDeck(List<SimilarityOrder> bank, int runSerial)
        {
            var deck=new List<SimilarityOrder>(Goal);
            int start=Pos(runSerial*37)%bank.Count;
            // 2 SSS → 3 SAS/AA → 3 혼합의 눈에 보이는 밴드 순서.
            int[] offsets={0,3,130,131,134,258,259,262};
            for(int i=0;i<Goal;i++)deck.Add(bank[(start+offsets[i])%bank.Count]);
            return deck;
        }

        public static bool IsCorrect(SimilarityOrder p,int cargoIndex)
        {
            if(p==null||cargoIndex<0||cargoIndex>=CandidateCount)return false;
            int[] t=p.target,c=p.cargo[cargoIndex].v;
            if(p.kind==SimilarityKind.SSS)return (long)t[0]*c[1]==(long)t[1]*c[0]&&(long)t[1]*c[2]==(long)t[2]*c[1];
            if(p.kind==SimilarityKind.SAS)return (long)t[0]*c[1]==(long)t[1]*c[0]&&t[2]==c[2];
            return t[0]==c[0]&&t[1]==c[1];
        }

        public static void ValidateBank(List<SimilarityOrder> bank)
        {
            var prompts=new HashSet<string>();
            foreach(var p in bank)
            {
                if(!prompts.Add(p.prompt))throw new InvalidOperationException("발문 중복 "+p.id);
                var choices=new HashSet<string>();int answers=0,mis=0;
                for(int i=0;i<CandidateCount;i++)
                {
                    if(!choices.Add(p.cargo[i].label))throw new InvalidOperationException("후보 중복 "+p.id);
                    if(p.kind==SimilarityKind.SSS&&
                       (!p.cargo[i].label.Contains("DE ")||!p.cargo[i].label.Contains("EF ")||!p.cargo[i].label.Contains("FD ")))
                        throw new InvalidOperationException("SSS 대응변 표기 누락 "+p.id+" cargo "+i);
                    bool ok=IsCorrect(p,i);if(ok)answers++;
                    if(!string.IsNullOrEmpty(p.cargo[i].misconceptionId))mis++;
                    if(ok!=(i==p.correctIndex))throw new InvalidOperationException("우연 정답 "+p.id+" cargo "+i);
                }
                if(answers!=1||mis<2)throw new InvalidOperationException("정답/오개념 수 오류 "+p.id);
            }
        }

        public static MgfProblem ToMgf(SimilarityOrder p)
        {
            var choices=new string[CandidateCount];for(int i=0;i<CandidateCount;i++)choices[i]=p.cargo[i].label;
            return new MgfProblem { id=p.id,prompt=p.prompt+" "+p.targetLabel,choices=choices,
                answer=p.cargo[p.correctIndex].label,answerNumeric=p.correctIndex,unitConcept=p.concept };
        }
    }
}
