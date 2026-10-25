using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 日志模块：分级 + 频道过滤 + 控制台输出。
/// 用法：Log.Info("xxx")、Log.Debug("xxx", LogChannel.Combat)、Log.SetLevel(LogLevel.Warn)。
/// 注意：这是 Singleton 的第一个使用者，静态方法内部都走 Instance。
/// </summary>
public class Log : Singleton<Log>
{
    private LogLevel _minLevel = LogLevel.Debug;          // 低于此级别不输出
    private readonly HashSet<string> _mutedChannels = new HashSet<string>();

    // ---- 级别开关 ----
    public static void SetLevel(LogLevel level) => Instance._minLevel = level;
    public static LogLevel GetLevel() => Instance._minLevel;

    // ---- 频道开关 ----
    public static void MuteChannel(string channel)   => Instance._mutedChannels.Add(channel);
    public static void UnmuteChannel(string channel) => Instance._mutedChannels.Remove(channel);
    public static void MuteAllChannels()             => Instance._mutedChannels.Clear();

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

        // 2. 频道过滤：频道被静音则丢弃
        if (!string.IsNullOrEmpty(channel) && _mutedChannels.Contains(channel))
            return;

        // 3. 组装输出文本
        string text = string.IsNullOrEmpty(channel)
            ? message
            : $"[{channel}] {message}";

        // 4. 输出到控制台（Warn/Error 用对应方法，Console 里才有颜色和堆栈）
        switch (level)
        {
            case LogLevel.Warn:
                UnityEngine.Debug.LogWarning(text);
                break;
            case LogLevel.Error:
                UnityEngine.Debug.LogError(text);
                break;
            default:
                UnityEngine.Debug.Log(text);
                break;
        }
    }
}