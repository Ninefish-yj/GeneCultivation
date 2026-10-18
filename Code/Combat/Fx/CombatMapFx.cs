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
        public static void OnHit(Actor by, Vector2 pos, PrototypeTag mainTag, PrototypeTag subTag)
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

            // L0：贴地持续域（视觉层，不改地形；任何境界都铺，意象按标签）
            string groundFx = GroundFxFor(mainTag, subTag);
            if (!string.IsNullOrEmpty(groundFx))
                SpawnGround(groundFx, pos, by, tier);

            // v0.21.2 冰系命中水面：封冻域（视觉持久冰霜 + 尝试降温；不破坏水面 tile）
            if (IsFrost(mainTag) || IsFrost(subTag)) FreezeWater(pos, by);

            // L1/L2：地形响应（境界分级；凡/玄/灵以下不动大地）
            int force = DSTerrainDestruction.ForceForTier(tier);
            if (force <= 0) return;
            if (!CombatFxDirector.CanFull(pos)) return;   // 屏幕内才破坏（离屏不挖地）

            // Light 尺度（克制：异能普攻不按重击破坏）+ extra +1 →
            //   6~8阶烧焦(force1) · 9~10阶炸坑(force2) · 11~12阶岩浆(force3) · 13阶二次降格"陆地打没"(force4)
            DSTerrainDestruction.Blast(by, pos, 1.2f, DSTerrainDestruction.DestructionScale.Light,
                keep: by.current_position, fire: IsFire(mainTag), extra: 1);
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
                    // 冻结成功后铺持久亮蓝冰霜视觉（封冻观感，2.5s）
                    var frames = AbilityFx.Load("magic_ice_c");
                    if (frames != null && frames.Length > 0)
                    {
                        float frameW = frames[0].rect.width;
                        if (frameW <= 0f) frameW = 128f;
                        var p = CombatFxCore.Spawn(frames, pos + new Vector2(0f, 0.02f), 1f / frameW, 2.5f, FxLayer.Behind, 8);
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
        private static string GroundFxFor(PrototypeTag main, PrototypeTag sub)
        {
            // 火系：地面起火平铺（火焰持续燃烧）
            if (main == PrototypeTag.Burn || main == PrototypeTag.Plasma || main == PrototypeTag.Berserk)
                return "vfx_ditheredfire";
            // 冰系：地面结霜（冰雾冰锥贴地）
            if (main == PrototypeTag.Frost || main == PrototypeTag.Frostbite || main == PrototypeTag.Slow)
                return "magic_ice_b";
            // 雷系：劈焦弧光（锯齿闪电贴地）
            if (main == PrototypeTag.Thunder || main == PrototypeTag.Chain || main == PrototypeTag.Stun)
                return "magic_lightning";
            // 水系：水波蔓延
            if (main == PrototypeTag.Water) return "soul_water";
            // 毒系：毒雾蔓延
            if (main == PrototypeTag.Poison) return "magic_smoke";
            // 暗系：暗影领域（紫暗影尖刺铺地）
            if (main == PrototypeTag.Shadow || main == PrototypeTag.Curse || main == PrototypeTag.Fear
                || main == PrototypeTag.Aging || main == PrototypeTag.Weakness) return "soul_yin";
            // 引力/念力/磁/声/振动/幻象：引力旋涡（贴地旋转）
            if (main == PrototypeTag.Gravity || main == PrototypeTag.Telekinesis
                || main == PrototypeTag.Magnetism || main == PrototypeTag.Sound
                || main == PrototypeTag.Vibration || main == PrototypeTag.Illusion) return "vfx_vortex";
            // 召唤/分身：三环发光领域
            if (main == PrototypeTag.Summon || main == PrototypeTag.Clone) return "vfx_eldenring";
            // 束缚/再生/荆棘（木系意象）：藤蔓铺地
            if (main == PrototypeTag.Entangle || main == PrototypeTag.Regeneration
                || main == PrototypeTag.Thorns) return "soul_wood";
            // 光/治疗/祝福/净化：圣光地面
            if (main == PrototypeTag.Light || main == PrototypeTag.Heal || main == PrototypeTag.HealAlly
                || main == PrototypeTag.Blessing || main == PrototypeTag.Cleanse) return "soul_yang";
            // 灵魂/吸血/汲取/血：灵魂火焰铺地
            if (main == PrototypeTag.Soul || main == PrototypeTag.Lifesteal
                || main == PrototypeTag.Drain || main == PrototypeTag.Blood) return "vfx_anima";
            // 石化/土：土岩结晶铺地
            if (main == PrototypeTag.Petrify || main == PrototypeTag.Earth) return "soul_earth";
            // 其余标签（暴击/击退/破甲/标记/反射/闪避/传送/隐身等）：无地面域，不硬凑
            return null;
        }

        /// <summary>贴地持续域：铺在命中位置地面上，横向展宽、低透明、持续 ~1.2s 淡出</summary>
        private static void SpawnGround(string id, Vector2 pos, Actor by, int tier)
        {
            var frames = AbilityFx.Load(id);
            if (frames == null || frames.Length == 0) return;
            AbilityFx.Style(id, out float size, out float life);
            float frameW = frames[0].rect.width;
            if (frameW <= 0f) frameW = 128f;
            float px = 1f / frameW;

            // 贴地（地面类特效 CenterOffset≈0.02×scale；领域级平铺不悬空）
            float scale = AbilityFx.UnitScale(by);
            Vector2 center = pos + new Vector2(0f, 0.02f * scale);
            // 持续时长：1.2s 左右（比单体命中 0.5s 长——"地面起火持续燃烧"的观感）
            float lifeSec = Mathf.Clamp(life * 1.6f, 1.0f, 1.6f);
            var p = CombatFxCore.Spawn(frames, center, px, lifeSec, FxLayer.Behind, 8);
            if (p == null) return;
            // 地面域：横向铺展（宽 1.6× 期望 size，高压到 0.45×——贴地不遮单位）
            float s = size * 1.6f * scale;
            p.Sized(s, s * 0.45f);
            // 半透明贴地淡出（不挡地图；领域级再降透明度）
            bool domain = s > 1.2f;
            p.Color(new Color(1f, 1f, 1f, domain ? 0.38f : 0.55f), new Color(1f, 1f, 1f, 0f));
            // 领域/旋涡类轻微自转（贴地转；火/冰/毒平铺类不自转——素材帧本身是生长动画）
            if (id == "vfx_vortex" || id == "vfx_eldenring" || id == "soul_yin") p.Spin = 30f;
        }
    }
}
