// ============================================================
// 单位窗·基因页签（v0.18.5 重构）——原版交互深度扩展
//
//  参考天人武道 TmShenZangEntryButton/TmShenZangTabPanel：
//    - 克隆原版单位窗现有页签按钮（族谱/装备/特质优先），改名插入
//    - 替换 tab_action 指向自己的内容容器（挂 scroll.transform_content）
//    - 反射注册进 WindowMetaTabButtonsContainer._tabs 私有列表
//    - 只克隆 UI 对象，不碰原版基因/存档结构（保持弱兼容边界）
//
//  职责（软工检查后拆分，Refresh 只做编排）：
//    ShouldShowEntry  —— 显示判定（存活判定）
//    HideTab          —— 隐藏路径（单位死亡/窗口未就绪）
//    EnsureTab        —— 页签按钮创建/复用 + 注册
//    ResetTabAction   —— 点击页签时 showTab + 懒渲染内容
//    UnitGeneTabPanel —— 纯视图渲染（数据只读）
//
//  懒渲染：默认页签（概览）激活时不重建内容；玩家点击"基因"页签
//  才渲染——避免每次切换单位全量重建 ~40 节点。
// ============================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI
{
    /// <summary>单位窗"基因"页签：入口按钮创建/显隐/注册（数据渲染交给 UnitGeneTabPanel）</summary>
    public static class UnitGeneTab
    {
        public const string TabName = "DS_GeneTab";

        private static readonly FieldInfo TabsListField =
            AccessTools.Field(typeof(WindowMetaTabButtonsContainer), "_tabs");
        private static readonly FieldInfo WorldTipField =
            AccessTools.Field(typeof(WindowMetaTab), "_worldtip_text");
        private static readonly FieldInfo ScrollWindowField =
            FindScrollWindowField();

        /// <summary>原版 Genetics 页签按钮子图标（懒取一次；纯原版素材，不自制）</summary>
        private static Sprite _geneTabIcon;

        /// <summary>
        /// scroll_window 声明在基类 TabbedWindow（protected）。typeof(UnitWindow).GetField
        /// 不沿继承链返回非 public 字段（会得 null），必须沿继承链逐级找（天人武道直接
        /// 访问 window.scroll_window 依赖同程序集可见性，模组必须反射基类字段）。
        /// </summary>
        private static FieldInfo FindScrollWindowField()
        {
            System.Type t = typeof(UnitWindow);
            while (t != null)
            {
                var f = t.GetField("scroll_window",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.DeclaredOnly);
                if (f != null) return f;
                t = t.BaseType;
            }
            return null;
        }

        /// <summary>每次单位窗刷新时调用——纯编排：判定→建页签→挂内容→按激活态懒渲染</summary>
        /// <param name="autoActivate">true=自动激活基因页签并渲染内容（常开：打开单位窗/切单位即显示基因）</param>
        public static void Refresh(UnitWindow window, bool autoActivate)
        {
            try
            {
                if (window == null) return;

                ScrollWindow scroll = GetScrollWindow(window);
                if (scroll == null || scroll.tabs == null)
                {
                    // 预期分支：窗口未完成页签初始化——静默
                    Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] scroll 或 tabs 未就绪，隐藏页签");
                    UnitGeneTabPanel.Hide();
                    return;
                }

                if (!ShouldShowEntry(window))
                {
                    Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] 无存活单位（ShouldShowEntry=false），隐藏页签");
                    HideTab(scroll);
                    return;
                }

                WindowMetaTab tab = EnsureTab(scroll, window);
                if (tab == null)
                {
                    // 预期分支：无可克隆页签源（原版 UI 结构异常）——静默
                    Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] EnsureTab 返回 null（无可克隆页签源），静默");
                    return;
                }
                Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] 页签就绪: " + tab.name +
                    " actor=" + (UnitWindowIntegration.GetUnitActor(window)?.getName() ?? "null") +
                    " onTab=" + scroll.tabs.isActiveTab(tab));

                tab.gameObject.SetActive(true);
                SetTabVisible(tab, true);
                // v0.25.9 常开：打开单位窗/切单位时自动激活基因页签（原版 showTab 只切内容容器+按钮高亮，
                // 不隐藏状态/背包按钮，用户仍可手动切回）
                if (autoActivate) scroll.tabs.showTab(tab);
                bool onTab = scroll.tabs.isActiveTab(tab);
                UnitGeneTabPanel.Ensure(window, tab, scroll);
                // 懒渲染：仅当前激活页签是"基因"时才重建内容
                UnitGeneTabPanel.ShowForActor(UnitWindowIntegration.GetUnitActor(window), onTab);
            }
            catch (Exception ex)
            {
                // 真异常：打 Warning 便于发现
                Code.Core.DSDebug.Warning("[GeneCultivation] UnitGeneTab.Refresh 异常: " + ex.Message);
            }
        }

        /// <summary>强制回退到默认页签（单位窗关闭/切走时）</summary>
        public static void ForceDefaultTab(UnitWindow window)
        {
            try
            {
                if (window == null) return;
                ScrollWindow scroll = GetScrollWindow(window);
                if (scroll?.tabs?.tab_default == null) return;
                WindowMetaTab tab = FindExistingTab(scroll);
                if (tab != null && scroll.tabs.isActiveTab(tab))
                    scroll.tabs.showTab(scroll.tabs.tab_default);
                UnitGeneTabPanel.Hide();
            }
            catch (Exception ex)
            {
                Code.Core.DSDebug.Warning("[GeneCultivation] UnitGeneTab.ForceDefaultTab 异常: " + ex.Message);
            }
        }

        /// <summary>补丁层入口诊断（TryRefresh 未取得单位时调用）</summary>

        /// <summary>显示判定：存活单位即显示（本模组全局可觉醒）</summary>
        private static bool ShouldShowEntry(UnitWindow window)
        {
            Actor actor = UnitWindowIntegration.GetUnitActor(window);
            return actor != null && actor.isAlive();
        }

        /// <summary>
        /// 单位窗「基因」按钮入口：直接切到基因页签（用户要求页签路径）。
        /// 页签不可用（无克隆源等）时回退独立窗口（GeneEditorWindow）。
        /// </summary>
        public static void ShowTab(UnitWindow window)
        {
            try
            {
                Actor actor = UnitWindowIntegration.GetUnitActor(window);
                if (actor == null || !actor.isAlive())
                {
                    GeneEditorWindow.Show(actor);
                    return;
                }
                ScrollWindow scroll = GetScrollWindow(window);
                if (scroll?.tabs == null)
                {
                    GeneEditorWindow.Show(actor);
                    return;
                }
                WindowMetaTab tab = EnsureTab(scroll, window);
                if (tab == null)
                {
                    GeneEditorWindow.Show(actor);
                    return;
                }
                tab.gameObject.SetActive(true);
                SetTabVisible(tab, true);
                scroll.tabs.showTab(tab);
                UnitGeneTabPanel.Ensure(window, tab, scroll);
                UnitGeneTabPanel.ShowForActor(actor, true);
                Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] 基因按钮 → 基因页签: " + tab.name);
            }
            catch (Exception ex)
            {
                Code.Core.DSDebug.Warning("[GeneCultivation] UnitGeneTab.ShowTab 异常: " + ex.Message);
                try { GeneEditorWindow.Show(UnitWindowIntegration.GetUnitActor(window)); } catch { }
            }
        }

        /// <summary>隐藏路径：隐藏页签按钮并卸载内容</summary>
        private static void HideTab(ScrollWindow scroll)
        {
            WindowMetaTab existing = FindExistingTab(scroll);
            if (existing != null) SetTabVisible(existing, false);
            UnitGeneTabPanel.Hide();
        }

        /// <summary>页签按钮创建/复用：克隆源页签 → 换名 → 重绑 tab_action → 注册 _tabs</summary>
        private static WindowMetaTab EnsureTab(ScrollWindow scroll, UnitWindow window)
        {
            WindowMetaTab existing = FindExistingTab(scroll);
            if (existing != null)
            {
                Reconfigure(existing, scroll, window);
                return existing;
            }

            WindowMetaTab source = FindCloneSource(scroll.tabs);
            if (source == null)
            {
                Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] 无克隆源页签（FindCloneSource=null）");
                return null;
            }
            Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] 克隆源页签: " + source.name);

            GameObject tabObj = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent);
            tabObj.name = TabName;
            // 克隆按钮插到按钮行首：规避行尾被裁剪/布局不刷新导致的不可见（v0.24.2-24.6 手机"页签也没有"根因）
            tabObj.transform.SetAsFirstSibling();
            try
            {
                var btnParent = tabObj.transform.parent;
                if (btnParent != null)
                {
                    var layout = btnParent.GetComponent<UnityEngine.UI.HorizontalOrVerticalLayoutGroup>();
                    if (layout != null)
                    {
                        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(btnParent as RectTransform);
                    }
                }
            }
            catch { }

            WindowMetaTab newTab = tabObj.GetComponent<WindowMetaTab>();
            if (newTab == null)
            {
                UnityEngine.Object.Destroy(tabObj);
                return null;
            }

            newTab.tab_elements.Clear();
            ResetTabAction(newTab, scroll, window);

            TipButton tip = newTab.GetComponent<TipButton>();
            if (tip != null)
            {
                tip.textOnClick = UILocalization.CurrentLanguage == "en" ? "Gene" : UILocalization.Get("unit_gene_tab");
                tip.textOnClickDescription = UILocalization.CurrentLanguage == "en" ? "Gene cultivation overview" : UILocalization.Get("unit_gene_tab_desc");
                tip.text_description_2 = string.Empty;
            }

            ApplyWorldTip(newTab);
            ApplyIcon(newTab);
            RegisterTab(scroll.tabs, newTab);
            return newTab;
        }

        internal static ScrollWindow GetScrollWindow(UnitWindow window)
        {
            if (window == null) return null;
            // scroll_window 是 TabbedWindow 的 protected 字段，跨程序集必须反射
            try
            {
                if (ScrollWindowField != null)
                {
                    var scroll = ScrollWindowField.GetValue(window) as ScrollWindow;
                    if (scroll != null)
                    {
                        Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] scroll_window 经字段反射取得");
                        return scroll;
                    }
                }
            }
            catch { }
            try
            {
                var prop = typeof(UnitWindow).GetProperty("scroll_window",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (prop != null)
                {
                    var scroll = prop.GetValue(window, null) as ScrollWindow;
                    if (scroll != null)
                    {
                        Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] scroll_window 经属性反射取得");
                        return scroll;
                    }
                }
            }
            catch { }
            try
            {
                Component host = (Component)(object)window;
                var scroll = host.GetComponent<ScrollWindow>() ?? host.GetComponentInParent<ScrollWindow>();
                if (scroll != null)
                {
                    Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] scroll_window 经组件查找取得");
                    return scroll;
                }
            }
            catch { }
            Code.Core.DSDebug.Verbose("[GeneCultivation][UnitGeneTab] scroll_window 三种方式均未取得");
            return null;
        }

        private static WindowMetaTab FindExistingTab(ScrollWindow scroll)
        {
            if (scroll?.tabs == null) return null;
            foreach (WindowMetaTab t in scroll.tabs.GetComponentsInChildren<WindowMetaTab>(true))
            {
                if (t != null && t.name == TabName) return t;
            }
            return null;
        }

        private static void SetTabVisible(WindowMetaTab tab, bool show)
        {
            if (tab == null) return;
            try { tab.toggleActive(show); } catch { }
            CanvasGroup cg = tab.GetComponent<CanvasGroup>();
            if (cg == null) return;
            cg.alpha = show ? 1f : 0f;
            cg.interactable = show;
            cg.blocksRaycasts = show;
        }

        private static WindowMetaTab FindCloneSource(WindowMetaTabButtonsContainer container)
        {
            WindowMetaTab[] all = container.GetComponentsInChildren<WindowMetaTab>(true);
            // 优先克隆族谱/装备/特质等常驻页签
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i]?.name ?? "";
                if (n.IndexOf("genealog", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("equipment", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("trait", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("subspecies", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (all[i].gameObject.activeSelf && all[i].getState())
                        return all[i];
                }
            }
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.activeSelf && all[i].getState())
                    return all[i];
            }
            return all.Length > 0 ? all[0] : null;
        }

        /// <summary>
        /// 替换克隆来的 tab_action（RemoveAllListeners 清不掉序列化持久监听器，必须整体替换）。
        /// 点击"基因"页签时：showTab 激活容器（原版机制）+ 懒渲染内容。
        /// showTab 内部不回调 tab_action，无递归风险（已核对原版 startTabAction）。
        /// </summary>
        private static void ResetTabAction(WindowMetaTab tab, ScrollWindow scroll, UnitWindow window)
        {
            tab.tab_action = new WindowMetaTabEvent();
            tab.tab_action.AddListener(_ =>
            {
                try
                {
                    scroll.tabs.showTab(tab);
                    Actor actor = UnitWindowIntegration.GetUnitActor(window);
                    if (actor != null && actor.isAlive())
                        UnitGeneTabPanel.ShowForActor(actor, true);
                }
                catch (Exception ex)
                {
                    Code.Core.DSDebug.Warning("[GeneCultivation] UnitGeneTab 页签点击异常: " + ex.Message);
                }
            });
        }

        private static void ApplyWorldTip(WindowMetaTab tab)
        {
            if (WorldTipField == null || tab == null) return;
            try
            {
                string tipText = UILocalization.CurrentLanguage == "en" ? "Gene" : UILocalization.Get("unit_gene_tab");
                WorldTipField.SetValue(tab, tipText);
            }
            catch { }
        }

        private static void Reconfigure(WindowMetaTab tab, ScrollWindow scroll, UnitWindow window)
        {
            if (tab == null || scroll == null) return;
            ResetTabAction(tab, scroll, window);
            ApplyIcon(tab);
            RegisterTab(scroll.tabs, tab);
        }

        /// <summary>懒取原版 Genetics 页签按钮子图标（subspecies 预制体 → Genetics 页签 → 第一个非底板子图标）</summary>
        private static void PrepareGeneTabIcon()
        {
            if (_geneTabIcon != null) return;
            try
            {
                var m = HarmonyLib.AccessTools.Method(typeof(WindowPreloader), "getWindowPrefab");
                if (m == null) return;
                var prefab = m.Invoke(null, new object[] { "subspecies" }) as ScrollWindow;
                if (prefab == null || prefab.tabs == null) return;
                var tabs = prefab.tabs.transform.FindAllRecursive<WindowMetaTab>();
                foreach (var tab in tabs)
                {
                    if (tab == null || tab.name != "Genetics") continue;
                    Image[] imgs = tab.GetComponentsInChildren<Image>(true);
                    foreach (var img in imgs)
                    {
                        if (img == null || img.gameObject == tab.gameObject) continue;
                        if (img.sprite != null && img.sprite.name != null
                            && img.sprite.name.IndexOf("tab_button", StringComparison.OrdinalIgnoreCase) >= 0)
                            continue; // 跳过页签底板，改子图标（与天人武道 ApplyIcon 同规则）
                        _geneTabIcon = img.sprite;
                        return;
                    }
                    return; // Genetics 按钮无子图标 → 保持克隆源图标
                }
            }
            catch { }
        }

        /// <summary>把克隆页签的子图标换成原版 Genetics 图标（基因主题语义正确）</summary>
        private static void ApplyIcon(WindowMetaTab tab)
        {
            if (tab == null) return;
            PrepareGeneTabIcon();
            if (_geneTabIcon == null) return;
            try
            {
                Image[] images = tab.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img == null || img.gameObject == tab.gameObject) continue;
                    if (img.sprite != null && img.sprite.name != null
                        && img.sprite.name.IndexOf("tab_button", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    img.sprite = _geneTabIcon;
                    img.color = Color.white;
                    return; // 只换第一个子图标
                }
            }
            catch { }
        }

        private static void RegisterTab(WindowMetaTabButtonsContainer container, WindowMetaTab tab)
        {
            if (TabsListField?.GetValue(container) is List<WindowMetaTab> list)
            {
                if (!list.Contains(tab))
                    list.Add(tab);
            }
            try
            {
                FieldInfo containerField = AccessTools.Field(typeof(WindowMetaTab), "container");
                containerField?.SetValue(tab, container);
            }
            catch { }
        }
    }
}
