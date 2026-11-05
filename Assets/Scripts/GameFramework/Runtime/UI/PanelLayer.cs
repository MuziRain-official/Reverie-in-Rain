using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 平铺式 UI 层：层内的界面互不影响，可以同时显示（HUD、提示、加载都用它）。
    ///
    /// 这里只负责把「显示 / 隐藏」转给界面本身，登记与查找由基类 UILayer 维护。
    /// </summary>
    public class PanelLayer : UILayer
    {
        /// <summary>直接打开界面；已经打开时按 UIScreen.Show 的规则刷新参数。</summary>
        public override void ShowScreen(UIScreen screen, object props = null)
        {
            screen.Show(props);
        }

        /// <summary>直接隐藏界面，不弹栈、不影响同层其他界面。</summary>
        public override void HideScreen(UIScreen screen)
        {
            screen.Hide();
        }
    }
}
