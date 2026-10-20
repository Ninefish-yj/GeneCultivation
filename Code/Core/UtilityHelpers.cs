
using System;
using UnityEngine;

namespace Code.Core
{
    /// <summary>
    /// 状态检查辅助
    /// </summary>
    public static class StatusHelper
    {
        /// <summary>直接调用hasStatus方法</summary>
        public static bool HasStatus(BaseSimObject obj, string statusId)
        {
            try
            {
                if (obj == null || string.IsNullOrEmpty(statusId)) return false;
                return obj.hasStatus(statusId);
            }
            catch
            {
                return false;
            }
        }
    }
}

namespace Code.Data
{
    /// <summary>
    /// Actor数据访问器（v0.21.47 反射版）
    /// 运行时 Actor.data / Actor.stats 为 internal，直接访问抛 FieldAccessException。
    /// 改用反射 + FieldInfo 缓存，兼容 publicized / 非 publicized 环境。
    /// </summary>
    public static class ActorDataAccessor
    {
        private static System.Reflection.FieldInfo _dataField;
        private static System.Reflection.FieldInfo _statsField;
        private static bool _resolved = false;

        private static void EnsureResolved()
        {
            if (_resolved) return;
            _resolved = true;
            try
            {
                _dataField = typeof(Actor).GetField("data",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _statsField = typeof(Actor).GetField("stats",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }
            catch (System.Exception e) { Code.Core.DSDebug.Warning("[ActorDataAccessor] 字段解析失败: " + e.Message); }
        }

        /// <summary>初始化（反射模式，保留空方法兼容）</summary>
        public static void Init()
        {
            EnsureResolved();
            Code.Core.DSDebug.Verbose("[GeneCultivation] ActorDataAccessor 反射模式 data=" + (_dataField != null) + " stats=" + (_statsField != null));
        }

        /// <summary>获取 Actor 的 data 字段（反射）</summary>
        public static ActorData GetData(Actor actor)
        {
            if (actor == null) return null;
            EnsureResolved();
            if (_dataField == null) return null;
            try { return _dataField.GetValue(actor) as ActorData; }
            catch { return null; }
        }

        /// <summary>获取 Actor 的 stats 字段（反射）</summary>
        public static BaseStats GetStats(Actor actor)
        {
            if (actor == null) return null;
            EnsureResolved();
            if (_statsField == null) return null;
            try { return _statsField.GetValue(actor) as BaseStats; }
            catch { return null; }
        }

        /// <summary>设置 Actor 的 data 字段（反射，用于召唤单位初始化）</summary>
        public static void SetData(Actor actor, ActorData data)
        {
            if (actor == null) return;
            EnsureResolved();
            if (_dataField == null) return;
            try { _dataField.SetValue(actor, data); }
            catch { }
        }
    }
}
