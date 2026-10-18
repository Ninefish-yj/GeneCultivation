// ============================================================
// 异能数据 · 光明系 / 黑暗系（v0.18.0）
// 大类：光明系（圣光/光刃/净化/晨曦）| 黑暗系（暗影/腐蚀/诅咒/夜幕）
// 描述 = 真实机制（由主/副标签组合表达），不含虚假效果。
// ============================================================

using System.Collections.Generic;

namespace Code.Realm.Prototype
{
    public static class PrototypeData_Light
    {
        public static readonly List<PrototypeDef> List = new List<PrototypeDef>
        {
            // ---- 圣光 ----
            new PrototypeDef("li_holy_01", "辉光", "攻击概率回复生命，护盾减伤", "光明系", "圣光",
                PrototypeTag.Light, PrototypeTag.Blessing, 3, 6),
            new PrototypeDef("li_holy_02", "裁决", "攻击念力贯穿高伤，溅射波及", "光明系", "圣光",
                PrototypeTag.Light, PrototypeTag.Execute, 3, 8),
            new PrototypeDef("li_holy_03", "圣盾", "受击回血，受击免伤", "光明系", "圣光",
                PrototypeTag.Shield, PrototypeTag.Light, 3, 9),
            new PrototypeDef("li_holy_04", "神恩", "圣光暴击，概率治愈友军", "光明系", "圣光",
                PrototypeTag.Blessing, PrototypeTag.Light, 3, 7),
            // ---- 光刃 ----
            new PrototypeDef("li_blade_01", "光刃", "攻击暴击破甲，念力贯穿", "光明系", "光刃",
                PrototypeTag.MultiShot, PrototypeTag.Light, 0, 4),
            new PrototypeDef("li_blade_02", "圣剑", "攻击溅射波及，概率眩晕", "光明系", "光刃",
                PrototypeTag.Light, PrototypeTag.ArmorBreak, 0, 7),
            new PrototypeDef("li_blade_03", "速斩", "攻击狂暴当次+25%，暴击", "光明系", "光刃",
                PrototypeTag.Execute, PrototypeTag.Light, 0, 9),
            // ---- 净化 ----
            new PrototypeDef("li_purify_01", "圣焰", "攻击附加灼烧（净化烈焰），溅射", "光明系", "净化",
                PrototypeTag.Light, PrototypeTag.Burn, 3, 4),
            new PrototypeDef("li_purify_02", "驱散", "圣光涤荡，驱散目标强化并净化诅咒", "光明系", "净化",
                PrototypeTag.Light, PrototypeTag.Mark, 3, 6),
            new PrototypeDef("li_purify_03", "圣水", "攻击概率回复生命，受击自愈", "光明系", "净化",
                PrototypeTag.ManaDrain, PrototypeTag.Light, 3, 9),
            // ---- 晨曦 ----
            new PrototypeDef("li_dawn_01", "晨曦", "攻击治疗强化，护盾减伤", "光明系", "晨曦",
                PrototypeTag.Light, PrototypeTag.Buff, 3, 3),
            new PrototypeDef("li_dawn_02", "破晓", "攻击暴击高伤，附加灼烧", "光明系", "晨曦",
                PrototypeTag.Light, PrototypeTag.Stun, 3, 6),
            new PrototypeDef("li_dawn_03", "黎明", "受击免伤强化，攻击回复生命", "光明系", "晨曦",
                PrototypeTag.Cleanse, PrototypeTag.Light, 3, 9),
            new PrototypeDef("li_holy_06", "圣光屏障", "圣光屏障，护盾祝福", "光明系", "圣光",
                PrototypeTag.Shield, PrototypeTag.Blessing, 0, 4),
            new PrototypeDef("li_holy_07", "光明印记", "圣光印记，光环普照", "光明系", "圣光",
                PrototypeTag.Light, PrototypeTag.Aura, 0, 5),
            new PrototypeDef("li_purify_04", "圣光净化", "圣光净化，再生治愈", "光明系", "净化",
                PrototypeTag.Light, PrototypeTag.Regeneration, 0, 5),
            new PrototypeDef("li_light_03", "天使之翼", "天使之翼，闪避腾空", "光明系", "光刃",
                PrototypeTag.Dodge, PrototypeTag.Light, 0, 3),
            new PrototypeDef("dk_night_04", "黑暗领域", "黑暗领域，领域减速", "黑暗系", "夜幕",
                PrototypeTag.Domain, PrototypeTag.Slow, 0, 7),
            new PrototypeDef("dk_corrode_08", "灵魂腐蚀", "灵魂腐蚀，侵蚀灵魂", "黑暗系", "腐蚀",
                PrototypeTag.Soul, PrototypeTag.Poison, 0, 6),
            new PrototypeDef("dk_curse_08", "死亡凝视", "死亡凝视，处决震慑", "黑暗系", "诅咒",
                PrototypeTag.Execute, PrototypeTag.Fear, 0, 8),
            new PrototypeDef("dk_shadow_04", "影袭", "影袭连斩，暗影多重", "黑暗系", "暗影",
                PrototypeTag.Shadow, PrototypeTag.MultiShot, 0, 4),
            new PrototypeDef("dk_night_05", "夜之帷幕", "夜幕帷幕，光环笼罩", "黑暗系", "夜幕",
                PrototypeTag.Shadow, PrototypeTag.Aura, 0, 6),
        };
    }

    public static class PrototypeData_Dark
    {
        public static readonly List<PrototypeDef> List = new List<PrototypeDef>
        {
            // ---- 暗影 ----
            new PrototypeDef("dk_shadow_01", "潜伏", "闪避强化，攻击暴击", "黑暗系", "暗影",
                PrototypeTag.Shadow, PrototypeTag.Crit, 5, 8),
            new PrototypeDef("dk_shadow_02", "突袭", "攻击吸血强化，念力贯穿", "黑暗系", "暗影",
                PrototypeTag.Shadow, PrototypeTag.Knockback, 5, 9),
            new PrototypeDef("dk_shadow_03", "影遁", "受击免伤最高，受击反击震慑攻击者", "黑暗系", "暗影",
                PrototypeTag.Shadow, PrototypeTag.Teleport, 5, 9),
            // ---- 腐蚀 ----
            new PrototypeDef("dk_corrode_01", "腐蚀", "攻击附加中毒持续伤害，诅咒加深", "黑暗系", "腐蚀",
                PrototypeTag.Poison, PrototypeTag.Curse, 5, 5),
            new PrototypeDef("dk_corrode_02", "溶解", "攻击附加灼烧持续伤害，溅射波及", "黑暗系", "腐蚀",
                PrototypeTag.Burn, PrototypeTag.Chain, 5, 7),
            new PrototypeDef("dk_corrode_03", "蚀甲", "攻击眩晕定身，破开护甲", "黑暗系", "腐蚀",
                PrototypeTag.Stun, PrototypeTag.ArmorBreak, 5, 9),
            new PrototypeDef("dk_corrode_04", "酸蚀", "中毒持续伤害，虚弱目标", "黑暗系", "腐蚀",
                PrototypeTag.Poison, PrototypeTag.Weakness, 5, 2),
            new PrototypeDef("dk_corrode_05", "腐沼", "中毒持续伤害，减速目标", "黑暗系", "腐蚀",
                PrototypeTag.Poison, PrototypeTag.Slow, 5, 4),
            new PrototypeDef("dk_corrode_06", "噬骨", "中毒持续伤害，衰老目标", "黑暗系", "腐蚀",
                PrototypeTag.Poison, PrototypeTag.Aging, 5, 6),
            new PrototypeDef("dk_corrode_07", "万蚀", "诅咒持续伤害，低血量斩杀", "黑暗系", "腐蚀",
                PrototypeTag.Curse, PrototypeTag.Execute, 5, 8),
            // ---- 诅咒 ----
            new PrototypeDef("dk_curse_01", "恶咒", "攻击附加诅咒持续伤害，恐惧眩晕", "黑暗系", "诅咒",
                PrototypeTag.Shadow, PrototypeTag.Sleep, 5, 4),
            new PrototypeDef("dk_curse_02", "死印", "攻击吸血，附加中毒", "血神系", "血咒",
                PrototypeTag.Shadow, PrototypeTag.Weakness, 5, 6),
            new PrototypeDef("dk_curse_03", "咒缚", "攻击减速时缓目标，概率眩晕", "黑暗系", "诅咒",
                PrototypeTag.Shadow, PrototypeTag.Chain, 5, 9),
            new PrototypeDef("dk_curse_04", "怨咒", "诅咒持续伤害，虚弱目标", "黑暗系", "诅咒",
                PrototypeTag.Shadow, PrototypeTag.Summon, 5, 3),
            new PrototypeDef("dk_curse_05", "恶咒缠身", "虚弱目标，减速目标", "黑暗系", "诅咒",
                PrototypeTag.Shadow, PrototypeTag.Entangle, 5, 5),
            new PrototypeDef("dk_curse_06", "死咒", "诅咒持续伤害，低血量斩杀", "黑暗系", "诅咒",
                PrototypeTag.Shadow, PrototypeTag.Aging, 5, 7),
            new PrototypeDef("dk_curse_07", "万咒归一", "诅咒持续伤害，破甲", "黑暗系", "诅咒",
                PrototypeTag.Shadow, PrototypeTag.ArmorBreak, 5, 8),
            // ---- 夜幕 ----
            new PrototypeDef("dk_night_01", "夜幕", "攻击恐惧眩晕目标，减速", "黑暗系", "夜幕",
                PrototypeTag.Shadow, PrototypeTag.Domain, 5, 5),
            new PrototypeDef("dk_night_02", "夜盾", "受击免伤，受击自愈回血", "黑暗系", "夜幕",
                PrototypeTag.Shadow, PrototypeTag.Shield, 5, 7),
            new PrototypeDef("dk_night_03", "夜行者", "闪避强化，攻击吸血", "黑暗系", "夜幕",
                PrototypeTag.Shadow, PrototypeTag.Invisibility, 5, 9),
        };
    }
}
