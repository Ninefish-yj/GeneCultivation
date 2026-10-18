// ============================================================
// 异能战斗风格判定（v0.21.23）
// 单一职责：按异能 MainTag+SubTag 判定攻击距离意图——
//   投射系（火水风雷暗毒光音命运…）= 远程拉开打
//   近战系（金血兽狂…）= 贴脸压制
//   控制系（束缚/减速/定身/恐惧…）= 中距游走
// 供 CombatMoveDirector（动作化站位）与 CombatStanceAI（真走位）共用，
// 消除两处重复的标签集合。
// ============================================================
using System;
using System.Collections.Generic;
using Code.Realm.Prototype;

namespace Code.Combat.AI
{
    public enum CombatStance
    {
        Ranged,   // 投射：保持 3~5 格距离
        Melee,    // 近战：贴脸 0~1.5 格
        Control   // 控制：中距游走 2.5~4 格
    }

    public static class CombatStyle
    {
        private static readonly HashSet<string> _rangedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Fire","Water","Wind","Thunder","Shadow","Curse","Poison","Frost","Frostbite","Slow",
            "Light","Sound","Magnetism","Telekinesis","TimeSlow","Fate","Gravity","Vibration",
            "Illusion","Sleep","Charm","Fear","Aura","Chain","MultiShot","Clone","Buff","Debuff",
            "ManaDrain","ElementResist","Plasma","Holy","Soul","Berserk"
        };

        private static readonly HashSet<string> _controlTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Entangle","Stun","Petrify","Fear","Sleep","Charm","Illusion","TimeSlow","Slow",
            "Aging","Weakness","ArmorBreak","Curse","Cleanse","Aura"
        };

        private static readonly HashSet<string> _meleeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Metal","Blood","Beast","Berserk","MultiShot","Drain","Lifesteal","Thorns","Execute",
            "Sound","Reflect"
        };

        /// <summary>该异能的战斗站位风格。</summary>
        public static CombatStance StanceOf(PrototypeDef def)
        {
            if (def == null) return CombatStance.Melee;
            string main = def.MainTag.ToString();
            string sub = def.SubTag.ToString();
            // 控制系优先（缠绕/定身/恐惧类异能需要中距游走放控制）
            if (_controlTags.Contains(main) || _controlTags.Contains(sub)) return CombatStance.Control;
            if (_rangedTags.Contains(main) || _rangedTags.Contains(sub)) return CombatStance.Ranged;
            return CombatStance.Melee;
        }

        /// <summary>该单位的异能风格是否投射/远程（CombatMoveDirector 动作化站位复用）。</summary>
        public static bool IsRanged(Actor by)
        {
            try
            {
                return StanceOf(PrototypeRegistry.GetForActor(by)) == CombatStance.Ranged;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>理想战斗距离（世界单位）：投射 4、控制 3、近战 0.6。</summary>
        public static float IdealDistance(CombatStance stance)
        {
            switch (stance)
            {
                case CombatStance.Ranged: return 4f;
                case CombatStance.Control: return 3f;
                default: return 0.6f;
            }
        }
    }
}
