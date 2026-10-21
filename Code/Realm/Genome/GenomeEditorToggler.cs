// ============================================================
// 文件: GenomeEditorToggler.cs
// 模组: 基因修炼 (GeneCultivation)
// 职责: P3 渲染批1——F8 调试模式切换器（替换 R2 GenomePrototypeToggler）
// 说明: 渲染批2 的 GenomeEditorHost 会在打开单位窗基因页签时自动 SetTarget(actor) 激活会话；
//       F8 仅用于调试模式（无单位绑定，渲染示例基因组预览管线），正常游玩无需按键。
// ============================================================

using UnityEngine;

namespace Code.Realm.Genome
{
    /// <summary>渲染会话调试切换器（ModClass.OnModLoad 实例化）</summary>
    public class GenomeEditorToggler : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                GenomeEditorSession.ToggleDebug();
            }
        }
    }
}
