// ============================================================
// 文件: GenomeModel.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P1 数据层运行时模型——LocusSlot / GenomeChromosome 纯数据结构
//       + 转原版 Chromosome（P0 定稿 A 路：代理基因资产 + 原版渲染）
// 说明:
//   - LocusSlot：基因 id + 表达层级（0-5，复用 GeneExpressionSystem.ExpressionLevel）
//   - 空位点：GeneId 为空串
//   - 扩增/虚空位点：由染色体级索引列表标记（编码规约 §4.1），位点本身 GeneId 为空
//   - ToVanillaChromosome：pNew=true 初始化位点（v0.21.50 已验证 pNew 必须 true），
//     反射写 _loci_amplifiers/_loci_empty，已填位点映射原版库代理基因资产
// ============================================================

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Code.Realm.Genome
{
    /// <summary>单个基因位点（§4.1）</summary>
    public struct LocusSlot
    {
        /// <summary>基因 id（空串 = 空位点）</summary>
        public string GeneId;
        /// <summary>表达层级 0-5（§4.1，复用 GeneExpressionSystem.ExpressionLevel）</summary>
        public int ExprLevel;

        public bool IsEmpty => string.IsNullOrEmpty(GeneId);

        public LocusSlot(string geneId, int exprLevel)
        {
            GeneId = geneId ?? "";
            ExprLevel = Math.Max(GenomeDef.MinExpressionLevel, Math.Min(GenomeDef.MaxExpressionLevel, exprLevel));
        }

        public static LocusSlot Empty() => new LocusSlot("", 0);
    }

    /// <summary>一条自定义染色体（§4.1）</summary>
    public class GenomeChromosome
    {
        /// <summary>类型代码 b/m/s（对应原版 big/medium/small）</summary>
        public string TypeCode;

        /// <summary>位点序列（长度 = 原版染色体资产位点数）</summary>
        public List<LocusSlot> Loci = new List<LocusSlot>();

        /// <summary>扩增位点索引（§4.1 superIdx）</summary>
        public List<int> AmplifierIndices = new List<int>();

        /// <summary>虚空位点索引（§4.1 voidIdx）</summary>
        public List<int> VoidIndices = new List<int>();

        /// <summary>原版染色体资产名（由 TypeCode 换算，§4.2）</summary>
        public string VanillaType
        {
            get
            {
                if (GenomeDef.VanillaChromosomeType.TryGetValue(TypeCode, out var t)) return t;
                return "chromosome_medium";
            }
        }

        public GenomeChromosome(string typeCode)
        {
            TypeCode = GenomeDef.VanillaChromosomeType.ContainsKey(typeCode) ? typeCode : "m";
        }

        public bool IsAmplifier(int index) => AmplifierIndices.Contains(index);
        public bool IsVoid(int index) => VoidIndices.Contains(index);

        /// <summary>
        /// 转为原版 Chromosome（P0 定稿 A 路渲染转换）。
        /// 空位点用原版 empty 基因 + void 索引；已填位点按 id 映射原版库代理基因。
        /// </summary>
        public Chromosome ToVanillaChromosome()
        {
            var chrom = new Chromosome(VanillaType, true);   // pNew=true：初始化位点（v0.21.50 经验）

            // 覆盖为固定扩增/虚空布局（readonly 字段反射可设）
            SetField(chrom, "_loci_amplifiers", new List<int>(AmplifierIndices));
            SetField(chrom, "_loci_empty", new List<int>(VoidIndices));

            // 位点填基因：空位点保持 empty（原版渲染空位点），已填位点映射代理基因资产
            int lociCount = Math.Min(Loci.Count, chrom.genes.Count);
            for (int i = 0; i < lociCount; i++)
            {
                var slot = Loci[i];
                if (slot.IsEmpty) continue;
                var proxy = GenomeProxyResolver.GetProxyGene(slot.GeneId);
                if (proxy != null)
                {
                    chrom.genes[i] = proxy;
                }
            }
            return chrom;
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var f = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null)
            {
                Code.Core.DSDebug.Warning("[Genome] 未找到原版字段: " + fieldName);
                return;
            }
            try { f.SetValue(obj, value); }
            catch (Exception e) { Code.Core.DSDebug.Warning("[Genome] 设置字段失败 " + fieldName + ": " + e.Message); }
        }
    }

    /// <summary>
    /// 基因 id → 原版库代理基因资产（P0 定稿 A 路：不伪造 GeneAsset，用原版库内资产代理渲染）。
    /// 模组基因未注册原版资产时，按库内同 id 查找；找不到用库内第一个非空基因兜底。
    /// </summary>
    public static class GenomeProxyResolver
    {
        private static GeneAsset _fallbackProxy;
        private static bool _resolved = false;

        public static GeneAsset GetProxyGene(string geneId)
        {
            if (string.IsNullOrEmpty(geneId)) return null;
            try
            {
                var lib = AssetManager.gene_library;
                if (lib == null || lib.list == null) return null;

                // 1) 同 id 直查
                var direct = lib.get(geneId);
                if (direct != null && !direct.is_empty) return direct;

                // 2) 兜底：库内第一个非空基因（缓存）
                if (_fallbackProxy == null && !_resolved)
                {
                    _resolved = true;
                    foreach (var g in lib.list)
                    {
                        if (g != null && !g.is_empty) { _fallbackProxy = g; break; }
                    }
                }
                return _fallbackProxy;
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[Genome] 代理基因解析失败 " + geneId + ": " + e.Message);
                return null;
            }
        }
    }
}
