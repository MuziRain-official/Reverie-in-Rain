using System.Collections.Generic;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 框架全局入口：启动时按顺序初始化各模块，关闭时逆序释放。
    /// 通过 RuntimeInitializeOnLoadMethod 在场景加载前自动创建，业务无需手动放置。
    ///
    /// 新增模块：在 OnInit 里加一行 _modules.Add(Xxx.Create())，按依赖顺序排列。
    /// 实现 IUpdateModule 的模块会由本入口的 Update 每帧驱动。
    /// </summary>
    public class GameEntry : MonoSingleton<GameEntry>
    {
        private readonly List<IModule> _modules = new List<IModule>();

        /// <summary>场景加载前自动引导，早于任何业务代码触发模块初始化。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => _ = Instance;

        protected override void OnInit()
        {
            Log.SetSink(UnityLogSink.Instance);   // 日志最先就绪，后续模块的报错才可见

            // 启动顺序：日志 → 事件中心 → 计时器 → 全局数据 → 配置 → 输入 → ……（按 README 第 5 节逐层补全）
            _modules.Add(Log.Create());
            _modules.Add(EventCenter.Create());
            _modules.Add(Timer.Create());
            _modules.Add(GameContext.Create());
            _modules.Add(Config.Create());
            _modules.Add(InputReader.Create());
        }

        protected override void OnRelease()
        {
            // 逆序释放：先停上层模块，再释放它依赖的模块
            for (int i = _modules.Count - 1; i >= 0; i--)
                _modules[i].Release();
            _modules.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _modules.Count; i++)
                if (_modules[i] is IUpdateModule module)
                    module.Tick(dt);
        }
    }
}
