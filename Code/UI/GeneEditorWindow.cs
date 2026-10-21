// ============================================================
// 文件: GeneEditorWindow.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P3 渲染接管——单位基因系统窗口（原版 ScrollWindow 克隆，v0.24.8）
//  创建方式（对照天人武道 LegacyWindowSuppressor 的原版窗口机制）：
//    克隆原版「亚种编辑器」ScrollWindow 预制体 → 实例化到 CanvasMain.transformWindows
//    → 剥离 SubspeciesWindow 组件（离开亚种上下文防 NRE）→ 隐藏非 Genetics 页签
//    → 窗口只剩基因系统（原版观感、原版标题栏/关闭/拖动）。
//  渲染：Genetics 页签内的原版 GeneEditor 由 GenomeEditorBridge 接管
//    （Session.IsActive 时渲染单位真实基因组，A 路代理渲染）；
//    窗口关闭（clickCloseButton）经 Bridge 清理会话 → 亚种编辑器零污染。
//  单位窗基因按钮 → Show(actor)。
// ============================================================

using System;
using UnityEngine;
using Code.Core;
using Code.Realm.Genome;

namespace Code.UI
{
    /// <summary>单位基因系统窗口（原版亚种窗口克隆）</summary>
    public static class GeneEditorWindow
    {
        private static ScrollWindow _window;
        private static WindowMetaTab _geneticsTab;
        private static bool _prepared;

        /// <summary>惰性克隆亚种窗口（一次性；窗口默认隐藏）</summary>
        public static void Ensure()
        {
            if (_prepared) return;
            _prepared = true;
            try
            {
                // WindowPreloader.getWindowPrefab 是 private static → 反射（P0 定稿：必须走 WindowPreloader）
                var m = HarmonyLib.AccessTools.Method(typeof(WindowPreloader), "getWindowPrefab");
                if (m == null) return;
                var prefab = m.Invoke(null, new object[] { "subspecies" }) as ScrollWindow;
                if (prefab == null) return;

                _window = UnityEngine.Object.Instantiate(prefab, CanvasMain.instance.transformWindows);
                _window.name = "GeneCultivation_GenesWindow";
                _window.screen_id = "GeneCultivation_Genes";

                // 剥离亚种窗组件（离开亚种编辑上下文会 NRE/污染）
                var subspecies = _window.GetComponentsInChildren<SubspeciesWindow>(true);
                foreach (var c in subspecies) UnityEngine.Object.Destroy(c);

                // 只保留 Genetics 页签：其余页签按钮透明隐藏
                var tabs = _window.tabs.transform.FindAllRecursive<WindowMetaTab>();
                foreach (var tab in tabs)
                {
                    if (tab == null) continue;
                    if (tab.name == "Genetics")
                    {
                        _geneticsTab = tab;
                    }
                    else
                    {
                        try { tab.toggleActive(false); } catch { }
                    }
                }

                _window.gameObject.SetActive(false);
                DSDebug.Verbose("[GeneCultivation][P3] 基因系统窗口克隆就绪 (Genetics页签: " + (_geneticsTab != null) + ")");
            }
            catch (Exception e)
            {
                DSDebug.Warning("[GeneCultivation][P3] 基因系统窗口克隆失败: " + e.Message);
            }
        }

        /// <summary>打开并渲染指定单位的基因（原版观感）</summary>
        public static void Show(Actor actor)
        {
            if (actor == null) return;
            Ensure();
            if (_window == null || _geneticsTab == null) return;
            try
            {
                // 先绑定会话（IsActive=true），再激活窗口触发 GeneEditor.OnEnable→load() → Bridge 接管渲染
                if (!GenomeEditorSession.SetTarget(actor))
                {
                    DSDebug.Warning("[GeneCultivation][P3] 单位基因组构建失败（无染色体？）");
                }
                _window.gameObject.SetActive(true);
                _window.tabs.showTab(_geneticsTab);
                DSDebug.Verbose("[GeneCultivation][P3] 基因系统窗口打开: " + actor.getName());
            }
            catch (Exception e)
            {
                DSDebug.Warning("[GeneCultivation][P3] 基因系统窗口打开异常: " + e.Message);
            }
        }

        /// <summary>关闭并结束会话（亚种编辑器门禁回归）</summary>
        public static void Hide()
        {
            GenomeEditorSession.EndSession();
            if (_window != null) _window.gameObject.SetActive(false);
        }

        public static bool IsVisible()
        {
            return _window != null && _window.gameObject.activeInHierarchy;
        }

        /// <summary>该窗口是否为本模组克隆实例（供关闭补丁判断）</summary>
        internal static bool IsOwned(ScrollWindow window)
        {
            return _window != null && window != null && _window == window;
        }
    }
}
