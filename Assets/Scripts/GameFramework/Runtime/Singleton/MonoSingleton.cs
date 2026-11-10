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
                // Unity 重载了 ==，被销毁的对象会表现为 null，这里同时挡住「悬空引用」
                if (_instance == null)
                {
                    // 先找场景里已有的，再新建：避免场景里已摆好一个、这里又建出第二个
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

        /// <summary>
        /// 只丢掉静态引用，不销毁对象。由 GameEntry 在启动时调用，清掉上一次会话的残留。
        /// 旧对象属于上一个场景，Unity 已随场景销毁，这里不 Destroy 它。
        /// </summary>
        public static void Reset() => _instance = null;

        protected virtual void Awake()
        {
            // 场景里已经摆了一个，而静态引用还指向「别的 / 已被销毁的」实例时，
            // 以场景里这个为准接管，而不是把它销毁——否则会出现一个场景里有实例、
            // Instance 却指向 null 或另一个对象的情况（快速进入播放模式时最容易踩到）。
            if (_instance == null || _instance == this)
            {
                _instance = this as T;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

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
