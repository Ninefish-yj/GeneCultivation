// ============================================================
// 文件: GenomeEditorToggler.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P3 渲染接管——基因编辑器调试模式驱动（v0.24.0 改为配置开关，手机无 F8）
// 配置: GeneCultivationConfig.EnableGeneEditorDebug（NML 设置界面开关，手机可操作）
//   - 开：激活调试态——亚种编辑器基因页签渲染示例基因组（只读预览渲染管线，
//         不写回亚种；调试按钮 randomize/shuffle 已被 Bridge 门禁拦截）
//   - 关（默认）：调试态退出，亚种编辑器 100% 原版（门禁回归，零污染）
// 单位窗基因页签主路径（GenomeEditorHost → SetTarget(actor)）不受此开关影响，
// 始终渲染真实基因组。
// PC 快捷：F8 同步切换（与配置一致）；手机直接在游戏设置界面拨开关即可。
// ============================================================

using UnityEngine;
using Code.Core;

namespace Code.Realm.Genome
{
    /// <summary>基因编辑器调试模式驱动器（ModClass.OnModLoad 实例化）</summary>
    public class GenomeEditorToggler : MonoBehaviour
    {
        private void Update()
        {
            bool cfg = GeneCultivationConfig.EnableGeneEditorDebug;

            // PC 快捷键：F8 同步切换配置（随后由下方逻辑生效）
            if (Input.GetKeyDown(KeyCode.F8))
            {
                cfg = !cfg;
                GeneCultivationConfig.EnableGeneEditorDebug = cfg;
            }

            // 调试态只在无单位绑定时存在（单位窗页签 Host 绑定单位后不干预）
            bool debugActive = GenomeEditorSession.Active && GenomeEditorSession.TargetActor == null;
            if (cfg && !debugActive)
            {
                GenomeEditorSession.ToggleDebug();
            }
            else if (!cfg && debugActive)
            {
                GenomeEditorSession.EndSession();
            }
        }
    }
}
