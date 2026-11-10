using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 日志模块：分级 + 频道过滤 + 输出端转发。
    /// 用法：Log.Info("xxx")、Log.Debug("xxx", LogChannel.Combat)、Log.SetLevel(LogLevel.Warn)。
    /// Core 不引用 UnityEngine：实际输出由入口通过 Log.SetSink(new UnityLogSink()) 注入。
    /// </summary>
    public class Log : Singleton<Log>
    {
        private static ILogSink _sink = NullLogSink.Instance;

        /// <summary>
        /// 复位静态输出端。由 GameEntry 在启动时调用：上一次会话注入的 UnityLogSink 已随会话失效。
        /// 实例字段（级别、静音、频道）由 Singleton&lt;Log&gt;.Reset() 跟着实例一起丢弃。
        /// </summary>
        public static void ResetSink() => _sink = NullLogSink.Instance;

        private LogLevel _minLevel = LogLevel.Debug;          // 低于此级别不输出
        private bool _muteAll;                                // 全局静音（Error 除外）
        private readonly HashSet<string> _mutedChannels = new HashSet<string>();

        /// <summary>注入日志输出端。传 null 时回退为空输出端。</summary>
        public static void SetSink(ILogSink sink) => _sink = sink ?? NullLogSink.Instance;

        // ---- 级别开关 ----
        public static void SetLevel(LogLevel level) => Instance._minLevel = level;
        public static LogLevel GetLevel() => Instance._minLevel;

        // ---- 频道开关 ----
        public static void MuteChannel(string channel)   => Instance._mutedChannels.Add(channel);
        public static void UnmuteChannel(string channel) => Instance._mutedChannels.Remove(channel);

        /// <summary>取消所有频道静音（恢复各频道默认输出）。注意不是「全静音」。</summary>
        public static void UnmuteAllChannels()           => Instance._mutedChannels.Clear();

        /// <summary>全局静音：屏蔽 Debug/Info/Warn，Error 始终输出，避免掩盖真实错误。</summary>
        public static void MuteAll()                     => Instance._muteAll = true;

        /// <summary>解除全局静音。</summary>
        public static void UnmuteAll()                   => Instance._muteAll = false;

        // ---- 静态快捷入口（业务只调这些）----
        public static void Debug(string msg)                     => Instance.Write(LogLevel.Debug, msg, null);
        public static void Debug(string msg, string channel)     => Instance.Write(LogLevel.Debug, msg, channel);
        public static void Info(string msg)                      => Instance.Write(LogLevel.Info,  msg, null);
        public static void Info(string msg, string channel)      => Instance.Write(LogLevel.Info,  msg, channel);
        public static void Warn(string msg)                      => Instance.Write(LogLevel.Warn,  msg, null);
        public static void Warn(string msg, string channel)      => Instance.Write(LogLevel.Warn,  msg, channel);
        public static void Error(string msg)                     => Instance.Write(LogLevel.Error, msg, null);
        public static void Error(string msg, string channel)     => Instance.Write(LogLevel.Error, msg, channel);

        // ---- 核心：写一条日志 ----
        private void Write(LogLevel level, string message, string channel)
        {
            // 1. 级别过滤：低于最小级别直接丢弃
            if (level < _minLevel)
                return;

            // 2. 全局静音：Error 始终输出
            if (_muteAll && level < LogLevel.Error)
                return;

            // 3. 频道过滤：频道被静音则丢弃
            if (!string.IsNullOrEmpty(channel) && _mutedChannels.Contains(channel))
                return;

            // 4. 组装输出文本并交给注入的输出端
            string text = string.IsNullOrEmpty(channel)
                ? message
                : $"[{channel}] {message}";

            _sink.Write(level, text);
        }
    }
}
