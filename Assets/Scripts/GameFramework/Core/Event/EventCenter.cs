using System;
using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 事件中心：模块间解耦通信的总线（字符串 key + 单中心版）。
    /// 支持：无参/带参事件、owner 自动退订、立即派发。
    ///
    /// 用法：
    ///   var e = EventCenter.Instance;
    ///   e.Subscribe("Hurt", OnHurt);                  // 无参
    ///   e.Subscribe&lt;int&gt;("Hurt", dmg =&gt; ...);         // 带参
    ///   e.Subscribe(this, OnHurt);                    // 带 owner（销毁时统一退订）
    ///   e.Publish("Hurt");                            // 立即
    ///   e.Publish("Hurt", 10);                        // 立即带参
    ///   e.UnsubscribeAll(this);                       // 对象销毁时调用
    ///
    /// 约定：一个事件名只对应一种参数类型（不要同名又发 int 又发 float）。
    /// 订阅与发布都会校验签名，类型不一致直接报错，不会静默丢弃。
    /// </summary>
    public class EventCenter : Singleton<EventCenter>
    {
        /// <summary>一条订阅：Owner 为 null 表示全局订阅，不随 owner 退订。</summary>
        private struct Subscription
        {
            public object Owner;
            public Delegate Callback;
        }

        // 订阅表：事件名 -> 订阅列表
        private readonly Dictionary<string, List<Subscription>> _subs = new();

        // 事件名 -> 参数类型（typeof(void) 表示无参事件）。首次订阅或发布时登记，之后强校验。
        private readonly Dictionary<string, Type> _signatures = new();

        // ============ 订阅 ============

        public void Subscribe(string name, Action action) => Add(name, null, action, typeof(void));
        public void Subscribe<T>(string name, Action<T> action) => Add(name, null, action, typeof(T));

        public void Subscribe(string name, object owner, Action action) => Add(name, owner, action, typeof(void));
        public void Subscribe<T>(string name, object owner, Action<T> action) => Add(name, owner, action, typeof(T));

        // ============ 退订 ============

        public void Unsubscribe(string name, Action action) => Remove(name, null, action);
        public void Unsubscribe<T>(string name, Action<T> action) => Remove(name, null, action);

        public void Unsubscribe(string name, object owner, Action action) => Remove(name, owner, action);
        public void Unsubscribe<T>(string name, object owner, Action<T> action) => Remove(name, owner, action);

        /// <summary>退订某个 owner 订阅的全部事件（对象销毁时调用，避免回调泄漏）。</summary>
        public void UnsubscribeAll(object owner)
        {
            if (owner == null) return;
            foreach (var kv in _subs)
                kv.Value.RemoveAll(s => s.Owner == owner);
        }

        // ============ 立即派发 ============
        // 对订阅列表拍摄快照后再遍历：回调中增删订阅不会影响本次派发，
        // 避免「删除排在前面的元素导致列表前移、当前回调被重复执行」的问题。
        // 语义：派发开始时的订阅者本次都会收到事件，派发过程中的变更从下一次生效。

        public void Publish(string name)
        {
            if (!CheckSignature(name, typeof(void))) return;
            if (!_subs.TryGetValue(name, out var list) || list.Count == 0) return;

            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                (snapshot[i].Callback as Action)?.Invoke();
        }

        public void Publish<T>(string name, T e)
        {
            if (!CheckSignature(name, typeof(T))) return;
            if (!_subs.TryGetValue(name, out var list) || list.Count == 0) return;

            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                (snapshot[i].Callback as Action<T>)?.Invoke(e);
        }

        // ============ 内部 ============

        private void Add(string name, object owner, Delegate cb, Type argType)
        {
            if (string.IsNullOrEmpty(name) || cb == null) return;
            if (!CheckSignature(name, argType)) return;

            if (!_subs.TryGetValue(name, out var list))
                _subs[name] = list = new List<Subscription>();
            list.Add(new Subscription { Owner = owner, Callback = cb });
        }

        /// <summary>
        /// 订阅与发布共用的签名校验：事件名首次出现时登记参数类型，之后必须一致。
        /// 返回 false 表示类型冲突，调用方应中止（已记录 Error）。
        /// </summary>
        private bool CheckSignature(string name, Type argType)
        {
            if (_signatures.TryGetValue(name, out var existing))
            {
                if (existing == argType) return true;

                Log.Error($"事件 \"{name}\" 已注册为 {Describe(existing)}，不能再用 {Describe(argType)} 订阅/发布", LogChannel.Core);
                return false;
            }

            _signatures[name] = argType;
            return true;
        }

        private static string Describe(Type t) => t == typeof(void) ? "无参" : t.Name;

        private void Remove(string name, object owner, Delegate cb)
        {
            if (!_subs.TryGetValue(name, out var list)) return;
            // owner == null 表示「退订任意 owner 的该回调」；owner 非 null 时精确匹配
            list.RemoveAll(s => s.Callback == cb && (owner == null || s.Owner == owner));
        }

        /// <summary>释放时清空订阅表，避免残留回调在模块释放后被派发。</summary>
        protected override void OnRelease()
        {
            _subs.Clear();
            _signatures.Clear();
        }
    }
}
