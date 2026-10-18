// ============================================================
// 战斗表现层·帧驱动（v0.18.47）
// 拦截原版世界模拟入口 MapBox.checkMainSimulationUpdate：
//   Prefix  —— 击停/冻结/慢动作空帧时返回 false，跳过世界模拟
//   Postfix —— 每帧驱动演出链（HitStop 倒计时、Cinema 计时、
//              粒子更新、导演定时器），并懒清理已销毁世界
// 参考「战斗之神」的 SakugaMod.SimulationPatch 设计。
// ============================================================
using System;
using HarmonyLib;
using UnityEngine;

namespace Code.Combat.Fx
{
    [HarmonyPatch(typeof(MapBox), "checkMainSimulationUpdate")]
    public static class CombatFxFramePatch
    {
        /// <summary>本帧世界模拟是否被跳过（演出仍在继续）。</summary>
        internal static bool Skipped;

        /// <summary>上次演出驱动时刻（真实时间）。倍速=每帧多次调用本方法，用它把 dt 收敛为真实帧间隔。</summary>
        private static float _lastTickReal = -1f;

        private static bool Prefix()
        {
            try
            {
                Skipped = CombatFxHitStop.Active || CombatFxCinema.SkipFrame();
                return !Skipped;
            }
            catch
            {
                Skipped = false;
                return true;
            }
        }

        private static void Postfix()
        {
            try
            {
                // v0.21.12 倍速闪退根因修复：原版倍速 = 每帧多次调用 checkMainSimulationUpdate，
                // 若每次都用 Time.deltaTime，演出链（冷却/姿态/粒子）会被 N 倍加速、招式与地形
                // 破坏在模拟时间轴上成倍密集。改为按"距上次驱动"的真实间隔收敛——每真实帧只
                // 驱动一次，演出跟随真实时间，倍速下不再叠加。
                float nowReal = Time.unscaledTime;
                float elapsed = _lastTickReal < 0f ? 0f : Mathf.Max(0f, nowReal - _lastTickReal);
                _lastTickReal = nowReal;

                CombatFxHitStop.Tick();
                CombatFxCinema.Tick();
                // 模拟时间步：击停/冻结/暂停时为 0（普通粒子冻结；Cine 粒子用 Cinema.Dt 继续）
                float dt = Skipped || World.world == null || World.world.isPaused()
                    ? 0f
                    : Mathf.Min(elapsed, 0.05f) * CombatFxCinema.Rate;
                CombatFxCore.Update(dt, CombatFxCinema.Dt);
                CombatFxDirector.Tick();
                Code.Combat.Moves.CombatMovePose.Tick(dt);      // v0.21.5 招式动作姿态衰减
                Code.Combat.Moves.CombatImpactPose.Tick(dt);    // v0.21.11 受击反应姿态衰减
                Code.Realm.Prototype.SummonEngine.Tick(dt);   // v0.18.99 召唤物生命周期（与演出同节奏）
                Code.Combat.Moves.CombatMoveDirector.Tick(dt);  // v0.21.0 招式冷却
                Code.Combat.AI.CombatBrain.Tick(dt);            // v0.21.0 战斗AI决策（节流）
                Code.Dimension.DimensionWorldManager.TickPendingSave(dt); // v0.21.16 维度空间延迟存档（等地图生成完成）
                // 世界已销毁/存档切换：懒清理演出（新世界由业务重新触发演出）
                if (World.world == null && CombatFxCore.Live > 0)
                {
                    CombatFxDirector.ClearAll();
                    Code.Realm.Prototype.SummonEngine.ClearAll();
                    Code.Combat.Moves.CombatMoveDirector.ClearAll();
                    Code.Combat.Moves.CombatMovePose.ClearAll();   // v0.21.5
                    Code.Combat.Moves.CombatImpactPose.ClearAll(); // v0.21.11
                }
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[CombatFx] frame: " + e.Message);
            }
        }
    }
}
