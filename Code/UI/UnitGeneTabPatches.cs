// ============================================================
// 单位窗·基因页签生命周期补丁（v0.18.4 新增）
//
//  挂 UnitWindow 开/关/刷新入口：
//    OnEnable / showInfo / showStatsRows Postfix → 创建/刷新页签
//    OnDisable Postfix → 强制回退默认页签并隐藏内容
//  与 UnitWindowIntegration（属性行）/ UnitWindowGeneButtonPatch（图标按钮）
//  共存——同方法多补丁由 Harmony 依次执行，互不覆盖。
// ============================================================

using System;
using HarmonyLib;

namespace Code.UI
{
    public static class UnitGeneTabPatches
    {
        private static int _lastRefreshFrame = -1;
        private static long _lastRefreshActorId;

        public static void OnEnable_Postfix(UnitWindow __instance)
        {
            Code.Realm.Genome.GenomeBridgeDiag.P3Diag = "[P3] OnEnable触发"; // v0.25.30 探针
            try { TryRefresh(__instance); } catch { }
        }

        public static void ShowInfo_Postfix(UnitWindow __instance)
        {
            Code.Realm.Genome.GenomeBridgeDiag.P3Diag = "[P3] showInfo触发"; // v0.25.30 探针
            try { TryRefresh(__instance); } catch { }
        }

        public static void ShowStatsRows_Postfix(UnitWindow __instance)
        {
            Code.Realm.Genome.GenomeBridgeDiag.P3Diag = "[P3] showStatsRows触发"; // v0.25.30 探针
            try { TryRefresh(__instance); } catch { }
        }

        public static void OnDisable_Postfix(UnitWindow __instance)
        {
            try { UnitGeneTab.ForceDefaultTab(__instance); } catch { }
            try { UnitGeneTabPanel.Hide(); } catch { }
            try { UnitOwnStatsRows.ClearOwnRows(); } catch { } // v0.18.67: 清理模组自建统计行（原版只清 stats_rows）
        }

        /// <summary>同帧同单位去重刷新（与天人武道同款防抖）</summary>
        private static void TryRefresh(UnitWindow window)
        {
            try
            {
                Actor actor = UnitWindowIntegration.GetUnitActor(window);
                if (actor == null)
                {
                    Code.Realm.Genome.GenomeBridgeDiag.P3Diag = "[P3] TryRefresh actor=null"; // v0.25.30 探针
                    return;
                }
                if (!actor.isAlive())
                {
                    Code.Realm.Genome.GenomeBridgeDiag.P3Diag = "[P3] TryRefresh 单位已死";
                    return;
                }

                int frame = UnityEngine.Time.frameCount;
                long id = 0L;
                try { id = actor.getID(); } catch { }
                if (frame == _lastRefreshFrame && id != 0L && id == _lastRefreshActorId)
                {
                    Code.Realm.Genome.GenomeBridgeDiag.P3Diag = "[P3] TryRefresh 帧去重";
                    return;
                }
                _lastRefreshFrame = frame;
                _lastRefreshActorId = id;

                UnitGeneTab.Refresh(window);
            }
            catch (System.Exception ex)
            {
                Code.Core.DSDebug.Verbose("[GeneCultivation] UnitGeneTabPatches 异常: " + ex.Message);
            }
        }
    }
}
