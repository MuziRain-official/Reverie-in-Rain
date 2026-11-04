using System.Collections.Generic;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 框架全局入口：按 README 第 5 节启动顺序初始化各模块，关闭时逆序释放。
    /// 通过 RuntimeInitializeOnLoadMethod 在场景加载前自动创建，业务无需手动放置。
    ///
    /// 新增模块：在 InitModules 里按依赖顺序 Add&lt;T&gt;() 一行即可，
    /// 该模块的 Singleton.Create() 会被调用并进入统一生命周期。
    /// 需要逐帧驱动的模块额外实现 IUpdateModule，由本入口的 Update 统一转发。
    /// </summary>
    public class GameEntry : MonoSingleton<GameEntry>
    {
        private readonly List<IModule> _modules = new List<IModule>();
        private readonly List<IUpdateModule> _updateModules = new List<IUpdateModule>();

        /// <summary>场景加载前自动引导，早于任何业务代码触发模块初始化。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => _ = Instance;

        protected override void OnInit()
        {
            // 日志最先就绪：注入 UnityEngine 输出端，后续模块的报错才可见
            Log.SetSink(UnityLogSink.Instance);
            InitModules();
        }

        protected override void OnRelease() => ReleaseModules();

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _updateModules.Count; i++)
                _updateModules[i].Tick(dt);
        }

        private void InitModules()
        {
            if (_modules.Count > 0) return;   // 已初始化（Bootstrap 与场景内实例二选一）

            // 启动顺序：日志与工具 → 事件中心 → ……（按 README 第 5 节逐层补全）
            Add<Log>();
            Add<EventCenter>();
        }

        private void ReleaseModules()
        {
            for (int i = _modules.Count - 1; i >= 0; i--)
                _modules[i].Release();
            _modules.Clear();
            _updateModules.Clear();
        }

        /// <summary>注册一个 Core 模块（Singleton&lt;T&gt; 派生），按注册顺序初始化。</summary>
        private void Add<T>() where T : Singleton<T>, new()
        {
            var module = Singleton<T>.Create();   // Create 内部已完成 Init
            Register(module);
        }

        /// <summary>注册一个自定义生命周期的模块（如 MonoBehaviour 服务），并接入统一驱动。</summary>
        private void Register(IModule module)
        {
            if (module == null) return;

            _modules.Add(module);
            if (module is IUpdateModule updatable)
                _updateModules.Add(updatable);
        }
    }
}
