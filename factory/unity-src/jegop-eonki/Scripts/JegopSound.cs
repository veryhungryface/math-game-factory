// 제곱 얹기 — 합성 효과음(외부 음원 없음). 부팅 때 1회 굽는다.
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.JegopEonki
{
    public static class JegopSound
    {
        const int Rate = 44100;
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static float lastTick;

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            var r = new System.Random(23);
            clips["pick"] = Make("pick", 0.10f, (t, i) => Ping(t, 620f, 22f) * 0.35f + Lp(r, 0.2f) * Env(t, 0.06f) * 0.2f);
            clips["place"] = Make("place", 0.16f, (t, i) =>
                Mathf.Sin(2 * Mathf.PI * 140f * t) * Mathf.Exp(-t * 14f) * 0.7f + Ping(t, 880f, 30f) * 0.2f);
            clips["latch"] = Make("latch", 0.22f, (t, i) =>
                Ping(t, 190f, 10f) * 0.8f + Ping(t, 980f, 28f) * 0.25f + Lp(r, 0.18f) * Env(t, 0.04f) * 0.25f);
            clips["kiln"] = Make("kiln", 0.55f, (t, i) =>
                Ping(t, 110f, 6f) * 0.7f + Ping(t, 220f, 8f) * 0.3f + Lp(r, 0.08f) * Mathf.Sin(Mathf.PI * t / 0.55f) * 0.35f);
            clips["slide"] = Make("slide", 0.42f, (t, i) => Lp(r, 0.16f) * Mathf.Sin(Mathf.PI * t / 0.42f) * 0.55f);
            clips["crack"] = Make("crack", 0.24f, (t, i) =>
                Hiss(r) * Env(t, 0.05f) * 0.5f + Ping(t, 310f, 16f) * 0.4f + Ping(t - 0.04f, 180f, 12f) * 0.3f);
            clips["impale"] = Make("impale", 0.22f, (t, i) => Ping(t, 170f, 11f) * 0.75f + Hiss(r) * Env(t, 0.04f) * 0.25f);
            clips["refuse"] = Make("refuse", 0.18f, (t, i) => Sq(t, t < 0.07f ? 280f : 220f) * (t < 0.07f || t > 0.10f ? 1f : 0f) * Mathf.Exp(-t * 8f) * 0.24f);
            clips["ripple"] = Make("ripple", 0.10f, (t, i) => Mathf.Sin(2 * Mathf.PI * 540f * t) * Env(t, 0.09f) * 0.18f);
            clips["stamp"] = Make("stamp", 0.18f, (t, i) => Ping(t, 240f, 14f) * 0.6f + Ping(t, 720f, 22f) * 0.25f);
            clips["counter"] = Make("counter", 0.07f, (t, i) => Ping(t, 1040f, 70f) * 0.22f);
            clips["combo"] = Make("combo", 0.30f, (t, i) => (Ping(t, 784f, 10f) + Ping(t - 0.07f, 988f, 10f) + Ping(t - 0.14f, 1175f, 9f)) * 0.26f);
            clips["win"] = Make("win", 0.70f, (t, i) => (Ping(t, 392f, 5f) + Ping(t - 0.10f, 523f, 5f) + Ping(t - 0.20f, 659f, 5f) + Ping(t - 0.34f, 784f, 4f)) * 0.32f);
            clips["lose"] = Make("lose", 0.55f, (t, i) => (Ping(t, 330f, 6f) + Ping(t - 0.12f, 262f, 6f) + Ping(t - 0.24f, 196f, 5f)) * 0.34f);
            clips["press"] = Make("press", 0.14f, (t, i) => Mathf.Sin(2 * Mathf.PI * (420f + 480f * t / 0.14f) * t) * Env(t, 0.13f) * 0.4f);
        }

        public static void Play(string k, float vol = 1f)
        {
            if (!src || !clips.TryGetValue(k, out var c)) return;
            if (k == "counter") { if (Time.unscaledTime - lastTick < 0.04f) return; lastTick = Time.unscaledTime; }
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
            var c = AudioClip.Create("je-" + name, n, 1, Rate, false);
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
