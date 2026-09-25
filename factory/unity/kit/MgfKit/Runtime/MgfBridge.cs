// MGF Unity kit — JS(window.__GAME_TEST__, 음소거 버튼) ↔ C# 브리지.
//
//   JS → C# : unityInstance.SendMessage('MgfBridge', 'OnCmd', '{"t":"start"}')   (동기 호출)
//             t = start | correct | wrong | mute(on) | ping
//   C# → JS : MgfBridge.jslib 의 MGF_Ready / MGF_PushState / MGF_PushBank
//
// 이 오브젝트는 씬과 무관하게 부팅 시 자동 생성된다(이름 "MgfBridge" 고정 — SendMessage 대상).
// 게임은 MgfBridge.Register(this) 한 줄만 부른다. 상태는 0.2초마다 + NotifyChanged() 시 + 명령 직후 푸시.
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Mgf
{
    [DefaultExecutionOrder(-1000)]
    public sealed class MgfBridge : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void MGF_Ready();
        [DllImport("__Internal")] static extern void MGF_PushState(string json);
        [DllImport("__Internal")] static extern void MGF_PushBank(string json);
        [DllImport("__Internal")] static extern int MGF_InitialMuted();
        [DllImport("__Internal")] static extern int MGF_LowGfx();
#else
        static void MGF_Ready() { Debug.Log("[MGF] ready"); }
        static void MGF_PushState(string json) { }
        static void MGF_PushBank(string json) { }
        static int MGF_InitialMuted() { return 0; }
        static int MGF_LowGfx() { return 0; }
#endif
        public const string ObjectName = "MgfBridge";
        const float PushInterval = 0.2f;

        public static MgfBridge Instance { get; private set; }
        public static IMgfGame Game { get; private set; }
        public static bool Muted { get; private set; }
        /// <summary>소프트웨어 렌더러(GPU 없음)이거나 ?lowgfx=1 이면 true. MgfLook.Quality() 가 그림자·MSAA 를 끈다.</summary>
        public static bool LowGfx { get; private set; }
        /// <summary>HTML 음소거 버튼이 눌렸을 때(true=음소거). 킷이 AudioListener.volume 은 이미 처리한다.</summary>
        public static event Action<bool> MuteChanged;

        float timer;
        bool dirty;
        string lastState;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Instance) return;
            var go = new GameObject(ObjectName);
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MgfBridge>();
            // WebGL 은 rAF 로 돈다. targetFrameRate 를 지정하면 setTimeout 루프로 바뀌어 오히려 끊긴다.
            Application.targetFrameRate = -1;
            Application.runInBackground = true;
            SetMuted(MGF_InitialMuted() == 1, false);
            LowGfx = MGF_LowGfx() == 1;
        }

        /// <summary>게임이 Awake/Start 에서 1회 호출. 문제 은행을 푸시하고 JS 에 ready 를 알린다.</summary>
        public static void Register(IMgfGame game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (!Instance) Boot();
            Game = game;
            MGF_PushBank(game.ProblemBankJson());
            Instance.PushNow();
            MGF_Ready();
        }

        /// <summary>상태가 바뀌었으니 이번 프레임 끝에 즉시 푸시하라.</summary>
        public static void NotifyChanged()
        {
            if (Instance) Instance.dirty = true;
        }

        void LateUpdate()
        {
            if (Game == null) return;
            timer += Time.unscaledDeltaTime;
            if (dirty || timer >= PushInterval) PushNow();
        }

        void PushNow()
        {
            dirty = false;
            timer = 0f;
            if (Game == null) return;
            var s = Game.StateJson();
            if (string.IsNullOrEmpty(s) || s == lastState) return;
            lastState = s;
            MGF_PushState(s);
        }

        [Serializable]
        class Cmd { public string t; public bool on; }

        /// <summary>JS 가 SendMessage 로 부른다. 동기 실행 → 반환 전에 상태가 푸시된다.</summary>
        public void OnCmd(string json)
        {
            Cmd c;
            try { c = JsonUtility.FromJson<Cmd>(json); }
            catch (Exception) { Debug.LogWarning("[MGF] 잘못된 명령: " + json); return; }
            if (c == null || string.IsNullOrEmpty(c.t)) return;
            switch (c.t)
            {
                case "start": Game?.TestStart(); break;
                case "correct": Game?.TestAnswerCorrect(); break;
                case "wrong": Game?.TestAnswerWrong(); break;
                case "mute": SetMuted(c.on, true); break;
                case "ping": break;
                default: Debug.LogWarning("[MGF] 알 수 없는 명령: " + c.t); break;
            }
            lastState = null; // 명령 직후엔 같은 상태라도 다시 보낸다
            PushNow();
        }

        static void SetMuted(bool on, bool notify)
        {
            Muted = on;
            AudioListener.volume = on ? 0f : 1f;
            if (notify) MuteChanged?.Invoke(on);
        }
    }
}
