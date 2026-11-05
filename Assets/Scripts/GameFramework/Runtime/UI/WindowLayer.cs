using System.Collections.Generic;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 栈式 UI 层：同一时刻只显示一个窗口，打开新的会隐藏当前的，关闭后自动回到上一个。
    /// 窗口层与弹窗层用这个，其余层用 PanelLayer。
    ///
    /// 历史栈记录的是「打开顺序」：
    ///   Show A → 栈 [A]
    ///   Show B → 栈 [A, B]，A 被隐藏但留在栈里
    ///   Back() → 弹掉 B，恢复 A
    ///
    /// 约定：
    ///   - 只能关闭栈顶（当前窗口），关其他窗口会被拒绝，否则历史栈会乱；
    ///   - 重复打开同一个窗口会按历史再压一次，Back 会原路退回，这是有意为之；
    ///   - 隐藏不等于销毁，窗口实例一直复用。
    /// </summary>
    public class WindowLayer : UILayer
    {
        // 打开顺序栈：栈顶是当前窗口，返回时弹一个、恢复一个
        private readonly Stack<UIScreen> history = new Stack<UIScreen>();

        /// <summary>当前显示的窗口，没有则为 null。</summary>
        public UIScreen Current { get; private set; }

        /// <summary>打开窗口：隐藏当前窗口并压栈；已经是当前窗口时忽略。</summary>
        public override void ShowScreen(UIScreen screen, object props = null)
        {
            if (screen == null)
            {
                Log.Warn("[WindowLayer] 要显示的窗口为空，已忽略", LogChannel.UI);
                return;
            }

            if (screen == Current)
            {
                Log.Debug($"[WindowLayer] 窗口已打开，忽略重复请求：{screen.UIScreenId}", LogChannel.UI);
                return;
            }

            // 隐藏上一个窗口，但保留在历史栈里（返回时还要回来）
            if (Current != null)
                Current.Hide();

            history.Push(screen);
            screen.Show(props);
            Current = screen;
        }

        /// <summary>关闭窗口：只能关当前窗口，弹栈后恢复上一个。</summary>
        public override void HideScreen(UIScreen screen)
        {
            if (screen == null) return;

            // 只能关闭当前窗口，否则历史栈会乱
            if (screen != Current)
            {
                Log.Warn($"[WindowLayer] 只能关闭当前窗口，忽略请求：{screen.UIScreenId}", LogChannel.UI);
                return;
            }

            screen.Hide();
            history.Pop();
            Current = null;

            // 栈里可能残留已销毁的窗口（被销毁或场景卸载），先跳过再恢复
            while (history.Count > 0 && history.Peek() == null)
                history.Pop();

            if (history.Count == 0) return;

            // 回到上一个窗口，并带回它打开时的参数
            UIScreen prev = history.Peek();
            Current = prev;
            prev.Show(prev.Props);
        }

        /// <summary>关闭当前窗口并恢复上一个；没有可关的窗口时返回 false。</summary>
        public bool Back()
        {
            if (Current == null) return false;

            HideScreen(Current);
            return true;
        }

        /// <summary>隐藏本层所有窗口并清空历史栈。</summary>
        public override void HideAllScreens(bool animate = true)
        {
            base.HideAllScreens(animate);
            history.Clear();
            Current = null;
        }
    }
}
