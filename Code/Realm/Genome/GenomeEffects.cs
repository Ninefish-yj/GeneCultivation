// ============================================================
// 文件: GenomeEffects.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P2 业务层聚合读源——表达层级/激活基因/突破统计的 Genome 统一入口
// 设计（P2 对照表已确认）：
//   - 统计口径 = 位点基因 ∪ 待挂基因（激活未入位点）= 全部激活基因
//     → 突破判定/效果聚合语义与迁移前完全一致（关卡：同单位同境界同结果）
//   - 表达层级：位点优先（LocusSlot.ExprLevel，迁移时搬入），回退旧键（待挂基因）
//   - 写入：双写（位点更新 + 旧键），旧 API 调用方兼容垫片（P4 退役前不删旧键）
//   - 挂载：软分桶（位点索引 → 目标 Group：i*8/12；同组桶优先，兜底任意空位点）
// ============================================================

using System.Collections.Generic;
using Code.Data;

namespace Code.Realm.Genome
{
    /// <summary>基因组业务读源（P2 桥接统一入口）</summary>
    public static class GenomeEffects
    {
        /// <summary>
        /// 有效基因集（保序）：位点基因在前（按位点序），待挂基因（激活未入位点）按激活序补充。
        /// 待挂基因 = ds_elem_unlocked ∪ ds_gene_unlocked 合并集 − 位点集。
        /// </summary>
        public static List<string> GetEffectiveGeneIds(Actor actor)
        {
            var result = new List<string>();
            if (actor == null) return result;
            var seen = new HashSet<string>();

            var chromosomes = GenomeData.GetChromosomes(actor);
            foreach (var c in chromosomes)
            {
                if (c == null) continue;
                foreach (var slot in c.Loci)
                {
                    if (slot.IsEmpty || !seen.Add(slot.GeneId)) continue;
                    result.Add(slot.GeneId);
                }
            }

            // 待挂基因：激活列表 − 位点集
            var activated = MergeActivated(actor);
            foreach (var id in activated)
            {
                if (string.IsNullOrEmpty(id) || !seen.Add(id)) continue;
                result.Add(id);
            }
            return result;
        }

        /// <summary>表达层级：位点优先，回退旧键（待挂基因）</summary>
        public static int GetExpressionLevel(Actor actor, string geneId)
        {
            if (actor == null || string.IsNullOrEmpty(geneId)) return 0;

            int slotLevel = FindSlotExpression(actor, geneId);
            if (slotLevel >= 0) return slotLevel;

            return CultivationData.GetGeneExpressionLevel(actor, geneId);   // 待挂基因读旧键
        }

        /// <summary>设置表达层级：双写（位点更新 + 旧键）</summary>
        public static void SetExpressionLevel(Actor actor, string geneId, int level)
        {
            if (actor == null || string.IsNullOrEmpty(geneId)) return;
            level = System.Math.Max(GenomeDef.MinExpressionLevel, System.Math.Min(GenomeDef.MaxExpressionLevel, level));

            // 写位点（在位基因）
            var chromosomes = GenomeData.GetChromosomes(actor);
            bool found = false;
            foreach (var c in chromosomes)
            {
                if (c == null) continue;
                for (int i = 0; i < c.Loci.Count; i++)
                {
                    if (c.Loci[i].GeneId == geneId)
                    {
                        c.Loci[i] = new LocusSlot(geneId, level);
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
            if (found) GenomeData.SaveChromosomes(actor, chromosomes);

            // 写旧键（兼容垫片）
            CultivationData.SetGeneExpressionLevel(actor, geneId, level);
        }

        /// <summary>按逻辑染色体（Group 0-7，基因固有属性）统计达标基因数——突破判定读源（阈值表不动）</summary>
        public static int GetGroupGeneCount(Actor actor, int group, int minExprLevel = 0)
        {
            if (actor == null || group < 0 || group > 7) return 0;
            int count = 0;
            foreach (var geneId in GetEffectiveGeneIds(actor))
            {
                var info = ElementDef.GetById(geneId);
                if (info == null || info.Group != group) continue;
                if (GetExpressionLevel(actor, geneId) >= minExprLevel) count++;
            }
            return count;
        }

        /// <summary>全部激活基因（业务源 + 图谱源合并去重，保激活序）</summary>
        public static List<string> MergeActivated(Actor actor)
        {
            var merged = new List<string>();
            if (actor == null) return merged;
            var seen = new HashSet<string>();

            var biz = CultivationData.GetUnlockedElements(actor);
            if (biz != null)
                foreach (var id in biz)
                    if (!string.IsNullOrEmpty(id) && seen.Add(id)) merged.Add(id);

            var graph = CultivationData.GetUnlockedGenes(actor);
            if (graph != null)
                foreach (var id in graph)
                    if (!string.IsNullOrEmpty(id) && seen.Add(id)) merged.Add(id);

            return merged;
        }

        /// <summary>待挂基因列表（激活未入位点，按激活序）</summary>
        public static List<string> GetPendingGeneIds(Actor actor)
        {
            var result = new List<string>();
            if (actor == null) return result;
            var mounted = new HashSet<string>();
            var chromosomes = GenomeData.GetChromosomes(actor);
            foreach (var c in chromosomes)
            {
                if (c == null) continue;
                foreach (var slot in c.Loci)
                    if (!slot.IsEmpty) mounted.Add(slot.GeneId);
            }
            foreach (var id in MergeActivated(actor))
                if (!string.IsNullOrEmpty(id) && !mounted.Contains(id)) result.Add(id);
            return result;
        }

        // ---------- 软分桶挂载（P2 对照表确认：按 Group 分桶挂载，位点自由排布） ----------

        /// <summary>位点索引 → 目标 Group（8 组均分映射 12 位点：i*8/12 → 0,0,1,1,2,3,3,4,5,5,6,7）</summary>
        public static int TargetGroupForIndex(int index, int lociCount)
        {
            if (lociCount <= 0) return 0;
            return System.Math.Min(7, index * 8 / lociCount);
        }

        /// <summary>
        /// 软分桶挂载：把 geneId 挂到第一条有该 Group 桶/空位的染色体。
        /// 优先落位本 Group 桶（位点目标 Group == 基因 Group），桶满兜底任意空位点。
        /// </summary>
        public static bool TryMountGene(Actor actor, string geneId)
        {
            if (actor == null || string.IsNullOrEmpty(geneId)) return false;
            var chromosomes = GenomeData.GetChromosomes(actor);
            if (chromosomes == null || chromosomes.Count == 0) return false;

            var info = ElementDef.GetById(geneId);
            int group = info != null ? info.Group : 0;

            bool saved = false;
            foreach (var c in chromosomes)
            {
                if (c == null || c.Loci == null) continue;
                int lociCount = c.Loci.Count;
                // 1) 本 Group 桶第一个空位点
                for (int i = 0; i < lociCount; i++)
                {
                    if (c.Loci[i].IsEmpty && TargetGroupForIndex(i, lociCount) == group)
                    {
                        c.Loci[i] = new LocusSlot(geneId, 0);
                        saved = true; break;
                    }
                }
                if (saved) break;
                // 2) 兜底任意空位点
                for (int i = 0; i < lociCount; i++)
                {
                    if (c.Loci[i].IsEmpty)
                    {
                        c.Loci[i] = new LocusSlot(geneId, 0);
                        saved = true; break;
                    }
                }
                if (saved) break;
            }
            if (saved) GenomeData.SaveChromosomes(actor, chromosomes);
            return saved;
        }

        /// <summary>基因是否已在位点（挂载完成）</summary>
        public static bool IsGeneMounted(Actor actor, string geneId)
        {
            if (actor == null || string.IsNullOrEmpty(geneId)) return false;
            var chromosomes = GenomeData.GetChromosomes(actor);
            if (chromosomes == null) return false;
            foreach (var c in chromosomes)
            {
                if (c == null) continue;
                foreach (var slot in c.Loci)
                    if (slot.GeneId == geneId) return true;
            }
            return false;
        }

        /// <summary>当前是否还有空位点可挂载</summary>
        public static bool HasFreeLocus(Actor actor)
        {
            var chromosomes = GenomeData.GetChromosomes(actor);
            if (chromosomes == null) return false;
            foreach (var c in chromosomes)
            {
                if (c == null || c.Loci == null) continue;
                foreach (var slot in c.Loci)
                    if (slot.IsEmpty) return true;
            }
            return false;
        }

        /// <summary>当前染色体条数</summary>
        public static int GetChromosomeCount(Actor actor)
        {
            var chromosomes = GenomeData.GetChromosomes(actor);
            return chromosomes != null ? chromosomes.Count : 0;
        }

        private static int FindSlotExpression(Actor actor, string geneId)
        {
            var chromosomes = GenomeData.GetChromosomes(actor);
            if (chromosomes == null) return -1;
            foreach (var c in chromosomes)
            {
                if (c == null) continue;
                foreach (var slot in c.Loci)
                    if (slot.GeneId == geneId) return slot.ExprLevel;
            }
            return -1;
        }
    }
}
