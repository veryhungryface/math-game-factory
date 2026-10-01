// 수문 — 합성 효과음(외부 음원 없음). 부팅 때 1회 굽는다.
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.Sumun
{
    public static class SumunSound
    {
        const int Rate = 44100;
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static float lastTick;

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f;
            var r = new System.Random(17);
            clips["tick"] = Make("tick", 0.04f, (t, i) => Ping(t, 2100f, 90f) * 0.28f + Hiss(r) * Env(t, 0.006f) * 0.12f);
            clips["clang"] = Make("clang", 0.38f, (t, i) => Ping(t, 420f, 8f) * 0.7f + Ping(t, 880f, 14f) * 0.35f + Ping(t - 0.04f, 1320f, 18f) * 0.2f);
            clips["pin"] = Make("pin", 0.18f, (t, i) => Ping(t, 1560f, 40f) * 0.5f + Ping(t - 0.03f, 980f, 30f) * 0.35f);
            clips["water"] = Make("water", 0.55f, (t, i) => Lp(r, 0.18f) * Mathf.Sin(Mathf.PI * t / 0.55f) * 0.55f + Ping(t, 240f, 6f) * 0.12f);
            clips["slide"] = Make("slide", 0.28f, (t, i) => Lp(r, 0.22f) * Mathf.Sin(Mathf.PI * t / 0.28f) * 0.5f);
            clips["leak"] = Make("leak", 0.4f, (t, i) => Lp(r, 0.35f) * Env(t, 0.38f) * 0.45f + Ping(t, 180f, 10f) * 0.2f);
            clips["good"] = Make("good", 0.55f, (t, i) => (Ping(t, 523f, 7f) + Ping(t - 0.08f, 784f, 8f) + Ping(t - 0.16f, 1047f, 8f)) * 0.28f);
            clips["refuse"] = Make("refuse", 0.2f, (t, i) => Sq(t, t < 0.08f ? 310f : 240f) * Env(t, 0.18f) * 0.22f);
            clips["ripple"] = Make("ripple", 0.08f, (t, i) => Mathf.Sin(2 * Mathf.PI * 640f * t) * Env(t, 0.07f) * 0.18f);
            clips["drop"] = Make("drop", 0.32f, (t, i) => Mathf.Sin(2 * Mathf.PI * (90f + 40f * Mathf.Exp(-t * 20f)) * t) * Mathf.Exp(-t * 8f) * 0.7f);
            clips["end"] = Make("end", 0.7f, (t, i) => (Ping(t, 392f, 5f) + Ping(t - 0.12f, 494f, 5f) + Ping(t - 0.24f, 587f, 5f)) * 0.28f);
            clips["win"] = Make("win", 0.8f, (t, i) => (Ping(t, 523f, 5f) + Ping(t - 0.1f, 659f, 5f) + Ping(t - 0.2f, 784f, 5f) + Ping(t - 0.32f, 1047f, 6f)) * 0.26f);
        }

        public static void Play(string k, float vol = 1f, float pitch = 1f)
        {
            if (!src || !clips.TryGetValue(k, out var c)) return;
            if (k == "tick") { if (Time.unscaledTime - lastTick < 0.028f) return; lastTick = Time.unscaledTime; }
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
                float fade = Mathf.Min(1f, (n - i) / (Rate * 0.012f));
                d[i] = Mathf.Clamp(f(t, i) * fade, -1f, 1f);
            }
            var c = AudioClip.Create("sm-" + name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
        static float Ping(float t, float hz, float decay) => t < 0 ? 0 : Mathf.Sin(2 * Mathf.PI * hz * t) * Mathf.Exp(-t * decay) * Mathf.Min(1f, t / 0.0016f);
        static float Env(float t, float len) => t < 0 || t > len ? 0 : (1f - t / len) * Mathf.Min(1f, t / 0.002f);
        static float Hiss(System.Random r) => (float)r.NextDouble() * 2f - 1f;
        static float Sq(float t, float hz) => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * hz * t));
        static float lp;
        static float Lp(System.Random r, float k) { lp += (Hiss(r) - lp) * k; return lp; }
    }
}
