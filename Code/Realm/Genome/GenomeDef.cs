// ============================================================
// 文件: GenomeDef.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P1 数据层配置表——染色体规格 / 解锁节奏 / 编码上限
// 数值来源（硬约束：不发明新数值，全部出自设计文档/原版/现有体系）：
//   - 染色体类型 b/m/s：原版 chromosome_big/medium/small（§4.2 P0 定稿，只引用原版类型）
//   - 位点列数 6：原版惯例（§4.1，Chromosome.cs _columns = amount_loci / 6）
//   - 解锁节奏：§4.4 设计表（1-4阶=1条 / 5-8阶=2条 / 9-12阶=3条 / 13阶=4条本源染色体）
//   - 编码上限：§5.2（位点 ≤64、条数 ≤8）
//   - 表达层级 0-5：现有 GeneExpressionSystem（沉默/低/中/高/过/突变）
// 填点阈值：按 §4.3 属 P2（GenomeCultivationProgress 接入，按境界配置表，不做固定 2.2）
// ============================================================

using System;
using System.Collections.Generic;

namespace Code.Realm.Genome
{
    /// <summary>基因组配置表（P1 数据层，静态只读）</summary>
    public static class GenomeDef
    {
        /// <summary>染色体类型代码 → 原版染色体资产名（§4.2：只用原版类型，不造新资产）</summary>
        public static readonly Dictionary<string, string> VanillaChromosomeType = new Dictionary<string, string>
        {
            { "b", "chromosome_big" },
            { "m", "chromosome_medium" },
            { "s", "chromosome_small" },
        };

        /// <summary>反向：原版资产名 → 类型代码</summary>
        public static string CodeOfVanillaType(string vanillaType)
        {
            foreach (var kv in VanillaChromosomeType)
                if (kv.Value == vanillaType) return kv.Key;
            return "m";
        }

        /// <summary>
        /// 境界 → 可解锁染色体条数（§4.4 设计表，与 13 阶对应）：
        ///   1-4 阶（单基因表达阶段）→ 1 条
        ///   5-8 阶（染色体整体表达）→ 2 条
        ///   9-12 阶（多染色体协同）→ 3 条
        ///   13 阶（极致表达）→ 4 条（本源染色体）
        /// </summary>
        public static int GetUnlockCountForTier(int tier)
        {
            if (tier >= 13) return 4;
            if (tier >= 9) return 3;
            if (tier >= 5) return 2;
            return 1;
        }

        /// <summary>编码健壮性上限（§5.2）：单条染色体位点数上限</summary>
        public const int MaxLociPerChromosome = 64;

        /// <summary>编码健壮性上限（§5.2）：染色体条数上限</summary>
        public const int MaxChromosomeCount = 8;

        /// <summary>位点列数（§4.1：6 列是原版惯例）</summary>
        public const int LociColumns = 6;

        /// <summary>表达层级范围（现有 GeneExpressionSystem.ExpressionLevel：0 沉默 ~ 5 突变）</summary>
        public const int MinExpressionLevel = 0;
        public const int MaxExpressionLevel = 5;

        /// <summary>元数据键（§4.1）：解锁条数 / 填点进度 / 位阶种子，格式 "{unlocked};{progress};{seed}"</summary>
        public const string MetaSeparator = ";";
    }
}
