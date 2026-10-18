// ============================================================
// 招式动作·姿态编排层（v0.21.5）
// 单一职责：让单位本体做出"非原版"的战斗动作（突进/腾空/下砸/后坐/挥击抖身）。
//
// 实现原理（原版 0.51.x 反编译核实）：
//   —— 单位渲染位置 = current_position + move_jump_offset（Actor.calculateMainSprite
//      用 move_jump_offset.y>0 判跳跃动画），该偏移由原版跳跃系统驱动；
//   —— 我们复用同一字段做"视觉动作偏移"：行为树/寻路/AI 完全无感（不读它决策），
//      纯表现层——符合全模组"纯表现不碰原版 AI"原则；
//   —— 招式结束时平滑回归 0，单位回到行为树管辖的原位。
//
// 与 CombatMoveDirector 的关系：Director 负责"编排特效+地形+音效"，
// 本层只负责"单位本体的动作姿态"——两者组合 = 完整招式演出。
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace Code.Combat.Moves
{
    /// <summary>单个单位的动作姿态状态。</summary>
    internal sealed class PoseState
    {
        /// <summary>水平偏移起点。</summary>
        public float OriginX;
        /// <summary>垂直偏移起点。</summary>
        public float OriginY;
        /// <summary>水平峰值偏移。</summary>
        public float PeakX;
        /// <summary>垂直峰值偏移。</summary>
        public float PeakY;
        /// <summary>剩余时长。</summary>
        public float Timer;
        /// <summary>总时长。</summary>
        public float Dur;
        /// <summary>动画曲线（0=线性 1=先快后慢 2=先慢后快）。</summary>
        public int Curve;
        /// <summary>是否有下砸回弹（下砸动作的落地顿挫）。</summary>
        public bool SlamBounce;
    }

    public static class CombatMovePose
    {
        private static readonly Dictionary<Actor, PoseState> _poses = new Dictionary<Actor, PoseState>();

        // ------------------------------------------------------ 对外动作 --

        /// <summary>突进（水平前冲 + 轻微腾身）：斩/突进系。</summary>
        public static void Dash(Actor a, Vector2 dir, float dist, float dur = 0.2f)
        {
            if (a == null || dir.sqrMagnitude < 0.0001f) return;
            Vector2 n = dir.normalized;
            Apply(a, new PoseState
            {
                OriginX = a.move_jump_offset.x,
                OriginY = a.move_jump_offset.y,
                PeakX = n.x * dist,
                PeakY = 0.12f,          // 起跳瞬间的腾身（视觉）
                Timer = dur,
                Dur = dur,
                Curve = 1               // 先快后慢：冲刺感
            });
        }

        /// <summary>挥击抖身（水平小位移 + 身体后带）：斩/刃系。</summary>
        public static void Swing(Actor a, Vector2 dir, float dist = 0.22f, float dur = 0.18f)
        {
            if (a == null || dir.sqrMagnitude < 0.0001f) return;
            Vector2 n = dir.normalized;
            Apply(a, new PoseState
            {
                OriginX = a.move_jump_offset.x,
                OriginY = a.move_jump_offset.y,
                PeakX = n.x * dist * 0.6f,
                PeakY = 0.08f,
                Timer = dur,
                Dur = dur,
                Curve = 2               // 先慢后快：蓄力挥出
            });
        }

        /// <summary>重击下砸（前压 + 落地回弹）：重击/震地系。</summary>
        public static void Slam(Actor a, Vector2 dir, float dist, float dur = 0.24f)
        {
            if (a == null || dir.sqrMagnitude < 0.0001f) return;
            Vector2 n = dir.normalized;
            Apply(a, new PoseState
            {
                OriginX = a.move_jump_offset.x,
                OriginY = a.move_jump_offset.y,
                PeakX = n.x * dist,
                PeakY = -0.1f,          // 下压（砸向地面）
                Timer = dur,
                Dur = dur,
                Curve = 1,
                SlamBounce = true       // 触底回弹顿挫
            });
        }

        /// <summary>腾空（领域/大招蓄势）：垂直抬升悬浮。</summary>
        public static void Lift(Actor a, float height = 0.3f, float dur = 0.5f)
        {
            if (a == null) return;
            Apply(a, new PoseState
            {
                OriginX = a.move_jump_offset.x,
                OriginY = a.move_jump_offset.y,
                PeakX = 0f,
                PeakY = height,
                Timer = dur,
                Dur = dur,
                Curve = 2               // 先慢后快：缓缓升空
            });
        }

        /// <summary>后坐（光束/爆裂反冲）：沿反方向小位移。</summary>
        public static void Recoil(Actor a, Vector2 dir, float dist = 0.18f, float dur = 0.16f)
        {
            if (a == null || dir.sqrMagnitude < 0.0001f) return;
            Vector2 n = dir.normalized;
            Apply(a, new PoseState
            {
                OriginX = a.move_jump_offset.x,
                OriginY = a.move_jump_offset.y,
                PeakX = -n.x * dist,
                PeakY = 0.05f,
                Timer = dur,
                Dur = dur,
                Curve = 1
            });
        }

        /// <summary>轻击点刺（极小位移，用于低阶试探招）。</summary>
        public static void Nudge(Actor a, Vector2 dir, float dur = 0.12f)
        {
            if (a == null || dir.sqrMagnitude < 0.0001f) return;
            Vector2 n = dir.normalized;
            Apply(a, new PoseState
            {
                OriginX = a.move_jump_offset.x,
                OriginY = a.move_jump_offset.y,
                PeakX = n.x * 0.1f,
                PeakY = 0.04f,
                Timer = dur,
                Dur = dur,
                Curve = 1
            });
        }

        // ------------------------------------------------------ 内部 --

        private static void Apply(Actor a, PoseState s)
        {
            if (_poses.ContainsKey(a)) Restore(a);       // 先还原旧姿态
            _poses[a] = s;
            a.move_jump_offset = new Vector2(s.OriginX + s.PeakX, s.OriginY + s.PeakY);
        }

        private static void Restore(Actor a)
        {
            if (_poses.TryGetValue(a, out var old))
                a.move_jump_offset = new Vector2(old.OriginX, old.OriginY);
            _poses.Remove(a);
        }

        /// <summary>每帧驱动：姿态衰减 + 清除死亡单位（由 CombatFxFramePatch 调用）。</summary>
        public static void Tick(float dt)
        {
            if (_poses.Count == 0) return;
            var dead = new List<Actor>();
            foreach (var kv in _poses)
            {
                Actor a = kv.Key;
                PoseState s = kv.Value;
                if (a == null || !a.isAlive())
                {
                    dead.Add(kv.Key);
                    continue;
                }
                s.Timer -= dt;
                float t = s.Dur > 0f ? Mathf.Clamp01(s.Timer / s.Dur) : 0f;
                float k = CurveEase(t, s.Curve);
                float x = s.OriginX + s.PeakX * k;
                float y = s.OriginY + s.PeakY * k;
                if (s.SlamBounce && t < 0.12f)
                {
                    // 落地回弹顿挫：触底瞬间反向小弹
                    float bounce = (0.12f - t) / 0.12f;
                    y += 0.06f * bounce;
                    x -= s.PeakX * 0.18f * bounce;
                }
                a.move_jump_offset = new Vector2(x, y);
                if (s.Timer <= 0f)
                {
                    a.move_jump_offset = new Vector2(s.OriginX, s.OriginY);
                    dead.Add(kv.Key);
                }
            }
            foreach (var k in dead) _poses.Remove(k);
        }

        /// <summary>姿态动画曲线。</summary>
        private static float CurveEase(float t, int curve)
        {
            switch (curve)
            {
                case 1: return 1f - Mathf.Pow(1f - t, 2.2f);   // 先快后慢
                case 2: return t * t * (3f - 2f * t);          // smoothstep（先慢后快-中段最快）
                default: return t;                              // 线性
            }
        }

        /// <summary>世界销毁/存档切换：清空全部姿态。</summary>
        public static void ClearAll()
        {
            foreach (var kv in _poses)
                if (kv.Key != null)
                    kv.Key.move_jump_offset = new Vector2(kv.Value.OriginX, kv.Value.OriginY);
            _poses.Clear();
        }
    }
}
