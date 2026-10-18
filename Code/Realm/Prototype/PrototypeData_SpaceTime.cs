// ============================================================
// 异能数据 · 时空系（v0.18.0）
// 大类：时空系 | 小类：时间/空间/次元/引力/速度
// 描述 = 真实机制（由主/副标签组合表达），不含虚假效果。
// ============================================================

using System.Collections.Generic;

namespace Code.Realm.Prototype
{
    public static class PrototypeData_SpaceTime
    {
        public static readonly List<PrototypeDef> List = new List<PrototypeDef>
        {
            // ---- 时间 ----
            new PrototypeDef("st_time_01", "加速", "时间加速，强化自身并多重连击", "时空系", "时间",
                PrototypeTag.Buff, PrototypeTag.MultiShot, 4, 1),
            new PrototypeDef("st_time_02", "时停", "时间凝滞，强减速并定身目标", "时空系", "时间",
                PrototypeTag.TimeSlow, PrototypeTag.Stun, 4, 5),
            new PrototypeDef("st_time_03", "回溯", "受击自愈回血，攻击治疗", "时空系", "时间",
                PrototypeTag.Regeneration, PrototypeTag.Heal, 4, 9),
            // ---- 空间 ----
            new PrototypeDef("st_space_01", "瞬移", "受击概率瞬移遁走免伤，幻象闪避", "时空系", "空间",
                PrototypeTag.Illusion, PrototypeTag.Teleport, 4, 3),
            new PrototypeDef("st_space_02", "空间斩", "攻击念力贯穿高伤，溅射波及", "时空系", "空间",
                PrototypeTag.Telekinesis, PrototypeTag.Chain, 4, 6),
            new PrototypeDef("st_space_03", "壁垒", "护盾减伤，受击免伤", "时空系", "空间",
                PrototypeTag.Shield, PrototypeTag.Dodge, 4, 9),
            // ---- 次元 ----
            new PrototypeDef("st_dimension_01", "裂缝", "撕裂空间裂隙，连锁波及敌人", "时空系", "次元",
                PrototypeTag.Teleport, PrototypeTag.Chain, 6, 1),
            new PrototypeDef("st_dimension_02", "折叠", "攻击重力压制目标，恐惧眩晕", "时空系", "次元",
                PrototypeTag.Gravity, PrototypeTag.Chain, 6, 5),
            new PrototypeDef("st_dimension_03", "相位", "受击免伤最高，受击回血", "时空系", "次元",
                PrototypeTag.Invisibility, PrototypeTag.Blessing, 6, 9),
            // ---- 引力 ----
            new PrototypeDef("st_gravity_01", "御重", "攻击重力压制，溅射波及", "时空系", "引力",
                PrototypeTag.Gravity, PrototypeTag.Slow, 6, 4),
            new PrototypeDef("st_gravity_02", "重力场", "攻击眩晕定身目标，减速", "时空系", "引力",
                PrototypeTag.Gravity, PrototypeTag.Domain, 6, 6),
            new PrototypeDef("st_gravity_03", "引壁", "护盾减伤，受击反击麻痹攻击者", "时空系", "引力",
                PrototypeTag.Gravity, PrototypeTag.Blessing, 6, 8),
            new PrototypeDef("st_gravity_04", "失重", "重力压制，减速目标", "时空系", "引力",
                PrototypeTag.Gravity, PrototypeTag.Dodge, 6, 5),
            new PrototypeDef("st_gravity_05", "引力坍缩", "重力压制，震退定身", "时空系", "引力",
                PrototypeTag.Gravity, PrototypeTag.ManaDrain, 6, 7),
            new PrototypeDef("st_gravity_06", "黑洞", "重力压制，低血量湮灭", "时空系", "引力",
                PrototypeTag.Gravity, PrototypeTag.Execute, 6, 9),
            // ---- 速度 ----
            new PrototypeDef("st_speed_01", "极速", "身法闪避，攻击暴击", "时空系", "速度",
                PrototypeTag.Dodge, PrototypeTag.Crit, 1, 1),
            new PrototypeDef("st_speed_02", "瞬连", "攻击暴击高伤，连击爆发", "时空系", "速度",
                PrototypeTag.Crit, PrototypeTag.MultiShot, 1, 5),
            new PrototypeDef("st_speed_03", "音速", "攻击击退定身，溅射波及", "时空系", "速度",
                PrototypeTag.Knockback, PrototypeTag.Chain, 1, 9),
            new PrototypeDef("st_time_04", "时间倒流", "时间倒流，回溯再生", "时空系", "时间",
                PrototypeTag.TimeSlow, PrototypeTag.Regeneration, 0, 7),
            new PrototypeDef("st_time_05", "时间护盾", "时间护盾，缓滞伤害", "时空系", "时间",
                PrototypeTag.TimeSlow, PrototypeTag.Shield, 0, 4),
            new PrototypeDef("st_time_06", "时之沙漏", "时之沙漏，汲取时间能量", "时空系", "时间",
                PrototypeTag.TimeSlow, PrototypeTag.ManaDrain, 0, 5),
            new PrototypeDef("st_dimension_04", "空间门", "开启空间门，召唤援军", "时空系", "空间",
                PrototypeTag.Teleport, PrototypeTag.Summon, 0, 5),
            new PrototypeDef("st_dimension_05", "次元斩", "次元斩击，空间连斩", "时空系", "次元",
                PrototypeTag.Teleport, PrototypeTag.MultiShot, 0, 6),
            new PrototypeDef("st_dimension_06", "空间锚定", "空间锚定，禁锢目标", "时空系", "空间",
                PrototypeTag.Teleport, PrototypeTag.Stun, 0, 4),
            new PrototypeDef("st_dimension_07", "维度牢笼", "维度牢笼，领域束缚", "时空系", "次元",
                PrototypeTag.Domain, PrototypeTag.Entangle, 0, 7),
            new PrototypeDef("st_gravity_07", "引力弹", "引力弹，暴击轰击", "时空系", "引力",
                PrototypeTag.Gravity, PrototypeTag.Crit, 0, 3),
            new PrototypeDef("st_dimension_08", "空间口袋", "空间口袋，收纳强化", "时空系", "空间",
                PrototypeTag.Teleport, PrototypeTag.Buff, 0, 3),
            new PrototypeDef("st_gravity_08", "引力护盾", "引力护盾，扭曲伤害", "时空系", "引力",
                PrototypeTag.Gravity, PrototypeTag.Shield, 0, 4),
        };
    }
}
