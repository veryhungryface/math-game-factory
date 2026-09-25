// MGF Unity kit — 연출 도우미(펀치 스케일·카메라 흔들림·파티클 폭발). 파티클은 시스템 1개를 재사용(Emit) —
// 매번 새 GameObject/ParticleSystem 을 만들면 QA perf.fpsdecay(15초 방치 후 저하율)에서 떨어진다.
using System.Collections;
using UnityEngine;

namespace Mgf
{
    public static class MgfFx
    {
        class Runner : MonoBehaviour { }
        static Runner runner;
        static ParticleSystem burst, glow;

        static Runner R
        {
            get
            {
                if (runner) return runner;
                var go = new GameObject("MgfFx");
                Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
                return runner;
            }
        }

        /// <summary>대상의 크기를 잠깐 키웠다 되돌린다(정답·탭 피드백).</summary>
        public static void Punch(Transform t, float amount = 0.18f, float duration = 0.28f)
        {
            if (t) R.StartCoroutine(PunchCo(t, amount, duration));
        }

        static IEnumerator PunchCo(Transform t, float amount, float duration)
        {
            var baseScale = t.localScale;
            for (float e = 0; e < duration && t; e += Time.deltaTime)
            {
                float k = e / duration;
                t.localScale = baseScale * (1f + amount * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f));
                yield return null;
            }
            if (t) t.localScale = baseScale;
        }

        /// <summary>카메라 흔들림(오답 피드백).</summary>
        public static void Shake(Camera cam, float amplitude = 0.12f, float duration = 0.3f)
        {
            if (cam) R.StartCoroutine(ShakeCo(cam.transform, amplitude, duration));
        }

        static IEnumerator ShakeCo(Transform t, float amp, float duration)
        {
            var basePos = t.localPosition;
            for (float e = 0; e < duration && t; e += Time.deltaTime)
            {
                float k = 1f - e / duration;
                t.localPosition = basePos + Random.insideUnitSphere * amp * k;
                yield return null;
            }
            if (t) t.localPosition = basePos;
        }

        static ParticleSystem MakeSystem(string name, Material mat, float gravity)
        {
            var go = new GameObject(name);
            Object.DontDestroyOnLoad(go);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0.2f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            return ps;
        }

        /// <summary>색 조각 폭발(정답 등). 시스템 1개 재사용.</summary>
        public static void Burst(Vector3 pos, Color color, int count = 28, float speed = 4f, float size = 0.22f)
        {
            if (!burst) burst = MakeSystem("MgfBurst", MgfLook.Alpha(Color.white, MgfLook.SoftDot), 0.9f);
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                p.position = pos;
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 1.2f + 0.2f;
                p.velocity = d * speed * Random.Range(0.5f, 1.1f);
                p.startColor = Color.Lerp(color, Color.white, Random.Range(0f, 0.35f));
                p.startSize = size * Random.Range(0.6f, 1.3f);
                p.startLifetime = Random.Range(0.5f, 0.9f);
                burst.Emit(p, 1);
            }
        }

        /// <summary>빛나는 가산 반짝임(느리게 떠오름).</summary>
        public static void Glow(Vector3 pos, Color color, int count = 12, float size = 0.5f)
        {
            if (!glow) glow = MakeSystem("MgfGlow", MgfLook.Additive(Color.white, MgfLook.SoftDot), -0.05f);
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                p.position = pos + Random.insideUnitSphere * 0.4f;
                p.velocity = Vector3.up * Random.Range(0.3f, 1.2f) + Random.insideUnitSphere * 0.3f;
                p.startColor = color;
                p.startSize = size * Random.Range(0.6f, 1.4f);
                p.startLifetime = Random.Range(0.6f, 1.2f);
                glow.Emit(p, 1);
            }
        }
    }
}
