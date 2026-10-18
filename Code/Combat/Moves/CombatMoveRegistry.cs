// ============================================================
// 招式注册表（v0.21.0）
// 所有世界观招式的唯一数据源（单一职责：数据与匹配）。
// 招式与异能标签的对应关系集中在这里，AI 只问"该用哪招"。
// 名称全部为超神机械师世界观原创（无撞名 Combat Gods 招式）。
// ============================================================
using System.Collections.Generic;
using Code.Realm.Prototype;

namespace Code.Combat.Moves
{
    public static class CombatMoveRegistry
    {
        private static readonly List<CombatMove> _all = new List<CombatMove>();

        /// <summary>异能标签 → 最贴合的招式（数据驱动匹配表）。</summary>
        private static readonly Dictionary<string, string> _tagToMove = new Dictionary<string, string>();

        /// <summary>全部招式（只读）。</summary>
        public static IReadOnlyList<CombatMove> All => _all;

        static CombatMoveRegistry()
        {
            // ---- 元素系 ----
            Register(new CombatMove("mv_fire_blast", "焰爆术", MoveWeight.Heavy, MoveKind.Blast,
                MoveTerrain.Magma, 4, 6f,
                "凝聚烈焰轰爆大地，灼烧成岩浆（火系重击）。", "Plasma", "Burn"));
            Register(new CombatMove("mv_ice_nova", "冰霜新星", MoveWeight.Heavy, MoveKind.Blast,
                MoveTerrain.Crater, 4, 7f,
                "冰霜以自身为中心炸开，地面崩裂成坑（冰系重击）。", "Frost", "Frostbite"));
            Register(new CombatMove("mv_thunder_judge", "雷劫轰顶", MoveWeight.Heavy, MoveKind.Strike,
                MoveTerrain.Scorch, 5, 8f,
                "引天雷劈落，焦痕蔓延（雷系重击）。", "Thunder", "Chain"));
            Register(new CombatMove("mv_earth_rend", "大地崩裂", MoveWeight.Heavy, MoveKind.Strike,
                MoveTerrain.Furrow, 3, 7f,
                "一拳砸裂大地，裂缝沿直线蔓延（土系重击）。", "Earth", "Gravity", "Knockback"));
            Register(new CombatMove("mv_wind_reap", "风镰回旋", MoveWeight.Light, MoveKind.Slash,
                MoveTerrain.None, 2, 3f,
                "风刃回旋收割，轻快连斩（风系轻击）。", "Wind", "Crit", "MultiShot"));
            Register(new CombatMove("mv_water_prison", "水牢困锁", MoveWeight.Light, MoveKind.Domain,
                MoveTerrain.None, 2, 4f,
                "水幕合拢成牢，困锁目标（水系控制）。", "Water", "Entangle", "Slow"));
            // ---- 血神系 ----
            Register(new CombatMove("mv_blood_moon", "血月斩", MoveWeight.Heavy, MoveKind.Slash,
                MoveTerrain.Crater, 5, 8f,
                "血月弯刃横斩，饮血成坑（血神系重击）。", "Blood", "Lifesteal", "Berserk"));
            // ---- 机械系 ----
            Register(new CombatMove("mv_metal_storm", "机械风暴", MoveWeight.Heavy, MoveKind.Chain,
                MoveTerrain.Scorch, 4, 7f,
                "金属碎片风暴连锁扫射（机械系重击）。", "Metal", "Magnetism", "Chain"));
            // ---- 时空系 ----
            Register(new CombatMove("mv_space_tear", "空间撕裂", MoveWeight.Ultimate, MoveKind.Beam,
                MoveTerrain.Furrow, 9, 20f,
                "撕裂空间，裂缝吞噬大地（时空系终极）。", "Teleport", "Gravity", "Execute"));
            Register(new CombatMove("mv_time_freeze", "时间凝滞", MoveWeight.Ultimate, MoveKind.Domain,
                MoveTerrain.None, 9, 22f,
                "领域内时间凝滞，万物迟滞（时间系终极）。", "TimeSlow", "Domain"));
            // ---- 召唤系 ----
            Register(new CombatMove("mv_call_legion", "万灵召唤", MoveWeight.Heavy, MoveKind.Summon,
                MoveTerrain.None, 4, 9f,
                "开启万灵之门，召出助战军团（召唤系重击）。", "Summon"));
            // ---- 光明/黑暗系 ----
            Register(new CombatMove("mv_holy_judgment", "圣光审判", MoveWeight.Ultimate, MoveKind.Beam,
                MoveTerrain.Scorch, 10, 24f,
                "圣光天罚贯穿，焦土百里（光明系终极）。", "Light", "Execute", "Cleanse"));
            Register(new CombatMove("mv_shadow_veil", "暗影降临", MoveWeight.Heavy, MoveKind.Domain,
                MoveTerrain.Crater, 6, 10f,
                "夜幕笼罩，暗影侵蚀大地（黑暗系重击）。", "Shadow", "Domain", "Fear"));
            // ---- 精神/命运/死亡系 ----
            Register(new CombatMove("mv_mind_storm", "精神风暴", MoveWeight.Heavy, MoveKind.Domain,
                MoveTerrain.None, 6, 9f,
                "精神风暴席卷全场（精神系重击）。", "Telekinesis", "Illusion", "Fear"));
            Register(new CombatMove("mv_fate_verdict", "命运裁决", MoveWeight.Ultimate, MoveKind.Strike,
                MoveTerrain.Magma, 11, 26f,
                "裁决落下，因果崩塌成熔岩（命运系终极）。", "Curse", "Reflect", "Execute"));
            Register(new CombatMove("mv_soul_harvest", "灵魂收割", MoveWeight.Heavy, MoveKind.Slash,
                MoveTerrain.Crater, 7, 11f,
                "收割灵魂，地面随镰刀崩裂（死亡系重击）。", "Soul", "Execute", "Aging"));

            // ---- v0.21.11 补全：强化/血肉/自然系（世界观原创命名）----
            Register(new CombatMove("mv_beast_frenzy", "狂兽冲撞", MoveWeight.Light, MoveKind.Lunge,
                MoveTerrain.None, 3, 3.5f,
                "兽性觉醒，四肢伏地猛冲撕咬（强化系突进）。", "Berserk", "MultiShot"));
            Register(new CombatMove("mv_steel_guard", "金刚护体", MoveWeight.Heavy, MoveKind.Buff,
                MoveTerrain.None, 4, 8f,
                "钢铁意志外放，金芒护体震荡（强化/机械系防守）。", "Buff", "Shield", "Metal"));
            Register(new CombatMove("mv_blood_sacrifice", "血祭爆发", MoveWeight.Heavy, MoveKind.Blast,
                MoveTerrain.Crater, 6, 10f,
                "燃烧自身鲜血引爆周身，血浪成坑（血肉系重击）。", "Blood", "Drain", "Berserk"));
            Register(new CombatMove("mv_regen_field", "再生领域", MoveWeight.Light, MoveKind.Domain,
                MoveTerrain.None, 5, 6f,
                "脚下藤蔓生根、绿光回涌，领域内快速再生（血肉/自然系领域）。", "Regeneration", "Heal", "HealAlly"));
            Register(new CombatMove("mv_thorn_bind", "荆棘囚笼", MoveWeight.Heavy, MoveKind.Domain,
                MoveTerrain.None, 3, 7f,
                "荆棘破土合拢成笼，困锁目标（自然系控制）。", "Thorns", "Entangle"));
            Register(new CombatMove("mv_wood_whiplash", "藤蔓鞭击", MoveWeight.Light, MoveKind.Chain,
                MoveTerrain.None, 2, 3f,
                "藤蔓长鞭连抽，缚缠横扫（自然系轻击）。", "Thorns", "Entangle", "Water"));

            // ---- 标签 → 招式映射（标签优先，多个标签取权重最大的招）----
            _tagToMove["Plasma"] = "mv_fire_blast";
            _tagToMove["Burn"] = "mv_fire_blast";
            _tagToMove["Frost"] = "mv_ice_nova";
            _tagToMove["Frostbite"] = "mv_ice_nova";
            _tagToMove["Thunder"] = "mv_thunder_judge";
            _tagToMove["Earth"] = "mv_earth_rend";
            _tagToMove["Gravity"] = "mv_earth_rend";
            _tagToMove["Wind"] = "mv_wind_reap";
            _tagToMove["Crit"] = "mv_wind_reap";
            _tagToMove["Water"] = "mv_water_prison";
            _tagToMove["Entangle"] = "mv_water_prison";
            _tagToMove["Blood"] = "mv_blood_moon";
            _tagToMove["Lifesteal"] = "mv_blood_moon";
            _tagToMove["Metal"] = "mv_metal_storm";
            _tagToMove["Magnetism"] = "mv_metal_storm";
            _tagToMove["Teleport"] = "mv_space_tear";
            _tagToMove["Space"] = "mv_space_tear";
            _tagToMove["TimeSlow"] = "mv_time_freeze";
            _tagToMove["Summon"] = "mv_call_legion";
            _tagToMove["Light"] = "mv_holy_judgment";
            _tagToMove["Shadow"] = "mv_shadow_veil";
            _tagToMove["Telekinesis"] = "mv_mind_storm";
            _tagToMove["Illusion"] = "mv_mind_storm";
            _tagToMove["Curse"] = "mv_fate_verdict";
            _tagToMove["Reflect"] = "mv_fate_verdict";
            _tagToMove["Soul"] = "mv_soul_harvest";
            _tagToMove["Aging"] = "mv_soul_harvest";
            // v0.21.11 补全映射：强化/血肉/自然系
            _tagToMove["Berserk"] = "mv_beast_frenzy";
            _tagToMove["MultiShot"] = "mv_beast_frenzy";
            _tagToMove["Buff"] = "mv_steel_guard";
            _tagToMove["Shield"] = "mv_steel_guard";
            _tagToMove["Drain"] = "mv_blood_sacrifice";
            _tagToMove["Regeneration"] = "mv_regen_field";
            _tagToMove["Heal"] = "mv_regen_field";
            _tagToMove["HealAlly"] = "mv_regen_field";
            _tagToMove["Thorns"] = "mv_thorn_bind";
        }

        private static void Register(CombatMove move)
        {
            _all.Add(move);
        }

        /// <summary>按 id 取招式。</summary>
        public static CombatMove Get(string id)
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i].Id == id) return _all[i];
            return null;
        }

        /// <summary>按异能标签选招式：标签命中越多越优先；同级取更重的一招。</summary>
        public static CombatMove PickForTags(IEnumerable<PrototypeTag> tags, int tier)
        {
            CombatMove best = null;
            int bestHits = 0;
            foreach (var m in _all)
            {
                if (tier < m.MinTier) continue;
                int hits = 0;
                foreach (var t in tags)
                {
                    string tn = t.ToString();
                    if (m.Tags.Contains(tn)) hits++;
                }
                if (hits == 0) continue;
                if (best == null || hits > bestHits ||
                    (hits == bestHits && (int)m.Weight > (int)best.Weight))
                {
                    best = m;
                    bestHits = hits;
                }
            }
            return best;
        }

        /// <summary>按异能 id 选招式（查该异能的标签组合）。</summary>
        public static CombatMove PickForAbility(PrototypeDef def)
        {
            if (def == null) return null;
            return PickForTags(new[] { def.MainTag, def.SubTag }, def.Tier);
        }

        /// <summary>取一个势力的默认通用招式（无标签命中时的兜底）。</summary>
        public static CombatMove DefaultMove(int tier)
        {
            return tier >= 9 ? Get("mv_space_tear") : Get("mv_wind_reap");
        }
    }
}
