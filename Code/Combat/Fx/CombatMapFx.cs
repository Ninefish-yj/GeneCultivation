// ============================================================
// 地图融合表现层（v0.21.1）
// 解决"所有特效都只是砸过去、没有和地图玩法融合"：
// 异能攻击命中后，按异能标签给目标地面"融合形态"——
//   L0 贴地持续域（视觉，零地形改动）：火=地面起火、冰=地面结霜、雷=劈焦弧光、
//      毒=毒雾蔓延、暗=暗影领域、引力=旋涡、领域=三环、木=藤蔓铺地、光=圣光地面…
//   L1 微地形响应（境界≥6）：命中烧焦（add_burned）/ 炸坑（force 1~2）
//   L2 真·地形破坏（境界≥11 登神）：岩浆化；13 阶超神命中二次降格"陆地打没"
//     ——复用 DSTerrainDestruction.Blast 的分级破坏（境界×轻重→0~4 力）
// 克制：每单位 1.2s 冷却一次、世界运行+暂停检查、屏幕内才触发、施法者脚下保护。
// 复用：AbilityFx.Load/Style/UnitScale（素材帧+尺寸+体型）、DSTerrainDestruction.Blast
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using Code.Data;
using Code.Realm.Prototype;

namespace Code.Combat.Fx
{
    public static class CombatMapFx
    {
        private static readonly Dictionary<Actor, float> _cooldown = new Dictionary<Actor, float>();
        private const float CD = 1.2f;   // 每单位地图融合冷却（防每次命中都改地形/铺地面）

        /// <summary>攻击命中入口（AbilityFx 攻击分支调用）：标签 → 地图融合形态</summary>
        public static void OnHit(Actor by, Vector2 pos, PrototypeTag mainTag, PrototypeTag subTag, PrototypeDef def = null)
        {
            if (by == null || !by.isAlive()) return;
            if (World.world == null || World.world.isPaused()) return;
            // 冷却克制
            float now = Time.unscaledTime;
            if (_cooldown.TryGetValue(by, out float last) && now - last < CD) return;
            _cooldown[by] = now;
            // 清理已死单位的冷却记录（防字典膨胀）
            if (_cooldown.Count > 128)
            {
                foreach (var k in new List<Actor>(_cooldown.Keys))
                    if (k == null || !k.isAlive()) _cooldown.Remove(k);
            }

            int tier = CultivationData.GetRealmTier(by);

            // L0：贴地持续域（视觉层，不改地形；任何境界都铺，意象按标签；
            // v0.21.9 异能级：色=异能派生色、帧节奏=异能 id 哈希——同标签不同异能地面域各异）
            var groundFx = GroundFxFor(mainTag, subTag, def);
            if (groundFx.Item1 >= 0)
                SpawnGround(groundFx, pos, by, tier, def);

            // v0.21.2 冰系命中水面：封冻域（视觉持久冰霜 + 尝试降温；不破坏水面 tile）
            if (IsFrost(mainTag) || IsFrost(subTag)) FreezeWater(pos, by);

            // L1/L2：地形响应（境界分级；凡/玄/灵以下不动大地）
            int force = DSTerrainDestruction.ForceForTier(tier);
            if (force <= 0) return;
            if (!CombatFxDirector.CanFull(pos)) return;   // 屏幕内才破坏（离屏不挖地）

            // v0.21.7 大规模地形破坏升级：登神(11+) 普攻即 Heavy（焚山煮海→真实裂地），
            // 真神(13+) 普攻即 Ultimate（"陆地打没"大范围重绘）。
            // 凡/玄/灵/圣维持 Light 克制尺度（异能普攻不按重击破坏）+ extra +1：
            //   6~8阶烧焦(force1) · 9~10阶炸坑(force2) · 11~12阶岩浆(force3) · 13阶二次降格(force4)
            var dScale = tier >= 13 ? DSTerrainDestruction.DestructionScale.Ultimate
                      : tier >= 11 ? DSTerrainDestruction.DestructionScale.Heavy
                      : DSTerrainDestruction.DestructionScale.Light;
            DSTerrainDestruction.Blast(by, pos,
                tier >= 13 ? 1.7f : tier >= 11 ? 1.4f : 1.2f,
                dScale, keep: by.current_position, fire: IsFire(mainTag), extra: 1);
        }

        /// <summary>该标签是否冰系（命中水面时触发封冻域）</summary>
        private static bool IsFrost(PrototypeTag tag)
        {
            return tag == PrototypeTag.Frost || tag == PrototypeTag.Frostbite || tag == PrototypeTag.Slow;
        }

        /// <summary>冰系命中水面 → 真冻结：复用原版地表冻结 API（反编译 0.51.x 已核实）——
        /// WorldTile.canBeFrozen() 判断 → tile.freeze(5) 按 freeze_to_id 换顶层冰面
        /// （frozen_low/frozen_high），温度回升由原版 WorldBehaviourTilesTemperatureUnFreeze 自动解冻。
        /// v0.21.3 从"视觉封冻"升级为原版真冻结；视觉冰霜域保留辅助观感。</summary>
        private static void FreezeWater(Vector2 pos, Actor by)
        {
            if (World.world == null) return;
            try
            {
                var tile = World.world.GetTileSimple((int)pos.x, (int)pos.y);
                if (tile == null || tile.Type == null || !tile.Type.liquid) return;
                // 原版真冻结：canBeFrozen + freeze（换 frozen 顶层 tile，温度回升自动解冻）
                if (tile.canBeFrozen())
                {
                    tile.freeze(5);
                    // 冻结成功后铺持久亮蓝冰霜视觉（封冻观感，2.5s；v0.21.9 程序化冰晶形态）
                    var frames = CombatFxArt.MakePrototypeFrames((int)(by.getID() * 37 + 11), AbilityFx.TagGlyphIndex(Code.Realm.Prototype.PrototypeTag.Frost), -1, 0);
                    if (frames != null && frames.Length > 0)
                    {
                        float frameW = frames[0].rect.width;
                        if (frameW <= 0f) frameW = 128f;
                        var p = CombatFxCore.Spawn(frames, pos + new Vector2(0f, 0.02f), 1f / frameW, 2.5f, FxLayer.Behind, 8);
                        if (p != null)
                        {
                            p.Loop = true;   // 冰晶形态循环涌动 = 冻结面持续发光
                        }
                        if (p != null)
                        {
                            float s = 1.6f * AbilityFx.UnitScale(by);
                            p.Sized(s, s * 0.45f);
                            p.Color(new Color(0.62f, 0.85f, 1f, 0.6f), new Color(0.62f, 0.85f, 1f, 0f));
                        }
                    }
                }
            }
            catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[CombatMapFx] FreezeWater: " + dsEx.Message); }
        }

        /// <summary>该标签是否"火系"（决定地形破坏出不出岩浆：火系砸穿=岩浆，冰/雷/暗=纯炸坑）</summary>
        private static bool IsFire(PrototypeTag tag)
        {
            return tag == PrototypeTag.Burn || tag == PrototypeTag.Plasma || tag == PrototypeTag.Berserk;
        }

        /// <summary>标签 → 贴地持续域素材（意象与大众认知对应；不铺=该标签无地面域，不硬凑）</summary>
        /// <summary>贴地形态映射（v0.21.9 全程序化：不再用素材图）。
        /// 返回 (主形态索引, 副形态索引(-1=无), 标签基准色, 是否自转)。形态复用
        /// AbilityFx.TagGlyphIndex 的 26 形态库，与命中爆同源——地图融合层与命中特效同视觉语言。</summary>
        private static (int, int, Color, bool) GroundFxFor(PrototypeTag main, PrototypeTag sub, PrototypeDef def = null)
        {
            bool spin = false;
            int m = AbilityFx.TagGlyphIndex(main);
            // 旋涡类（引力/念力/磁/声/振动/幻象）：Moon 环状旋涡意象 + 贴地自转
            if (main == PrototypeTag.Gravity || main == PrototypeTag.Telekinesis
                || main == PrototypeTag.Magnetism || main == PrototypeTag.Sound
                || main == PrototypeTag.Vibration || main == PrototypeTag.Illusion)
            {
                m = 15;   // GlyphMoonAt（环状旋涡）
                spin = true;
            }
            // 领域类（召唤/分身）：Burn 火环意象（seed 区分，贴地铺展成发光领域）
            if (main == PrototypeTag.Summon || main == PrototypeTag.Clone)
                m = 0;    // GlyphFlameAt（领域=持续燃烧的发光环）
            int s2 = -1;
            if (sub != main)
            {
                int sm = AbilityFx.TagGlyphIndex(sub);
                if (sm != m) s2 = sm;
            }
            // 异能级：def 存在时用异能专属派生色（标签基准±哈希偏色），否则回落标签基准色
            Color tint = def != null ? AbilityFx.DeriveTint(def) : AbilityFx.TagBaseColor(main);
            return (m, s2, tint, spin);
        }

        private static void SpawnGround((int, int, Color, bool) fx, Vector2 pos, Actor by, int tier, PrototypeDef def = null)
        {
            int mainIdx = fx.Item1, subIdx = fx.Item2;
            Color tint = fx.Item3;
            bool spin = fx.Item4;
            // 异能级帧节奏：seed=异能 id 哈希（同标签不同异能贴地域生长/爆散方向各异）
            int seed = (def != null && !string.IsNullOrEmpty(def.Id)) ? AbilityFx.StableSeed(def.Id) : (by != null ? (int)(by.getID() * 31 + 17) : 0);
            var frames = CombatFxArt.MakePrototypeFrames(seed, mainIdx, subIdx, Mathf.Max(0, tier));
            if (frames == null || frames.Length == 0) return;
            float frameW = frames[0].rect.width;
            if (frameW <= 0f) frameW = 128f;
            float px = 1f / frameW;

            // 贴地（地面类特效贴地铺展；领域级平铺不悬空）
            float scale = AbilityFx.UnitScale(by);
            Vector2 center = pos + new Vector2(0f, 0.02f * scale);
            // 持续时长：1.5s（贴地持续燃烧/蔓延的观感；帧动画循环播）
            float lifeSec = 1.5f;
            var p = CombatFxCore.Spawn(frames, center, px, lifeSec, FxLayer.Behind, 8);
            if (p == null) return;
            p.Loop = true;   // v0.21.9 循环播形态演化帧 = 持续生长-消散的贴地域（不再静态贴图）
            // 地面域：横向铺展（宽 1.6× 期望尺寸，高压到 0.45×——贴地不遮单位）
            float s = 0.45f * 1.6f * scale;
            p.Sized(s, s * 0.45f);
            // 半透明贴地淡出（不挡地图；领域级再降透明度），着色跟随标签基准色
            bool domain = s > 1.2f;
            p.Color(new Color(tint.r, tint.g, tint.b, domain ? 0.38f : 0.55f),
                    new Color(tint.r, tint.g, tint.b, 0f));
            // 领域/旋涡类轻微自转（贴地转；火/冰/毒平铺类不自转——帧动画本身是生长-消散循环）
            if (spin) p.Spin = 30f;
        }    }
}
