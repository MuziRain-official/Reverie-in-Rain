namespace Reverie.Framework
{
    /// <summary>
    /// 日志输出端（纯 C#，不依赖 UnityEngine）。
    /// Core 只负责分级/过滤/格式化，具体输出由 Runtime 注入（见 UnityLogSink）。
    /// </summary>
    public interface ILogSink
    {
        void Write(LogLevel level, string message);
    }

    /// <summary>默认空输出端：在入口注入真实输出端之前静默丢弃日志。</summary>
    public sealed class NullLogSink : ILogSink
    {
        public static readonly NullLogSink Instance = new NullLogSink();

        public void Write(LogLevel level, string message) { }
    }
}
