#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

// 타이틀 화면에 쓰는 이미지(어두운 대도시 빌딩 층, 푸른 안개, 빨간 눈, 가로등 빛, UI 프레임)를 코드로 그려 PNG로 저장한다.
// 메뉴: Tools > Vesper > 1. 타이틀 에셋 생성. 결과는 Assets/04.Images/Title에 쌓이고, 다시 실행하면 폴더를 비우고 새로 쓴다.
// 배경 층은 설계 해상도 960x540(2배 확대하면 1920x1080)으로 그린 뒤 부드럽게(Bilinear) 늘려 쓰며,
// 시차 연출로 가장자리가 비지 않도록 사방에 16칸씩 더 그린다.
public static class TitleAssetGenerator
{
    public const string Dir = "Assets/04.Images/Title";
    private const int M = 16;                // 가장자리 여분
    private const int W = 960 + M * 2;
    private const int H = 540 + M * 2;
    private const int GroundY = 470;         // 길바닥이 시작하는 설계 y

    [MenuItem("Tools/Vesper/1. 타이틀 에셋 생성")]
    public static void GenerateAll()
    {
        if (Directory.Exists(Dir)) AssetDatabase.DeleteAsset(Dir);
        Directory.CreateDirectory(Dir);
        AssetDatabase.Refresh();

        Save(Sky(), "title_sky", false);
        Save(FarCity(), "title_city_far", false);
        Save(MidCity(), "title_city_mid", false);
        Save(NearCity(), "title_city_near", false);
        Save(Fog(0), "title_fog_a", true);
        Save(Fog(1), "title_fog_b", true);
        Save(Fog(2), "title_fog_c", true);
        Save(Eye(), "title_eye", false);
        Save(Glow(), "title_glow", false);
        Save(Bolt(), "title_bolt", false);
        Save(Cone(), "title_cone", false);
        Save(Vignette(), "title_vignette", false);
        Save(Title(), "title_vesper", false, null, FilterMode.Point);
        Save(Ornament(), "title_ornament", false);
        Save(Panel(), "ui_panel", false, new Vector4(24, 24, 24, 24));
        Save(Field(), "ui_field", false, new Vector4(12, 12, 12, 12));
        for (int i = 0; i < 4; i++)
            Save(ButtonSprite(i), new[] { "ui_button", "ui_button_hover", "ui_button_pressed", "ui_button_disabled" }[i], false, new Vector4(12, 12, 12, 12));

        AssetDatabase.Refresh();
        Debug.Log("[TitleAssetGenerator] 타이틀 에셋을 " + Dir + "에 만들었습니다.");
    }

    // ───────────────────────────── 색, 잡음 도우미 ─────────────────────────────

    private static Color32 C(string hex, byte alpha = 255)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        Color32 r = c;
        r.a = alpha;
        return r;
    }

    private static Color32 Mix(Color32 a, Color32 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xffff) / 65535f;
        }
    }

    // 값 잡음. periodX가 0보다 크면 가로로 그 칸 수마다 이어 붙는다(안개 타일용).
    private static float Noise(float x, float y, int seed, int periodX)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        int xa = x0, xb = x0 + 1;
        if (periodX > 0)
        {
            xa = ((xa % periodX) + periodX) % periodX;
            xb = ((xb % periodX) + periodX) % periodX;
        }
        float a = Mathf.Lerp(Hash(xa, y0, seed), Hash(xb, y0, seed), fx);
        float b = Mathf.Lerp(Hash(xa, y0 + 1, seed), Hash(xb, y0 + 1, seed), fx);
        return Mathf.Lerp(a, b, fy);
    }

    private static float Fbm(float x, float y, int seed, int periodX, int octaves)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += amp * Noise(x, y, seed + i * 17, periodX);
            norm += amp;
            x *= 2f; y *= 2f;
            if (periodX > 0) periodX *= 2;
            amp *= 0.5f;
        }
        return sum / norm;
    }

    // ───────────────────────────── 캔버스 ─────────────────────────────

    private class PixelCanvas
    {
        public readonly int W, H;
        public int Ox, Oy;
        public readonly Color32[] D;

        public PixelCanvas(int w, int h) { W = w; H = h; D = new Color32[w * h]; }

        public bool In(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }
        public void SetRaw(int x, int y, Color32 c) { if (In(x, y)) D[y * W + x] = c; }
        public Color32 GetRaw(int x, int y) { return In(x, y) ? D[y * W + x] : default; }
        public void Set(int x, int y, Color32 c) { SetRaw(x + Ox, y + Oy, c); }
        public Color32 Get(int x, int y) { return GetRaw(x + Ox, y + Oy); }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    Set(xx, yy, c);
        }

        // 이미 그려진 픽셀 위에 색을 섞어 얹는다(알파가 있는 색도 된다).
        public void Over(int x, int y, Color32 c)
        {
            Color32 d = Get(x, y);
            Set(x, y, d.a == 0 ? c : Mix(d, new Color32(c.r, c.g, c.b, d.a), c.a / 255f));
        }

        public void OverRect(int x, int y, int w, int h, Color32 c)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    Over(xx, yy, c);
        }

        public void Disc(float cx, float cy, float r, Color32 c)
        {
            for (int yy = Mathf.FloorToInt(cy - r) - 1; yy <= Mathf.CeilToInt(cy + r) + 1; yy++)
                for (int xx = Mathf.FloorToInt(cx - r) - 1; xx <= Mathf.CeilToInt(cx + r) + 1; xx++)
                {
                    float dx = xx + 0.5f - cx, dy = yy + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r) Set(xx, yy, c);
                }
        }

        public void Thick(float x0, float y0, float x1, float y1, float r0, float r1, Color32 c)
        {
            float len = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            int steps = Mathf.Max(1, Mathf.CeilToInt(len * 2f));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Disc(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), Mathf.Lerp(r0, r1, t), c);
            }
        }

        public void Poly(IList<Vector2> pts, Color32 c)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector2 p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }

            List<float> xs = new List<float>();
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
            {
                float sy = y + 0.5f;
                xs.Clear();
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                    if ((a.y <= sy && b.y > sy) || (b.y <= sy && a.y > sy))
                        xs.Add(a.x + (sy - a.y) / (b.y - a.y) * (b.x - a.x));
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                    for (int x = Mathf.RoundToInt(xs[k]); x < Mathf.RoundToInt(xs[k + 1]); x++)
                        Set(x, y, c);
            }
        }

        public Texture2D ToTexture()
        {
            Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            Color32[] flipped = new Color32[D.Length];
            for (int y = 0; y < H; y++)
                Array.Copy(D, y * W, flipped, (H - 1 - y) * W, W);
            t.SetPixels32(flipped);
            t.Apply();
            return t;
        }
    }

    private static Vector2 V(float x, float y) { return new Vector2(x, y); }

    // ───────────────────────────── 하늘 ─────────────────────────────

    private static Texture2D Sky()
    {
        PixelCanvas p = new PixelCanvas(W, H) { Ox = M, Oy = M };
        Color32[] stops = { C("010208"), C("04060f"), C("091024"), C("101b3a"), C("18284d") };
        float[] at = { 0f, 0.3f, 0.6f, 0.82f, 1f };

        for (int y = 0; y < H; y++)
        {
            float t = Mathf.Clamp01((y - M) / 460f);
            int i = 0;
            while (i < at.Length - 2 && t > at[i + 1]) i++;
            Color32 baseColor = Mix(stops[i], stops[i + 1], (t - at[i]) / (at[i + 1] - at[i]));

            for (int x = 0; x < W; x++)
            {
                Color32 c = baseColor;
                // 도시 불빛이 낮은 구름에 번진 푸른 기운과, 구름 덩어리
                float cloud = Fbm(x * 0.006f, y * 0.012f, 3, 0, 4);
                c = Mix(c, C("1b2f5e"), Smooth(0.45f, 0.85f, cloud) * 0.4f * (0.3f + t));
                // 오른쪽 위 구름 뒤의 흐린 달빛
                float dx = x - 760f, dy = y - 130f;
                c = Mix(c, C("3a5088"), Mathf.Exp(-(dx * dx + dy * dy) / 9000f) * 0.35f);
                p.SetRaw(x, y, c);
            }
        }
        return p.ToTexture();
    }

    // ───────────────────────────── 건물 ─────────────────────────────

    private class TowerStyle
    {
        public Color32 Body, Rim, DimWindow, CoolWindow, WarmWindow;
        public int MinW, MaxW, MinTop, MaxTop;
        public int StepX = 9, StepY = 12, WinW = 3, WinH = 5;
        public float DimChance = 0.5f, CoolChance = 0.03f, WarmChance = 0.012f;
        public float AntennaChance = 0.35f;
    }

    private static void DrawTower(PixelCanvas p, Random rng, int x, int w, int top, TowerStyle s)
    {
        int tiers = rng.Next(1, 4);
        int curTop = top;
        int tierX = x, tierW = w;
        for (int t = 0; t < tiers; t++)
        {
            p.Rect(tierX, curTop, tierW, 700, s.Body);
            p.Rect(tierX, curTop, 1, 700, s.Rim);                 // 왼쪽 모서리의 희미한 빛
            p.Rect(tierX, curTop, tierW, 1, s.Rim);

            // 창문 격자: 대부분 꺼져 있고 드물게 켜져 있다.
            for (int yy = curTop + s.StepY; yy < GroundY - 6; yy += s.StepY)
                for (int xx = tierX + s.StepX / 2; xx < tierX + tierW - s.WinW - 2; xx += s.StepX)
                {
                    double r = rng.NextDouble();
                    Color32? c = null;
                    if (r < s.WarmChance) c = s.WarmWindow;
                    else if (r < s.WarmChance + s.CoolChance) c = s.CoolWindow;
                    else if (r < s.DimChance) c = s.DimWindow;
                    if (c.HasValue) p.Rect(xx, yy, s.WinW, s.WinH, c.Value);
                }

            // 다음 층은 좁아지며 올라간다.
            int inset = Mathf.Max(3, tierW / 6);
            tierX += inset;
            tierW -= inset * 2;
            curTop -= rng.Next(18, 55);
            if (tierW < 12) break;
        }

        // 안테나와 항공 장애등(빨간 점)
        if (w > 36 && rng.NextDouble() < s.AntennaChance)
        {
            int ax = tierX + tierW / 2, ah = rng.Next(20, 60);
            p.Rect(ax, curTop - ah + 55, 1, ah, s.Rim);
            p.Disc(ax + 0.5f, curTop - ah + 55, 1.6f, C("c8202a"));
            p.OverRect(ax - 2, curTop - ah + 53, 5, 5, C("c8202a", 40));
        }
    }

    private static void Towers(PixelCanvas p, Random rng, int xStart, int xEnd, TowerStyle s)
    {
        int x = xStart;
        while (x < xEnd)
        {
            int w = rng.Next(s.MinW, s.MaxW);
            DrawTower(p, rng, x, w, rng.Next(s.MinTop, s.MaxTop), s);
            x += w - rng.Next(0, w / 4);
        }
    }

    // 아래로 갈수록(지평선 쪽일수록) 푸른 안개에 묻히게 색을 섞는다.
    private static void Haze(PixelCanvas p, int fromY, int toY, Color32 haze, float maxAmount)
    {
        for (int y = fromY; y < toY; y++)
            for (int x = 0; x < 960; x++)
            {
                Color32 c = p.Get(x, y);
                if (c.a == 0) continue;
                p.Set(x, y, Mix(c, new Color32(haze.r, haze.g, haze.b, 255), (y - fromY) / (float)(toY - fromY) * maxAmount));
            }
    }

    private static Texture2D FarCity()
    {
        PixelCanvas p = new PixelCanvas(W, H) { Ox = M, Oy = M };
        TowerStyle s = new TowerStyle
        {
            Body = C("0c1429"), Rim = C("1c2c50"), DimWindow = C("111c38"), CoolWindow = C("2c4a80"), WarmWindow = C("8a6a3a"),
            MinW = 40, MaxW = 96, MinTop = 150, MaxTop = 330, StepX = 8, StepY = 11, WinW = 2, WinH = 4, DimChance = 0.45f,
        };
        Towers(p, new Random(21), -30, 990, s);
        Haze(p, 200, 480, C("22386a"), 0.55f);
        return p.ToTexture();
    }

    private static Texture2D MidCity()
    {
        PixelCanvas p = new PixelCanvas(W, H) { Ox = M, Oy = M };
        TowerStyle s = new TowerStyle
        {
            Body = C("070b18"), Rim = C("141f3b"), DimWindow = C("0b1226"), CoolWindow = C("27427a"), WarmWindow = C("a87a38"),
            MinW = 60, MaxW = 130, MinTop = 90, MaxTop = 300, StepX = 10, StepY = 14, WinW = 4, WinH = 6, DimChance = 0.4f, CoolChance = 0.025f,
        };
        Towers(p, new Random(33), -40, 1000, s);
        Haze(p, 260, 480, C("162a52"), 0.4f);
        return p.ToTexture();
    }

    // 가까운 건물: 거의 검은 거대한 빌딩이 양옆을 막고, 가운데는 낮은 폐허. 길바닥, 버려진 차, 꺼져 가는 가로등 하나.
    private static Texture2D NearCity()
    {
        PixelCanvas p = new PixelCanvas(W, H) { Ox = M, Oy = M };
        Random rng = new Random(5);
        Color32 body = C("020308"), rim = C("0e1630");

        TowerStyle big = new TowerStyle
        {
            Body = body, Rim = rim, DimWindow = C("060a16"), CoolWindow = C("162a52"), WarmWindow = C("2a2034"),
            StepX = 16, StepY = 22, WinW = 7, WinH = 11, DimChance = 0.5f, CoolChance = 0.02f, WarmChance = 0f, AntennaChance = 0f,
        };
        // 왼쪽과 오른쪽의 거대한 건물
        DrawTower(p, rng, -30, 150, 20, big);
        DrawTower(p, rng, 100, 120, 170, big);
        DrawTower(p, rng, 800, 110, 90, big);
        DrawTower(p, rng, 880, 120, 10, big);
        DrawTower(p, rng, 730, 90, 250, big);

        // 가운데의 낮은 폐허: 들쭉날쭉한 지붕
        TowerStyle low = new TowerStyle
        {
            Body = body, Rim = rim, DimWindow = C("060a16"), CoolWindow = C("162a52"), WarmWindow = C("2a2034"),
            MinW = 40, MaxW = 80, MinTop = 360, MaxTop = 430, StepX = 12, StepY = 16, WinW = 5, WinH = 8, DimChance = 0.45f, CoolChance = 0.03f, AntennaChance = 0f,
        };
        Towers(p, rng, 215, 730, low);
        for (int i = 0; i < 40; i++) // 부서진 지붕 조각
            p.Rect(rng.Next(215, 730), rng.Next(350, 420), rng.Next(3, 9), rng.Next(3, 12), body);

        // 길바닥과 연석
        p.Rect(-M, GroundY, W, 300, C("03040a"));
        p.Rect(-M, GroundY - 2, W, 3, rim);
        p.Rect(-M, GroundY - 2, W, 1, C("1a2850"));
        // 젖은 길에 비친 푸른 불빛
        for (int i = 0; i < 90; i++)
        {
            int rx = rng.Next(-10, 970), ry = rng.Next(GroundY + 4, 540), rw = rng.Next(20, 140);
            p.OverRect(rx, ry, rw, rng.Next(1, 3), C("2a58b0", (byte)rng.Next(8, 30)));
        }

        // 버려진 차량 실루엣
        int[] carX = { 430, 590, 760 };
        for (int i = 0; i < carX.Length; i++)
        {
            int cx = carX[i], len = 110 + i * 8;
            p.Rect(cx, GroundY - 16, len, 16, C("010206"));
            p.Poly(new[] { V(cx + 22, GroundY - 16), V(cx + 36, GroundY - 34), V(cx + len - 34, GroundY - 34), V(cx + len - 16, GroundY - 16) }, C("010206"));
            p.Rect(cx, GroundY - 17, len, 1, rim);
            p.Disc(cx + 24, GroundY - 2, 8, C("010206"));
            p.Disc(cx + len - 24, GroundY - 2, 8, C("010206"));
            p.Rect(cx + 40, GroundY - 31, len - 78, 1, rim);
        }

        // 꺼져 가는 가로등(왼쪽): 기둥, 팔, 켜진 등
        const int lx = 250;
        p.Rect(lx, 300, 4, GroundY - 296, C("05070f"));
        p.Rect(lx, 300, 1, GroundY - 296, C("2a3556"));
        p.Rect(lx - 5, GroundY - 8, 14, 8, C("05070f"));
        p.Thick(lx + 2, 302, lx + 22, 294, 1.8f, 1.8f, C("05070f"));
        p.Rect(lx + 14, 290, 22, 4, C("05070f"));
        p.Rect(lx + 16, 294, 18, 11, C("ffd98a"));
        p.Rect(lx + 18, 296, 14, 7, C("fff1c4"));
        p.Rect(lx + 15, 294, 1, 11, C("05070f"));
        p.Rect(lx + 34, 294, 1, 11, C("05070f"));
        p.Rect(lx + 18, 305, 14, 2, C("05070f"));

        return p.ToTexture();
    }

    // ───────────────────────────── 안개 ─────────────────────────────

    // 화면 전체를 덮는 옥빛과 푸른빛이 섞인 수증기. 좌우로 이어 붙어서 옆으로 흘려 보내 쓴다.
    // 0은 크고 둥근 덩어리, 1은 바람에 길게 늘어난 가는 결, 2는 중간 크기 덩어리다.
    private static Texture2D Fog(int kind)
    {
        const int fw = 512, fh = 288;
        PixelCanvas p = new PixelCanvas(fw, fh);
        Color32 blue = C("2a64dc"), jade = C("46d8b4"), pale = C("c4f6ea");
        int seed = 11 + kind * 18;

        for (int y = 0; y < fh; y++)
            for (int x = 0; x < fw; x++)
            {
                float u = x / (float)fw, v = (y + 0.5f) / fh;
                float n;
                switch (kind)
                {
                    case 1: n = Smooth(0.5f, 0.82f, Fbm(u * 2f, v * 10f, seed, 2, 4)); break;
                    case 2: n = Smooth(0.5f, 0.85f, Fbm(u * 5f, v * 3.4f, seed, 5, 5)); break;
                    default: n = Smooth(0.48f, 0.85f, Fbm(u * 3f, v * 2.2f, seed, 3, 5)); break;
                }

                // 위아래 가장자리에서는 옅어져서, 층을 살짝 흔들어도 경계가 보이지 않는다.
                float window = Smooth(0f, 0.15f, v) * Smooth(1f, 0.85f, v);
                // 옥빛과 푸른빛이 덩어리마다 다르게 섞이고, 짙은 곳은 하얗게 밝다.
                float hue = Fbm(u * 2f, v * 2f, seed + 5, 2, 3);
                Color32 c = Mix(blue, jade, Smooth(0.3f, 0.7f, hue));
                c = Mix(c, pale, n * n * 0.45f);

                float a = n * window * 0.6f;
                p.SetRaw(x, y, new Color32(c.r, c.g, c.b, (byte)Mathf.RoundToInt(a * 255f)));
            }
        return p.ToTexture();
    }

    // ───────────────────────────── 눈, 빛, 비네트 ─────────────────────────────

    // 미간 쪽이 아래로 내려간 맹수의 빨간 눈 하나(오른쪽 눈은 코드가 뒤집어 쓴다). 둘레에 붉은 번짐, 가운데 밝은 심.
    private static Texture2D Eye()
    {
        const int w = 64, h = 32;
        PixelCanvas p = new PixelCanvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x + 0.5f) / w * 2f - 1f;
                float ny = (y + 0.5f) / h * 2f - 1f;
                // 이 그림은 왼쪽 눈이고(오른쪽 눈은 코드가 뒤집어 쓴다), 오른쪽 끝이 미간 쪽이다.
                // 미간 쪽(오른쪽)으로 갈수록 눈이 아래로 내려가야 맹수처럼 노려보는 눈이 된다.
                float tilt = ny - nx * 0.45f;
                // 양 끝이 뾰족한 아몬드꼴. 윗눈꺼풀은 평평하게 눌려 있고 아래쪽은 불룩하다.
                float tip = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(nx)), 0.85f);
                float halfH = (tilt < 0f ? 0.3f : 0.42f) * tip;
                float glow = Mathf.Exp(-(nx * nx * 0.8f + ny * ny * 2.4f) * 3f);
                float shape = 1f - Smooth(halfH * 0.8f, halfH + 0.03f, Mathf.Abs(tilt));
                float core = shape * (1f - Smooth(0f, halfH, Mathf.Abs(tilt)) * 0.45f);

                Color32 red = C("ff2418");
                Color32 hot = C("ffc9b0");
                Color32 c = Mix(red, hot, core * core);
                float a = Mathf.Clamp01(Mathf.Max(glow * 0.5f, shape));
                p.SetRaw(x, y, new Color32(c.r, c.g, c.b, (byte)Mathf.RoundToInt(a * 255f)));
            }
        return p.ToTexture();
    }

    // 하늘에서 갈라져 내려오는 번개 줄기(흰 심에 푸른 번짐). 위쪽 가운데가 시작점이라 코드가 좌우로 뒤집어 쓴다.
    private static Texture2D Bolt()
    {
        const int w = 128, h = 512;
        PixelCanvas mask = new PixelCanvas(w, h);
        Random rng = new Random(77);
        Color32 on = new Color32(255, 255, 255, 255);

        // 본줄기: 아래로 지그재그
        float x = w / 2f, y = 0f;
        List<Vector2> trunk = new List<Vector2> { new Vector2(x, y) };
        while (y < h * 0.82f)
        {
            float nx = Mathf.Clamp(x + ((float)rng.NextDouble() - 0.5f) * 40f, 24f, w - 24f);
            float ny = y + 24f + (float)rng.NextDouble() * 30f;
            mask.Thick(x, y, nx, ny, 1.8f, 1.4f, on);
            x = nx; y = ny;
            trunk.Add(new Vector2(x, y));
        }
        // 가지: 본줄기 중간중간에서 옆으로 갈라진다.
        for (int i = 2; i < trunk.Count - 1; i += 2)
        {
            Vector2 s = trunk[i];
            float dir = rng.NextDouble() < 0.5 ? -1f : 1f;
            float bx = s.x, by = s.y;
            for (int k = 0; k < 3; k++)
            {
                float nx = Mathf.Clamp(bx + dir * (10f + (float)rng.NextDouble() * 14f), 4f, w - 4f);
                float ny = by + 12f + (float)rng.NextDouble() * 16f;
                mask.Thick(bx, by, nx, ny, 1.1f - k * 0.2f, 0.9f - k * 0.2f, on);
                bx = nx; by = ny;
            }
        }

        // 번짐: 심을 여러 번 흐려서 푸른 빛으로 번지게 한다.
        float[] core = new float[w * h], glow = new float[w * h], tmp = new float[w * h];
        for (int i = 0; i < core.Length; i++) { core[i] = mask.D[i].a > 0 ? 1f : 0f; glow[i] = core[i]; }
        for (int pass = 0; pass < 4; pass++)
        {
            for (int yy = 0; yy < h; yy++)
                for (int xx = 0; xx < w; xx++)
                {
                    float sum = 0f; int n = 0;
                    for (int d = -5; d <= 5; d++) { int sx = xx + d; if (sx >= 0 && sx < w) { sum += glow[yy * w + sx]; n++; } }
                    tmp[yy * w + xx] = sum / 11f;
                }
            for (int yy = 0; yy < h; yy++)
                for (int xx = 0; xx < w; xx++)
                {
                    float sum = 0f;
                    for (int d = -5; d <= 5; d++) { int sy = yy + d; if (sy >= 0 && sy < h) sum += tmp[sy * w + xx]; }
                    glow[yy * w + xx] = sum / 11f;
                }
        }

        PixelCanvas p = new PixelCanvas(w, h);
        for (int yy = 0; yy < h; yy++)
        {
            float fade = 1f - Smooth(0.6f, 1f, yy / (float)h);   // 아래로 갈수록 사라진다.
            for (int xx = 0; xx < w; xx++)
            {
                float c = core[yy * w + xx];
                float a = Mathf.Clamp01(c + Mathf.Clamp01(glow[yy * w + xx] * 9f) * 0.55f) * fade;
                Color32 col = Mix(C("7fb4ff"), C("ffffff"), c);
                p.SetRaw(xx, yy, new Color32(col.r, col.g, col.b, (byte)Mathf.RoundToInt(a * 255f)));
            }
        }
        return p.ToTexture();
    }

    private static Texture2D Glow()
    {
        const int s = 128;
        PixelCanvas p = new PixelCanvas(s, s);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt(Mathf.Pow((x + 0.5f) / s * 2f - 1f, 2f) + Mathf.Pow((y + 0.5f) / s * 2f - 1f, 2f));
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                p.SetRaw(x, y, new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f)));
            }
        return p.ToTexture();
    }

    // 가로등에서 아래로 퍼지는 빛줄기: 위가 좁고 아래로 넓어지며 옅어진다.
    private static Texture2D Cone()
    {
        const int w = 128, h = 256;
        PixelCanvas p = new PixelCanvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float t = y / (float)(h - 1);
                float nx = (x + 0.5f) / w * 2f - 1f;
                float half = Mathf.Lerp(0.12f, 1f, t);
                float edge = Mathf.Clamp01(1f - Mathf.Abs(nx) / half);
                float a = Mathf.Pow(edge, 1.4f) * Mathf.Pow(1f - t, 1.1f);
                p.SetRaw(x, y, new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f)));
            }
        return p.ToTexture();
    }

    private static Texture2D Vignette()
    {
        const int vw = 192, vh = 108;
        PixelCanvas p = new PixelCanvas(vw, vh);
        for (int y = 0; y < vh; y++)
            for (int x = 0; x < vw; x++)
            {
                float dx = (x + 0.5f) / vw * 2f - 1f, dy = (y + 0.5f) / vh * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.85f + dy * dy * 1.1f);
                float a = Mathf.Pow(Mathf.Clamp01((d - 0.35f) / 0.8f), 1.5f) * 0.96f;
                p.SetRaw(x, y, new Color32(1, 2, 6, (byte)Mathf.RoundToInt(a * 255f)));
            }
        return p.ToTexture();
    }

    // ───────────────────────────── 제목 글자(픽셀) ─────────────────────────────

    // "VESPER": 굵은 획의 픽셀 글자. 안쪽은 어두운 남색, 테두리는 도트 한 칸의 가로등색이다.
    // 판타지풍 장식으로 글자마다 호박색 보석을 박고, 위쪽 가운데에 마름모 장식, 양옆에 장식선을 단다.
    // 도트 하나를 캔버스 4칸으로 그리므로 Point 필터로 가져온다.
    private static Texture2D Title()
    {
        const int cellW = 36, cellH = 44, gap = 6, padX = 36, padTop = 24, padBottom = 10;
        const string word = "VESPER";
        int tw = word.Length * cellW + (word.Length - 1) * gap + padX * 2;
        int th = padTop + cellH + padBottom;
        Color32 on = new Color32(255, 255, 255, 255);

        // 1) 글자 몸통
        PixelCanvas letters = new PixelCanvas(tw, th) { Oy = padTop };
        for (int i = 0; i < word.Length; i++)
        {
            letters.Ox = padX + i * (cellW + gap);
            Glyph(letters, word[i], on);
        }

        // 2) 장식: 보석, 위쪽 마름모, 양옆 장식선
        PixelCanvas deco = new PixelCanvas(tw, th);
        for (int i = 0; i < word.Length; i++)
        {
            int ox = padX + i * (cellW + gap);
            Vector2Int gem = word[i] == 'V' ? new Vector2Int(8, 14) : word[i] == 'S' ? new Vector2Int(18, 22) : new Vector2Int(9, 12);
            Diamond(deco, ox + gem.x, padTop + gem.y, 2, on);
        }
        int cx = tw / 2;
        Diamond(deco, cx, 10, 6, on);
        Diamond(deco, cx - 15, 10, 3, on);
        Diamond(deco, cx + 15, 10, 3, on);
        deco.Rect(cx - 40, 10, 22, 1, on);
        deco.Rect(cx + 19, 10, 22, 1, on);
        Diamond(deco, cx - 44, 10, 2, on);
        Diamond(deco, cx + 44, 10, 2, on);
        int midY = padTop + 22;
        for (int side = 0; side < 2; side++)
        {
            int x0 = side == 0 ? 4 : tw - 4 - 24;
            deco.Rect(x0, midY, 24, 2, on);
            deco.Rect(x0 + (side == 0 ? 0 : 20), midY - 3, 4, 8, on);
            Diamond(deco, side == 0 ? 30 : tw - 31, midY + 1, 3, on);
        }

        bool[] face = new bool[tw * th], gems = new bool[tw * th];
        for (int i = 0; i < face.Length; i++) { face[i] = letters.D[i].a > 0; gems[i] = deco.D[i].a > 0; }
        Func<bool[], int, int, bool> at = (a, x, y) => x >= 0 && y >= 0 && x < tw && y < th && a[y * tw + x];

        // 3) 색 입히기
        PixelCanvas p = new PixelCanvas(tw, th);
        Color32 faceTop = C("2e3a5c"), faceBottom = C("070a16"), lamp = C("ffcf7a"), lampDeep = C("e8963a");
        for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                if (at(gems, x, y))
                {
                    float t = Mathf.Clamp01((y - 6) / (float)(th - 12));
                    p.SetRaw(x, y, Mix(C("fff0c4"), lampDeep, t));
                }
                else if (at(face, x, y))
                {
                    Color32 c = Mix(faceTop, faceBottom, (y - padTop) / (float)cellH);
                    if (!at(face, x - 1, y) || !at(face, x, y - 1)) c = Mix(c, C("5a6a98"), 0.6f);   // 윗면, 왼쪽 모서리의 희미한 빛
                    p.SetRaw(x, y, c);
                }
                else
                {
                    bool outline = false, glow = false;
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            if (!at(face, x + dx, y + dy)) continue;
                            if (Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= 1) outline = true; else glow = true;
                        }
                    if (outline) p.SetRaw(x, y, lamp);
                    else if (glow) p.SetRaw(x, y, new Color32(lamp.r, lamp.g, lamp.b, 70));
                    else if (at(face, x - 2, y - 3)) p.SetRaw(x, y, new Color32(2, 1, 8, 150));
                }
            }

        return p.ToTexture();
    }

    private static void Diamond(PixelCanvas p, int cx, int cy, int r, Color32 c)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= r) p.SetRaw(cx + dx, cy + dy, c);
    }

    // 굵은 획에 끝이 뾰족하게 뻗은 세리프를 단 판타지풍 글자(36x44 칸)
    private static void Glyph(PixelCanvas g, char ch, Color32 c)
    {
        switch (ch)
        {
            case 'V':
                g.Thick(5, 2, 18, 40, 5f, 4f, c);
                g.Thick(18, 40, 31, 2, 4f, 2.4f, c);
                g.Rect(0, 0, 16, 4, c); g.Rect(23, 0, 13, 4, c);
                g.Rect(0, 0, 3, 9, c); g.Rect(33, 0, 3, 9, c);
                g.Rect(16, 40, 4, 4, c);
                break;
            case 'E':
                g.Rect(4, 2, 10, 40, c);
                g.Rect(4, 2, 28, 6, c); g.Rect(4, 19, 22, 5, c); g.Rect(4, 36, 30, 6, c);
                g.Rect(0, 0, 17, 4, c); g.Rect(0, 40, 17, 4, c);
                g.Rect(31, 2, 4, 15, c); g.Rect(25, 16, 3, 12, c); g.Rect(33, 27, 3, 15, c);
                break;
            case 'S':
                {
                    float[][] pts =
                    {
                        new[] { 30f, 9f, 2.5f }, new[] { 25f, 4f, 3.5f }, new[] { 18f, 2f, 4.5f }, new[] { 11f, 4f, 5f }, new[] { 7f, 10f, 5f },
                        new[] { 10f, 17f, 5f }, new[] { 18f, 22f, 5f }, new[] { 27f, 27f, 5f }, new[] { 29f, 34f, 5f }, new[] { 24f, 40f, 4.5f },
                        new[] { 16f, 42f, 4f }, new[] { 8f, 40f, 3f }, new[] { 3f, 35f, 2.5f },
                    };
                    for (int i = 0; i + 1 < pts.Length; i++)
                        g.Thick(pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], pts[i][2], pts[i + 1][2], c);
                    g.Rect(28, 5, 4, 13, c);
                    g.Rect(4, 26, 4, 13, c);
                    break;
                }
            case 'P':
                PGlyph(g, c);
                break;
            case 'R':
                PGlyph(g, c);
                g.Thick(17, 26, 31, 41, 5f, 4f, c);
                g.Rect(25, 40, 11, 4, c);
                break;
        }
    }

    private static void PGlyph(PixelCanvas g, Color32 c)
    {
        g.Rect(4, 2, 10, 40, c);
        g.Rect(0, 0, 17, 4, c);
        g.Rect(0, 40, 20, 4, c);
        float[][] bowl =
        {
            new[] { 14f, 5f, 2.5f }, new[] { 24f, 4f, 3.5f }, new[] { 31f, 9f, 4.5f }, new[] { 32f, 16f, 5f },
            new[] { 27f, 23f, 4.5f }, new[] { 18f, 26f, 3f }, new[] { 14f, 26f, 2.5f },
        };
        for (int i = 0; i + 1 < bowl.Length; i++)
            g.Thick(bowl[i][0], bowl[i][1], bowl[i + 1][0], bowl[i + 1][1], bowl[i][2], bowl[i + 1][2], c);
    }

    // 제목 아래 구분선: 가운데 호박색 마름모와 양옆으로 옅어지는 선
    private static Texture2D Ornament()
    {
        const int w = 480, h = 24;
        PixelCanvas p = new PixelCanvas(w, h);
        float cx = w / 2f, cy = h / 2f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - cx), dy = Mathf.Abs(y + 0.5f - cy);
                float line = Mathf.Clamp01(1.3f - dy) * (1f - Smooth(40f, 230f, dx));
                line *= dx < 14f ? 0f : 1f;
                float glow = Mathf.Exp(-(dx * dx + dy * dy * 2f) / 260f) * 0.5f;
                float diamond = 1f - Smooth(8f, 9.5f, dx + dy);
                float core = 1f - Smooth(3f, 4.5f, dx + dy);
                float small = (1f - Smooth(2.5f, 3.5f, Mathf.Abs(dx - 34f) + dy)) * 0.9f;

                Color32 c = C("c9b27a");
                Color32 amber = Mix(C("e8a63a"), C("fff0c4"), core);
                float a = Mathf.Max(line * 0.8f, Mathf.Max(diamond, small));
                Color32 outColor = diamond > 0.01f ? amber : c;
                float total = Mathf.Clamp01(Mathf.Max(a, glow * 0.6f));
                p.SetRaw(x, y, new Color32(outColor.r, outColor.g, outColor.b, (byte)Mathf.RoundToInt(total * 255f)));
            }
        return p.ToTexture();
    }

    // ───────────────────────────── UI 프레임 ─────────────────────────────

    // 둥근 사각형(안티앨리어싱) 한 장. 안은 위아래 색이 다르고, 테두리는 위가 밝고 아래가 어둡다.
    private static Texture2D RoundBox(int s, float radius, float borderWidth, Color32 fillTop, Color32 fillBottom, Color32 borderTop, Color32 borderBottom, Color32? innerLine = null)
    {
        PixelCanvas p = new PixelCanvas(s, s);
        float half = s / 2f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float px = x + 0.5f - half, py = y + 0.5f - half;
                float qx = Mathf.Abs(px) - (half - radius), qy = Mathf.Abs(py) - (half - radius);
                float d = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;

                float coverage = Mathf.Clamp01(0.5f - d);
                if (coverage <= 0f) continue;

                float v = y / (float)(s - 1);
                float depth = -d;
                float border = Mathf.Clamp01(borderWidth - depth + 0.5f);
                Color32 fill = Mix(fillTop, fillBottom, v);
                Color32 edge = Mix(borderTop, borderBottom, v);
                Color32 c = Mix(fill, new Color32(edge.r, edge.g, edge.b, edge.a), border);

                if (innerLine.HasValue)
                {
                    float il = Mathf.Clamp01(1f - Mathf.Abs(depth - (borderWidth + 4f)) / 1f) * 0.6f;
                    Color32 ic = innerLine.Value;
                    c = Mix(c, new Color32(ic.r, ic.g, ic.b, c.a), il);
                }

                p.SetRaw(x, y, new Color32(c.r, c.g, c.b, (byte)Mathf.RoundToInt(c.a * coverage)));
            }
        return p.ToTexture();
    }

    private static Texture2D Panel()
    {
        return RoundBox(96, 14f, 2f, C("070a15", 222), C("04060e", 232), C("4a62a0"), C("1e2a4a"), C("1a2748"));
    }

    private static Texture2D Field()
    {
        return RoundBox(48, 8f, 1.6f, C("02030a", 240), C("05070f", 240), C("1c2848"), C("3c5088"));
    }

    // 0 기본, 1 마우스 올림(호박색), 2 누름, 3 비활성
    private static Texture2D ButtonSprite(int state)
    {
        switch (state)
        {
            case 1: return RoundBox(48, 8f, 1.8f, C("4a2e12"), C("2a1808"), C("ffc46a"), C("c98a3a"));
            case 2: return RoundBox(48, 8f, 1.8f, C("0c0a08"), C("160e08"), C("c98a3a"), C("8a5a22"));
            case 3: return RoundBox(48, 8f, 1.6f, C("0e111c"), C("0a0d16"), C("2a3350"), C("1c2238"));
            default: return RoundBox(48, 8f, 1.8f, C("1a2848"), C("0e1630"), C("5a76b8"), C("2a3c6c"));
        }
    }

    // ───────────────────────────── 저장과 임포트 설정 ─────────────────────────────

    private static void Save(Texture2D tex, string name, bool repeat, Vector4? border = null, FilterMode filter = FilterMode.Bilinear)
    {
        string path = Dir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = repeat ? TextureImporterType.Default : TextureImporterType.Sprite;
        if (!repeat)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1f;
            importer.spriteBorder = border ?? Vector4.zero;
        }
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = filter;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }
}
#endif
