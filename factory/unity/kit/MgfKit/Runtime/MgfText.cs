// MGF Unity kit — 한국어 TextMeshPro. 킷 폰트(NotoSansKR Bold 서브셋: KS X 1001 한글 2350자 + ASCII + 수학 기호, OFL)를
// 런타임 동적 SDF 폰트 에셋으로 만든다. 서브셋에 없는 글자는 □ 로 나온다 — 희귀 글자가 필요하면 docs/unity-track.md 참조.
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Mgf
{
    public static class MgfText
    {
        static TMP_FontAsset font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font) return font;
                // 워크스페이스 설정이 킷 한글 폰트를 TMP 기본 폰트(동적 SDF)로 지정해 둔다(MgfSetup.EnsureTmpFont).
                font = TMP_Settings.defaultFontAsset;
                if (font && font.name.StartsWith("MgfKR")) { font.TryAddCharacters("0123456789+-×÷=?.,/()%<>!:;·−≤≥≠□○△ "); return font; }
                var src = Resources.Load<Font>("MgfKit/Fonts/MgfKR-Bold");
                if (!src) { Debug.LogError("[MGF] 킷 폰트 없음"); return null; }
                font = TMP_FontAsset.CreateFontAsset(src, 72, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                font.name = "MgfKR";
                font.TryAddCharacters("0123456789+-×÷=?.,/()%<>!:;·−≤≥≠□○△ ");
                return font;
            }
        }

        /// <summary>처음 쓰는 글자는 그 프레임에 SDF 를 굽느라 잠깐 멈춘다. 부팅 때 미리 구워 둔다.</summary>
        public static void Prewarm(string chars)
        {
            if (Font) Font.TryAddCharacters(chars);
        }

        /// <summary>3D 월드 텍스트(카메라를 향하게 하려면 게임이 회전시켜라).</summary>
        public static TextMeshPro World(string text, Vector3 pos, float size, Color color, Transform parent = null)
        {
            var go = new GameObject("Text");
            if (parent) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var t = go.AddComponent<TextMeshPro>();
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = text;
            return t;
        }

        static Canvas canvas;

        /// <summary>화면 UI 캔버스(Screen Space Overlay, 390×844 기준 스케일). 1개만 만든다.</summary>
        public static Canvas Canvas
        {
            get
            {
                if (canvas) return canvas;
                var go = new GameObject("MgfCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(390, 844);
                scaler.matchWidthOrHeight = 0.5f;
                return canvas;
            }
        }

        /// <summary>화면 텍스트. anchor = 화면 비율 좌표(0~1), offset = 기준 해상도 px.</summary>
        public static TextMeshProUGUI Ui(string text, Vector2 anchor, Vector2 offset, float size, Color color, float width = 360f)
        {
            var go = new GameObject("UiText", typeof(RectTransform));
            go.transform.SetParent(Canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(width, size * 1.6f);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }
    }
}
