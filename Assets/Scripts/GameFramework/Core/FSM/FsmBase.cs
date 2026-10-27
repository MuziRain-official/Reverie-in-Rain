using System;
using System.Collections.Generic;

/// <summary>
/// 状态机基类（继承式）：进程状态机、角色状态机、敌人状态机都继承它。
/// 子类职责：定义状态枚举 → 构造函数里 Register 各状态 → 写状态回调方法。
/// </summary>
public abstract class FsmBase<TState> where TState : struct
{
    /// <summary>一个状态的定义：三段生命周期回调。</summary>
    protected sealed class State
    {
        public Action OnEnter;
        public Action OnUpdate;
        public Action OnExit;
    }

    // 状态表：状态标识 -> 状态定义
    private readonly Dictionary<TState, State> _states = new Dictionary<TState, State>();

    // 当前 / 上一个状态
    public TState Current  { get; private set; }
    public TState Previous { get; private set; }
    public bool IsRunning  { get; private set; }

    // ============ 子类用：注册状态 ============

    /// <summary>注册一个状态。onEnter/onUpdate/onExit 都可为 null（不需要就省略）。</summary>
    protected void Register(TState id, Action onEnter = null, Action onUpdate = null, Action onExit = null)
    {
        if (_states.ContainsKey(id))
            throw new InvalidOperationException($"状态 {id} 重复注册");

        _states[id] = new State { OnEnter = onEnter, OnUpdate = onUpdate, OnExit = onExit };
    }

    // ============ 子类可选覆写：切换钩子 ============

    /// <summary>状态切换后回调（可用于发事件、刷新 HUD 等）。</summary>
    protected virtual void OnStateChanged(TState prev, TState next) { }

    // ============ 对外：启动 / 驱动 / 切换 / 查询 ============

    /// <summary>以某状态启动。只调 OnEnter，不调上一个状态的 OnExit。</summary>
    public void Start(TState id)
    {
        if (!_states.ContainsKey(id))
            throw new KeyNotFoundException($"未注册的状态 {id}");

        Current = id;
        Previous = id;
        IsRunning = true;

        _states[id].OnEnter?.Invoke();
    }

    /// <summary>驱动当前状态的 OnUpdate（每帧调用）。</summary>
    public void Update()
    {
        if (!IsRunning) return;
        _states[Current].OnUpdate?.Invoke();
    }

    /// <summary>切换状态：先退旧、再进新。</summary>
    public void ChangeState(TState id)
    {
        if (!IsRunning)
            throw new InvalidOperationException("状态机尚未启动，请先 Start");
        if (id.Equals(Current))
            return;                                    // 切到自己，忽略
        if (!_states.ContainsKey(id))
            throw new KeyNotFoundException($"未注册的状态 {id}");

        var prev = Current;

        _states[prev].OnExit?.Invoke();   // ① 退旧

        Previous = prev;                  // ② 记上一个
        Current = id;                     // ③ 换当前

        _states[id].OnEnter?.Invoke();    // ④ 进新
        OnStateChanged(prev, id);         // ⑤ 通知切换
    }

    /// <summary>当前是否处于某状态。</summary>
    public bool Is(TState id) => IsRunning && id.Equals(Current);
}