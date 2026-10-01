// 석회 착지 — 합성 효과음(외부 음원 없음). 부팅 때 1회 굽는다.
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.SeokhoeChakji
{
    public static class SeokhoeSound
    {
        const int Rate = 44100;
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static float lastTick;

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f;
            var r = new System.Random(23);
            clips["hop"] = Make("hop", 0.09f, (t, i) => Ping(t, 420f, 18f) * 0.45f + Hiss(r) * Env(t, 0.012f) * 0.2f);
            clips["land"] = Make("land", 0.22f, (t, i) => Mathf.Sin(2 * Mathf.PI * (90f + 40f * Mathf.Exp(-t * 18f)) * t) * Mathf.Exp(-t * 10f) * 0.85f + Hiss(r) * Env(t, 0.03f) * 0.25f);
            clips["stamp"] = Make("stamp", 0.45f, (t, i) => Ping(t, 523f, 8f) * 0.4f + Ping(t - 0.06f, 784f, 9f) * 0.32f + Hiss(r) * Env(t, 0.04f) * 0.18f);
            clips["smear"] = Make("smear", 0.38f, (t, i) => Lp(r, 0.28f) * Env(t, 0.36f) * 0.5f + Ping(t, 180f, 9f) * 0.18f);
            clips["card"] = Make("card", 0.28f, (t, i) => Lp(r, 0.2f) * Mathf.Sin(Mathf.PI * t / 0.28f) * 0.55f + Ping(t, 240f, 12f) * 0.12f);
            clips["refuse"] = Make("refuse", 0.2f, (t, i) => Sq(t, t < 0.08f ? 310f : 240f) * Env(t, 0.18f) * 0.22f);
            clips["ripple"] = Make("ripple", 0.08f, (t, i) => Mathf.Sin(2 * Mathf.PI * 640f * t) * Env(t, 0.07f) * 0.18f);
            clips["tick"] = Make("tick", 0.03f, (t, i) => Ping(t, 2100f, 90f) * 0.28f);
            clips["whistle"] = Make("whistle", 0.7f, (t, i) => Ping(t, 1480f, 4f) * 0.28f + Ping(t - 0.18f, 1320f, 5f) * 0.22f);
            clips["win"] = Make("win", 0.8f, (t, i) => (Ping(t, 523f, 5f) + Ping(t - 0.1f, 659f, 5f) + Ping(t - 0.2f, 784f, 5f) + Ping(t - 0.32f, 1047f, 6f)) * 0.26f);
            clips["end"] = Make("end", 0.7f, (t, i) => (Ping(t, 392f, 5f) + Ping(t - 0.12f, 494f, 5f) + Ping(t - 0.24f, 587f, 5f)) * 0.28f);
            clips["combo"] = Make("combo", 0.32f, (t, i) => (Ping(t, 1568f, 10f) + Ping(t - 0.08f, 2093f, 10f)) * 0.28f);
        }

        public static void Play(string k, float vol = 1f, float pitch = 1f)
        {
            if (!src || !clips.TryGetValue(k, out var c)) return;
            if (k == "tick" || k == "hop")
            {
                if (Time.unscaledTime - lastTick < 0.04f) return;
                lastTick = Time.unscaledTime;
            }
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
            var c = AudioClip.Create("sk-" + name, n, 1, Rate, false);
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
