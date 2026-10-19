// ============================================================
// 战斗AI·招式决策器（v0.21.0）
// 让登神/使徒级异能单位在战斗中"像会打架"：
//   找目标 → 按异能标签选招式 → 残血/威胁比决定轻重 → 出招。
// 克制原则（延续全模组"纯表现不碰 AI"）：
//   —— 不修改单位的移动/寻路/攻击数据（原版行为树仍主导）；
//   —— 只触发招式演出 + 地形破坏 + 击停，做"战斗之神级"观感。
// 节流决策（0.4s 一拍），由 CombatFxFramePatch.Postfix 驱动。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using Code.Combat.Fx;
using Code.Combat.Moves;
using Code.Data;
using Code.Realm.Prototype;

namespace Code.Combat.AI
{
    public static class CombatBrain
    {
        /// <summary>决策周期（秒）：0.4s 一拍，避免每帧扫单位。</summary>
        private const float DecisionInterval = 0.4f;

        /// <summary>最近决策时间。</summary>
        private static float _lastDecision;

        /// <summary>参与战斗决策的最低境界（v0.21.5 放宽：凡/玄 2 阶以上异能单位也会用异能招式）。
        /// 世界观：凡人觉醒异能后战斗即会显化异能，不必等到 6 阶灵级——轻重随境界分级，
        /// 低阶只有轻招（Nudge/Slash/Chain），高阶才上重招与大招。</summary>
        private const int MinFightTier = 2;

        /// <summary>每帧驱动（FramePatch Postfix 调用；dt=0 时跳过=击停/冻结中不决策）。</summary>
        public static void Tick(float dt)
        {
            try
            {
                if (World.world == null || World.world.isPaused()) return;
                float now = Time.unscaledTime;
                if (now - _lastDecision < DecisionInterval) return;
                _lastDecision = now;
                RunDecisions();
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[CombatBrain] Tick: " + e.Message);
            }
        }

        private static void RunDecisions()
        {
            // 遍历视野内的异能单位（chunk 分区，性能安全）
            Camera cam = Camera.main;
            if (cam == null) return;
            WorldTile centerTile = World.world.GetTileSimple(
                (int)cam.transform.position.x, (int)cam.transform.position.y);
            if (centerTile == null) return;
            var units = Finder.getUnitsFromChunk(centerTile, 24, 60f, false);
            if (units == null) return;

            int budget = 0;
            foreach (Actor unit in units)
            {
                if (++budget > 80) break;                    // 每拍最多看 80 个单位
                if (unit == null || !unit.isAlive()) continue;
                if (CultivationData.GetRealmTier(unit) < MinFightTier) continue;
                if (!PrototypeRegistry.HasPrototype(unit)) continue;
                // v0.21.19 异能辨识光晕：视野内异能单位一眼区分（火=火环、冰=冰环…），
                // 兼作"这是异能战斗"的视觉标识；已挂单位幂等跳过
                Code.Combat.Fx.CombatAuraFx.EnsureAura(unit, PrototypeRegistry.GetForActor(unit));
                DecideFor(unit);
            }
            Code.Combat.Fx.CombatAuraFx.Prune();   // 清理已死单位记录（粒子由跟随机制自动回收）
            CombatStanceAI.Prune();                // 清理已死单位的走位冷却记录
        }

        private static void DecideFor(Actor self)
        {
            Actor enemy = FindBestEnemy(self, out int enemyCount);
            if (enemy == null) return;

            // v0.21.23 真走位：先按异能风格调整站位（投射拉开/近战贴脸/控制游走），再出招
            //  —— 走原版 Actor.moveTo 寻路、每单位 2s 冷却、目标格校验、全 try-catch（防崩溃）
            CombatStanceAI.AdjustStance(self, enemy);
            PrototypeDef def = PrototypeRegistry.GetForActor(self);
            CombatMove move = def != null
                ? CombatMoveRegistry.PickForAbility(def)
                : CombatMoveRegistry.DefaultMove(CultivationData.GetRealmTier(self));
            // v0.21.5：异能品质 F（def.Tier=1）会被招式 MinTier 全过滤 → 选不到就兜底默认招，
            // 保证凡级异能单位也能显化招式（不再"看不见非原版动作"）。
            if (move == null)
                move = CombatMoveRegistry.DefaultMove(CultivationData.GetRealmTier(self));
            if (move == null) return;

            // ================= v0.21.28 觉醒衔击 =================
            // 世界观：每次得手都是下次攻击的起点——
            //   目标处于失衡（腾空/倒地/震慑）→ 优先衔接追击招（衔击 ×1.5 伤害）
            //   目标未失衡且贴身 → 概率用挑空起手/擒拿开启衔击链
            int tierSelf = CultivationData.GetRealmTier(self);
            LinkState enemyLink = CombatLink.Get(enemy);
            if (enemyLink != LinkState.None)
            {
                CombatMove finisher = CombatMoveRegistry.LinkFinisherFor(enemyLink, tierSelf);
                if (finisher != null && CombatMoveDirector.CanUse(self, finisher))
                {
                    // 衔击追击（残血/群战保命仍可覆盖：处决优先但给保命让路）
                    bool fleeWants = CombatThreat.WantsFlee(self, enemy);
                    move = finisher;
                    if (!fleeWants) goto DistanceCheck;
                }
            }
            else if (enemyCount < 3 && UnityEngine.Random.value < 0.22f)
            {
                // 起手：贴身时挑空/擒拿开启衔击链（22% 触发，不喧宾夺主）
                float d = Vector2.Distance(self.current_position, enemy.current_position);
                if (d < 2.6f)
                {
                    CombatMove grapple = CombatMoveRegistry.GrappleFor(tierSelf);
                    if (grapple != null && CombatMoveDirector.CanUse(self, grapple)) move = grapple;
                    else
                    {
                        CombatMove launcher = CombatMoveRegistry.LauncherFor(tierSelf);
                        if (launcher != null && CombatMoveDirector.CanUse(self, launcher)) move = launcher;
                    }
                }
            }

            // v0.21.11 战斗AI补全（纯表现，不碰原版行为树）：
            //   —— 群战 AOE：被 ≥3 敌人包围时优先领域/爆破招（清场演出，观感"孤身战群"）
            //   —— 残血保命：HP<25% 且威胁比>2 时优先再生领域/金刚护体（演出上"自救回气"）
            //   —— 终极判定：残血拼大（原有）保持
            if (enemyCount >= 3)
            {
                CombatMove aoe = PickAoEFor(self);
                if (aoe != null && CombatMoveDirector.CanUse(self, aoe)) move = aoe;
            }
            else if (CombatThreat.WantsFlee(self, enemy))
            {
                CombatMove heal = PickHealFor(self);
                if (heal != null && CombatMoveDirector.CanUse(self, heal)) move = heal;
            }

            // 威胁比/残血 → 轻重提升（残血拼大）
            if (CombatThreat.WantsUltimate(self, enemy))
            {
                CombatMove ult = PickUltimateFor(self);
                if (ult != null && CombatMoveDirector.CanUse(self, ult)) move = ult;
            }

            if (!CombatMoveDirector.CanUse(self, move)) return;

        DistanceCheck:
            // 距离克制：近战招式只在贴脸时用；远程招式拉开才用
            float dist = Vector2.Distance(self.current_position, enemy.current_position);
            if (move.Kind == MoveKind.Slash || move.Kind == MoveKind.Strike || move.Kind == MoveKind.Lunge)
            {
                if (dist > 4.5f) return;
            }
            if (move.Kind == MoveKind.Beam && dist < 1.2f) return;

            // 出招（演出+地形破坏+冷却）
            CombatMoveDirector.Execute(move, self, enemy);
        }

        /// <summary>群战 AOE 招：领域/爆破/连锁（清场演出）。</summary>
        private static CombatMove PickAoEFor(Actor self)
        {
            PrototypeDef def = PrototypeRegistry.GetForActor(self);
            CombatMove tagged = def != null ? CombatMoveRegistry.PickForAbility(def) : null;
            if (tagged != null && (tagged.Kind == MoveKind.Domain || tagged.Kind == MoveKind.Blast || tagged.Kind == MoveKind.Chain))
                return tagged;
            // 无契合 AOE 标签时兜底：雷劫轰顶（雷系连锁）/精神风暴（领域）——有冷却时返回空
            int tier = CultivationData.GetRealmTier(self);
            CombatMove thunder = CombatMoveRegistry.Get("mv_thunder_judge");
            if (thunder != null && CombatMoveDirector.CanUse(self, thunder)) return thunder;
            CombatMove mind = CombatMoveRegistry.Get("mv_mind_storm");
            if (mind != null && CombatMoveDirector.CanUse(self, mind)) return mind;
            return null;
        }

        /// <summary>残血保命招：再生领域/金刚护体（演出自救）。</summary>
        private static CombatMove PickHealFor(Actor self)
        {
            CombatMove regen = CombatMoveRegistry.Get("mv_regen_field");
            if (regen != null && CombatMoveDirector.CanUse(self, regen)) return regen;
            CombatMove guard = CombatMoveRegistry.Get("mv_steel_guard");
            if (guard != null && CombatMoveDirector.CanUse(self, guard)) return guard;
            return null;
        }

        /// <summary>找最近的敌对单位 + 记敌人数（复用原版 chunk 查询 + 王国敌我判断；群战 AOE 判定用）。</summary>
        private static Actor FindBestEnemy(Actor self, out int enemyCount)
        {
            enemyCount = 0;
            WorldTile selfTile = World.world.GetTileSimple((int)self.current_position.x, (int)self.current_position.y);
            if (selfTile == null) return null;
            var nearby = Finder.getUnitsFromChunk(selfTile, 8, 10f, false);
            if (nearby == null) return null;
            Actor best = null;
            float bestDist = float.MaxValue;
            foreach (Actor a in nearby)
            {
                if (a == null || !a.isAlive() || a == self) continue;
                if (!IsEnemy(self, a)) continue;
                enemyCount++;
                float d = Vector2.Distance(self.current_position, a.current_position);
                if (d < bestDist) { bestDist = d; best = a; }
            }
            return best;
        }

        /// <summary>敌我判断（王国敌对优先，无王国=野生互敌）。v0.21.40 改 public：CombatRead 打断蓄势复用。</summary>
        public static bool IsEnemy(Actor a, Actor b)
        {
            try
            {
                if (a.kingdom != null && b.kingdom != null)
                    return a.kingdom.isEnemy(b.kingdom);
                return a.kingdom != b.kingdom;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>挑一个可用的终极招式（按单位境界 tier 判断，9/10/11 阶对应三档大招）。</summary>
        private static CombatMove PickUltimateFor(Actor self)
        {
            int tier = CultivationData.GetRealmTier(self);
            if (tier >= 11) return CombatMoveRegistry.Get("mv_fate_verdict");
            if (tier >= 10) return CombatMoveRegistry.Get("mv_holy_judgment");
            if (tier >= 9) return CombatMoveRegistry.Get("mv_space_tear");
            return null;
        }
    }
}
