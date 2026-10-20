
using System.Reflection;
using UnityEngine;

namespace Code.Core
{
    /// <summary>
    /// 反射工具类（v0.21.47 反射版）
    /// MapBox.era_manager / map_stats 为 internal，直接访问抛 FieldAccessException。
    /// 改用反射 + FieldInfo 缓存。
    /// </summary>
    public static class DSReflectionHelper
    {
        private static FieldInfo _eraManagerField;
        private static FieldInfo _mapStatsField;
        private static bool _resolved = false;

        private static void EnsureResolved()
        {
            if (_resolved) return;
            _resolved = true;
            try
            {
                _eraManagerField = typeof(MapBox).GetField("era_manager",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _mapStatsField = typeof(MapBox).GetField("map_stats",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (System.Exception e) { DSDebug.Warning("[DSReflectionHelper] 字段解析失败: " + e.Message); }
        }

        /// <summary>获取 MapBox.map_stats 字段（反射）</summary>
        public static MapStats GetMapStats()
        {
            try
            {
                if (World.world == null) return null;
                EnsureResolved();
                if (_mapStatsField == null) return null;
                return _mapStatsField.GetValue(World.world) as MapStats;
            }
            catch { return null; }
        }

        /// <summary>获取 MapBox.era_manager 字段（反射）</summary>
        public static WorldAgeManager GetEraManager()
        {
            try
            {
                if (World.world == null) return null;
                EnsureResolved();
                if (_eraManagerField == null) return null;
                return _eraManagerField.GetValue(World.world) as WorldAgeManager;
            }
            catch { return null; }
        }

        /// <summary>
        /// 获取世界时间（map_stats.world_time）
        /// </summary>
        public static float GetWorldTime()
        {
            try
            {
                MapStats mapStats = GetMapStats();
                if (mapStats == null) return 0f;
                return (float)mapStats.world_time;
            }
            catch { return 0f; }
        }

        /// <summary>
        /// 获取下一个ID（map_stats.getNextId(prefix)）
        /// </summary>
        public static long? GetNextId(string prefix)
        {
            try
            {
                MapStats mapStats = GetMapStats();
                if (mapStats == null) return null;
                return mapStats.getNextId(prefix);
            }
            catch { return null; }
        }

        /// <summary>
        /// 安全调用addStatusEffect（直接调用，publicized环境下可访问）
        /// </summary>
        public static void SafeAddStatusEffect(Actor actor, string statusId, float duration, bool addToUI = true)
        {
            if (actor == null) return;
            try
            {
                actor.addStatusEffect(statusId, duration, addToUI);
            }
            catch (System.Exception e)
            {
                DSDebug.Verbose($"SafeAddStatusEffect失败: {e.Message}");
            }
        }
    }
}
