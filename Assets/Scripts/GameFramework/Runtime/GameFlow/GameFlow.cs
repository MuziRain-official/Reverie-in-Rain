using System;

namespace Reverie.Framework
{
    /// <summary>
    /// 游戏流程：应用级状态的顶层调度，管「游戏在哪一步」。
    /// 房间管地图单元，流程管应用状态，两者互补。
    ///
    ///   Boot → Title → Loading → Playing ⇄ Paused
    ///                              │
    ///                              └──► Title（回主菜单）
    ///
    ///   GameFlow.Instance.StartGame();   // 标题里「开始游戏」调这句
    ///   GameFlow.Instance.Pause();       // 暂停
    ///   GameFlow.Instance.Resume();      // 恢复
    ///   GameFlow.Instance.BackToTitle(); // 回标题
    ///
    /// 五个状态各是一个类（见文件末尾），行为写在各自的 OnEnter/OnUpdate/OnExit 里；
    /// 业务层只调用上面这几个方法，不直接碰内部状态机。
    /// </summary>
    public class GameFlow : Singleton<GameFlow>, IUpdateModule
    {
        private Flow _fsm;

        /// <summary>当前流程状态。</summary>
        public GameFlowState Current => _fsm != null ? _fsm.Current : GameFlowState.Boot;

        /// <summary>暂停事件名，业务层可订阅它来播暂停动画。</summary>
        public const string EventPause = "flow.pause";

        /// <summary>恢复事件名。</summary>
        public const string EventResume = "flow.resume";

        /// <summary>流程切换事件名，参数是 GameFlowState。</summary>
        public const string EventStateChanged = "flow.stateChanged";

        protected override void OnInit()
        {
            _fsm = new Flow(this);
            _fsm.Start(GameFlowState.Boot);
        }

        /// <summary>标题页「开始游戏」：进载入，载入完自动进游玩。</summary>
        public void StartGame()
        {
            if (Is(GameFlowState.Loading) || Is(GameFlowState.Playing))
            {
                Log.Warn("已经在游戏里了，忽略 StartGame", LogChannel.Core);
                return;
            }
            _fsm.ChangeState(GameFlowState.Loading);
        }

        /// <summary>暂停。只在游玩中有效。</summary>
        public void Pause()
        {
            if (!Is(GameFlowState.Playing)) return;
            _fsm.ChangeState(GameFlowState.Paused);
        }

        /// <summary>恢复。只在暂停中有效。</summary>
        public void Resume()
        {
            if (!Is(GameFlowState.Paused)) return;
            _fsm.ChangeState(GameFlowState.Playing);
        }

        /// <summary>回标题。游玩中、暂停中、载入中都可用。</summary>
        public void BackToTitle()
        {
            if (Is(GameFlowState.Title) || Is(GameFlowState.Boot)) return;
            _fsm.ChangeState(GameFlowState.Title);
        }

        /// <summary>是否处在某个状态。</summary>
        public bool Is(GameFlowState state) => _fsm != null && _fsm.Is(state);

        public void Tick(float deltaTime)
        {
            if (_fsm != null) _fsm.Update();
        }

        protected override void OnRelease()
        {
            _fsm = null;
        }

        // ---------------- 内部状态机 ----------------

        /// <summary>把五个状态的注册集中在一处，业务流程里不用写 switch。</summary>
        private class Flow : FsmBase<GameFlowState>
        {
            public Flow(GameFlow owner)
            {
                Register(GameFlowState.Boot,    new BootState());
                Register(GameFlowState.Title,   new TitleState(owner));
                Register(GameFlowState.Loading, new LoadingState(owner));
                Register(GameFlowState.Playing, new PlayingState());
                Register(GameFlowState.Paused,  new PausedState());
            }
        }

        // ---- 五个状态类 ----

        /// <summary>启动：打完日志直接进游玩（跳过标题页，方便直接进游戏调试）。</summary>
        private class BootState : FsmState<GameFlowState>
        {
            public override void OnEnter()
            {
                Log.Info("流程：Boot", LogChannel.Core);
                Fsm.ChangeState(GameFlowState.Title);   // 骨架版：先过一遍标题
                Fsm.ChangeState(GameFlowState.Playing); // 临时：跳过标题直接进游玩
            }
        }

        /// <summary>标题页：锁输入，按跳跃键开始。真做的时候换成 UI 按钮调 StartGame()。</summary>
        private class TitleState : FsmState<GameFlowState>
        {
            private readonly GameFlow _owner;

            public TitleState(GameFlow owner) { _owner = owner; }

            public override void OnEnter()
            {
                Log.Info("流程：Title", LogChannel.Core);
                InputReader.Instance.Lock();        // 标题页不接受游戏操作
            }

            public override void OnUpdate()
            {
                if (InputReader.Instance.JumpPressed) _owner.StartGame();
            }

            public override void OnExit()
            {
                InputReader.Instance.Unlock();
            }
        }

        /// <summary>载入：重新加载当前场景，加载完自动进游玩。</summary>
        private class LoadingState : FsmState<GameFlowState>
        {
            private readonly GameFlow _owner;

            public LoadingState(GameFlow owner) { _owner = owner; }

            public override void OnEnter()
            {
                Log.Info("流程：Loading", LogChannel.Core);
                InputReader.Instance.Lock();

                // 骨架版：先重新加载当前这张图，走完 Scene.LoadMap 的完整流程再进游玩。
                // 接真实关卡后换成 Scene.Instance.LoadMap("关卡名", onProgress, onDone)。
                Scene.Instance.LoadMap(
                    Scene.Instance.CurrentMap,
                    OnProgress,
                    () => Fsm.ChangeState(GameFlowState.Playing));   // 解锁交给 PlayingState.OnEnter
            }

            private void OnProgress(float progress)
            {
                // 骨架版只往外发事件，进度条 UI 由业务层订阅
                EventCenter.Instance.Publish(EventStateChanged, GameFlowState.Loading);
            }
        }

        /// <summary>游玩：解锁输入，广播恢复事件。</summary>
        private class PlayingState : FsmState<GameFlowState>
        {
            public override void OnEnter()
            {
                Log.Info("流程：Playing", LogChannel.Core);
                InputReader.Instance.Unlock();
                EventCenter.Instance.Publish(EventResume);
            }

            public override void OnUpdate()
            {
                // 骨架版：暂停键还没在 Input Actions 里配，等配好再加 InputReader.PausePressed
            }

            public override void OnExit()
            {
                InputReader.Instance.Lock();
            }
        }

        /// <summary>暂停：锁输入，广播暂停事件。</summary>
        private class PausedState : FsmState<GameFlowState>
        {
            public override void OnEnter()
            {
                Log.Info("流程：Paused", LogChannel.Core);
                InputReader.Instance.Lock();
                EventCenter.Instance.Publish(EventPause);
            }

            public override void OnUpdate()
            {
                // 骨架版：回游戏 / 回标题由 UI 按钮决定，这里先留空
            }

            public override void OnExit()
            {
                InputReader.Instance.Unlock();
            }
        }
    }
}
