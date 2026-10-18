// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using Code.Core;

namespace Code.Combat
{
    /// <summary>
    /// 地形破坏类型枚举
    /// </summary>
    public enum TerrainDestructionType
    {
        /// <summary>能量风暴型：烧焦+点火+加热+移除龙卷风</summary>
        EnergyStorm = 0,
        /// <summary>维度打击型：降低高度+移除液体+生成坑洞</summary>
        DimensionalStrike = 1,
        /// <summary>神罚型：全屏烧焦+点火+屏幕震动+加热</summary>
        DivinePunishment = 2,
        /// <summary>通用型：烧焦+少量加热</summary>
        Generic = 3
    }

    /// <summary>
    /// 地形破坏系统（重制版）
    /// 使用原版游戏的 TerraformOptions 和 MapAction API
    /// </summary>
    public static class DSTerrainDestruction
    {
        // 地形破坏的冷却时间（避免频繁破坏同一区域）
        private static HashSet<string> _recentlyDestroyed = new HashSet<string>();
        private static float _lastCleanupTime = 0f;

        // ============================================================
        // 公共接口
        // ============================================================

        /// <summary>
        /// 在指定位置周围破坏地形
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">破坏半径（格）</param>
        /// <param name="destructionChance">每个地块的破坏概率（0-1）</param>
        /// <param name="destructionType">破坏类型</param>
        /// <param name="caster">施法者（用于敌友判断和盖亚契约）</param>
        /// <returns>破坏的地块数量</returns>
        public static int DestroyTerrain(
            Vector2 center,
            float radius,
            float destructionChance,
            TerrainDestructionType destructionType = TerrainDestructionType.Generic,
            Actor caster = null)
        {
            if (World.world == null || radius <= 0f || destructionChance <= 0f) return 0;

            // 清理过期的冷却记录（每60秒清理一次）
            if (Time.time - _lastCleanupTime > 60f)
            {
                _recentlyDestroyed.Clear();
                _lastCleanupTime = Time.time;
            }

            int destroyedCount = 0;
            System.Random random = new System.Random();

            try
            {
                // 获取中心地块
                WorldTile centerTile = World.world.GetTileSimple((int)center.x, (int)center.y);
                if (centerTile == null) return 0;

                // 构建地形改造配置
                TerraformOptions options = BuildTerraformOptions(destructionType);

                // 遍历范围内的地块
                int minX = Mathf.Max(0, (int)(center.x - radius));
                int maxX = Mathf.Min(MapBox.width - 1, (int)(center.x + radius));
                int minY = Mathf.Max(0, (int)(center.y - radius));
                int maxY = Mathf.Min(MapBox.height - 1, (int)(center.y + radius));

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        // 距离检查（圆形范围）
                        float dist = Vector2.Distance(center, new Vector2(x, y));
                        if (dist > radius) continue;

                        // 概率检查（v0.18.46：中心地块必破，边缘线性递减至 chance×0.5——修复旧版中心也只有50%概率导致"破坏太弱"）
                        float adjustedChance = Mathf.Lerp(1f, destructionChance * 0.5f, dist / radius);
                        if (random.NextDouble() > adjustedChance) continue;

                        // 冷却检查
                        string tileKey = $"{x}_{y}";
                        if (_recentlyDestroyed.Contains(tileKey)) continue;

                        // 获取地块
                        WorldTile tile = World.world.GetTileSimple(x, y);
                        if (tile == null) continue;

                        // 应用地形破坏
                        if (ApplyDestructionToTile(tile, destructionType, options, caster))
                        {
                            _recentlyDestroyed.Add(tileKey);
                            destroyedCount++;
                        }
                    }
                }

                // 重置重绘计时器（让地形变化立即显示）
                if (destroyedCount > 0)
                {
                    try { World.world.resetRedrawTimer(); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }
                }
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose($"[地形破坏] DestroyTerrain 异常: {e.Message}");
            }

            return destroyedCount;
        }

        /// <summary>
        /// 在施法者周围破坏地形（便捷方法）
        /// </summary>
                // ============================================================
        // 内部方法
        // ============================================================

        // ============================================================
        // 渲染安全校验（v0.19.14 崩溃修复）
        // 原版 WorldTilemap.renderTile → getVariation 依赖 tile.Type.sprites / main_type.sprites；
        // 若图形缺失返回 null，TilemapExtended.redraw → SetTiles 数组含 null →
        // Unity 2022.3.60f1 RefreshTile → NativeArray.CopySafe(source=null) → ArgumentNullException 周期崩溃。
        // 所有破坏步骤前先校验，避免把 tile 弄进"类型有效但图形缺失"的渲染死区。
        // ============================================================

        /// <summary>
        /// tile 是否处于可安全渲染状态（Type/main_type 及对应 sprites 均非空）。
        /// 任一图形缺失即为渲染死区，禁止继续破坏。
        /// </summary>
        private static bool IsTileRenderSafe(WorldTile tile)
        {
            if (tile == null) return false;
            try
            {
                if (tile.Type == null) return false;
                if (tile.Type.sprites == null) return false;
                if (tile.main_type != null && tile.main_type.sprites == null) return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// tile 是否还能继续降格（有 decrease_to 且非液体，避免降到"无图形"边界）。
        /// </summary>
        private static bool CanDecrease(WorldTile tile)
        {
            if (!IsTileRenderSafe(tile)) return false;
            if (tile.Type.liquid) return false;
            if (tile.Type.decrease_to == null) return false;
            return true;
        }

        /// <summary>
        /// 从世界重新取该格最新 tile（explodeTile/decreaseTile 后原引用数据可能已变化）。
        /// </summary>
        private static WorldTile RefreshTileRef(WorldTile tile)
        {
            if (tile == null) return null;
            try { return World.world.GetTileSimple(tile.pos.x, tile.pos.y); }
            catch (Exception) { return tile; }
        }

        /// <summary>
        /// 构建地形改造配置
        /// v0.18.77：爆炸化——explode_tile 炸树/炸坑、damage_buildings 炸全部建筑（原版 applyTileDamage 处理），
        /// damage>0 对单位造成爆炸伤害。旧版只烧焦+加热(damage=0)，树/非城市建筑/坑都不破坏，配不上炫特效。
        /// </summary>
        private static TerraformOptions BuildTerraformOptions(TerrainDestructionType type)
        {
            TerraformOptions options = new TerraformOptions();
            options.explode_tile = true;        // 炸树/炸可爆地块/炸坑
            options.damage_buildings = true;    // 炸建筑（含非城市建筑）
            options.add_burned = true;          // 烧焦
            options.shake = true;               // 屏幕震动（视觉匹配）
            options.shake_duration = 0.5f;
            options.shake_interval = 0.04f;
            options.shake_intensity = 0.35f;    // v0.18.88 上调（大破坏配大震动）

            switch (type)
            {
                case TerrainDestructionType.EnergyStorm:
                    // 能量风暴：烧焦+点火+加热+移除龙卷风+爆炸伤害（v0.18.88 伤害/加热上调）
                    options.set_fire = true;
                    options.add_heat = 25;
                    options.remove_tornado = true;
                    options.lightning_effect = false;
                    options.damage = 50;
                    break;

                case TerrainDestructionType.DimensionalStrike:
                    // 维度打击：烧焦+少量加热+爆炸伤害（高度变化在ApplyDestructionToTile中处理；v0.18.88 上调）
                    options.add_heat = 10;
                    options.damage = 90;
                    break;

                case TerrainDestructionType.DivinePunishment:
                    // 神罚：烧焦+点火+强加热+高爆炸伤害（v0.18.88 大幅上调，匹配超神级特效）
                    options.set_fire = true;
                    options.add_heat = 45;
                    options.damage = 160;
                    break;

                case TerrainDestructionType.Generic:
                default:
                    // 通用型：烧焦+少量加热+基础爆炸伤害（v0.18.88 上调）
                    options.add_heat = 10;
                    options.damage = 20;
                    break;
            }

            return options;
        }

        /// <summary>
        /// 对单个地块应用破坏效果
        /// </summary>
        private static bool ApplyDestructionToTile(
            WorldTile tile,
            TerrainDestructionType destructionType,
            TerraformOptions options,
            Actor caster)
        {
            if (tile == null) return false;

            try
            {
                // 盖亚契约检查（保护某些地块不被破坏）
                try
                {
                    if (!MapAction.checkTileDamageGaiaCovenant(tile, true))
                    {
                        return false;
                    }
                }
                catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }

                // 跳过液体地块（海洋等）
                if (tile.Type != null && tile.Type.liquid)
                {
                    // 维度打击可以移除液体
                    if (destructionType == TerrainDestructionType.DimensionalStrike)
                    {
                        try { MapAction.removeLiquid(tile); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }
                    }
                    else
                    {
                        return false;
                    }
                }

                // v0.19.14 渲染安全门：图形缺失的 tile 不做任何破坏（见 IsTileRenderSafe 注释）
                if (!IsTileRenderSafe(tile))
                {
                    Code.Core.DSDebug.Verbose("[地形破坏] 跳过渲染不安全地块: " + tile.pos.x + "," + tile.pos.y);
                    return false;
                }

                // 应用地形破坏（v0.18.77：原版 explodeTile 中心格全强度——
                // 单位爆炸伤害 + 炸树(explodable) + 炸坑(decreaseTile) + 烧焦 + 随机火）
                MapAction.explodeTile(tile, 0f, 1f, tile, options);

                // 建筑伤害（explodeTile 只炸液体建筑；applyTileDamage 的 damage_buildings 逻辑手动补，含非城市建筑）
                if (options.damage_buildings && tile.hasBuilding() && tile.building.isAlive())
                {
                    try { tile.building.getHit(options.damage); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }
                }

                // 固定点火（参考西幻世界：fire_timestamp + setFireData）
                if (options.set_fire && tile.Type != null && !tile.Type.liquid)
                {
                    try { tile.data.fire_timestamp = World.world.getCurWorldTime(); tile.setFireData(true); } catch { }
                }

                // 加热
                if (options.add_heat != 0)
                {
                    try { World.world.heat.addTile(tile, options.add_heat); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }
                }

                // 移除龙卷风
                if (options.remove_tornado)
                {
                    try { MapAction.tryRemoveTornadoFromTile(tile); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }
                }

                // 应用特定类型的额外效果
                switch (destructionType)
                {
                    case TerrainDestructionType.DimensionalStrike:
                        // 维度打击：降低地块高度（v0.19.6 连续两次降格=陆地打没，露沙/水）
                        // v0.19.14：每步降格前重新取格并校验——还能降才降，杜绝把 tile 降进渲染死区
                        if (CanDecrease(tile))
                        {
                            try { MapAction.decreaseTile(tile, false, "flash"); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }
                            tile = RefreshTileRef(tile);
                        }
                        if (tile != null && CanDecrease(tile))
                        {
                            try { MapAction.decreaseTile(tile, false, "flash"); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }
                            tile = RefreshTileRef(tile);
                        }
                        // 维度打击：部分地块涌出岩浆（v0.19.6 真神级"打出岩浆"观感；v0.19.14 排除液体与渲染死区）
                        if (tile != null && IsTileRenderSafe(tile) && !tile.Type.liquid && UnityEngine.Random.value < 0.4f)
                        {
                            try { LavaHelper.addLava(tile); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 岩浆化失败: " + ex.Message); }
                        }
                        break;

                    case TerrainDestructionType.DivinePunishment:
                        // 神罚：额外加热
                        try { World.world.heat.addTile(tile, 10); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }
                        // v0.19.6 超神神罚：陆地打没 + 高概率岩浆化（战斗之神把陆地打出岩浆）
                        // v0.19.14：降格前校验
                        if (CanDecrease(tile))
                        {
                            try { MapAction.decreaseTile(tile, false, "flash"); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 操作失败: " + ex.Message); }
                            tile = RefreshTileRef(tile);
                        }
                        if (tile != null && IsTileRenderSafe(tile) && !tile.Type.liquid && UnityEngine.Random.value < 0.55f)
                        {
                            try { LavaHelper.addLava(tile); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 岩浆化失败: " + ex.Message); }
                        }
                        break;

                    case TerrainDestructionType.EnergyStorm:
                        // v0.19.6 登神能量风暴：部分地块岩浆化（能量宣泄灼烧大地成岩浆）
                        // v0.19.14：排除液体与渲染死区
                        if (IsTileRenderSafe(tile) && !tile.Type.liquid && UnityEngine.Random.value < 0.3f)
                        {
                            try { LavaHelper.addLava(tile); } catch (Exception ex) { Code.Core.DSDebug.Warning("[GeneCultivation] 岩浆化失败: " + ex.Message); }
                        }
                        break;
                }

                return true;
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose($"[地形破坏] ApplyDestructionToTile 异常: {e.Message}");
                return false;
            }
        }

        // ============================================================
        // v0.21.0 战斗之神级破坏增强（分级力度 + 圆形岩浆坑 + 直线裂缝 + 脚下保护）
        // 设计参考「战斗之神」Ruin 的机制思路（境界×招式轻重→破坏力分级，
        // 重击才炸坑、终极才出岩浆），API 组合/参数/流程均为本项目原创。
        // ============================================================

        /// <summary>招式轻重（与招式编排语义一致，独立定义避免跨命名空间耦合）。</summary>
        public enum DestructionScale { Light = 0, Heavy = 1, Ultimate = 2 }

        /// <summary>境界 tier → 基础破坏力（0..4）。档位映射按超神机械师世界观。</summary>
        public static int ForceForTier(int tier)
        {
            if (tier >= 13) return 4;   // 真神/超神：天地崩裂
            if (tier >= 11) return 3;   // 登神：焚山煮海
            if (tier >= 9) return 2;    // 圣：地裂
            if (tier >= 6) return 1;    // 灵/使徒：焦痕
            return 0;                   // 凡/玄：不动大地
        }

        /// <summary>最终破坏力（基础 + 轻重调整，夹到 0..4）。</summary>
        public static int FinalForce(int tier, DestructionScale scale, int extra = 0)
        {
            int f = ForceForTier(tier);
            switch (scale)
            {
                case DestructionScale.Light: f--; break;
                case DestructionScale.Heavy: f++; break;
                case DestructionScale.Ultimate: f += 2; break;
            }
            return Mathf.Clamp(f + extra, 0, 4);
        }

        /// <summary>影响范围缩放：境界与轻重共同决定（参考 Ruin 半径乘子思路，参数自定）。</summary>
        private static float ScaleRadius(int tier, DestructionScale scale, float baseR)
        {
            float k = scale == DestructionScale.Light ? 0.7f : scale == DestructionScale.Heavy ? 1.3f : 1.8f;
            float tierK = tier >= 13 ? 1.7f : tier >= 11 ? 1.4f : tier >= 9 ? 1.25f : tier >= 6 ? 1.12f : 1f;
            return Mathf.Clamp(baseR * tierK * k, 0.6f, 16f);
        }

        /// <summary>
        /// 圆形破坏（Blast）：force≥1 才动手；force 2+ 炸坑降格，3+ 岩浆化（fire 且非液体），
        /// 4 二次降格（"陆地打没"）。keep 为施法者脚下——只烧焦、绝不挖/岩浆（保护立足）。
        /// </summary>
        public static int Blast(Actor by, Vector2 at, float baseRadius, DestructionScale scale,
            Vector2? keep = null, bool fire = true, int extra = 0)
        {
            if (World.world == null) return 0;
            int tier = by != null ? Code.Data.CultivationData.GetRealmTier(by) : 0;
            int force = FinalForce(tier, scale, extra);
            if (force <= 0) return 0;
            float r = ScaleRadius(tier, scale, baseRadius);
            int count = 0;
            int minX = Mathf.Max(0, (int)(at.x - r));
            int maxX = Mathf.Min(MapBox.width - 1, (int)(at.x + r));
            int minY = Mathf.Max(0, (int)(at.y - r));
            int maxY = Mathf.Min(MapBox.height - 1, (int)(at.y + r));
            bool magma = fire && force >= 3;
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (Vector2.Distance(at, new Vector2(x, y)) > r) continue;
                    // 边缘衰减：轻击只在近中心动手
                    float edge = 1f - Vector2.Distance(at, new Vector2(x, y)) / r;
                    if (force <= 1 && UnityEngine.Random.value > 0.4f + edge * 0.6f) continue;
                    if (keep.HasValue && Vector2.Distance(keep.Value, new Vector2(x, y)) < 1.2f) continue;
                    if (ApplyForceToTile(x, y, force, magma)) count++;
                }
            }
            if (count > 0)
            {
                try { World.world.resetRedrawTimer(); } catch { }
                Quake(force, at);
            }
            return count;
        }

        /// <summary>
        /// 直线裂缝（Furrow）：从 a 到 b 沿线裂开成沟壑（十字采样，宽≈width），
        /// 土/空间/重击系专属。keep 脚下保护。
        /// </summary>
        public static int Furrow(Actor by, Vector2 a, Vector2 b, float width, DestructionScale scale,
            Vector2? keep = null, int extra = 0)
        {
            if (World.world == null) return 0;
            int tier = by != null ? Code.Data.CultivationData.GetRealmTier(by) : 0;
            int force = FinalForce(tier, scale, extra);
            if (force <= 0) return 0;
            float w = Mathf.Clamp(width * (tier >= 13 ? 1.7f : tier >= 11 ? 1.4f : tier >= 9 ? 1.25f : 1f), 0.5f, 6f);
            Vector2 dir = b - a;
            float dist = dir.magnitude;
            if (dist < 0.5f) dist = 1f;
            dir /= dist;
            int steps = Mathf.CeilToInt(dist / w);
            bool magma = force >= 3;
            int count = 0;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = a + dir * (w * i);
                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        if (ox != 0 && oy != 0) continue;   // 十字采样成线
                        int x = (int)(p.x + ox);
                        int y = (int)(p.y + oy);
                        if (x < 0 || y < 0 || x >= MapBox.width || y >= MapBox.height) continue;
                        if (keep.HasValue && Vector2.Distance(keep.Value, new Vector2(x, y)) < 1.2f) continue;
                        // 裂缝主体比 Blast 深一档（开沟），岩浆随 force
                        if (ApplyForceToTile(x, y, Mathf.Max(1, force), magma)) count++;
                    }
                }
            }
            if (count > 0)
            {
                try { World.world.resetRedrawTimer(); } catch { }
                Quake(force, a);
            }
            return count;
        }

        /// <summary>相机震动（Quake）：按破坏力分级，只有进入镜头附近才震。</summary>
        public static void Quake(int force, Vector2 at)
        {
            if (World.world == null || force <= 0) return;
            try
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(at.x, at.y, 0f));
                if (sp.z < 0f || sp.x < -200f || sp.x > Screen.width + 200f ||
                    sp.y < -200f || sp.y > Screen.height + 200f) return;
                float intensity = 0.1f + force * 0.14f;      // 0.1→0.66，重击才明显
                float duration = 0.14f + force * 0.18f;      // 0.14→0.86s
                World.world.startShake(duration, 0.04f, intensity);
            }
            catch (Exception e)
            {
                Code.Core.DSDebug.Verbose("[地形破坏] Quake 异常: " + e.Message);
            }
        }

        /// <summary>对单格按力度应用破坏（force 2+ 降格、3+ 岩浆、4 二次降格）。</summary>
        private static bool ApplyForceToTile(int x, int y, int force, bool magma)
        {
            WorldTile tile = World.world.GetTileSimple(x, y);
            if (tile == null || !IsTileRenderSafe(tile)) return false;
            try { if (!MapAction.checkTileDamageGaiaCovenant(tile, true)) return false; } catch { return false; }
            if (tile.Type != null && tile.Type.liquid) return false;
            bool changed = false;
            if (force >= 1)
            {
                try
                {
                    // 轻量烧焦（不带 shake：震动由 Blast/Furrow 统一 Quake 触发）
                    TerraformOptions opt = new TerraformOptions
                    {
                        explode_tile = true,
                        damage_buildings = true,
                        add_burned = true,
                        add_heat = 8 + force * 4
                    };
                    MapAction.explodeTile(tile, 0f, 1f, tile, opt);
                    changed = true;
                }
                catch { }
            }
            if (force >= 2 && CanDecrease(tile))
            {
                try { MapAction.decreaseTile(tile, false, "flash"); tile = RefreshTileRef(tile); changed = true; } catch { }
            }
            if (magma && tile != null && IsTileRenderSafe(tile) && !tile.Type.liquid && UnityEngine.Random.value < 0.45f)
            {
                try { LavaHelper.addLava(tile); changed = true; } catch { }
            }
            if (force >= 4 && tile != null && CanDecrease(tile))
            {
                try { MapAction.decreaseTile(tile, false, "flash"); changed = true; } catch { }
            }
            return changed;
        }
    }
}
