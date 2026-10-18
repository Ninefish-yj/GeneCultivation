// ============================================================
// 战斗表现层·程序化精灵工厂（v0.18.47）
// 运行时用 Texture2D 生成像素美术（FilterMode.Point 保持锐利），
// 零素材文件依赖：星形/火花/碎石/尘土/圆环/冲击波/闪光/光斑。
// 全部白色生成，颜色由粒子的 Grad 渐变控制；alpha 由图元函数给出。
// ============================================================
using System;
using UnityEngine;

namespace Code.Combat.Fx
{
    internal static class CombatFxArt
    {
        /// <summary>命中星形（3 档大小）。</summary>
        internal static Sprite[] HitStars;
        /// <summary>火花/斜条（5 变体）。</summary>
        internal static Sprite[] Sparks;
        /// <summary>碎石块（4 变体）。</summary>
        internal static Sprite[] Debris;
        /// <summary>尘土软斑（4 档）。</summary>
        internal static Sprite[] Dust;
        /// <summary>细圆环（小/中/大）。</summary>
        internal static Sprite[] Rings;
        /// <summary>粗冲击波环（小/中/大）。</summary>
        internal static Sprite[] Shocks;
        /// <summary>十字闪光。</summary>
        internal static Sprite Glint;
        /// <summary>中心光斑（径向渐隐）。</summary>
        internal static Sprite Burst;
        /// <summary>纯白 1×1（暗幕/遮罩用）。</summary>
        internal static Sprite White;

        // ---- v0.18.70 战斗动作图元 ----
        /// <summary>月牙剑弧（御剑横扫）。</summary>
        internal static Sprite Slash;
        /// <summary>飞剑/剑气（长菱形条）。</summary>
        internal static Sprite Blade;
        /// <summary>锯齿闪电（连锁电弧）。</summary>
        internal static Sprite Zap;
        /// <summary>地面放射裂纹（震地重击）。</summary>
        internal static Sprite Crack;
        /// <summary>符文法阵环（领域展开）。</summary>
        internal static Sprite RuneRing;

        // ---- v0.18.74 中式特效重生成（替换 soul_* 人形动画等不符世界观素材）----
        /// <summary>魂焰（魂灵之火：外焰内芯，灵魂/生命/死亡系）。</summary>
        internal static Sprite SoulFlame;
        /// <summary>风痕（双交叉细弧旋风，风系/幻象/残影）。</summary>
        internal static Sprite WindArc;
        /// <summary>木藤（S 形藤蔓带叶片，木系/藤蔓/缠缚）。</summary>
        internal static Sprite WoodVine;

        // ---- v0.19.5 奥术光膜护盾 ----
        /// <summary>奥术光膜（西幻奥术护盾观感：淡光膜罩全身，护盾池变体之一）。</summary>
        internal static Sprite ArcaneShield;

        // ---- v0.19.8 元素细分图元 ----
        internal static Sprite WaterWave;    // 水波（波纹扩散）
        internal static Sprite WindBlade;    // 风刃（双弧刃）
        internal static Sprite ThunderBolt;  // 雷闪（锯齿闪电）
        internal static Sprite EarthRock;    // 土块（碎石岩块）
        internal static Sprite MetalSlash;   // 金属斩刃（冷刃弧）
        internal static Sprite HolyLight;    // 圣光（光柱光环）
        internal static Sprite ShadowWave;   // 暗影波（暗波扩散）

        // ---- v0.21.6 异能专属图元形态库（每异能独立特效的主/副形态素材）----
        internal static Sprite GlyphFlame;        // 火苗（焰尖摆动）
        internal static Sprite GlyphIceCrystal;   // 六角冰晶
        internal static Sprite GlyphBolt;         // Z 闪电
        internal static Sprite GlyphWindEdge;     // 三斜刃（风刃）
        internal static Sprite GlyphWave;         // 波纹+水滴
        internal static Sprite GlyphRock;         // 菱形岩
        internal static Sprite GlyphMetalEdge;    // 冷刃弧
        internal static Sprite GlyphHolyRay;      // 光芒星
        internal static Sprite GlyphShadowArc;    // 暗弧+瞳
        internal static Sprite GlyphBloodDrop;    // 血滴
        internal static Sprite GlyphBoneScythe;   // 骨镰
        internal static Sprite GlyphHeart;        // 心
        internal static Sprite GlyphEye;          // 眼瞳
        internal static Sprite GlyphClaw;         // 爪印
        internal static Sprite GlyphHourglass;    // 沙漏（时缓）
        internal static Sprite GlyphMoon;         // 月牙（睡眠）
        internal static Sprite GlyphToxic;        // 毒滴+气泡
        internal static Sprite GlyphVapor;        // 雾团（毒/衰老）
        internal static Sprite GlyphChain;        // 链环（束缚/连锁）
        internal static Sprite GlyphWing;         // 光翼（召唤/圣光）
        internal static Sprite GlyphStar;         // 星芒（暴击/强化）
        internal static Sprite GlyphSigil;        // 符环（念力/领域）
        internal static Sprite GlyphRipple;       // 冲击双环（击退/溅射）
        internal static Sprite GlyphShield;       // 盾形（护盾/反射）
        internal static Sprite GlyphVine;         // 藤蔓（束缚/自然）
        internal static Sprite GlyphPrism;        // 结晶（石化/土）

        // ---- v0.19.19 程序化神形显现（替代原版 unit_anim boss 待机动画素材）----
        /// <summary>神形显现（v0.19.19 程序化，替代原版 unit_anim 素材，81×81 帧沿用宽归一化）。</summary>
        internal static Sprite DivineVision;

        private static bool _ready;

        /// <summary>幂等初始化：任意消费方调用均安全，仅首次真正生成。</summary>
        internal static void EnsureInit()
        {
            if (_ready) return;
            _ready = true;
            Init();
        }

        /// <summary>程序化特效图元分发（art_ 前缀由 AbilityFx 播放器消费）。</summary>
        internal static Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            switch (id)
            {
                case "art_soulflame": return SoulFlame;
                case "art_windarc":   return WindArc;
                case "art_woodvine":  return WoodVine;
                case "art_rune":      return RuneRing;
                case "art_glint":     return Glint;
                case "art_burst":     return Burst;
                case "art_zap":       return Zap;
                case "art_crack":     return Crack;
                case "art_blade":     return Blade;
                case "art_slash":     return Slash;
                case "art_arcaneshield": return ArcaneShield;   // v0.19.5 奥术光膜护盾（西幻观感）
                // ---- v0.19.8 元素细分图元（水/风/雷/土/金/光/暗）----
                case "art_waterwave":   return WaterWave;
                case "art_windblade":   return WindBlade;
                case "art_thunderbolt": return ThunderBolt;
                case "art_earthrock":   return EarthRock;
                case "art_metalslash":  return MetalSlash;
                case "art_holylight":   return HolyLight;
                case "art_shadowwave":  return ShadowWave;
                case "art_divinevision": return DivineVision;   // v0.19.19 程序化神形显现
                default:              return null;
            }
        }

        internal static void Init()
        {
            try
            {
                HitStars = new[]
                {
                    Make(9, 9, (x, y) => Star5At(x, y, 9, 9, 4.2f)),
                    Make(13, 13, (x, y) => Star5At(x, y, 13, 13, 6.1f)),
                    Make(17, 17, (x, y) => Star5At(x, y, 17, 17, 8.0f))
                };
                Sparks = new[]
                {
                    Make(5, 5, (x, y) => Diamond(x, y, 5, 5, 1.6f)),
                    Make(7, 7, (x, y) => Diamond(x, y, 7, 7, 2.4f)),
                    Make(9, 9, (x, y) => Diamond(x, y, 9, 9, 3.2f)),
                    Make(3, 9, (x, y) => Bar(x, y, 3, 9, 0.9f)),
                    Make(5, 13, (x, y) => Bar(x, y, 5, 13, 1.5f))
                };
                Debris = new[]
                {
                    Make(7, 7, (x, y) => Hash(x, y, 7, 0.45f)),
                    Make(8, 8, (x, y) => Hash(x, y, 8, 0.40f)),
                    Make(9, 9, (x, y) => Hash(x, y, 9, 0.38f)),
                    Make(11, 11, (x, y) => Hash(x, y, 11, 0.34f))
                };
                Dust = new[]
                {
                    Make(9, 9, (x, y) => SoftDot(x, y, 9, 9, 4.2f)),
                    Make(13, 13, (x, y) => SoftDot(x, y, 13, 13, 6.0f)),
                    Make(17, 17, (x, y) => SoftDot(x, y, 17, 17, 8.0f)),
                    Make(23, 23, (x, y) => SoftDot(x, y, 23, 23, 11.0f))
                };
                Rings = new[]
                {
                    Make(17, 17, (x, y) => Ring(x, y, 17, 17, 8.0f, 0.62f, 1.0f)),
                    Make(25, 25, (x, y) => Ring(x, y, 25, 25, 12.0f, 0.66f, 1.0f)),
                    Make(33, 33, (x, y) => Ring(x, y, 33, 33, 16.0f, 0.70f, 1.0f))
                };
                Shocks = new[]
                {
                    Make(25, 25, (x, y) => Ring(x, y, 25, 25, 12.0f, 0.55f, 0.9f)),
                    Make(41, 41, (x, y) => Ring(x, y, 41, 41, 20.0f, 0.58f, 0.9f)),
                    Make(57, 57, (x, y) => Ring(x, y, 57, 57, 28.0f, 0.62f, 0.9f))
                };
                Glint = Make(9, 9, (x, y) => Cross(x, y, 9, 9));
                Burst = Make(21, 21, (x, y) => SoftDot(x, y, 21, 21, 9.5f));
                White = Make(1, 1, (x, y) => 1f);
                // v0.18.70 战斗动作图元
                Slash = Make(33, 33, (x, y) => SlashAt(x, y, 33, 33));
                Blade = Make(29, 9, (x, y) => BladeAt(x, y, 29, 9));
                Zap = Make(33, 13, (x, y) => ZapAt(x, y, 33, 13));
                Crack = Make(41, 41, (x, y) => CrackAt(x, y, 41, 41));
                RuneRing = Make(49, 49, (x, y) => RuneRingAt(x, y, 49, 49));
                // v0.18.74 中式重生成图元
                SoulFlame = Make(31, 41, (x, y) => SoulFlameAt(x, y, 31, 41));
                WindArc = Make(33, 17, (x, y) => WindArcAt(x, y, 33, 17));
                WoodVine = Make(21, 35, (x, y) => WoodVineAt(x, y, 21, 35));
                // 奥术光膜护盾（淡光膜罩全身，护盾池变体之一）
                ArcaneShield = Make(49, 49, (x, y) => ArcaneShieldAt(x, y, 49, 49));
                // v0.19.8 元素细分图元（水/风/雷/土/金/光/暗 专属意象）
                WaterWave    = Make(33, 33, (x, y) => WaterWaveAt(x, y, 33, 33));
                WindBlade    = Make(33, 33, (x, y) => WindBladeAt(x, y, 33, 33));
                ThunderBolt  = Make(33, 33, (x, y) => ThunderBoltAt(x, y, 33, 33));
                EarthRock    = Make(33, 33, (x, y) => EarthRockAt(x, y, 33, 33));
                MetalSlash   = Make(33, 33, (x, y) => MetalSlashAt(x, y, 33, 33));
                HolyLight    = Make(33, 33, (x, y) => HolyLightAt(x, y, 33, 33));
                ShadowWave   = Make(33, 33, (x, y) => ShadowWaveAt(x, y, 33, 33));
                // v0.19.19 程序化神形显现（替代原版 unit_anim 素材，81×81 帧沿用 1.5 宽归一化）
                DivineVision = Make(81, 81, (x, y) => DivineVisionAt(x, y, 81, 81));
                // ---- v0.21.6 异能专属形态库（64×64，全部按 id 哈希 seed 参数化）----
                GlyphFlame     = Make(64, 64, (x, y) => GlyphFlameAt(x, y, 64, 64, 7));
                GlyphIceCrystal= Make(64, 64, (x, y) => GlyphIceCrystalAt(x, y, 64, 64, 11));
                GlyphBolt      = Make(64, 64, (x, y) => GlyphBoltAt(x, y, 64, 64, 13));
                GlyphWindEdge  = Make(64, 64, (x, y) => GlyphWindEdgeAt(x, y, 64, 64, 17));
                GlyphWave      = Make(64, 64, (x, y) => GlyphWaveAt(x, y, 64, 64, 19));
                GlyphRock      = Make(64, 64, (x, y) => GlyphRockAt(x, y, 64, 64, 23));
                GlyphMetalEdge = Make(64, 64, (x, y) => GlyphMetalEdgeAt(x, y, 64, 64, 29));
                GlyphHolyRay   = Make(64, 64, (x, y) => GlyphHolyRayAt(x, y, 64, 64, 31));
                GlyphShadowArc = Make(64, 64, (x, y) => GlyphShadowArcAt(x, y, 64, 64, 37));
                GlyphBloodDrop = Make(64, 64, (x, y) => GlyphBloodDropAt(x, y, 64, 64, 41));
                GlyphBoneScythe= Make(64, 64, (x, y) => GlyphBoneScytheAt(x, y, 64, 64, 43));
                GlyphHeart     = Make(64, 64, (x, y) => GlyphHeartAt(x, y, 64, 64, 47));
                GlyphEye       = Make(64, 64, (x, y) => GlyphEyeAt(x, y, 64, 64, 53));
                GlyphClaw      = Make(64, 64, (x, y) => GlyphClawAt(x, y, 64, 64, 59));
                GlyphHourglass = Make(64, 64, (x, y) => GlyphHourglassAt(x, y, 64, 64, 61));
                GlyphMoon      = Make(64, 64, (x, y) => GlyphMoonAt(x, y, 64, 64, 67));
                GlyphToxic     = Make(64, 64, (x, y) => GlyphToxicAt(x, y, 64, 64, 71));
                GlyphVapor     = Make(64, 64, (x, y) => GlyphVaporAt(x, y, 64, 64, 73));
                GlyphChain     = Make(64, 64, (x, y) => GlyphChainAt(x, y, 64, 64, 79));
                GlyphWing      = Make(64, 64, (x, y) => GlyphWingAt(x, y, 64, 64, 83));
                GlyphStar      = Make(64, 64, (x, y) => GlyphStarAt(x, y, 64, 64, 89));
                GlyphSigil     = Make(64, 64, (x, y) => GlyphSigilAt(x, y, 64, 64, 97));
                GlyphRipple    = Make(64, 64, (x, y) => GlyphRippleAt(x, y, 64, 64, 101));
                GlyphShield    = Make(64, 64, (x, y) => GlyphShieldAt(x, y, 64, 64, 103));
                GlyphVine      = Make(64, 64, (x, y) => GlyphVineAt(x, y, 64, 64, 107));
                GlyphPrism     = Make(64, 64, (x, y) => GlyphPrismAt(x, y, 64, 64, 109));
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Error("[CombatFxArt] 精灵生成失败: " + e.Message);
            }
        }

        /// <summary>按像素 alpha 生成精灵：PPU 固定 100（世界尺寸 = 像素×px 由粒子的 Px 缩放实现）。</summary>
        private static Sprite Make(int w, int h, Func<int, int, float> alpha)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float a = Mathf.Clamp01(alpha(x, y));
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------------------------------------------------------- 图元 --

        /// <summary>五角星：尖角半径 r，谷底半径 0.62r，硬边。</summary>
        private static float Star5At(int x, int y, int w, int h, float r)
        {
            float cx = (w - 1) * 0.5f;
            float cy = (h - 1) * 0.5f;
            float dx = x - cx;
            float dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > r) return 0f;
            float theta = Mathf.Atan2(dy, dx) + Mathf.PI / 2f; // 尖角朝上
            float sector = theta % (Mathf.PI * 2f / 5f);
            if (sector > Mathf.PI / 5f) sector -= Mathf.PI * 2f / 5f;
            if (sector < -Mathf.PI / 5f) sector += Mathf.PI * 2f / 5f;
            float rad = r * (0.62f + 0.38f * (1f - Mathf.Abs(sector) / (Mathf.PI / 5f)));
            return dist <= rad ? 1f : 0f;
        }

        /// <summary>菱形斜条（火花）：|dx|+|dy| 距离，边缘 1px 软。</summary>
        private static float Diamond(int x, int y, int w, int h, float t)
        {
            float dx = Mathf.Abs(x - (w - 1) * 0.5f);
            float dy = Mathf.Abs(y - (h - 1) * 0.5f);
            return Mathf.Clamp01(t + 0.5f - (dx + dy));
        }

        /// <summary>细长斜条（火花）：绕中心 45° 的窄矩形。</summary>
        private static float Bar(int x, int y, int w, int h, float half)
        {
            float dx = x - (w - 1) * 0.5f;
            float dy = y - (h - 1) * 0.5f;
            float ux = (dx + dy) * 0.7071f; // 旋转 45°
            float uy = (-dx + dy) * 0.7071f;
            float len = (Mathf.Max(w, h) - 1) * 0.5f;
            if (Mathf.Abs(uy) > len * 0.98f) return 0f;
            return Mathf.Clamp01(half + 0.5f - Mathf.Abs(ux));
        }

        /// <summary>确定性随机斑点（碎石）：hash 阈值。</summary>
        private static float Hash(int x, int y, int seed, float threshold)
        {
            float h = Mathf.Sin(x * 12.9898f + y * 78.233f + seed * 37.719f) * 43758.5453f;
            h = h - Mathf.Floor(h);
            return h < threshold ? 1f : 0f;
        }

        /// <summary>软圆斑：径向渐隐。</summary>
        private static float SoftDot(int x, int y, int w, int h, float r)
        {
            float dx = x - (w - 1) * 0.5f;
            float dy = y - (h - 1) * 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(1f - dist / r);
        }

        /// <summary>圆环：d ∈ [r*inner, r*outer]，边缘 1px 软。</summary>
        private static float Ring(int x, int y, int w, int h, float r, float inner, float outer)
        {
            float dx = x - (w - 1) * 0.5f;
            float dy = y - (h - 1) * 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float lo = r * inner;
            float hi = r * outer;
            if (dist < lo - 1f || dist > hi) return 0f;
            if (dist < lo) return dist - (lo - 1f);
            if (dist > hi - 1f) return hi - dist;
            return 1f;
        }

        /// <summary>奥术光膜护盾（v0.19.5，西幻奥术护盾观感）：
        /// 内部≈0.12 淡光膜（透明不遮人）→ 0.78~1.0 光边渐强到 0.6 → 外缘 1.18 处微晕渐隐。
        /// 圆形罩全身，替代原网格贴图护盾（shield_* 网格观感不符大众认知）。</summary>
        private static float ArcaneShieldAt(int x, int y, int w, int h)
        {
            float dx = x - (w - 1) * 0.5f;
            float dy = y - (h - 1) * 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float nd = dist / ((w - 1) * 0.5f);   // 0 中心 ~ 1 边缘
            if (nd > 1.18f) return 0f;
            if (nd < 0.78f) return 0.12f;                                   // 内部淡膜
            if (nd < 1.0f) return 0.12f + 0.48f * (nd - 0.78f) / 0.22f;    // 光边渐强
            return 0.60f * (1.18f - nd) / 0.18f;                            // 外缘微晕渐隐
        }

        // ---------------------------------------------------------- 元素细分图元（v0.19.8）--

        /// <summary>水波：内外双圈扩散环 + 少量水珠点（水系意象，圆形扩散不旋转）。</summary>
        private static float WaterWaveAt(int x, int y, int w, int h)
        {
            float dx = x - (w - 1) * 0.5f;
            float dy = y - (h - 1) * 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float nd = dist / ((w - 1) * 0.5f);
            // 外圈（0.62~0.95）+ 内圈（0.30~0.45）扩散环，柔边
            float a = 0f;
            if (nd > 0.62f && nd < 0.95f) a = 0.7f * Mathf.Min(1f, 0.95f - nd) * Mathf.Min(1f, nd - 0.62f) * 9f;
            if (nd > 0.30f && nd < 0.45f) a = Mathf.Max(a, 0.5f * Mathf.Min(1f, 0.45f - nd) * Mathf.Min(1f, nd - 0.30f) * 9f);
            // 水珠点
            if (nd < 0.22f) { float s = Mathf.Sin(x * 7.31f + y * 13.7f) * 0.5f + 0.5f; if (s > 0.75f) a = Mathf.Max(a, 0.4f); }
            return Mathf.Clamp01(a);
        }

        /// <summary>风刃：双交叉弧形刃（绕中心 45° 双弧，风系切割意象）。</summary>
        private static float WindBladeAt(int x, int y, int w, int h)
        {
            float dx = x - (w - 1) * 0.5f;
            float dy = y - (h - 1) * 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > (w - 1) * 0.48f) return 0f;
            // 双弧：|dx|≈|dy| 斜带（两条 45° 交叉弧）
            float u = Mathf.Abs(dx - dy);
            float v = Mathf.Abs(dx + dy);
            float a = 0f;
            // 弧带 1
            if (u < 2.6f && dist > 4f) a = Mathf.Max(a, 1f - u / 2.6f);
            // 弧带 2（垂直交叉）
            if (v < 2.6f && dist > 4f) a = Mathf.Max(a, 1f - v / 2.6f);
            // 边缘柔化
            float edge = (w - 1) * 0.48f - dist;
            if (edge < 2f) a *= Mathf.Clamp01(edge);
            return Mathf.Clamp01(a * 0.9f);
        }

        /// <summary>雷闪：主锯齿闪电（Z 字形折线，雷系击穿意象）。</summary>
        private static float ThunderBoltAt(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f;
            float cy = (h - 1) * 0.5f;
            float t = (x - cx) / (w - 1) * 2f;      // -1~1
            float yy = (y - cy) / (h - 1) * 2f;     // -1~1
            // 主闪电：x 从 -0.3 到 0.3 的锯齿折线，粗 1.6px
            float a = 0f;
            float target = 0.5f * Mathf.Sin(t * 9f + 1.3f) * Mathf.Clamp01(1f - Mathf.Abs(t) * 1.5f);
            if (Mathf.Abs(t) < 0.45f)
            {
                float d = Mathf.Abs(yy - target);
                if (d < 0.08f) a = Mathf.Max(a, 1f - d / 0.08f);
            }
            // 分支小叉
            float bt = 0.22f;
            if (Mathf.Abs(t - bt) < 0.1f)
            {
                float d = Mathf.Abs(yy - (target + 0.28f));
                if (d < 0.05f) a = Mathf.Max(a, 1f - d / 0.05f);
            }
            if (Mathf.Abs(t + bt) < 0.1f)
            {
                float d = Mathf.Abs(yy - (target - 0.28f));
                if (d < 0.05f) a = Mathf.Max(a, 1f - d / 0.05f);
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>土块：碎石岩块（随机斑点团，土系厚重意象）。</summary>
        private static float EarthRockAt(int x, int y, int w, int h)
        {
            float dx = x - (w - 1) * 0.5f;
            float dy = y - (h - 1) * 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > (w - 1) * 0.46f) return 0f;
            // 主体圆团 + 棱角（用菱形距离）
            float q = Mathf.Abs(dx) + Mathf.Abs(dy);
            float a = Mathf.Clamp01(1f - q / ((w - 1) * 0.46f));
            if (a < 0.05f) return 0f;
            // 岩缝（暗线）：q≈某值的窄缝
            float crack = Mathf.Abs(q - (w - 1) * 0.28f);
            if (crack < 1.2f) a *= 0.35f;
            return Mathf.Clamp01(a * 0.85f);
        }

        /// <summary>金属斩刃：月牙金属刃弧（金系锐利意象，与御剑 Blade 区分：更宽弧刃）。</summary>
        private static float MetalSlashAt(int x, int y, int w, int h)
        {
            float dx = x - (w - 1) * 0.5f;
            float dy = y - (h - 1) * 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            // 月牙弧：半径带 + 角度带
            if (dist < 6f || dist > (w - 1) * 0.48f) return 0f;
            float ang = Mathf.Atan2(dy, dx);       // -π~π
            float a = 0f;
            // 主弧：角度在 [0.15π, 0.62π]（右上方月牙）
            if (ang > 0.15f * Mathf.PI && ang < 0.62f * Mathf.PI)
            {
                float edge = Mathf.Min(dist - 6f, (w - 1) * 0.48f - dist);
                a = Mathf.Clamp01(edge / 3f) * 0.95f;
            }
            return a;
        }

        /// <summary>圣光：竖向光柱 + 顶部光环（光明系神圣意象）。</summary>
        private static float HolyLightAt(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f;
            float dx = x - cx;
            float dy = y - (h - 1) * 0.5f;
            float a = 0f;
            // 光柱（中央窄带，纵向渐隐）
            if (Mathf.Abs(dx) < 4f)
            {
                float v = Mathf.Clamp01((h - 1) * 0.48f - Mathf.Abs(dy)) / ((h - 1) * 0.48f);
                a = Mathf.Max(a, (1f - Mathf.Abs(dx) / 4f) * v * 0.9f);
            }
            // 顶部光环（半径 8~13 的圆环）
            float dist = Mathf.Sqrt(dx * dx + (dy + (h - 1) * 0.42f) * (dy + (h - 1) * 0.42f));
            if (dist > 7f && dist < 13f)
            {
                float edge = Mathf.Min(dist - 7f, 13f - dist);
                a = Mathf.Max(a, Mathf.Clamp01(edge / 2.5f) * 0.8f);
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>暗影波：中心暗核 + 外扩暗波（黑暗系吞噬意象，圆形扩散）。</summary>
        private static float ShadowWaveAt(int x, int y, int w, int h)
        {
            float dx = x - (w - 1) * 0.5f;
            float dy = y - (h - 1) * 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float nd = dist / ((w - 1) * 0.5f);
            if (nd > 1.0f) return 0f;
            float a = 0f;
            // 暗核（中心实心，柔和）
            if (nd < 0.28f) a = 0.95f * (1f - nd / 0.28f);
            // 外扩波环（0.45~0.85 双环）
            if (nd > 0.45f && nd < 0.60f) a = Mathf.Max(a, 0.6f * Mathf.Min(1f, 0.60f - nd) * 8f);
            if (nd > 0.68f && nd < 0.85f) a = Mathf.Max(a, 0.4f * Mathf.Min(1f, 0.85f - nd) * 8f);
            return Mathf.Clamp01(a);
        }

        /// <summary>十字闪光：细十字。</summary>
        private static float Cross(int x, int y, int w, int h)
        {
            float dx = Mathf.Abs(x - (w - 1) * 0.5f);
            float dy = Mathf.Abs(y - (h - 1) * 0.5f);
            if (dx <= 0.8f || dy <= 0.8f) return 1f;
            return 0f;
        }

        // ---------------------------------------------------------- 战斗动作图元（v0.18.70）--

        /// <summary>确定性线性 hash（[0,1)）。</summary>
        private static float LinHash(int seed)
        {
            float h = Mathf.Sin(seed * 12.9898f + 78.233f) * 43758.5453f;
            h = h - Mathf.Floor(h);
            return h;
        }

        /// <summary>点到线段距离。</summary>
        private static float SegDist(int x, int y, float ax, float ay, float bx, float by)
        {
            float px = x, py = y;
            float abx = bx - ax, aby = by - ay;
            float len2 = abx * abx + aby * aby;
            float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / len2);
            float cx = ax + abx * t, cy = ay + aby * t;
            float dx = px - cx, dy = py - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>月牙剑弧（御剑横扫）：环形段（角度扇区 ±0.55rad），旋转交给粒子。</summary>
        private static float SlashAt(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float r = (w - 1) * 0.5f * 0.78f;
            float lo = r * 0.42f, hi = r * 0.72f;
            if (dist < lo - 1f || dist > hi) return 0f;
            float a = Mathf.Atan2(dy, dx);
            if (a < -0.55f || a > 0.55f) return 0f;   // 弧段朝右，旋转/翻转由粒子控制
            float alpha = 1f;
            if (dist < lo) alpha = dist - (lo - 1f);
            if (dist > hi - 1f) alpha = hi - dist;
            // 两端渐隐
            float edge = Mathf.Abs(a) > 0.42f ? (0.55f - Mathf.Abs(a)) / 0.13f : 1f;
            return alpha * Mathf.Clamp01(edge);
        }

        /// <summary>飞剑/剑气（长菱形条）：|dx|+3|dy| 窄条，两端尖。</summary>
        private static float BladeAt(int x, int y, int w, int h)
        {
            float dx = Mathf.Abs(x - (w - 1) * 0.5f);
            float dy = Mathf.Abs(y - (h - 1) * 0.5f);
            float t = (w - 1) * 0.5f;
            return Mathf.Clamp01(t + 0.8f - (dx + 3f * dy));
        }

        /// <summary>锯齿闪电（横向折线）：4 段确定性折点，线宽 1.4px。</summary>
        private static float ZapAt(int x, int y, int w, int h)
        {
            float cy = (h - 1) * 0.5f;
            float x0 = 0f, y0 = cy;
            float amp = (h - 1) * 0.5f - 1.5f;
            float seg = (w - 1) / 4f;
            float min = 10f;
            for (int i = 1; i <= 4; i++)
            {
                float nx = seg * i;
                float ny = cy + (LinHash(i * 7 + 13) - 0.5f) * 2f * amp * (i == 4 ? 0.2f : 1f);
                min = Mathf.Min(min, SegDist(x, y, x0, y0, nx, ny));
                x0 = nx; y0 = ny;
            }
            return min <= 1.4f ? 1f : 0f;
        }

        /// <summary>地面放射裂纹：中心 6 条射线（角度 hash 抖动，线长 11~19px）。</summary>
        private static float CrackAt(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float min = 10f;
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + (LinHash(i + 3) - 0.5f) * 22f) * Mathf.Deg2Rad;
                float len = 11f + LinHash(i + 9) * 8f;
                float ex = cx + Mathf.Cos(a) * len;
                float ey = cy + Mathf.Sin(a) * len;
                min = Mathf.Min(min, SegDist(x, y, cx, cy, ex, ey));
            }
            // 中心点实心
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return (min <= 1.5f || d <= 2.2f) ? 1f : 0f;
        }

        /// <summary>符文法阵环：粗环 + 圆周 12 刻度（领域展开）。</summary>
        private static float RuneRingAt(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float r = (w - 1) * 0.5f * 0.88f;
            float lo = r * 0.82f, hi = r * 0.94f;
            if (dist >= lo - 1f && dist <= hi)
            {
                float alpha = 1f;
                if (dist < lo) alpha = dist - (lo - 1f);
                if (dist > hi - 1f) alpha = hi - dist;
                if (alpha > 0f)
                {
                    float a = Mathf.Atan2(dy, dx);
                    // 12 刻度：环上凸起短条
                    float step = Mathf.PI * 2f / 12f;
                    float k = a / step;
                    float kf = k - Mathf.Floor(k);
                    float spread = Mathf.Min(kf, 1f - kf) * step;
                    if (spread < 0.14f) return alpha;
                    return alpha * 0.55f;   // 环体略暗，刻度亮
                }
            }
            return 0f;
        }

        // ---- v0.18.74 中式重生成图元函数 ----

        /// <summary>魂焰：竖向火苗，外焰半透明、内芯实亮（y 越靠上越收尖）。</summary>
        private static float SoulFlameAt(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.62f;
            float dx = (x - cx) / (w * 0.36f), dy = (y - cy) / (h * 0.32f);
            float top = (cy - y) / (h * 0.5f);          // 顶部收尖
            float squash = 1f + Mathf.Max(0f, top) * 1.5f;
            float dd = Mathf.Sqrt(dx * dx * squash * squash + dy * dy);
            float outer = 1f - Mathf.Clamp01(dd);
            if (outer <= 0f) return 0f;
            // 内芯：火苗下部核心更亮
            float inner = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx * 0.9f + dy * dy * 1.5f));
            if (inner > 0f) return 0.95f;
            return outer * 0.45f;
        }

        /// <summary>风痕：双交叉细弧（X 形风刃），中心交叉处留空。</summary>
        private static float WindArcAt(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float s = (w - 1) * 0.42f;
            float d1 = SegDist(x, y, cx - s, cy + s * 0.55f, cx + s, cy - s * 0.55f);
            float d2 = SegDist(x, y, cx - s, cy - s * 0.55f, cx + s, cy + s * 0.55f);
            float a = Mathf.Min(d1, d2);
            float t = (w - 1) * 0.075f;
            if (a > t) return 0f;
            float alpha = 1f - a / t;
            // 中心留空（两弧交叉点）
            float cdist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float gap = (w - 1) * 0.13f;
            if (cdist < gap) alpha *= cdist / gap;
            return alpha * 0.9f;
        }

        /// <summary>木藤：S 形藤蔓曲线 + 周期叶片凸点。</summary>
        private static float WoodVineAt(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f;
            float t = y / (h - 1f);
            float amp = (w - 1) * 0.30f;
            float curveX = cx + Mathf.Sin(t * Mathf.PI * 2f) * amp;
            float dx = x - curveX;
            float th = (w - 1) * 0.08f;
            float alpha = Mathf.Clamp01(1f - Mathf.Abs(dx) / th);
            if (alpha <= 0f) return 0f;
            // 叶片：藤线两侧的周期凸点
            float leaf = Mathf.Max(0f, Mathf.Sin(t * 8f * Mathf.PI));
            float side = Mathf.Sin(t * 8f * Mathf.PI * 0.5f) > 0f ? 1f : -1f;
            if (leaf > 0.8f)
            {
                float ldx = Mathf.Abs(dx) - th;
                float lr = 1.6f + leaf * 2.4f;
                if (ldx >= 0f && ldx <= lr)
                {
                    float la = 1f - ldx / lr;
                    alpha = Mathf.Max(alpha, la * 0.85f);
                }
            }
            return alpha;
        }

        // ---- v0.19.19 程序化神形显现图元函数 ----

        /// <summary>神形显现（金色半透明神祇虚影，v0.19.19 纯几何自生成，零素材依赖）：
        /// 背后放射神光（中心向外的细射线，烘托神圣）→ 头顶光环（完整细环，贴头上方）
        /// → 圆首 → 长袍剪影（肩宽下摆喇叭形）→ 两侧展开光翼（斜光带）。
        /// 全部白色生成、alpha 分段给值；粒子端用金色渐变+上升呈现"虚影"，不旋转。</summary>
        private static float DivineVisionAt(int x, int y, int w, int h)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float W = (w - 1), H = (h - 1);
            float a = 0f;

            // v0.21.5 弱化"人形贴图"感：头/袍/翼主体降为 50% 半透明光体，
            // 放射神光与光环提亮——整体观感从"人形剪影"转向"神性光晕"（登神时的光之化身，而非单位贴图）。
            // 背后放射神光：中心 (cx, cy+0.16H) 向外 10 条细射线（神性光晕，提亮）
            float rx = cx, ry = cy + 0.16f * H;
            float dRay = Mathf.Sqrt((x - rx) * (x - rx) + (y - ry) * (y - ry));
            float rayR = 0.44f * H;
            if (dRay < rayR && dRay > 2f)
            {
                float ang = Mathf.Atan2(y - ry, x - rx);
                float step = Mathf.PI * 2f / 10f;
                float k = ang / step - Mathf.Floor(ang / step);
                float spread = Mathf.Min(k, 1f - k) * step;
                if (spread < 0.09f)
                    a = Mathf.Max(a, 0.48f * (1f - dRay / rayR));
            }

            // 头顶光环（神性光轮）：圆心 (cx, cy-0.36H)，环带 0.82r~r，贴头上缘不压头
            float hx = cx, hy = cy - 0.36f * H;
            float haloR = 0.13f * H;
            float dHalo = Mathf.Sqrt((x - hx) * (x - hx) + (y - hy) * (y - hy));
            if (dHalo > haloR * 0.82f && dHalo < haloR)
            {
                float edge = Mathf.Min(dHalo - haloR * 0.82f, haloR - dHalo);
                a = Mathf.Max(a, Mathf.Clamp01(edge / 1.5f) * 0.8f);
            }

            // 头：圆 (cx, cy-0.17H) 半径 0.115H（半透明光感，非实心人形）
            float headY = cy - 0.17f * H;
            float headR = 0.115f * H;
            float dHead = Mathf.Sqrt((x - cx) * (x - cx) + (y - headY) * (y - headY));
            if (dHead < headR) a = Mathf.Max(a, 0.30f * (1f - dHead / headR * 0.35f));

            // 长袍剪影：梯形（肩 y=+0.07H 半宽 0.18W → 下摆 y=+0.34H 半宽 0.26W），喇叭形（半透明光衣）
            float ty = (y - cy) / H;
            if (ty > 0.07f && ty < 0.34f)
            {
                float k = (ty - 0.07f) / 0.27f;          // 0~1（上→下张开）
                float halfW = W * 0.5f * (0.18f + 0.08f * k);
                if (Mathf.Abs(x - cx) < halfW)
                {
                    float edge = halfW - Mathf.Abs(x - cx);
                    a = Mathf.Max(a, Mathf.Clamp01(edge / 2f) * 0.26f);
                }
            }

            // 展开光翼（双臂）：两条斜光带 (cx±0.20W, cy+0.10H)→(cx±0.42W, cy+0.30H)，线宽 0.055W（降透明度）
            float wx1 = cx - 0.20f * W, wy1 = cy + 0.10f * H;
            float wx2 = cx - 0.42f * W, wy2 = cy + 0.30f * H;
            float dWing1 = SegDist(x, y, wx1, wy1, wx2, wy2);
            float dWing2 = SegDist(x, y, cx + 0.20f * W, cy + 0.10f * H, cx + 0.42f * W, cy + 0.30f * H);
            float wingT = 0.055f * W;
            if (dWing1 < wingT) a = Mathf.Max(a, (1f - dWing1 / wingT) * 0.22f);
            if (dWing2 < wingT) a = Mathf.Max(a, (1f - dWing2 / wingT) * 0.22f);

            return Mathf.Clamp01(a);
        }

        // ================================================================
        // v0.21.6 异能专属形态库：26 个程序化图元，每个异能 = 主形态（MainTag）
        // × 副形态（SubTag 点缀）× id 哈希 seed（形变参数）× 专属派生色。
        // 全部 64×64、白膜 alpha（颜色由 AbilityFx.DeriveTint 运行时叠加）。
        // seed 只改变形变细节（尖角数/环数/刃数/弯曲度），不改变大众认知意象。
        // ================================================================

        /// <summary>火苗：下宽上尖焰体，seed 控制焰尖摆动角度。</summary>
        private static float GlyphFlameAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float bend = ((seed % 5) - 2) * 0.06f;           // 焰尖左右摆
            float a = 0f;
            for (int i = -1; i <= 1; i++)
            {
                float fx = cx + i * 7f * ((seed + i) % 3 == 0 ? 0.5f : 1f);
                float tipX = fx + bend * 18f;
                float tipY = cy - 20f - ((seed + i) % 2) * 4f;
                float baseY = cy + 14f;
                float seg = SegDist(x, y, fx, baseY, tipX, tipY);
                float thick = 5.5f - i * 1.2f;
                if (seg < thick) a = Mathf.Max(a, (1f - seg / thick) * 0.95f);
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>六角冰晶：主棱×6 + 次级棱，seed 控制次级棱长度。</summary>
        private static float GlyphIceCrystalAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float R = 22f + (seed % 3) * 2f;
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI / 3f + ((seed % 2) * 0.05f);
                float ex = cx + Mathf.Cos(ang) * R, ey = cy + Mathf.Sin(ang) * R;
                a = Mathf.Max(a, SegGlow(x, y, cx, cy, ex, ey, 2.6f));
                // 次级棱（v 形分支）
                float bx = cx + Mathf.Cos(ang) * R * 0.55f, by = cy + Mathf.Sin(ang) * R * 0.55f;
                float off = ang + Mathf.PI / 2f;
                float br = R * (0.3f + (seed % 4) * 0.06f);
                a = Mathf.Max(a, SegGlow(x, y, bx, by, bx + Mathf.Cos(off) * br, by + Mathf.Sin(off) * br, 1.8f));
                a = Mathf.Max(a, SegGlow(x, y, bx, by, bx - Mathf.Cos(off) * br, by - Mathf.Sin(off) * br, 1.8f));
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>Z 闪电：多段折线，seed 控制折点数量。</summary>
        private static float GlyphBoltAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            int n = 3 + (seed % 3);
            float a = 0f;
            float px = cx - 10f, py = cy - 20f;
            float targetX = cx + 8f * (((seed / 3) % 2) == 0 ? -1f : 1f), targetY = cy + 22f;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float nx = px + (targetX - px) * t + (i > 0 && i < n ? Mathf.Sin(i * 9f + seed) * 6f : 0f);
                float ny = py + (targetY - py) * t;
                if (i > 0) a = Mathf.Max(a, SegGlow(x, y, px, py, nx, ny, 3.4f));
                px = nx; py = ny;
            }
            // 侧支闪电
            float sx = cx - 4f, sy = cy;
            a = Mathf.Max(a, SegGlow(x, y, sx, sy, sx - 12f, sy - 8f, 2.2f));
            return Mathf.Clamp01(a);
        }

        /// <summary>三斜刃：三片风刃绕中心，seed 控制刃夹角。</summary>
        private static float GlyphWindEdgeAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float baseAng = (seed % 6) * 0.12f;
            for (int i = 0; i < 3; i++)
            {
                float ang = baseAng + i * Mathf.PI * 2f / 3f;
                float sx = cx + Mathf.Cos(ang) * 6f, sy = cy + Mathf.Sin(ang) * 6f;
                float ex = cx + Mathf.Cos(ang) * 24f, ey = cy + Mathf.Sin(ang) * 24f;
                a = Mathf.Max(a, SegGlow(x, y, sx, sy, ex, ey, 4f));
                // 刃尖三角
                float txa = ex + Mathf.Cos(ang + 0.5f) * 6f, tya = ey + Mathf.Sin(ang + 0.5f) * 6f;
                float txb = ex + Mathf.Cos(ang - 0.5f) * 6f, tyb = ey + Mathf.Sin(ang - 0.5f) * 6f;
                a = Mathf.Max(a, TriGlow(x, y, ex, ey, txa, tya, txb, tyb));
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>波纹+水滴：同心波纹与一滴水珠，seed 控制波数。</summary>
        private static float GlyphWaveAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            int rings = 2 + (seed % 2);
            for (int i = 1; i <= rings; i++)
            {
                float r = 10f + i * 7f + (seed % 3);
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy + 6f) * (y - cy + 6f));
                float edge = Mathf.Abs(d - r);
                if (edge < 2.2f) a = Mathf.Max(a, (1f - edge / 2.2f) * 0.8f);
            }
            // 水滴（泪形）
            float dx = cx - 8f + (seed % 3), dy = cy - 14f;
            float dDrop = Mathf.Sqrt((x - dx) * (x - dx) + (y - dy) * (y - dy));
            if (dDrop < 6f) a = Mathf.Max(a, 0.9f * (1f - dDrop / 6f * 0.4f));
            float seg2 = SegDist(x, y, dx, dy + 3f, dx + 4f, dy + 12f);
            if (seg2 < 3f) a = Mathf.Max(a, (1f - seg2 / 3f) * 0.8f);
            return Mathf.Clamp01(a);
        }

        /// <summary>菱形岩：粗棱菱形+内裂，seed 控制棱角。</summary>
        private static float GlyphRockAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float skew = ((seed % 5) - 2) * 0.08f;
            float topX = cx + skew * 10f;
            float d1 = SegDist(x, y, topX, cy - 22f, cx - 18f, cy + 6f);
            float d2 = SegDist(x, y, topX, cy - 22f, cx + 18f, cy + 6f);
            float d3 = SegDist(x, y, cx - 18f, cy + 6f, cx + 8f, cy + 22f);
            float d4 = SegDist(x, y, cx + 18f, cy + 6f, cx - 8f, cy + 22f);
            float t = 3.2f;
            a = Mathf.Max(Mathf.Max(a, 1f - Mathf.Min(d1, d3) / t), 1f - Mathf.Min(d2, d4) / t);
            // 内裂
            float cr = ((seed / 5) % 3) * 0.3f + 0.5f;
            a = Mathf.Max(a, SegGlow(x, y, cx - cr * 8f, cy - cr * 6f, cx + cr * 6f, cy + cr * 4f, 2f));
            return Mathf.Clamp01(a);
        }

        /// <summary>冷刃弧：双刃弧形交错，seed 控制弧向。</summary>
        private static float GlyphMetalEdgeAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            int dir = (seed % 2) == 0 ? 1 : -1;
            // 两道弧刃（贝塞尔式：用多段折线近似）
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f;
                float ang = -1.1f + t * 2.2f;
                float px1 = cx + Mathf.Cos(ang) * (14f + 6f * t) * dir, py1 = cy + Mathf.Sin(ang) * 14f - 6f * t;
                float px2 = cx + Mathf.Cos(-ang) * (14f + 6f * t) * dir, py2 = cy - Mathf.Sin(-ang) * 14f + 6f * t;
                if (i > 0)
                {
                    a = Mathf.Max(a, SegGlow(x, y, prev1x, prev1y, px1, py1, 3f));
                    a = Mathf.Max(a, SegGlow(x, y, prev2x, prev2y, px2, py2, 3f));
                }
                prev1x = px1; prev1y = py1; prev2x = px2; prev2y = py2;
            }
            return Mathf.Clamp01(a);
        }
        private static float prev1x, prev1y, prev2x, prev2y;   // 弧线折点暂存

        /// <summary>光芒星：核心星芒（8~16 芒），seed 控制芒数。</summary>
        private static float GlyphHolyRayAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            int rays = 8 + (seed % 9);
            float R = 26f;
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d < 7f) a = Mathf.Max(a, 1f - d / 7f);
            for (int i = 0; i < rays; i++)
            {
                float ang = i * Mathf.PI * 2f / rays + ((seed % 4) * 0.07f);
                float ex = cx + Mathf.Cos(ang) * R, ey = cy + Mathf.Sin(ang) * R;
                a = Mathf.Max(a, SegGlow(x, y, cx, cy, ex, ey, 2.4f));
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>暗弧+瞳：弧带环抱瞳点，seed 控制瞳位。</summary>
        private static float GlyphShadowArcAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                float ang = 0.3f + t * (Mathf.PI * 2f - 0.6f);
                float rx = 20f + (seed % 3) * 2f, ry = 12f + ((seed / 3) % 2) * 3f;
                float px = cx + Mathf.Cos(ang) * rx, py = cy + Mathf.Sin(ang) * ry;
                if (i > 0) a = Mathf.Max(a, SegGlow(x, y, prevArcX, prevArcY, px, py, 2.8f));
                prevArcX = px; prevArcY = py;
            }
            // 瞳（中心竖瞳）
            float dx = cx + ((seed % 5) - 2) * 2f, dy = cy;
            float dEye = Mathf.Sqrt((x - dx) * (x - dx) + ((y - dy) * 0.45f) * ((y - dy) * 0.45f));
            if (dEye < 6f) a = Mathf.Max(a, 0.95f * (1f - dEye / 6f * 0.3f));
            return Mathf.Clamp01(a);
        }
        private static float prevArcX, prevArcY;

        /// <summary>血滴：悬垂血滴+溅点，seed 控制滴数。</summary>
        private static float GlyphBloodDropAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            int drops = 2 + (seed % 2);
            for (int i = 0; i < drops; i++)
            {
                float dx = cx + (i == 0 ? 0f : ((seed % 3) - 1) * 14f);
                float dy = cy - (i == 0 ? 6f : -6f);
                float d = Mathf.Sqrt((x - dx) * (x - dx) + (y - dy) * (y - dy));
                if (d < 8f) a = Mathf.Max(a, 0.9f * (1f - d / 8f * 0.35f));
                // 滴尾
                float seg = SegDist(x, y, dx, dy + 2f, dx + 2f, dy + 13f);
                if (seg < 3f) a = Mathf.Max(a, (1f - seg / 3f) * 0.85f);
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>骨镰：镰刃弧+柄，seed 控制刃弯。</summary>
        private static float GlyphBoneScytheAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float bend = 0.15f + (seed % 4) * 0.05f;
            // 刃弧（上半圆弧）
            for (int i = 0; i <= 18; i++)
            {
                float t = i / 18f;
                float ang = Mathf.PI * (0.15f + t * 0.7f);
                float px = cx + Mathf.Cos(ang) * 22f, py = cy - 8f + Mathf.Sin(ang) * 20f;
                if (i > 0) a = Mathf.Max(a, SegGlow(x, y, prevSx, prevSy, px, py, 3.4f));
                prevSx = px; prevSy = py;
            }
            // 柄
            float hx1 = cx - 14f, hy1 = cy + 14f, hx2 = cx + 16f, hy2 = cy - 6f;
            a = Mathf.Max(a, SegGlow(x, y, hx1, hy1, hx2, hy2, 2.8f));
            return Mathf.Clamp01(a);
        }
        private static float prevSx, prevSy;

        /// <summary>心：经典心形，seed 控制比例。</summary>
        private static float GlyphHeartAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float k = 0.8f + (seed % 3) * 0.1f;    // 心尖长度
            float nx = (x - cx) / 24f, ny = (y - cy) / 24f;
            // 隐式心形曲线 x²+(y-k√|x|)²=1 的近似：到曲面的距离
            float fx = Mathf.Abs(nx);
            float yy = ny * 1.15f - k * 0.35f * Mathf.Sqrt(fx);
            float curve = fx * fx + yy * yy;
            if (curve < 1.15f)
            {
                float inside = 1.15f - curve;
                if (inside > 0f && inside < 0.25f) a = Mathf.Max(a, inside / 0.25f);
                else if (inside >= 0.25f) a = Mathf.Max(a, 0.9f);
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>眼瞳：眼型+瞳孔，seed 控制瞳偏移。</summary>
        private static float GlyphEyeAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float ex = (x - cx) / 20f, ey = (y - cy) / 12f;
            float d = ex * ex + ey * ey;
            if (d < 1f)
            {
                float ring = Mathf.Abs(d - 0.72f);
                if (ring < 0.14f) a = Mathf.Max(a, (0.14f - ring) / 0.14f);
                else if (d < 0.72f) a = Mathf.Max(a, 0.6f);
                else a = Mathf.Max(a, 0.15f);
            }
            // 瞳孔
            float pox = cx + ((seed % 5) - 2) * 3f, poy = cy + ((seed / 5) % 3 - 1) * 3f;
            float pd = Mathf.Sqrt((x - pox) * (x - pox) + (y - poy) * (y - poy));
            if (pd < 5f) a = Mathf.Max(a, 0.95f * (1f - pd / 5f * 0.2f));
            return Mathf.Clamp01(a);
        }

        /// <summary>爪印：三道爪痕，seed 控制间距。</summary>
        private static float GlyphClawAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float gap = 6f + (seed % 3) * 1.5f;
            for (int i = -1; i <= 1; i++)
            {
                float bx = cx + i * gap, by = cy + 16f;
                float ex = bx + (i * 3f) + ((seed % 2) * 2f), ey = cy - 14f - Mathf.Abs(i) * 3f;
                a = Mathf.Max(a, SegGlow(x, y, bx, by, ex, ey, 4f));
                // 爪尖三角
                a = Mathf.Max(a, TriGlow(x, y, ex, ey, ex + 5f, ey + 4f, ex - 3f, ey + 6f));
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>沙漏：双三角+中腰，seed 控制沙量。</summary>
        private static float GlyphHourglassAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float topX = cx, topY = cy - 20f, botX = cx, botY = cy + 20f;
            a = Mathf.Max(a, SegGlow(x, y, topX, topY, cx - 14f, cy, 3f));
            a = Mathf.Max(a, SegGlow(x, y, topX, topY, cx + 14f, cy, 3f));
            a = Mathf.Max(a, SegGlow(x, y, cx - 14f, cy, botX, botY, 3f));
            a = Mathf.Max(a, SegGlow(x, y, cx + 14f, cy, botX, botY, 3f));
            // 沙（下半填充点）
            float sandY = cy + 4f + (seed % 4) * 2f;
            for (int sy = (int)cy + 6; sy < (int)(cy + 18f); sy += 4)
            {
                float half = 14f * (sy - cy) / 20f;
                if (Mathf.Abs(y - sy) < 2f && Mathf.Abs(x - cx) < half) a = Mathf.Max(a, 0.5f);
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>月牙：新月弧，seed 控制弯度。</summary>
        private static float GlyphMoonAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float bend = 10f + (seed % 3) * 2f;
            // 外弧与内弧之间的月牙带
            float dOut = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float innerCx = cx + bend, innerCy = cy;
            float dIn = Mathf.Sqrt((x - innerCx) * (x - innerCx) + (y - innerCy) * (y - innerCy));
            if (dOut < 20f && dIn > 12f)
            {
                float band = Mathf.Min(20f - dOut, dIn - 12f);
                if (band > 0f && band < 4f) a = Mathf.Max(a, band / 4f);
                else if (band >= 4f) a = Mathf.Max(a, 0.85f);
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>毒滴+气泡：毒液滴与升腾气泡，seed 控制泡数。</summary>
        private static float GlyphToxicAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d < 10f) a = Mathf.Max(a, 0.75f * (1f - d / 10f * 0.3f));
            // 升腾气泡
            int bubbles = 2 + (seed % 3);
            for (int i = 0; i < bubbles; i++)
            {
                float bx = cx + ((seed + i * 7) % 21) - 10f;
                float by = cy - 8f - ((seed + i * 13) % 16);
                float bd = Mathf.Sqrt((x - bx) * (x - bx) + (y - by) * (y - by));
                if (bd < 3.5f) a = Mathf.Max(a, 0.6f * (1f - bd / 3.5f));
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>雾团：多斑雾云，seed 控制斑点数。</summary>
        private static float GlyphVaporAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            int blobs = 3 + (seed % 3);
            for (int i = 0; i < blobs; i++)
            {
                float bx = cx + ((seed + i * 11) % 25) - 12f;
                float by = cy + ((seed + i * 17) % 19) - 9f;
                float r = 7f + ((seed + i * 3) % 5);
                float d = Mathf.Sqrt((x - bx) * (x - bx) + (y - by) * (y - by));
                if (d < r) a = Mathf.Max(a, 0.4f * (1f - d / r));
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>链环：双环相扣，seed 控制环径。</summary>
        private static float GlyphChainAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float r = 10f + (seed % 3) * 2f;
            float d1 = Mathf.Sqrt((x - cx + r * 0.5f) * (x - cx + r * 0.5f) + (y - cy) * (y - cy));
            float d2 = Mathf.Sqrt((x - cx - r * 0.5f) * (x - cx - r * 0.5f) + (y - cy) * (y - cy));
            float w1 = Mathf.Abs(d1 - r);
            float w2 = Mathf.Abs(d2 - r);
            if (w1 < 2.6f) a = Mathf.Max(a, (1f - w1 / 2.6f) * 0.9f);
            if (w2 < 2.6f) a = Mathf.Max(a, (1f - w2 / 2.6f) * 0.9f);
            return Mathf.Clamp01(a);
        }

        /// <summary>光翼：展翅双翼，seed 控制翼展。</summary>
        private static float GlyphWingAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float span = 20f + (seed % 3) * 4f;
            // 左翼（肩→翼尖，羽状三层）
            for (int i = 0; i < 3; i++)
            {
                float y0 = cy - 6f + i * 6f;
                float x1 = cx - span, y1 = cy - 12f + i * 8f;
                a = Mathf.Max(a, SegGlow(x, y, cx - 2f, y0, x1, y1, 3f - i * 0.5f));
            }
            for (int i = 0; i < 3; i++)
            {
                float y0 = cy - 6f + i * 6f;
                float x1 = cx + span, y1 = cy - 12f + i * 8f;
                a = Mathf.Max(a, SegGlow(x, y, cx + 2f, y0, x1, y1, 3f - i * 0.5f));
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>星芒：粗星（五角星形），seed 控制星角。</summary>
        private static float GlyphStarAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float r = 22f;
            int pts = 5 + (seed % 2) * 2;    // 5 或 7 角星
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d > r) return 0f;
            float theta = Mathf.Atan2(y - cy, x - cx) + Mathf.PI / 2f;
            float step = Mathf.PI * 2f / pts;
            float k = theta / step - Mathf.Floor(theta / step);
            float inSeg = Mathf.Min(k, 1f - k) * step;   // 0~step/2
            float halfStep = step * 0.5f;
            float inR = r * (0.62f + (seed % 4) * 0.03f);
            float edge = inR + (r - inR) * (inSeg / halfStep);
            if (d < edge) a = Mathf.Max(a, 0.9f);
            return Mathf.Clamp01(a);
        }

        /// <summary>符环：外环+符文短线，seed 控制符数。</summary>
        private static float GlyphSigilAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float r = 22f;
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float edge = Mathf.Abs(d - r);
            if (edge < 3f) a = Mathf.Max(a, (1f - edge / 3f) * 0.9f);
            // 内符短线
            int glyphs = 4 + (seed % 4);
            for (int i = 0; i < glyphs; i++)
            {
                float ang = i * Mathf.PI * 2f / glyphs + (seed % 7) * 0.09f;
                float bx = cx + Mathf.Cos(ang) * (r - 7f), by = cy + Mathf.Sin(ang) * (r - 7f);
                float ex = cx + Mathf.Cos(ang) * (r - 2f), ey = cy + Mathf.Sin(ang) * (r - 2f);
                a = Mathf.Max(a, SegGlow(x, y, bx, by, ex, ey, 2.2f));
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>冲击双环：两同心环，seed 控制环距。</summary>
        private static float GlyphRippleAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float r1 = 15f, r2 = 15f + 8f + (seed % 3);
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float e1 = Mathf.Abs(d - r1), e2 = Mathf.Abs(d - r2);
            if (e1 < 2.6f) a = Mathf.Max(a, (1f - e1 / 2.6f) * 0.9f);
            if (e2 < 2.6f) a = Mathf.Max(a, (1f - e2 / 2.6f) * 0.7f);
            return Mathf.Clamp01(a);
        }

        /// <summary>盾形：圆盾+中脊，seed 控制盾宽。</summary>
        private static float GlyphShieldAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            // 盾外缘（下半圆+上收）
            float nx = (x - cx) / 22f, ny = (y - cy) / 24f;
            float d = nx * nx + ny * ny;
            if (d < 1.05f)
            {
                float band = Mathf.Abs(d - 0.8f);
                if (band < 0.2f) a = Mathf.Max(a, (0.2f - band) / 0.2f);
                else if (d < 0.8f) a = Mathf.Max(a, 0.25f);
            }
            // 中脊
            float seg = SegDist(x, y, cx, cy - 20f, cx, cy + 20f);
            if (seg < 3f) a = Mathf.Max(a, (1f - seg / 3f) * 0.8f);
            return Mathf.Clamp01(a);
        }

        /// <summary>藤蔓：卷曲藤枝，seed 控制卷数。</summary>
        private static float GlyphVineAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float px = cx - 12f, py = cy + 18f;
            for (int i = 1; i <= 10; i++)
            {
                float t = i / 10f;
                float nx = cx - 12f + t * 24f + Mathf.Sin(t * 3f + seed) * 6f;
                float ny = cy + 18f - t * 30f + Mathf.Cos(t * 2f + seed) * 4f;
                a = Mathf.Max(a, SegGlow(x, y, px, py, nx, ny, 2.6f));
                px = nx; py = ny;
            }
            return Mathf.Clamp01(a);
        }

        /// <summary>结晶：多面晶体（菱形柱+边），seed 控制切面。</summary>
        private static float GlyphPrismAt(int x, int y, int w, int h, int seed)
        {
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float a = 0f;
            float skew = ((seed % 5) - 2) * 0.1f;
            float tx = cx + skew * 8f;
            a = Mathf.Max(a, SegGlow(x, y, tx, cy - 22f, cx - 14f, cy + 16f, 3.2f));
            a = Mathf.Max(a, SegGlow(x, y, tx, cy - 22f, cx + 16f, cy + 12f, 3.2f));
            a = Mathf.Max(a, SegGlow(x, y, cx - 14f, cy + 16f, cx + 6f, cy + 22f, 3f));
            a = Mathf.Max(a, SegGlow(x, y, cx + 16f, cy + 12f, cx - 6f, cy + 22f, 3f));
            // 内切面
            a = Mathf.Max(a, SegGlow(x, y, tx, cy - 22f, cx, cy, 1.8f));
            return Mathf.Clamp01(a);
        }

        // ---- 几何助手 ----
        /// <summary>线段距离（点-线段最近距离）。</summary>
        private static float SegDist(float x, float y, float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1, dy = y2 - y1;
            float len2 = dx * dx + dy * dy;
            float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(((x - x1) * dx + (y - y1) * dy) / len2);
            float px = x1 + dx * t, py = y1 + dy * t;
            return Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
        }

        /// <summary>线段辉光（带核心亮度）。</summary>
        private static float SegGlow(float x, float y, float x1, float y1, float x2, float y2, float thick)
        {
            float d = SegDist(x, y, x1, y1, x2, y2);
            if (d > thick) return 0f;
            return Mathf.Clamp01(1f - d / thick);
        }

        /// <summary>三角形内亮度（点 P 到三角形三边最近距离衰减）。</summary>
        private static float TriGlow(float x, float y, float ax, float ay, float bx, float by, float cx2, float cy2)
        {
            float d = Mathf.Min(
                SegDist(x, y, ax, ay, bx, by),
                Mathf.Min(SegDist(x, y, bx, by, cx2, cy2), SegDist(x, y, cx2, cy2, ax, ay)));
            float t = 4f;
            if (d > t) return 0f;
            return Mathf.Clamp01(1f - d / t);
        }

        // ================================================================
        // v0.21.6 异能专属图元组合器：主形态（MainTag）× 副形态（SubTag 点缀）
        // × seed（Id 哈希）× 品质辉光。运行时懒生成 + 调用方缓存（AbilityFx）。
        // 398 异能每异能独立可辨识；同标签异能靠 seed 形变 + 副形态 + 专属色区分。
        // ================================================================

        /// <summary>生成异能专属图元精灵（白膜 alpha，颜色由 AbilityFx.DeriveTint 叠加）。</summary>
        internal static Sprite MakePrototypeGlyph(int idHash, int mainIdx, int subIdx, int tier)
        {
            try
            {
                EnsureInit();
                return Make(64, 64, (x, y) =>
                {
                    float a = GlyphMain(x, y, 64, 64, mainIdx, idHash);
                    if (subIdx >= 0)
                        a = Mathf.Max(a, GlyphSub(x, y, 64, 64, subIdx, idHash) * 0.55f);
                    // 品质辉光：高品阶整体提亮（同异能高品阶更"亮"）
                    if (tier >= 7) a = Mathf.Clamp01(a * 1.15f);
                    return a;
                });
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[CombatFxArt] PrototypeGlyph: " + e.Message);
                return null;
            }
        }

        /// <summary>主形态分发（按 PrototypeFx 主标签索引）。</summary>
        private static float GlyphMain(int x, int y, int w, int h, int idx, int seed)
        {
            switch (idx)
            {
                case 0:  return GlyphFlameAt(x, y, w, h, seed);       // 火/狂暴/等离子
                case 1:  return GlyphIceCrystalAt(x, y, w, h, seed);  // 冰/减速
                case 2:  return GlyphBoltAt(x, y, w, h, seed);        // 雷/连锁/磁力
                case 3:  return GlyphWindEdgeAt(x, y, w, h, seed);    // 风/多重
                case 4:  return GlyphWaveAt(x, y, w, h, seed);        // 水
                case 5:  return GlyphRockAt(x, y, w, h, seed);        // 土/石化
                case 6:  return GlyphMetalEdgeAt(x, y, w, h, seed);   // 金/破甲
                case 7:  return GlyphHolyRayAt(x, y, w, h, seed);     // 光/净化/祝福
                case 8:  return GlyphShadowArcAt(x, y, w, h, seed);   // 暗/诅咒/恐惧/虚弱
                case 9:  return GlyphBloodDropAt(x, y, w, h, seed);   // 血/吸血/吸能
                case 10: return GlyphBoneScytheAt(x, y, w, h, seed);  // 死亡/衰老/致死
                case 11: return GlyphHeartAt(x, y, w, h, seed);       // 治疗/再生
                case 12: return GlyphEyeAt(x, y, w, h, seed);         // 灵魂/精神
                case 13: return GlyphClawAt(x, y, w, h, seed);        // 召唤
                case 14: return GlyphHourglassAt(x, y, w, h, seed);   // 时缓
                case 15: return GlyphMoonAt(x, y, w, h, seed);        // 睡眠
                case 16: return GlyphToxicAt(x, y, w, h, seed);       // 毒
                case 17: return GlyphVaporAt(x, y, w, h, seed);       // 毒雾/衰老
                case 18: return GlyphChainAt(x, y, w, h, seed);       // 束缚
                case 19: return GlyphWingAt(x, y, w, h, seed);        // 神圣翼/降临
                case 20: return GlyphStarAt(x, y, w, h, seed);        // 暴击/强化
                case 21: return GlyphSigilAt(x, y, w, h, seed);       // 念力/领域/瞬移
                case 22: return GlyphRippleAt(x, y, w, h, seed);      // 击退/溅射/重力
                case 23: return GlyphShieldAt(x, y, w, h, seed);      // 护盾/反射/元素抗
                case 24: return GlyphVineAt(x, y, w, h, seed);        // 自然/藤蔓
                case 25: return GlyphPrismAt(x, y, w, h, seed);       // 结晶/土
                default: return GlyphStarAt(x, y, w, h, seed);
            }
        }

        /// <summary>副形态点缀（弱化叠加）。</summary>
        private static float GlyphSub(int x, int y, int w, int h, int idx, int seed)
        {
            // 副形态只取主形态库的一个子集（重复部分弱化即可，避免喧宾夺主）
            return GlyphMain(x, y, w, h, idx, seed + 31);
        }

        /// <summary>单帧图元 → 真·帧动画序列（v0.21.6.4 每异能独立帧动画）。
        /// 用户要求"那就生成这么多特效图"——用代码按像素仿射变换生成 count 帧
        /// （呼吸缩放 + 摆动平移 + 旋转漂移），等于为每异能生成独立动画帧，
        /// 零素材成本（不烧 AI 生图额度），CombatFxCore 已支持帧序列播放（Fps 自动 15）。
        /// seed 决定摆动方向/幅度/相位 → 398 异能动画各不相同。
        /// 帧0=原形，后续帧为环绕原形的"形变呼吸"，命中处播完整 4~6 帧=真动画而非一张图。</summary>
        internal static Sprite[] GlyphFrames(Sprite baseSpr, int count, int seed)
        {
            if (baseSpr == null) return null;
            count = Mathf.Clamp(count, 3, 8);
            var tex = baseSpr.texture;
            int w = tex.width, h = tex.height;
            var frames = new Sprite[count];
            // seed 派生节奏参数（幅度/方向/相位）
            float amp = 0.10f + (seed % 4) * 0.025f;          // 摆动幅度 10~17.5%
            int dirX = (seed % 2 == 0) ? 1 : -1;              // 横向摆动方向
            int dirY = ((seed / 2) % 2 == 0) ? 1 : -1;        // 纵向摆动方向
            float phase = (seed % 7) * 0.45f;                 // 相位偏移（弧度步）
            for (int i = 0; i < count; i++)
            {
                // 环绕呼吸：scale 1±amp、摆动 dx/dy ±amp*12px、旋转 ±amp*30°
                float t = (i / (float)(count - 1)) * Mathf.PI * 2f + phase;
                float sc = 1f + Mathf.Sin(t) * amp;
                float dx = Mathf.Cos(t) * amp * 12f * dirX;
                float dy = Mathf.Sin(t * 0.7f) * amp * 8f * dirY;
                float rot = Mathf.Sin(t * 0.8f) * amp * 30f * dirX;
                frames[i] = WarpToSprite(tex, sc, dx, dy, rot);
            }
            return frames;
        }

        /// <summary>像素仿射重采样（逆变换采样，Point 滤波保锐利；旋转绕中心）。</summary>
        private static Sprite WarpToSprite(Texture2D src, float scale, float dx, float dy, float rotDeg)
        {
            int w = src.width, h = src.height;
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            float rad = rotDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            var px = new Color32[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = x - cx, v = y - cy;
                    // 逆变换：先逆平移，再逆旋转，再逆缩放
                    float u2 = (u - dx) * cos + (v - dy) * sin;
                    float v2 = -(u - dx) * sin + (v - dy) * cos;
                    u2 /= scale; v2 /= scale;
                    int sx = (int)(u2 + cx);
                    int sy = (int)(v2 + cy);
                    px[y * w + x] = (sx >= 0 && sy >= 0 && sx < w && sy < h)
                        ? src.GetPixel(sx, sy) : new Color32(0, 0, 0, 0);
                }
            }
            dst.SetPixels32(px);
            dst.Apply(false, false);
            return Sprite.Create(dst, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
