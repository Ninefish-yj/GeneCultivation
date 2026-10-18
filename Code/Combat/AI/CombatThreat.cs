// ============================================================
// 战斗AI·威胁评估（v0.21.0）
// 单一职责：量化"这个敌人对施法者有多大威胁"。
// 设计参考「战斗之神」Threat 的机制思路（生命×输出×境界 → 威胁值），
// 公式与字段为本项目原创（用我方可靠属性：health/maxHealth/level/境界）。
// 不持有状态、无副作用——纯函数，AI 决策器调用。
// ============================================================
using UnityEngine;

namespace Code.Combat.AI
{
    public static class CombatThreat
    {
        /// <summary>
        /// 单单位威胁值：有效生命 × 输出能力（等级+境界） × 境界倍增。
        /// 攻高血厚者威胁大；高境界单位威胁不可忽视。
        /// </summary>
        public static float Value(Actor a)
        {
            if (a == null || !a.isAlive()) return 0f;
            float hp = Mathf.Max(0f, a.data.health);
            float maxHp = Mathf.Max(1f, a.getMaxHealth());
            int level = Mathf.Max(1, a.data.level);
            int tier = Code.Data.CultivationData.GetRealmTier(a);
            float might = 1f + tier * 0.35f;                     // 境界倍增
            float effectiveHp = Mathf.Max(1f, hp * (0.6f + 0.6f * (hp / maxHp)));
            float output = level * (0.8f + tier * 0.25f);        // 输出能力近似（等级×境界）
            return Mathf.Sqrt(effectiveHp) * Mathf.Sqrt(output) * might;
        }

        /// <summary>威胁比（我方=分母）：&gt;1 表示敌人强于我方。</summary>
        public static float Ratio(Actor self, Actor enemy)
        {
            float mine = Value(self);
            float theirs = Value(enemy);
            if (mine <= 0f) return 10f;
            return theirs / mine;
        }

        /// <summary>
        /// 该不该用大招（残血拼命 + 敌方威胁压倒性）：
        /// 自身 HP&lt;30% 或 威胁比≥2 时倾向终极招式。
        /// </summary>
        public static bool WantsUltimate(Actor self, Actor enemy)
        {
            if (self == null || !self.isAlive()) return false;
            float hpPct = self.data.health / Mathf.Max(1f, self.getMaxHealth());
            if (hpPct < 0.3f) return true;
            if (enemy != null && Ratio(self, enemy) >= 2f) return true;
            return false;
        }

        /// <summary>近战还是远程倾向：距离阈值按威胁比调整（敌方强→敢于贴身拼）。</summary>
        public static bool WantsEngage(Actor self, Actor enemy, float dist)
        {
            if (self == null || enemy == null) return false;
            float ratio = Ratio(self, enemy);
            float threshold = 2.2f + (ratio > 1.5f ? 1.2f : 0f);
            return dist < threshold;
        }
    }
}
