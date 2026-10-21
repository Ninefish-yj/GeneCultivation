// ============================================================
// 文件: GenomePrototypeSession.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P0 R2 最小渲染原型——会话状态 + 测试 Nucleus 构建 + F8 切换
// 说明: 仅用于验证"自定义染色体能否在原版 GeneEditor 渲染"。
//       验收通过后由 P3 正式实现（GenomeEditorSession/Bridge/Host）替换。
// 激活: 默认开启（Toggler 挂载时自动构建测试染色体）；游戏内按 F8 可关闭/再开启。
//       开启后打开亚种编辑器→基因页签即可看到测试染色体。
// ============================================================

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Code.Realm.Genome
{
    public static class GenomePrototypeSession
    {
        public static bool Active = true;   // v0.21.48 起默认开启
        public static Nucleus TestNucleus;

        /// <summary>确保会话激活且有测试染色体（v0.21.49 修复：资产未就绪时返回 false，调用方每帧重试，直到构建成功）</summary>
        public static bool EnsureActive()
        {
            if (!Active) return true;
            if (TestNucleus != null) return true;
            if (AssetManager.gene_library == null || AssetManager.gene_library.list == null) return false;
            try
            {
                TestNucleus = BuildTestNucleus();
                Debug.Log("[GeneCultivation][R2原型] 默认开启，测试染色体数=" +
                          (TestNucleus != null ? TestNucleus.chromosomes.Count : 0));
                return TestNucleus != null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GeneCultivation][R2原型] 测试染色体构建失败: " + e.Message);
                return false;
            }
        }

        public static void Toggle()
        {
            Active = !Active;
            if (Active) EnsureActive();
            Debug.Log("[GeneCultivation][R2原型] 会话激活=" + Active +
                      (TestNucleus != null ? " 测试染色体数=" + TestNucleus.chromosomes.Count : ""));
        }

        /// <summary>
        /// 构建 1 条混合位点测试染色体（chromosome_medium=12 位点，2 行×6 列）。
        /// 布局：标准位点(填代理基因) / 虚空位点(empty+void) / 放大器位点(empty+amplifier) 混合。
        /// v0.21.50 修复：pNew 必须传 true——false 时构造函数不初始化 genes（空列表），
        /// showGenes 遍历 genes.Count=0 → 位点栏空。
        /// </summary>
        private static Nucleus BuildTestNucleus()
        {
            var nucleus = new Nucleus();

            // pNew=true：初始化 12 个 empty 基因位点（chromosome_medium=12，天人武道已验）
            var chrom = new Chromosome("chromosome_medium", true);

            // 覆盖为固定布局（readonly 字段反射可设；失败不打致命，仅警告）
            SetPrivateField(chrom, "_loci_amplifiers", new List<int> { 4, 9 });
            SetPrivateField(chrom, "_loci_empty", new List<int> { 3, 7 });

            // 从原版基因库取 3 个非空基因作渲染代理（避免硬编码 id 不存在）
            var proxies = new List<GeneAsset>();
            foreach (var g in AssetManager.gene_library.list)
            {
                if (g != null && !g.is_empty)
                {
                    proxies.Add(g);
                    if (proxies.Count >= 3) break;
                }
            }
            Debug.Log("[GeneCultivation][R2原型] 代理基因数=" + proxies.Count +
                      (proxies.Count > 0 ? " 首个=" + proxies[0].id : "") +
                      " 位点数=" + chrom.genes.Count);

            // 标准位点填代理基因（索引 0,1,2,5,6,8,10,11）；虚空/放大器保持 empty
            // v0.21.50：基因库全 empty 时跳过填充（染色体仍是 12 空位点，可渲染）
            int[] standardLoci = { 0, 1, 2, 5, 6, 8, 10, 11 };
            if (proxies.Count > 0)
            {
                for (int i = 0; i < standardLoci.Length; i++)
                {
                    int idx = standardLoci[i];
                    if (idx < chrom.genes.Count)
                    {
                        chrom.genes[idx] = proxies[i % proxies.Count];
                    }
                }
            }

            nucleus.addChromosome(chrom);
            return nucleus;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var f = obj.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null)
            {
                Debug.LogWarning("[GeneCultivation][R2原型] 未找到字段: " + fieldName);
                return;
            }
            try
            {
                f.SetValue(obj, value);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GeneCultivation][R2原型] 设置字段失败 " + fieldName + ": " + e.Message);
            }
        }
    }

    /// <summary>原型切换器（MonoBehaviour，在 ModClass.OnModLoad 中实例化）
    /// 挂载即默认开启并构建测试染色体；每帧重试直到构建成功（资产未就绪时静默等待）；
    /// F8 用于手动关闭/再开启。</summary>
    public class GenomePrototypeToggler : MonoBehaviour
    {
        private void Awake()
        {
            GenomePrototypeSession.EnsureActive();   // 默认开启（首次尝试）
        }

        private void Update()
        {
            // v0.21.49：资产未就绪时每帧重试，构建成功后零开销跳过
            GenomePrototypeSession.EnsureActive();
            if (Input.GetKeyDown(KeyCode.F8))
            {
                GenomePrototypeSession.Toggle();
            }
        }
    }
}
