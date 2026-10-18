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
                Execute(move, by, pos, targetPos);
                return true;
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[MoveDirector] Execute(actor): " + e.Message);
                return false;
            }
        }

        /// <summary>执行招式（显式坐标版本，AI 用目标格、事件用技能格）。</summary>
        public static void Execute(CombatMove move, Actor by, Vector2 pos, Vector2 targetPos)
        {
            try
            {
                if (move == null || by == null || !by.isAlive()) return;
                int tier = CultivationData.GetRealmTier(by);
                // 蓄势：中心光斑（不同轻重不同强度）
                Charge(move, by, pos, tier);
                // 主击：动作 + 地形破坏
                Strike(move, by, pos, targetPos, tier);
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
            float chargeSize = move.Weight == MoveWeight.Ultimate ? 0.32f : 0.2f;
            CombatFxCore.Spawn(CombatFxArt.Burst, pos, 0.08f, 0.3f, FxLayer.Over, 28)
                .Color(c, new Color(c.r, c.g, c.b, 0f))
                .Sized(chargeSize, chargeSize * 0.38f);
        }

        /// <summary>主击：按动作类型分派到现有动作 + 地形破坏。</summary>
        private static void Strike(CombatMove move, Actor by, Vector2 pos, Vector2 targetPos, int tier)
        {
            bool visible = CanFull(targetPos);
            switch (move.Kind)
            {
                case MoveKind.Lunge:
                    if (visible) CombatFxDirector.DashStrike(by, targetPos);
                    break;
                case MoveKind.Slash:
                    if (visible) CombatFxDirector.SlashArc(by, targetPos);
                    break;
                case MoveKind.Strike:
                    if (visible) CombatFxDirector.GroundSlam(by, targetPos);
                    break;
                case MoveKind.Chain:
                    if (targetPos.y == pos.y && targetPos.x == pos.x) break;
                    if (visible) CombatFxDirector.ChainArc(by);
                    break;
                case MoveKind.Domain:
                    float r = 1.2f + Mathf.Min(4f, tier * 0.25f);
                    if (visible) CombatFxDirector.DomainCircle(targetPos, r, TierColor(tier));
                    break;
                case MoveKind.Blast:
                    if (visible)
                    {
                        CombatFxDirector.Shockwave(targetPos, 3f + tier * 0.8f);
                        CombatFxDirector.SpellBurst(by, targetPos, tier);
                    }
                    break;
                case MoveKind.Beam:
                    BeamVisual(by, pos, targetPos, tier);
                    break;
                case MoveKind.Summon:
                    // 召唤：目标格唤出助战灵兽（复用召唤引擎）
                    TrySummonFor(by, targetPos, tier);
                    if (visible) CombatFxDirector.DomainCircle(targetPos, 1.6f, TierColor(tier));
                    break;
                case MoveKind.Heal:
                case MoveKind.Buff:
                    if (visible) CombatFxDirector.SpellBurst(by, pos, tier);
                    break;
            }

            // 地形破坏（按招式形态；脚下保护=自身位置）
            ApplyTerrain(move, by, pos, targetPos, tier);
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
