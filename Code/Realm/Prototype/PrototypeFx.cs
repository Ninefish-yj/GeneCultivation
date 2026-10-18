// ============================================================
// 异能特效层（v0.18.28 → v0.18.66）
// v0.18.66：接入异能级自定义特效 AbilityFx（GameResources/effects 素材，每异能专属组合）。
// 优先异能级（标签主体+家族氛围两层）；自定义素材缺失时回退原版 fx_* 粒子。
// 纯展示层，不写任何业务数据；失败静默降级（try/catch + 出镜率限制防刷屏）。
// 播放模式 = DisasterManager 已验证：EffectsLibrary.spawn(effectName, tile, ...)
// ============================================================

using UnityEngine;
using Code.Combat.Fx;

namespace Code.Realm.Prototype
{
    public static class PrototypeFx
    {
        /// <summary>异能特效总开关（默认开；卡顿时可关）</summary>
        public static bool Enabled = true;

        /// <summary>攻击标签 → 原版特效 id（目标位置播放）。
        /// v0.21.6 已删除：原版通用光效（fx_hit/fx_hearts/fx_cast_* 等）意象与异能不符、
        /// 无每异能辨识度（"什么异能都是紫光蓝光"），全部改走异能专属程序化图元
        /// （AbilityFx.PlayAbilityFx 每异能独立形态×seed×专属色）。兜底路径不再播原版通用特效。</summary>
        private static string GetAttackFx(PrototypeTag tag)
        {
            return null;
        }

        /// <summary>防守标签 → 原版特效 id（自身位置播放）。
        /// v0.21.6 同攻击层：原版通用光效（fx_cast_*/fx_shield_hit 等）意象不符，
        /// 全部改走异能专属程序化图元；兜底不再播原版通用特效。</summary>
        private static string GetDefendFx(PrototypeTag tag)
        {
            return null;
        }

        /// <summary>攻击触发：优先异能级自定义特效（目标位置双层演出），回退原版粒子（35% 出镜率）</summary>
        public static void PlayAttackFx(Actor attacker, Actor target, PrototypeTag tag, bool force = false)
        {
            if (!Enabled || attacker == null) return;
            if (target == null || !target.isAlive()) return;
            // v0.18.66 异能级：查单位异能定义，命中即播专属特效（50% 出镜，内部再门禁）+ 冲刺动作感
            var def = PrototypeRegistry.GetForActor(attacker);
            if (def != null)
            {
                AbilityFx.PlayAbilityFx(attacker, target, def, true, force);
                CombatFxDirector.DashStrike(attacker, target.current_position);   // 突进拖影+残影+命中环
                // v0.18.70 战斗动作：斩系标签=御剑剑气斩，重击系标签=震地重击
                if (def.MainTag == PrototypeTag.MultiShot || def.MainTag == PrototypeTag.Execute
                    || def.SubTag == PrototypeTag.MultiShot || def.SubTag == PrototypeTag.Execute)
                {
                    CombatFxDirector.SlashArc(attacker, target.current_position);
                }
                else if (def.MainTag == PrototypeTag.Knockback || def.MainTag == PrototypeTag.Gravity
                    || def.MainTag == PrototypeTag.Petrify
                    || def.SubTag == PrototypeTag.Knockback || def.SubTag == PrototypeTag.Gravity
                    || def.SubTag == PrototypeTag.Petrify)
                {
                    CombatFxDirector.GroundSlam(attacker, target.current_position);
                }
                return;
            }
            string fx = GetAttackFx(tag);
            if (string.IsNullOrEmpty(fx)) return;
            if (!force && UnityEngine.Random.value > 0.35f) return;
            PlayAt(target, fx);
        }

        /// <summary>防守触发：优先异能级自定义特效（自身位置双层演出），回退原版粒子（chance 出镜率）</summary>
        public static void PlayDefendFx(Actor defender, PrototypeTag tag, float chance = 0.6f)
        {
            if (!Enabled || defender == null) return;
            var def = PrototypeRegistry.GetForActor(defender);
            if (def != null)
            {
                AbilityFx.PlayAbilityFx(defender, defender, def, false, false);
                return;
            }
            string fx = GetDefendFx(tag);
            if (string.IsNullOrEmpty(fx)) return;
            if (UnityEngine.Random.value > chance) return;
            PlayAt(defender, fx);
        }

        /// <summary>在单位位置播放原版特效（模组已验证模式：DisasterManager.cs）。
        /// v0.18.67：spawn 后按单位体型缩放（原版资产默认 1.0 偏大，压到 0.5×体型）。</summary>
        private static void PlayAt(Actor actor, string fxId)
        {
            if (actor == null || !actor.isAlive() || World.world == null) return;
            try
            {
                var pos = actor.current_position;
                WorldTile tile = World.world.GetTileSimple((int)pos.x, (int)pos.y);
                if (tile == null) return;
                var fx = EffectsLibrary.spawn(fxId, tile, null, null, 0f, -1f, -1f, null);
                if (fx != null)
                {
                    float unitScale = Mathf.Max(0.3f, (float)actor.asset.actor_size / 13f);
                    // v0.18.91：对准身体中心（脚底+0.17×体型），原贴脚底
                    pos.y += 0.17f * unitScale;
                    fx.prepare(pos, 1f);
                    // v0.18.91：帧宽归一化——目标世界宽 = 0.25×体型（人类 actor_scale≈0.25，1 倍单位宽≈0.25 世界单位）
                    CombatFxCore.ScaleFxToWorldWidth(fx, 0.25f * unitScale);
                }
            }
            catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[GeneCultivation] 异能特效失败 " + fxId + ": " + dsEx.Message); }
        }
    }
}
