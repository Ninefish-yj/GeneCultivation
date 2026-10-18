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
        /// <summary>v0.21.18 空中连击段数：滞空期再次受击→空中二段（浮空对拼，不落回地面）。</summary>
        public int AirChain;
        /// <summary>v0.21.18 滞空（平顶悬停）时长占比：抛到顶后短暂停留，形成"在天上打"的浮空观感。</summary>
        public float HangFrac;
    }

    public static class CombatImpactPose
    {
        private static readonly Dictionary<Actor, ImpactState> _impacts = new Dictionary<Actor, ImpactState>();
        /// <summary>受击防抖：同一受击者最近一次触发时刻（真实时间），防倍速/群战高频打飞乱姿态。</summary>
        private static readonly Dictionary<Actor, float> _lastHitAt = new Dictionary<Actor, float>();
        private const float HitDebounce = 0.12f;

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

            // 防抖：倍速下 getHit 每帧多次，受击姿态只按真实时间 0.12s 一次触发
            float now = Time.unscaledTime;
            if (_lastHitAt.TryGetValue(victim, out float lastHit) && now - lastHit < HitDebounce) return;
            _lastHitAt[victim] = now;

            Vector2 dir = attacker != null && victim != null
                ? (victim.current_position - attacker.current_position)
                : new Vector2(1f, 0f);
            if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(1f, 0f);
            dir.Normalize();

            // v0.21.18 空中连击：目标正处于击飞滞空期（未落地）再次受重击 → 空中二段
            //   （浮空对拼——"在天上打"的核心：不落回地面，被继续抛得更高）
            if (level >= ImpactLevel.Stagger && IsAirborne(victim) && _impacts.TryGetValue(victim, out var cur) && cur.Level == ImpactLevel.Launch)
            {
                if (cur.AirChain < 2)  // 最多 2 连，杜绝永久浮空
                {
                    ApplyAirChain(victim, dir, cur.AirChain + 1);
                    return;
                }
            }
            // 击飞方向：沿攻击方向反向抛离（被击退）
            Apply(victim, -dir, level);
        }

        /// <summary>v0.21.18 空中二段连击：沿攻击方向横向漂移 + 抛得更高，滞空更久（浮空对拼）。</summary>
        private static void ApplyAirChain(Actor a, Vector2 hitDir, int chain)
        {
            if (CombatMovePose.IsActive(a)) CombatMovePose.Release(a);
            if (_impacts.ContainsKey(a)) Restore(a);
            float scale = AbilityFx.UnitScale(a);
            ImpactState s = new ImpactState();
            s.OriginX = a.move_jump_offset.x;
            s.OriginY = a.move_jump_offset.y;
            s.Level = ImpactLevel.Launch;
            // 二段更高更远：峰值 y 0.42→0.58，横向沿攻击方向漂移（被连击推向攻击方向）
            s.PeakX = hitDir.x * (0.35f + 0.12f * chain) * scale;
            s.PeakY = 0.58f * scale;
            s.Dur = 0.5f + 0.08f * chain;
            s.HangFrac = 0.32f;
            s.Bounce = true;
            s.AirChain = chain;
            s.Timer = s.Dur;
            _impacts[a] = s;
            a.move_jump_offset = new Vector2(s.OriginX + s.PeakX, s.OriginY + s.PeakY);
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
                    s.Dur = 0.46f; s.Bounce = true;
                    s.HangFrac = 0.22f;   // v0.21.18 抛到顶后滞空停留（浮空观感）
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

        /// <summary>v0.21.18 单位当前表现浮空高度（move_jump_offset.y 的活跃值）。
        /// 命中特效定位用——人被抛上天，特效跟随打在天上。</summary>
        public static float AirOffsetY(Actor a)
        {
            if (a == null) return 0f;
            if (_impacts.TryGetValue(a, out var s))
            {
                // 用当前即时偏移（Tick 已写 move_jump_offset，直接读表现值）
                return a.move_jump_offset.y - s.OriginY;
            }
            return 0f;
        }

        /// <summary>v0.21.18 是否处于击飞滞空/空中连击状态（未落地）。</summary>
        public static bool IsAirborne(Actor a)
        {
            if (a == null) return false;
            return _impacts.TryGetValue(a, out var s) && s.Level == ImpactLevel.Launch && s.Timer > 0f && s.Timer / s.Dur < 1f - s.HangFrac * 0.5f;
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
                // v0.21.18 三段曲线：上升(0→1-2h) → 滞空平顶(1-2h→1-h) → 下落(1-h→1)+落地回弹
                //   滞空段形成"在天上打"的浮空停留，下落段保留先快后慢的吃招感
                float k;
                float h = s.Level == ImpactLevel.Launch ? s.HangFrac : 0f;
                if (t < 1f - 2f * h)
                {
                    // 上升：快速冲顶（ease-out）
                    float u = t / Mathf.Max(0.001f, 1f - 2f * h);
                    k = 1f - Mathf.Pow(1f - u, 2.0f);
                }
                else if (t < 1f - h)
                {
                    // 滞空：平顶悬停（微浮动，浮空对拼窗口）
                    float u = (t - (1f - 2f * h)) / Mathf.Max(0.001f, h);
                    k = 1f - 0.06f * Mathf.Sin(u * Mathf.PI);   // 顶点轻微呼吸浮动
                }
                else
                {
                    // 下落：加速坠地
                    float u = (t - (1f - h)) / Mathf.Max(0.001f, h);
                    k = 1f - u * u;
                }
                float x = s.OriginX + s.PeakX * k;
                float y = s.OriginY + s.PeakY * k;
                if (s.Bounce && t >= 1f - 0.12f && h <= 0f)
                {
                    // 落地回弹顿挫：触底瞬间反向小弹（踉跄/击飞的"吃招"感；带滞空的击飞下落段已含回弹曲线，不重复叠加）
                    float bounce = (t - (1f - 0.12f)) / 0.12f;
                    y -= 0.05f * (1f - bounce);
                    x += s.PeakX * 0.2f * (1f - bounce);
                }
                a.move_jump_offset = new Vector2(x, y);
                if (s.Timer <= 0f)
                {
                    a.move_jump_offset = new Vector2(s.OriginX, s.OriginY);
                    dead.Add(kv.Key);
                }
            }
            foreach (var k in dead)
            {
                _impacts.Remove(k);
                _lastHitAt.Remove(k);
            }
        }

        /// <summary>世界销毁/存档切换：清空全部受击姿态。</summary>
        public static void ClearAll()
        {
            foreach (var kv in _impacts)
                if (kv.Key != null)
                    kv.Key.move_jump_offset = new Vector2(kv.Value.OriginX, kv.Value.OriginY);
            _impacts.Clear();
            _lastHitAt.Clear();
        }
    }
}
