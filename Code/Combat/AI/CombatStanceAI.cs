// ============================================================
// 异能单位真走位（v0.21.23 → v0.21.35 环绕侧移）
// 解决"战斗全挤在一起"的硬缺口：让异能单位按战斗风格主动调整站位——
//   投射系 → 与敌人保持 4 格距离（拉开打）
//   近战系 → 贴脸 0.6 格（压制）
//   控制系 → 中距 3 格游走
// v0.21.35：加"环绕侧移"——v0.21.23 只沿 自己→敌人 连线进退，
//   多人混战时所有单位挤在同一条直线上（看起来还是挤成一排）。
//   现在目标点 = 连线理想点 + 垂直侧移（每单位按实例哈希稳定分左右），
//   单位天然错开成"面"而非"线"，近战包围、远程拉开时观感散开。
//
// 安全边界（防止再次出现 0.51.4 SmoothLoader 崩溃类问题）：
//   —— 移动走原版 Actor.moveTo（寻路处理碰撞），不直接改 current_position；
//   —— 反射 MethodInfo 静态缓存（不每拍 GetMethod）；
//   —— 每单位每 1.6 秒最多一条走位指令（不频繁接管原版移动）；
//   —— 总偏差（进退+侧移）<0.9 格不动（不折腾已合适的身位）；
//   —— 目标格必须是有效地表格（GetTileSimple 非 null）；
//   —— 全部 try-catch，moveTo 失败静默跳过（不影响战斗逻辑）。
// 纯表现层：不改行为树/伤害/寻路策略，只给单位"换个目标点"。
// ============================================================
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Code.Realm.Prototype;

namespace Code.Combat.AI
{
    public static class CombatStanceAI
    {
        private static MethodInfo _moveTo;
        private static readonly Dictionary<Actor, float> _nextMove = new Dictionary<Actor, float>();
        private const float MinStanceInterval = 1.6f;   // 每单位走位指令最小间隔（秒）
        private const float StanceDeadband = 0.9f;      // 总偏差死区（<0.9 格不折腾）
        private const float MaxStanceRange = 8f;       // 超过 8 格不追（避免全场拉扯）

        /// <summary>战斗站位调整：距离偏差过大时让单位走向理想位置。每拍由 CombatBrain 调用。</summary>
        public static void AdjustStance(Actor self, Actor enemy)
        {
            try
            {
                if (self == null || enemy == null) return;
                if (!self.isAlive() || !enemy.isAlive()) return;
                if (World.world == null || World.world.isPaused()) return;

                float now = Time.unscaledTime;
                if (_nextMove.TryGetValue(self, out float next) && now < next) return;

                PrototypeDef def = PrototypeRegistry.GetForActor(self);
                if (def == null) return;
                CombatStance stance = CombatStyle.StanceOf(def);
                float ideal = CombatStyle.IdealDistance(stance);

                Vector2 me = self.current_position;
                Vector2 them = enemy.current_position;
                float dist = Vector2.Distance(me, them);
                if (dist > MaxStanceRange) return;          // 太远不追（保持当前战斗）
                float err = dist - ideal;

                // v0.21.35 环绕侧移：目标点 = 连线理想点 + 垂直侧移
                //  —— toEnemy：自己→敌人的连线（进退方向）
                //  —— perp：连线垂直方向（环绕方向）
                //  —— side：按实例哈希稳定分左右（同单位不换边，多人左右交替错开）
                //  —— flank：侧移幅度按风格（近战贴脸少绕、远程/控制多绕）
                // 效果：多人混战不再挤成一条直线，而是错开成包围/散开的"面"。
                Vector2 toEnemy = them - me;
                if (toEnemy.sqrMagnitude < 0.0001f) toEnemy = new Vector2(1f, 0f);
                toEnemy.Normalize();
                Vector2 perp = new Vector2(-toEnemy.y, toEnemy.x);
                int side = (self.GetHashCode() & 1) == 0 ? 1 : -1;
                float flank = FlankOffset(stance);
                Vector2 target = me + toEnemy * err + perp * (side * flank);

                // 总偏差（进退 err + 侧移）落在死区内 = 身位已合适，不动
                if (Vector2.Distance(me, target) < StanceDeadband) return;

                // 目标格必须是有效地表（不可走/水格由原版寻路自行处理，走不到就不走，不干预）
                WorldTile tile = World.world.GetTileSimple((int)target.x, (int)target.y);
                if (tile == null) return;

                if (MoveTo(self, target))
                    _nextMove[self] = now + MinStanceInterval;
            }
            catch (Exception dsEx)
            {
                Code.Core.DSDebug.Verbose("[CombatStanceAI] " + dsEx.Message);
            }
        }

        /// <summary>侧移幅度（世界单位）：近战贴脸少绕保持压制，远程/控制多绕散开阵型。</summary>
        private static float FlankOffset(CombatStance stance)
        {
            switch (stance)
            {
                case CombatStance.Ranged: return 1.6f;
                case CombatStance.Control: return 1.3f;
                default: return 0.7f;
            }
        }

        /// <summary>反射调用原版 Actor.moveTo（寻路移动；MethodInfo 静态缓存）。</summary>
        private static bool MoveTo(Actor a, Vector2 pos)
        {
            try
            {
                if (_moveTo == null)
                {
                    _moveTo = a.GetType().GetMethod("moveTo",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (_moveTo == null) return false;
                }
                _moveTo.Invoke(a, new object[] { pos });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>清掉已死亡单位的冷却记录（防字典膨胀）。</summary>
        public static void Prune()
        {
            if (_nextMove.Count <= 0) return;
            var dead = new List<Actor>();
            foreach (var k in _nextMove.Keys)
                if (k == null || !k.isAlive()) dead.Add(k);
            for (int i = 0; i < dead.Count; i++) _nextMove.Remove(dead[i]);
        }
    }
}
