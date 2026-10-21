// ============================================================
// 文件: GenomeEditorSession.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P3 渲染批1——渲染会话状态（Active 门禁 + 目标 Actor + 真实基因组数据源）
// 说明: 替换 P0 R2 原型（GenomePrototypeSession，TestNucleus 静态测试染色体）——
//       数据源改为 GenomeData.GetChromosomes(actor) → ToVanillaChromosome（P0 定稿 A 路）。
//       会话默认不激活（Active=false）；由 GenomeEditorHost（渲染批2）在打开单位窗基因页签时
//       SetTarget(actor) 激活；F8 为调试模式（GenomeEditorToggler），可手动接管预览渲染管线。
// 门禁: patch 统一走 IsActive——false 时全部走原版（亚种编辑器零污染，回归关卡）。
// ============================================================

using System;
using System.Collections.Generic;
using Code.Data;
using UnityEngine;

namespace Code.Realm.Genome
{
    /// <summary>基因编辑器渲染会话（P3）</summary>
    public static class GenomeEditorSession
    {
        /// <summary>会话激活（渲染批2 Host 打开单位窗基因页签时置 true；F8 调试也可）</summary>
        public static bool Active = false;

        /// <summary>目标单位（渲染数据源；null=调试示例模式）</summary>
        public static Actor TargetActor;

        /// <summary>当前渲染 Nucleus（内存对象，不写回原版亚种）</summary>
        public static Nucleus CurrentNucleus;

        private static long _cachedActorId = -1;

        /// <summary>patch 门禁：会话激活且渲染数据就绪才接管，否则全部走原版</summary>
        public static bool IsActive
        {
            get { return Active && CurrentNucleus != null; }
        }

        /// <summary>设置目标单位并构建渲染数据（Host 打开页签时调用；幂等缓存按单位 id）</summary>
        public static bool SetTarget(Actor actor)
        {
            Active = true;
            if (actor == null) return false;
            if (CurrentNucleus != null && _cachedActorId == actor.getID()) return true;

            try
            {
                var chromosomes = GenomeData.GetChromosomes(actor);
                if (chromosomes == null || chromosomes.Count == 0) return false;

                var nucleus = new Nucleus();
                foreach (var c in chromosomes)
                {
                    if (c == null) continue;
                    var vc = c.ToVanillaChromosome();
                    if (vc != null) nucleus.addChromosome(vc);
                }
                CurrentNucleus = nucleus;
                TargetActor = actor;
                _cachedActorId = actor.getID();
                Debug.Log("[GeneCultivation][P3] 渲染会话绑定单位=" + actor.getName() +
                          " 染色体=" + nucleus.chromosomes.Count +
                          " 总位点=" + CountLoci(nucleus));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GeneCultivation][P3] 渲染数据构建失败: " + e.Message);
                return false;
            }
        }

        /// <summary>结束会话（Host 关闭页签时调用）</summary>
        public static void EndSession()
        {
            Active = false;
            TargetActor = null;
            CurrentNucleus = null;
            _cachedActorId = -1;
        }

        /// <summary>F8 调试模式切换：无单位绑定，渲染示例基因组（走真实渲染管线）</summary>
        public static void ToggleDebug()
        {
            if (IsActive && TargetActor == null)
            {
                EndSession();
                Debug.Log("[GeneCultivation][P3] 调试模式关闭");
                return;
            }
            Active = true;
            TargetActor = null;
            if (CurrentNucleus == null)
            {
                CurrentNucleus = BuildDebugNucleus();
            }
            Debug.Log("[GeneCultivation][P3] 调试模式开启 染色体=" +
                      (CurrentNucleus != null ? CurrentNucleus.chromosomes.Count : 0));
        }

        /// <summary>调试示例基因组：1 条 medium 染色体，位点填真实模组基因 id（代理渲染）</summary>
        private static Nucleus BuildDebugNucleus()
        {
            var nucleus = new Nucleus();
            var chrom = new GenomeChromosome("m");
            var all = ElementDef.AllElements;
            int fill = Math.Min(6, all != null ? all.Count : 0);
            for (int i = 0; i < 12; i++)
            {
                if (i < fill && all != null && i < all.Count)
                    chrom.Loci.Add(new LocusSlot(all[i].Id, (i % 3) + 1));
                else
                    chrom.Loci.Add(LocusSlot.Empty());
            }
            var vc = chrom.ToVanillaChromosome();
            if (vc != null) nucleus.addChromosome(vc);
            return nucleus;
        }

        private static int CountLoci(Nucleus nucleus)
        {
            int total = 0;
            foreach (var c in nucleus.chromosomes) total += c.genes != null ? c.genes.Count : 0;
            return total;
        }
    }
}
