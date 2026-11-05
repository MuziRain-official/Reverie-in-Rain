using System.Collections.Generic;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// UI 层基类：维护「id → 界面」登记表，并决定怎么显示 / 隐藏界面。
    /// 业务不直接调用这里，统一走 UIManager；子类只需实现 ShowScreen / HideScreen，
    /// 登记、查找、批量隐藏这些公共逻辑都在本类。
    ///
    /// 登记约定：
    ///   - 同一个层里 id 唯一，重复登记会打警告并保留先登记的那个；
    ///   - 登记时会把界面挂到层节点下、先隐藏，等 Show 再打开；
    ///   - 界面 id 由 UIManager 和业务约定，建议用常量类集中管理，别散落字符串。
    /// </summary>
    public abstract class UILayer : MonoBehaviour
    {
        // id → 界面。界面在层里的父节点顺序不影响逻辑，渲染顺序由层顺序决定
        protected readonly Dictionary<string, UIScreen> Screens = new Dictionary<string, UIScreen>();

        /// <summary>子类实现：如何显示一个界面（平铺还是入栈）。</summary>
        public abstract void ShowScreen(UIScreen screen, object props = null);

        /// <summary>子类实现：如何隐藏一个界面。</summary>
        public abstract void HideScreen(UIScreen screen);

        /// <summary>按 id 取界面，没有返回 null（Show 时没登记会用警告提示）。</summary>
        public UIScreen Get(string id) => Screens.TryGetValue(id, out UIScreen screen) ? screen : null;

        /// <summary>
        /// 登记界面：写入 id、挂到本层下、先隐藏，等 Show 再打开。
        /// id 为空、界面为空、id 重复都会被拒绝并打日志。
        /// </summary>
        public virtual void Register(string id, UIScreen screen)
        {
            if (string.IsNullOrEmpty(id))
            {
                Log.Error("注册界面失败，id 为空", LogChannel.UI);
                return;
            }

            if (screen == null)
            {
                Log.Error($"注册界面失败，界面为空：{id}", LogChannel.UI);
                return;
            }

            if (Screens.ContainsKey(id))
            {
                Log.Warn($"该层已有界面 id：{id}，本次注册被忽略", LogChannel.UI);
                return;
            }

            screen.UIScreenId = id;
            screen.transform.SetParent(transform, false);
            screen.gameObject.SetActive(false);   // 登记后先隐藏，等 Show 再打开
            Screens.Add(id, screen);
        }

        /// <summary>从登记表移除（不销毁、不隐藏）；需要回收界面时由业务自己 Destroy。</summary>
        public virtual void Unregister(string id) => Screens.Remove(id);

        /// <summary>按 id 打开；没登记过会打警告，不会自动加载预制体。</summary>
        public void ShowById(string id, object props = null)
        {
            UIScreen screen = Get(id);
            if (screen == null)
            {
                Log.Warn($"界面未登记：{id}", LogChannel.UI);
                return;
            }

            ShowScreen(screen, props);
        }

        /// <summary>按 id 关闭；没登记过会打警告。</summary>
        public void HideById(string id)
        {
            UIScreen screen = Get(id);
            if (screen == null)
            {
                Log.Warn($"界面未登记：{id}", LogChannel.UI);
                return;
            }

            HideScreen(screen);
        }

        /// <summary>隐藏本层所有界面；animate 为 false 时不播退出动画（切场景 / 释放时用）。</summary>
        public virtual void HideAllScreens(bool animate = true)
        {
            // 快照遍历：界面在 OnHide 里增删界面时不会打乱本次循环
            foreach (UIScreen screen in new List<UIScreen>(Screens.Values))
                screen.Hide(animate);
        }
    }
}
