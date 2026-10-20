// ============================================================
// 技能特效管理器（v0.21.46 全程序化版）
// 11 个登神技能特效全部由 CombatFxArt 程序化图元生成，零素材、零原版 fx_*。
// 容器从 EffectsLibrary.spawn("fx_slash") 改为 CombatFxCore.Spawn（纯程序化粒子池）。
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using Code.Core;
using Code.Combat.Fx;

namespace Code.Realm
{
    public static class DSSkillEffectManager
    {
        // ============================================================
        // 帧缓存（性能优化）
        // ============================================================
        private static readonly Dictionary<string, Sprite[]> _spriteCache = new Dictionary<string, Sprite[]>();

        // ============================================================
        // 程序化形态表：11 技能特效全部由程序化图元生成（零素材图）。
        // 格式：skill 目录名 → (主形态索引, 副形态索引, 半透明专属色)
        // 形态索引见 CombatFxArt 26 图元库（0火 1冰 2雷 3风 4水 5土 6金 7光 8暗 9血 10镰
        //   11心 12眼 13爪 14沙漏 15月 16毒 17毒雾 18链 19翼 20星 21符印 22涟漪 23盾 24藤 25棱晶）
        // ============================================================
        private static readonly Dictionary<string, (int MainFx, int SubFx, Color Tint)> _proceduralFx =
            new Dictionary<string, (int, int, Color)>(StringComparer.OrdinalIgnoreCase)
            {
                // 6阶 能量弹：星芒火弹（投射物）
                { "energy_bolt", (20, 0, new Color(0.65f, 0.8f, 1f)) },
                // 7阶 能量刃：金刃圣光（虚拟武器）
                { "energy_blade", (6, 7, new Color(0.55f, 0.85f, 1f)) },
                // 7阶 治疗之光：心形圣辉
                { "healing_light", (11, 20, new Color(0.6f, 1f, 0.7f)) },
                // 8阶 能量风暴：旋涡雷暴
                { "energy_storm", (15, 2, new Color(0.55f, 0.6f, 1f)) },
                // 9阶 领域展开：符印涟漪（法阵）
                { "domain_expand", (21, 22, new Color(0.55f, 0.62f, 1f)) },
                // 9阶 法则压制：盾符镇压
                { "law_suppress", (23, 21, new Color(0.75f, 0.6f, 1f)) },
                // 10阶 本源汲取：血滴吸蚀
                { "origin_drain", (9, 17, new Color(0.85f, 0.4f, 0.45f)) },
                // 10阶 空间撕裂：风刃裂土
                { "space_tear", (3, 5, new Color(0.85f, 0.8f, 1f)) },
                // 11阶 圣域：圣翼神盾
                { "sanctuary", (19, 23, new Color(1f, 0.85f, 0.5f)) },
                // 12阶 维度打击：棱晶雷光（大范围）
                { "dimensional_strike", (25, 2, new Color(0.5f, 0.75f, 1f)) },
                // 13阶 神罚：天雷圣光（全屏）
                { "divine_punishment", (2, 7, new Color(1f, 0.9f, 0.4f)) },
            };

        // ============================================================
        // 技能动画配置（11个技能全部注册）
        // ============================================================
        public class SkillEffectConfig
        {
            public string AnimationPath;
            public float Scale;
            public float FrameInterval;
            public bool FollowActor;
            public float PositionOffsetY;
            public bool Loop;
            public float Duration;

            public SkillEffectConfig(string path, float scale, float frameInterval,
                bool followActor = false, float offsetY = 0f, bool loop = false, float duration = 0f)
            {
                AnimationPath = path;
                Scale = scale;
                FrameInterval = frameInterval;
                FollowActor = followActor;
                PositionOffsetY = offsetY;
                Loop = loop;
                Duration = duration;
            }
        }

        /// <summary>11个技能全部注册动画配置</summary>
        private static readonly Dictionary<string, SkillEffectConfig> _effectConfigs =
            new Dictionary<string, SkillEffectConfig>(StringComparer.OrdinalIgnoreCase)
            {
                // 6阶 能量弹：投射物特效
                { "ds_ability_energy_bolt",
                    new SkillEffectConfig("skill/energy_bolt", 0.35f, 0.05f, followActor: true, offsetY: 0.17f) },

                // 7阶 能量刃（虚拟武器）：跟随施法者
                { "ds_ability_energy_blade",
                    new SkillEffectConfig("skill/energy_blade", 0.4f, 0.08f, followActor: true, offsetY: 0.17f) },

                // 7阶 治疗之光：跟随施法者，持续3秒
                { "ds_ability_healing_light",
                    new SkillEffectConfig("skill/healing_light", 0.4f, 0.1f, followActor: true, offsetY: 0.17f, duration: 3.0f) },

                // 8阶 能量风暴：范围伤害
                { "ds_ability_energy_storm",
                    new SkillEffectConfig("skill/energy_storm", 1.2f, 0.12f, followActor: true, offsetY: 0.17f, duration: 3.0f) },

                // 9阶 领域展开：跟随施法者，持续5秒
                { "ds_ability_domain_expand",
                    new SkillEffectConfig("skill/domain_expand", 1.0f, 0.15f, followActor: true, offsetY: 0.17f, duration: 5.0f) },

                // 9阶 法则压制：目标位置
                { "ds_ability_law_suppress",
                    new SkillEffectConfig("skill/law_suppress", 2.5f, 0.1f, followActor: true, offsetY: 0.17f, duration: 2.0f) },

                // 10阶 本源汲取：目标位置
                { "ds_ability_origin_drain",
                    new SkillEffectConfig("skill/origin_drain", 3f, 0.12f, followActor: false, duration: 2.5f) },

                // 10阶 空间撕裂：目标位置
                { "ds_ability_space_tear",
                    new SkillEffectConfig("skill/space_tear", 2f, 0.1f, followActor: false, duration: 2.5f) },

                // 11阶 圣域：跟随施法者，持续10秒
                { "ds_ability_sanctuary",
                    new SkillEffectConfig("skill/sanctuary", 1.5f, 0.12f, followActor: true, offsetY: 0.17f, duration: 10.0f) },

                // 12阶 维度打击：大范围
                { "ds_ability_dimensional_strike",
                    new SkillEffectConfig("skill/dimensional_strike", 1.5f, 0.08f, followActor: false, duration: 3.0f) },

                // 13阶 神罚：全屏
                { "ds_ability_divine_punishment",
                    new SkillEffectConfig("skill/divine_punishment", 1.5f, 0.05f, followActor: false, duration: 4.0f) },
            };

        // ============================================================
        // 初始化与预加载
        // ============================================================
        public static void Init()
        {
            DSDebug.Verbose("[技能特效] 初始化（全程序化版）");
            PreloadAllEffects();
        }

        /// <summary>预加载所有技能的帧序列</summary>
        private static void PreloadAllEffects()
        {
            int success = 0, fail = 0;
            foreach (var kv in _effectConfigs)
            {
                try
                {
                    var sprites = LoadSprites(kv.Value.AnimationPath);
                    if (sprites != null && sprites.Length > 0)
                    {
                        success++;
                    }
                    else
                    {
                        fail++;
                        DSDebug.Warning($"[技能特效] 预加载失败: {kv.Key} -> {kv.Value.AnimationPath}");
                    }
                }
                catch (Exception e)
                {
                    fail++;
                    DSDebug.Warning($"[技能特效] 预加载异常: {kv.Key} -> {e.Message}");
                }
            }
            DSDebug.Verbose($"[技能特效] 预加载完成: 成功 {success}/{_effectConfigs.Count}, 失败 {fail}");
        }

        /// <summary>检查技能是否有特效</summary>
        public static bool HasEffect(string abilityId)
        {
            return !string.IsNullOrEmpty(abilityId) && _effectConfigs.ContainsKey(abilityId);
        }

        // ============================================================
        // 核心播放方法
        // ============================================================
        public static void PlayEffect(Actor actor, string abilityId, Actor target = null)
        {
            if (actor == null || string.IsNullOrEmpty(abilityId)) return;

            if (!_effectConfigs.TryGetValue(abilityId, out SkillEffectConfig config))
            {
                return;
            }

            // 确定播放位置
            Vector2 position;
            if (config.FollowActor)
            {
                position = actor.current_position;
                position.y += config.PositionOffsetY;
            }
            else if (target != null && target.isAlive())
            {
                position = target.current_position;
            }
            else
            {
                position = actor.current_position;
                position.x += actor.is_looking_left ? -3f : 3f;
            }

            // 领域展开：范围技能（Scale≥2）释放时加符文法阵演出
            try
            {
                if (config.Scale >= 2f)
                {
                    CombatFxDirector.DomainCircle(position, config.Scale,
                        new Color(0.55f, 0.62f, 1f));
                }
            }
            catch (System.Exception dsEx) { DSDebug.Verbose("[GeneCultivation] 领域法阵演出失败: " + dsEx.Message); }

            PlayEffectAtPosition(position, config, actor, target);
        }

        /// <summary>在指定位置播放特效（全程序化：CombatFxCore.Spawn + CombatFxArt 帧）</summary>
        private static void PlayEffectAtPosition(Vector2 position, SkillEffectConfig config,
            Actor caster = null, Actor target = null)
        {
            if (config == null || string.IsNullOrEmpty(config.AnimationPath)) return;

            try
            {
                var sprites = LoadSprites(config.AnimationPath);
                if (sprites == null || sprites.Length == 0) return;

                float frameW = sprites[0].rect.width;
                if (frameW <= 0f) frameW = 64f;
                float px = 1f / frameW;   // 基准宽=1 世界单位，Sized 再缩到 config.Scale

                // 位置：非跟随技能对准目标身体中心；大范围技能贴地
                Vector2 spawnPos = position;
                if (!config.FollowActor)
                {
                    float lift = config.Scale >= 5f ? 0.06f : 0.17f;
                    spawnPos.y += lift;
                }

                // 生命期：循环=Duration，非循环=帧动画时长
                float life = config.Loop ? config.Duration : (sprites.Length * config.FrameInterval);
                if (life <= 0f) life = 1f;

                var p = CombatFxCore.Spawn(sprites, spawnPos, px, life, FxLayer.Behind, 8);
                if (p == null) return;

                // 帧动画
                p.Fps = 1f / config.FrameInterval;
                p.Loop = config.Loop;

                // 尺寸：config.Scale = 期望世界宽
                float aspect = sprites[0].rect.height / Mathf.Max(0.0001f, sprites[0].rect.width);
                p.Sized(config.Scale, config.Scale * aspect);

                // 专属色 + 透明度分级（大范围更透明）
                string tintKey = config.AnimationPath.StartsWith("skill/")
                    ? config.AnimationPath.Substring("skill/".Length) : config.AnimationPath;
                Color tint = Color.white;
                if (_proceduralFx.TryGetValue(tintKey, out var tintFx)) tint = tintFx.Tint;
                tint.a = config.Scale >= 5f ? 0.35f : 0.5f;
                p.Color(tint);

                // 跟随施法者
                if (config.FollowActor && caster != null)
                {
                    p.FollowActor = caster;
                    p.FollowOffset = new Vector2(0f, config.PositionOffsetY);
                }
            }
            catch (Exception e)
            {
                DSDebug.Verbose($"[技能特效] 播放失败: {e.Message}");
            }
        }

        // ============================================================
        // 帧序列加载（带缓存，全程序化）
        // ============================================================
        private static Sprite[] LoadSprites(string path)
        {
            if (_spriteCache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            try
            {
                string key = path.StartsWith("skill/") ? path.Substring("skill/".Length) : path;
                if (_proceduralFx.TryGetValue(key, out var fx))
                {
                    int seed = 0;
                    foreach (char c in key) seed = seed * 31 + c;
                    if (seed < 0) seed = -seed;
                    var sprites = CombatFxArt.MakePrototypeFrames(
                        seed, fx.MainFx, fx.SubFx, 7);
                    if (sprites != null && sprites.Length > 0)
                    {
                        _spriteCache[path] = sprites;
                        return sprites;
                    }
                }
                _spriteCache[path] = new Sprite[0];
                return new Sprite[0];
            }
            catch
            {
                _spriteCache[path] = new Sprite[0];
                return new Sprite[0];
            }
        }

        // ============================================================
        // 预留更新接口（v0.21.46 起定时停止由 CombatFxCore.Life 处理，无需轮询）
        // ============================================================
        public static void UpdateScheduledStops(float deltaTime) { }

        public static void UpdatePendingEffects(float deltaTime) { }
    }
}
