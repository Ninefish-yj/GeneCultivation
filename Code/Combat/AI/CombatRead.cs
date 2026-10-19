// ============================================================
// 读招反击（Read & Counter，v0.21.40）
// 现实格斗逻辑三件事（"见招拆招"的落地）：
//   1) 反震格挡（Block）：受重击瞬间，境界不低于攻击者 → 概率格挡卸力
//      （伤害 ×0.6）+ 后撤格挡姿态——被打不是只会挨，能"接下"。
//   2) 读招反击（Counter）：格挡成功后 0.45s 窗口内回敬一记轻击
//      （真实伤害 getHit + 击退）——拆完就反打。
//   3) 打断蓄势（Interrupt）：敌方蓄大招窗口内，附近敌对方冲上去打断
//      （小额伤害 + 施法者失衡）——大招不是无代价的。
// 克制原则：不碰原版 AI 状态机；仅异能战斗触发（不干扰原版观感）；
//   每单位冷却防刷屏；全部 try-catch；不新增任何素材。
// 挂载：AbilityStatusPatch.GetHit_Prefix / CombatMoveDirector.Charge /
//       CombatBrain.Tick（见对应调用点）。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using Code.Data;
using Code.Combat.Moves;

namespace Code.Combat.AI
{
    public static class CombatRead
    {
        private const float CounterWindow = 0.45f;      // 格挡后反击窗口
        private const float CounterTiming = 0.10f;      // 窗口后段才出手（先格后反的节奏）
        private const float ReadCooldown = 2.5f;        // 每单位读招冷却（防刷屏）
        private const float BlockChanceBase = 0.18f;    // 同级反震概率
        private const float BlockChancePerTier = 0.07f; // 每高一阶加成
        private const float BlockMitigate = 0.60f;      // 格挡卸力：伤害×0.6
        private const float CounterDmgPct = 0.04f;      // 反击伤害 = 自身最大血 4%（轻击）
        private const float InterruptDmgPct = 0.02f;    // 打断伤害 = 打断者最大血 2%
        private const float InterruptRange = 2.2f;      // 打断感知半径（格）
        private const float ReadThreshold = 0.06f;      // 伤害占比 ≥6% 才读招（刮痧不拆）

        private class CounterState
        {
            public float Until;
            public Actor Target;
        }

        private static readonly Dictionary<Actor, float> _readCooldown = new Dictionary<Actor, float>();
        private static readonly Dictionary<Actor, CounterState> _counters = new Dictionary<Actor, CounterState>();

        // ------------------------------------------------ 反震格挡 --

        /// <summary>受击时调用：返回伤害系数（1.0 正常 / 0.6 格挡卸力）。
        /// 成功格挡会开启反击窗口（由 Tick 驱动回敬轻击）。</summary>
        public static float TryBlock(Actor victim, Actor attacker, float damage, float maxHp)
        {
            if (victim == null || attacker == null || victim == attacker) return 1f;
            if (!IsGeneBattle(victim, attacker)) return 1f;
            int vt = CultivationData.GetRealmTier(victim);
            int at = CultivationData.GetRealmTier(attacker);
            if (vt < at) return 1f;                       // 低境界被压制，无法格挡（境界压制世界观）
            if (damage / Mathf.Max(1f, maxHp) < ReadThreshold) return 1f;

            float now = Time.unscaledTime;
            if (_readCooldown.TryGetValue(victim, out float cd) && now < cd) return 1f;

            float chance = BlockChanceBase + BlockChancePerTier * Mathf.Clamp(vt - at, 0, 4);
            if (UnityEngine.Random.value > chance) return 1f;

            _readCooldown[victim] = now + ReadCooldown;
            // 格挡姿态：轻微踉跄后撤（纯视觉位移，不碰 AI）
            Vector2 dir = victim.current_position - attacker.current_position;
            if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(1f, 0f);
            dir.Normalize();
            try { CombatImpactPose.Force(victim, dir, ImpactLevel.Stagger); }
            catch (System.Exception) { }
            // 开反击窗口
            _counters[victim] = new CounterState { Until = now + CounterWindow, Target = attacker };
            return BlockMitigate;
        }

        // ------------------------------------------------ 读招反击 --

        /// <summary>每帧驱动（CombatBrain.Tick 调用）：窗口后段回敬轻击。</summary>
        public static void Tick(float dt)
        {
            if (_counters.Count == 0) return;
            float now = Time.unscaledTime;
            List<Actor> done = null;
            foreach (var kv in _counters)
            {
                CounterState s = kv.Value;
                if (s == null || s.Target == null || !s.Target.isAlive())
                {
                    if (done == null) done = new List<Actor>();
                    done.Add(kv.Key);
                    continue;
                }
                if (now >= s.Until)
                {
                    if (done == null) done = new List<Actor>();
                    done.Add(kv.Key);
                    continue;
                }
                if (now < s.Until - CounterWindow + CounterTiming) continue; // 后段才出手
                if (done == null) done = new List<Actor>();
                done.Add(kv.Key);
                CounterStrike(kv.Key, s.Target);
            }
            if (done != null)
            {
                for (int i = 0; i < done.Count; i++) _counters.Remove(done[i]);
            }
        }

        /// <summary>反击出手：真实轻击伤害 + 击退（见招拆招）。</summary>
        private static void CounterStrike(Actor self, Actor target)
        {
            if (self == null || !self.isAlive()) return;
            try
            {
                float dmg = self.getMaxHealth() * CounterDmgPct;
                target.getHit(dmg, false, AttackType.Other, self, true, true, false);
                Vector2 dir = target.current_position - self.current_position;
                if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(1f, 0f);
                dir.Normalize();
                CombatImpactPose.Force(target, dir, ImpactLevel.Stagger);
            }
            catch (System.Exception) { }
        }

        // ------------------------------------------------ 打断蓄势 --

        /// <summary>敌方蓄大招窗口内：附近敌对方概率冲上去打断（小额伤害+失衡）。
        /// 由 CombatMoveDirector.Charge 在蓄势时调用。</summary>
        public static void NotifyCharge(Actor caster, Vector2 pos, int tier)
        {
            if (caster == null || !caster.isAlive() || World.world == null) return;
            WorldTile tile = World.world.GetTileSimple((int)pos.x, (int)pos.y);
            if (tile == null) return;
            var nearby = Finder.getUnitsFromChunk(tile, 8, InterruptRange, false);
            if (nearby == null) return;
            float now = Time.unscaledTime;
            foreach (Actor foe in nearby)
            {
                if (foe == null || !foe.isAlive() || foe == caster) continue;
                if (!IsGeneBattle(caster, foe)) continue;
                if (!CombatBrain.IsEnemy(caster, foe)) continue;
                if (_readCooldown.TryGetValue(foe, out float cd) && now < cd) continue;
                int ft = CultivationData.GetRealmTier(foe);
                float chance = 0.10f + 0.05f * Mathf.Clamp(ft - tier, 0, 4);
                if (UnityEngine.Random.value > chance) continue;
                _readCooldown[foe] = now + ReadCooldown;
                try { caster.getHit(foe.getMaxHealth() * InterruptDmgPct, false, AttackType.Other, foe, true, true, false); }
                catch (System.Exception) { }
                Vector2 d = caster.current_position - foe.current_position;
                if (d.sqrMagnitude < 0.0001f) d = new Vector2(1f, 0f);
                try { CombatImpactPose.Force(caster, d, ImpactLevel.Fluster); }
                catch (System.Exception) { }
                break; // 一个打断者就够
            }
        }

        // ------------------------------------------------ 门禁 --

        /// <summary>异能战斗门禁：双方任一为觉醒者或原型单位才读招（不干扰原版观感）。</summary>
        public static bool IsGeneBattle(Actor a, Actor b)
        {
            if (a == null || b == null) return false;
            try
            {
                return (CultivationData.IsAscended(a) || Code.Realm.Prototype.PrototypeRegistry.HasPrototype(a))
                    && (CultivationData.IsAscended(b) || Code.Realm.Prototype.PrototypeRegistry.HasPrototype(b));
            }
            catch (System.Exception) { return false; }
        }
    }
}
