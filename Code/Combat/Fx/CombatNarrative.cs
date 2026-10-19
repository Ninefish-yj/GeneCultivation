// ============================================================
// 战斗叙事层（Combat Narrative，v0.21.43）
// 把异能战斗从"单招堆叠"变成"有起承转合的一场戏"：
//   ① 觉醒对峙（AwakeningStandoff）：高阶觉醒者单挑三幕演出
//      （幕一·对峙暗场 → 幕二·气势碰撞 → 幕三·残血终结）
//   ② 绝境爆发（LastStand）：残血反扑的怒吼演出（击退围攻）
//   ③ 斩杀终幕（KillFinale）：击杀强敌的电影化收尾
//   ④ 援护救援（GuardianRescue）：友方被围时的救援突进
// 纯表现层：不写业务数据、不改伤害/AI/境界；全部命中驱动
// （OnHit 由 AbilityStatusPatch 在伤害计算完成后调用，零每帧
//   扫描成本）；每情境独立冷却 + 全场同时最多一场对决演出 +
//   异常隔离。复用 Spotlight/Cinema/Schedule/Shockwave 基建，
//   不新增素材（零贴图，遵守性能红线）。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using Code.Combat.AI;
using Code.Data;
using Code.Realm.Prototype;

namespace Code.Combat.Fx
{
    public static class CombatNarrative
    {
        // ---- 情境门槛（数值可调）----
        private const float DuelMinTier = 9f;      // 圣体+ 才触发觉醒对峙
        private const float DuelMaxDist = 3.5f;    // 格
        private const float DuelCooldown = 30f;
        private const float LastStandHpPct = 0.18f;   // 残血线
        private const float LastStandMinTier = 5f;
        private const float LastStandCooldown = 25f;
        private const float FinaleCooldown = 12f;
        private const float RescueHpPct = 0.25f;
        private const float RescueCooldown = 20f;
        private const float RescueRange = 3.0f;
        private const int    RescueMinFoes = 2;

        private static readonly Dictionary<Actor, float> _cd = new Dictionary<Actor, float>(16);
        private static bool _duelActive;   // 全场同时最多一场对决
        private static float _duelEnd;

        /// <summary>命中演出入口（由 AbilityStatusPatch 在全部伤害计算完成后调用）。
        /// 依次尝试四个情境；任一失败不影响其余（异常隔离）。</summary>
        public static void OnHit(Actor attacker, Actor victim, float damage)
        {
            if (attacker == null || victim == null || !victim.isAlive()) return;
            if (World.world == null || World.world.isPaused()) return;
            if (!CombatRead.IsGeneBattle(attacker, victim)) return;   // 门禁：不干扰原版观感
            float now = Time.unscaledTime;
            Cleanup(now);
            try { TryAwakeningStandoff(attacker, victim, now); } catch (System.Exception e) { Log(e); }
            try { TryLastStand(victim, now); }                       catch (System.Exception e) { Log(e); }
            try { TryKillFinale(attacker, victim, damage, now); }     catch (System.Exception e) { Log(e); }
            try { TryGuardianRescue(victim, now); }                   catch (System.Exception e) { Log(e); }
        }

        // ------------------------------------------------ ① 觉醒对峙 --

        /// <summary>高阶觉醒者单挑：三幕演出（对峙 → 气势碰撞 → 残血终结）。</summary>
        private static void TryAwakeningStandoff(Actor attacker, Actor victim, float now)
        {
            if (_duelActive || now < _duelEnd) return;
            if (CultivationData.GetRealmTier(attacker) < DuelMinTier) return;
            if (CultivationData.GetRealmTier(victim) < DuelMinTier) return;
            if (Vector2.Distance(attacker.current_position, victim.current_position) > DuelMaxDist) return;
            if (_cd.TryGetValue(attacker, out float cd) && now < cd) return;

            _cd[attacker] = now + DuelCooldown;
            _duelActive = true;
            _duelEnd = now + 3.2f;

            // 幕一·对峙：双焦点暗场（脚下光环+头顶标记）+ 开局慢动作
            CombatSpotlight.Focus(attacker, victim, 2.8f);
            CombatFxCinema.SlowMo(0.6f, 0.35f);

            // 幕二·气势碰撞：1.2s 后中点冲击波（两股气势对撞）
            CombatFxDirector.Schedule(1.2f, () =>
            {
                if (attacker == null || victim == null || !attacker.isAlive() || !victim.isAlive()) return;
                Vector2 mid = (attacker.current_position + victim.current_position) * 0.5f;
                CombatFxDirector.Shockwave(mid, 3.2f);
            });

            // 幕三·终局：2.4s 后任一方残血 → 终结演出（慢动作 + 终结爆）
            CombatFxDirector.Schedule(2.4f, () =>
            {
                if (attacker == null || victim == null) return;
                bool aLow = attacker.isAlive() && victim.isAlive()
                    && attacker.getHealth() <= attacker.getMaxHealth() * 0.35f;
                bool vLow = attacker.isAlive() && victim.isAlive()
                    && victim.getHealth() <= victim.getMaxHealth() * 0.35f;
                if (aLow || vLow)
                {
                    Actor low = aLow ? attacker : victim;
                    CombatFxCinema.SlowMo(0.35f, 0.6f);
                    CombatFxDirector.SpellBurst(low, low.current_position,
                        Mathf.Max(CultivationData.GetRealmTier(low), 9));
                }
            });

            CombatFxDirector.Schedule(3.0f, () => _duelActive = false);
        }

        // ------------------------------------------------ ② 绝境爆发 --

        /// <summary>残血觉醒者反扑：怒吼冲击环 + 击退围攻者 + 红色聚焦。</summary>
        private static void TryLastStand(Actor victim, float now)
        {
            if (victim.getHealth() > victim.getMaxHealth() * LastStandHpPct) return;
            if (CultivationData.GetRealmTier(victim) < LastStandMinTier) return;
            if (_cd.TryGetValue(victim, out float cd) && now < cd) return;
            _cd[victim] = now + LastStandCooldown;

            Vector2 pos = victim.current_position;
            CombatSpotlight.Pulse(victim, 1.1f);
            CombatFxDirector.Shockwave(pos, 2.6f);
            CombatFxDirector.Schedule(0.1f, () =>
            {
                if (victim != null && victim.isAlive()) CombatFxCinema.SlowMo(0.6f, 0.3f);
            });

            // 击退周围敌人（表现位移，不改伤害）
            WorldTile tile = World.world.GetTileSimple((int)pos.x, (int)pos.y);
            if (tile == null) return;
            var nearby = Finder.getUnitsFromChunk(tile, 8, 2.2f, false);
            if (nearby == null) return;
            foreach (Actor foe in nearby)
            {
                if (foe == null || !foe.isAlive() || foe == victim) continue;
                if (!CombatBrain.IsEnemy(victim, foe)) continue;
                Vector2 d = foe.current_position - victim.current_position;
                if (d.sqrMagnitude < 0.0001f) d = new Vector2(1f, 0f);
                CombatFxDirector.ActorDash(foe, d.normalized, 1.2f, 0.22f);
            }
        }

        // ------------------------------------------------ ③ 斩杀终幕 --

        /// <summary>本次伤害将击杀强敌：慢动作 + 终结冲击波 + 聚焦攻击者。</summary>
        private static void TryKillFinale(Actor attacker, Actor victim, float damage, float now)
        {
            if (damage <= 0f) return;
            float hp = victim.getHealth();           // prefix 时机=受伤前血量
            if (hp - damage > 0f) return;            // 本次伤害不足以致死
            if (!(CultivationData.IsAscended(victim) || PrototypeRegistry.HasPrototype(victim))) return;
            int at = CultivationData.GetRealmTier(attacker);
            int vt = CultivationData.GetRealmTier(victim);
            if (vt < at && vt < DuelMinTier) return; // 碾压杂兵不演（只对同级/高阶强敌）
            if (_cd.TryGetValue(attacker, out float cd) && now < cd) return;
            _cd[attacker] = now + FinaleCooldown;

            CombatFxCinema.SlowMo(0.5f, 0.55f);
            CombatFxDirector.SpellBurst(attacker, victim.current_position, at);
            CombatSpotlight.Pulse(attacker, 1.2f);
        }

        // ------------------------------------------------ ④ 援护救援 --

        /// <summary>友方觉醒者残血被 ≥2 敌人包围：附近觉醒者疾冲救援。</summary>
        private static void TryGuardianRescue(Actor victim, float now)
        {
            if (victim.getHealth() > victim.getMaxHealth() * RescueHpPct) return;
            if (!(CultivationData.IsAscended(victim) || PrototypeRegistry.HasPrototype(victim))) return;
            if (_cd.TryGetValue(victim, out float cd) && now < cd) return;

            Vector2 pos = victim.current_position;
            WorldTile tile = World.world.GetTileSimple((int)pos.x, (int)pos.y);
            if (tile == null) return;
            var nearby = Finder.getUnitsFromChunk(tile, 8, RescueRange, false);
            if (nearby == null) return;

            int foeCount = 0;
            foreach (Actor u in nearby)
            {
                if (u == null || !u.isAlive() || u == victim) continue;
                if (CombatBrain.IsEnemy(victim, u)) foeCount++;
            }
            if (foeCount < RescueMinFoes) return;
            _cd[victim] = now + RescueCooldown;

            foreach (Actor u in nearby)
            {
                if (u == null || !u.isAlive() || u == victim) continue;
                if (CombatBrain.IsEnemy(victim, u)) continue;
                if (!(CultivationData.IsAscended(u) || PrototypeRegistry.HasPrototype(u))) continue;
                Vector2 d = victim.current_position - u.current_position;
                if (d.sqrMagnitude < 0.0001f) d = new Vector2(1f, 0f);
                CombatFxDirector.ActorDash(u, d.normalized, 2.4f, 0.3f);
                CombatFxDirector.Schedule(0.05f, () => CombatSpotlight.Pulse(u, 0.9f));
                CombatFxDirector.Schedule(0.3f, () =>
                {
                    if (u != null && u.isAlive()) CombatFxDirector.Shockwave(u.current_position, 1.8f);
                });
                break; // 一个救援者就够
            }
        }

        // ------------------------------------------------------ 清理 --

        private static void Cleanup(float now)
        {
            if (_cd.Count <= 16) return;
            foreach (var k in new List<Actor>(_cd.Keys))
                if (k == null || !k.isAlive() || now > _cd[k] + 60f) _cd.Remove(k);
        }

        private static void Log(System.Exception e)
        {
            Code.Core.DSDebug.Verbose("[CombatNarrative] " + e.Message);
        }
    }
}
