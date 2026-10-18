// ============================================================
// 战斗本能连击·失衡状态机（v0.21.28）
// 世界观：基因觉醒者的战斗本能——每一次得手都是下一次攻击的起点。
//   腾空(Launched)：被挑空抛离，滞空期间是追击窗口（对应既有击飞演出）
//   失衡(Down)：重击倒地，起身前的追击窗口
//   震慑(Stunned)：僵直眩晕，短暂的任意追击窗口
//   擒拿(Grabbed)：被异能抓取持有（藤蔓擒拿/念力擒拿），持有者摔投
// 境界抗性：攻击者境界 < 目标境界 → 降档/无效；同阶正常；
//   攻击者高出 3+ 阶 → 窗口延长。登神目标窗口减半（难被凡人控住）。
// 纯表现+标记层：不碰原版行为树/寻路/AI，状态只驱动我方招式衔接
//   与伤害放大（衔击 ×1.5），崩溃面为零。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using Code.Data;

namespace Code.Combat.Moves
{
    /// <summary>失衡状态（世界观原创命名，非战斗之神 Hurt/Launch 体系）。</summary>
    public enum LinkState
    {
        None = 0,      // 无
        Launched,      // 腾空：被挑空抛离（滞空追击窗口）
        Down,          // 失衡：重击倒地（起身前追击窗口）
        Stunned,       // 震慑：僵直眩晕（任意追击窗口）
        Grabbed        // 擒拿：被抓取持有（擒拿衔接窗口）
    }

    internal sealed class LinkEntry
    {
        public LinkState State;
        public float Left;      // 剩余窗口（秒）
    }

    /// <summary>失衡状态机：应用/查询/清理，带境界抗性与登神减窗。</summary>
    public static class CombatLink
    {
        private static readonly Dictionary<Actor, LinkEntry> _links = new Dictionary<Actor, LinkEntry>();

        /// <summary>各状态基础窗口（秒）。</summary>
        private const float WinLaunched = 0.9f;
        private const float WinDown = 1.2f;
        private const float WinStunned = 0.7f;
        private const float WinGrabbed = 1.5f;

        /// <summary>应用失衡状态。攻击者境界低于目标 → 降档；高于 3+ 阶 → 延长。
        /// 登神目标窗口减半（神躯难被控）。返回实际生效状态（None=完全抵抗）。</summary>
        public static LinkState Apply(Actor target, LinkState state, int attackerTier)
        {
            if (target == null || !target.isAlive()) return LinkState.None;
            if (state == LinkState.None) return LinkState.None;
            try
            {
                int defTier = CultivationData.GetRealmTier(target);
                int diff = attackerTier - defTier;
                // 境界抗性：攻击低于目标 2+ 阶 → 完全抵抗（越阶无法控住）
                if (diff < -1) return LinkState.None;
                // 同阶/低 1 阶 → 降一档表现（腾空→失衡→震慑→None 依次衰减）
                if (diff <= 0)
                {
                    if (state == LinkState.Launched) state = LinkState.Stunned;
                    else if (state == LinkState.Down) state = LinkState.Stunned;
                    else if (state == LinkState.Stunned) return LinkState.None;
                    // Grabbed 同阶仍可擒拿（控制技对同阶生效，但窗口短）
                }
                float win;
                switch (state)
                {
                    case LinkState.Launched: win = WinLaunched; break;
                    case LinkState.Down: win = WinDown; break;
                    case LinkState.Stunned: win = WinStunned; break;
                    default: win = WinGrabbed; break;
                }
                if (diff >= 3) win *= 1.6f;              // 境界碾压 → 窗口延长
                if (CultivationData.IsAscended(target)) win *= 0.5f;   // 登神神躯减半
                if (state == LinkState.Grabbed && diff < 1 && CultivationData.IsAscended(target))
                    return LinkState.None;   // 登神目标不被同阶擒拿
                _links[target] = new LinkEntry { State = state, Left = win };
                return state;
            }
            catch (System.Exception dsEx)
            {
                Code.Core.DSDebug.Verbose("[CombatLink] apply: " + dsEx.Message);
                return LinkState.None;
            }
        }

        /// <summary>查询目标当前失衡状态。</summary>
        public static LinkState Get(Actor target)
        {
            if (target == null) return LinkState.None;
            LinkEntry e;
            if (!_links.TryGetValue(target, out e)) return LinkState.None;
            return e.State;
        }

        /// <summary>目标是否处于指定状态（追击判定）。</summary>
        public static bool Is(Actor target, LinkState state)
        {
            return state != LinkState.None && Get(target) == state;
        }

        /// <summary>每帧衰减窗口；过期清除（防字典泄漏）。</summary>
        public static void Tick(float dt)
        {
            if (_links.Count == 0) return;
            List<Actor> dead = null;
            foreach (var kv in _links)
            {
                kv.Value.Left -= dt;
                if (kv.Value.Left <= 0f || kv.Key == null || !kv.Key.isAlive())
                {
                    if (dead == null) dead = new List<Actor>();
                    dead.Add(kv.Key);
                }
            }
            if (dead != null)
                for (int i = 0; i < dead.Count; i++) _links.Remove(dead[i]);
        }

        /// <summary>擒拿演出结束时由 Director 显式清除。</summary>
        public static void Clear(Actor target)
        {
            if (target != null) _links.Remove(target);
        }
    }
}
