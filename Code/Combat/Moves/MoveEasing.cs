// ============================================================
// 招式编排·缓动曲线库（v0.21.0）
// 动画语言的"语法"：所有招式动作的时间曲线统一从这里取。
// 纯静态函数、无副作用、无 Unity 组件依赖（单一职责：数学）。
// 设计参考「战斗之神」Anim 的机制思路（招式需要缓动节奏），
// 实现为独立原创（函数名/公式/用法均为本项目自定）。
// ============================================================
using UnityEngine;

namespace Code.Combat.Moves
{
    /// <summary>
    /// 招式时间曲线库：把招式编排的"进度 k(0..1)"映射成表现位移/缩放/旋转的因子。
    /// 命名约定：Out*=减速收尾（蓄势出手，先快后慢）、In*=加速入场（逼近目标）、InOut*=两端缓中间快。
    /// </summary>
    public static class MoveEasing
    {
        /// <summary>二次减速：蓄势/出手常用（猛起后收住）。</summary>
        public static float OutQuad(float k)
        {
            return 1f - (1f - k) * (1f - k);
        }

        /// <summary>三次减速：招式收尾（重击落地/剑弧收束）。</summary>
        public static float OutCubic(float k)
        {
            float i = 1f - k;
            return 1f - i * i * i;
        }

        /// <summary>二次加速：冲刺入场（突进逼近）。</summary>
        public static float InQuad(float k)
        {
            return k * k;
        }

        /// <summary>三次缓入缓出：领域扩散/法阵展开。</summary>
        public static float InOutCubic(float k)
        {
            return k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) / 2f;
        }

        /// <summary>正弦缓入缓出：光环/护盾脉冲。</summary>
        public static float InOutSine(float k)
        {
            return 0.5f - 0.5f * Mathf.Cos(Mathf.PI * k);
        }

        /// <summary>区间平滑过渡（smoothstep）：出场淡入/收尾淡出。</summary>
        public static float Smooth(float a, float b, float k)
        {
            float t = Mathf.Clamp01((k - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>阻尼弹簧（过冲一次后回落）：击中抖动/余波震颤。</summary>
        public static float Spring(float t, float damping, float freq)
        {
            return Mathf.Exp(-damping * t) * Mathf.Cos(freq * t);
        }

        /// <summary>回弹过冲（overshoot）：突进到达后小幅回弹。</summary>
        public static float OutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = k - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        /// <summary>把任意因子夹到 0..1。</summary>
        public static float Clamp01(float k)
        {
            return Mathf.Clamp01(k);
        }
    }
}
