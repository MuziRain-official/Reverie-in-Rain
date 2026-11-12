namespace Reverie.Framework
{
    /// <summary>
    /// 状态的共同父类。
    /// FsmBase 的存储单位：状态机里存的不是回调，而是这个类的实例。
    ///
    /// 两层关系（组合，不是继承）：
    ///   FsmBase&lt;TState&gt;  ──持有──&gt;  FsmState&lt;TState&gt;  ──继承──&gt;  XxxState
    ///
    /// 用法：子类覆写 OnEnter / OnUpdate / OnExit，自己的字段写在子类里。
    ///   private class IdleState : FsmState&lt;GameFlowState&gt;
    ///   {
    ///       private float _idleTimer;              // 状态自己记的东西
    ///       public override void OnEnter() { _idleTimer = 0f; }
    ///       public override void OnUpdate() { _idleTimer += 0.016f; }
    ///   }
    ///
    /// 生命周期：new 出来 → Register 时 Bind 拿到 Fsm 和 Id → Start/ChangeState 时才收三个回调。
    /// 所以构造函数里不能调 Fsm.ChangeState，那时 Fsm 还是 null。
    /// </summary>
    public abstract class FsmState<TState> where TState : struct
    {
        /// <summary>所属状态机，切换状态时用 Fsm.ChangeState(xxx)。</summary>
        protected FsmBase<TState> Fsm;

        /// <summary>自己在状态机里登记的标识。</summary>
        protected TState Id;

        /// <summary>注册时由 FsmBase 调用，外部绑不了。</summary>
        internal void Bind(FsmBase<TState> fsm, TState id)
        {
            Fsm = fsm;
            Id = id;
        }

        /// <summary>进入状态：初始化自己的字段、播动画、锁输入。</summary>
        public virtual void OnEnter() { }

        /// <summary>每帧调用：读判断条件、推进计时。</summary>
        public virtual void OnUpdate() { }

        /// <summary>离开状态：收尾、解锁输入。</summary>
        public virtual void OnExit() { }
    }
}
