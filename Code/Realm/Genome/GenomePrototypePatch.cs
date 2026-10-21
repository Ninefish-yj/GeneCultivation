// ============================================================
// 文件: GenomePrototypePatch.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P0 R2 最小渲染原型——Harmony 接管原版 GeneEditor
// 说明: 会话默认开启（GenomePrototypeSession.Active=true）；F8 可手动关闭后走原版。
//       验收通过后由 P3 正式实现（六方法门禁 + 持久化）替换。
// ============================================================

using HarmonyLib;
using UnityEngine;

namespace Code.Realm.Genome
{
    /// <summary>接管 loadChromosomes：会话激活时喂测试 Nucleus，否则走原版</summary>
    [HarmonyPatch(typeof(GeneEditor), "loadChromosomes")]
    public static class GenomePrototype_LoadChromosomes_Patch
    {
        static bool Prefix(GeneEditor __instance)
        {
            // v0.21.49 兜底：打开基因编辑器时若测试染色体未构建（资产就绪必然可构建），立即补齐
            if (GenomePrototypeSession.Active && GenomePrototypeSession.TestNucleus == null)
            {
                GenomePrototypeSession.EnsureActive();
            }
            if (!GenomePrototypeSession.Active) return true;
            if (GenomePrototypeSession.TestNucleus == null) return true;

            try
            {
                var poolField = AccessTools.Field(typeof(GeneEditor), "_pool_elements_chromosomes");
                var pool = (ObjectPoolGenericMono<ChromosomeElement>)poolField.GetValue(__instance);
                pool.clear();

                foreach (var chrom in GenomePrototypeSession.TestNucleus.chromosomes)
                {
                    var elem = pool.getNext();
                    elem.show(chrom, __instance.clickChromosome);
                }

                if (GenomePrototypeSession.TestNucleus.chromosomes.Count > 0)
                {
                    __instance.clickChromosome(GenomePrototypeSession.TestNucleus.chromosomes[0]);
                }

                Debug.Log("[GeneCultivation][R2原型] loadChromosomes 接管成功，染色体数=" +
                          GenomePrototypeSession.TestNucleus.chromosomes.Count);
                return false; // 跳过原版
            }
            catch (System.Exception e)
            {
                Debug.LogError("[GeneCultivation][R2原型] loadChromosomes 接管失败: " + e.Message +
                               "\n" + e.StackTrace);
                return true; // 失败回退原版
            }
        }
    }

    /// <summary>接管 chromosomeUpdatedEvent：会话激活时阻止写回原版亚种</summary>
    [HarmonyPatch(typeof(GeneEditor), "chromosomeUpdatedEvent")]
    public static class GenomePrototype_ChromosomeUpdated_Patch
    {
        static bool Prefix()
        {
            if (!GenomePrototypeSession.Active) return true;
            // 原型只读：阻止 setDirty/recalculate/genesChangedEvent/eventGMO 写回亚种
            return false;
        }
    }
}
