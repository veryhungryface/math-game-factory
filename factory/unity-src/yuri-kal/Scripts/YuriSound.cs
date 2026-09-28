// 유리칼 — 합성 효과음(외부 음원 없음). 부팅 때 1회 굽는다. 오디오는 첫 사용자 제스처 뒤에만 난다(킷·브라우저 정책).
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.YuriKal
{
    public static class YuriSound
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
            clips["tick"] = Make("tick", 0.03f, (t, i) => Ping(t, 3400f, 180f) * 0.35f + Hiss(r) * Env(t, 0.004f) * 0.25f);
            clips["lock"] = Make("lock", 0.12f, (t, i) => (Ping(t, 2300f, 60f) + Ping(t - 0.045f, 3100f, 70f)) * 0.45f);
            clips["snap"] = Make("snap", 0.55f, (t, i) =>
                Hiss(r) * Env(t, 0.03f) * 0.8f + (Ping(t - 0.01f, 4180f, 9f) * 0.35f + Ping(t - 0.01f, 5230f, 12f) * 0.25f + Ping(t - 0.02f, 2890f, 7f) * 0.3f));
            clips["slide"] = Make("slide", 0.35f, (t, i) => Lp(r, 0.12f) * Mathf.Sin(Mathf.PI * t / 0.35f) * 0.5f);
            clips["clunk"] = Make("clunk", 0.35f, (t, i) => Mathf.Sin(2 * Mathf.PI * 92f * t) * Mathf.Exp(-t * 14f) * 0.8f + Lp(r, 0.3f) * Env(t, 0.05f) * 0.6f);
            clips["frost"] = Make("frost", 0.5f, (t, i) =>
                Mathf.Sin(2 * Mathf.PI * 70f * t) * Mathf.Exp(-t * 10f) * 0.7f + (r.NextDouble() < 0.012 * Mathf.Exp(-t * 5f) ? 0.9f : 0f) + Hiss(r) * Env(t, 0.08f) * 0.2f);
            clips["refuse"] = Make("refuse", 0.22f, (t, i) => Sq(t, t < 0.09f ? 330f : 262f) * (t < 0.09f || t > 0.12f ? 1f : 0f) * Mathf.Exp(-t * 6f) * 0.28f);
            clips["caliper"] = Make("caliper", 0.16f, (t, i) => (Ping(t, 2600f, 90f) + Ping(t - 0.05f, 2600f, 90f) + Ping(t - 0.1f, 3000f, 70f)) * 0.3f);
            clips["blip"] = Make("blip", 0.05f, (t, i) => Mathf.Sin(2 * Mathf.PI * 1320f * t) * Env(t, 0.04f) * 0.3f);
            clips["press"] = Make("press", 0.14f, (t, i) => Mathf.Sin(2 * Mathf.PI * (520f + 400f * t / 0.14f) * t) * Env(t, 0.13f) * 0.4f);
            clips["rot"] = Make("rot", 0.03f, (t, i) => Ping(t, 1900f, 200f) * 0.3f);
            clips["ripple"] = Make("ripple", 0.08f, (t, i) => Mathf.Sin(2 * Mathf.PI * 700f * t) * Env(t, 0.07f) * 0.18f);
            clips["combo"] = Make("combo", 0.35f, (t, i) => (Ping(t, 1568f, 10f) + Ping(t - 0.08f, 2093f, 10f)) * 0.3f);
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
            var c = AudioClip.Create("yk-" + name, n, 1, Rate, false);
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
