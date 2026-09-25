// Unity 트랙 스모크 게임 — 킷(브리지·템플릿·빌드) 검증용. 게시용 게임이 아니다.
// 5학년 1학기 1단원 「자연수의 혼합 계산」: 식을 보고 떠 있는 정답 블록을 탭한다.
// 구조 예시로 쓴다: 부트스트랩 MonoBehaviour 1개가 IMgfGame 을 구현하고 씬 전체를 코드로 만든다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.UnitySmoke
{
    public class SmokeGame : MonoBehaviour, IMgfGame
    {
        [System.Serializable]
        class State : MgfState { public int streak; public string prompt; }

        class Problem { public string prompt; public int answer; public int[] choices; public string concept; }

        readonly State st = new State();
        readonly List<Problem> bank = new List<Problem>();
        readonly System.Random rng = new System.Random(20260926);
        Problem cur;
        Camera cam;
        readonly GameObject[] blocks = new GameObject[3];
        readonly TextMeshPro[] labels = new TextMeshPro[3];
        readonly Vector3[] home = new Vector3[3];
        TextMeshProUGUI promptUi, hudUi, hintUi;
        Material blockMat, goodMat, badMat;
        int bankCursor;

        void Awake()
        {
            MgfLook.Quality(30f);
            MgfLook.Sky(MgfLook.Hex("5A8FD8"), MgfLook.Hex("F6E2C4"), MgfLook.Hex("3C5C7A"));
            MgfLook.Sun(new Vector3(48, -32, 0), MgfLook.Hex("FFF1D6"), 1.15f);
            cam = MgfLook.Camera(new Vector3(0, 5.2f, -9.5f), new Vector3(0, 1.2f, 0), 34f);

            // 섬 바닥 + 장식 기둥
            MgfLook.Block("Island", new Vector3(0, -0.5f, 0), new Vector3(11, 1, 7), 0.45f, MgfLook.Lit(MgfLook.Hex("7FBF6A"), 0.15f));
            MgfLook.Block("Cliff", new Vector3(0, -1.6f, 0), new Vector3(10.2f, 1.4f, 6.2f), 0.5f, MgfLook.Lit(MgfLook.Hex("B98A5E"), 0.1f));
            var stone = MgfLook.Lit(MgfLook.Hex("E9DCC8"), 0.35f);
            for (int i = 0; i < 6; i++)
            {
                float x = -4.6f + i * 1.84f;
                MgfLook.Block("Pillar" + i, new Vector3(x, 0.35f + (i % 2) * 0.2f, 2.6f), new Vector3(0.7f, 0.7f + (i % 2) * 0.4f, 0.7f), 0.12f, stone);
            }

            blockMat = MgfLook.Lit(MgfLook.Hex("FFB84D"), 0.45f, 0f, MgfLook.Hex("2A1800"));
            goodMat = MgfLook.Lit(MgfLook.Hex("5FD38A"), 0.5f, 0f, MgfLook.Hex("0E3A20"));
            badMat = MgfLook.Lit(MgfLook.Hex("F2675C"), 0.5f, 0f, MgfLook.Hex("3A0E0E"));
            for (int i = 0; i < 3; i++)
            {
                home[i] = new Vector3(-2.7f + i * 2.7f, 1.35f, 0f);
                blocks[i] = MgfLook.Block("Answer" + i, home[i], new Vector3(1.9f, 1.9f, 1.9f), 0.32f, blockMat);
                MgfLook.Block("Pedestal" + i, new Vector3(home[i].x, 0.12f, 0), new Vector3(1.4f, 0.25f, 1.4f), 0.1f, stone);
                labels[i] = MgfText.World("0", new Vector3(0, 0, -0.97f), 7.5f, MgfLook.Hex("3B2412"), blocks[i].transform);
            }

            promptUi = MgfText.Ui("", new Vector2(0.5f, 1f), new Vector2(0, -150), 44, Color.white, 380);
            promptUi.outlineWidth = 0.18f;
            promptUi.outlineColor = new Color32(30, 40, 70, 200);
            hudUi = MgfText.Ui("", new Vector2(0.5f, 1f), new Vector2(0, -80), 22, Color.white, 360);
            hintUi = MgfText.Ui("화면을 눌러 시작해요", new Vector2(0.5f, 0f), new Vector2(0, 120), 24, Color.white, 360);
            MgfText.Prewarm("화면을 눌러 시작해요점수목숨단계다시하기끝났어요정답!틀렸어요");

            BuildBank();
            NextProblem();
            Refresh();
            MgfBridge.Register(this);
        }

        // ── 문제 생성기: 정수만 쓴다. 계산 순서(곱셈·나눗셈 먼저, 괄호 먼저)를 데이터 모델에서 직접 계산한다.
        void BuildBank()
        {
            var seen = new HashSet<string>();
            int guard = 0;
            while (bank.Count < 360 && guard++ < 20000)
            {
                var p = Make();
                if (p != null && seen.Add(p.prompt)) bank.Add(p);
            }
            // 섞기(결정적 시드)
            for (int i = bank.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (bank[i], bank[j]) = (bank[j], bank[i]); }
        }

        Problem Make()
        {
            int kind = rng.Next(4);
            int a = rng.Next(2, 20), b = rng.Next(2, 10), c = rng.Next(2, 10);
            int ans, naive; string prompt; string concept;
            switch (kind)
            {
                case 0: ans = a + b * c; naive = (a + b) * c; prompt = $"{a} + {b} × {c}"; concept = "덧셈과 곱셈의 혼합 계산"; break;
                case 1: if (a * b <= c) return null; ans = a * b - c; naive = a * (b - c); prompt = $"{a} × {b} − {c}"; concept = "곱셈과 뺄셈의 혼합 계산"; break;
                case 2: ans = (a + b) * c; naive = a + b * c; prompt = $"({a} + {b}) × {c}"; concept = "괄호가 있는 혼합 계산"; break;
                default:
                    int q = rng.Next(2, 10); int bb = c * q; // bb ÷ c = q (나누어떨어짐)
                    if (a + 20 <= q) return null;
                    ans = a + 20 - q; naive = (a + 20 - bb) / c; prompt = $"{a + 20} − {bb} ÷ {c}"; concept = "뺄셈과 나눗셈의 혼합 계산"; break;
            }
            if (ans < 0) return null;
            var set = new List<int> { ans };
            if (naive != ans && naive >= 0) set.Add(naive);
            int d = 1;
            while (set.Count < 3) { int cand = ans + (rng.Next(2) == 0 ? d : -d); if (cand >= 0 && !set.Contains(cand)) set.Add(cand); d++; }
            for (int i = set.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (set[i], set[j]) = (set[j], set[i]); }
            return new Problem { prompt = prompt + " = ?", answer = ans, choices = set.ToArray(), concept = concept };
        }

        void NextProblem()
        {
            cur = bank[bankCursor++ % bank.Count];
            for (int i = 0; i < 3; i++)
            {
                labels[i].text = cur.choices[i].ToString();
                blocks[i].GetComponent<Renderer>().sharedMaterial = blockMat;
            }
            st.prompt = cur.prompt;
        }

        void Refresh()
        {
            promptUi.text = st.phase == "playing" ? cur.prompt : st.phase == "gameover" ? "끝났어요!" : "혼합 계산 블록";
            hudUi.text = $"점수 {st.score}   목숨 {new string('♥', Mathf.Max(0, st.lives))}   {st.level}단계";
            hintUi.text = st.phase == "playing" ? "정답 블록을 눌러요" : st.phase == "gameover" ? "화면을 눌러 다시 하기" : "화면을 눌러 시작해요";
            MgfBridge.NotifyChanged();
        }

        void Begin()
        {
            st.score = 0; st.lives = 3; st.level = 1; st.solved = 0; st.streak = 0; st.phase = "playing";
            NextProblem();
            Refresh();
        }

        void Answer(int index)
        {
            if (st.phase != "playing") return;
            bool ok = cur.choices[index] == cur.answer;
            var b = blocks[index];
            if (ok)
            {
                st.score += 10 + st.streak * 2; st.solved++; st.streak++;
                st.level = 1 + st.solved / 5;
                b.GetComponent<Renderer>().sharedMaterial = goodMat;
                MgfFx.Punch(b.transform, 0.22f);
                MgfFx.Burst(b.transform.position + Vector3.up * 0.6f, MgfLook.Hex("FFE27A"), 32);
                MgfFx.Glow(b.transform.position, MgfLook.Hex("FFD27A"), 10);
                MgfSfx.Play("correct");
                NextProblemSoon();
            }
            else
            {
                st.lives--; st.streak = 0;
                b.GetComponent<Renderer>().sharedMaterial = badMat;
                MgfFx.Shake(cam, 0.14f);
                MgfSfx.Play("wrong");
                if (st.lives <= 0) { st.phase = "gameover"; MgfSfx.Play("lose"); }
            }
            Refresh();
        }

        float nextAt = -1;
        void NextProblemSoon() => nextAt = Time.time + 0.45f;

        void Update()
        {
            if (nextAt > 0 && Time.time >= nextAt) { nextAt = -1; if (st.phase == "playing") { NextProblem(); Refresh(); } }

            // 떠 있는 블록 — 은은한 흔들림(누적 없음: 기준 위치에서 매 프레임 계산)
            float t = Time.time;
            for (int i = 0; i < 3; i++)
            {
                blocks[i].transform.localPosition = home[i] + Vector3.up * (Mathf.Sin(t * 1.6f + i * 1.3f) * 0.12f);
                blocks[i].transform.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.9f + i) * 8f, 0);
            }
            MgfLook.FitWidth(cam, 34f, 1.5f);

            if (!MgfPointer.Down) return;
            if (st.phase != "playing") { MgfSfx.Play("tap"); Begin(); return; }
            if (MgfPointer.DownHit(cam, out var hit))
                for (int i = 0; i < 3; i++)
                    if (hit.collider.gameObject == blocks[i]) { Answer(i); break; }
        }

        // ── IMgfGame
        public void TestStart() => Begin();
        public void TestAnswerCorrect() { if (st.phase != "playing") Begin(); Answer(System.Array.IndexOf(cur.choices, cur.answer)); }
        public void TestAnswerWrong()
        {
            if (st.phase != "playing") Begin();
            for (int i = 0; i < 3; i++) if (cur.choices[i] != cur.answer) { Answer(i); return; }
        }
        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson()
        {
            var list = new List<MgfProblem>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                list.Add(new MgfProblem
                {
                    id = "s" + (i + 1),
                    prompt = p.prompt,
                    choices = System.Array.ConvertAll(p.choices, x => x.ToString()),
                    answer = p.answer.ToString(),
                    answerNumeric = p.answer,
                    unitConcept = p.concept
                });
            }
            return MgfJson.Bank(list);
        }
    }
}
