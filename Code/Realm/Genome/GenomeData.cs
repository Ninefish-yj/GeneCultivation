// ============================================================
// 文件: GenomeData.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P1 数据层访问——actor.data 读写、EnsureInitialized 惰性初始化、迁移入口
// 存储键（§4.1/§5.1，前缀 gc_ 隔离，不与现有键冲突）：
//   - gc_genome：主数据（染色体编码）
//   - gc_genome_meta：元数据（解锁条数/填点进度/位阶种子）
//   - gc_genome_version：迁移版本标记（当前=1）
// 原则：纯读写无业务逻辑；单位首次被访问时惰性初始化（§5.1：绝不在加载全图时全量迁移）
// ============================================================

using System.Collections.Generic;
using Code.Data;

namespace Code.Realm.Genome
{
    /// <summary>基因组数据访问层（actor.data，纯读写）</summary>
    public static class GenomeData
    {
        public const string GC_GENOME = "gc_genome";
        public const string GC_GENOME_META = "gc_genome_meta";
        public const string GC_GENOME_VERSION = "gc_genome_version";
        /// <summary>迁移版本：1=初始（仅 ds_gene_unlocked 源，v0.22.0）；2=业务源+图谱源合并（v0.22.1，version=1 触发重迁）</summary>
        public const string CURRENT_VERSION = "2";

        /// <summary>惰性初始化：无版本标记则迁移旧档；已初始化直接返回（幂等）</summary>
        public static void EnsureInitialized(Actor actor)
        {
            if (actor == null) return;
            var data = ActorDataAccessor.GetData(actor);
            if (data == null) return;
            if (data.custom_data_string == null) return;

            if (!data.custom_data_string.TryGetValue(GC_GENOME_VERSION, out string version) ||
                string.IsNullOrEmpty(version))
            {
                GenomeMigration.Migrate(actor);   // 首次访问时迁移（§5.1 时机；新单位无版本键不可用索引器）
            }
        }

        /// <summary>获取单位基因组染色体列表（未初始化先迁移）</summary>
        public static List<GenomeChromosome> GetChromosomes(Actor actor)
        {
            EnsureInitialized(actor);
            var data = ActorDataAccessor.GetData(actor);
            if (data == null || data.custom_data_string == null) return new List<GenomeChromosome>();
            data.custom_data_string.TryGetValue(GC_GENOME, out string raw);
            return GenomeCodec.DecodeNucleus(raw);
        }

        /// <summary>保存染色体列表（写 gc_genome）</summary>
        public static void SaveChromosomes(Actor actor, List<GenomeChromosome> chromosomes)
        {
            if (actor == null) return;
            var data = ActorDataAccessor.GetData(actor);
            if (data == null || data.custom_data_string == null) return;
            data.custom_data_string[GC_GENOME] = GenomeCodec.EncodeNucleus(chromosomes);
        }

        /// <summary>获取元数据（未初始化先迁移；损坏回退默认）</summary>
        public static GenomeCodec.GenomeMeta GetMeta(Actor actor)
        {
            EnsureInitialized(actor);
            var data = ActorDataAccessor.GetData(actor);
            if (data == null || data.custom_data_string == null) return GenomeCodec.GenomeMeta.Default();
            data.custom_data_string.TryGetValue(GC_GENOME_META, out string raw);
            return GenomeCodec.DecodeMeta(raw);
        }

        /// <summary>保存元数据（写 gc_genome_meta）</summary>
        public static void SaveMeta(Actor actor, GenomeCodec.GenomeMeta meta)
        {
            if (actor == null) return;
            var data = ActorDataAccessor.GetData(actor);
            if (data == null || data.custom_data_string == null) return;
            data.custom_data_string[GC_GENOME_META] = GenomeCodec.EncodeMeta(meta);
        }

        /// <summary>是否已完成新格式初始化</summary>
        public static bool IsInitialized(Actor actor)
        {
            var data = ActorDataAccessor.GetData(actor);
            if (data == null || data.custom_data_string == null) return false;
            string v;
            return data.custom_data_string.TryGetValue(GC_GENOME_VERSION, out v) && !string.IsNullOrEmpty(v);
        }
    }
}
