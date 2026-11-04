using System;
using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 事件中心：以事件名（字符串）为 key 的订阅/发布总线。
    /// 同一事件名可同时有带参和不带参的订阅者，发布时按回调类型各发各的。
    ///
    ///   var e = EventCenter.Instance;
    ///   e.Subscribe("Hurt", OnHurt);             // 不带参
    ///   e.Subscribe&lt;int&gt;("Hurt", OnHurtInt);     // 带参
    ///   e.Publish("Hurt");                       // 只通知 Action 订阅者
    ///   e.Publish("Hurt", 10);                   // 只通知 Action&lt;int&gt; 订阅者
    ///   e.Unsubscribe("Hurt", OnHurt);           // 按回调退订
    ///   e.Subscribe(this, OnHurt);               // 带 owner
    ///   e.UnsubscribeAll(this);                  // 对象销毁时统一退订
    ///
    /// 约定：同一事件名的带参订阅只用一种参数类型。
    /// </summary>
    public class EventCenter : Singleton<EventCenter>
    {
        // 一条订阅：Owner 为订阅者（可为 null），Callback 为回调方法。
        private struct Subscription
        {
            public object Owner;
            public Delegate Callback;
        }

        // 事件名 -> 订阅列表；带参与不带参混存，发布时按类型筛选。
        private readonly Dictionary<string, List<Subscription>> _events = new();

        // ============ 订阅 ============

        public void Subscribe(string name, Action callback) => Add(name, null, callback);
        public void Subscribe<T>(string name, Action<T> callback) => Add(name, null, callback);
        public void Subscribe(string name, object owner, Action callback) => Add(name, owner, callback);
        public void Subscribe<T>(string name, object owner, Action<T> callback) => Add(name, owner, callback);

        // ============ 退订 ============

        public void Unsubscribe(string name, Action callback) => Remove(name, callback);
        public void Unsubscribe<T>(string name, Action<T> callback) => Remove(name, callback);

        /// <summary>退订某个 owner 的全部事件（对象销毁时调用）。</summary>
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

        /// <summary>释放时清空订阅表。</summary>
        protected override void OnRelease() => _events.Clear();
    }
}
