using System;
using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 状态机基类（继承式）：进程状态机、角色状态机、敌人状态机都继承它。
    ///
    /// 子类做两件事：
    ///   ① 定一个状态枚举（struct 约束只用了 enum）；
    ///   ② 在构造函数里 Register 各个状态对象。
    ///
    ///   private class Flow : FsmBase&lt;GameFlowState&gt;
    ///   {
    ///       public Flow(GameFlow owner)
    ///       {
    ///           Register(GameFlowState.Boot,  new BootState());
    ///           Register(GameFlowState.Playing, new PlayingState());
    ///       }
    ///   }
    ///
    /// 内部存的是状态对象（FsmState&lt;TState&gt;），不是回调。
    /// 状态对象在构造函数里一次性注册满，之后只切换、不增删。
    /// </summary>
    public abstract class FsmBase<TState> where TState : struct
    {
        // 状态表：状态标识 -> 状态对象。构造期注册完就不再变。
        private readonly Dictionary<TState, FsmState<TState>> _states
            = new Dictionary<TState, FsmState<TState>>();

        // 当前状态。注意 default(TState) 往往是一个合法的状态（enum 的第一个成员），
        // 所以「当前是谁」不能只看这个字段，还要看 _entered。
        private TState _current;

        // 是否真的进入过某个状态：Start 之后、第一次 OnEnter 之前为 false。
        // 没有它，Start(0 号值) 会被当成「切到自己」而跳过 OnEnter，
        // Start(非 0 号值) 又会误调 0 号状态的 OnExit。 
        private bool _entered;

        /// <summary>当前状态。未启动时是 default，所以判断“在不在某状态”请用 Is()。</summary>
        public TState Current => _current;

        /// <summary>已启动时为 true，Stop 后为 false。</summary>
        public bool IsRunning { get; private set; }

        // ============ 子类用：注册状态 ============

        /// <summary>注册一个状态对象。同一个状态重复注册会抛异常。</summary>
        protected void Register(TState id, FsmState<TState> state)
        {
            if (_states.ContainsKey(id))
                throw new InvalidOperationException($"状态 {id} 重复注册");

            state.Bind(this, id);
            _states[id] = state;
        }

        // ============ 子类可选覆写：切换钩子 ============

        /// <summary>
        /// 状态切换后回调：改动画、刷 HUD、发事件都放这里。
        /// 首次 Start 进入时也会触发一次，所以第一帧的表现不用另写。
        /// </summary>
        protected virtual void OnInitState(TState id) { }

        // ============ 对外：启动 / 驱动 / 切换 / 查询 ============

        /// <summary>以某状态启动。先置 IsRunning 再切，这样 OnEnter 里可以继续 ChangeState。</summary>
        public void Start(TState id)
        {
            if (IsRunning)
                throw new InvalidOperationException("状态机已启动，请勿重复 Start");

            IsRunning = true;
            _entered = false;   // 还没进过任何状态，第一次切换不该退旧状态
            ChangeState(id);
        }

        /// <summary>停止驱动。停止后 Update 不再调用当前状态，切换会抛异常。</summary>
        public void Stop()
        {
            IsRunning = false;
            _entered = false;
        }

        /// <summary>驱动当前状态的 OnUpdate（每帧调用）。</summary>
        public void Update()
        {
            if (!IsRunning || !_entered) return;
            _states[_current].OnUpdate();
        }

        /// <summary>切换状态：先退旧、再进新。切到自己直接忽略。</summary>
        public void ChangeState(TState id)
        {
            if (!IsRunning)
                throw new InvalidOperationException("状态机尚未启动，请先 Start");

            var ec = EqualityComparer<TState>.Default;

            // 没进过任何状态时，_current 就是 default，不能拿它和 id 比
            if (_entered && ec.Equals(id, _current))
                return;

            if (!_states.TryGetValue(id, out FsmState<TState> next))
                throw new KeyNotFoundException($"未注册的状态 {id}");

            // 只有真的进去过，才需要退
            if (_entered && _states.TryGetValue(_current, out FsmState<TState> prev))
            {
                prev.OnExit();
                _entered = false;
            }

            _current = id;

            next.OnEnter();
            _entered = true;
            OnInitState(id);
        }

        /// <summary>当前是否处于某状态。未启动时恒为 false。</summary>
        public bool Is(TState id)
        {
            if (!IsRunning || !_entered) return false;
            return EqualityComparer<TState>.Default.Equals(_current, id);
        }
    }
}
