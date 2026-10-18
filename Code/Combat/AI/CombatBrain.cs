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

        /// <summary>参与战斗决策的最低境界（灵/使徒 6 阶以上才用得上大招观感）。</summary>
        private const int MinFightTier = 6;

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
                DecideFor(unit);
            }
        }

        private static void DecideFor(Actor self)
        {
            Actor enemy = FindBestEnemy(self);
            if (enemy == null) return;

            PrototypeDef def = PrototypeRegistry.GetForActor(self);
            CombatMove move = def != null
                ? CombatMoveRegistry.PickForAbility(def)
                : CombatMoveRegistry.DefaultMove(CultivationData.GetRealmTier(self));
            if (move == null) return;

            // 威胁比/残血 → 轻重提升（残血拼大）
            if (CombatThreat.WantsUltimate(self, enemy))
            {
                CombatMove ult = PickUltimateFor(self);
                if (ult != null && CombatMoveDirector.CanUse(self, ult)) move = ult;
            }

            if (!CombatMoveDirector.CanUse(self, move)) return;

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

        /// <summary>找最近的敌对单位（复用原版 chunk 查询 + 王国敌我判断）。</summary>
        private static Actor FindBestEnemy(Actor self)
        {
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
                float d = Vector2.Distance(self.current_position, a.current_position);
                if (d < bestDist) { bestDist = d; best = a; }
            }
            return best;
        }

        /// <summary>敌我判断（王国敌对优先，无王国=野生互敌）。</summary>
        private static bool IsEnemy(Actor a, Actor b)
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
