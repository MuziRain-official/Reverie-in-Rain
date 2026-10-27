using System;
using System.Collections.Generic;

/// <summary>
/// 事件中心：模块间解耦通信的总线（字符串 key + 单中心版）。
/// 支持：无参/带参事件、owner 自动退订、立即派发。
///
/// 用法：
///   var e = EventCenter.Instance;
///   e.Subscribe("Hurt", OnHurt);                  // 无参
///   e.Subscribe<int>("Hurt", dmg => ...);         // 带参
///   e.Subscribe(this, OnHurt);                    // 带 owner（销毁时统一退订）
///   e.Publish("Hurt");                            // 立即
///   e.Publish("Hurt", 10);                        // 立即带参
///   e.UnsubscribeAll(this);                       // 对象销毁时调用
///
/// 约定：一个事件名只对应一种参数类型（不要同名又发 int 又发 float）。
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

    // ============ 订阅 ============

    public void Subscribe(string name, Action action) => Add(name, null, action);
    public void Subscribe<T>(string name, Action<T> action) => Add(name, null, action);

    public void Subscribe(string name, object owner, Action action) => Add(name, owner, action);
    public void Subscribe<T>(string name, object owner, Action<T> action) => Add(name, owner, action);

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

    public void Publish(string name)
    {
        if (!_subs.TryGetValue(name, out var list)) return;
        for (int i = list.Count - 1; i >= 0; i--)
            (list[i].Callback as Action)?.Invoke();
    }

    public void Publish<T>(string name, T e)
    {
        if (!_subs.TryGetValue(name, out var list)) return;
        for (int i = list.Count - 1; i >= 0; i--)
            (list[i].Callback as Action<T>)?.Invoke(e);
    }

    // ============ 内部 ============

    private void Add(string name, object owner, Delegate cb)
    {
        if (cb == null) return;
        if (!_subs.TryGetValue(name, out var list))
            _subs[name] = list = new List<Subscription>();
        list.Add(new Subscription { Owner = owner, Callback = cb });
    }

    private void Remove(string name, object owner, Delegate cb)
    {
        if (!_subs.TryGetValue(name, out var list)) return;
        // owner == null 表示「退订任意 owner 的该回调」；owner 非 null 时精确匹配
        list.RemoveAll(s => s.Callback == cb && (owner == null || s.Owner == owner));
    }
}
