// ============================================================
// 文件: GenomeMutation.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P2 业务层——位点突变桥接（§8 文件规划）
// 设计：
//   - 现有 GeneMutationSystem 的激活/表达升级调用保持不变（UnlockElement/SetExpressionLevel
//     已由 GenomeEffects 双写/待挂队列兜底），突变激活的基因自动进入待挂队列，由修炼填点入位点
//   - 本文件提供位点级辅助：突变激活后立即尝试入位点（有空位时直接挂载，位点满则留待填点），
//     以及位点表达突变的读写辅助（P4 演出期细化）
// ============================================================

using Code.Data;

namespace Code.Realm.Genome
{
    /// <summary>位点突变桥接（P2）</summary>
    public static class GenomeMutation
    {
        /// <summary>
        /// 突变激活新基因后调用：位点有空立即挂载（软分桶），位点满则留待挂队列由填点驱动。
        /// 挂载后立即同步表达层级（迁移/突变 SetExpressionLevel 写入的旧键值）到位点。
        /// </summary>
        public static void SyncAfterMutation(Actor actor)
        {
            if (actor == null) return;

            // 待挂基因中，位点有空则立即挂载（不消耗填点进度——突变基因优先进位）
            var pending = GenomeEffects.GetPendingGeneIds(actor);
            foreach (var geneId in pending)
            {
                if (!GenomeEffects.HasFreeLocus(actor)) break;
                if (GenomeEffects.TryMountGene(actor, geneId))
                {
                    // 同步表达层级到位点
                    int level = CultivationData.GetGeneExpressionLevel(actor, geneId);
                    if (level > 0) GenomeEffects.SetExpressionLevel(actor, geneId, level);
                }
            }
        }
    }
}
