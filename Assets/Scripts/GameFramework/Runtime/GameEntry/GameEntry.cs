using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 框架全局入口：按 README 第 5 节启动顺序初始化各模块，关闭时逆序释放。
/// 通过 RuntimeInitializeOnLoadMethod 在场景加载前自动创建，业务无需手动放置。
///
/// 新增模块：在 InitModules 里按依赖顺序 Add&lt;T&gt;() 一行即可，
/// 该模块的 Singleton.Create() 会被调用并进入统一生命周期。
/// </summary>
public class GameEntry : MonoSingleton<GameEntry>
{
    private readonly List<IModule> _modules = new List<IModule>();

    /// <summary>场景加载前自动引导，早于任何业务代码触发模块初始化。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => _ = Instance;

    protected override void OnInit() => InitModules();

    protected override void OnRelease() => ReleaseModules();

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
    }

    /// <summary>注册一个 Core 模块（Singleton&lt;T&gt; 派生），按注册顺序初始化。</summary>
    private void Add<T>() where T : Singleton<T>, new()
        => Register(Singleton<T>.Create());

    private void Register(IModule module)
    {
        _modules.Add(module);
        module.Init();
    }
}
