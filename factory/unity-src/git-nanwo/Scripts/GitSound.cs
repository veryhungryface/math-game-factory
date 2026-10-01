// 깃 나눠 — 합성 효과음(외부 음원 없음). 부팅 때 1회 굽는다.
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.GitNanwo
{
    public static class GitSound
    {
        const int Rate = 44100;
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static float lastTick;

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f;
            var r = new System.Random(19);
            clips["tick"] = Make("tick", 0.04f, (t, i) => Ping(t, 2100f, 140f) * 0.28f);
            clips["drop"] = Make("drop", 0.22f, (t, i) =>
                Mathf.Sin(2 * Mathf.PI * 110f * t) * Mathf.Exp(-t * 16f) * 0.85f
                + Ping(t, 780f, 28f) * 0.35f + Lp(r, 0.28f) * Env(t, 0.05f) * 0.45f);
            clips["tear"] = Make("tear", 0.28f, (t, i) =>
                Hiss(r) * Env(t, 0.08f) * 0.55f + Ping(t, 3400f, 40f) * 0.2f + Ping(t - 0.04f, 1900f, 22f) * 0.18f);
            clips["slide"] = Make("slide", 0.40f, (t, i) => Lp(r, 0.14f) * Mathf.Sin(Mathf.PI * t / 0.40f) * 0.5f);
            clips["clang"] = Make("clang", 0.18f, (t, i) =>
                (Ping(t, 2480f, 50f) + Ping(t, 3720f, 70f) * 0.5f) * 0.4f);
            clips["rattle"] = Make("rattle", 0.28f, (t, i) =>
                Ping(t, 420f, 18f) * 0.4f + Ping(t - 0.06f, 380f, 16f) * 0.35f + Ping(t - 0.12f, 340f, 14f) * 0.3f);
            clips["impale"] = Make("impale", 0.22f, (t, i) =>
                Ping(t, 190f, 12f) * 0.7f + Hiss(r) * Env(t, 0.04f) * 0.3f);
            clips["refuse"] = Make("refuse", 0.20f, (t, i) => Sq(t, t < 0.08f ? 310f : 246f) * (t < 0.08f || t > 0.11f ? 1f : 0f) * Mathf.Exp(-t * 7f) * 0.26f);
            clips["ripple"] = Make("ripple", 0.10f, (t, i) => Mathf.Sin(2 * Mathf.PI * 640f * t) * Env(t, 0.09f) * 0.2f);
            clips["press"] = Make("press", 0.16f, (t, i) => Mathf.Sin(2 * Mathf.PI * (480f + 520f * t / 0.16f) * t) * Env(t, 0.15f) * 0.42f);
            clips["combo"] = Make("combo", 0.32f, (t, i) => (Ping(t, 1568f, 10f) + Ping(t - 0.07f, 1976f, 10f) + Ping(t - 0.14f, 2349f, 9f)) * 0.28f);
            clips["win"] = Make("win", 0.70f, (t, i) => (Ping(t, 523f, 5f) + Ping(t - 0.10f, 659f, 5f) + Ping(t - 0.20f, 784f, 5f) + Ping(t - 0.32f, 1047f, 4f)) * 0.32f);
            clips["lose"] = Make("lose", 0.55f, (t, i) => (Ping(t, 392f, 6f) + Ping(t - 0.12f, 311f, 6f) + Ping(t - 0.24f, 247f, 5f)) * 0.34f);
            clips["counter"] = Make("counter", 0.08f, (t, i) => Ping(t, 980f + 40f, 80f) * 0.22f);
        }

        public static void Play(string k, float vol = 1f)
        {
            if (!src || !clips.TryGetValue(k, out var c)) return;
            if (k == "tick") { if (Time.unscaledTime - lastTick < 0.045f) return; lastTick = Time.unscaledTime; }
            src.pitch = 1f;
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
            var c = AudioClip.Create("gn-" + name, n, 1, Rate, false);
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
