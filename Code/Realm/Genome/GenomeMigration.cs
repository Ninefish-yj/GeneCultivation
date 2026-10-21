// ============================================================
// 文件: GenomeMigration.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P1 旧格式 → 新格式迁移（§5.1，幂等 + 版本标记 + 旧键保留）
// 旧数据：
//   - ds_gene_unlocked（K_GENE_UNLOCKED）：已解锁基因 id 列表（逗号分隔）
//   - gene_expr_{id}（K_GENE_EXPR_PREFIX）：每个基因的表达层级（0-5）
// 迁移逻辑（§5.1）：
//   1. 有旧数据：已解锁基因按序分配到第一条染色体空位点，表达层级原值搬入 LocusSlot.ExprLevel
//   2. 无旧数据：默认生成第一条染色体（chromosome_medium，12 位点）
//   3. 迁移后写 gc_genome_version=1，旧键保留（只读兼容回滚，不删）
// 时机：单位首次访问时惰性迁移（§5.1：绝不在加载全图时全量迁移）
// ============================================================

using System.Collections.Generic;
using Code.Data;

namespace Code.Realm.Genome
{
    /// <summary>旧档迁移（幂等：version 已存在直接返回）</summary>
    public static class GenomeMigration
    {
        public static void Migrate(Actor actor)
        {
            if (actor == null) return;
            var data = ActorDataAccessor.GetData(actor);
            if (data == null || data.custom_data_string == null) return;

            // 幂等：已迁移则跳过
            if (!string.IsNullOrEmpty(data.custom_data_string[GenomeData.GC_GENOME_VERSION])) return;

            // 默认第一条染色体（§5.1：无旧数据默认生成第一条；默认 medium，P0 已验证 12 位点）
            var chromosomes = new List<GenomeChromosome>();
            var chrom = new GenomeChromosome("m");
            int lociCount = 12;   // chromosome_medium 位点数（P0 实测）

            // 有旧数据：已解锁基因 → 分配到第一条染色体空位点，表达层级原值搬入
            List<string> unlocked = CultivationData.GetUnlockedGenes(actor);
            if (unlocked != null && unlocked.Count > 0)
            {
                int fillIndex = 0;
                for (int i = 0; i < lociCount && fillIndex < unlocked.Count; i++)
                {
                    string geneId = unlocked[fillIndex];
                    if (string.IsNullOrEmpty(geneId)) { fillIndex++; continue; }
                    int level = CultivationData.GetGeneExpressionLevel(actor, geneId);
                    chrom.Loci.Add(new LocusSlot(geneId, level));
                    fillIndex++;
                }
                // 剩余空位点补齐
                while (chrom.Loci.Count < lociCount) chrom.Loci.Add(LocusSlot.Empty());
            }
            else
            {
                // 无旧数据：全空位点
                for (int i = 0; i < lociCount; i++) chrom.Loci.Add(LocusSlot.Empty());
            }

            chromosomes.Add(chrom);

            // 写新格式 + 版本标记
            GenomeData.SaveChromosomes(actor, chromosomes);
            GenomeData.SaveMeta(actor, GenomeCodec.GenomeMeta.Default());
            data.custom_data_string[GenomeData.GC_GENOME_VERSION] = GenomeData.CURRENT_VERSION;

            // 旧键保留不删（§5.1：只读兼容回滚）
            Code.Core.DSDebug.Verbose("[Genome] 旧档迁移完成 " + actor.getName() +
                                      " 解锁基因=" + unlocked.Count + " 写入版本=" + GenomeData.CURRENT_VERSION);
        }
    }
}
