namespace Reverie.Framework
{
    /// <summary>
    /// UI 层。声明顺序即显示顺序：越靠后，画在越上面。
    ///
    /// UIManager 启动时按这个顺序创建层节点（子物体顺序决定渲染顺序），
    /// 所以新增层要同时改这里和 <see cref="UIManager"/> 的 Build。
    ///
    /// 分层规则：
    ///   - 栈式层（Window / Popup）：同一时刻只显示一个界面，关闭后自动回到上一个；
    ///   - 平铺层（其余）：层内多个界面可以同时显示，互不影响。
    /// </summary>
    public enum UILayerType
    {
        /// <summary>场景层：主菜单、地图等整屏界面。</summary>
        Scene,

        /// <summary>常驻 HUD：生命、能量、交互提示。</summary>
        Hud,

        /// <summary>窗口层：背包、暂停菜单。栈式，同一时刻只显示一个。</summary>
        Window,

        /// <summary>弹窗层：确认框、对话框。栈式，盖在窗口上面。</summary>
        Popup,

        /// <summary>提示层：飘字、拾取提示。平铺，通常配合计时器自动消失。</summary>
        Toast,

        /// <summary>加载层：过场遮罩，盖在所有层最上面。</summary>
        Loading,
    }
}
