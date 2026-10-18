// ============================================================
// 招式定义（v0.21.0）
// 招式 = 动画语言的"词汇"。纯数据类（单一职责：描述招式），
// 不包含任何执行逻辑——执行交给 CombatMoveDirector。
//
// 设计目标：贴合超神机械师世界观与 14 大类异能体系，
// 招式的名称/意象/轻重/地形破坏全部自洽，与「战斗之神」
// 招式名（CrescentFang/HeavenSplitter 等）无撞名、无素材复用。
// ============================================================
using System.Collections.Generic;

namespace Code.Combat.Moves
{
    /// <summary>招式轻重（对应 Ruin 思路里的"这一击有多重"，三档足矣）。</summary>
    public enum MoveWeight
    {
        Light = 0,      // 轻击：试探/消耗，基本不破坏地形
        Heavy = 1,      // 重击：主力招式，破地形
        Ultimate = 2    // 终极：大招，大范围破坏
    }

    /// <summary>招式动作类型（决定动作编排与特效组合）。</summary>
    public enum MoveKind
    {
        Lunge = 0,      // 突进
        Slash = 1,      // 斩击
        Strike = 2,     // 重击
        Chain = 3,      // 连锁
        Domain = 4,     // 领域
        Blast = 5,      // 爆裂
        Beam = 6,       // 光束
        Summon = 7,     // 召唤
        Heal = 8,       // 治愈
        Buff = 9        // 强化
    }

    /// <summary>地形破坏形态（与 DSTerrainDestruction 联动，None=不破坏）。</summary>
    public enum MoveTerrain
    {
        None = 0,       // 不破坏
        Scorch = 1,     // 烧焦（轻火系）
        Crater = 2,     // 坑（重击/爆裂）
        Furrow = 3,     // 裂缝（直线沟壑，土/空间系）
        Magma = 4       // 岩浆（火/血神/超神级）
    }

    /// <summary>
    /// 招式定义：一条招式 = 动作类型 + 轻重 + 地形破坏形态 + 适配异能标签集。
    /// 数据驱动：新增招式只在此处加一行，执行与 AI 均通过注册表读取。
    /// </summary>
    public sealed class CombatMove
    {
        /// <summary>招式唯一标识（英文 id，用于调试/冷却）。</summary>
        public string Id;

        /// <summary>招式名（世界观中文名）。</summary>
        public string Name;

        /// <summary>轻重。</summary>
        public MoveWeight Weight;

        /// <summary>动作类型。</summary>
        public MoveKind Kind;

        /// <summary>地形破坏形态。</summary>
        public MoveTerrain Terrain;

        /// <summary>适配的异能标签（PrototypeTag 名，用字符串避免硬依赖枚举顺序）。</summary>
        public HashSet<string> Tags;

        /// <summary>最低境界 tier（低于此不选用）。</summary>
        public int MinTier;

        /// <summary>基础冷却（秒）。</summary>
        public float Cooldown;

        /// <summary>招式描述（世界观一句话）。</summary>
        public string Desc;

        public CombatMove(string id, string name, MoveWeight weight, MoveKind kind,
            MoveTerrain terrain, int minTier, float cooldown, string desc, params string[] tags)
        {
            Id = id;
            Name = name;
            Weight = weight;
            Kind = kind;
            Terrain = terrain;
            MinTier = minTier;
            Cooldown = cooldown;
            Desc = desc;
            Tags = new HashSet<string>(tags);
        }
    }
}
