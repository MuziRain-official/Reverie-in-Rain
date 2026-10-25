using System;

/// <summary>
/// 普通单例基类（进程级，纯 C#，不依赖 Unity 场景）。
/// 用法：public class EventCenter : Singleton<EventCenter> { }
/// 规则：子类只覆写 OnInit / OnRelease；构造必须是无参 public。
/// </summary>
public abstract class Singleton<T> where T : class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                        (_instance as Singleton<T>)?.OnInit(); 
                    }
                }
            }
            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;
    
    public static void Release()
    {
        lock (_lock)
        {
            if (_instance == null)
                return;

            (_instance as Singleton<T>)?.OnRelease();
            _instance = null;
        }
    }

    protected virtual void OnInit() { }
    protected virtual void OnRelease() { }
}