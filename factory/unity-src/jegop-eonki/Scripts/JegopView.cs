// 제곱 얹기 — 월드. 정오 기와골 마른 진흙 작업대 + 삼나무 삼각 지그 + 테라코타 기왓장.
// 최빈 픽셀은 #E6C9A8 점토(부팅 1회 베이크한 결·모래). 회화 배경 없음.
// 정답: 나무못이 먹줄을 따라 들어가며 쟁반이 눌렸다 탄성 복귀, 넓이 숫자 스프링.
// 오답: 기왓장에 금, 지그 한 번 비틀림, 쇠꽂이에 금 간 기와. 파티클 버스트·점수 팝업·화면 흔들림 없음.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.JegopEonki
{
    public partial class JegopEonkiGame
    {
        static readonly Color Clay = Hex("E6C9A8"), Terra = Hex("C65A3A"), Cedar = Hex("6B3A28");
        static readonly Color InkC = Hex("2F4A3C"), Cream = Hex("F3E6D0"), Brass = Hex("C4A15A"), Sand = Hex("D4A574");
        static Color Hex(string h) => MgfLook.Hex(h);
        static Color32 C32(Color c, float a = 1f)
        {
            var l = c.linear;
            return new Color32((byte)(l.r * 255), (byte)(l.g * 255), (byte)(l.b * 255), (byte)(Mathf.Clamp01(a) * 255));
        }

        Camera cam;
        Transform world, jig, spikeT, troughT, fingerT, ghostRing;
        readonly Transform[] trayT = new Transform[3];
        readonly Transform[] latchT = new Transform[3];
        readonly Transform[] stampTform = new Transform[3];
        readonly TextMeshPro[] sideLbl = new TextMeshPro[3];
        readonly TextMeshPro[] traySLbl = new TextMeshPro[3];
        readonly Transform[] crackedPile = new Transform[3];
        TextMeshPro countLbl, hypLbl;
        Transform rightMark, ghostSq;
        Material matClay, matWood, matTile, matInk, matBrass, matIron, matAlpha, matCrack;
        Texture2D clayTex, woodTex, tileTex;
        Vector3 A, B, C, centroid;
        Vector3[] trayPos = new Vector3[3];
        Vector3[] trayFwd = new Vector3[3];
        Vector3 camHome, lookHome;
        readonly Vector3[] latchBase = new Vector3[3];
        readonly Vector3[] trayBaseScale = new Vector3[3];
        int hoverTray = -1;
        int impaled;
        LineRenderer trailLr;
        float ambientT;
        MeshFilter ghostMf;
        static readonly float TraySize = 1.72f;

        static void NoShadow(GameObject o)
        {
            var r = o.GetComponent<Renderer>();
            if (r) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
        }

        Texture2D BakeClay()
        {
            const int S = 256;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Clay", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[S * S];
            var rng = new System.Random(11);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float g = 0.92f + 0.06f * Mathf.Sin(x * 0.11f) * Mathf.Sin(y * 0.09f) + 0.03f * Mathf.Sin((x + y) * 0.37f);
                g += (rng.Next(9) - 4) / 255f;
                byte r = (byte)Mathf.Clamp(230 * g, 0, 255);
                byte gg = (byte)Mathf.Clamp(201 * g, 0, 255);
                byte b = (byte)Mathf.Clamp(168 * g, 0, 255);
                if (rng.Next(40) == 0) { r = (byte)Mathf.Min(255, r + 12); gg = (byte)Mathf.Min(255, gg + 6); }
                px[y * S + x] = new Color32(r, gg, b, 255);
            }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        Texture2D BakeWood()
        {
            const int S = 256;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Cedar", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            var rng = new System.Random(5);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float g = 0.62f + 0.12f * Mathf.Sin((x + y * 0.07f) * 0.19f) + 0.04f * Mathf.Sin(y * 0.33f);
                g += (rng.Next(7) - 3) / 255f;
                byte r = (byte)Mathf.Clamp(140 * g, 0, 255);
                byte gg = (byte)Mathf.Clamp(72 * g, 0, 255);
                byte b = (byte)Mathf.Clamp(48 * g, 0, 255);
                px[y * S + x] = new Color32(r, gg, b, 255);
            }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        Texture2D BakeTile()
        {
            const int S = 128;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Terra", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            var rng = new System.Random(9);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float groove = (x % 16 < 2 || y % 16 < 2) ? 0.82f : 1f;
                float g = groove * (0.95f + (rng.Next(8) - 4) / 255f);
                byte r = (byte)Mathf.Clamp(198 * g, 0, 255);
                byte gg = (byte)Mathf.Clamp(90 * g, 0, 255);
                byte b = (byte)Mathf.Clamp(58 * g, 0, 255);
                px[y * S + x] = new Color32(r, gg, b, 255);
            }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        GameObject MakeTri(string name, Vector3 a, Vector3 b, Vector3 c, float y, float thick, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            var top = new[] { new Vector3(a.x, y, a.z), new Vector3(b.x, y, b.z), new Vector3(c.x, y, c.z) };
            var bot = new[] { new Vector3(a.x, y - thick, a.z), new Vector3(b.x, y - thick, b.z), new Vector3(c.x, y - thick, c.z) };
            var v = new List<Vector3>(18);
            var n = new List<Vector3>(18);
            var u = new List<Vector2>(18);
            var tri = new List<int>(24);
            void Face(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 nrm)
            {
                int i = v.Count;
                v.Add(p0); v.Add(p1); v.Add(p2); n.Add(nrm); n.Add(nrm); n.Add(nrm);
                u.Add(new Vector2(0.5f, 1)); u.Add(Vector2.zero); u.Add(Vector2.one);
                tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
            }
            Face(top[0], top[2], top[1], Vector3.up);
            Face(bot[0], bot[1], bot[2], Vector3.down);
            Face(top[0], top[1], bot[1], Vector3.Cross(top[1] - top[0], bot[0] - top[0]).normalized);
            Face(top[0], bot[1], bot[0], Vector3.Cross(top[1] - top[0], bot[0] - top[0]).normalized);
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, u); mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
            var col = go.AddComponent<MeshCollider>(); col.sharedMesh = mesh;
            return go;
        }

        Mesh GridMesh(int cells, bool square)
        {
            int cols, rows;
            if (square)
            {
                int side = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(cells)));
                cols = side; rows = side;
            }
            else
            {
                cols = Mathf.Max(2, Mathf.CeilToInt(Mathf.Sqrt(cells * 1.4f)));
                rows = Mathf.Max(1, Mathf.CeilToInt(cells / (float)cols));
            }
            float size = 1.28f;
            float gap = 0.04f;
            float cw = (size - gap * (cols + 1)) / cols;
            float ch = (size - gap * (rows + 1)) / rows;
            var v = new List<Vector3>(cells * 4);
            var n = new List<Vector3>(cells * 4);
            var u = new List<Vector2>(cells * 4);
            var t = new List<int>(cells * 6);
            int k = 0;
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (k >= cells) break;
                float x0 = -size * 0.5f + gap + c * (cw + gap);
                float z0 = -size * 0.5f + gap + r * (ch + gap);
                int i = v.Count;
                v.Add(new Vector3(x0, 0.03f, z0));
                v.Add(new Vector3(x0 + cw, 0.03f, z0));
                v.Add(new Vector3(x0 + cw, 0.03f, z0 + ch));
                v.Add(new Vector3(x0, 0.03f, z0 + ch));
                n.Add(Vector3.up); n.Add(Vector3.up); n.Add(Vector3.up); n.Add(Vector3.up);
                u.Add(new Vector2(0, 0)); u.Add(new Vector2(1, 0)); u.Add(new Vector2(1, 1)); u.Add(new Vector2(0, 1));
                t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
                k++;
            }
            var mesh = new Mesh { name = "Grid" + cells };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, u); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        void BuildWorld()
        {
            MgfLook.Quality(28f);
            MgfLook.Sky(Hex("C8D6E0"), Hex("E8D5B8"), Hex("C4A882"), 0.95f);
            MgfLook.Sun(new Vector3(52, -28, 0), Hex("FFF1D0"), 1.18f, 0.55f);
            clayTex = BakeClay(); woodTex = BakeWood(); tileTex = BakeTile();
            matClay = MgfLook.Lit(Clay, 0.12f, 0f, null, clayTex);
            matWood = MgfLook.Lit(Cedar, 0.28f, 0.02f, null, woodTex);
            matTile = MgfLook.Lit(Terra, 0.22f, 0f, null, tileTex);
            matInk = MgfLook.Unlit(InkC);
            matBrass = MgfLook.Lit(Brass, 0.65f, 0.35f, Hex("3A2A10"));
            matIron = MgfLook.Lit(Hex("3A3A3E"), 0.4f, 0.5f);
            matAlpha = MgfLook.Alpha(new Color(InkC.r, InkC.g, InkC.b, 0.45f));
            matCrack = MgfLook.Lit(Hex("8A4A3A"), 0.15f, 0f);

            world = new GameObject("World").transform;
            MgfLook.Block("Bench", new Vector3(0, -0.22f, 0.4f), new Vector3(16f, 0.44f, 12f), 0.08f, matClay, world);
            MgfLook.Block("BenchLip", new Vector3(0, 0.05f, 5.8f), new Vector3(16f, 0.12f, 0.35f), 0.04f, matWood, world);

            jig = new GameObject("Jig").transform;
            jig.SetParent(world, false);

            for (int i = 0; i < 3; i++)
            {
                trayT[i] = MgfLook.Block("Tray" + i, Vector3.zero, new Vector3(TraySize, 0.10f, TraySize), 0.04f, matWood, jig).transform;
                trayBaseScale[i] = trayT[i].localScale;
                latchT[i] = MgfLook.Block("Latch" + i, new Vector3(0, 0.42f, TraySize * 0.56f), new Vector3(0.28f, 0.46f, 0.28f), 0.04f, matBrass, trayT[i]).transform;
                latchBase[i] = latchT[i].localPosition;
                MgfLook.Block("Peg" + i, new Vector3(0, 0.28f, 0), new Vector3(0.16f, 0.16f, 0.16f), 0.05f, matWood, latchT[i]);
                sideLbl[i] = MgfText.World("", new Vector3(0, 0.22f, 0), 3.2f, Cream, trayT[i]);
                sideLbl[i].fontStyle = FontStyles.Bold;
                traySLbl[i] = MgfText.World("", new Vector3(0, 0.8f, -0.15f), 4.4f, Cream, trayT[i]);
                traySLbl[i].fontStyle = FontStyles.Bold;
                stampTform[i] = MgfLook.Block("Stamp" + i, Vector3.zero, new Vector3(0.55f, 0.08f, 0.55f), 0.12f, matBrass, jig).transform;
                stampTform[i].gameObject.SetActive(false);
            }

            rightMark = new GameObject("Right").transform;
            rightMark.SetParent(jig, false);
            MgfLook.Block("SqH", new Vector3(0.18f, 0.12f, 0), new Vector3(0.36f, 0.04f, 0.05f), 0.01f, matInk, rightMark);
            MgfLook.Block("SqV", new Vector3(0, 0.12f, 0.18f), new Vector3(0.05f, 0.04f, 0.36f), 0.01f, matInk, rightMark);

            hypLbl = MgfText.World("", new Vector3(0, 0.5f, 0), 3.6f, InkC, jig);
            hypLbl.fontStyle = FontStyles.Bold;
            hypLbl.gameObject.SetActive(false);

            countLbl = MgfText.World("", new Vector3(0, 1.1f, 0), 6.5f, InkC, jig);
            countLbl.fontStyle = FontStyles.Bold;

            troughT = MgfLook.Block("Discard", new Vector3(-5.2f, 0.12f, -1.6f), new Vector3(1.6f, 0.28f, 3.2f), 0.08f, matIron, world).transform;
            MgfLook.Block("TroughLip", new Vector3(0, 0.18f, 0), new Vector3(1.7f, 0.08f, 3.3f), 0.04f, matWood, troughT);
            var troughCap = MgfText.World("폐기", new Vector3(0, 0.42f, 1.3f), 3.0f, Cream, troughT);
            troughCap.fontStyle = FontStyles.Bold;

            spikeT = new GameObject("Spike").transform;
            spikeT.SetParent(world, false);
            spikeT.position = new Vector3(-5.3f, 0.1f, 2.2f);
            MgfLook.Block("Rod", new Vector3(0, 0.9f, 0), new Vector3(0.14f, 1.8f, 0.14f), 0.04f, matIron, spikeT);
            MgfLook.Block("Base", new Vector3(0, 0.08f, 0), new Vector3(0.5f, 0.16f, 0.5f), 0.04f, matIron, spikeT);
            for (int i = 0; i < 3; i++)
            {
                crackedPile[i] = MakeMiniTile("Crack" + i, spikeT).transform;
                crackedPile[i].gameObject.SetActive(false);
            }

            ghostSq = MgfLook.Block("GhostSq", Vector3.zero, new Vector3(TraySize * 0.92f, 0.02f, TraySize * 0.92f), 0.02f, MgfLook.Alpha(new Color(InkC.r, InkC.g, InkC.b, 0.22f)), jig).transform;
            ghostSq.gameObject.SetActive(false);

            fingerT = new GameObject("Finger").transform;
            fingerT.SetParent(world, false);
            MgfLook.Block("Pad", new Vector3(0, 0.15f, 0), new Vector3(0.34f, 0.22f, 0.34f), 0.1f, MgfLook.Lit(Hex("E8C8A8"), 0.4f), fingerT);
            MgfLook.Block("Stick", new Vector3(0.05f, 0.42f, -0.05f), new Vector3(0.14f, 0.5f, 0.14f), 0.05f, MgfLook.Lit(Hex("E8C8A8"), 0.4f), fingerT);
            fingerT.gameObject.SetActive(false);

            ghostRing = MgfLook.Block("Ring", Vector3.zero, new Vector3(1.1f, 0.04f, 1.1f), 0.4f, MgfLook.Alpha(new Color(Brass.r, Brass.g, Brass.b, 0.55f)), world).transform;
            ghostRing.gameObject.SetActive(false);

            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(world, false);
            trailLr = trailGo.AddComponent<LineRenderer>();
            trailLr.material = matAlpha;
            trailLr.widthMultiplier = 0.06f;
            trailLr.positionCount = 8;
            trailLr.useWorldSpace = true;
            trailLr.shadowCastingMode = ShadowCastingMode.Off;
            trailLr.enabled = false;

            for (int i = 0; i < 3; i++)
            {
                var tile = new Tile();
                tile.root = MakeTileRoot("Tile" + i, world);
                tile.label = tile.root.GetComponentInChildren<TextMeshPro>();
                tile.grid = tile.root.Find("Grid");
                tiles[i] = tile;
            }

            lookHome = new Vector3(0.2f, 0.2f, 0.6f);
            camHome = new Vector3(0.2f, 11.2f, -8.4f);
            cam = MgfLook.Camera(camHome, lookHome, 36f);
            FrameCam();
        }

        Transform MakeTileRoot(string name, Transform parent)
        {
            var root = MgfLook.Block(name, Vector3.zero, new Vector3(1.36f, 0.12f, 1.36f), 0.04f, matTile, parent).transform;
            var grid = new GameObject("Grid");
            grid.transform.SetParent(root, false);
            grid.transform.localPosition = new Vector3(0, 0.07f, 0);
            var mf = grid.AddComponent<MeshFilter>();
            mf.sharedMesh = GridMesh(9, true);
            var mr = grid.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MgfLook.Lit(Hex("B24C32"), 0.18f, 0f, null, tileTex);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            var lab = MgfText.World("9", new Vector3(0, 0.28f, 0), 5.2f, Cream, root);
            lab.fontStyle = FontStyles.Bold;
            lab.outlineWidth = 0.18f;
            lab.outlineColor = new Color32(40, 20, 12, 220);
            return root;
        }

        GameObject MakeMiniTile(string name, Transform parent)
        {
            var o = MgfLook.Block(name, Vector3.zero, new Vector3(0.55f, 0.07f, 0.55f), 0.03f, matCrack, parent);
            return o;
        }

        void LayoutJig(Sheet s)
        {
            const float us = 0.82f;
            float aa = 4f * us, bb = 3f * us; // visual 3-4-5, 숫자는 라벨
            C = Vector3.zero;
            A = new Vector3(aa, 0, 0);
            B = new Vector3(0, 0, bb);
            centroid = (A + B + C) / 3f;
            jig.position = land ? new Vector3(0.45f, 0.08f, 0.45f) : new Vector3(0.40f, 0.08f, 0.80f);
            jig.rotation = Quaternion.Euler(0, s.rot, 0);

            var tri = jig.Find("TriBody");
            if (tri) Destroy(tri.gameObject);
            MakeTri("TriBody", A - centroid, B - centroid, C - centroid, 0.16f, 0.22f, matWood, jig);

            Vector3[] mid =
            {
                (B + C) * 0.5f - centroid, // a = BC
                (A + C) * 0.5f - centroid, // b = AC
                (A + B) * 0.5f - centroid  // c = AB
            };
            Vector3[] outw =
            {
                Vector3.Cross(Vector3.up, B - C).normalized,
                Vector3.Cross(Vector3.up, C - A).normalized,
                Vector3.Cross(Vector3.up, A - B).normalized
            };
            // 바깥쪽인지 확인: 중점에서 바깥으로 나가야 함 (centroid 반대)
            for (int i = 0; i < 3; i++)
            {
                Vector3 fromC = mid[i];
                if (Vector3.Dot(outw[i], fromC) < 0) outw[i] = -outw[i];
                trayPos[i] = mid[i] + outw[i] * (TraySize * 0.72f);
                trayFwd[i] = outw[i];
                trayT[i].localPosition = trayPos[i] + Vector3.up * 0.12f;
                trayT[i].localRotation = Quaternion.LookRotation(outw[i], Vector3.up);
                latchT[i].localPosition = latchBase[i];
            }
            rightMark.localPosition = (C - centroid) + new Vector3(0.28f, 0.02f, 0.28f);
            rightMark.gameObject.SetActive(s.showRight);
            Vector3[] vtx = { A - centroid, B - centroid, C - centroid };
            for (int i = 0; i < 3; i++)
            {
                stampTform[i].localPosition = vtx[i] + Vector3.up * 0.22f;
                stampTform[i].gameObject.SetActive(s.kind == Kind.Reverse);
            }
            hypLbl.gameObject.SetActive(false);
        }

        void SetupSheet(Sheet s, bool title)
        {
            if (s == null) return;
            LayoutJig(s);
            for (int i = 0; i < 3; i++)
            {
                trayLocked[i] = false;
                trayArea[i] = 0;
                trayT[i].localScale = trayBaseScale[i];
                int n = s.Side(i);
                bool hide = (s.hide == i + 1);
                sideLbl[i].text = hide ? "□" : n + " cm";
                sideLbl[i].transform.localRotation = Quaternion.Euler(90, 180, 0);
                traySLbl[i].text = "";
                traySLbl[i].transform.localRotation = Quaternion.Euler(90, 180, 0);
            }
            for (int i = 0; i < 3; i++)
            {
                var t = tiles[i];
                t.area = s.dock[i];
                t.mis = s.dockMis[i];
                t.onTray = -1;
                t.locked = false;
                t.cracked = false;
                t.squash = 1f;
                DressTile(t);
            }
            LayoutDock();
            countLbl.text = "";
            ghostSq.gameObject.SetActive(false);
            if (title)
            {
                // 타이틀 루프용: 9·16 을 다리에 올려 둔다
                if (tiles[0].area == 9) PlaceOnTray(0, 0, false);
                if (tiles[1].area == 16) PlaceOnTray(1, 1, false);
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (tiles[i].area == 9) PlaceOnTray(i, 0, false);
                        if (tiles[i].area == 16) PlaceOnTray(i, 1, false);
                    }
                }
            }
            RefreshTrayLabels();
            RefreshHypLabel();
        }

        void DressTile(Tile t)
        {
            if (t == null || t.root == null) return;
            bool sq = IsSquare(t.area, out int n);
            var mf = t.grid ? t.grid.GetComponent<MeshFilter>() : null;
            if (mf) mf.sharedMesh = GridMesh(Mathf.Clamp(t.area, 1, 64), sq);
            if (t.label) t.label.text = t.area.ToString();
            var r = t.root.GetComponent<MeshRenderer>();
            if (r) r.sharedMaterial = t.cracked ? matCrack : matTile;
            t.root.gameObject.SetActive(true);
        }

        static bool IsSquare(int x, out int n)
        {
            n = 1;
            while (n * n < x) n++;
            return n * n == x;
        }

        void LayoutDock()
        {
            Vector3[] homes;
            if (land)
            {
                homes = new[]
                {
                    new Vector3(5.55f, 0.22f, 1.4f),
                    new Vector3(5.55f, 0.22f, 0.0f),
                    new Vector3(5.55f, 0.22f, -1.4f)
                };
                troughT.position = new Vector3(-5.5f, 0.12f, -0.6f);
                spikeT.position = new Vector3(-5.5f, 0.1f, 2.3f);
            }
            else
            {
                float dockZ = PortraitSquat() ? -2.85f : -3.25f;
                homes = new[]
                {
                    new Vector3(-0.95f, 0.22f, dockZ),
                    new Vector3(0.65f, 0.22f, dockZ),
                    new Vector3(2.25f, 0.22f, dockZ)
                };
                // 폐기 홈을 지그 왼쪽이 아니라 독 왼쪽으로 — BC 쟁반(3 cm)과 겹치지 않게.
                // 390 세로에서 화면 왼쪽 끝에 잘리지 않게(「폐기」 글자 전부 노출) 안쪽으로 당기고 독을 오른쪽으로 민다.
                troughT.position = new Vector3(-2.75f, 0.12f, dockZ + 0.15f);
                spikeT.position = new Vector3(-3.3f, 0.1f, 1.85f);
            }
            for (int i = 0; i < 3; i++)
            {
                tiles[i].dockPos = homes[i];
                if (tiles[i].onTray < 0) SnapTile(i);
            }
        }

        void SnapTile(int i)
        {
            var t = tiles[i];
            if (t == null || t.root == null) return;
            // 쟁반 위에서는 판 숫자를 숨기고 쟁반의 넓이 합(○ cm²) 하나만 읽히게 한다(두 숫자가 겹치던 문제).
            if (t.label) t.label.enabled = t.onTray < 0;
            Vector3 p;
            if (t.onTray == -2)
                p = troughT.position + Vector3.up * 0.25f + new Vector3(0, 0, (i - 1) * 0.55f);
            else if (t.onTray >= 0)
            {
                Vector3 baseP = jig.TransformPoint(trayPos[t.onTray] + Vector3.up * 0.22f);
                int stack = 0;
                for (int k = 0; k < i; k++) if (tiles[k].onTray == t.onTray) stack++;
                p = baseP + Vector3.up * (0.14f * stack) + jig.TransformDirection(trayFwd[t.onTray]) * (stack * 0.08f);
            }
            else p = t.dockPos;
            if (i == selTile && t.onTray == -1) p += Vector3.up * (0.55f + 0.07f * Mathf.Sin(Time.time * 7f));
            t.root.position = p;
            t.root.rotation = Quaternion.Euler(0, t.onTray >= 0 ? jig.eulerAngles.y : 0, t.cracked ? 8f : 0);
            float s = (i == selTile && t.onTray == -1 ? 1.1f : 1.0f) * t.squash;
            t.root.localScale = new Vector3(s, s * (t.squash < 1 ? 0.85f : 1f), s);
        }

        void RefreshTrayLabels()
        {
            if (cur == null) return;
            for (int i = 0; i < 3; i++)
            {
                int s = trayArea[i];
                int n = cur.Side(i);
                // 판이 얹히면 쟁반 바닥의 변 길이 글자는 판 밑에서 비쳐 보이므로 끈다.
                if (sideLbl[i]) sideLbl[i].enabled = s <= 0;
                if (s > 0)
                {
                    traySLbl[i].text = s + " cm²";
                    bool match = n > 0 && (long)s == (long)n * n && (cur.hide != i + 1);
                    traySLbl[i].color = match ? Cream : Hex("E7C3A6");
                }
                else traySLbl[i].text = "";
            }
            if (cur.ghostOn && hoverTray >= 0 && cur.hide != hoverTray + 1)
            {
                ghostSq.gameObject.SetActive(true);
                ghostSq.localPosition = trayPos[hoverTray] + Vector3.up * 0.18f;
                ghostSq.localRotation = trayT[hoverTray].localRotation;
            }
            else if (ghostSq) ghostSq.gameObject.SetActive(false);
        }

        void RefreshCountLabel()
        {
            if (!countLbl) return;
            if (nGoal <= 0) { countLbl.text = ""; return; }
            countLbl.text = Mathf.RoundToInt(shownN).ToString();
            countLbl.transform.position = jig.TransformPoint(trayPos[Mathf.Clamp(cur != null ? cur.targetSide : 2, 0, 2)] + Vector3.up * 1.6f);
            countLbl.transform.rotation = Quaternion.LookRotation(countLbl.transform.position - cam.transform.position, Vector3.up);
        }

        int NearestTray(Vector3 p, float max)
        {
            int bestI = -1; float bestD = max * max;
            for (int i = 0; i < 3; i++)
            {
                Vector3 tp = jig.TransformPoint(trayPos[i]);
                float d = (new Vector3(p.x, 0, p.z) - new Vector3(tp.x, 0, tp.z)).sqrMagnitude;
                if (d < bestD) { bestD = d; bestI = i; }
            }
            return bestI;
        }

        bool InDiscard(Vector3 p)
        {
            Vector3 t = troughT.position;
            return Mathf.Abs(p.x - t.x) < 1.2f && Mathf.Abs(p.z - t.z) < 2.0f;
        }

        void HighlightHover(Vector3 p)
        {
            hoverTray = NearestTray(p, 1.2f);
            RefreshTrayLabels();
            if (trailLr && dragTile >= 0)
            {
                trailLr.enabled = true;
                Vector3 a = tiles[dragTile].root.position;
                for (int i = 0; i < 8; i++)
                    trailLr.SetPosition(i, Vector3.Lerp(tiles[dragTile].dockPos + Vector3.up * 0.3f, a, i / 7f));
            }
        }

        void ClearHover()
        {
            hoverTray = -1;
            if (trailLr) trailLr.enabled = false;
            RefreshTrayLabels();
        }

        void JiggleLatch(int tray)
        {
            jiggleT = 0.22f;
            swingTray = tray;
        }

        void PulseTray(int tray)
        {
            swingT = 0.28f;
            swingTray = tray;
        }

        void ShowGhost(int tileIdx, int tray)
        {
            ghostT = 0;
            fingerT.gameObject.SetActive(true);
            ghostRing.gameObject.SetActive(true);
            ghostFrom = tiles[Mathf.Clamp(tileIdx, 0, 2)].dockPos + Vector3.up * 0.4f;
            ghostTo = jig.TransformPoint(trayPos[Mathf.Clamp(tray, 0, 2)]) + Vector3.up * 0.4f;
            ghostRing.position = ghostTo;
        }

        void ShowGhostLatch(int tray)
        {
            tray = Mathf.Clamp(tray, 0, 2);
            ghostT = 0;
            fingerT.gameObject.SetActive(true);
            ghostRing.gameObject.SetActive(true);
            ghostFrom = jig.TransformPoint(trayPos[tray]) + Vector3.up * 0.55f;
            ghostTo = latchT[tray].position + Vector3.up * 0.38f;
            ghostRing.position = ghostTo;
        }

        void ShowGhostAt(Vector3 to)
        {
            ghostT = 0;
            fingerT.gameObject.SetActive(true);
            ghostRing.gameObject.SetActive(true);
            ghostFrom = to + Vector3.up * 0.5f;
            ghostTo = to;
            ghostRing.position = to;
        }

        Vector3 ghostFrom, ghostTo;

        void HideGhost()
        {
            if (fingerT) fingerT.gameObject.SetActive(false);
            if (ghostRing) ghostRing.gameObject.SetActive(false);
        }

        void PointAtDock()
        {
            if (tiles[0] == null) return;
            if (ph == Ph.Practice) { PracticeGhost(); return; }
            ShowGhost(0, cur != null && cur.kind == Kind.Reverse ? 2 : 0);
            ghostT = 0;
        }

        void RefreshHypLabel()
        {
            if (!hypLbl) return;
            bool show = ph == Ph.Practice && cur != null && cur.kind != Kind.Reverse;
            hypLbl.gameObject.SetActive(show);
            if (!show) return;
            hypLbl.text = "빗변";
            // 쟁반 옆(나무못과 반대쪽 옆)에 붙여 판·못·유령 손가락을 가리지 않게 한다.
            Vector3 side = Vector3.Cross(Vector3.up, trayFwd[2]).normalized;
            hypLbl.transform.localPosition = trayPos[2] + Vector3.up * 0.58f + side * (TraySize * 0.85f);
        }

        bool PortraitSquat()
        {
            if (land) return false;
            return (float)Screen.height / Mathf.Max(1, Screen.width) < 1.7f;
        }

        void ResetSpike()
        {
            impaled = 0;
            for (int i = 0; i < 3; i++) if (crackedPile[i]) crackedPile[i].gameObject.SetActive(false);
        }

        void ImpaleTile()
        {
            if (impaled >= 3) return;
            var p = crackedPile[impaled];
            p.gameObject.SetActive(true);
            p.localPosition = new Vector3(0.12f, 0.45f + impaled * 0.42f, 0);
            p.localRotation = Quaternion.Euler(18, 20 * impaled, -12);
            impaled++;
        }

        void FrameCam()
        {
            if (!cam) return;
            if (land)
            {
                camHome = new Vector3(0.5f, 12.8f, -8.0f);
                lookHome = new Vector3(0.5f, 0.1f, 1.05f);
                cam.fieldOfView = 32f;
            }
            else
            {
                camHome = new Vector3(0.25f, 12.4f, -9.4f);
                lookHome = new Vector3(0.25f, 0.2f, 0.55f);
                cam.fieldOfView = PortraitSquat() ? 40f : 38f;
            }
            cam.transform.position = camHome;
            cam.transform.LookAt(lookHome);
            MgfLook.FitWidth(cam, cam.fieldOfView, land ? 0.62f : 0.78f);
        }

        void Animate(float dt)
        {
            ambientT += dt;
            if (jig && ph == Ph.Title)
                jig.rotation = Quaternion.Euler(0, cur != null ? cur.rot + Mathf.Sin(ambientT * 0.4f) * 6f : 0, 0);

            if (jiggleT > 0)
            {
                jiggleT -= dt;
                if (jig)
                {
                    float k = jiggleT / 0.32f;
                    jig.localRotation = Quaternion.Euler(0, (cur != null ? cur.rot : 0) + Mathf.Sin(jiggleT * 42f) * 4f * k, 0);
                }
            }
            else if (ph != Ph.Title && jig && cur != null)
                jig.localRotation = Quaternion.Euler(0, cur.rot, 0);

            if (swingT > 0)
            {
                swingT -= dt;
                int tr = Mathf.Clamp(swingTray, 0, 2);
                float u = 1f - Mathf.Clamp01(swingT / 0.28f);
                float squash = 1f - 0.12f * Mathf.Sin(u * Mathf.PI) * (1f - u * 0.3f);
                trayT[tr].localScale = new Vector3(trayBaseScale[tr].x, trayBaseScale[tr].y * squash, trayBaseScale[tr].z);
                latchT[tr].localPosition = latchBase[tr] + Vector3.down * (0.06f * Mathf.Sin(u * Mathf.PI));
            }

            if (slideT > 0 && cur != null && (ph == Ph.Title || ph == Ph.Practice || revealT > 0))
            {
                slideT -= dt;
                float u = 1f - Mathf.Clamp01(slideT / 1.2f);
                u = u * u * (3f - 2f * u);
                if (u > 0.35f)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (tiles[i].area == 9 || tiles[i].area == 16)
                        {
                            Vector3 from = jig.TransformPoint(trayPos[tiles[i].area == 9 ? 0 : 1] + Vector3.up * 0.22f);
                            Vector3 to = jig.TransformPoint(trayPos[2] + Vector3.up * 0.22f);
                            tiles[i].root.position = Vector3.Lerp(from, to, (u - 0.35f) / 0.65f);
                        }
                    }
                }
            }

            for (int i = 0; i < 3; i++)
            {
                var t = tiles[i];
                if (t == null) continue;
                if (t.squash > 1.001f || t.squash < 0.999f)
                    t.squash = Mathf.Lerp(t.squash, 1f, 1f - Mathf.Exp(-10f * dt));
                if (dragTile != i) SnapTile(i);
                if (t.label && cam)
                    t.label.transform.rotation = Quaternion.LookRotation(t.label.transform.position - cam.transform.position + Vector3.up * 0.2f, Vector3.up);
            }

            if (fingerT && fingerT.gameObject.activeSelf)
            {
                ghostT += dt;
                float loop = 1.2f;
                float u = (ghostT % loop) / loop;
                float e = u < 0.7f ? u / 0.7f : 1f;
                e = e * e * (3 - 2 * e);
                fingerT.position = Vector3.Lerp(ghostFrom, ghostTo, e) + Vector3.up * (0.15f * Mathf.Sin(u * Mathf.PI));
                float pulse = 1f + 0.12f * Mathf.Sin(ghostT * 6f);
                ghostRing.localScale = Vector3.one * (0.9f * ghostScale * pulse);
                var rr = ghostRing.GetComponent<Renderer>();
                if (rr)
                {
                    var c = rr.sharedMaterial.GetColor("_Color");
                    c.a = 0.35f + 0.25f * Mathf.Sin(ghostT * 5f);
                }
            }

            if (sideLbl[0] && cam)
            {
                for (int i = 0; i < 3; i++)
                {
                    sideLbl[i].transform.rotation = Quaternion.LookRotation(sideLbl[i].transform.position - cam.transform.position, Vector3.up);
                    traySLbl[i].transform.rotation = sideLbl[i].transform.rotation;
                }
            }

            if (revealT > 0) revealT -= dt;
            if (crackT > 0) crackT -= dt;
            if (stampT > 0) stampT -= dt;
            if (trailT > 0) { trailT -= dt; if (trailT <= 0 && trailLr) trailLr.enabled = false; }

            if (cam)
            {
                MgfLook.FitWidth(cam, land ? 32f : (PortraitSquat() ? 40f : 38f), land ? 0.62f : 0.78f);
                if (countLbl && countLbl.text.Length > 0)
                    countLbl.transform.rotation = Quaternion.LookRotation(countLbl.transform.position - cam.transform.position, Vector3.up);
                if (hypLbl && hypLbl.gameObject.activeSelf)
                    hypLbl.transform.rotation = Quaternion.LookRotation(hypLbl.transform.position - cam.transform.position, Vector3.up);
            }

            if (toastT > 0)
            {
                toastT -= dt;
                if (toastG) toastG.alpha = Mathf.Clamp01(toastT / 0.15f) * Mathf.Clamp01(toastDur > 0 ? toastT / Mathf.Max(0.1f, toastDur - 0.15f) : 1f);
                if (toastT <= 0 && toastG) toastG.alpha = 0;
            }

            TickRipples(dt);
            TickTimeBar();
        }

        Vector2 refuseAt;

        void TickTimeBar()
        {
            if (ph != Ph.Play && ph != Ph.Practice) return;
            int sec = Mathf.Max(0, Mathf.CeilToInt(runLeft));
            if (sec != lastTimeInt && timeTxt)
            {
                lastTimeInt = sec;
                timeTxt.text = sec.ToString();
            }
            if (timeFill)
            {
                float u = Mathf.Clamp01(runLeft / Rules.RunSec);
                timeFill.rectTransform.anchorMax = new Vector2(u, 1f);
            }
            if (sheetFill && sheetTimed)
            {
                float u = Mathf.Clamp01(sheetLeft / Rules.SheetLimit(st.level));
                sheetFill.rectTransform.anchorMax = new Vector2(u, 1f);
            }
            else if (sheetFill) sheetFill.rectTransform.anchorMax = new Vector2(1f, 1f);
        }
    }
}
