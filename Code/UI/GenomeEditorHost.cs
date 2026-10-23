// ============================================================
// 文件: GenomeEditorHost.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P3 渲染批2——单位窗基因页签宿主：克隆原版 GeneEditor 挂载到页签容器
// 克隆路径（P0 定稿 R1）：
//   - 克隆源 = WindowPreloader.getWindowPrefab("subspecies") → Genetics 页签
//     （WindowMetaTab.gameObject.name=="Genetics"）内含 GeneEditor 的 tab 元素根
//   - Instantiate 到页签容器 → 剥离 ScrollWindow/SubspeciesWindow/WindowMetaElementBase
//     （离开亚种窗会 NRE）→ 顶锚贴齐 + LayoutElement 控高 → SetActive(true) 触发 OnEnable→load()
//   - 禁止重置 _initialized（否则 _dictionary_gene_buttons 重复 Add）
// 会话：挂载时 GenomeEditorSession.SetTarget(actor)；隐藏时 EndSession（门禁回归：亚种编辑器零污染）
// 回退：克隆失败时回退自绘视图（UnitGeneTabPanel.Render 保底，不空屏）
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using Code.Core;
using Code.Realm.Genome;

namespace Code.UI
{
    /// <summary>原版 GeneEditor 克隆宿主（页签容器）</summary>
    public static class GenomeEditorHost
    {
        private static bool _sourcePrepared = false;
        private static Transform _cloneSource;      // Genetics 页签内含 GeneEditor 的元素根
        private static GeneEditor _hostedEditor;    // 当前宿主实例（克隆体）

        /// <summary>挂载原版 GeneEditor 并绑定会话到目标单位（幂等；失败返回 false）</summary>
        public static bool EnsureHost(Actor actor, RectTransform container)
        {
            if (actor == null || container == null) return false;
            try
            {
                // 会话绑定必须先于克隆体激活：克隆体 Instantiate 即激活 → OnEnable → load()。
                // 若此时 Session 未绑定（IsActive=false）会走原版 loadChromosomes/showGenes，
                // 读全局 _meta_object（未选中亚种为 null）/ null _window_subspecies → NRE
                // （v0.24.9 实机堆栈：GeneEditor.load ← EnsureHost ← 基因按钮，日志 NRE 先于会话绑定）
                if (!GenomeEditorSession.SetTarget(actor)) return false;

                if (!_sourcePrepared)
                {
                    _sourcePrepared = true;
                    PrepareCloneSource();
                }
                if (_cloneSource == null) return false;

                if (_hostedEditor != null && _hostedEditor.transform.parent != container)
                {
                    // 容器已重建（单位窗关闭再打开）→ 旧克隆体挂在旧容器上，销毁重建
                    try { UnityEngine.Object.Destroy(_hostedEditor.gameObject); } catch (Exception) { }
                    _hostedEditor = null;
                }

                if (_hostedEditor == null)
                {
                    // 关键时序：Instantiate 的瞬间克隆体内所有组件 OnEnable 连锁执行。
                    // 若直接实例化到容器，WindowMetaElement`2 等组件未剥离即触发
                    // OnEnable → 读亚种全局态 NRE（v0.25.1 实机堆栈）。
                    // 因此：实例化到「非激活」临时父（OnEnable 不触发）→ 剥离 → 挂容器（保持 inactive）→ 由调用方 SetActive(true) 触发。
                    var tmp = new GameObject("[GC]HostTmp", typeof(RectTransform));
                    tmp.SetActive(false);
                    GameObject inst = UnityEngine.Object.Instantiate(_cloneSource.gameObject, tmp.transform, false);
                    inst.SetActive(false);
                    UnityEngine.Object.Destroy(tmp);   // 帧末销毁；inst 已显式 inactive，脱离后不会激活
                    StripWindowComponents(inst);
                    AlignToContainer(inst);
                    inst.transform.SetParent(container, false);
                    _hostedEditor = inst.GetComponentInChildren<GeneEditor>(true);
                    if (_hostedEditor == null)
                    {
                        DSDebug.Warning("[GeneCultivation][P3] 克隆体未找到 GeneEditor，回退自绘");
                        return false;
                    }
                }

                // 关键：激活整个克隆根（v0.25.14）。此前只 SetActive(GeneEditor 子对象)，
                // 克隆根一直 inactive → 位点池容器等在 GeneEditor 子树外的元素不可见（白板：
                // 标题"遗传学"在 GeneEditor 子树内可见，位点区在子树外空白）。
                // 从 hostedEditor 向上找到克隆根（容器直属子），再整根激活 → 触发 OnEnable→load()→我方 Prefix 渲染。
                Transform hostRoot = _hostedEditor.transform;
                while (hostRoot.parent != null && hostRoot.parent != container)
                {
                    hostRoot = hostRoot.parent;
                }
                hostRoot.gameObject.SetActive(true);   // OnEnable → load() → 我方 Prefix 接管渲染（IsActive=true）
                GenomeEditorBridge.P3Diag = "[P3] host OK root=" + hostRoot.name +
                    " rootActive=" + hostRoot.gameObject.activeInHierarchy +
                    " editor=" + _hostedEditor.name +
                    " actor=" + actor.getName();
                return true;
            }
            catch (Exception e)
            {
                DSDebug.Warning("[GeneCultivation][P3] GenomeEditorHost 挂载异常: " + e.Message);
                return false;
            }
        }

        /// <summary>隐藏宿主并结束会话；销毁克隆体（单位窗关闭后容器重建，静态缓存的旧实例会挂在已销毁的旧容器上导致白板）</summary>
        public static void HideHost()
        {
            if (_hostedEditor != null)
            {
                try { UnityEngine.Object.Destroy(_hostedEditor.gameObject); } catch (Exception) { }
                _hostedEditor = null;
            }
            GenomeEditorSession.EndSession();
        }

        // ---------- 克隆源准备（懒加载一次） ----------

        private static void PrepareCloneSource()
        {
            try
            {
                // WindowPreloader.getWindowPrefab 是 private static → 反射调用
                var m = HarmonyLib.AccessTools.Method(typeof(WindowPreloader), "getWindowPrefab");
                if (m == null) return;
                var prefab = m.Invoke(null, new object[] { "subspecies" }) as ScrollWindow;
                if (prefab == null || prefab.tabs == null) return;

                // 找 Genetics 页签（tab 元素根 = 含 GeneEditor 者）
                var tabs = prefab.tabs.transform.FindAllRecursive<WindowMetaTab>();
                foreach (var tab in tabs)
                {
                    if (tab == null || tab.name != "Genetics") continue;
                    foreach (var el in tab.tab_elements)
                    {
                        if (el == null) continue;
                        if (el.GetComponentInChildren<GeneEditor>(true) != null)
                        {
                            _cloneSource = el;
                            DSDebug.Verbose("[GeneCultivation][P3] 克隆源就绪: " + el.name);
                            return;
                        }
                    }
                }
                DSDebug.Warning("[GeneCultivation][P3] 未找到 Genetics 页签 GeneEditor 克隆源");
            }
            catch (Exception e)
            {
                DSDebug.Warning("[GeneCultivation][P3] 克隆源准备失败: " + e.Message);
            }
        }

        // ---------- 克隆体处理 ----------

        /// <summary>剥离亚种窗专属组件（离开亚种窗会 NRE）；_initialized 禁止重置</summary>
        private static void StripWindowComponents(GameObject inst)
        {
            var scrolls = inst.GetComponentsInChildren<ScrollWindow>(true);
            foreach (var c in scrolls) UnityEngine.Object.Destroy(c);
            var subspecies = inst.GetComponentsInChildren<SubspeciesWindow>(true);
            foreach (var c in subspecies) UnityEngine.Object.Destroy(c);
            var metaBases = inst.GetComponentsInChildren<WindowMetaElementBase>(true);
            foreach (var c in metaBases) UnityEngine.Object.Destroy(c);
        }

        /// <summary>顶锚贴齐容器 + 固定高度 300（R1：克隆体顶锚贴齐 + LayoutElement 控高）。
        /// v0.25.3 曾改为 ContentSizeFitter 自适应，破坏克隆根内部布局（位点/染色体区白板），回退。</summary>
        private static void AlignToContainer(GameObject inst)
        {
            var rt = inst.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.offsetMin = new Vector2(0f, -300f);   // 高度 300（原版 GeneEditor 内容区高度）
                rt.offsetMax = Vector2.zero;
            }
            var le = inst.GetComponent<LayoutElement>();
            if (le == null) le = inst.AddComponent<LayoutElement>();
            le.preferredHeight = 300f;
            le.minHeight = 300f;
        }
    }
}
