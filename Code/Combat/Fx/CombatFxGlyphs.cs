// ============================================================
// 程序化特效图元库（v0.19.12）
// 参考 unit_gifs 自生成路线：战斗特效全部程序化自生成，不依赖原版素材拼凑。
// 每个主标签有符合大众认知意象的专属图元（火=火苗/冰=冰晶/毒=毒滴/血=血滴/治疗=心…），
// 帧尺寸 64×64 对齐原版特效帧（effects/projectiles），PPU 100（世界宽由 Style 缩放控制）。
// 单一职责：只产白底 alpha Sprite（染色交给 AbilityFx 的标签基准色×id 哈希派生），
// 与 CombatFxArt（核心基元）分离，避免单文件膨胀。
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Combat.Fx
{
    /// <summary>程序化标签意象图元：白底 Sprite，运行时懒生成缓存。</summary>
    public static class CombatFxGlyphs
    {
        public const int SIZE = 64;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string glyphId)
        {
            if (Cache.TryGetValue(glyphId, out var sp)) return sp;
            var fn = Resolve(glyphId);
            if (fn == null) return null;
            sp = Make(fn);
            Cache[glyphId] = sp;
            return sp;
        }

        private static Func<int, int, float> Resolve(string id)
        {
            switch (id)
            {
                case "art_glyph_fire":       return G_Fire;
                case "art_glyph_ice":        return G_Snowflake;
                case "art_glyph_poison":     return G_Poison;
                case "art_glyph_blood":      return G_Blood;
                case "art_glyph_heart":      return G_Heart;
                case "art_glyph_shield":     return G_Shield;
                case "art_glyph_star":       return G_Star;
                case "art_glyph_scythe":     return G_Scythe;
                case "art_glyph_moon":       return G_Moon;
                case "art_glyph_eye":        return G_Eye;
                case "art_glyph_vine":       return G_Vine;
                case "art_glyph_bone":       return G_Bone;
                case "art_glyph_magnet":     return G_Magnet;
                case "art_glyph_claw":       return G_Claw;
                case "art_glyph_cross":      return G_Cross;
                case "art_glyph_ghost":      return G_Ghost;
                case "art_glyph_rings":      return G_Rings;
                case "art_glyph_hourglass":  return G_Hourglass;
                case "art_glyph_lightning":  return G_Thunder;
                case "art_glyph_water":      return G_Water;
                case "art_glyph_wind":       return G_Wind;
                case "art_glyph_earth":      return G_Earth;
                case "art_glyph_metal":      return G_Metal;
                case "art_glyph_holy":       return G_Light;
                case "art_glyph_shadow":     return G_Shadow;
                case "art_glyph_impact":     return G_Impact;
                case "art_glyph_pulse":      return G_Pulse;
                default:                     return null;
            }
        }

        /// <summary>按像素 alpha 生成白底精灵（PPU 100：世界宽 = size 由 Style 控制）。</summary>
        private static Sprite Make(Func<int, int, float> alpha)
        {
            var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            Color32[] px = new Color32[SIZE * SIZE];
            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float a = Mathf.Clamp01(alpha(x, y));
                    px[y * SIZE + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, SIZE, SIZE), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------------------------------------------------------- 基元 --

        private static float Dist(float x, float y, float cx, float cy)
            => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

        private static float RingAlpha(float d, float r, float inner, float outer)
        {
            float lo = r * inner, hi = r * outer;
            if (d < lo || d > hi) return 0f;
            if (d < lo + 1f) return d - lo;
            if (d > hi - 1f) return hi - d;
            return 1f;
        }

        private static float DotAlpha(float d, float r)
            => Mathf.Clamp01(r - d);

        private static float BarAlpha(float ux, float half)
            => Mathf.Clamp01(half + 0.5f - Mathf.Abs(ux));

        // ---------------------------------------------------------- 意象图元 --

        /// <summary>火：外焰环 + 内核，意象=燃烧。</summary>
        private static float G_Fire(int x, int y)
        {
            float d = Dist(x, y, 32f, 34f);
            float a = RingAlpha(d, 24f, 0.42f, 0.56f) * 0.85f;
            a = Mathf.Max(a, DotAlpha(Dist(x, y, 32f, 30f), 7f) * 0.95f);
            return a;
        }

        /// <summary>冰晶：六角雪花（三线+端点叉），意象=霜冻。</summary>
        private static float G_Snowflake(int x, int y)
        {
            float a = 0f;
            for (int i = 0; i < 3; i++)
            {
                float ang = i * Mathf.PI / 3f;
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                float dx = (x - 32f) * ca + (y - 32f) * sa;
                float dy = -(x - 32f) * sa + (y - 32f) * ca;
                a = Mathf.Max(a, BarAlpha(dy, 0.7f) * Mathf.Clamp01(1f - Mathf.Abs(dx) / 20f));
                if (Mathf.Abs(dx - 13f) < 6f)
                    a = Mathf.Max(a, BarAlpha(dy + 2f, 0.5f) * 0.6f * Mathf.Clamp01(1f - Mathf.Abs(dx - 13f) / 6f));
            }
            return a;
        }

        /// <summary>毒：毒滴 + 气泡，意象=毒液。</summary>
        private static float G_Poison(int x, int y)
        {
            float a = DotAlpha(Dist(x, y, 36f, 36f), 8f) * 0.9f;
            a = Mathf.Max(a, RingAlpha(Dist(x, y, 25f, 26f), 5f, 0.4f, 0.85f) * 0.8f);
            a = Mathf.Max(a, RingAlpha(Dist(x, y, 43f, 26f), 3.5f, 0.3f, 0.85f) * 0.6f);
            return a;
        }

        /// <summary>血：血滴（尖滴形），意象=嗜血。</summary>
        private static float G_Blood(int x, int y)
        {
            float a = 0f;
            float dx = Mathf.Abs(x - 32f), dy = y - 36f;
            if (dy > 0f && Mathf.Abs(dx) <= 9f - dy * 0.35f) a = 1f;      // 下尖
            if (Dist(x, y, 32f, 26f) <= 8f) a = 1f;                       // 上圆
            return a;
        }

        /// <summary>心：双圆 + 尖底，意象=治疗/再生。</summary>
        private static float G_Heart(int x, int y)
        {
            float a = 0f;
            if (Dist(x, y, 27f, 27f) <= 7f) a = 1f;
            if (Dist(x, y, 37f, 27f) <= 7f) a = 1f;
            float dx = x - 32f, dy = y - 40f;
            if (dy > 0f && Mathf.Abs(dx) <= 12f - dy * 0.45f) a = 1f;
            return a;
        }

        /// <summary>盾：上圆下尖盾形，意象=护盾/元素抗性。</summary>
        private static float G_Shield(int x, int y)
        {
            float a = 0f;
            if (Dist(x, y, 32f, 27f) <= 13f) a = 1f;
            float dx = Mathf.Abs(x - 32f), dy = y - 38f;
            if (dy >= 0f && Mathf.Abs(dx) <= 13f - dy * 0.6f) a = 1f;
            if (a > 0f && Dist(x, y, 32f, 27f) <= 6f) a = 0.55f;          // 内纹
            return a;
        }

        /// <summary>星：五角星，意象=暴击/多重。</summary>
        private static float G_Star(int x, int y)
        {
            float d = Dist(x, y, 32f, 32f);
            if (d > 24f) return 0f;
            float theta = Mathf.Atan2(y - 32f, x - 32f) + Mathf.PI / 2f;
            float sector = theta % (Mathf.PI * 2f / 5f);
            if (sector > Mathf.PI / 5f) sector -= Mathf.PI * 2f / 5f;
            if (sector < -Mathf.PI / 5f) sector += Mathf.PI * 2f / 5f;
            float rad = 24f * (0.62f + 0.38f * (1f - Mathf.Abs(sector) / (Mathf.PI / 5f)));
            return d <= rad ? 1f : 0f;
        }

        /// <summary>镰刀：弧刃 + 柄，意象=致死/斩杀。</summary>
        private static float G_Scythe(int x, int y)
        {
            float a = RingAlpha(Dist(x, y, 44f, 30f), 18f, 0.55f, 0.75f) * 0.9f;
            float dx = x - 46f, dy = y - 52f;
            if (Mathf.Abs(dx) <= 1.2f && dy >= -16f && dy <= 0f) a = 1f;
            return a;
        }

        /// <summary>月牙：弯月，意象=睡眠。</summary>
        private static float G_Moon(int x, int y)
        {
            float a = RingAlpha(Dist(x, y, 34f, 32f), 16f, 0.45f, 0.62f) * 0.9f;
            if (Dist(x, y, 30f, 32f) <= 12f) a = 0f;
            return a;
        }

        /// <summary>眼：圆环+瞳，意象=灵魂/凝视。</summary>
        private static float G_Eye(int x, int y)
        {
            float a = RingAlpha(Dist(x, y, 32f, 32f), 13f, 0.45f, 0.85f) * 0.9f;
            a = Mathf.Max(a, DotAlpha(Dist(x, y, 32f, 32f), 4f) * 1f);
            return a;
        }

        /// <summary>藤蔓：三波浪线，意象=束缚/缠绕。</summary>
        private static float G_Vine(int x, int y)
        {
            float a = 0f;
            for (int i = 0; i < 3; i++)
            {
                float baseY = 22f + i * 8f;
                if (Mathf.Abs(y - (baseY + Mathf.Sin(x * 0.15f + i * 2f) * 3f)) <= 3f)
                    a = Mathf.Max(a, 0.9f);
            }
            return a;
        }

        /// <summary>骨：竖骨+两端结节，意象=衰老/虚弱。</summary>
        private static float G_Bone(int x, int y)
        {
            float a = 0f;
            if (Mathf.Abs(x - 32f) <= 2f && Mathf.Abs(y - 32f) <= 16f) a = 1f;
            if (Dist(x, y, 27f, 19f) <= 5f) a = 1f;
            if (Dist(x, y, 37f, 19f) <= 5f) a = 1f;
            if (Dist(x, y, 27f, 45f) <= 5f) a = 1f;
            if (Dist(x, y, 37f, 45f) <= 5f) a = 1f;
            return a;
        }

        /// <summary>U 磁铁，意象=磁力。</summary>
        private static float G_Magnet(int x, int y)
        {
            float a = 0f;
            float dx = Mathf.Abs(x - 32f), dy = y - 32f;
            if (dx <= 5f && dy >= 0f && dy <= 18f) a = 1f;
            if (dx <= 14f && dy >= -8f && dy <= 0f) a = 1f;
            if (Dist(x, y, 32f, 14f) <= 5f) a = 1f;
            return a;
        }

        /// <summary>爪印：三爪弧，意象=召唤兽。</summary>
        private static float G_Claw(int x, int y)
        {
            float a = 0f;
            for (int i = -1; i <= 1; i++)
            {
                float cx = 30f + i * 10f;
                if (Dist(x, y, cx, 36f) <= 6f && y >= 34f) a = 1f;
            }
            return a;
        }

        /// <summary>十字：净化光十字。</summary>
        private static float G_Cross(int x, int y)
        {
            float a = 0f;
            if (Mathf.Abs(x - 32f) <= 2.5f && Mathf.Abs(y - 32f) <= 13f) a = 1f;
            if (Mathf.Abs(y - 32f) <= 2.5f && Mathf.Abs(x - 32f) <= 13f) a = 1f;
            return a;
        }

        /// <summary>虚影：环+内影，意象=闪避/隐身/幻象/分身/瞬移。</summary>
        private static float G_Ghost(int x, int y)
        {
            float a = RingAlpha(Dist(x, y, 32f, 32f), 18f, 0.55f, 0.72f) * 0.8f;
            a = Mathf.Max(a, DotAlpha(Dist(x, y, 32f, 32f), 5f) * 0.6f);
            return a;
        }

        /// <summary>冲击环：双环，意象=溅射/声波/震动/击退/重力/念力。</summary>
        private static float G_Rings(int x, int y)
        {
            float d = Dist(x, y, 32f, 32f);
            float a = RingAlpha(d, 24f, 0.55f, 0.66f) * 0.9f;
            a = Mathf.Max(a, RingAlpha(d, 24f, 0.82f, 0.90f) * 0.65f);
            return a;
        }

        /// <summary>沙漏：上下锥+束腰，意象=时缓。</summary>
        private static float G_Hourglass(int x, int y)
        {
            float a = 0f;
            float dx = Mathf.Abs(x - 32f);
            if (Mathf.Abs(y - 32f) <= 20f && dx <= 12f - (Mathf.Abs(y - 32f)) * 0.18f) a = 0.85f;
            return a;
        }

        /// <summary>闪电：Z 折线，意象=雷/麻痹。</summary>
        private static float G_Thunder(int x, int y)
        {
            float a = 0f;
            float[][] seg = {
                new float[]{ 40f, 22f, 26f, 34f },
                new float[]{ 26f, 34f, 40f, 40f },
                new float[]{ 40f, 40f, 24f, 52f },
            };
            foreach (var s in seg)
                a = Mathf.Max(a, SegmentAlpha(x, y, s[0], s[1], s[2], s[3], 2.2f));
            return a;
        }

        /// <summary>水：波纹+水滴，意象=水。</summary>
        private static float G_Water(int x, int y)
        {
            float d = Dist(x, y, 32f, 32f);
            float a = RingAlpha(d, 26f, 0.55f, 0.62f) * 0.9f;
            a = Mathf.Max(a, RingAlpha(d, 26f, 0.78f, 0.85f) * 0.7f);
            float dx = x - 32f, dy = y - 32f;
            if (dy > 0f && d <= 9f && Mathf.Abs(dx) <= 6f - dy * 0.5f) a = Mathf.Max(a, 0.95f);
            return a;
        }

        /// <summary>风：三斜刃，意象=风刃。</summary>
        private static float G_Wind(int x, int y)
        {
            float a = 0f;
            for (int i = -1; i <= 1; i++)
            {
                float ang = i * 0.5f;
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                float dx = (x - 32f) * ca + (y - 32f) * sa;
                float dy = -(x - 32f) * sa + (y - 32f) * ca;
                a = Mathf.Max(a, BarAlpha(dy, 1.2f) * Mathf.Clamp01(1f - Mathf.Abs(dx) / 24f));
            }
            return a;
        }

        /// <summary>土：菱形土块+裂纹，意象=土/石化。</summary>
        private static float G_Earth(int x, int y)
        {
            float dx = Mathf.Abs(x - 32f), dy = Mathf.Abs(y - 32f);
            float a = Mathf.Clamp01(1f - (dx + dy) / 21f) * 0.95f;
            a = Mathf.Max(a, BarAlpha(x - 30f, 0.5f) * Mathf.Clamp01(1f - Mathf.Abs(y - 34f) / 12f) * 0.8f);
            return a;
        }

        /// <summary>金：斜刃条，意象=金属刃/破甲。</summary>
        private static float G_Metal(int x, int y)
        {
            float dx = x - 32f, dy = y - 32f;
            float ux = (dx - dy) * 0.7071f;
            float uy = (dx + dy) * 0.7071f;
            float a = BarAlpha(uy, 2.2f) * Mathf.Clamp01(1f - Mathf.Abs(ux) / 24f);
            a = Mathf.Max(a, BarAlpha(uy + 6f, 1.0f) * Mathf.Clamp01(1f - Mathf.Abs(ux - 8f) / 14f) * 0.7f);
            return a;
        }

        /// <summary>圣光：光芒星+中心圆，意象=光明。</summary>
        private static float G_Light(int x, int y)
        {
            float d = Dist(x, y, 32f, 32f);
            float a = Mathf.Max(DotAlpha(d, 7f), RingAlpha(d, 14f, 0.55f, 0.68f) * 0.6f);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f;
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                float dx = (x - 32f) * ca + (y - 32f) * sa;
                float dy = -(x - 32f) * sa + (y - 32f) * ca;
                a = Mathf.Max(a, BarAlpha(dy, 0.9f) * Mathf.Clamp01(1f - Mathf.Abs(dx - 17f) / 9f) * 0.85f);
            }
            return a;
        }

        /// <summary>暗影：弧+拖尾+眼，意象=暗影/诅咒。</summary>
        private static float G_Shadow(int x, int y)
        {
            float a = RingAlpha(Dist(x, y, 32f, 36f), 24f, 0.60f, 0.68f) * 0.8f;
            a = Mathf.Max(a, Mathf.Clamp01(1f - Mathf.Abs(x - 32f) / 16f) * Mathf.Clamp01(1f - (y - 40f) / 14f) * 0.5f);
            a = Mathf.Max(a, DotAlpha(Dist(x, y, 32f, 32f), 6f) * 0.9f);
            return a;
        }

        /// <summary>命中闪光：实心圆+外晕，意象=普通打击。</summary>
        private static float G_Impact(int x, int y)
        {
            float d = Dist(x, y, 32f, 32f);
            float a = DotAlpha(d, 6f);
            a = Mathf.Max(a, RingAlpha(d, 16f, 0.35f, 0.5f) * 0.5f);
            return a;
        }

        /// <summary>脉冲：呼吸圆环，意象=领域/威压。</summary>
        private static float G_Pulse(int x, int y)
        {
            float d = Dist(x, y, 32f, 32f);
            float a = RingAlpha(d, 28f, 0.55f, 0.66f) * 0.8f;
            a = Mathf.Max(a, RingAlpha(d, 28f, 0.85f, 0.92f) * 0.5f);
            return a;
        }

        // ---------------------------------------------------------- 工具 --

        private static float SegmentAlpha(int x, int y, float x1, float y1, float x2, float y2, float w)
        {
            float dx = x2 - x1, dy = y2 - y1;
            float len2 = dx * dx + dy * dy;
            if (len2 < 0.001f) return 0f;
            float t = ((x - x1) * dx + (y - y1) * dy) / len2;
            t = Mathf.Clamp01(t);
            float px = x1 + t * dx, py = y1 + t * dy;
            return Mathf.Clamp01(w + 0.5f - Dist(x, y, px, py));
        }
    }
}
