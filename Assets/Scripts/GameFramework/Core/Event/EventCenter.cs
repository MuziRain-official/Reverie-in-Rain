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
    ///   e.Subscribe&lt;int&gt;("Hurt", dmg =&gt; ...);         // 带参（与无参通道互不冲突）
    ///   e.Subscribe(this, OnHurt);                    // 带 owner（销毁时统一退订）
    ///   e.Publish("Hurt");                            // 立即（只派发给无参订阅者）
    ///   e.Publish("Hurt", 10);                        // 立即带参（只派发给带参订阅者）
    ///   e.UnsubscribeAll(this);                       // 对象销毁时调用
    ///
    /// 约定：同一事件名的带参通道只对应一种参数类型（不要同名又发 int 又发 float）。
    /// 类型不一致直接报错，不会静默丢弃；同一 owner 对同一回调重复订阅会自动去重。
    /// </summary>
    public class EventCenter : Singleton<EventCenter>
    {
        /// <summary>一条订阅：Owner 为 null 表示全局订阅，不随 owner 退订。</summary>
        private struct Subscription
        {
            public object Owner;
            public Delegate Callback;
        }

        /// <summary>
        /// 一个事件名下的两条独立通道：无参（Action）与带参（Action&lt;T&gt;）。
        /// 两条通道互不冲突，可同时存在；带参通道的 VoidUsed / GenericType 首次订阅即登记，之后强校验。
        /// </summary>
        private class EventEntry
        {
            public bool VoidUsed;                  // 无参通道是否已登记
            public Type GenericType;               // 带参通道的参数类型；null 表示尚未登记
            public readonly List<Subscription> Void = new List<Subscription>();
            public readonly List<Subscription> Generic = new List<Subscription>();
        }

        // 订阅表：事件名 -> 该事件下的无参 / 带参通道
        private readonly Dictionary<string, EventEntry> _subs = new();

        // ============ 订阅 ============

        public void Subscribe(string name, Action action) => AddVoid(name, null, action);
        public void Subscribe<T>(string name, Action<T> action) => AddGeneric(name, null, action, typeof(T));

        public void Subscribe(string name, object owner, Action action) => AddVoid(name, owner, action);
        public void Subscribe<T>(string name, object owner, Action<T> action) => AddGeneric(name, owner, action, typeof(T));

        // ============ 退订 ============

        public void Unsubscribe(string name, Action action) => RemoveVoid(name, null, action);
        public void Unsubscribe<T>(string name, Action<T> action) => RemoveGeneric(name, null, action);

        public void Unsubscribe(string name, object owner, Action action) => RemoveVoid(name, owner, action);
        public void Unsubscribe<T>(string name, object owner, Action<T> action) => RemoveGeneric(name, owner, action);

        /// <summary>退订某个 owner 订阅的全部事件（对象销毁时调用，避免回调泄漏）。</summary>
        public void UnsubscribeAll(object owner)
        {
            if (owner == null)
            {
                Log.Warn("EventCenter.UnsubscribeAll 收到 null owner，已忽略；请检查调用方对象是否未初始化", LogChannel.Core);
                return;
            }

            foreach (var kv in _subs)
            {
                kv.Value.Void.RemoveAll(s => s.Owner == owner);
                kv.Value.Generic.RemoveAll(s => s.Owner == owner);
            }
        }

        // ============ 立即派发 ============
        // 对订阅列表拍摄快照后再遍历：回调中增删订阅不会影响本次派发，
        // 避免「删除排在前面的元素导致列表前移、当前回调被重复执行」的问题。
        // 语义：派发开始时的订阅者本次都会收到事件，派发过程中的变更从下一次生效。
        // 单个回调抛异常只记录并继续，避免一个订阅者打断其余订阅者。

        public void Publish(string name)
        {
            if (!_subs.TryGetValue(name, out var entry) || !entry.VoidUsed) return;
            if (entry.Void.Count == 0) return;

            var snapshot = entry.Void.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    var cb = snapshot[i].Callback as Action;
                    if (cb != null) cb.Invoke();
                    else Log.Error($"事件 \"{name}\" 的无参订阅回调类型不匹配，已跳过", LogChannel.Core);
                }
                catch (Exception ex)
                {
                    Log.Error($"事件 \"{name}\" 的无参订阅回调抛出异常：{ex}", LogChannel.Core);
                }
            }
        }

        public void Publish<T>(string name, T e)
        {
            // 发布不登记新类型：从未订阅过的事件名视为无订阅者直接返回（EVT-7），
            // 避免一次漏传 / 误发的调用永久锁死该事件名的参数类型。
            if (!_subs.TryGetValue(name, out var entry) || entry.GenericType == null) return;

            if (entry.GenericType != typeof(T))
            {
                Log.Error($"事件 \"{name}\" 已登记为 {Describe(entry.GenericType)}，不能用 {Describe(typeof(T))} 发布", LogChannel.Core);
                return;
            }

            if (entry.Generic.Count == 0) return;

            var snapshot = entry.Generic.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    var cb = snapshot[i].Callback as Action<T>;
                    if (cb != null) cb.Invoke(e);
                    else Log.Error($"事件 \"{name}\" 的带参订阅回调类型不匹配，已跳过", LogChannel.Core);
                }
                catch (Exception ex)
                {
                    Log.Error($"事件 \"{name}\" 的带参订阅回调抛出异常：{ex}", LogChannel.Core);
                }
            }
        }

        // ============ 内部 ============

        private void AddVoid(string name, object owner, Action cb)
        {
            if (string.IsNullOrEmpty(name) || cb == null) return;

            var entry = GetOrCreate(name);
            entry.VoidUsed = true;
            // 同一 owner 对同一回调重复订阅去重，避免 Unsubscribe 时一次删掉多份
            // 转成 Delegate 比较，走委托的值比较（目标对象 + 方法），而非引用比较
            if (entry.Void.Exists(s => s.Owner == owner && s.Callback == (Delegate)cb)) return;
            entry.Void.Add(new Subscription { Owner = owner, Callback = cb });
        }

        private void AddGeneric(string name, object owner, Delegate cb, Type argType)
        {
            if (string.IsNullOrEmpty(name) || cb == null) return;

            var entry = GetOrCreate(name);
            if (entry.GenericType != null && entry.GenericType != argType)
            {
                Log.Error($"事件 \"{name}\" 已登记为 {Describe(entry.GenericType)}，不能再用 {Describe(argType)} 订阅", LogChannel.Core);
                return;
            }

            entry.GenericType = argType;
            if (entry.Generic.Exists(s => s.Owner == owner && s.Callback == cb)) return;
            entry.Generic.Add(new Subscription { Owner = owner, Callback = cb });
        }

        private void RemoveVoid(string name, object owner, Action cb)
        {
            if (!_subs.TryGetValue(name, out var entry)) return;
            // 精确匹配 owner：传 null 只退订全局订阅，避免误删其他对象的相同回调
            entry.Void.RemoveAll(s => s.Callback == (Delegate)cb && s.Owner == owner);
        }

        private void RemoveGeneric(string name, object owner, Delegate cb)
        {
            if (!_subs.TryGetValue(name, out var entry)) return;
            entry.Generic.RemoveAll(s => s.Callback == cb && s.Owner == owner);
        }

        private EventEntry GetOrCreate(string name)
        {
            if (!_subs.TryGetValue(name, out var entry))
                _subs[name] = entry = new EventEntry();
            return entry;
        }

        private static string Describe(Type t) => t == typeof(void) ? "无参" : t.Name;

        /// <summary>释放时清空订阅表，避免残留回调在模块释放后被派发。</summary>
        protected override void OnRelease()
        {
            _subs.Clear();
        }
    }
}
