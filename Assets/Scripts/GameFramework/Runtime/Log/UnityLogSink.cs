using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// UnityEngine 日志输出端：把 Core 的 LogLevel 映射到 UnityEngine.Debug，
    /// 使 Warn/Error 在 Console 里有颜色与堆栈。由 GameEntry 在初始化时注入。
    /// </summary>
    public sealed class UnityLogSink : ILogSink
    {
        public static readonly UnityLogSink Instance = new UnityLogSink();

        public void Write(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Warn:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Debug.LogError(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }
    }
}
