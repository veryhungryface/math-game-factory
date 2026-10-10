// 만두 묶기 — 합성 효과음(외부 음원 없음). 정답 = 주름 2박 + 철판 셔터 1박 + 집게 포착 1박 + 증기 숨,
// 오답 = 젖은 반죽이 슬롯에 걸리는 둔탁음 + 회수통의 낮은 울림. 사용자 제스처 뒤에만 소리가 난다(브라우저 정책).
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.ManduMukgi
{
    public static class ManduSfx
    {
        const int Rate = 44100;
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static System.Random rng = new System.Random(11);

        public static void Play(string kind, float vol = 0.6f, float pitch = 1f)
        {
            if (!src)
            {
                var go = new GameObject("ManduSfx");
                Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.playOnAwake = false; src.spatialBlend = 0f;
            }
            if (!clips.TryGetValue(kind, out var c)) clips[kind] = c = Make(kind);
            if (!c) return;
            src.pitch = pitch;
            src.PlayOneShot(c, vol);
        }

        public static void Prewarm()
        {
            foreach (var k in new[] { "pleat", "shutter", "tong", "steam", "thud", "crack", "flip", "tick", "refuse", "lift", "drip", "fanfare", "fail" })
                if (!clips.ContainsKey(k)) clips[k] = Make(k);
        }

        static AudioClip Make(string k)
        {
            switch (k)
            {
                case "pleat": return Build(k, 0.12f, (t, i) => Pluck(t, 210f, 26f) * 0.8f + Noise() * Env(t, 0.004f, 30f) * 0.25f);
                case "lift": return Build(k, 0.09f, (t, i) => Pluck(t, 520f, 40f) * 0.4f + Pluck(t, 780f, 50f) * 0.2f);
                case "shutter": return Build(k, 0.32f, (t, i) => Metal(t, 340f, 9f) * 0.55f + Noise() * Env(t, 0.002f, 60f) * 0.35f);
                case "tong": return Build(k, 0.16f, (t, i) => Metal(t, 1850f, 22f) * 0.45f + Pluck(t, 1240f, 35f) * 0.3f);
                case "steam": return Build(k, 0.55f, (t, i) => Hiss(t, 0.55f) * 0.32f);
                case "thud": return Build(k, 0.28f, (t, i) => Pluck(t, 92f, 12f) * 0.9f + Noise() * Env(t, 0.003f, 25f) * 0.3f);
                case "crack": return Build(k, 0.3f, (t, i) => Noise() * Env(t, 0.001f, 18f) * 0.55f + Metal(t, 2600f, 30f) * 0.25f + Pluck(t, 70f, 9f) * 0.5f);
                case "flip": return Build(k, 0.11f, (t, i) => Noise() * Env(t, 0.001f, 70f) * 0.5f + Pluck(t, 900f, 60f) * 0.35f);
                case "tick": return Build(k, 0.05f, (t, i) => Pluck(t, 1500f, 90f) * 0.35f);
                case "refuse": return Build(k, 0.2f, (t, i) => (Pluck(t, 330f, 22f) + Pluck(t - 0.08f, 262f, 22f)) * 0.4f);
                case "drip": return Build(k, 0.09f, (t, i) => Mathf.Sin(2f * Mathf.PI * (1400f - 6000f * t) * t) * Env(t, 0.002f, 45f) * 0.3f);
                case "fanfare": return Build(k, 0.9f, (t, i) => Chime(t, new[] { 523f, 659f, 784f, 1047f }, 0.13f) * 0.5f);
                case "fail": return Build(k, 0.8f, (t, i) => Chime(t, new[] { 392f, 330f, 262f }, 0.2f) * 0.45f);
            }
            return null;
        }

        static float Noise() => (float)rng.NextDouble() * 2f - 1f;
        static float Env(float t, float atk, float decay) => t < 0 ? 0 : Mathf.Min(1f, t / atk) * Mathf.Exp(-t * decay);
        static float Pluck(float t, float f, float decay) => t < 0 ? 0 : Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.003f, decay);
        static float Metal(float t, float f, float decay)
        {
            if (t < 0) return 0;
            float e = Env(t, 0.001f, decay);
            return (Mathf.Sin(2f * Mathf.PI * f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 5.4f * t)) * e * 0.5f;
        }
        static float hissLp;
        static float Hiss(float t, float dur)
        {
            hissLp += (Noise() - hissLp) * 0.35f;
            float e = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur));
            return hissLp * e;
        }
        static float Chime(float t, float[] notes, float step)
        {
            float s = 0;
            for (int n = 0; n < notes.Length; n++) s += Pluck(t - n * step, notes[n], 5f) * 0.6f + Pluck(t - n * step, notes[n] * 2f, 9f) * 0.15f;
            return s;
        }

        static AudioClip Build(string name, float dur, System.Func<float, int, float> f)
        {
            int len = (int)(Rate * dur);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / Rate;
                float fade = Mathf.Clamp01((len - i) / (Rate * 0.01f));
                data[i] = Mathf.Clamp(f(t, i), -1f, 1f) * fade;
            }
            var c = AudioClip.Create("mandu-" + name, len, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
