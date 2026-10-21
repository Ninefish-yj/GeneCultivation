// ============================================================
// 文件: GenomeCultivationProgress.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P2 业务层——修炼填点（§4.3）
// 机制（P2 对照表已确认）：
//   - 输入：年度修炼产出（复用 AnnualTickManager 的 finalGain，不另造产出）
//   - 进度：存 actor.data 浮点键 gc_fill_progress（§4.3 定稿）
//   - 阈值：填满 1 格 = 当前境界年修炼产出 × 基准年数 × (1 + 0.2 × 已填格数)
//     ——"一个更比一个难"（用户确认）：同一条染色体内逐格递增难度
//   - 挂载：取下一个待挂基因（激活未入位点）→ 软分桶入位点（GenomeEffects.TryMountGene）
//   - 开新染色体：当前染色体位点全满 + 境界允许（§4.4 解锁节奏）→ 默认 medium 新染色体
// 校准注记：基准年数 3、递增率 0.2 为默认参数（代码常量，标注可校准，不写死进规约）
// ============================================================

using Code.Data;

namespace Code.Realm.Genome
{
    /// <summary>修炼填点进度（§4.3）</summary>
    public static class GenomeCultivationProgress
    {
        public const string GC_FILL_PROGRESS = "gc_fill_progress";

        /// <summary>基准年数（填满 1 格的基础修炼年数，默认 3，可校准）</summary>
        public const float BaseYearsPerLocus = 3f;

        /// <summary>递增率（每填 1 格难度 +20%，"一个更比一个难"，可校准）</summary>
        public const float DifficultyIncreasePerLocus = 0.2f;

        /// <summary>获取当前填点进度（0~需求值，未达需求不折算）</summary>
        public static float GetFillProgress(Actor actor)
        {
            if (actor == null) return 0f;
            var data = ActorDataAccessor.GetData(actor);
            if (data == null || data.custom_data_float == null) return 0f;
            data.custom_data_float.TryGetValue(GC_FILL_PROGRESS, out float v);
            return v;
        }

        /// <summary>
        /// 填满 1 格所需进度（§4.3 + P2 对照表"一个更比一个难"）：
        /// 需求 = 当前境界年修炼产出 × 基准年数 × (1 + 递增率 × 该条染色体已填格数)
        /// 年修炼产出复用现有体系（1-3阶15/年、4-6阶10/年、7-9阶7/年、10-12阶5/年，AnnualTickManager）
        /// </summary>
        public static float GetFillRequirement(Actor actor)
        {
            if (actor == null) return 1f;
            int tier = CultivationData.GetRealmTier(actor);
            float baseGain = 15f;
            if (tier >= 4) baseGain = 10f;
            if (tier >= 7) baseGain = 7f;
            if (tier >= 10) baseGain = 5f;

            int filled = GetFilledLocusCount(actor);
            return baseGain * BaseYearsPerLocus * (1f + DifficultyIncreasePerLocus * filled);
        }

        /// <summary>年度修炼入账：进度 += amount，达标则挂载一个待挂基因并清零（可连续）</summary>
        public static void AddProgress(Actor actor, float amount)
        {
            if (actor == null || amount <= 0f) return;
            if (!CultivationData.IsAscended(actor)) return;

            var pending = GenomeEffects.GetPendingGeneIds(actor);
            if (pending.Count == 0) return;   // 无待挂基因，进度不积累

            float progress = GetFillProgress(actor) + amount;
            int guard = 0;
            while (guard++ < 32)
            {
                float req = GetFillRequirement(actor);
                if (progress < req) break;
                progress -= req;
                if (!MountNextPending(actor)) break;   // 无空位且无法开新染色体 → 停止
            }
            SaveProgress(actor, progress);
        }

        /// <summary>挂载下一个待挂基因；位点满且境界允许则开新染色体后继续</summary>
        private static bool MountNextPending(Actor actor)
        {
            var pending = GenomeEffects.GetPendingGeneIds(actor);
            if (pending.Count == 0) return false;

            string geneId = pending[0];
            if (GenomeEffects.TryMountGene(actor, geneId)) return true;

            // 位点满：境界允许则开新染色体（§4.4 解锁节奏）
            if (!GenomeEffects.HasFreeLocus(actor))
            {
                int current = GenomeEffects.GetChromosomeCount(actor);
                int tier = CultivationData.GetRealmTier(actor);
                if (current >= GenomeDef.MaxChromosomeCount) return false;
                if (current >= GenomeDef.GetUnlockCountForTier(tier)) return false;   // 境界未允许
                if (!AddNewChromosome(actor)) return false;
                return GenomeEffects.TryMountGene(actor, geneId);
            }
            return false;
        }

        private static bool AddNewChromosome(Actor actor)
        {
            var chromosomes = GenomeData.GetChromosomes(actor);
            var next = new GenomeChromosome("m");   // 新染色体默认 medium（P0 已验证 12 位点）
            for (int i = 0; i < 12; i++) next.Loci.Add(LocusSlot.Empty());
            chromosomes.Add(next);
            GenomeData.SaveChromosomes(actor, chromosomes);
            return true;
        }

        private static void SaveProgress(Actor actor, float value)
        {
            var data = ActorDataAccessor.GetData(actor);
            if (data == null || data.custom_data_float == null) return;
            data.custom_data_float[GC_FILL_PROGRESS] = value;
        }

        /// <summary>当前染色体已填格数（第一条开始计，用于难度递增）</summary>
        public static int GetFilledLocusCount(Actor actor)
        {
            int filled = 0;
            var chromosomes = GenomeData.GetChromosomes(actor);
            if (chromosomes == null) return 0;
            foreach (var c in chromosomes)
            {
                if (c == null) continue;
                foreach (var slot in c.Loci)
                    if (!slot.IsEmpty) filled++;
            }
            return filled;
        }
    }
}
