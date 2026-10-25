/// <summary>
/// 日志级别，从低到高。SetLevel 后低于该级别的日志不输出。
/// </summary>

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warn = 2,
    Error = 3,
    None = 4,
}

/// <summary>
/// 频道常量。给日志挂频道，便于按模块过滤刷屏。
/// </summary>

public static class LogChannel
{
    public const string Default = "Default";
    public const string Core    = "Core";
    public const string Player  = "Player";
    public const string Combat  = "Combat";
    public const string UI      = "UI";
    public const string Audio   = "Audio";
    public const string Save    = "Save";
    public const string Resource = "Resource";
    public const string Room    = "Room";
    public const string Config  = "Config";
    // 业务频道以后按需追加
}