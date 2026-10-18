// ============================================================
// 受击反应·身体演出层（v0.21.11）
// 动画语言补全（对标"战斗之神受击编排"的观感，机制原创）：
//   被异能招式/重击命中的单位做出"非原版"身体反应——
//     Fluster 微颤（轻击吃招，小位移抖身）
//     Stagger 踉跄（重击受创，沿攻击方向退步+顿挫）
//     Launch  击飞（终极技/境界压制，抛离+落地回弹）
// 世界观：境界差决定档位——高阶打低阶自动升档（登神一掌
//   拍飞凡俗），同阶对轰只到踉跄，低阶打高阶只到微颤。
// 实现原理：与 CombatMovePose 同源，复用 move_jump_offset
//   做纯表现位移（行为树/寻路/AI 无感），不碰原版 AI。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using Code.Combat.Fx;
using Code.Combat.Moves;

namespace Code.Combat.Moves
{
    /// <summary>受击反应档位（世界观原创命名，非战斗之神 Hurt 体系）。</summary>
    public enum ImpactLevel
    {
        None = 0,
        Fluster,     // 微颤：轻击吃招，原地小位移抖身
        Stagger,     // 踉跄：重击受创，沿攻击方向退步+顿挫
        Launch       // 击飞：终极技/境界压制，抛离+落地回弹
    }

    internal sealed class ImpactState
    {
        public float OriginX;
        public float OriginY;
        public float PeakX;      // 沿攻击方向的水平位移（击飞=向后抛离）
        public float PeakY;      // 垂直峰值（击飞=抛高）
        public float Timer;
        public float Dur;
        public ImpactLevel Level;
        /// <summary>击飞落地回弹（顿挫）。</summary>
        public bool Bounce;
    }

    public static class CombatImpactPose
    {
        private static readonly Dictionary<Actor, ImpactState> _impacts = new Dictionary<Actor, ImpactState>();

        // ------------------------------------------------------ 对外接口 --

        /// <summary>
        /// 受击反应入口（AbilityStatusPatch.getHit 调用）。
        /// 档位 = 伤害占比 × 境界差 × 是否终极演出：
        ///   —— 伤害占比 &lt; 8% 或 攻击方境界≤受击方 → 微颤/无
        ///   —— 伤害占比 ≥ 8% 且攻击方境界高 → 踉跄
        ///   —— 伤害占比 ≥ 25% 或 攻击方境界高 ≥3 阶 → 击飞（境界压制）
        /// </summary>
        public static void OnHit(Actor victim, Actor attacker, float damage, float maxHp)
        {
            if (victim == null || !victim.isAlive()) return;
            int atk = attacker != null ? Code.Data.CultivationData.GetRealmTier(attacker) : 0;
            int def = Code.Data.CultivationData.GetRealmTier(victim);
            float hpPct = damage / Mathf.Max(1f, maxHp);
            int diff = atk - def;

            ImpactLevel level;
            if (hpPct >= 0.25f || diff >= 3) level = ImpactLevel.Launch;          // 终极技/境界压制：击飞
            else if (hpPct >= 0.08f || diff >= 1) level = ImpactLevel.Stagger;    // 重击/高一阶：踉跄
            else if (hpPct >= 0.02f) level = ImpactLevel.Fluster;                 // 轻击：微颤
            else return;                                                          // 刮痧不动

            Vector2 dir = attacker != null && victim != null
                ? (victim.current_position - attacker.current_position)
                : new Vector2(1f, 0f);
            if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(1f, 0f);
            dir.Normalize();

            // 击飞方向：沿攻击方向反向抛离（被击退）
            Apply(victim, -dir, level);
        }

        /// <summary>显式触发（招式导演在终极演出时给目标格单位补受击反应）。</summary>
        public static void Force(Actor victim, Vector2 awayDir, ImpactLevel level)
        {
            if (victim == null || !victim.isAlive()) return;
            Vector2 d = awayDir;
            if (d.sqrMagnitude < 0.0001f) d = new Vector2(1f, 0f);
            Apply(victim, d.normalized, level);
        }

        // ------------------------------------------------------ 内部 --

        private static void Apply(Actor a, Vector2 away, ImpactLevel level)
        {
            // 受击优先：单位正在出攻击姿态时先释放（被打飞时不再维持攻击动作）
            if (CombatMovePose.IsActive(a)) CombatMovePose.Release(a);

            if (_impacts.ContainsKey(a)) Restore(a);
            float scale = AbilityFx.UnitScale(a);
            ImpactState s = new ImpactState();
            s.OriginX = a.move_jump_offset.x;
            s.OriginY = a.move_jump_offset.y;
            s.Level = level;
            switch (level)
            {
                case ImpactLevel.Fluster:
                    s.PeakX = away.x * 0.06f * scale;
                    s.PeakY = 0.03f * scale;
                    s.Dur = 0.12f; s.Bounce = false;
                    break;
                case ImpactLevel.Stagger:
                    s.PeakX = away.x * 0.22f * scale;
                    s.PeakY = 0.05f * scale;
                    s.Dur = 0.2f; s.Bounce = true;
                    break;
                default: // Launch
                    s.PeakX = away.x * 0.55f * scale;
                    s.PeakY = 0.42f * scale;
                    s.Dur = 0.38f; s.Bounce = true;
                    break;
            }
            s.Timer = s.Dur;
            _impacts[a] = s;
            a.move_jump_offset = new Vector2(s.OriginX + s.PeakX, s.OriginY + s.PeakY);
        }

        private static void Restore(Actor a)
        {
            if (_impacts.TryGetValue(a, out var old))
                a.move_jump_offset = new Vector2(old.OriginX, old.OriginY);
            _impacts.Remove(a);
        }

        /// <summary>每帧驱动：受击姿态衰减 + 击飞落地回弹 + 清除死亡单位（FramePatch 调用）。</summary>
        public static void Tick(float dt)
        {
            if (_impacts.Count == 0) return;
            var dead = new List<Actor>();
            foreach (var kv in _impacts)
            {
                Actor a = kv.Key;
                ImpactState s = kv.Value;
                if (a == null || !a.isAlive())
                {
                    dead.Add(kv.Key);
                    continue;
                }
                s.Timer -= dt;
                float t = s.Dur > 0f ? Mathf.Clamp01(s.Timer / s.Dur) : 0f;
                // 先快后慢：击飞瞬间冲力最猛，末尾缓停
                float k = 1f - Mathf.Pow(1f - t, 2.2f);
                float x = s.OriginX + s.PeakX * k;
                float y = s.OriginY + s.PeakY * k;
                if (s.Bounce && t < 0.15f)
                {
                    // 落地回弹顿挫：触底瞬间反向小弹（踉跄/击飞的"吃招"感）
                    float bounce = (0.15f - t) / 0.15f;
                    y -= 0.05f * bounce;
                    x += s.PeakX * 0.2f * bounce;
                }
                a.move_jump_offset = new Vector2(x, y);
                if (s.Timer <= 0f)
                {
                    a.move_jump_offset = new Vector2(s.OriginX, s.OriginY);
                    dead.Add(kv.Key);
                }
            }
            foreach (var k in dead) _impacts.Remove(k);
        }

        /// <summary>世界销毁/存档切换：清空全部受击姿态。</summary>
        public static void ClearAll()
        {
            foreach (var kv in _impacts)
                if (kv.Key != null)
                    kv.Key.move_jump_offset = new Vector2(kv.Value.OriginX, kv.Value.OriginY);
            _impacts.Clear();
        }
    }
}
