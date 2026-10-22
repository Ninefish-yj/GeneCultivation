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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitWindow), "OnEnable")]
        public static void OnEnable_Postfix(UnitWindow __instance)
        {
            try { TryRefresh(__instance, false); } catch { } // 页签常驻：只创建/显示按钮，不自动激活内容
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitWindow), "showInfo")]
        public static void ShowInfo_Postfix(UnitWindow __instance)
        {
            try { TryRefresh(__instance, false); } catch { } // 切单位也保持当前页签，不自动跳基因
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]
        public static void ShowStatsRows_Postfix(UnitWindow __instance)
        {
            // 每帧刷新不抢页签：用户手动切到状态/背包后保持（常开只作用于开窗/切单位）
            try { TryRefresh(__instance, false); } catch { }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitWindow), "OnDisable")]
        public static void OnDisable_Postfix(UnitWindow __instance)
        {
            try { UnitGeneTab.ForceDefaultTab(__instance); } catch { }
            try { UnitGeneTabPanel.Hide(); } catch { }
            try { UnitOwnStatsRows.ClearOwnRows(); } catch { } // v0.18.67: 清理模组自建统计行（原版只清 stats_rows）
        }

        /// <summary>同帧同单位去重刷新（与天人武道同款防抖）</summary>
        /// <param name="autoActivate">true=自动激活基因页签（常开：开窗/切单位）；false=不抢当前页签</param>
        private static void TryRefresh(UnitWindow window, bool autoActivate)
        {
            try
            {
                Actor actor = UnitWindowIntegration.GetUnitActor(window);
                if (actor == null || !actor.isAlive()) return;

                int frame = UnityEngine.Time.frameCount;
                long id = 0L;
                try { id = actor.getID(); } catch { }
                if (frame == _lastRefreshFrame && id != 0L && id == _lastRefreshActorId)
                    return;

                // v0.25.10：只有 Refresh 真正执行（scroll 就绪+EnsureTab 成功）才记录去重。
                // 否则 OnEnable 时 scroll.tabs 未就绪会提前 return 并占掉同帧去重，
                // 导致 showInfo（scroll 已就绪）被跳过 → 常开失效。
                if (UnitGeneTab.Refresh(window, autoActivate))
                {
                    _lastRefreshFrame = frame;
                    _lastRefreshActorId = id;
                }
            }
            catch (System.Exception ex)
            {
                Code.Core.DSDebug.Verbose("[GeneCultivation] UnitGeneTabPatches 异常: " + ex.Message);
            }
        }
    }
}
