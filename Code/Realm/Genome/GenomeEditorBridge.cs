// ============================================================
// 文件: GenomeEditorBridge.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P3 渲染批1——Harmony 接管原版 GeneEditor 六方法（A 路：代理基因资产渲染）
// 门禁: 全部 Prefix 以 GenomeEditorSession.IsActive 为开关——false 时 return true（走原版，
//       亚种编辑器零污染）；true 时接管渲染。
// 接管清单（§4.2）：
//   1. loadChromosomes    : 清 pool → 喂会话 Nucleus → 选中第一条
//   2. chromosomeUpdatedEvent : dirty+recalculate → 阻止写回亚种（genesChangedEvent/eventGMO 跳过）；
//                              持久化回写留 P4（点击写入未启用，数据由修炼填点驱动）
//   3. showGenes          : 渲染位点（跳过 _window_subspecies.updateStats，防污染/空引用）
//   4. reloadButtons      : 计数文本改我方口径（已激活/库按钮总数），按钮不隐藏（保持原版观感）
//   5. clickGeneAssetAction : 阻止原版写亚种（点击写入位点 P4 细化；挂载由修炼填点驱动）
//   6. Update             : 选中框跟随（不读 _meta_object，防空引用）
// ============================================================

using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Code.UI;

namespace Code.Realm.Genome
{
    /// <summary>P3 渲染诊断（屏幕常驻小字，手机无日志时的定位手段）</summary>
    public static class GenomeBridgeDiag
    {
        public static string P3Diag = "[P3] 未初始化";
    }

    // ---------- 1. loadChromosomes ----------

    [HarmonyPatch(typeof(GeneEditor), "loadChromosomes")]
    public static class GenomeBridge_LoadChromosomes
    {
        static bool Prefix(GeneEditor __instance)
        {
            if (!GenomeEditorSession.IsActive) return true;
            if (GenomeEditorSession.CurrentNucleus == null) return true;
            try
            {
                var pool = AccessTools.Field(typeof(GeneEditor), "_pool_elements_chromosomes").GetValue(__instance)
                           as ObjectPoolGenericMono<ChromosomeElement>;
                if (pool == null) return true;
                pool.clear();

                var chromosomes = GenomeEditorSession.CurrentNucleus.chromosomes;
                foreach (var chrom in chromosomes)
                {
                    pool.getNext().show(chrom, __instance.clickChromosome);
                }
                if (chromosomes.Count > 0)
                {
                    __instance.clickChromosome(chromosomes[0]);
                }
                GenomeBridgeDiag.P3Diag = "[P3] chromPool OK C=" + chromosomes.Count + " sel=" +
                    (AccessTools.Field(typeof(GeneEditor), "_selected_chromosome")?.GetValue(__instance) ?? "null");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GeneCultivation][P3] loadChromosomes 接管失败: " + e.Message);
                return true;
            }
        }
    }

    // ---------- 2. chromosomeUpdatedEvent ----------

    [HarmonyPatch(typeof(GeneEditor), "chromosomeUpdatedEvent")]
    public static class GenomeBridge_ChromosomeUpdated
    {
        static bool Prefix(GeneEditor __instance)
        {
            if (!GenomeEditorSession.IsActive) return true;
            try
            {
                var sel = AccessTools.Field(typeof(GeneEditor), "_selected_chromosome").GetValue(__instance)
                          as Chromosome;
                if (sel != null)
                {
                    sel.setDirty();
                    sel.recalculate();
                    // 不调用 _meta_object.genesChangedEvent()/eventGMO()——防写回原版亚种
                    // 持久化回写：P4 启用点击写入时按代理→真实基因反查写回 GenomeData
                }
                // 刷新位点与染色体列表（走我方 Prefix）
                AccessTools.Method(typeof(GeneEditor), "showGenes").Invoke(__instance, new object[] { sel });
                AccessTools.Method(typeof(GeneEditor), "loadChromosomes").Invoke(__instance, new object[] { false });
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GeneCultivation][P3] chromosomeUpdatedEvent 接管失败: " + e.Message);
                return true;
            }
        }
    }

    // ---------- 3. showGenes ----------

    [HarmonyPatch(typeof(GeneEditor), "showGenes")]
    public static class GenomeBridge_ShowGenes
    {
        static bool Prefix(GeneEditor __instance, Chromosome pChromosome)
        {
            if (!GenomeEditorSession.IsActive) return true;
            try
            {
                var pool = AccessTools.Field(typeof(GeneEditor), "_pool_elements_loci").GetValue(__instance)
                           as ObjectPoolGenericMono<LocusElement>;
                if (pool == null || pChromosome == null || pChromosome.genes == null)
                {
                    GenomeBridgeDiag.P3Diag = "[P3] showGenes 失败 pool=" + (pool != null) +
                        " chrom=" + (pChromosome != null) +
                        " genes=" + (pChromosome != null ? pChromosome.genes : null);
                    return true; // 走原版（可能 NRE，诊断上屏）
                }

                pool.clear();
                for (int i = 0; i < pChromosome.genes.Count; i++)
                {
                    GeneAsset pGene = pChromosome.genes[i];
                    var next = pool.getNext();
                    var clickDelegate = AccessTools.Method(typeof(GeneEditor), "selectLocus")
                        .CreateDelegate(typeof(LocusClickEvent), __instance) as LocusClickEvent;
                    next.show(i, pChromosome, pGene, pChromosome.getLocusType(i), clickDelegate);
                }
                GenomeBridgeDiag.P3Diag = "[P3] showGenes OK loci=" + pChromosome.genes.Count +
                    " chrom=" + (AccessTools.Field(typeof(GeneEditor), "_selected_chromosome")?.GetValue(__instance)?.ToString() ?? "null");
                // 跳过 _window_subspecies.updateStats()——防亚种窗污染/空引用（§4.2）
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GeneCultivation][P3] showGenes 接管失败: " + e.Message);
                return true;
            }
        }
    }

    // ---------- 4. reloadButtons ----------

    [HarmonyPatch(typeof(GeneEditor), "reloadButtons")]
    public static class GenomeBridge_ReloadButtons
    {
        static bool Prefix(GeneEditor __instance)
        {
            if (!GenomeEditorSession.IsActive) return true;
            try
            {
                // 按钮不隐藏（保持原版观感）；计数文本改我方口径：已激活基因/图谱基因
                int activated = 0;
                if (GenomeEditorSession.TargetActor != null)
                {
                    activated = GenomeEffects.GetEffectiveGeneIds(GenomeEditorSession.TargetActor).Count;
                }
                var dict = AccessTools.Field(typeof(GeneEditor), "_dictionary_gene_buttons").GetValue(__instance)
                           as System.Collections.Generic.Dictionary<GeneAsset, GeneButton>;
                int total = dict != null ? dict.Count : 0;
                if (__instance.genome_counter_text != null)
                {
                    __instance.genome_counter_text.text = activated + "/" + total;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GeneCultivation][P3] reloadButtons 接管失败: " + e.Message);
                return true;
            }
        }
    }

    // ---------- 5. clickGeneAssetAction ----------

    [HarmonyPatch(typeof(GeneEditor), "clickGeneAssetAction")]
    public static class GenomeBridge_ClickGeneAsset
    {
        static bool Prefix()
        {
            // 会话激活时阻止原版写入亚种；点击写入位点 P4 细化（挂载由修炼填点驱动）
            if (!GenomeEditorSession.IsActive) return true;
            return false;
        }
    }

    // ---------- 5b. 调试按钮门禁（克隆体上的 randomize/shuffle 按钮不得写回亚种） ----------

    [HarmonyPatch(typeof(GeneEditor), "debugRandomizeGenes")]
    public static class GenomeBridge_DebugRandomize
    {
        static bool Prefix() => !GenomeEditorSession.IsActive;
    }

    [HarmonyPatch(typeof(GeneEditor), "debugShuffleGenes")]
    public static class GenomeBridge_DebugShuffle
    {
        static bool Prefix() => !GenomeEditorSession.IsActive;
    }

    // ---------- 5c. 基因系统窗口关闭 → 结束会话（防会话残留污染亚种编辑器） ----------

    [HarmonyPatch(typeof(ScrollWindow), "clickCloseButton")]
    public static class GenomeBridge_WindowClose
    {
        static void Prefix(ScrollWindow __instance)
        {
            try
            {
                if (Code.UI.GeneEditorWindow.IsOwned(__instance))
                    GenomeEditorSession.EndSession();
            }
            catch { }
        }
    }

    // ---------- 6. Update ----------

    [HarmonyPatch(typeof(GeneEditor), "Update")]
    public static class GenomeBridge_Update
    {
        static bool Prefix(GeneEditor __instance)
        {
            if (!GenomeEditorSession.IsActive) return true;
            try
            {
                var selLocus = AccessTools.Field(typeof(GeneEditor), "_selected_locus").GetValue(__instance)
                               as LocusElement;
                var selChrom = AccessTools.Field(typeof(GeneEditor), "_selected_chromosome").GetValue(__instance)
                               as Chromosome;
                if (selChrom == null) return false;   // 会话内无选中染色体，不渲染选中框

                if (__instance.selection_gene_asset != null)
                    __instance.selection_gene_asset.gameObject.SetActive(selLocus != null);
                if (__instance.selection_locus != null)
                    __instance.selection_locus.gameObject.SetActive(selLocus != null);

                if (selLocus != null)
                {
                    if (__instance.selection_locus != null)
                        __instance.selection_locus.transform.position = selLocus.transform.position;
                    var btn = GetCurrentGeneAssetButton(__instance, selLocus);
                    if (__instance.selection_gene_asset != null && btn != null)
                        __instance.selection_gene_asset.transform.position = btn.transform.position;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GeneCultivation][P3] Update 接管失败: " + e.Message);
                return true;
            }
        }

        private static GeneButton GetCurrentGeneAssetButton(GeneEditor __instance, LocusElement selLocus)
        {
            try
            {
                var gene = selLocus.getGeneAsset();
                if (gene == null) return null;
                var dict = AccessTools.Field(typeof(GeneEditor), "_dictionary_gene_buttons").GetValue(__instance)
                           as System.Collections.Generic.Dictionary<GeneAsset, GeneButton>;
                if (dict != null && dict.ContainsKey(gene)) return dict[gene];
            }
            catch (Exception) { }
            return null;
        }
    }
}
