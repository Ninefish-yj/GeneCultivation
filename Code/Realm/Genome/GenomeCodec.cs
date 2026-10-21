// ============================================================
// 文件: GenomeCodec.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P1 数据层编码/解码（纯函数，无状态）——编码规约见 §4.1
// 规约（§4.1 编码规约表，必须与实现一致；v0.22.0 实现注记：locus 内分隔用 ':' 避免与字段分隔 '@' 二义）：
//   - 主数据键 gc_genome，值："条1编码|条2编码|..."
//   - 每条："type@locus1,locus2,...@superIdx1;idx2@voidIdx1;idx2"
//     - type：b=大 / m=中 / s=小
//     - locus：empty=空位点 / "geneId:exprLevel"（如 regen:3）
//     - superIdx：扩增位点索引列表（分号分隔，可为空）
//     - voidIdx：虚空位点索引列表（可为空）
//   - 元数据键 gc_genome_meta："unlocked;fillProgress;seed"（§4.1：解锁条数/填点进度/位阶种子）
// 健壮性（§5.2）：try-catch + 损坏片段丢弃 + 上限校验（位点≤64、条数≤8）
// ============================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Code.Realm.Genome
{
    /// <summary>基因组编解码（纯函数，损坏回退，不炸档）</summary>
    public static class GenomeCodec
    {
        // ---------- 主数据 ----------

        /// <summary>编码染色体列表 → gc_genome 字符串</summary>
        public static string EncodeNucleus(List<GenomeChromosome> chromosomes)
        {
            if (chromosomes == null || chromosomes.Count == 0) return "";
            var parts = new List<string>();
            foreach (var c in chromosomes)
            {
                if (c == null) continue;
                parts.Add(EncodeChromosome(c));
            }
            return string.Join("|", parts);
        }

        /// <summary>解码 gc_genome 字符串 → 染色体列表（损坏片段丢弃，超限截断修复）</summary>
        public static List<GenomeChromosome> DecodeNucleus(string raw)
        {
            var result = new List<GenomeChromosome>();
            if (string.IsNullOrEmpty(raw)) return result;

            try
            {
                string[] blocks = raw.Split('|');
                foreach (var block in blocks)
                {
                    if (string.IsNullOrEmpty(block)) continue;
                    var chrom = DecodeChromosome(block);
                    if (chrom == null) continue;                       // 损坏片段丢弃（§5.2）
                    result.Add(chrom);
                    if (result.Count >= GenomeDef.MaxChromosomeCount) break;  // 条数上限（§5.2）
                }
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Warning("[Genome] gc_genome 解码失败，重置: " + e.Message);
                return new List<GenomeChromosome>();
            }
            return result;
        }

        private static string EncodeChromosome(GenomeChromosome c)
        {
            // loci：逗号分隔（locus 内 geneId:exprLevel，':' 见实现注记）
            var loci = new List<string>();
            foreach (var slot in c.Loci)
            {
                if (slot.IsEmpty) loci.Add("empty");
                else loci.Add(slot.GeneId + ":" + slot.ExprLevel);
            }
            // 索引列表：分号分隔
            string amps = string.Join(";", c.AmplifierIndices);
            string voids = string.Join(";", c.VoidIndices);
            return c.TypeCode + "@" + string.Join(",", loci) + "@" + amps + "@" + voids;
        }

        private static GenomeChromosome DecodeChromosome(string block)
        {
            try
            {
                string[] fields = block.Split('@');
                if (fields.Length < 2) return null;

                string typeCode = fields[0];
                if (!GenomeDef.VanillaChromosomeType.ContainsKey(typeCode)) return null;

                var chrom = new GenomeChromosome(typeCode);

                // loci
                string[] lociTokens = fields[1].Split(',');
                if (lociTokens.Length > GenomeDef.MaxLociPerChromosome) return null;   // 位点上限（§5.2）
                foreach (var tok in lociTokens)
                {
                    if (string.IsNullOrEmpty(tok)) { chrom.Loci.Add(LocusSlot.Empty()); continue; }
                    if (tok == "empty") { chrom.Loci.Add(LocusSlot.Empty()); continue; }
                    int atIdx = tok.IndexOf(':');
                    if (atIdx <= 0) return null;   // 非法 locus 格式，整条丢弃
                    string geneId = tok.Substring(0, atIdx);
                    int level;
                    if (!int.TryParse(tok.Substring(atIdx + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out level))
                        return null;
                    chrom.Loci.Add(new LocusSlot(geneId, level));
                }

                // amplifier / void 索引（可能缺字段，空则无）
                if (fields.Length >= 3) ParseIndices(fields[2], chrom.AmplifierIndices);
                if (fields.Length >= 4) ParseIndices(fields[3], chrom.VoidIndices);

                return chrom;
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[Genome] 单条染色体解码失败丢弃: " + e.Message);
                return null;
            }
        }

        private static void ParseIndices(string raw, List<int> target)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var tok in raw.Split(';'))
            {
                int idx;
                if (int.TryParse(tok, NumberStyles.Integer, CultureInfo.InvariantCulture, out idx))
                {
                    if (idx >= 0 && idx < GenomeDef.MaxLociPerChromosome)
                        target.Add(idx);
                }
            }
        }

        // ---------- 元数据 ----------

        /// <summary>基因组元数据（§4.1：解锁条数 / 填点进度 / 位阶种子）</summary>
        public struct GenomeMeta
        {
            public int UnlockedCount;   // 已解锁染色体条数
            public float FillProgress;  // 当前填点进度（0~1 区间）
            public int Seed;            // 位阶种子（染色体生成确定性）

            public static GenomeMeta Default() => new GenomeMeta { UnlockedCount = 1, FillProgress = 0f, Seed = 0 };
        }

        public static string EncodeMeta(GenomeMeta meta)
        {
            return meta.UnlockedCount + GenomeDef.MetaSeparator +
                   meta.FillProgress.ToString("0.###", CultureInfo.InvariantCulture) + GenomeDef.MetaSeparator +
                   meta.Seed;
        }

        public static GenomeMeta DecodeMeta(string raw)
        {
            var meta = GenomeMeta.Default();
            if (string.IsNullOrEmpty(raw)) return meta;
            try
            {
                string[] parts = raw.Split(GenomeDef.MetaSeparator[0]);
                if (parts.Length >= 1)
                {
                    int count;
                    if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                        meta.UnlockedCount = Math.Max(1, Math.Min(GenomeDef.MaxChromosomeCount, count));
                }
                if (parts.Length >= 2)
                {
                    float p;
                    if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out p))
                        meta.FillProgress = Math.Max(0f, Math.Min(1f, p));
                }
                if (parts.Length >= 3)
                {
                    int seed;
                    if (int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
                        meta.Seed = seed;
                }
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[Genome] 元数据解码失败回退默认: " + e.Message);
            }
            return meta;
        }
    }
}
