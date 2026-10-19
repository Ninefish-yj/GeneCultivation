// ============================================================
// 模组自定义状态资产注册入口（v0.21.34）
// 职责：确保模组所有自定义状态在原版 StatusLibrary 中有真实资产，
//       否则原版 addStatusEffect(id) 会因 AssetManager.status.get(id)==null
//       静默 return false（状态栏不显示、hasStatus 查不到、原版行为不生效）。
// 分层：
//   · DSStatusAsset.Init() —— 权威注册器（17 个：境界技能 ds_ability_* + 修炼/维度状态），
//     v0.21.34 已改为直连注册（原反射路径在部分环境取不到类型，从未真正注册）。
//   · 本注册器 —— 补丁层：补 DSStatusAsset 未覆盖的战斗负面 weakened/slow
//     （原版无此状态资产，异能引擎 Aging/Weakness/ArmorBreak 标签依赖它们）。
// 幂等：两处注册均先 get(id) 判重；EnsureRegistered 进程内只执行一次。
// 纯注册层：不实现业务逻辑；各状态数值由 base_stats 原版属性与模组机制承担。
// ============================================================
using UnityEngine;

namespace Code.Core
{
    public static class DSStatusRegistry
    {
        private static bool _registered = false;

        /// <summary>注册模组自定义状态资产（幂等）。在 SafeAddStatusEffect 前调用，
        /// 确保 addStatusEffect 能命中真资产。</summary>
        public static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;
            try
            {
                // 权威注册器：17 个状态（技能/修炼/维度）；失败打 Warning 不影响后续
                DSStatusAsset.Init();
                if (AssetManager.status == null) return;

                // 补丁：异能引擎战斗负面（原版无 weakened/slow 状态资产）
                if (AssetManager.status.get(DSStatusManager.STATUS_WEAKENED) == null)
                {
                    AssetManager.status.add(Make(DSStatusManager.STATUS_WEAKENED, "ui/Icons/iconSlowness",
                        ("multiplier_damage", -0.3f), ("multiplier_speed", -0.15f)));
                }
                if (AssetManager.status.get(DSStatusManager.STATUS_SLOW) == null)
                {
                    AssetManager.status.add(Make(DSStatusManager.STATUS_SLOW, "ui/Icons/iconSlowness",
                        ("multiplier_speed", -0.3f)));
                }
                DSDebug.Verbose("[GeneCultivation] 状态资产注册入口完成");
            }
            catch (System.Exception dsEx)
            {
                DSDebug.Warning($"[GeneCultivation] 状态资产注册异常: {dsEx.Message}");
            }
        }

        private static StatusAsset Make(string id, string icon,
            params (string key, float val)[] stats)
        {
            var a = new StatusAsset
            {
                id = id,
                duration = 10f,
                animated = false,
                path_icon = icon,
                can_be_cured = false,
                loop = true,
                scale = 1f
            };
            for (int i = 0; i < stats.Length; i++)
                a.base_stats[stats[i].key] = stats[i].val;
            return a;
        }
    }
}
