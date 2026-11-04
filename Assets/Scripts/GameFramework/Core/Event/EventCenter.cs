using System;
using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 事件中心：模块之间通过事件名（字符串）通信的总线。
    /// 支持订阅、发布、退订，分为不带参和带参两种事件。
    ///
    /// 用法：
    ///   var e = EventCenter.Instance;
    ///   e.Subscribe("Hurt", OnHurt);                  // 不带参
    ///   e.Subscribe&lt;int&gt;("Hurt", OnHurtInt);          // 带参
    ///   e.Publish("Hurt");                            // 只通知不带参的订阅者
    ///   e.Publish("Hurt", 10);                        // 只通知 Action&lt;int&gt; 的订阅者
    ///   e.Unsubscribe("Hurt", OnHurt);                // 按回调退订
    ///   e.Subscribe(this, OnHurt);                    // 带 owner 订阅
    ///   e.UnsubscribeAll(this);                       // 对象销毁时统一退订
    ///
    /// 约定：同一个事件名的带参订阅只使用一种参数类型（不要同名又发 int 又发 float）。
    /// </summary>
    public class EventCenter : Singleton<EventCenter>
    {
        // 一条订阅记录：Owner 记录订阅者对象（可为 null），Callback 是用户传进来的回调方法。
        private struct Subscription
        {
            public object Owner;
            public Delegate Callback;
        }

        // 事件名 -> 订阅列表。带参和不带参的订阅都放在同一个列表里，发布时按类型筛选。
        private readonly Dictionary<string, List<Subscription>> _events = new();

        // ============ 订阅 ============

        public void Subscribe(string name, Action callback) => Add(name, null, callback);
        public void Subscribe<T>(string name, Action<T> callback) => Add(name, null, callback);
        public void Subscribe(string name, object owner, Action callback) => Add(name, owner, callback);
        public void Subscribe<T>(string name, object owner, Action<T> callback) => Add(name, owner, callback);

        // ============ 退订 ============

        public void Unsubscribe(string name, Action callback) => Remove(name, callback);
        public void Unsubscribe<T>(string name, Action<T> callback) => Remove(name, callback);

        /// <summary>退订某个 owner 的全部事件（对象销毁时调用，避免回调泄漏）。</summary>
        public void UnsubscribeAll(object owner)
        {
            if (owner == null) return;
            foreach (var list in _events.Values)
                list.RemoveAll(s => s.Owner == owner);
        }

        // ============ 发布 ============
        // 先把订阅列表复制一份再遍历：订阅者在回调里增删订阅不会打乱本次派发的顺序。

        public void Publish(string name)
        {
            if (!_events.TryGetValue(name, out var list)) return;

            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    if (snapshot[i].Callback is Action callback)
                        callback();
                }
                catch (Exception ex)
                {
                    Log.Error($"事件 \"{name}\" 的回调抛出异常：{ex}", LogChannel.Core);
                }
            }
        }

        public void Publish<T>(string name, T arg)
        {
            if (!_events.TryGetValue(name, out var list)) return;

            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    if (snapshot[i].Callback is Action<T> callback)
                        callback(arg);
                }
                catch (Exception ex)
                {
                    Log.Error($"事件 \"{name}\" 的回调抛出异常：{ex}", LogChannel.Core);
                }
            }
        }

        // ============ 内部 ============

        private void Add(string name, object owner, Delegate callback)
        {
            if (string.IsNullOrEmpty(name) || callback == null) return;

            if (!_events.TryGetValue(name, out var list))
                _events[name] = list = new List<Subscription>();
            list.Add(new Subscription { Owner = owner, Callback = callback });
        }

        private void Remove(string name, Delegate callback)
        {
            if (!_events.TryGetValue(name, out var list)) return;
            list.RemoveAll(s => s.Callback == callback);
        }

        /// <summary>释放时清空订阅表，避免残留回调在模块释放后被派发。</summary>
        protected override void OnRelease() => _events.Clear();
    }
}
