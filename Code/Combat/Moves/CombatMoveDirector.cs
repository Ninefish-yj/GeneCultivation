// ============================================================
// 招式执行器（v0.21.0）
// 编排层（单一职责：把 CombatMove 变成一段有节奏的演出）：
//   蓄势(charge) → 主击(strike) → 收尾(follow)
// 动作/地形/音效全部复用现有表现层（CombatFxDirector /
// DSTerrainDestruction / CombatFxCore / SummonEngine），
// 本类不新增任何素材——只负责"编排"与"冷却"。
//
// 招式冷却：静态表，AI 用 CanUse/GetCooldown 决定出招；
// Tick() 每帧驱动（由 CombatFxFramePatch 调用）。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using Code.Combat.Fx;
using Code.Data;
using Code.Realm.Prototype;

namespace Code.Combat.Moves
{
    public static class CombatMoveDirector
    {
        /// <summary>招式冷却表（actor → (moveId → 剩余秒)）。</summary>
        private static readonly Dictionary<Actor, Dictionary<string, float>> _cooldowns =
            new Dictionary<Actor, Dictionary<string, float>>();

        private const float MaxFullZoom = 60f;

        private static bool CanFull(Vector2 at)
        {
            if (World.world == null || World.world.isPaused()) return false;
            Camera cam = Camera.main;
            if (cam == null || cam.orthographicSize > MaxFullZoom) return false;
            return CombatFxCore.OnScreen(at, 0.1f);
        }

        // ------------------------------------------------------ 对外接口 --

        /// <summary>该招式对目标是否可用（境界达标 + 冷却结束）。</summary>
        public static bool CanUse(Actor by, CombatMove move)
        {
            if (by == null || !by.isAlive() || move == null) return false;
            if (CultivationData.GetRealmTier(by) < move.MinTier) return false;
            return !IsOnCooldown(by, move.Id);
        }

        /// <summary>招式剩余冷却（秒，0=可用）。</summary>
        public static float GetCooldown(Actor by, CombatMove move)
        {
            if (by == null || move == null) return 0f;
            if (!_cooldowns.TryGetValue(by, out var map)) return 0f;
            return map.TryGetValue(move.Id, out float v) ? Mathf.Max(0f, v) : 0f;
        }

        private static bool IsOnCooldown(Actor by, string moveId)
        {
            if (!_cooldowns.TryGetValue(by, out var map)) return false;
            return map.TryGetValue(moveId, out float v) && v > 0f;
        }

        // ================= v0.21.28 觉醒衔击 =================
        /// <summary>每个单位最近执行的招式（供 getHit 查衔击加成/反震归属）。</summary>
        private static readonly Dictionary<Actor, CombatMove> _lastMove = new Dictionary<Actor, CombatMove>();

        /// <summary>单位最近一次使用的招式（无则 null）。</summary>
        public static CombatMove LastMoveOf(Actor by)
        {
            if (by == null) return null;
            CombatMove m;
            return _lastMove.TryGetValue(by, out m) ? m : null;
        }

        /// <summary>
        /// 执行招式（编排：蓄势光效 → 主击动作+地形破坏 → 收尾/冷却）。
        /// 全部包 try，演出失败不影响战斗逻辑。
        /// </summary>
        public static bool Execute(CombatMove move, Actor by, Actor target)
        {
            try
            {
                if (!CanUse(by, move)) return false;
                if (by.current_position == null) return false;
                Vector2 pos = by.current_position;
                Vector2 targetPos = target != null && target.isAlive()
                    ? target.current_position
                    : pos + new Vector2(1f, 0f);
                Execute(move, by, pos, targetPos, target);
                return true;
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[MoveDirector] Execute(actor): " + e.Message);
                return false;
            }
        }

        /// <summary>执行招式（显式坐标版本，AI 用目标格、事件用技能格）。</summary>
        public static void Execute(CombatMove move, Actor by, Vector2 pos, Vector2 targetPos, Actor target = null)
        {
            try
            {
                if (move == null || by == null || !by.isAlive()) return;
                int tier = CultivationData.GetRealmTier(by);
                // v0.21.28 记录最近招式（衔击伤害放大/反震归属查询）
                if (move.Id != null) _lastMove[by] = move;
                // 蓄势：中心光斑（不同轻重不同强度）
                Charge(move, by, pos, tier);
                // 主击：动作 + 地形破坏
                Strike(move, by, pos, targetPos, tier, target);
                // 收尾：冷却登记
                PutOnCooldown(by, move.Id, move.Cooldown);
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[MoveDirector] Execute: " + e.Message);
            }
        }

        // ------------------------------------------------------ 编排分段 --

        /// <summary>蓄势：招式起手光效（轻重决定光斑强度）。</summary>
        private static void Charge(CombatMove move, Actor by, Vector2 pos, int tier)
        {
            if (!CanFull(pos)) return;
            pos.y += 0.17f;
            Color c = TierColor(tier);
            // v0.21.30 蓄势光斑重标：px0.08×100×size → 世界宽 0.32/0.2（单位 0.9 宽的三分之一，小光斑）
            float chargeSize = move.Weight == MoveWeight.Ultimate ? 0.04f : 0.025f;
            CombatFxCore.Spawn(CombatFxArt.Burst, pos, 0.08f, 0.3f, FxLayer.Over, 28)
                .Color(c, new Color(c.r, c.g, c.b, 0f))
                .Sized(chargeSize, chargeSize * 0.38f);
        }

        /// <summary>主击：动作姿态 + 按动作类型分派到现有动作 + 地形破坏。</summary>
        private static void Strike(CombatMove move, Actor by, Vector2 pos, Vector2 targetPos, int tier, Actor target = null)
        {
            bool visible = CanFull(targetPos);
            // v0.21.11 动画语言分层：重招/终极给击停+慢动作（神战级顿挫节奏）；
            // 凡级以下（tier<6）只做普通节奏——低境界搏杀、高境界毁天灭地的观感梯度
            CinematicBeat(move, by, targetPos, tier);
            // v0.21.5 单位本体动作姿态（纯表现不碰 AI）：先让单位"做出动作"再叠特效
            // v0.21.22 动作幅度全面强化（像素游戏 0.2s/0.3格位移根本看不见）+
            //         按异能派系差异化（投射系=后跳拉开距离、近战系=突进贴脸压制）——
            //         纯视觉位移实现"战斗散开"，零 AI 干预、零崩溃风险
            Vector2 dir = targetPos - pos;
            if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(1f, 0f);
            dir.Normalize();
            bool rangedStyle = IsRangedStyle(by);   // 异能派系 → 攻击距离意图
            switch (move.Kind)
            {
                case MoveKind.Lunge:
                    // 近战突进：幅度×3、时长加长——"贴脸压制"肉眼可见
                    float d1 = Mathf.Min(1.6f, Vector2.Distance(pos, targetPos) * 0.7f);
                    CombatMovePose.Dash(by, dir, d1, 0.34f);
                    if (visible) CombatFxDirector.DashStrike(by, targetPos);
                    break;
                case MoveKind.Slash:
                    // 挥击：幅度×2；投射系反向后跳（拉开），近战正向挥砍
                    if (rangedStyle)
                        CombatMovePose.Recoil(by, -dir, 0.45f, 0.28f);
                    else
                        CombatMovePose.Swing(by, dir, 0.45f, 0.3f);
                    if (visible) CombatFxDirector.SlashArc(by, targetPos);
                    break;
                case MoveKind.Strike:
                    // 下砸：幅度×2.3 + 顿挫
                    float d2 = Mathf.Min(0.9f, Vector2.Distance(pos, targetPos) * 0.55f);
                    CombatMovePose.Slam(by, dir, d2, 0.34f);
                    if (visible) CombatFxDirector.GroundSlam(by, targetPos);
                    break;
                case MoveKind.Chain:
                    if (targetPos.y == pos.y && targetPos.x == pos.x) break;
                    // 链式跳跃位移加大（电链/血链的"跳一下"感）
                    CombatMovePose.Nudge(by, dir, 0.2f);
                    if (visible) CombatFxDirector.ChainArc(by);
                    break;
                case MoveKind.Domain:
                    // 腾空展开领域：更高更长
                    CombatMovePose.Lift(by, 0.5f, 0.7f);
                    float r = 1.2f + Mathf.Min(4f, tier * 0.25f);
                    if (visible) CombatFxDirector.DomainCircle(targetPos, r, TierColor(tier));
                    break;
                case MoveKind.Blast:
                    // 起跳引爆 + 反向后坐：投射系后跳幅度更大（远程施法拉开身位）
                    CombatMovePose.Lift(by, 0.26f, 0.34f);
                    CombatMovePose.Recoil(by, -dir, rangedStyle ? 0.55f : 0.28f, 0.3f);
                    if (visible)
                    {
                        CombatFxDirector.Shockwave(targetPos, 3f + tier * 0.8f);
                        CombatFxDirector.SpellBurst(by, targetPos, tier);
                    }
                    break;
                case MoveKind.Beam:
                    // 光束后坐：投射系拉开（施法者保持远程身位）
                    CombatMovePose.Recoil(by, -dir, rangedStyle ? 0.5f : 0.3f, 0.3f);
                    BeamVisual(by, pos, targetPos, tier);
                    break;
                case MoveKind.Summon:
                    CombatMovePose.Lift(by, 0.32f, 0.45f);          // 召唤腾身
                    // 召唤：目标格唤出助战灵兽（复用召唤引擎）
                    TrySummonFor(by, targetPos, tier);
                    if (visible) CombatFxDirector.DomainCircle(targetPos, 1.6f, TierColor(tier));
                    break;
                case MoveKind.Heal:
                case MoveKind.Buff:
                    CombatMovePose.Lift(by, 0.18f, 0.3f);
                    if (visible) CombatFxDirector.SpellBurst(by, pos, tier);
                    break;
            }

            // 地形破坏（按招式形态；脚下保护=自身位置）
            ApplyTerrain(move, by, pos, targetPos, tier);

            // ================= v0.21.28 觉醒衔击 =================
            // 擒拿演出：抓取目标拉近 → 短暂持有 → 摔投（纯表现位移，不碰原版 AI）
            if (move.IsGrapple && target != null && target.isAlive())
            {
                try { GrappleShow(move, by, target, pos, tier); }
                catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[MoveDirector] grapple: " + dsEx.Message); }
            }
            // 命中后把目标送入失衡状态（腾空/失衡/震慑），供 AI 衔击追击
            if (move.LinkOut != LinkState.None && target != null)
            {
                CombatLink.Apply(target, move.LinkOut, tier);
            }
        }

        /// <summary>擒拿演出：目标被拉到攻击者身前 → 短暂持有（失衡=擒拿）→ 摔投抛出。</summary>
        private static void GrappleShow(CombatMove move, Actor by, Actor target, Vector2 pos, int tier)
        {
            Vector2 toTarget = target.current_position - pos;
            if (toTarget.sqrMagnitude < 0.0001f) toTarget = new Vector2(1f, 0f);
            float grabDist = Mathf.Max(0.4f, toTarget.magnitude - 0.55f);   // 拉近到贴身
            Vector2 grabDir = toTarget.normalized;
            // 1) 目标被拽到攻击者身前（表现位移）
            try { CombatMovePose.GrabPull(target, grabDir, grabDist, 0.22f); }
            catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[MoveDirector] grab pull: " + dsEx.Message); }
            // 2) 目标进入擒拿状态（1.2s 窗口：持有/摔投衔接）
            CombatLink.Apply(target, LinkState.Grabbed, tier);
            // 3) 摔投抛出（背向抛离 + 砸地光效）
            if (target != null && target.isAlive())
            {
                CombatMovePose.ThrowAway(target, -grabDir, 0.9f, 0.5f);
                if (CanFull(target.current_position))
                {
                    Vector2 land = target.current_position + (-grabDir) * 0.9f;
                    CombatFxDirector.GroundSlam(by, land);
                }
            }
        }

        /// <summary>
        /// 节奏分层：终极招=0.08s 击停+0.45×慢动作 0.3s（命中瞬间世界定格）；
        /// 重招（境界≥8）=0.05s 击停+0.6×慢动作 0.18s；轻招/低境界=不动（保持流畅搏杀）。
        /// 世界观：登神级对轰才配得上"世界为之停顿"。
        /// </summary>
        private static void CinematicBeat(CombatMove move, Actor by, Vector2 targetPos, int tier)
        {
            if (by == null || !by.isAlive()) return;
            if (!CanFull(targetPos)) return;
            if (move.Weight == MoveWeight.Ultimate)
            {
                CombatFxHitStop.Request(targetPos, 0.08f);
                CombatFxCinema.SlowMo(0.45f, 0.3f);
            }
            else if (move.Weight == MoveWeight.Heavy && tier >= 8)
            {
                CombatFxHitStop.Request(targetPos, 0.05f);
                CombatFxCinema.SlowMo(0.6f, 0.18f);
            }
            // v0.21.22 动画语言降门槛：低境界搏杀也有节奏——
            // 中阶重招（tier>=4）给击停顿挫，所有招式给微击停，普通战斗不再"无声无息"
            else if (move.Weight == MoveWeight.Heavy && tier >= 4)
            {
                CombatFxHitStop.Request(targetPos, 0.04f);
            }
            else
            {
                CombatFxHitStop.Request(targetPos, 0.02f);
            }
        }

        // v0.21.23 异能战斗风格判定抽到共享 CombatStyle（投射/近战/控制），此处直接复用，
        // 消除 CombatMoveDirector 与 CombatStanceAI 两处的重复标签集合
        private static bool IsRangedStyle(Actor by)
        {
            return Code.Combat.AI.CombatStyle.IsRanged(by);
        }

        /// <summary>光束贯穿演出（空间撕裂/圣光审判）：粗刃贯穿 + 两翼光带 + 命中爆闪。</summary>
        private static void BeamVisual(Actor by, Vector2 pos, Vector2 targetPos, int tier)
        {
            if (!CanFull(targetPos) || !CanFull(pos)) return;
            Vector2 d = targetPos - pos;
            float dist = d.magnitude;
            if (dist < 0.5f) return;
            d /= dist;
            Vector2 mid = (pos + targetPos) * 0.5f;
            mid.y += 0.17f;
            Color c = TierColor(tier);
            // 贯穿刃（Blade 拉长，旋转朝向）
            var beam = CombatFxCore.Spawn(CombatFxArt.Blade, mid, 0.09f, 0.4f, FxLayer.Over, 34);
            if (beam != null)
            {
                beam.Color(c, new Color(c.r, c.g, c.b, 0f)).Sized(dist * 0.3f, 0.22f);
                beam.Rot = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
            }
            // 两翼斜光带（交叉刃）
            for (int i = 0; i < 2; i++)
            {
                float ang = (i == 0 ? -0.35f : 0.35f);
                Vector2 wd = new Vector2(d.x * Mathf.Cos(ang) - d.y * Mathf.Sin(ang),
                                         d.x * Mathf.Sin(ang) + d.y * Mathf.Cos(ang));
                var wing = CombatFxCore.Spawn(CombatFxArt.Slash, mid, 0.08f, 0.34f, FxLayer.Over, 30);
                if (wing != null)
                {
                    wing.Color(c, new Color(c.r, c.g, c.b, 0f)).Sized(0.5f, 0.2f);
                    wing.Rot = Mathf.Atan2(wd.y, wd.x) * Mathf.Rad2Deg - 90f;
                }
            }
            // 命中爆闪（星雨）
            for (int i = 0; i < 6; i++)
            {
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                CombatFxCore.Spawn(CombatFxArt.HitStars[UnityEngine.Random.Range(0, CombatFxArt.HitStars.Length)],
                    targetPos + new Vector2(0f, 0.17f), 0.1f, 0.35f, FxLayer.Top, 40)
                    .Color(c, new Color(c.r, c.g, c.b, 0f)).Sized(0.3f, 0.12f)
                    .Moving(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 7f, 0f, 2f, 0f);
            }
            CombatFxSound.Play(tier >= 10 ? CombatFxSound.ExplosionMeteorite : CombatFxSound.Whoosh, mid);
            if (tier >= 10) CombatFxHitStop.Request(targetPos, 0.14f);
        }

        /// <summary>召唤演出：为施法者召一只与自身异能同源的灵兽（无则跳过）。</summary>
        private static void TrySummonFor(Actor by, Vector2 targetPos, int tier)
        {
            try
            {
                PrototypeDef def = PrototypeRegistry.GetForActor(by);
                if (def == null) return;
                SummonEngine.TrySummon(by, null, def);
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[MoveDirector] Summon: " + e.Message);
            }
        }

        /// <summary>地形破坏：按招式形态分派（脚下保护=施法者立足）。</summary>
        private static void ApplyTerrain(CombatMove move, Actor by, Vector2 pos, Vector2 targetPos, int tier)
        {
            try
            {
                DSTerrainDestruction.DestructionScale scale =
                    move.Weight == MoveWeight.Ultimate ? DSTerrainDestruction.DestructionScale.Ultimate :
                    move.Weight == MoveWeight.Heavy ? DSTerrainDestruction.DestructionScale.Heavy :
                    DSTerrainDestruction.DestructionScale.Light;
                Vector2? keep = by.current_position;
                switch (move.Terrain)
                {
                    case MoveTerrain.Crater:
                        DSTerrainDestruction.Blast(by, targetPos, 2.2f, scale, keep, fire: false);
                        break;
                    case MoveTerrain.None:
                        // v0.21.11 无地形形态的终极招（时间凝滞/再生领域等）仍给一次小震波——领域展开撼动大地
                        if (move.Weight == MoveWeight.Ultimate)
                            DSTerrainDestruction.Quake(Mathf.Max(1, DSTerrainDestruction.ForceForTier(tier) - 1), targetPos);
                        break;
                    case MoveTerrain.Furrow:
                        DSTerrainDestruction.Furrow(by, pos, targetPos, 0.9f, scale, keep);
                        break;
                    case MoveTerrain.Magma:
                        DSTerrainDestruction.Blast(by, targetPos, 2.6f, scale, keep, fire: true);
                        break;
                    case MoveTerrain.Scorch:
                        DSTerrainDestruction.Blast(by, targetPos, 1.6f, scale, keep, fire: false);
                        break;
                }
                // v0.21.11 终极招地形余波：主破坏后再震一次（裂痕沿地面蔓延）
                if (move.Weight == MoveWeight.Ultimate && move.Terrain != MoveTerrain.None)
                    DSTerrainDestruction.Quake(Mathf.Max(1, DSTerrainDestruction.ForceForTier(tier) - 1), targetPos);
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[MoveDirector] Terrain: " + e.Message);
            }
        }

        /// <summary>境界档位主色（与演出导演一致，Ultimate 亮金强调）。</summary>
        private static Color TierColor(int tier)
        {
            if (tier >= 13) return new Color(1f, 0.85f, 0.25f); // 超神：金
            if (tier >= 11) return new Color(0.75f, 0.9f, 1f);  // 登神：苍蓝
            if (tier >= 9) return new Color(0.85f, 0.7f, 1f);   // 圣：紫
            if (tier >= 6) return new Color(0.55f, 0.8f, 1f);   // 使徒：蓝
            return new Color(1f, 1f, 1f);                       // 低阶：白
        }

        // ------------------------------------------------------ 冷却 --

        private static void PutOnCooldown(Actor by, string moveId, float seconds)
        {
            if (by == null || moveId == null) return;
            if (!_cooldowns.TryGetValue(by, out var map))
            {
                map = new Dictionary<string, float>();
                _cooldowns[by] = map;
            }
            map[moveId] = seconds;
        }

        /// <summary>每帧驱动：冷却递减 + 清死单位（FramePatch Postfix 调用）。</summary>
        public static void Tick(float dt)
        {
            // v0.21.28 失衡窗口衰减（衔击追击计时）
            try { CombatLink.Tick(dt); }
            catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[CombatLink] tick: " + dsEx.Message); }
            if (_cooldowns.Count == 0) return;
            var dead = new List<Actor>();
            foreach (var kv in _cooldowns)
            {
                if (kv.Key == null || !kv.Key.isAlive())
                {
                    dead.Add(kv.Key);
                    continue;
                }
                bool empty = true;
                foreach (var mk in kv.Value)
                {
                    float nv = mk.Value - dt;
                    kv.Value[mk.Key] = nv;
                    if (nv > 0f) empty = false;
                }
                if (empty) dead.Add(kv.Key);
            }
            foreach (var d in dead) _cooldowns.Remove(d);
        }

        /// <summary>世界销毁/存档切换：清空冷却。</summary>
        public static void ClearAll()
        {
            _cooldowns.Clear();
        }
    }
}
