// ============================================================
// 文件: GeneEditorWindow.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P3 渲染接管——单位基因渲染弹窗（uGUI）
//  单位窗「基因」按钮（GeneGraphButton）点击 → 打开本窗口：
//    内容区挂载原版 GeneEditor 克隆体（GenomeEditorHost），
//    GenomeEditorSession 绑定当前单位，渲染真实基因组（A 路代理渲染）。
//  背景：0.51.4 手机单位窗无可见页签栏，页签克隆方案在手机端不可用；
//   按钮→弹窗为确定可行的挂载路径（P0 R1 克隆路径复用）。
//  关闭/隐藏时 EndSession（门禁回归：亚种编辑器零污染）。
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using Code.Core;
using Code.Realm.Genome;

namespace Code.UI
{
    /// <summary>单位基因渲染弹窗（原版 GeneEditor 宿主）</summary>
    public static class GeneEditorWindow
    {
        private static GameObject _root;
        private static RectTransform _content;
        private static Text _titleText;
        private static bool _inited;

        /// <summary>惰性创建窗口骨架（隐藏态）</summary>
        public static void Ensure()
        {
            if (_inited) return;
            _inited = true;
            try
            {
                var parent = CanvasMain.instance != null
                    ? CanvasMain.instance.transformWindows
                    : CanvasMain.instance != null ? CanvasMain.instance.transform : null;
                if (parent == null)
                {
                    DSDebug.Warning("[GeneCultivation][P3] CanvasMain 未就绪，基因渲染窗口延迟创建");
                    _inited = false;
                    return;
                }

                _root = new GameObject("[GeneCultivation] GeneEditorWindow", typeof(RectTransform));
                _root.transform.SetParent(parent, false);
                _root.SetActive(false);

                // 全屏遮罩（点击关闭）
                var mask = new GameObject("Mask", typeof(RectTransform), typeof(Image));
                mask.transform.SetParent(_root.transform, false);
                var maskRt = mask.GetComponent<RectTransform>();
                maskRt.anchorMin = Vector2.zero;
                maskRt.anchorMax = Vector2.one;
                maskRt.offsetMin = Vector2.zero;
                maskRt.offsetMax = Vector2.zero;
                var maskImg = mask.GetComponent<Image>();
                maskImg.color = new Color(0f, 0f, 0f, 0.55f);
                maskImg.raycastTarget = true;
                mask.AddComponent<Button>().onClick.AddListener(Hide);

                // 主面板
                var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(_root.transform, false);
                var panelRt = panel.GetComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.5f, 0.5f);
                panelRt.anchorMax = new Vector2(0.5f, 0.5f);
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.sizeDelta = new Vector2(760f, 560f);
                var panelImg = panel.GetComponent<Image>();
                panelImg.color = new Color(0.09f, 0.10f, 0.13f, 0.98f);

                // 标题栏
                var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
                titleBar.transform.SetParent(panel.transform, false);
                var tbRt = titleBar.GetComponent<RectTransform>();
                tbRt.anchorMin = new Vector2(0f, 1f);
                tbRt.anchorMax = new Vector2(1f, 1f);
                tbRt.pivot = new Vector2(0.5f, 1f);
                tbRt.offsetMin = new Vector2(0f, -34f);
                tbRt.offsetMax = Vector2.zero;
                titleBar.GetComponent<Image>().color = new Color(0.16f, 0.17f, 0.22f, 1f);

                _titleText = new GameObject("Title", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                _titleText.transform.SetParent(titleBar.transform, false);
                var titleRt = _titleText.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 0f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.offsetMin = new Vector2(12f, 0f);
                titleRt.offsetMax = new Vector2(-50f, 0f);
                _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _titleText.fontSize = 16;
                _titleText.fontStyle = FontStyle.Bold;
                _titleText.color = new Color(0.95f, 0.85f, 0.5f);
                _titleText.alignment = TextAnchor.MiddleLeft;

                // 关闭按钮
                var closeBtn = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
                closeBtn.transform.SetParent(titleBar.transform, false);
                var closeRt = closeBtn.GetComponent<RectTransform>();
                closeRt.anchorMin = new Vector2(1f, 0.5f);
                closeRt.anchorMax = new Vector2(1f, 0.5f);
                closeRt.pivot = new Vector2(0.5f, 0.5f);
                closeRt.sizeDelta = new Vector2(28f, 28f);
                closeRt.anchoredPosition = new Vector2(-8f, 0f);
                closeBtn.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.3f, 0.9f);
                closeBtn.GetComponent<Button>().onClick.AddListener(Hide);
                var closeText = new GameObject("X", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                closeText.transform.SetParent(closeBtn.transform, false);
                var closeTextRt = closeText.GetComponent<RectTransform>();
                closeTextRt.anchorMin = Vector2.zero;
                closeTextRt.anchorMax = Vector2.one;
                closeTextRt.offsetMin = Vector2.zero;
                closeTextRt.offsetMax = Vector2.zero;
                closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                closeText.fontSize = 14;
                closeText.fontStyle = FontStyle.Bold;
                closeText.color = Color.white;
                closeText.alignment = TextAnchor.MiddleCenter;
                closeText.text = "×";

                // 内容容器（原版 GeneEditor 克隆宿主）
                _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
                _content.transform.SetParent(panel.transform, false);
                _content.anchorMin = new Vector2(0f, 0f);
                _content.anchorMax = new Vector2(1f, 1f);
                _content.pivot = new Vector2(0.5f, 0.5f);
                _content.offsetMin = new Vector2(8f, 8f);
                _content.offsetMax = new Vector2(-8f, -42f);

                _root.SetActive(false);
            }
            catch (System.Exception e)
            {
                DSDebug.Warning("[GeneCultivation][P3] 基因渲染窗口创建失败: " + e.Message);
            }
        }

        /// <summary>打开并渲染指定单位的基因（原版观感）</summary>
        public static void Show(Actor actor)
        {
            if (actor == null) return;
            Ensure();
            if (_root == null) return;

            try
            {
                _root.SetActive(true);
                string actorName = actor.getName();
                if (_titleText != null)
                {
                    _titleText.text = UILocalization.CurrentLanguage == "en"
                        ? "Gene System - " + actorName
                        : "基因系统 - " + actorName;
                }

                // 绑定会话 + 挂载原版 GeneEditor 克隆体
                if (GenomeEditorHost.EnsureHost(actor, _content))
                {
                    DSDebug.Verbose("[GeneCultivation][P3] 基因渲染窗口已挂载: " + actorName);
                }
                else
                {
                    DSDebug.Warning("[GeneCultivation][P3] GeneEditor 挂载失败，显示空白容器");
                }
            }
            catch (System.Exception e)
            {
                DSDebug.Warning("[GeneCultivation][P3] 基因渲染窗口打开异常: " + e.Message);
            }
        }

        /// <summary>关闭并结束会话（亚种编辑器门禁回归）</summary>
        public static void Hide()
        {
            try { GenomeEditorHost.HideHost(); } catch { }
            if (_root != null) _root.SetActive(false);
        }

        public static bool IsVisible()
        {
            return _root != null && _root.activeSelf;
        }
    }
}
