// ============================================================
// 异能级特效库（v0.18.66）
// 全量素材：GameResources/effects/<id>/frame_NN.png（VFX 免费包精筛 100 特效单元）
// 每个异能 = 标签主体特效（按 主+副标签组合 选池内变体，保证 233 异能组合唯一）
//          + 家族氛围特效（低透明背景层，贴合小类意象）
// 播放走 CombatFxCore 粒子（自有渲染器，不依赖原版 EffectsLibrary）；
// 加载失败静默回退（调用方保留原版标签特效兜底）。
// 纯展示层，不写业务数据；出镜率 + 粒子预算双重门禁防刷屏。
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using Code.Realm.Prototype;

namespace Code.Combat.Fx
{
    public static class AbilityFx
    {
        /// <summary>特效总开关（默认开；卡顿时可关）</summary>
        public static bool Enabled = true;

        private static readonly Dictionary<string, Sprite[]> _cache = new Dictionary<string, Sprite[]>();

        // v0.21.6 每异能独立特效：异能Id → 专属程序化图元（懒生成，首次播放才建，398 上限）
        private static readonly Dictionary<string, Sprite[]> _prototypeGlyphs = new Dictionary<string, Sprite[]>();
        private static bool _glyphInited;

        // 主标签 → 特效池（多个变体；按副标签哈希取用，保证 (主,副) 组合唯一）
        private static readonly Dictionary<PrototypeTag, string[]> _tagPool = new Dictionary<PrototypeTag, string[]>();
        // 家族（大类/小类）→ 氛围特效
        private static readonly Dictionary<string, string> _family = new Dictionary<string, string>();

        static AbilityFx()
        {
            // ---- 攻击标签池（v0.19.13 素材全面回接：12 素材包图检确认接入，意象对位见 VFXSource/FX_CONTENT_MAP.md）----
            _tagPool[PrototypeTag.Burn]       = new[] { "vfx_fastfire", "vfx_ditheredfire", "magic_fireball" };
            _tagPool[PrototypeTag.Plasma]     = new[] { "magic_firebomb", "vfx_explosion2", "spx_explosion" };
            _tagPool[PrototypeTag.Frost]      = new[] { "magic_ice_a", "magic_ice_b", "magic_ice_c" };
            _tagPool[PrototypeTag.Frostbite]  = new[] { "magic_ice_c", "magic_ice_b", "magic_ice_a" };
            _tagPool[PrototypeTag.TimeSlow]   = new[] { "vfx_eldenring", "soul_kong", "magic_spark4" };   // 时间环意象
            _tagPool[PrototypeTag.Stun]       = new[] { "magic_lightning", "vfx_impact", "magic_spark" };
            _tagPool[PrototypeTag.Crit]       = new[] { "vfx_bighit", "magic_spark4", "vfx_smallhit" };   // 暴击=重击大闪（v0.19.8 定，非爆炸）
            _tagPool[PrototypeTag.Telekinesis]= new[] { "soul_kong", "magic_bolt", "vfx_constellation" };
            _tagPool[PrototypeTag.Poison]     = new[] { "bub_toxic", "magic_smoke", "magic_dust" };
            _tagPool[PrototypeTag.Curse]      = new[] { "soul_yin", "magic_darkbolt", "vfx_vortex" };
            _tagPool[PrototypeTag.Fear]       = new[] { "vfx_vortex", "soul_yin", "magic_darkbolt" };
            _tagPool[PrototypeTag.Slow]       = new[] { "magic_ice_b", "magic_ice_a", "bub_ice" };
            _tagPool[PrototypeTag.Lifesteal]  = new[] { "blood_vfx1", "blood_vfx3", "bub_blood" };
            _tagPool[PrototypeTag.Drain]      = new[] { "blood_vfx2", "blood_vfx1", "blood_vfx3" };
            _tagPool[PrototypeTag.Blood]      = new[] { "blood_vfx3", "blood_vfx1", "bub_blood" };
            _tagPool[PrototypeTag.Soul]       = new[] { "vfx_anima", "soul_yang", "magic_darkbolt" };
            _tagPool[PrototypeTag.Heal]       = new[] { "vfx_puffstars", "magic_flash", "bub_green2" };
            _tagPool[PrototypeTag.Chain]      = new[] { "magic_lightning", "magic_bolt", "vfx_elecshield" };
            _tagPool[PrototypeTag.Knockback]  = new[] { "vfx_smallhit", "magic_spark", "vfx_impact" };
            _tagPool[PrototypeTag.Gravity]    = new[] { "vfx_vortex", "soul_kong", "magic_dust" };
            _tagPool[PrototypeTag.Berserk]    = new[] { "vfx_magma", "vfx_fastfire", "spx_explosion" };
            _tagPool[PrototypeTag.Entangle]   = new[] { "vfx_tentacles", "soul_wood", "bub_green1" };
            _tagPool[PrototypeTag.Petrify]    = new[] { "soul_earth", "magic_dust", "vfx_impact" };
            _tagPool[PrototypeTag.Aging]      = new[] { "soul_yin", "magic_darkbolt", "bub_purple" };
            _tagPool[PrototypeTag.Weakness]   = new[] { "magic_darkbolt", "soul_yin", "bub_purple2" };
            _tagPool[PrototypeTag.ArmorBreak] = new[] { "magic_spark", "magic_spark4", "vfx_smallhit" };
            _tagPool[PrototypeTag.Execute]    = new[] { "vfx_explosion2", "spx_explosion", "blood_vfx1" };   // v0.19.7 剔除KABOOM
            _tagPool[PrototypeTag.Sleep]      = new[] { "soul_yin", "magic_spark4", "magic_darkbolt" };
            _tagPool[PrototypeTag.MultiShot]  = new[] { "magic_spark2", "magic_spark4", "spx_splatter" };
            _tagPool[PrototypeTag.HealAlly]   = new[] { "vfx_puffstars", "bub_green1", "magic_flash" };
            _tagPool[PrototypeTag.Summon]     = new[] { "vfx_vortex", "magic_flash", "soul_jin" };
            // ---- 元素细分标签池（v0.19.13：Souls 包十元素专属素材）----
            _tagPool[PrototypeTag.Water]      = new[] { "soul_water", "magic_splash", "bub_water1" };
            _tagPool[PrototypeTag.Wind]       = new[] { "soul_wind", "magic_spark2", "vfx_anima" };
            _tagPool[PrototypeTag.Thunder]    = new[] { "soul_thunder", "magic_lightning", "magic_bolt" };
            _tagPool[PrototypeTag.Earth]      = new[] { "soul_earth", "magic_dust", "vfx_impact" };
            _tagPool[PrototypeTag.Metal]      = new[] { "soul_jin", "magic_spark4", "magic_spark" };
            _tagPool[PrototypeTag.Light]      = new[] { "soul_yang", "magic_flash", "vfx_eldenring" };
            _tagPool[PrototypeTag.Shadow]     = new[] { "soul_yin", "magic_darkbolt", "vfx_vortex" };
            // ---- v0.20.0 6 新机制标签特效池 ----
            _tagPool[PrototypeTag.Aura]       = new[] { "vfx_eldenring", "vfx_powerchords", "magic_flash" };   // 光环=金色环
            _tagPool[PrototypeTag.Mark]       = new[] { "magic_spark4", "vfx_smallhit", "magic_spark" };       // 标记=锐点
            _tagPool[PrototypeTag.ManaDrain]  = new[] { "soul_yin", "magic_darkbolt", "vfx_vortex" };          // 吸能=暗紫夺灵
            _tagPool[PrototypeTag.Buff]       = new[] { "vfx_fastfire", "vfx_magma", "spx_explosion" };        // 强化=赤红增益
            _tagPool[PrototypeTag.Reflect]    = new[] { "magic_spark", "vfx_smallhit", "vfx_impact" };         // 反射=反震
            _tagPool[PrototypeTag.Domain]     = new[] { "vfx_elecshield", "vfx_eldenring", "magic_flash" };    // 领域=结界罩

            // ---- 防守标签池（v0.19.13 全素材，剔除 art_windarc/art_burst/art_holylight/art_woodvine 程序化残留）----
            // v0.19.13b 网格护盾回归：用户澄清从未否决网格盾，问题只在尺寸过大——现统一 0.28 罩全身（与奥术光膜同级）
            _tagPool[PrototypeTag.Shield]         = new[] { "art_arcaneshield", "shield_blue_loop", "shield_green_loop" };
            _tagPool[PrototypeTag.Dodge]          = new[] { "soul_wind", "magic_spark2" };
            _tagPool[PrototypeTag.Illusion]       = new[] { "magic_spark4", "soul_wind" };
            _tagPool[PrototypeTag.Invisibility]   = new[] { "magic_spark2", "soul_yin" };
            _tagPool[PrototypeTag.Teleport]       = new[] { "soul_kong", "vfx_vortex" };
            _tagPool[PrototypeTag.Clone]          = new[] { "vfx_elecshield", "soul_wind" };
            _tagPool[PrototypeTag.Blessing]       = new[] { "vfx_puffstars", "soul_yang" };
            _tagPool[PrototypeTag.Regeneration]   = new[] { "vfx_puffstars", "soul_wood" };
            _tagPool[PrototypeTag.Magnetism]      = new[] { "vfx_elecshield", "magic_bolt", "magic_spark4" };
            _tagPool[PrototypeTag.Vibration]      = new[] { "vfx_smallhit", "vfx_impact" };
            _tagPool[PrototypeTag.Sound]          = new[] { "vfx_powerchords", "vfx_impact", "magic_spark2" };   // v0.19.8 声波=音爆波纹，不是爆炸
            _tagPool[PrototypeTag.Thorns]         = new[] { "magic_spark", "spx_splatter", "vfx_impact" };
            _tagPool[PrototypeTag.ElementResist]  = new[] { "art_arcaneshield", "shield_bw_loop", "shield_blue_loop" };   // 元素抗性=同款淡罩/网格盾（0.28 罩全身）
            _tagPool[PrototypeTag.Cleanse]        = new[] { "magic_spark4", "vfx_puffstars" };

            // ---- 家族氛围（大类/小类 → 特效）----
            // ---- 家族氛围（v0.19.13 全素材，剔除 art_* 程序化残留）----
            _family["元素系/火"] = "vfx_fastfire";     _family["元素系/水"] = "magic_splash";
            _family["元素系/风"] = "soul_wind";        _family["元素系/雷"] = "magic_lightning";
            _family["元素系/冰"] = "magic_ice_a";      _family["元素系/土"] = "magic_dust";
            _family["元素系/木"] = "soul_wood";        _family["元素系/光"] = "soul_yang";
            _family["元素系/暗"] = "soul_yin";
            // ---- v0.20.0 血神系/机械系独立成大分类（自黑暗系/元素系金属迁出）----
            _family["血神系/嗜血"] = "blood_vfx1";     _family["血神系/血蚀"] = "blood_vfx2";
            _family["血神系/血缚"] = "vfx_tentacles";  _family["血神系/血咒"] = "magic_darkbolt";
            _family["机械系/钢铁"] = "magic_spark4";   _family["机械系/电磁"] = "vfx_elecshield";
            _family["机械系/机械"] = "soul_jin";
            _family["光明系/圣光"] = "soul_yang";       _family["光明系/光刃"] = "spx_shock";
            _family["光明系/净化"] = "magic_spark4";    _family["光明系/晨曦"] = "magic_flash";
            _family["黑暗系/暗影"] = "soul_yin";        _family["黑暗系/腐蚀"] = "magic_smoke";
            _family["黑暗系/诅咒"] = "magic_darkbolt";  _family["黑暗系/夜幕"] = "soul_yin";
            _family["生命系/生命"] = "vfx_anima";       _family["生命系/治愈"] = "vfx_puffstars";
            _family["生命系/再生"] = "vfx_puffstars";   _family["生命系/生长"] = "soul_wood";
            _family["生命系/祝福"] = "soul_yang";
            _family["死亡系/亡灵"] = "vfx_bloodimpact"; _family["死亡系/凋零"] = "bub_black";
            _family["死亡系/死亡"] = "vfx_anima";       _family["死亡系/灵魂"] = "vfx_anima";
            _family["死亡系/腐化"] = "soul_yin";
            _family["精神系/催眠"] = "magic_spark4";    _family["精神系/幻象"] = "soul_wind";
            _family["精神系/心灵"] = "vfx_constellation"; _family["精神系/念力"] = "vfx_constellation";
            _family["精神系/意志"] = "vfx_eldenring";   _family["精神系/操控"] = "vfx_constellation";
            _family["精神系/预知"] = "vfx_constellation";
            _family["时空系/时间"] = "magic_spark4";    _family["时空系/空间"] = "soul_kong";
            _family["时空系/次元"] = "vfx_vortex";      _family["时空系/引力"] = "vfx_vortex";
            _family["时空系/速度"] = "magic_spark2";    // v0.19.8 剔除原版等离子尾迹(可能持续残留)
            _family["自然系/动物"] = "vfx_puffstars";   _family["自然系/植物"] = "soul_wood";
            _family["自然系/毒"] = "bub_toxic";         _family["自然系/气象"] = "vfx_elecshield";
            _family["自然系/藤蔓"] = "vfx_tentacles";   _family["自然系/野兽"] = "spx_shock";
            _family["强化系/体魄"] = "vfx_impact";      _family["强化系/能量"] = "vfx_anima";
            // ---- v0.18.99 新体系家族氛围 ----
            _family["兽化系/猛兽"] = "vfx_puffstars";    _family["兽化系/飞禽"] = "soul_wind";
            _family["兽化系/爬行"] = "soul_wood";       _family["兽化系/虫豸"] = "spx_shock";
            _family["兽化系/神兽"] = "vfx_powerchords";
            _family["命运系/命运"] = "vfx_constellation"; _family["命运系/因果"] = "vfx_constellation";
            _family["命运系/气运"] = "vfx_puffstars";    _family["命运系/灾厄"] = "vfx_vortex";
            _family["召唤系/兽灵"] = "vfx_puffstars";    _family["召唤系/元素"] = "magic_lightning";
            _family["召唤系/亡灵"] = "vfx_anima";        _family["召唤系/幻影"] = "soul_wind";
            _family["召唤系/虫潮"] = "spx_shock";        _family["召唤系/巨兽"] = "vfx_tentacles";
            _family["召唤系/圣灵"] = "soul_yang";        _family["召唤系/龙裔"] = "vfx_powerchords";
            _family["召唤系/百鬼"] = "vfx_vortex";
            _family["强化系/力量"] = "vfx_impact";       _family["强化系/速度"] = "magic_spark2";   // v0.19.8 剔除原版等离子尾迹(可能持续残留)
            _family["强化系/防御"] = "art_arcaneshield"; _family["强化系/变形"] = "soul_wind";   // v0.19.5 防御氛围=奥术光膜
        }

        // ============================================================
        // 资源加载（带缓存）
        // ============================================================
        public static Sprite[] Load(string effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return null;
            if (_cache.TryGetValue(effectId, out var cached)) return cached;
            try
            {
                // art_ 前缀：程序化图元（v0.18.74 中式重生成，零素材文件依赖）
                if (effectId.StartsWith("art_"))
                {
                    CombatFxArt.EnsureInit();
                    var art = CombatFxArt.Get(effectId);
                    if (art != null)
                    {
                        _cache[effectId] = new[] { art };
                        return _cache[effectId];
                    }
                    // 素材缺失兜底（v0.19.13 起主池全素材，仅异常路径触发）
                    var glyph = CombatFxGlyphs.Get(effectId);
                    if (glyph != null)
                    {
                        _cache[effectId] = new[] { glyph };
                        return _cache[effectId];
                    }
                    _cache[effectId] = new Sprite[0];
                    return null;
                }
                var sprites = Resources.LoadAll<Sprite>("effects/" + effectId);
                if (sprites == null || sprites.Length == 0)
                {
                    _cache[effectId] = new Sprite[0];
                    return null;
                }
                Array.Sort(sprites, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                _cache[effectId] = sprites;
                return sprites;
            }
            catch (Exception dsEx)
            {
                Code.Core.DSDebug.Verbose("[AbilityFx] 加载失败 effects/" + effectId + ": " + dsEx.Message);
                _cache[effectId] = new Sprite[0];
                return null;
            }
        }

        // ============================================================
        // 特效样式（按 id 前缀分组：大/中/小/护盾/气泡）
        // ============================================================
        internal static void Style(string id, out float size, out float life)
        {
            // 尺寸基准：px=1/帧宽 → 世界宽恒等于 size（size 即期望世界宽）。
            // 人类 actor_scale≈0.25 → 单位视觉宽≈0.22~0.28 世界单位（v0.18.91 反编译复核；
            // 西幻奥术护盾 0.24 世界单位=罩全身的标准）。
            // v0.19.4 按异能定位分级（大众认知），v0.19.5 回调防遮挡，v0.19.7 状态泡/命中贴体型：
            //   护盾≈1.1 倍单位宽罩全身（奥术光膜）· 状态泡≈0.8 倍（小泡贴体）· 单体命中≈1.3 倍 · 图元≈1.5 倍
            //   星座≈2 倍（念力星图）· 大爆≈2.2 倍（波及周围）· 领域/结界/威压≈2.5 倍（笼罩四方但半透明贴地，不遮地图）
            if (id == null) { size = 0.4f; life = 0.6f; return; }
            // v0.19.13 Souls 十元素：命中贴体；空间/阳=爆发级
            if (id.StartsWith("soul_"))
            {
                if (id == "soul_kong" || id == "soul_yang") { size = 0.5f; life = 0.6f; return; }
                size = 0.2f; life = 0.5f; return;
            }
            // v0.19.13 Magic Packs 投射弹/魔法分级
            if (id == "magic_bolt" || id == "magic_darkbolt") { size = 0.2f; life = 0.5f; return; }
            if (id == "magic_fireball" || id == "magic_firebomb") { size = 0.22f; life = 0.55f; return; }
            if (id == "magic_lightning") { size = 0.25f; life = 0.5f; return; }
            if (id.StartsWith("magic_ice")) { size = 0.2f; life = 0.5f; return; }
            if (id == "magic_smoke") { size = 0.25f; life = 0.7f; return; }
            if (id == "magic_splash") { size = 0.2f; life = 0.5f; return; }
            if (id.StartsWith("art_"))
            {
                // 奥术光膜护盾（罩全身≈1.1 倍单位宽，持续环绕）；符环=领域环（v0.19.5 回调 0.62）
                if (id == "art_arcaneshield") { size = 0.28f; life = 2.0f; return; }
                if (id == "art_rune") { size = 0.45f; life = 0.8f; return; }
                // 程序化图元兜底尺寸（主池已全素材，仅异常路径）
                if (id.StartsWith("art_glyph_"))
                {
                    if (id == "art_glyph_shield") { size = 0.28f; life = 2.0f; return; }        // 持续护罩
                    if (id == "art_glyph_ghost") { size = 0.32f; life = 0.5f; return; }         // 虚影瞬闪
                    if (id == "art_glyph_pulse") { size = 0.62f; life = 0.8f; return; }         // 领域脉冲
                    size = 0.22f; life = 0.55f; return;                                        // 命中意象贴体（≈0.9 倍单位宽，原版命中 0.1~0.25）
                }
                size = 0.26f; life = 0.55f; return;                                     // v0.21.6.2 定标：≈单位 scale（原版施法特效=stats["scale"]0.25）
            }
            if (id.StartsWith("shield_")) { size = 0.28f; life = 2.0f; return; }      // 兼容旧引用（池已切奥术光膜）
            if (id.StartsWith("bub_")) { size = 0.1f; life = 0.55f; return; }        // v0.19.11 状态泡回调：0.32 倍单位宽（粒子级小光点，贴身不显眼）
            if (id.StartsWith("spx_") || id.StartsWith("magic_spark") || id == "magic_flash"
                || id == "magic_dust" || id == "vfx_impact" || id == "vfx_smallhit"
                || id == "vfx_ditheredfire" || id == "vfx_fastfire" || id == "vfx_bloodimpact"
                || id == "vfx_puffstars" || id == "vfx_elecshield" || id == "vfx_anima")
            {
                size = 0.22f; life = 0.5f; return;                                    // v0.21.6.2 定标：≈0.9 倍单位宽（原版命中 0.1~0.25）
            }
            // v0.19.5 领域级：结界/领域/威压/巨兽（贴地扩散，笼罩≈2.5 倍单位宽，回调防遮挡）
            if (id.StartsWith("vfx_") && (id.Contains("tentacles") || id.Contains("vortex")
                || id.Contains("eldenring") || id.Contains("powerchords")))
            {
                size = 0.45f; life = 0.8f; return;
            }
            // v0.19.5 大爆级：爆炸/震地/重击（波及≈2.2 倍单位宽）
            if (id.StartsWith("vfx_") && (id.Contains("explosion") || id.Contains("kabooms")
                || id.Contains("bighit") || id.Contains("charged") || id.Contains("magma")))
            {
                size = 0.4f; life = 0.7f; return;
            }
            // 星座：念力星图（≈2 倍单位宽）
            if (id.StartsWith("vfx_") && id.Contains("constellation"))
            {
                size = 0.35f; life = 0.6f; return;
            }
            size = 0.25f; life = 0.6f;
        }

        // ============================================================
        // 主标签特效（按副标签哈希从池内取变体 → 组合唯一）
        // ============================================================
        private static string PickTagFx(PrototypeTag main, PrototypeTag sub)
        {
            if (!_tagPool.TryGetValue(main, out var pool) || pool == null || pool.Length == 0) return null;
            if (pool.Length == 1) return pool[0];
            int idx = Math.Abs((int)sub) % pool.Length;
            return pool[idx];
        }

        private static string PickFamilyFx(string category, string sub)
        {
            if (string.IsNullOrEmpty(category)) return null;
            string key = category + "/" + (string.IsNullOrEmpty(sub) ? "通用" : sub);
            _family.TryGetValue(key, out var fx);
            return fx;
        }

        // ============================================================
        // v0.18.92 异能专属视觉：44 标签基准色 × 异能 id 稳定哈希派生
        // 每个异能都有稳定可辨识的专属色调（同标签异能互不撞色），
        // 且不破坏大众认知（火系仍是暖色、冰系仍是冷色、毒仍是绿）。
        // 同一异能每次播放色调一致（哈希稳定，非随机）。
        // ============================================================

        private static Color TagBaseColor(PrototypeTag tag)
        {
            switch (tag)
            {
                case PrototypeTag.Burn:          return new Color(1f, 0.42f, 0.18f);  // 橙红
                case PrototypeTag.Plasma:        return new Color(1f, 0.28f, 0.15f);  // 炽红
                case PrototypeTag.Frost:         return new Color(0.55f, 0.82f, 1f);   // 淡蓝
                case PrototypeTag.Frostbite:     return new Color(0.62f, 0.9f, 1f);    // 冰蓝白
                case PrototypeTag.TimeSlow:      return new Color(0.45f, 0.6f, 0.95f); // 蓝紫
                case PrototypeTag.Stun:          return new Color(1f, 0.82f, 0.2f);   // 金黄
                case PrototypeTag.Crit:          return new Color(1f, 0.65f, 0.15f);  // 橙
                case PrototypeTag.Telekinesis:   return new Color(0.68f, 0.5f, 0.95f); // 紫
                case PrototypeTag.Poison:        return new Color(0.45f, 0.85f, 0.35f);// 绿
                case PrototypeTag.Curse:         return new Color(0.5f, 0.3f, 0.65f);  // 暗紫
                case PrototypeTag.Fear:          return new Color(0.4f, 0.25f, 0.55f); // 暗紫
                case PrototypeTag.Slow:          return new Color(0.4f, 0.7f, 0.9f);   // 水蓝
                case PrototypeTag.Lifesteal:     return new Color(0.85f, 0.2f, 0.22f);// 血红
                case PrototypeTag.Drain:         return new Color(0.6f, 0.15f, 0.25f);// 暗红
                case PrototypeTag.Blood:         return new Color(0.75f, 0.12f, 0.15f);// 血红
                case PrototypeTag.Soul:          return new Color(0.45f, 0.75f, 0.95f);// 幽蓝
                case PrototypeTag.Heal:          return new Color(0.4f, 0.85f, 0.5f); // 翠绿
                case PrototypeTag.Chain:         return new Color(0.45f, 0.72f, 1f);  // 电蓝
                case PrototypeTag.Knockback:     return new Color(1f, 0.7f, 0.3f);    // 橙
                case PrototypeTag.Gravity:       return new Color(0.42f, 0.3f, 0.55f);// 暗紫
                case PrototypeTag.Berserk:       return new Color(0.9f, 0.25f, 0.12f);// 血红
                case PrototypeTag.Entangle:      return new Color(0.4f, 0.75f, 0.3f); // 藤绿
                case PrototypeTag.Petrify:       return new Color(0.75f, 0.72f, 0.68f);// 灰
                case PrototypeTag.Aging:         return new Color(0.62f, 0.55f, 0.5f);// 灰褐
                case PrototypeTag.Weakness:      return new Color(0.55f, 0.5f, 0.3f); // 暗黄
                case PrototypeTag.ArmorBreak:    return new Color(1f, 0.72f, 0.2f);   // 橙黄
                case PrototypeTag.Execute:       return new Color(0.85f, 0.1f, 0.08f);// 深红
                case PrototypeTag.Sleep:         return new Color(0.6f, 0.75f, 0.9f); // 淡蓝白
                case PrototypeTag.MultiShot:     return new Color(1f, 0.85f, 0.35f);  // 金黄
                case PrototypeTag.HealAlly:      return new Color(0.45f, 0.85f, 0.55f);// 翠绿
                case PrototypeTag.Shield:        return new Color(0.45f, 0.65f, 1f);  // 蓝
                case PrototypeTag.Dodge:         return new Color(0.4f, 0.8f, 0.85f); // 青
                case PrototypeTag.Blessing:      return new Color(1f, 0.85f, 0.4f);   // 金
                case PrototypeTag.Magnetism:     return new Color(0.4f, 0.6f, 0.95f); // 电蓝
                case PrototypeTag.Vibration:     return new Color(1f, 0.6f, 0.25f);   // 橙
                case PrototypeTag.Sound:         return new Color(0.4f, 0.8f, 0.7f);  // 青绿
                case PrototypeTag.Illusion:      return new Color(0.85f, 0.75f, 0.95f);// 浅紫
                case PrototypeTag.Invisibility:  return new Color(0.8f, 0.85f, 0.95f);// 淡蓝白
                case PrototypeTag.Regeneration:  return new Color(0.45f, 0.9f, 0.6f); // 翠绿
                case PrototypeTag.Thorns:        return new Color(0.5f, 0.8f, 0.35f); // 绿
                case PrototypeTag.Clone:         return new Color(0.55f, 0.8f, 0.85f);// 青
                case PrototypeTag.ElementResist: return new Color(0.5f, 0.7f, 1f);    // 蓝
                case PrototypeTag.Cleanse:       return new Color(0.75f, 0.9f, 0.95f);// 淡青白
                case PrototypeTag.Teleport:      return new Color(0.6f, 0.45f, 0.9f); // 紫
                case PrototypeTag.Summon:        return new Color(0.55f, 0.4f, 0.85f);// 召唤紫（v0.18.99）
                // ---- 元素细分基准色（v0.19.8）----
                case PrototypeTag.Water:         return new Color(0.25f, 0.55f, 0.95f);// 水蓝
                case PrototypeTag.Wind:          return new Color(0.35f, 0.85f, 0.75f);// 风青
                case PrototypeTag.Thunder:       return new Color(0.95f, 0.82f, 0.15f);// 雷黄
                case PrototypeTag.Earth:         return new Color(0.62f, 0.45f, 0.28f);// 土褐
                case PrototypeTag.Metal:         return new Color(0.78f, 0.8f, 0.85f); // 金属银
                case PrototypeTag.Light:         return new Color(1f, 0.92f, 0.55f);  // 圣光金
                case PrototypeTag.Shadow:        return new Color(0.42f, 0.25f, 0.6f);// 暗影紫
                default:                         return new Color(1f, 1f, 1f);
            }
        }

        /// <summary>异能专属派生色：基准标签色 + 异能 id 稳定哈希（色相 ±12°、明度 ±0.08）。
        /// 233 个异能各自稳定可辨识；同标签下不同异能互不撞色；不破坏标签大众认知。</summary>
        private static Color DeriveTint(PrototypeDef def)
        {
            Color c = TagBaseColor(def.MainTag);
            if (def == null || string.IsNullOrEmpty(def.Id)) return c;
            unchecked
            {
                int h = 17;
                foreach (char ch in def.Id) h = h * 31 + ch;
                if (h < 0) h = -h;
                float hueShift = ((h % 240) - 120) / 10f;      // -12° ~ +12°
                float bright = 1f + (((h / 240) % 160) - 80) / 1000f; // ±0.08
                Color.RGBToHSV(c, out float H, out float S, out float V);
                H = (H + hueShift / 360f) % 1f;
                if (H < 0f) H += 1f;
                V = Mathf.Clamp01(V * bright);
                return Color.HSVToRGB(H, S, V);
            }
        }

        // ============================================================
        // 神形显现（v0.19.19 程序化自生成，替代原版 unit_anim boss 待机动画素材）
        // 金色半透明神祇虚影图元（art_divinevision，CombatFxArt 纯几何生成），
        // 零素材文件依赖：不再读取 effects_unit_anims 清单与 unit_anim_* 帧序列。
        // ============================================================

        public static Sprite[] PickDivineVision()
        {
            try
            {
                CombatFxArt.EnsureInit();
                if (CombatFxArt.DivineVision == null) return null;
                return new[] { CombatFxArt.DivineVision };
            }
            catch (Exception) { return null; }
        }

        // ============================================================
        // 播放（异能级：投射物飞行/原地爆发 + 家族氛围 + 命中冲击环）
        // attack=true：self=攻击者，other=目标——投射类特效从攻击者飞向目标并命中爆发；
        //             非投射类直接在目标位置爆发 + 扩张冲击环（战斗之神 SpellBurst 思路）。
        // attack=false：self=防守者，特效绑定单位跟随（护盾/再生/祝福环绕演出）。
        // 全部尺寸按目标单位体型缩放（actor_size/13，Human=1.0）。
        // ============================================================
        public static void PlayAbilityFx(Actor self, Actor other, PrototypeDef def, bool attack, bool force = false)
        {
            if (!Enabled || def == null || self == null) return;
            if (!attack && !self.isAlive()) return;
            Actor anchor = attack ? (other != null && other.isAlive() ? other : self) : self;
            Vector2 pos = anchor.current_position;
            if (World.world == null || World.world.GetTileSimple((int)pos.x, (int)pos.y) == null) return;
            if (!force && UnityEngine.Random.value > 0.5f) return;   // 50% 出镜率

            float scale = UnitScale(anchor);
            string mainFx = PickTagFx(def.MainTag, def.SubTag);
            Color tint = DeriveTint(def);   // v0.18.92 异能专属派生色（基准标签色×异能id哈希）

            if (attack)
            {
                // ---- 攻击：投射飞行 or 目标爆发 ----
                if (!string.IsNullOrEmpty(mainFx) && IsProjectile(mainFx))
                {
                    SpawnProjectile(mainFx, self.current_position, pos, scale, tint);
                    // v0.21.6 命中爆：每异能独立专属图元（形态×seed×专属色），不再共用素材命中爆
                    Sprite[] glyph = PrototypeFramesFor(def);
                    if (glyph != null) SpawnGlyphBurst(glyph, pos, scale, tint, StableSeed(def.Id));
                    else SpawnFx(mainFx, pos, FxLayer.Top, 36, true, scale, delay: 0.13f, follow: null, tint: tint);
                }
                else if (!string.IsNullOrEmpty(mainFx))
                {
                    // v0.18.69：护盾环绕类特效必须绑定单位跟随——护盾是"套在身上环绕"，
                    // 不是命中贴图。攻击路径也跟随被攻击目标（给目标上盾的视觉），
                    // 防守路径（else 大分支）已 follow:self。
                    bool shieldFx = mainFx.StartsWith("shield_") || mainFx == "art_arcaneshield";   // v0.19.5 奥术光膜同护盾跟随
                    SpawnFx(mainFx, pos, FxLayer.Top, shieldFx ? 25 : 35, true, scale, 0f,
                        shieldFx ? anchor : null, tint: tint);
                    // v0.21.6 每异能独立特效：素材主演出之外叠专属图元爆发（形态可辨每异能）
                    Sprite[] glyph = PrototypeFramesFor(def);
                    if (glyph != null) SpawnGlyphBurst(glyph, pos, scale, tint, StableSeed(def.Id));
                }
                else
                {
                    // v0.21.6 无池素材兜底：纯专属图元（新/未入池标签不再"无特效"）
                    Sprite[] glyph = PrototypeFramesFor(def);
                    if (glyph != null) SpawnGlyphBurst(glyph, pos, scale, tint, StableSeed(def.Id));
                }
                // 命中冲击环（扩张淡出，贴合目标体型，细环不遮视野）
                var ring = CombatFxCore.Spawn(CombatFxArt.Rings[0], pos, 0.09f, 0.35f, FxLayer.Top, 30);
                if (ring != null)
                {
                    // v0.18.91：命中冲击环按帧宽归一化（世界宽=px×帧宽×size，px=0.09/帧宽17→1.53×size），
                    // 目标 0.9→0.35 世界单位（≈1.5 倍单位宽扩散），原 1.7 世界单位偏大
                    // v0.18.92：环色跟随异能专属派生色
                    ring.Color(new Color(tint.r, tint.g, tint.b, 0.85f), new Color(tint.r, tint.g, tint.b, 0f))
                        .Sized(0.59f * scale, 0.23f * scale);
                }
                // v0.21.1 地图融合：攻击命中后按异能标签铺贴地持续域 + 分级地形响应
                // （不再"所有特效都只是砸过去"；火→地面起火、冰→地面结霜、雷→劈焦大地、
                //   登神+真实炸坑/岩浆、超神命中"陆地打没"，复用 DSTerrainDestruction 分级破坏）
                try { if (def != null) CombatMapFx.OnHit(self, pos, def.MainTag, def.SubTag); }
                catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[CombatMapFx] " + dsEx.Message); }
            }
            else
            {
                // ---- 防守：特效绑定单位跟随（护盾/再生/祝福/净化环绕）----
                if (!string.IsNullOrEmpty(mainFx))
                {
                    SpawnFx(mainFx, pos, FxLayer.Over, 25, true, scale, 0f, self, tint: tint);
                }
                // v0.21.6 防守印记：弱化专属图元环贴自身（护盾/闪避时异能印记可辨，不遮人）
                Sprite[] glyph = PrototypeFramesFor(def);
                if (glyph != null)
                {
                    var g = CombatFxCore.Spawn(glyph, pos + new Vector2(0f, 0.17f * scale),
                        1f / 64f, 0.35f, FxLayer.Over, 22);
                    if (g != null)
                    {
                        g.Sized(0.2f * scale, 0.26f * scale);         // 微扩散环（0.8~1 倍单位宽）
                        g.Spin = ((StableSeed(def.Id) % 2 == 0) ? 1f : -1f) * 0.9f;  // 慢转 51°/s
                        g.Color(new Color(tint.r, tint.g, tint.b, 0.5f), new Color(tint.r, tint.g, tint.b, 0f));
                    }
                }
            }

            // 家族氛围层（与主特效不同才叠；低透明背景）
            // 攻击时不叠——避免战斗时每下都有背景光效堆叠成"一片条纹"；仅防守/强效演出叠
            string famFx = PickFamilyFx(def.Category, def.SubCategory);
            if (!string.IsNullOrEmpty(famFx) && famFx != mainFx && (!attack || force))
            {
                SpawnFx(famFx, pos, FxLayer.Behind, 5, false, scale * 0.8f, 0f, attack ? null : self, tint: tint);
            }
        }

        /// <summary>单位体型缩放系数（ActorSize 0-17 / 13，Human=1.0，下限 0.3 防虫级过小）</summary>
        internal static float UnitScale(Actor a)
        {
            if (a == null || a.asset == null) return 1f;
            return Mathf.Max(0.3f, (float)a.asset.actor_size / 13f);
        }

        // ================================================================
        // v0.21.6 每异能独立特效核心：异能专属程序化图元
        // 主形态 = MainTag 意象，副形态 = SubTag 点缀，seed = 异能Id 稳定哈希，
        // 颜色 = DeriveTint（标签基准色×Id 哈希）。同标签异能靠 seed 形变+副形态+色区分，
        // 398 异能每异能唯一。懒生成 + 缓存，首次播放才建纹理（非每帧）。
        // ================================================================

        /// <summary>取异能专属图元（懒生成+缓存；失败返回 null 走素材兜底）。</summary>
        /// <summary>每异能独立帧动画序列（v0.21.6.4）。
        /// 原单帧图元 → 由 CombatFxArt.GlyphFrames 生成 5 帧真·动画序列
        /// （呼吸缩放+摆动+旋转漂移，seed 定节奏），懒生成 + 512 LRU 缓存。
        /// 命中/投射命中播帧序列（CombatFxCore Fps=15 自动），不再是"一张图"。</summary>
        private static Sprite[] PrototypeFramesFor(PrototypeDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return null;
            if (_prototypeGlyphs.TryGetValue(def.Id, out var cached)) return cached;
            try
            {
                CombatFxArt.EnsureInit();
                int seed = StableSeed(def.Id);
                int mainIdx = TagGlyphIndex(def.MainTag);
                int subIdx = def.SubTag == def.MainTag ? -1 : TagGlyphIndex(def.SubTag);   // 副标签同主标签不重复叠加
                var sprite = CombatFxArt.MakePrototypeGlyph(seed, mainIdx, subIdx, def.Tier);
                if (sprite == null) return null;
                var frames = CombatFxArt.GlyphFrames(sprite, 5, seed);
                if (frames == null) return null;
                if (_prototypeGlyphs.Count > 512) _prototypeGlyphs.Clear();   // 防膨胀（超 512 清重建）
                _prototypeGlyphs[def.Id] = frames;
                return frames;
            }
            catch (System.Exception dsEx)
            {
                Code.Core.DSDebug.Verbose("[AbilityFx] glyph " + def.Id + ": " + dsEx.Message);
                return null;
            }
        }

        /// <summary>异能 Id 稳定哈希（与 DeriveTint 同一算法，保证色/形同源）。</summary>
        private static int StableSeed(string id)
        {
            unchecked
            {
                int h = 17;
                foreach (char ch in id) h = h * 31 + ch;
                return h < 0 ? -h : h;
            }
        }

        /// <summary>标签 → 形态库索引（主/副形态分发）。</summary>
        private static int TagGlyphIndex(PrototypeTag tag)
        {
            switch (tag)
            {
                case PrototypeTag.Burn:
                case PrototypeTag.Plasma:
                case PrototypeTag.Berserk: return 0;    // 火苗
                case PrototypeTag.Frost:
                case PrototypeTag.Frostbite:
                case PrototypeTag.Slow:
                case PrototypeTag.TimeSlow: return 1;   // 冰晶（时缓同冰蓝，seed 区分）
                case PrototypeTag.Thunder:
                case PrototypeTag.Chain:
                case PrototypeTag.Magnetism:
                case PrototypeTag.Stun: return 2;       // Z 闪电
                case PrototypeTag.Wind:
                case PrototypeTag.MultiShot:
                case PrototypeTag.Crit: return 3;       // 风刃
                case PrototypeTag.Water: return 4;      // 波纹
                case PrototypeTag.Earth:
                case PrototypeTag.Petrify: return 5;    // 岩
                case PrototypeTag.Metal:
                case PrototypeTag.ArmorBreak: return 6; // 冷刃
                case PrototypeTag.Light:
                case PrototypeTag.Cleanse:
                case PrototypeTag.Blessing:
                case PrototypeTag.Aura: return 7;       // 光芒星
                case PrototypeTag.Shadow:
                case PrototypeTag.Curse:
                case PrototypeTag.Fear:
                case PrototypeTag.Aging:
                case PrototypeTag.Weakness: return 8;   // 暗弧瞳
                case PrototypeTag.Blood:
                case PrototypeTag.Lifesteal:
                case PrototypeTag.Drain:
                case PrototypeTag.ManaDrain: return 9;  // 血滴
                case PrototypeTag.Execute:
                case PrototypeTag.Soul: return 10;      // 骨镰
                case PrototypeTag.Heal:
                case PrototypeTag.HealAlly:
                case PrototypeTag.Regeneration: return 11; // 心
                case PrototypeTag.Telekinesis:
                case PrototypeTag.Illusion: return 12;  // 眼瞳
                case PrototypeTag.Summon: return 13;    // 爪印
                case PrototypeTag.Sleep: return 15;     // 月牙
                case PrototypeTag.Poison: return 16;    // 毒滴
                case PrototypeTag.Entangle: return 24;  // 藤蔓
                case PrototypeTag.Knockback:
                case PrototypeTag.Gravity:
                case PrototypeTag.Vibration:
                case PrototypeTag.Sound: return 22;     // 冲击双环
                case PrototypeTag.Shield:
                case PrototypeTag.Reflect:
                case PrototypeTag.ElementResist: return 23; // 盾
                case PrototypeTag.Dodge:
                case PrototypeTag.Invisibility:
                case PrototypeTag.Clone: return 22;     // 虚影环
                case PrototypeTag.Teleport:
                case PrototypeTag.Domain:
                case PrototypeTag.Mark: return 21;      // 符环
                case PrototypeTag.Thorns: return 22;    // 反弹环
                default: return 20;                     // 星芒兜底
            }
        }

        /// <summary>专属图元命中爆（真·粒子爆，非贴图）。
        /// v0.21.6.2 用户验收"这就是贴图"——单帧整图扩散+旋转观感仍是"一张图"，
        /// 重做为：中心核心闪光（0.14s）+ 8~10 片同形态碎片八方飞散（Vel 径向外抛 +
        /// Spin 高速自旋 + 尺寸收缩 + 专属色淡出）——"异能形态炸开成碎屑"的粒子爆，
        /// 命中处不再存在可辨认的整张图。</summary>
        private static void SpawnGlyphBurst(Sprite[] glyph, Vector2 pos, float scale, Color tint, int seed)
        {
            try
            {
                Vector2 center = pos + new Vector2(0f, 0.08f * scale);
                // 中心核心：极短闪光（只留"命中闪点"，不构成贴图）
                var core = CombatFxCore.Spawn(glyph, center, 1f / 64f, 0.4f, FxLayer.Top, 35);
                if (core != null)
                {
                    core.Sized(0.12f * scale, 0.1f * scale);
                    core.Spin = ((seed % 2 == 0) ? 1f : -1f) * (1.4f + (seed % 4) * 0.25f);
                    core.Color(new Color(tint.r, tint.g, tint.b, 1f), new Color(tint.r, tint.g, tint.b, 0.5f));
                }
                // 碎片溅射：8~10 片同形态小图元向八方飞散（粒子爆主体）
                int shards = 8 + (seed % 3);
                for (int i = 0; i < shards; i++)
                {
                    float ang = i * Mathf.PI * 2f / shards + ((seed % 7) * 0.13f);
                    float speed = (0.5f + ((seed + i * 3) % 5) * 0.09f) * scale;
                    var s = CombatFxCore.Spawn(glyph, center, 1f / 64f, 0.4f, FxLayer.Top, 33);
                    if (s == null) continue;
                    s.Vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;
                    s.Spin = ((i % 2 == 0) ? 1f : -1f) * (1.8f + (seed % 5) * 0.3f);
                    s.Sized(0.06f * scale, 0.01f * scale);
                    s.Color(new Color(tint.r, tint.g, tint.b, 0.85f), new Color(tint.r, tint.g, tint.b, 0f));
                }
            }
            catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[AbilityFx] glyph burst: " + dsEx.Message); }
        }

        /// <summary>投射类特效（球/弹/泡/星/飞火）：可做飞行弹道。
        /// v0.21.2 白名单扩展：soul_ 十元素（水弹/雷球/金刃/风刃/暗波/土块/木刺/圣光弹/空间弹）
        /// 与 blood_vfx 血弹、vfx_anima 灵魂火同为"飞行意象"素材——之前不在白名单全被误判
        /// 成"直接贴目标炸"（用户点名"冰锥类投射物没搞成投射"）。现全部转投射飞行。
        /// 排除窄长条素材（ray/lightning 等）——竖长条旋转 90° 横飞会变成"横着的光条"，观感违和。</summary>
        private static bool IsProjectile(string id)
        {
            if (id == null) return false;
            if (id.Contains("ray") || id.Contains("lightning") || id.Contains("flash")
                || id.Contains("dust") || id.Contains("splash") || id.Contains("smoke")
                || id.Contains("puff") || id.Contains("spark"))
            {
                return false;
            }
            return id.StartsWith("magic_") || id.StartsWith("spx_") || id.StartsWith("bub_")
                || id.StartsWith("soul_") || id.StartsWith("blood_vfx")
                || id == "vfx_fastfire" || id == "vfx_ditheredfire" || id == "vfx_anima";
        }

        /// <summary>地面类特效：贴地扩散（领域/气象/神兽威压/大爆炸/符环）。
        /// 大众认知：领域、海啸、龙威、雷劫是"地/天级演出"，特效应贴地扩散而不是悬在单位头顶。</summary>
        private static bool IsGroundFx(string id)
        {
            if (id == null) return false;
            return id.Contains("vortex") || id.Contains("magma") || id.Contains("eldenring")
                || id.Contains("constellation") || id.Contains("powerchords") || id.Contains("kabooms")
                || id.Contains("explosion") || id == "vfx_bighit" || id == "vfx_tentacles"
                || id == "vfx_worm" || id == "vfx_wheel" || id == "vfx_hyperspeed"
                || id.StartsWith("art_rune");
        }

        /// <summary>特效纵向中心偏移（单位高≈0.33×scale）：
        /// 地面类贴地（0.02）；护盾/符环类罩身体中心（0.17）；普通命中贴躯干（0.08，不再是"头顶"）。</summary>
        private static float CenterOffset(string id, float scale)
        {
            if (id == null) return 0.08f * scale;
            if (IsGroundFx(id)) return 0.02f * scale;
            if (id.StartsWith("shield_") || id == "art_arcaneshield") return 0.17f * scale;   // v0.19.5 奥术光膜罩身体中心
            return 0.08f * scale;
        }

        /// <summary>投射物：自 from 飞向 to（FaceVel 朝向 + 自旋），飞行 0.4s 恰好到点。
        /// v0.18.69：px=1/帧宽归一化（世界尺寸=Size 参数，不再随素材帧宽漂移）；起点从单位身体中部飞出。
        /// v0.18.92：投射体颜色跟随异能专属派生色。
        /// v0.21.2：飞行 0.26s→0.4s（近战距离也能看清弹道；原太快"瞬移到目标"被当成贴脸炸）。</summary>
        private static void SpawnProjectile(string id, Vector2 from, Vector2 to, float scale, Color? tint = null)
        {
            var frames = Load(id);
            if (frames == null || frames.Length == 0) return;
            Vector2 dir = to - from;
            float dist = Mathf.Max(0.5f, dir.magnitude);
            dir /= dist;
            float fly = 0.4f;
            float frameW = frames[0].rect.width;
            if (frameW <= 0f) frameW = 128f;
            float px = 1f / frameW;
            // v0.18.91：起点从单位身体中心飞出（脚底+0.17×体型），原 0.3×scale 在头顶
            var p = CombatFxCore.Spawn(frames, from + new Vector2(0f, 0.17f * scale), px, fly, FxLayer.Top, 34);
            if (p == null) return;
            Color tc = tint ?? Color.white;
            // v0.21.6.2 尺寸定标（反编译 ProjectileAsset/ProjectileLibrary 实锤）：
            //   原版投射物 scale_target=0.075~0.2（弹丸级）、scale_start=0.035~0.1，
            //   飞行从小变大。此前 0.34→0.22 = 1.4 倍单位宽（比原版大 2~4 倍）且方向相反。
            //   修正 0.1→0.16（命中时≈0.64 倍单位宽，弹丸级）。
            p.Sized(0.1f * scale, 0.16f * scale)
             .Color(new Color(tc.r, tc.g, tc.b, 0.7f), new Color(tc.r, tc.g, tc.b, 0f))
             .Moving(dir * (dist / fly), 0f, 0.5f, 0f);
            p.FaceVel = true;
            p.Spin = UnityEngine.Random.Range(4f, 8f);
        }

        /// <summary>
        /// 单层特效播放（px 0.01 基准；尺寸=样式组×单位缩放；alpha 渐隐；可延迟、可绑定单位）。
        /// fx_* 前缀走原版 EffectsLibrary（原版内置图，自动回池）。
        /// v0.18.92：tint 为异能专属派生色（null=原白色），应用于特效主体着色。
        /// </summary>
        private static void SpawnFx(string id, Vector2 pos, FxLayer layer, int order, bool strong,
            float scale = 1f, float delay = 0f, Actor follow = null, Color? tint = null)
        {
            // 原版特效（fx_* 前缀）：走 EffectsLibrary（原版内置图，自动回池）。
            // v0.18.67：spawn 后按单位缩放 prepare（原版资产默认 1.0 偏大，压到 0.5×单位缩放）。
            if (id.StartsWith("fx_"))
            {
                try
                {
                    WorldTile tile = World.world.GetTileSimple((int)pos.x, (int)pos.y);
                    if (tile != null)
                    {
                        var fx = EffectsLibrary.spawn(id, tile, null, null, 0f, -1f, -1f, null);
                        if (fx != null)
                        {
                            // v0.18.99：特效中心按定位贴地/贴体（原固定 0.17 悬在头顶）
                            fx.prepare(pos + new Vector2(0f, CenterOffset(id, scale)), 1f);
                            // v0.18.91：帧宽归一化——目标世界宽 = 0.25×体型（人类 actor_scale≈0.25，
                            // 1 倍单位宽≈0.25 世界单位，与西幻奥术护盾 0.24 对齐）
                            CombatFxCore.ScaleFxToWorldWidth(fx, 0.25f * scale);
                            // v0.18.92：原版特效同样跟随异能专属派生色
                            if (tint.HasValue && fx.sprite_renderer != null)
                            {
                                Color fc = fx.sprite_renderer.color;
                                fx.sprite_renderer.color = new Color(tint.Value.r, tint.Value.g, tint.Value.b, fc.a);
                            }
                        }
                    }
                }
                catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[AbilityFx] 原版特效 " + id + ": " + dsEx.Message); }
                return;
            }
            var frames = Load(id);
            if (frames == null || frames.Length == 0) return;
            Style(id, out float size, out float life);
            // v0.18.69：尺寸按帧宽归一化——世界宽 = px×帧宽×s，素材帧宽 55~140px 差异巨大，
            // 固定 px 会导致同 size 的特效世界尺寸漂移近一倍（"好多大小不对"根因）。
            // px = 1/帧宽 → 世界宽恒等于 s（s 即期望世界宽，≈人类单位 0.55）。
            float frameW = frames[0].rect.width;
            if (frameW <= 0f) frameW = 128f;
            float px = 1f / frameW;
            // 特效中心对准单位身体中心（单位 current_position 是脚底；v0.18.91 由 0.3×scale 修正——
            // v0.18.99 再校准：地面类贴地、普通命中贴躯干，不再悬在头顶）
            Vector2 center = pos + new Vector2(0f, CenterOffset(id, scale));
            var p = CombatFxCore.Spawn(frames, center, px, life, layer, order);
            if (p == null) return;
            p.Delay = delay;
            p.FollowActor = follow;
            p.FollowOffset = new Vector2(0f, 0.17f * scale);   // 跟随单位时同样对准身体中心，护盾/光环套在身上
            float s = (strong ? size : size * 0.55f) * scale;
            p.Sized(s, s * 0.72f);
            // v0.18.72：竖素材（96x128 护盾）默认高=1.33×宽会显大——护盾压扁到 0.75（高≈0.32，只罩躯干）
            // v0.18.75：对齐西幻奥术护盾 alpha≈0.03 的"淡罩"观感——护盾透明度压到 0.2（淡可见不遮人）
            // v0.18.92：着色跟随异能专属派生色
            Color tc = tint ?? Color.white;
            if (id.StartsWith("shield_") || id == "art_arcaneshield")
            {
                // v0.19.5 奥术光膜护盾：圆形光膜 YRatio=1.0；alpha 0.2 淡罩（西幻奥术护盾观感）
                p.YRatio = 1f;
                p.Color(new Color(tc.r, tc.g, tc.b, 0.2f), new Color(tc.r, tc.g, tc.b, 0.05f));
                return;
            }
            if (strong)
            {
                // v0.18.69：非半透明命中特效不透明度从 0.92 压到 0.55——
                // 大块不透明会遮住地图和人物，半透明既能看清特效又不挡视野
                p.Color(new Color(tc.r, tc.g, tc.b, 0.55f), new Color(tc.r, tc.g, tc.b, 0f));
            }
            else
            {
                p.Color(new Color(tc.r, tc.g, tc.b, 0.34f), new Color(tc.r, tc.g, tc.b, 0f));
            }
        }
    }
}
