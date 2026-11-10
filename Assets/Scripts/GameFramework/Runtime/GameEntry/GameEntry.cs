using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 框架全局入口：启动时按顺序初始化各模块，关闭时逆序释放。
    /// 通过 RuntimeInitializeOnLoadMethod 在场景加载前自动创建，业务无需手动放置。
    ///
    /// 新增模块：在 OnInit 里加一行 Add(Xxx.Create())，按依赖顺序排列。
    /// 实现 IUpdateModule 的模块会由本入口的 Update 每帧驱动。
    /// </summary>
    public class GameEntry : MonoSingleton<GameEntry>
    {
        private readonly List<IModule> _modules = new List<IModule>();

        // 需要逐帧驱动的模块，注册时筛出来一次，避免每帧对全部模块做类型判断
        private readonly List<IUpdateModule> _updateModules = new List<IUpdateModule>();

        /// <summary>场景加载前自动引导，早于任何业务代码触发模块初始化。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            ResetSingletons();
            _ = Instance;
        }

        /// <summary>
        /// 复位所有单例的静态字段，必须早于任何模块的 Init。
        /// 退出播放模式时 Unity 会销毁对象，但不会清空纯 C# 类的静态字段：
        /// 上一次会话的实例会残留到下一次会话，导致 Create() 拿到一个已经失效的旧对象、Init 被跳过。
        ///
        /// 只丢掉引用、不调 Release：旧实例多半已随会话失效，再调 Release 只会抛异常。
        /// 顺序与 OnInit 的启动顺序一致，新增模块时这里记得同步加一行。
        /// </summary>
        private static void ResetSingletons()
        {
            Log.ResetSink();
            Log.Reset();
            EventCenter.Reset();
            Timer.Reset();
            GameContext.Reset();
            Config.Reset();
            InputReader.Reset();
            UIManager.Reset();
            Pool.Reset();
            Resource.Reset();
            Scene.Reset();
            Room.Reset();
            GameFlow.Reset();
            GameEntry.Reset();
        }

        protected override void OnInit()
        {
            Log.SetSink(UnityLogSink.Instance);   // 日志最先就绪，后续模块的报错才可见

            // 启动顺序：日志 → 事件中心 → 计时器 → 全局数据 → 配置 → 输入 → UI → 房间 → 流程 → ……
            // （按文档第 4 节逐层补全；流程放最后，因为它要调度前面的模块）
            Add(Log.Create());
            Add(EventCenter.Create());
            Add(Timer.Create());
            Add(GameContext.Create());
            Add(Config.Create());
            Add(InputReader.Create());
            Add(UIManager.Create());
            Add(Room.Create());
            Add(GameFlow.Create());
        }

        /// <summary>登记模块；实现 IUpdateModule 的同时进入逐帧驱动列表。</summary>
        private void Add(IModule module)
        {
            _modules.Add(module);
            if (module is IUpdateModule update) _updateModules.Add(update);
        }

        protected override void OnRelease()
        {
            // 逆序释放：先停上层模块，再释放它依赖的模块
            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                try { _modules[i].Release(); }
                catch (Exception ex) { Log.Error($"模块 {_modules[i].GetType().Name} 释放失败：{ex}", LogChannel.Core); }
            }
            _modules.Clear();
            _updateModules.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _updateModules.Count; i++)
            {
                // 单个模块抛异常只记录，不中断本帧其余模块（与事件中心的隔离策略一致）
                try { _updateModules[i].Tick(dt); }
                catch (Exception ex) { Log.Error($"模块 {_updateModules[i].GetType().Name} Update 失败：{ex}", LogChannel.Core); }
            }
        }
    }
}
