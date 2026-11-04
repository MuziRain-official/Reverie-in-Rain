using System;
using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 全局数据：一张「字符串 key → 值」的表，存放跨模块共享的状态。
    /// 值变化时通过事件中心通知（事件名加 "ctx." 前缀）。
    ///
    ///   GameContext.Instance.Set("player.hp", 100);
    ///   int hp = GameContext.Instance.Get("player.hp", 0);
    ///   GameContext.Instance.Subscribe&lt;int&gt;(this, "player.hp", OnHpChanged);
    ///   EventCenter.Instance.UnsubscribeAll(this);   // 对象销毁时统一退订
    ///
    /// 约定：同一个 key 只用一种类型。key 由业务层用常量定义，框架不出现游戏概念。
    /// </summary>
    public class GameContext : Singleton<GameContext>
    {
        private const string Prefix = "ctx.";

        // key -> 值。值用 object 存，读的时候强制转换成目标类型。
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        /// <summary>读当前值；没有存过时返回 defaultValue。</summary>
        public T Get<T>(string key, T defaultValue = default)
            => _values.TryGetValue(key, out var value) ? (T)value : defaultValue;

        /// <summary>写值，并通知所有订阅这个 key 的地方。</summary>
        public void Set<T>(string key, T value)
        {
            _values[key] = value;
            EventCenter.Instance.Publish(Prefix + key, value);
        }

        public void Subscribe<T>(string key, Action<T> callback)
            => EventCenter.Instance.Subscribe(Prefix + key, callback);

        public void Subscribe<T>(string key, object owner, Action<T> callback)
            => EventCenter.Instance.Subscribe(Prefix + key, owner, callback);

        public void Unsubscribe<T>(string key, Action<T> callback)
            => EventCenter.Instance.Unsubscribe(Prefix + key, callback);

        protected override void OnRelease() => _values.Clear();
    }
}
