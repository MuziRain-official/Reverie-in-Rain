using System;
using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 计时器：由 GameEntry 每帧驱动，提供一次延时与循环回调。
    ///
    ///   Timer.Instance.Delay(1f, () => Debug.Log("1 秒后"));   // 延时一次
    ///   var t = Timer.Instance.Repeat(0.5f, OnTick);           // 每 0.5 秒一次
    ///   t.Cancel();                                            // 取消
    /// </summary>
    public class Timer : Singleton<Timer>, IUpdateModule
    {
        /// <summary>计时器句柄，调用 Cancel 后不再触发。</summary>
        public class Handle
        {
            internal bool Canceled;
            public void Cancel() => Canceled = true;
        }

        private class Item : Handle
        {
            public float Remaining;
            public float Interval;   // > 0 表示循环，0 表示一次性
            public Action Callback;
        }

        private readonly List<Item> _items = new List<Item>();

        /// <summary>延时一次：seconds 秒后触发一次。</summary>
        public Handle Delay(float seconds, Action callback) => Add(seconds, 0f, callback);

        /// <summary>每隔 interval 秒触发一次，直到 Cancel。</summary>
        public Handle Repeat(float interval, Action callback) => Add(interval, interval, callback);

        private Handle Add(float delay, float interval, Action callback)
        {
            var item = new Item { Remaining = delay, Interval = interval, Callback = callback };
            _items.Add(item);
            return item;
        }

        public void Tick(float deltaTime)
        {
            // 用快照遍历：回调里新增或取消计时器不会打乱本次循环
            var snapshot = _items.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                var item = snapshot[i];
                if (item.Canceled) continue;

                item.Remaining -= deltaTime;
                if (item.Remaining > 0f) continue;

                if (item.Interval > 0f) item.Remaining = item.Interval;   // 循环：重新计一整段
                else item.Canceled = true;                                // 一次性：标记后清理

                try { item.Callback?.Invoke(); }
                catch (Exception ex) { Log.Error($"计时器回调抛出异常：{ex}", LogChannel.Core); }
            }

            _items.RemoveAll(item => item.Canceled);
        }
    }
}
