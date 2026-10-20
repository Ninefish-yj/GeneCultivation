// ============================================================
// 异能特效层（v0.21.46 全程序化版）
// 攻击/防守触发均路由到 AbilityFx.PlayAbilityFx（每异能专属程序化图元）。
// v0.21.46 移除原版 fx_* 兜底路径（GetAttackFx/GetDefendFx/PlayAt）——
// 异能单位必有 PrototypeDef，原版兜底永远不可达，且含 EffectsLibrary.spawn 原版图。
// 纯展示层，不写任何业务数据。
// ============================================================

using UnityEngine;
using Code.Combat.Fx;

namespace Code.Realm.Prototype
{
    public static class PrototypeFx
    {
        /// <summary>异能特效总开关（默认开；卡顿时可关）</summary>
        public static bool Enabled = true;

        /// <summary>攻击触发：异能级专属程序化特效（目标位置双层演出）+ 突进拖影</summary>
        public static void PlayAttackFx(Actor attacker, Actor target, PrototypeTag tag, bool force = false)
        {
            if (!Enabled || attacker == null) return;
            if (target == null || !target.isAlive()) return;
            var def = PrototypeRegistry.GetForActor(attacker);
            if (def == null) return;
            AbilityFx.PlayAbilityFx(attacker, target, def, true, force);
            CombatFxDirector.DashStrike(attacker, target.current_position);
            // 战斗动作：斩系标签=御剑剑气斩，重击系标签=震地重击
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
        }

        /// <summary>防守触发：异能级专属程序化特效（自身位置）</summary>
        public static void PlayDefendFx(Actor defender, PrototypeTag tag, float chance = 0.6f)
        {
            if (!Enabled || defender == null) return;
            if (UnityEngine.Random.value > chance) return;
            var def = PrototypeRegistry.GetForActor(defender);
            if (def == null) return;
            AbilityFx.PlayAbilityFx(defender, defender, def, false, false);
        }
    }
}
