using System;

/// <summary>
/// 普通单例基类（进程级，纯 C#，不依赖 Unity 场景），实现 IModule 统一生命周期。
/// 用法：public class EventCenter : Singleton<EventCenter> { }
/// 规则：子类只覆写 OnInit / OnRelease；构造必须是无参 public。
///
/// 生命周期由 GameEntry 主导：
///   - Create() 由入口显式创建并初始化；
///   - Release() 由入口逆序释放；
///   - Instance 仅在入口之前被访问时兜底 new（不初始化，保持「由入口初始化」的契约）。
/// 这样消除了「谁先访问谁就先各自初始化」导致的顺序不可控。
/// </summary>
public abstract class Singleton<T> : IModule where T : Singleton<T>, new()
{
    private static T _instance;
    private bool _inited;

    /// <summary>访问入口。尚未由 GameEntry 创建时兜底 new（不初始化）。</summary>
    public static T Instance => _instance ??= new T();

    public static bool HasInstance => _instance != null;

    /// <summary>由 GameEntry 显式创建并初始化。幂等。</summary>
    public static T Create()
    {
        var inst = _instance ??= new T();
        inst.Init();
        return inst;
    }

    /// <summary>彻底释放并清空静态引用（应用退出 / 测试复位用）。</summary>
    public static void Destroy()
    {
        _instance?.Release();
        _instance = null;
    }

    public void Init()
    {
        if (_inited) return;
        _inited = true;
        OnInit();
    }

    public void Release()
    {
        if (!_inited) return;
        _inited = false;
        OnRelease();
    }

    protected virtual void OnInit() { }
    protected virtual void OnRelease() { }
}
