// ============================================================
// 异能数据 · 精神系（v0.18.0）
// 大类：精神系 | 小类：念力/心灵/意志/预知/催眠/操控/幻象
// 描述 = 真实机制（由主/副标签组合表达），不含虚假效果。
// ============================================================

using System.Collections.Generic;

namespace Code.Realm.Prototype
{
    public static class PrototypeData_Psychic
    {
        public static readonly List<PrototypeDef> List = new List<PrototypeDef>
        {
            // ---- 念力 ----
            new PrototypeDef("ps_psycho_01", "念动力", "攻击念力贯穿高伤，概率眩晕", "精神系", "念力",
                PrototypeTag.Telekinesis, PrototypeTag.Stun, 4, 5),
            new PrototypeDef("ps_psycho_02", "念壁", "护盾减伤，受击反击震慑攻击者", "精神系", "念力",
                PrototypeTag.Shield, PrototypeTag.Sound, 4, 7),
            new PrototypeDef("ps_psycho_03", "力场", "攻击重力压制目标，溅射波及", "精神系", "念力",
                PrototypeTag.Gravity, PrototypeTag.Chain, 4, 9),
            // ---- 心灵 ----
            new PrototypeDef("ps_mind_01", "心感", "攻击概率回复生命，闪避免伤", "精神系", "心灵",
                PrototypeTag.Heal, PrototypeTag.Dodge, 3, 3),
            new PrototypeDef("ps_mind_02", "心爆", "攻击恐惧眩晕目标，附加诅咒", "精神系", "心灵",
                PrototypeTag.Fear, PrototypeTag.Curse, 3, 6),
            new PrototypeDef("ps_mind_03", "心链", "受击自愈回血，攻击治疗", "精神系", "心灵",
                PrototypeTag.Regeneration, PrototypeTag.Heal, 3, 9),
            // ---- 意志 ----
            new PrototypeDef("ps_will_01", "钢铁之心", "护盾减伤强化，受击反击眩晕攻击者", "精神系", "意志",
                PrototypeTag.Shield, PrototypeTag.Magnetism, 3, 4),
            new PrototypeDef("ps_will_02", "战意", "攻击狂暴当次+25%，吸血", "精神系", "意志",
                PrototypeTag.Berserk, PrototypeTag.Lifesteal, 3, 6),
            new PrototypeDef("ps_will_03", "心志", "攻击概率回复生命，受击免伤", "精神系", "意志",
                PrototypeTag.Soul, PrototypeTag.Invisibility, 3, 9),
            // ---- 预知 ----
            new PrototypeDef("ps_foresight_01", "警兆", "闪避强化，受击概率免伤", "精神系", "预知",
                PrototypeTag.Illusion, PrototypeTag.Dodge, 4, 1),
            new PrototypeDef("ps_foresight_02", "未来视", "攻击暴击破甲，念力贯穿", "精神系", "预知",
                PrototypeTag.Crit, PrototypeTag.Telekinesis, 4, 5),
            new PrototypeDef("ps_foresight_03", "先读", "受击反击麻痹攻击者，攻击减速", "精神系", "预知",
                PrototypeTag.Vibration, PrototypeTag.Slow, 4, 9),
            // ---- 催眠 ----
            new PrototypeDef("ps_hypno_01", "催眠术", "攻击眩晕定身目标，恐惧加深", "精神系", "催眠",
                PrototypeTag.Stun, PrototypeTag.Fear, 5, 1),
            new PrototypeDef("ps_hypno_02", "沉睡", "攻击时缓目标，概率沉睡", "精神系", "催眠",
                PrototypeTag.TimeSlow, PrototypeTag.Sleep, 5, 5),
            new PrototypeDef("ps_hypno_03", "梦魇", "攻击附加诅咒持续伤害，眩晕", "精神系", "催眠",
                PrototypeTag.Curse, PrototypeTag.Stun, 5, 9),
            // ---- 操控 ----
            new PrototypeDef("ps_control_01", "精神控制", "攻击恐惧眩晕，减速目标", "精神系", "操控",
                PrototypeTag.Fear, PrototypeTag.Slow, 5, 1),
            new PrototypeDef("ps_control_02", "夺心", "攻击眩晕定身，溅射波及", "精神系", "操控",
                PrototypeTag.Stun, PrototypeTag.Chain, 5, 5),
            new PrototypeDef("ps_control_03", "提线", "攻击击退定身，吸血回复", "精神系", "操控",
                PrototypeTag.Knockback, PrototypeTag.Lifesteal, 5, 9),
            // ---- 幻象 ----
            new PrototypeDef("ps_illusion_01", "幻术", "闪避强化，受击概率免伤", "精神系", "幻象",
                PrototypeTag.Illusion, PrototypeTag.Fear, 4, 3),
            new PrototypeDef("ps_illusion_02", "影分身", "受击免伤最高，分身回敬伤害", "精神系", "幻象",
                PrototypeTag.Invisibility, PrototypeTag.Clone, 4, 6),
            new PrototypeDef("ps_illusion_03", "幻境", "受击反击震慑攻击者，闪避", "精神系", "幻象",
                PrototypeTag.Sound, PrototypeTag.Illusion, 4, 9),
            // ---- 记忆（v0.19.2 大众认知补全：记忆操控系）----
            new PrototypeDef("ps_memory_01", "篡忆", "攻击眩晕定身目标，附加诅咒", "精神系", "记忆",
                PrototypeTag.Stun, PrototypeTag.Curse, 4, 2),
            new PrototypeDef("ps_memory_02", "遗忆", "攻击恐惧定身目标，石化易碎", "精神系", "记忆",
                PrototypeTag.Fear, PrototypeTag.Petrify, 4, 5),
            new PrototypeDef("ps_memory_03", "织忆", "攻击恐惧眩晕，受击反击震慑攻击者", "精神系", "记忆",
                PrototypeTag.Fear, PrototypeTag.Sound, 4, 8),
            // ---- 情绪（v0.19.2 大众认知补全：情绪操控系）----
            new PrototypeDef("ps_emotion_01", "煽情", "攻击恐惧眩晕目标，制造幻象", "精神系", "情绪",
                PrototypeTag.Fear, PrototypeTag.Illusion, 5, 3),
            new PrototypeDef("ps_emotion_02", "狂澜", "攻击狂暴提升，攫取灵魂回复", "精神系", "情绪",
                PrototypeTag.Berserk, PrototypeTag.Soul, 5, 6),
            new PrototypeDef("ps_emotion_03", "心潮", "攻击恐惧眩晕，受击反击震慑攻击者", "精神系", "情绪",
                PrototypeTag.Fear, PrototypeTag.Vibration, 5, 9),
            // ---- 梦境（v0.19.2 大众认知补全：梦境/梦魇系）----
            new PrototypeDef("ps_dream_01", "入梦", "攻击沉睡定身目标，扭曲时间减速", "精神系", "梦境",
                PrototypeTag.Sleep, PrototypeTag.TimeSlow, 4, 3),
            new PrototypeDef("ps_dream_02", "梦蚀", "攻击沉睡目标，诅咒持续折磨", "精神系", "梦境",
                PrototypeTag.Sleep, PrototypeTag.Curse, 4, 6),
            new PrototypeDef("ps_dream_03", "噩梦", "攻击沉睡定身，攫取灵魂回复", "精神系", "梦境",
                PrototypeTag.Sleep, PrototypeTag.Soul, 4, 9),
            // ---- 感知（v0.19.2 大众认知补全：痛觉共感与超感官）----
            new PrototypeDef("ps_sense_01", "共感", "心灵共感，感知目标弱点并精准暴击", "精神系", "感知",
                PrototypeTag.Telekinesis, PrototypeTag.Crit, 4, 3),
            new PrototypeDef("ps_sense_02", "剥夺", "攻击眩晕定身目标，削弱虚弱", "精神系", "感知",
                PrototypeTag.Stun, PrototypeTag.Weakness, 4, 5),
            new PrototypeDef("ps_sense_03", "过载", "攻击等离子灼烧，受击反击震慑攻击者", "精神系", "感知",
                PrototypeTag.Plasma, PrototypeTag.Vibration, 4, 7),
            new PrototypeDef("ps_sense_04", "穿刺", "攻击念力贯穿，攫取灵魂回复", "精神系", "感知",
                PrototypeTag.Telekinesis, PrototypeTag.Soul, 4, 8),
            new PrototypeDef("ps_mind_04", "思维加速", "思维超频，攻击强化暴击", "精神系", "心灵",
                PrototypeTag.Buff, PrototypeTag.Crit, 0, 4),
            new PrototypeDef("ps_mind_05", "精神鞭挞", "精神鞭挞，侵蚀心灵", "精神系", "心灵",
                PrototypeTag.Telekinesis, PrototypeTag.Poison, 0, 5),
            new PrototypeDef("ps_mind_06", "傀儡术", "精神操控，傀儡定身", "精神系", "操控",
                PrototypeTag.Telekinesis, PrototypeTag.Summon, 0, 8),
            new PrototypeDef("ps_mind_07", "记忆读取", "读取记忆，洞察弱点", "精神系", "感知",
                PrototypeTag.Telekinesis, PrototypeTag.Weakness, 0, 5),
            new PrototypeDef("ps_mind_08", "脑域开发", "开发脑域，强化念力", "精神系", "心灵",
                PrototypeTag.Buff, PrototypeTag.Telekinesis, 0, 4),
            new PrototypeDef("ps_mind_09", "精神分裂", "精神分裂，幻象连锁", "精神系", "心灵",
                PrototypeTag.Illusion, PrototypeTag.Chain, 0, 7),
            new PrototypeDef("ps_mind_10", "心灵传输", "心灵传输，瞬移念力", "精神系", "心灵",
                PrototypeTag.Teleport, PrototypeTag.Telekinesis, 0, 6),
            new PrototypeDef("ps_mind_11", "记忆抹除", "抹除目标记忆，汲取精神能量", "精神系", "心灵",
                PrototypeTag.Telekinesis, PrototypeTag.ManaDrain, 0, 7),
            new PrototypeDef("ps_mind_12", "意识入侵", "入侵意识，标记精神弱点", "精神系", "心灵",
                PrototypeTag.Telekinesis, PrototypeTag.Mark, 0, 6),
            new PrototypeDef("ps_mind_13", "心灵屏障", "心灵护壁，抵御精神攻击", "精神系", "心灵",
                PrototypeTag.Shield, PrototypeTag.Telekinesis, 0, 3),
        };
    }
}
