// MGF Unity kit — 게임 ↔ 공장 계약 타입. (factory/unity/kit 이 정본. 워크스페이스 사본을 고치지 마라)
//
// 게임 코드(namespace Mgf.<Slug>)는 MonoBehaviour 하나가 IMgfGame 을 구현하고
// Awake/Start 에서 MgfBridge.Register(this) 를 부른다. 나머지(JS 훅·상태 푸시·문제 은행
// 푸시·음소거)는 킷이 한다. 계약 전문: docs/unity-track.md
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mgf
{
    /// <summary>QA 훅(window.__GAME_TEST__)이 부르는 게임 쪽 구현.</summary>
    public interface IMgfGame
    {
        /// <summary>인트로를 건너뛰고 즉시 플레이 상태로. 이미 플레이 중이면 새 판을 시작해도 된다.</summary>
        void TestStart();
        /// <summary>현재 문제를 정답 처리(점수 상승). 실제 정답 경로와 같은 함수를 타야 한다.</summary>
        void TestAnswerCorrect();
        /// <summary>현재 문제를 오답 처리(목숨/점수 변화).</summary>
        void TestAnswerWrong();
        /// <summary>현재 상태 JSON. 최소 {score,lives,level,phase,solved}. MgfState 를 상속해 JsonUtility.ToJson 권장.
        /// 시간처럼 입력 없이 계속 변하는 값은 넣지 마라 — QA input.real 이 무입력 변화와 입력 반응을 구분 못 한다.</summary>
        string StateJson();
        /// <summary>문제 은행 JSON. MgfJson.Bank(list) 로 만든다. 부팅 시 1회 호출된다. 고유 문항 300개 이상 권장.</summary>
        string ProblemBankJson();
    }

    /// <summary>getState() 최소 스키마. 게임은 상속해 필드를 더해도 된다(JsonUtility 가 부모 필드까지 직렬화).</summary>
    [Serializable]
    public class MgfState
    {
        public int score;
        public int lives = 3;
        public int level = 1;
        /// <summary>"title" | "playing" | "paused" | "gameover" | "clear" 권장</summary>
        public string phase = "title";
        public int solved;
    }

    /// <summary>sampleProblems(n) 가 돌려주는 문항 한 개. CLAUDE.md 스키마와 같다.</summary>
    [Serializable]
    public class MgfProblem
    {
        public string id;
        /// <summary>학생에게 보이는 문제 문장(화면 문장과 같아야 한다)</summary>
        public string prompt;
        /// <summary>객관식이면 선택지(정답 포함), 아니면 null 또는 빈 배열(JS 에서 null 로 바뀐다)</summary>
        public string[] choices;
        public string answer;
        /// <summary>정답의 수치(검산용). 수치로 못 나타내면 double.NaN → JS 에서 필드 생략.</summary>
        public double answerNumeric = double.NaN;
        public string unitConcept;
    }

    public static class MgfJson
    {
        [Serializable]
        class BankWrap { public MgfProblem[] items; }

        /// <summary>문제 은행 → {"items":[...]} JSON. 중복 prompt 는 경고만 남긴다(QA 다양성 70% 기준).</summary>
        public static string Bank(IList<MgfProblem> problems)
        {
            var arr = new MgfProblem[problems.Count];
            var seen = new HashSet<string>();
            int dup = 0;
            for (int i = 0; i < problems.Count; i++)
            {
                var p = problems[i];
                if (string.IsNullOrEmpty(p.id)) p.id = "p" + (i + 1);
                if (!seen.Add(p.prompt + "|" + p.answer)) dup++;
                arr[i] = p;
            }
            if (dup > 0) Debug.LogWarning("[MGF] 문제 은행에 중복 문항 " + dup + "개");
            // JsonUtility 는 NaN 을 그대로 NaN 으로 쓴다(유효한 JSON 아님) → 문자열로 치환해 JS 에서 처리.
            return JsonUtility.ToJson(new BankWrap { items = arr }).Replace(":NaN", ":null");
        }

        /// <summary>JSON 문자열 이스케이프(직접 조립할 때).</summary>
        public static string Esc(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
