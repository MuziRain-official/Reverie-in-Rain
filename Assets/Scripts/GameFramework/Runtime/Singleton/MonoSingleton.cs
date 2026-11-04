using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// MonoBehaviour 单例基类（访问时自动创建模式）。
    /// 用法：public class GameEntry : MonoSingleton&lt;GameEntry&gt; { }
    /// 规则：子类只覆写 OnInit / OnRelease，不要自己写 Awake / OnDestroy。
    ///
    /// 默认 autoInit / dontDestroyOnLoad 均为 true，适用于全局入口；
    /// 场景级服务（如房间内的临时管理器）继承时应把两者设为 false，由入口或场景自行管理。
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private bool _isActiveInstance;

        /// <summary>Awake 时是否自动调用 OnInit。入口创建的常驻服务为 true，场景自管的为 false。</summary>
        protected virtual bool AutoInit => true;

        /// <summary>Awake 时是否 DontDestroyOnLoad。常驻服务为 true，随场景销毁的为 false。</summary>
        protected virtual bool DontDestroyOnLoadEnabled => true;

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

            if (DontDestroyOnLoadEnabled)
                DontDestroyOnLoad(gameObject);

            if (AutoInit)
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
}
