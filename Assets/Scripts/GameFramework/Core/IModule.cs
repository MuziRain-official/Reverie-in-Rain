namespace Reverie.Framework
{
    /// <summary>
    /// 框架模块的统一生命周期契约。
    /// 由 GameEntry 按注册顺序 Init、逆序 Release；模块不自举初始化。
    /// 任何需要被框架托管生命周期的模块（日志、事件、计时器……）都实现此接口。
    /// </summary>
    public interface IModule
    {
        /// <summary>初始化。由 GameEntry 在启动时按依赖顺序调用，实现须幂等。</summary>
        void Init();

        /// <summary>释放。由 GameEntry 在关闭时逆序调用，实现须幂等。</summary>
        void Release();
    }

    /// <summary>
    /// 需要逐帧驱动的模块契约。实现此接口的模块由 GameEntry.Update 每帧调用 Tick，
    /// 模块自身不再各自写 Update，保证驱动入口统一、顺序可控。
    /// </summary>
    public interface IUpdateModule
    {
        /// <summary>逐帧驱动。deltaTime 为上一帧到本帧的秒数，由入口统一提供。</summary>
        void Tick(float deltaTime);
    }
}
