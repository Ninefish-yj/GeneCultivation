// ============================================================
// 异能辨识光晕（v0.21.19）
// 解决"战斗挤在一起根本分不清谁是谁"：
// 给视野内的异能觉醒单位挂一枚跟随光晕（按 MainTag 形态 + 异能专属色），
// 一眼认出该单位的异能类型/派系——火系=火苗光环、冰系=冰晶光环、雷系=雷光…，
// 同时兼作"异能单位 vs 原版单位"的视觉区分（路人一眼看出这是异能战斗）。
//
// 纯表现层：不改原版行为树/移动/伤害；复用 CombatFxCore 跟随机制
// （FollowActor 绑定单位、单位死亡自动回收，零手动清理）。
// 性能：只对视野内单位挂（CombatBrain chunk 扫描复用），单单位一个粒子，
// 已挂单位跳过（Dictionary 防重复），128 上限防膨胀。
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using Code.Realm.Prototype;

namespace Code.Combat.Fx
{
    public static class CombatAuraFx
    {
        private static readonly Dictionary<Actor, bool> _attached = new Dictionary<Actor, bool>();
        private const int MaxAuras = 128;

        /// <summary>给异能单位挂辨识光晕（幂等：已挂跳过；无异能跳过；非活体跳过）</summary>
        public static void EnsureAura(Actor actor, PrototypeDef def)
        {
            if (actor == null || !actor.isAlive()) return;
            if (def == null) return;
            if (_attached.ContainsKey(actor)) return;
            if (_attached.Count >= MaxAuras) return;
            if (World.world == null || World.world.isPaused()) return;

            try
            {
                // 形态 = 主标签形态（与命中爆/贴地域同视觉语言），色 = 异能专属派生色
                int glyph = AbilityFx.TagGlyphIndex(def.MainTag);
                int seed = AbilityFx.StableSeed(def.Id);
                var frames = CombatFxArt.MakePrototypeFrames(seed, glyph, -1, 0);
                if (frames == null || frames.Length == 0) return;

                float frameW = frames[0].rect.width;
                if (frameW <= 0f) frameW = 128f;
                float px = 1f / frameW;
                float scale = AbilityFx.UnitScale(actor);
                Color tint = AbilityFx.DeriveTint(def);

                var p = CombatFxCore.Spawn(frames,
                    actor.current_position + new Vector2(0f, 0.55f * scale),
                    px, 9999f, FxLayer.Top, 20);
                if (p == null) return;
                p.Loop = true;                       // 光晕持续脉动
                p.FollowActor = actor;               // 跟随单位，死亡自动回收
                p.FollowOffset = new Vector2(0f, 0.55f * scale);   // 头顶光晕
                // 尺寸：0.22×体型（原版 buff 光环同级别，不遮单位本体）
                float s = 0.22f * scale;
                p.Sized(s, s * 0.9f);
                p.Color(new Color(tint.r, tint.g, tint.b, 0.35f),
                        new Color(tint.r, tint.g, tint.b, 0.12f));  // 半透明，不挡视野
                p.Spin = 20f;                         // 缓转，增加"标识感"

                _attached[actor] = true;
            }
            catch (System.Exception dsEx)
            {
                Code.Core.DSDebug.Verbose("[CombatAuraFx] " + dsEx.Message);
            }
        }

        /// <summary>清掉已死亡/已离开视野单位的记录（防字典膨胀；粒子本身由跟随机制自动回收）</summary>
        public static void Prune()
        {
            if (_attached.Count <= 0) return;
            var dead = new List<Actor>();
            foreach (var k in _attached.Keys)
                if (k == null || !k.isAlive()) dead.Add(k);
            for (int i = 0; i < dead.Count; i++) _attached.Remove(dead[i]);
        }
    }
}
