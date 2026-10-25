using UnityEngine;
/// <summary>
/// /// MonoBehaviour 单例基类（访问时自动创建模式）。
/// 用法：public class GameEntry : MonoSingleton<GameEntry> { }
/// 规则：子类只覆写 OnInit / OnRelease，不要自己写 Awake / OnDestroy。
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;
    private bool _isActiveInstance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
                if (_instance == null)
                {
                    var obj = new GameObject(typeof(T).Name);
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        _isActiveInstance = true;

        DontDestroyOnLoad(gameObject);
        OnInit();
    }

    protected virtual void OnDestroy()
    {
        if (!_isActiveInstance)
            return;

        if (_instance == this)
            _instance = null;

        OnRelease();
    }

    protected virtual void OnInit() { }
    protected virtual void OnRelease() { }
}