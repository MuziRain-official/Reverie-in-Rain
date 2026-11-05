using System;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 界面动画基类。UIScreen 在 Show / Hide 时调用它，所以动画本身不用关心是进入还是退出，
    /// 由 UIScreen 上的 animIn / animOut 两个字段决定挂哪个动画。
    ///
    /// 一个缩放入场的写法：
    ///   public class ScaleIn : ScreenAnim
    ///   {
    ///       public override void Animate(Transform target, Action onFinished)
    ///       {
    ///           target.localScale = Vector3.one * 0.8f;
    ///           // ……用协程或 Timer 把缩放补到 1……
    ///           onFinished();
    ///       }
    ///   }
    ///
    /// 约定：onFinished 必须调用（哪怕时长是 0），否则界面不会停用，也不会广播 ui.shown / ui.hidden。
    /// 动画可以留空，留空时 UIScreen 会立即完成切换。
    /// </summary>
    public abstract class ScreenAnim : MonoBehaviour
    {
        /// <summary>播放动画。</summary>
        /// <param name="target">要动画的对象，即 UIScreen 的 transform</param>
        /// <param name="onFinished">播放完成回调，必须调用</param>
        public abstract void Animate(Transform target, Action onFinished);
    }
}
