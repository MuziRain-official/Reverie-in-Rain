using System;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 界面基类：统一生命周期与开关动画。子类只覆写 OnPropsSet / OnShow / OnHide，
    /// 不要自己写 SetActive、自己播动画或手动销毁。
    ///
    ///   UIManager.Instance.Register(UILayerType.Window, "Pause", pauseScreen);
    ///   var screen = UIManager.Instance.Show(UILayerType.Window, "Pause", args);
    ///
    /// 生命周期：
    ///   Show：写入参数 → OnPropsSet → OnShow → 播进入动画 → 广播 ui.shown
    ///   Hide：OnHide → 播退出动画 → 停用 → 广播 ui.hidden
    ///
    /// 参数用 object 传递，子类在 OnPropsSet 里自己转成具体类型。
    /// 打开 / 隐藏完成会通过事件中心广播 ui.shown / ui.hidden（参数为界面 id），
    /// 适合外部系统监听（锁输入、压暗 HUD 等）；界面内部刷新直接写在 OnShow 里即可。
    /// </summary>
    public class UIScreen : MonoBehaviour
    {
        /// <summary>界面打开完成时广播的事件名，参数为界面 id。</summary>
        public const string EventShown = "ui.shown";

        /// <summary>界面隐藏完成时广播的事件名，参数为界面 id。</summary>
        public const string EventHidden = "ui.hidden";

        // 进入 / 退出动画，可选。留空表示直接切换，OnShow / OnHide 之后立即完成
        [SerializeField] private ScreenAnim animIn;
        [SerializeField] private ScreenAnim animOut;

        // 连续开关时让旧的动画回调失效：否则快速「关 → 开」会被上一次的退出动画重新关掉
        private int _animVersion;

        // 正在播放退出动画：期间视为已关闭，重复 Hide 会被忽略
        private bool _hiding;

        /// <summary>界面 id，由 UIManager 登记时写入。</summary>
        public string UIScreenId { get; internal set; }

        /// <summary>本次打开传入的参数，供 OnPropsSet 与窗口返回时恢复使用。</summary>
        public object Props { get; private set; }

        /// <summary>是否处于打开状态；退出动画播放期间视为已关闭。</summary>
        public bool IsShowing => gameObject.activeSelf && !_hiding;

        /// <summary>打开界面：激活、写参数、刷新内容，然后播进入动画。</summary>
        public void Show(object props = null)
        {
            Props = props;
            _hiding = false;
            gameObject.SetActive(true);

            OnPropsSet();
            OnShow();

            PlayAnim(animIn, () => EventCenter.Instance.Publish(EventShown, UIScreenId));
        }

        /// <summary>隐藏界面；animate 为 false 时跳过退出动画，立即停用（切场景 / 释放时用）。</summary>
        public void Hide(bool animate = true)
        {
            if (_hiding) return;
            if (!gameObject.activeSelf) return;

            _hiding = true;
            OnHide();

            // 不播动画就把 animOut 传空，PlayAnim 会立即完成并作废未完成的动画回调
            PlayAnim(animate ? animOut : null, FinishHide);
        }

        /// <summary>退出动画结束（或跳过）后真正停用界面，并广播隐藏事件。</summary>
        private void FinishHide()
        {
            _hiding = false;
            gameObject.SetActive(false);
            EventCenter.Instance.Publish(EventHidden, UIScreenId);
        }

        /// <summary>界面已激活、进入动画开始前调用。适合锁输入、暂停逻辑。</summary>
        protected virtual void OnShow() {}

        /// <summary>界面开始关闭时调用（此时还没停用）。适合退订事件、恢复输入。</summary>
        protected virtual void OnHide() {}

        /// <summary>每次 Show 都会调用，用来把 Props 填到界面上。</summary>
        protected virtual void OnPropsSet() {}

        /// <summary>播放动画，没有配动画时立即完成；期间被新一轮开关打断则丢弃本次回调。</summary>
        private void PlayAnim(ScreenAnim anim, Action onFinished)
        {
            int version = ++_animVersion;
            if (anim == null)
            {
                onFinished();
                return;
            }

            anim.Animate(transform, () =>
            {
                if (version == _animVersion) onFinished();
            });
        }
    }
}
