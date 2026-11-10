namespace Reverie.Framework
{
    /// <summary>
    /// 普通单例基类（进程级，纯 C#，不依赖 Unity 场景），实现 IModule 统一生命周期。
    /// 用法：public class EventCenter : Singleton&lt;EventCenter&gt; { }
    /// 规则：子类只覆写 OnInit / OnRelease；构造必须是无参 public。
    ///
    /// 生命周期由 GameEntry 主导：
    ///   - Create() 由入口显式创建并初始化；
    ///   - Release() 由入口逆序释放；
    ///   - Instance 仅在入口之前被访问时兜底 new（不初始化，保持「由入口初始化」的契约）。
    /// 这样消除了「谁先访问谁就先各自初始化」导致的顺序不可控。
    ///
    /// 静态复位：静态字段不会随退出播放模式自动清空，上一次会话的实例会残留到下一次会话。
    /// 入口在创建模块之前调用 Reset() 丢掉旧引用即可；是否调用由 GameEntry 显式决定。
    /// </summary>
    public abstract class Singleton<T> : IModule where T : Singleton<T>, new()
    {
        private static T _instance;
        private static bool _warnedEarlyInit;
        private bool _inited;

        /// <summary>访问入口。尚未由 GameEntry 创建时兜底 new（不初始化）。</summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 兜底实例没有 Init，若业务依赖其 OnInit 结果会静默拿错——开发期暴露时序问题。
                    if (!_warnedEarlyInit)
                    {
                        _warnedEarlyInit = true;
                        Log.Warn($"模块 {typeof(T).Name} 在 GameEntry 初始化前被访问，返回的是未 Init 的实例", LogChannel.Core);
                    }
                    _instance = new T();
                }
                return _instance;
            }
        }

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
            _warnedEarlyInit = false;
        }

        /// <summary>
        /// 只丢掉静态引用，不调用 OnRelease。由 GameEntry 在启动时调用，清掉上一次会话的残留。
        /// 与 Destroy 的区别：旧实例可能已经失效（对象随场景销毁 / 会话结束），
        /// 此时再调 Release 只会抛异常，所以复位阶段只做「忘掉它」。
        /// </summary>
        public static void Reset()
        {
            _instance = null;
            _warnedEarlyInit = false;
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
}
