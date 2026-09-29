// 배율 밀어 — 합성 효과음(외부 음원 없음). 부팅 때 1회 굽는다. 오디오는 첫 사용자 제스처 뒤에만 난다(킷·브라우저 정책).
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.BaeyulMireo
{
    public static class BaeyulSound
    {
        const int Rate = 44100;
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static float lastTick;

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f;
            var r = new System.Random(11);
            // 놋쇠 레일 한 칸: 짧은 금속 딸깍
            clips["tick"] = Make("tick", 0.035f, (t, i) => Ping(t, 2900f, 160f) * 0.32f + Hiss(r) * Env(t, 0.004f) * 0.2f);
            // 플래튼이 종이를 누름: 낮은 쿵 + 종이 눌림 잡음
            clips["thunk"] = Make("thunk", 0.5f, (t, i) => Mathf.Sin(2 * Mathf.PI * (70f + 40f * Mathf.Exp(-t * 30f)) * t) * Mathf.Exp(-t * 9f) * 0.9f + Lp(r, 0.25f) * Env(t, 0.06f) * 0.7f);
            // 잉크가 번짐: 젖은 끈적 소리(저역 잡음 스윕)
            clips["ink"] = Make("ink", 0.45f, (t, i) => Lp(r, 0.05f + 0.1f * t) * Mathf.Sin(Mathf.PI * t / 0.45f) * 0.55f + Ping(t - 0.02f, 880f, 14f) * 0.12f);
            // 정답 상승음(교정 완료 벨)
            clips["good"] = Make("good", 0.6f, (t, i) => (Ping(t, 784f, 7f) + Ping(t - 0.07f, 1175f, 7f) + Ping(t - 0.14f, 1568f, 8f)) * 0.26f);
            clips["spike"] = Make("spike", 0.3f, (t, i) => Hiss(r) * Env(t, 0.05f) * 0.6f + Ping(t - 0.04f, 1500f, 40f) * 0.25f + Mathf.Sin(2 * Mathf.PI * 120f * t) * Mathf.Exp(-t * 25f) * 0.4f);
            clips["crumple"] = Make("crumple", 0.5f, (t, i) => (r.NextDouble() < 0.08 * Mathf.Exp(-t * 3f) ? (float)(r.NextDouble() - 0.5) * 1.6f : 0f) + Lp(r, 0.5f) * Env(t, 0.4f) * 0.35f);
            clips["smear"] = Make("smear", 0.45f, (t, i) => Mathf.Sin(2 * Mathf.PI * 62f * t) * Mathf.Exp(-t * 8f) * 0.7f + Lp(r, 0.4f) * Env(t, 0.3f) * 0.4f + Sq(t, 110f) * Env(t, 0.12f) * 0.08f);
            clips["refuse"] = Make("refuse", 0.22f, (t, i) => Sq(t, t < 0.09f ? 330f : 262f) * (t < 0.09f || t > 0.12f ? 1f : 0f) * Mathf.Exp(-t * 6f) * 0.24f);
            clips["slide"] = Make("slide", 0.32f, (t, i) => Lp(r, 0.14f) * Mathf.Sin(Mathf.PI * t / 0.32f) * 0.45f);
            clips["press"] = Make("press", 0.14f, (t, i) => Mathf.Sin(2 * Mathf.PI * (420f + 380f * t / 0.14f) * t) * Env(t, 0.13f) * 0.4f);
            clips["ripple"] = Make("ripple", 0.08f, (t, i) => Mathf.Sin(2 * Mathf.PI * 700f * t) * Env(t, 0.07f) * 0.16f);
            clips["lock"] = Make("lock", 0.12f, (t, i) => (Ping(t, 2100f, 60f) + Ping(t - 0.045f, 2800f, 70f)) * 0.4f);
            clips["combo"] = Make("combo", 0.35f, (t, i) => (Ping(t, 1568f, 10f) + Ping(t - 0.08f, 2093f, 10f)) * 0.28f);
            clips["blip"] = Make("blip", 0.05f, (t, i) => Mathf.Sin(2 * Mathf.PI * 1320f * t) * Env(t, 0.04f) * 0.26f);
            clips["end"] = Make("end", 0.8f, (t, i) => (Ping(t, 523f, 4f) + Ping(t - 0.12f, 659f, 4f) + Ping(t - 0.24f, 784f, 4f)) * 0.3f);
        }

        public static void Play(string k, float vol = 1f, float pitch = 1f)
        {
            if (!src || !clips.TryGetValue(k, out var c)) return;
            if (k == "tick") { if (Time.unscaledTime - lastTick < 0.03f) return; lastTick = Time.unscaledTime; }
            src.pitch = pitch;
            src.PlayOneShot(c, vol);
        }

        delegate float Fn(float t, int i);
        static AudioClip Make(string name, float dur, Fn f)
        {
            int n = (int)(Rate * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float fade = Mathf.Min(1f, (n - i) / (Rate * 0.01f));
                d[i] = Mathf.Clamp(f(t, i) * fade, -1f, 1f);
            }
            var c = AudioClip.Create("bm-" + name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
        static float Ping(float t, float hz, float decay) => t < 0 ? 0 : Mathf.Sin(2 * Mathf.PI * hz * t) * Mathf.Exp(-t * decay) * Mathf.Min(1f, t / 0.0015f);
        static float Env(float t, float len) => t < 0 || t > len ? 0 : (1f - t / len) * Mathf.Min(1f, t / 0.002f);
        static float Hiss(System.Random r) => (float)r.NextDouble() * 2f - 1f;
        static float Sq(float t, float hz) => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * hz * t));
        static float lpState;
        static float Lp(System.Random r, float k) { lpState += (Hiss(r) - lpState) * k; return lpState; }
    }
}
