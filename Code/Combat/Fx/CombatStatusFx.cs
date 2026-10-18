// ============================================================
// 状态附着表现层（v0.21.3）
// 用户批评："效果特效图没做成效果，而是做成攻击特效"——
//   原版机制（反编译 0.51.x 已核实）：
//     · 燃烧附着 = addStatusEffect("burning") → Status 挂在单位身上播放
//       fx_status_burning_t 帧动画（animation_speed 0.1s / random_frame /
//       mat_world_object_lit 发光材质 / draw_light 照亮 / 每 2s 灼烧 /
//       AI run_to_water_when_on_fire），原版自带 3 套火焰帧
//       effects/fx_status_burning_t(_2/_3)（QuantumSpriteLibrary）
//     · 冻结附着 = 温度<-200 → addStatusEffect("frozen") → fx_status_frozen_t
//       贴图 + 定身（speed-10000 / immovable / frozen_ai）
//     · 地表冻结 = tile.canBeFrozen() → tile.freeze(5) → 按 freeze_to_id 换
//       顶层冰面（frozen_low/high），温度回升自动解冻
// 本模块：命中状态效果成功附加后，给目标单位挂"持续状态视觉"——
//   燃烧=复用原版 fx_status_burning_t 火焰帧循环跟随；冻结=复用原版
//   fx_status_frozen_t 贴图跟随；中毒=原版 poisoned 无贴图，用我方毒雾补。
// 纯表现层；同单位同状态 0.8s 冷却防叠；不写业务数据。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using Code.Realm.Prototype;

namespace Code.Combat.Fx
{
    public static class CombatStatusFx
    {
        // 同单位同状态附着冷却（防连续命中叠出多层火焰/冰晶）
        private static readonly Dictionary<Actor, Dictionary<string, float>> _attachCd =
            new Dictionary<Actor, Dictionary<string, float>>();
        private const float CD = 0.8f;

        /// <summary>状态成功附加后调用：给目标挂持续状态视觉（时长=状态剩余时长）</summary>
        public static void Attach(Actor target, string statusId, float duration)
        {
            if (target == null || !target.isAlive()) return;
            if (World.world == null || World.world.isPaused()) return;

            // 状态 → 附着素材（优先复用原版 fx_status_* 状态贴图；原版没有的用我方素材补，避免重复造图）
            string fxId;
            float alpha;
            switch (statusId)
            {
                case "burning": fxId = "fx_status_burning_t"; alpha = 0.4f; break;   // v0.21.24 压小贴体（小火苗，不再大火焰裹身）
                case "frozen":  fxId = "fx_status_frozen_t";  alpha = 0.5f;  break;   // v0.21.24 压小贴体（薄冰晶，不再大冰块裹身）
                case "poisoned": fxId = "__proto_poison";   alpha = 0.3f;  break;  // 原版 poisoned 无贴图 → 程序化毒雾（v0.21.9 全程序化）
                default: return;   // stunned/slowness 等短状态不挂，防刷屏
            }

            // 冷却（同单位同状态 0.8s 内不重复挂；不同状态可同时挂）；顺带清理已死单位
            float now = Time.unscaledTime;
            if (_attachCd.Count > 256)
            {
                foreach (var k in new List<Actor>(_attachCd.Keys))
                    if (k == null || !k.isAlive()) _attachCd.Remove(k);
            }
            if (_attachCd.TryGetValue(target, out var map))
            {
                if (map.TryGetValue(statusId, out float last) && now - last < CD) return;
                map[statusId] = now;
            }
            else
            {
                _attachCd[target] = new Dictionary<string, float> { { statusId, now } };
            }

            try { SpawnAttach(target, fxId, alpha, duration); }
            catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[CombatStatusFx] " + dsEx.Message); }
        }

        private static void SpawnAttach(Actor target, string fxId, float alpha, float duration)
        {
            Sprite[] frames;
            if (fxId.StartsWith("fx_"))
            {
                // 原版状态贴图（QuantumSpriteLibrary 同源：effects/fx_status_*）
                frames = Resources.LoadAll<Sprite>("effects/" + fxId);
                if (frames == null || frames.Length == 0)
                {
                    // v0.21.19 原版贴图缺失兜底：程序化形态（着火=火形、冻结=冰晶），
                    // 保证"单位身上持续冒火/被冰晶覆盖"的视觉一定出现（不依赖原版素材存在性）
                    int glyph = fxId.Contains("frozen") ? AbilityFx.TagGlyphIndex(Code.Realm.Prototype.PrototypeTag.Frost)
                              : fxId.Contains("burning") ? AbilityFx.TagGlyphIndex(Code.Realm.Prototype.PrototypeTag.Burn) : -1;
                    if (glyph < 0) return;
                    int seed = target != null ? (int)(target.getID() * 131 + 7) : 0;
                    frames = CombatFxArt.MakePrototypeFrames(seed, glyph, -1, 0);
                    if (frames == null || frames.Length == 0) return;
                }
                else
                {
                    Array.Sort(frames, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                }
            }
            else if (fxId == "__proto_poison")
            {
                // v0.21.9 程序化毒附着：淡绿烟雾形态（TagGlyphIndex(Poison)=火形变体 seed 区分），
                // 循环播形态演化帧 = 单位身上持续涌毒雾
                int seed = target != null ? (int)(target.getID() * 131 + 7) : 0;
                frames = CombatFxArt.MakePrototypeFrames(seed, AbilityFx.TagGlyphIndex(Code.Realm.Prototype.PrototypeTag.Poison), -1, 0);
                if (frames == null || frames.Length == 0) return;
                alpha = 0.3f;
            }
            else
            {
                frames = AbilityFx.Load(fxId);
                if (frames == null || frames.Length == 0) return;
            }

            float frameW = frames[0].rect.width;
            if (frameW <= 0f) frameW = 128f;
            float px = 1f / frameW;
            float scale = AbilityFx.UnitScale(target);
            // 挂在单位身体中心（脚底+0.17×体型），跟随移动
            var p = CombatFxCore.Spawn(frames,
                target.current_position + new Vector2(0f, 0.17f * scale),
                px, duration, FxLayer.Top, 25);
            if (p == null) return;
            if (fxId == "__proto_poison") p.Loop = true;   // 毒雾循环涌动
            p.FollowActor = target;
            p.FollowOffset = new Vector2(0f, 0.17f * scale);
            p.Loop = true;   // 帧动画循环（燃烧持续冒火）
            // v0.21.4 尺寸对齐原版状态附着（反编译 0.51.x 定标）：
            //   原版 drawStatusEffectFor → setScale(current_scale.y × asset.scale)，
            //   人类 current_scale.y≈0.25、frozen/burning 的 asset.scale=1 → 附着≈1.0×单位视觉宽
            //   （≈0.25 世界单位，与我方护盾 0.28"罩全身"同级别）。
            //   v0.21.3 误用 0.45（≈1.8 倍单位宽，比原版大 80%）→ 修正 0.26。
            //   v0.21.24 用户点名"老子不要大冰块和大火焰"——状态附着压到 0.55 倍单位宽贴体，
            //   观感=小火苗/薄冰晶（状态机制不变，视觉不再裹全身）
            float s = 0.14f * scale;
            p.Sized(s, s * 0.9f);
            p.Color(new Color(1f, 1f, 1f, alpha), new Color(1f, 1f, 1f, alpha * 0.5f));
        }
    }
}
