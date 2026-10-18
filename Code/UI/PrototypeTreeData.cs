// ============================================================
// 异能树 · 数据与布局层（v0.19.17 径向辐条链式 · 紧凑重排）
// 职责（单一职责）：
//   1. 从 PrototypeRegistry（异能全量库）构建树节点模型：异能体系 → 大类 → 小类 → 异能
//   2. 辐射树布局（参考流放之路天赋树）：
//      · 根"异能"居中；
//      · 大类按系内小类数加权分配圆周扇区（容纳密度差异，花瓣对称）；
//      · 小类在其大类扇区内等角距均分（不再按叶数加权，杜绝同系内疏密不均）；
//      · 异能叶 = 以所属小类为起点、沿辐条短距外延的"径向链"（v0.19.17 替代 v0.19.15 全树3大环）：
//        高品贴内、低品在外（传承源头向外辐射链接更低品质异能），
//        链内相邻叶半径差=RADIAL_STEP（≥高品直径×1.15 防重叠），
//        角度微错开成扇面（leafStep=min(2.5°, subSector/叶数)）。
//        最密小类（黑暗系·暗影11叶）链尾半径≈1506，树整体紧凑——不再把叶子甩到3000+px外。
//   3. 防重叠：圆形节点，链内靠径向步距（半径差≥直径×1.15），串间靠扇区角距（弧距≥直径×1.15）。
//   4. 输出可见层包围盒（根+大类+小类，异能叶默认折叠由渲染层展开），供 Fit 聚焦。
// 纯数据，不含任何 GUI 调用（UI 与业务/数据逻辑分离）。
// 自适应：大类顺序动态枚举（新增大类自动追加扇区），小类/异能增减只改变所属扇区角度，
//         无需重新设计布局——拓展异能不用改树。
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using Code.Realm.Prototype;

namespace Code.UI
{
    /// <summary>异能树节点（数据模型）</summary>
    public sealed class ProtoTreeNode
    {
        public string Id;          // 异能id / 大类名 / 小类名
        public string Name;        // 显示名
        public int Level;          // 0=大类 1=小类 2=异能叶子
        public string ParentId;    // 父节点 Id（连线用）
        public float X, Y;         // 布局坐标（左上角）
        public float W, H;         // 节点尺寸（圆形节点：W=H=直径）
        public PrototypeDef Def;   // 异能定义（根/大类/小类为 null）
        public Color Color;        // 节点主色（大类统一色系，异能随大类）
    }

    /// <summary>异能树布局结果（数据模型）</summary>
    public sealed class ProtoTreeLayout
    {
        public readonly List<ProtoTreeNode> Nodes = new List<ProtoTreeNode>();
        public readonly List<QualityEdge> QualityEdges = new List<QualityEdge>();   // v0.19.1 品质进化链（低品叶 → 更高品质上级）
        public Rect Bounds;        // 可见层内容包围盒（根+系+小类，渲染层 Fit 用；异能叶按需展开）

        private Dictionary<string, ProtoTreeNode> _byId;

        public ProtoTreeNode GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null)
            {
                _byId = new Dictionary<string, ProtoTreeNode>();
                foreach (var n in Nodes) _byId[n.Id] = n;
            }
            _byId.TryGetValue(id, out var node);
            return node;
        }
    }

    /// <summary>品质进化链边（v0.19.1）：LowId（低品质异能）→ HighId（同家族更高品质上级，PrototypeEvolution.GetEvolveTarget）</summary>
    public sealed class QualityEdge
    {
        public string LowId;
        public string HighId;
    }

    /// <summary>
    /// 异能树辐射布局构建器（单一职责：输入异能全量库 → 输出辐射树布局）
    /// 纯数据计算，无 GUI / 无业务副作用。
    /// </summary>
    public static class ProtoTreeLayoutBuilder
    {
        // ---- 辐射布局常量（v0.19.17 径向辐条链式 · 圆形节点）----
        // 节点为圆形：W=H=直径。防重叠数学：
        // · 链内（同小类）：相邻叶半径差 RADIAL_STEP ≥ 高品直径×1.15，径向不重叠；
        // · 串间（相邻小类）：小类扇区角距 → 链尾弧距 ≥ 叶直径×1.15，角度不重叠。
        // · 大类扇区 = max(小类数×MIN_SUB_SECTOR+6°, 叶占比×360, MIN_CAT_SECTOR) 归一化：
        //   小类多的系（精神系11小类）扇区大，容纳密度差异，整体花瓣对称；
        // · 小类扇区 = 大类扇区 / 小类数（等角距均分，不再按叶数加权——杜绝同系内疏密不均）；
        // · 异能叶 = 所属小类起点的径向链：高品(tier≥7)贴内、中品(4~6)居中、低品(≤3)在外，
        //   链内角度微错开成扇面（leafStep=min(2.5°, subSector/叶数)），半径逐叶+RADIAL_STEP。
        //   最密小类（黑暗系·暗影11叶）链尾=850+40+11×56=1506，全树最大半径≈1506——紧凑，无长线。
        public const float NODE_ROOT_D = 100f;    // 根（异能）圆直径
        public const float NODE_CAT_D = 72f;     // 大类圆直径
        public const float NODE_SUB_D = 48f;     // 小类圆直径（v0.19.17 56→48：大类扇区两端让出叶链缓冲后，
                                                //  最密系精神系11小类 subSector≈3.9°，R=850 弧距57.6px≥48×1.15=55.2 ✓）
        public const float RADIUS_CAT = 400f;    // 大类环半径
        public const float RADIUS_SUB = 850f;    // 小类环半径（最密系11小类扇区4.4°→弧距65px≥64 ✓）
        // v0.19.17 叶子径向链（替代 v0.19.15 的 RADIUS_LEAF_*=4000/4600/5200 全树3大环——
        // 那套把叶子甩到小类外3000+px，展开后一条长线横跨屏幕，不符合参考图"叶子贴辐条短距外延"）
        public const float LEAF_CHAIN_PAD = 40f; // 链起点距小类节点的额外间隙
        public const float RADIAL_STEP = 56f;    // 链内相邻叶半径步距（高品直径46×1.15=52.9 取56）
        public const float LEAF_ANGLE_STEP = 2.5f;// 链内相邻叶角距（度；主要防重叠靠径向步距，角度仅微错成扇面）
        // 叶直径按品质分级：SS/SSS 大、A/S 中、F~C 小（品质越高视觉权重越大）
        public const float LEAF_D_HIGH = 46f;
        public const float LEAF_D_MID = 42f;
        public const float LEAF_D_LOW = 38f;
        public const float MIN_CAT_SECTOR = 24f; // 系最小扇区角（度，小系兜底）
        public const float MIN_SUB_SECTOR = 6.5f;// 小类最小扇区角（度，用于大类扇区保底计算）
        public const float CAT_EDGE_PAD = 2f;    // 大类扇区两端叶链缓冲（度，v0.19.17：叶链不贴大类边界，
                                                //  相邻大类边界两侧链尾叶最小角差=2×2°=4°≥链尾弧距需求≈2°；
                                                //  取2°而非3°：自然系/时空系小类弧距才够（48×1.15=55.2px））
        private const float ARC_SAFETY = 1.15f;  // 弧距安全系数（≥直径×1.15 不重叠）

        // 大类固定展示顺序（Categories 字典无序，用顺序数组保证树 UI 稳定；大众认知 9 系）。
        // 数据中出现的新大类自动追加到末尾，无需改此数组——拓展异能/体系不用重新设计树。
        private static readonly string[] CategoryOrder =
        {
            "元素系", "自然系", "精神系", "时空系", "光明系", "黑暗系",
            "生命系", "死亡系", "强化系"
        };

        /// <summary>构建全量异能树布局（根居中，大类/小类按异能数占比分配扇区向四周辐射）</summary>
        public static ProtoTreeLayout Build()
        {
            var layout = new ProtoTreeLayout();
            var registry = PrototypeRegistry.Categories;

            // ---- 阶段0：大类动态枚举（固定顺序优先，新系自动追加）----
            var cats = new List<string>();
            foreach (string c in CategoryOrder)
                if (registry.ContainsKey(c)) cats.Add(c);
            foreach (var kv in registry)
                if (!cats.Contains(kv.Key)) cats.Add(kv.Key);

            // ---- 阶段1：按系/小类异能数计算扇区角（带最小保底并归一化）----
            int totalLeaves = 0;
            foreach (string cat in cats)
            {
                if (!registry.TryGetValue(cat, out var subs)) continue;
                foreach (var kv in subs)
                    if (kv.Value != null) totalLeaves += kv.Value.Count;
            }
            if (totalLeaves == 0)
            {
                layout.Bounds = new Rect(-100f, -100f, 200f, 200f);
                return layout;
            }

            // 系扇区：raw = max(叶占比×360, 小类数×MIN_SUB_SECTOR+6°, MIN_CAT_SECTOR)，再归一化 Σ=360
            // （小类数保底保证系内小类铺得开；叶占比保证异能多的系扇区大）
            var catLeavesList = new List<int>();      // 每系异能数
            var catSubCountList = new List<int>();    // 每系小类数
            var catSectorRaw = new List<float>();     // 每系保底后原始扇区
            foreach (string cat in cats)
            {
                if (!registry.TryGetValue(cat, out var subs)) continue;
                int leaves = 0, subCount = 0;
                foreach (var kv in subs)
                {
                    if (kv.Value == null || kv.Value.Count == 0) continue;
                    subCount++;
                    leaves += kv.Value.Count;
                }
                if (leaves == 0) continue;
                catLeavesList.Add(leaves);
                catSubCountList.Add(subCount);
                catSectorRaw.Add(Mathf.Max(Mathf.Max(
                    leaves / (float)totalLeaves * 360f,
                    subCount * MIN_SUB_SECTOR + 6f), MIN_CAT_SECTOR));
            }
            float rawSum = 0f;
            foreach (float s in catSectorRaw) rawSum += s;
            var catSector = new List<float>();        // 归一化后的系扇区
            foreach (float s in catSectorRaw) catSector.Add(s / rawSum * 360f);

            // 根节点（异能）：世界中心（圆形）
            var root = new ProtoTreeNode
            {
                Id = ROOT_ID, Name = "异能", Level = -1, ParentId = null,
                X = -NODE_ROOT_D / 2f, Y = -NODE_ROOT_D / 2f,
                W = NODE_ROOT_D, H = NODE_ROOT_D,
                Color = new Color(0.95f, 0.85f, 0.55f, 1f)
            };
            layout.Nodes.Add(root);

            // 大类环半径自适应：最小相邻系中心角距 → 弧距 ≥ 直径×1.15
            float minCatGap = 360f;                   // 最小相邻系中心角距（度）
            for (int i = 0; i < catSector.Count; i++)
            {
                float gap = (catSector[i] + catSector[(i + 1) % catSector.Count]) / 2f;
                minCatGap = Mathf.Min(minCatGap, gap);
            }
            float catR = Mathf.Max(RADIUS_CAT,
                NODE_CAT_D * ARC_SAFETY / (minCatGap * Mathf.Deg2Rad));

            float ang = 0f;   // 当前扇区起始角（度，0=正右，顺时针；绘制时 -90° 偏移使顶部为起点）
            for (int ci = 0; ci < cats.Count; ci++)
            {
                string cat = cats[ci];
                if (!registry.TryGetValue(cat, out var subs)) continue;
                int catLeaves = catLeavesList[ci];
                if (catLeaves == 0) continue;
                float catSec = catSector[ci];
                float catCenter = ang + catSec / 2f;

                // 大类节点（环 catR）
                var catNode = new ProtoTreeNode
                {
                    Id = cat, Name = cat, Level = 0, ParentId = ROOT_ID,
                    W = NODE_CAT_D, H = NODE_CAT_D,
                    Color = GetCategoryColor(cat)
                };
                SetRadialPos(catNode, catCenter, catR);
                layout.Nodes.Add(catNode);

                // 系内小类：等角距均分大类扇区（v0.19.15，不再按叶数加权——杜绝同系内疏密不均）
                // v0.19.17 大类扇区两端各留 CAT_EDGE_PAD 叶链缓冲：小类中心角整体内移，
                // 叶链链尾不逼近大类边界，相邻大类边界两侧链尾最小角差=2×CAT_EDGE_PAD≥弧距需求
                var subNames = new List<string>();
                foreach (var kv in subs)
                {
                    if (kv.Value == null || kv.Value.Count == 0) continue;
                    subNames.Add(kv.Key);
                }
                float subSector = (catSec - CAT_EDGE_PAD * 2f) / Mathf.Max(1, subNames.Count);
                float subAng = ang + CAT_EDGE_PAD;
                for (int si = 0; si < subNames.Count; si++)
                {
                    string subName = subNames[si];
                    float subCenter = subAng + subSector / 2f;

                    // 小类节点（环 RADIUS_SUB，圆形）
                    var subNode = new ProtoTreeNode
                    {
                        Id = cat + "::" + subName, Name = subName, Level = 1, ParentId = cat,
                        W = NODE_SUB_D, H = NODE_SUB_D,
                        Color = GetCategoryColor(cat) * new Color(0.85f, 0.85f, 0.85f, 1f)
                    };
                    SetRadialPos(subNode, subCenter, RADIUS_SUB);
                    layout.Nodes.Add(subNode);

                    // 异能叶：所属小类起点的径向链（v0.19.17 替代 v0.19.15 全树3大环）
                    // 品质降序排列（高品贴内贴近传承源头、低品在外），链内相邻叶半径差=RADIAL_STEP，
                    // 角度微错开成扇面（leafStep=min(LEAF_ANGLE_STEP, subSector/叶数)），串间不重叠。
                    var subList = subs[subName];
                    var gHigh = new List<PrototypeDef>();
                    var gMid = new List<PrototypeDef>();
                    var gLow = new List<PrototypeDef>();
                    foreach (var def in subList)
                    {
                        if (def == null) continue;
                        int t = PrototypeEvolution.GetTier(def);
                        if (t >= 7) gHigh.Add(def);
                        else if (t >= 4) gMid.Add(def);
                        else gLow.Add(def);
                    }
                    var ordered = new List<PrototypeDef>();
                    ordered.AddRange(gHigh);
                    ordered.AddRange(gMid);
                    ordered.AddRange(gLow);
                    int k = ordered.Count;
                    // 链内角距：≤2.5°，且 ≤ subSector/(k+1)（多留一个 step 边距，链首/链尾不逼近小类扇区边缘）
                    float leafStep = Mathf.Min(LEAF_ANGLE_STEP, subSector / Mathf.Max(1, k + 1));
                    for (int i = 0; i < k; i++)
                    {
                        var def = ordered[i];
                        int t = PrototypeEvolution.GetTier(def);
                        float d = t >= 7 ? LEAF_D_HIGH : (t >= 4 ? LEAF_D_MID : LEAF_D_LOW);
                        float offset = (i - (k - 1) / 2f) * leafStep;
                        float leafAngle = subCenter + offset;
                        float leafRadius = RADIUS_SUB + LEAF_CHAIN_PAD + (i + 1) * RADIAL_STEP;
                        var leaf = new ProtoTreeNode
                        {
                            Id = def.Id, Name = def.NameZh, Level = 2,
                            ParentId = cat + "::" + subName,
                            W = d, H = d,
                            Def = def, Color = GetCategoryColor(cat)
                        };
                        SetRadialPos(leaf, leafAngle, leafRadius);
                        layout.Nodes.Add(leaf);

                        // 品质进化链（低品叶 → 更高品质上级；SSS/家族顶点无上级）
                        var evo = PrototypeEvolution.GetEvolveTarget(def);
                        if (evo != null)
                            layout.QualityEdges.Add(new QualityEdge { LowId = def.Id, HighId = evo.Id });
                    }
                    subAng += subSector;
                }
                ang += catSec;
            }

            // ---- 阶段2：可见层包围盒（根+系+小类；异能叶折叠不计入，避免全树 Fit 过小）----
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var n in layout.Nodes)
            {
                if (n.Level == 2) continue;   // 异能叶默认折叠
                x0 = Mathf.Min(x0, n.X); y0 = Mathf.Min(y0, n.Y);
                x1 = Mathf.Max(x1, n.X + n.W); y1 = Mathf.Max(y1, n.Y + n.H);
            }
            if (layout.Nodes.Count == 0) { layout.Bounds = new Rect(0, 0, 1, 1); return layout; }
            layout.Bounds = Rect.MinMaxRect(x0 - 40f, y0 - 40f, x1 + 40f, y1 + 40f);
            return layout;
        }

        /// <summary>把节点中心放到极坐标 (radius, angleDeg) 处（左上角对齐）</summary>
        private static void SetRadialPos(ProtoTreeNode node, float angleDeg, float radius)
        {
            float rad = (angleDeg - 90f) * Mathf.Deg2Rad;   // -90°：顶部为 0 起点，顺时针绕
            float cx = Mathf.Cos(rad) * radius;
            float cy = Mathf.Sin(rad) * radius;
            node.X = cx - node.W / 2f;
            node.Y = cy - node.H / 2f;
        }

        /// <summary>根节点 ID（异能）</summary>
        public const string ROOT_ID = "__root__";

        /// <summary>品质色（v0.19.1，网文异能等级制：F灰 → SSS金橙；渲染层异能叶外圈/进化链用）</summary>
        public static Color GetQualityColor(int tier)
        {
            switch (tier)
            {
                case 1: return new Color(0.55f, 0.56f, 0.60f, 1f);   // F 灰
                case 2: return new Color(0.45f, 0.72f, 0.45f, 1f);   // E 绿
                case 3: return new Color(0.48f, 0.62f, 0.95f, 1f);   // D 蓝
                case 4: return new Color(0.45f, 0.80f, 0.80f, 1f);   // C 青
                case 5: return new Color(0.60f, 0.50f, 0.90f, 1f);   // B 紫
                case 6: return new Color(0.78f, 0.45f, 0.88f, 1f);   // A 亮紫
                case 7: return new Color(0.90f, 0.62f, 0.30f, 1f);   // S 橙金
                case 8: return new Color(0.95f, 0.80f, 0.30f, 1f);   // SS 金
                case 9: return new Color(1.00f, 0.88f, 0.42f, 1f);   // SSS 金橙
                default: return new Color(0.60f, 0.60f, 0.60f, 1f);
            }
        }

        /// <summary>大类主色（元素系红蓝系 / 自然系绿 / 精神系紫 / 时空系青 / 光明金 / 黑暗紫灰 / 生命翠 / 死亡灰绿 / 强化赤橙 / 星辰蓝紫 / 血族血红 / 神话金橙）</summary>
        public static Color GetCategoryColor(string category)
        {
            switch (category)
            {
                case "元素系": return new Color(0.85f, 0.55f, 0.35f, 1f);   // 火橙
                case "自然系": return new Color(0.45f, 0.75f, 0.45f, 1f);   // 草木绿
                case "精神系": return new Color(0.65f, 0.50f, 0.85f, 1f);   // 幻紫
                case "时空系": return new Color(0.45f, 0.80f, 0.80f, 1f);   // 时空青
                case "光明系": return new Color(0.95f, 0.82f, 0.45f, 1f);   // 圣光金
                case "黑暗系": return new Color(0.55f, 0.45f, 0.70f, 1f);   // 暗影紫
                case "生命系": return new Color(0.40f, 0.85f, 0.55f, 1f);   // 生命翠
                case "死亡系": return new Color(0.60f, 0.60f, 0.55f, 1f);   // 死寂灰绿
                case "强化系": return new Color(0.88f, 0.55f, 0.40f, 1f);   // 强化赤橙（力量感）
                case "星辰系": return new Color(0.50f, 0.55f, 0.90f, 1f);   // 星辰蓝紫
                case "血族系": return new Color(0.80f, 0.25f, 0.30f, 1f);   // 血族血红
                case "神话系": return new Color(0.90f, 0.72f, 0.35f, 1f);   // 神话金橙
                default: return new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }
    }
}
