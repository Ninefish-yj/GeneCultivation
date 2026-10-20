// ============================================================
// 状态附着表现层（v0.21.46 全程序化版）
// 命中状态效果成功附加后，给目标单位挂"持续状态视觉"——
//   燃烧=火形态循环跟随、冻结=冰霜形态跟随、中毒=毒雾形态跟随。
// v0.21.46 移除 Resources.LoadAll<Sprite>("effects/fx_status_*") 原版状态贴图，
// 全部走 CombatFxArt 程序化图元（零素材零原版图）。
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

            // 状态 → 程序化形态索引 + 透明度
            int glyph;
            float alpha;
            switch (statusId)
            {
                case "burning":  glyph = AbilityFx.TagGlyphIndex(PrototypeTag.Burn);  alpha = 0.55f; break;
                case "frozen":   glyph = AbilityFx.TagGlyphIndex(PrototypeTag.Frost); alpha = 0.7f;  break;
                case "poisoned": glyph = AbilityFx.TagGlyphIndex(PrototypeTag.Poison); alpha = 0.3f; break;
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

            try { SpawnAttach(target, glyph, alpha, duration); }
            catch (System.Exception dsEx) { Code.Core.DSDebug.Verbose("[CombatStatusFx] " + dsEx.Message); }
        }

        private static void SpawnAttach(Actor target, int glyph, float alpha, float duration)
        {
            // 全程序化：CombatFxArt 生成状态附着帧（零素材）
            int seed = target != null ? (int)(target.getID() * 131 + 7) : 0;
            var frames = CombatFxArt.MakePrototypeFrames(seed, glyph, -1, 0);
            if (frames == null || frames.Length == 0) return;

            float frameW = frames[0].rect.width;
            if (frameW <= 0f) frameW = 128f;
            float px = 1f / frameW;
            float scale = AbilityFx.UnitScale(target);
            // 挂在单位身体中心（脚底+0.17×体型），跟随移动
            var p = CombatFxCore.Spawn(frames,
                target.current_position + new Vector2(0f, 0.17f * scale),
                px, duration, FxLayer.Top, 25);
            if (p == null) return;
            p.FollowActor = target;
            p.FollowOffset = new Vector2(0f, 0.17f * scale);
            p.Loop = true;   // 帧动画循环（燃烧持续冒火/冰霜持续闪烁/毒雾持续涌动）
            p.Fps = 10f;
            // 尺寸：0.85×scale → 普通单位 0.255 世界宽（对齐原版状态附着定标）
            float s = 0.85f * scale;
            p.Sized(s, s * 0.9f);
            // 状态专属色：燃烧=橙红、冻结=冰蓝、中毒=暗绿
            Color tint;
            if (glyph == AbilityFx.TagGlyphIndex(PrototypeTag.Burn))
                tint = new Color(1f, 0.5f, 0.2f, alpha);
            else if (glyph == AbilityFx.TagGlyphIndex(PrototypeTag.Frost))
                tint = new Color(0.6f, 0.85f, 1f, alpha);
            else
                tint = new Color(0.4f, 0.8f, 0.3f, alpha);
            p.Color(tint, new Color(tint.r, tint.g, tint.b, alpha * 0.5f));
        }
    }
}
