// ============================================================
// 文件: GenomeMigration.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P1 旧格式 → 新格式迁移（§5.1，幂等 + 版本标记 + 旧键保留）
// 旧数据（v0.22.1 修正——业务源与图谱源合并，见下）：
//   - ds_elem_unlocked（K_ELEMENTS_UNLOCKED）：【业务真实源】已激活基因 id（觉醒/突变/登神/图谱窗口写）
//   - ds_gene_unlocked（K_GENE_UNLOCKED）：【图谱兜底源】基因图谱节点解锁记录（年度图谱检查写，与元素源独立不同步）
//   - gene_expr_{id}（K_GENE_EXPR_PREFIX）：每个基因的表达层级（0-5，两源共用，按 id 读写）
// 迁移逻辑（§5.1）：
//   1. 有旧数据：已解锁基因按序分配到第一条染色体空位点，表达层级原值搬入 LocusSlot.ExprLevel
//   2. 无旧数据：默认生成第一条染色体（chromosome_medium，12 位点）
//   3. 迁移后写 gc_genome_version，旧键保留（只读兼容回滚，不删）
// v0.22.1 修正：旧档基因来源由「仅 ds_gene_unlocked」改为「ds_elem_unlocked（业务）+ ds_gene_unlocked（图谱）合并去重」，
//   避免业务激活基因未被图谱记录时迁移出空基因组；迁移版本升至 2，version=1 的档触发重迁（P2 前无用户填点数据，覆盖安全）
// 时机：单位首次访问时惰性迁移（§5.1：绝不在加载全图时全量迁移）
// ============================================================

using System.Collections.Generic;
using Code.Data;

namespace Code.Realm.Genome
{
    /// <summary>旧档迁移（幂等：version >= 当前版本直接返回）</summary>
    public static class GenomeMigration
    {
        public static void Migrate(Actor actor)
        {
            if (actor == null) return;
            var data = ActorDataAccessor.GetData(actor);
            if (data == null || data.custom_data_string == null) return;

            // 幂等：已迁移到当前版本则跳过（v0.22.1 升至版本 2，旧 version=1 档触发重迁）
            string existing = data.custom_data_string[GenomeData.GC_GENOME_VERSION];
            if (!string.IsNullOrEmpty(existing) && existing == GenomeData.CURRENT_VERSION) return;

            // 默认第一条染色体（§5.1：无旧数据默认生成第一条；默认 medium，P0 已验证 12 位点）
            var chromosomes = new List<GenomeChromosome>();
            var chrom = new GenomeChromosome("m");
            int lociCount = 12;   // chromosome_medium 位点数（P0 实测）

            // v0.22.1：两源合并去重——业务激活源优先，图谱记录源兜底
            List<string> unlocked = MergeUnlockedGenes(actor);
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
                                      " 激活基因=" + (unlocked != null ? unlocked.Count : 0) +
                                      " 写入版本=" + GenomeData.CURRENT_VERSION);
        }

        /// <summary>
        /// 合并业务激活源与图谱解锁源（去重保序）：
        /// 业务真实数据在 ds_elem_unlocked（GetUnlockedElements），图谱记录在 ds_gene_unlocked（GetUnlockedGenes），
        /// 两套独立不同步，任一有值都应收录，避免基因丢失。
        /// </summary>
        private static List<string> MergeUnlockedGenes(Actor actor)
        {
            var merged = new List<string>();
            var seen = new HashSet<string>();

            List<string> biz = CultivationData.GetUnlockedElements(actor);
            List<string> graph = CultivationData.GetUnlockedGenes(actor);

            if (biz != null)
                foreach (var id in biz)
                    if (!string.IsNullOrEmpty(id) && seen.Add(id)) merged.Add(id);

            if (graph != null)
                foreach (var id in graph)
                    if (!string.IsNullOrEmpty(id) && seen.Add(id)) merged.Add(id);

            return merged;
        }
    }
}
