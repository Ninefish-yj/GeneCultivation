// ============================================================
// 异能数据 · 命运系（v0.18.99 大众认知补齐）
// 大类：命运系（特殊系/命运——操控概率与因果的大众认知体系）
// 小类：命运/因果/气运/灾厄
// 机制 = 主/副标签组合（概率类标签：暴击/斩杀/诅咒/衰老/闪避/回血等）。
// ============================================================

using System.Collections.Generic;

namespace Code.Realm.Prototype
{
    public static class PrototypeData_Fate
    {
        public static readonly List<PrototypeDef> List = new List<PrototypeDef>
        {
            // ---- 命运 ----
            new PrototypeDef("fate_thread", "命运丝线", "编织命运丝线，标记目标并施加命运诅咒", "命运系", "命运",
                PrototypeTag.Mark, PrototypeTag.Curse, 5, 6),
            new PrototypeDef("fate_causality", "因果律", "因果纠缠，受击反噬伤害并标记宿命", "命运系", "命运",
                PrototypeTag.Reflect, PrototypeTag.Mark, 5, 9),
            new PrototypeDef("fate_weave", "命运编织", "攻击暴击，分身免伤", "命运系", "命运",
                PrototypeTag.Crit, PrototypeTag.Clone, 5, 8),
            // ---- 因果 ----
            new PrototypeDef("fate_karma", "业力缠身", "诅咒持续伤害，衰老目标", "命运系", "因果",
                PrototypeTag.Curse, PrototypeTag.Aging, 5, 7),
            new PrototypeDef("fate_reincarnation", "宿命轮回", "灵魂回生，净化祛邪", "命运系", "因果",
                PrototypeTag.Soul, PrototypeTag.Cleanse, 5, 8),
            new PrototypeDef("fate_doom", "命数已尽", "宿命已定，处决重伤目标并加速其衰老", "命运系", "因果",
                PrototypeTag.Execute, PrototypeTag.Aging, 5, 9),
            // ---- 气运 ----
            new PrototypeDef("fate_fortune", "气运加身", "攻击回血，治疗友军", "命运系", "气运",
                PrototypeTag.Heal, PrototypeTag.HealAlly, 5, 5),
            new PrototypeDef("fate_opportunity", "时来运转", "攻击暴击，祝福庇护", "命运系", "气运",
                PrototypeTag.Crit, PrototypeTag.Blessing, 5, 6),
            // ---- 灾厄 ----
            new PrototypeDef("fate_misfortune", "厄运缠身", "虚弱目标，衰老目标", "命运系", "灾厄",
                PrototypeTag.Weakness, PrototypeTag.Aging, 5, 7),
            new PrototypeDef("fate_reversal", "命运逆转", "命运逆转，反噬并再生", "命运系", "因果",
                PrototypeTag.Reflect, PrototypeTag.Regeneration, 0, 8),
            new PrototypeDef("fate_chain", "因果锁链", "因果锁链，标记连锁", "命运系", "因果",
                PrototypeTag.Mark, PrototypeTag.Chain, 0, 6),
            new PrototypeDef("fate_blessing", "气运加身", "气运加身，强化祝福", "命运系", "气运",
                PrototypeTag.Buff, PrototypeTag.Blessing, 0, 5),
            new PrototypeDef("fate_curse", "厄运缠身", "厄运缠身，诅咒虚弱", "命运系", "灾厄",
                PrototypeTag.Curse, PrototypeTag.Weakness, 0, 6),
            new PrototypeDef("fate_gaze", "命运窥视", "窥视命运，精准暴击", "命运系", "命运",
                PrototypeTag.Telekinesis, PrototypeTag.Crit, 0, 7),
            new PrototypeDef("fate_rewrite", "命数改写", "改写命数，时缓诅咒", "命运系", "命运",
                PrototypeTag.TimeSlow, PrototypeTag.Curse, 0, 9),
        };
    }
}
