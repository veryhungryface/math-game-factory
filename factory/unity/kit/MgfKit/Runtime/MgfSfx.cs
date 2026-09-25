// MGF Unity kit — 합성 효과음(에셋 불필요). 오디오는 사용자 제스처 뒤에만 실제로 난다(브라우저 정책 — Unity 가 첫 입력 때 AudioContext 를 깨운다).
// 음소거는 HTML 버튼 → MgfBridge 가 AudioListener.volume 으로 처리한다.
using System.Collections.Generic;
using UnityEngine;

namespace Mgf
{
    public static class MgfSfx
    {
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        const int Rate = 44100;

        /// <summary>kind: tap | correct | wrong | win | lose | whoosh | pop</summary>
        public static void Play(string kind, float volume = 0.55f)
        {
            if (!src)
            {
                var go = new GameObject("MgfSfx");
                Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
            }
            if (!clips.TryGetValue(kind, out var clip)) clips[kind] = clip = Make(kind);
            if (clip) src.PlayOneShot(clip, volume);
        }

        static AudioClip Make(string kind)
        {
            switch (kind)
            {
                case "tap": return Tone(kind, new[] { 880f }, 0.06f, 0.0f);
                case "pop": return Tone(kind, new[] { 620f, 980f }, 0.09f, 0.0f);
                case "correct": return Tone(kind, new[] { 660f, 880f, 1320f }, 0.09f, 0.02f);
                case "wrong": return Tone(kind, new[] { 220f, 165f }, 0.16f, 0.0f, true);
                case "win": return Tone(kind, new[] { 523f, 659f, 784f, 1047f }, 0.12f, 0.02f);
                case "lose": return Tone(kind, new[] { 392f, 311f, 247f }, 0.18f, 0.02f, true);
                case "whoosh": return Noise(kind, 0.25f);
                default: return null;
            }
        }

        static AudioClip Tone(string name, float[] notes, float each, float gap, bool square = false)
        {
            int per = (int)(Rate * (each + gap));
            var data = new float[per * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                int len = (int)(Rate * each);
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Min(1f, i / (Rate * 0.004f)) * Mathf.Exp(-t * 18f / each * 0.25f) * (1f - (float)i / len);
                    float ph = 2f * Mathf.PI * notes[n] * t;
                    float w = square ? Mathf.Sign(Mathf.Sin(ph)) * 0.45f : Mathf.Sin(ph) + 0.25f * Mathf.Sin(ph * 2f);
                    data[n * per + i] = w * env * 0.5f;
                }
            }
            var c = AudioClip.Create("mgf-" + name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Noise(string name, float dur)
        {
            int len = (int)(Rate * dur);
            var data = new float[len];
            var rng = new System.Random(7);
            float lp = 0;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float env = Mathf.Sin(Mathf.PI * t);
                float k = Mathf.Lerp(0.05f, 0.4f, t);
                lp += ((float)rng.NextDouble() * 2f - 1f - lp) * k;
                data[i] = lp * env * 0.6f;
            }
            var c = AudioClip.Create("mgf-" + name, len, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
