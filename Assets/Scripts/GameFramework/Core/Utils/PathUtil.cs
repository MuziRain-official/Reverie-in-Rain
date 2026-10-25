using System.IO;

/// <summary>
/// 路径工具：纯 C#，不依赖 UnityEngine。
/// </summary>
public static class PathUtil
{
    /// <summary>用系统分隔符拼接两个路径段。</summary>
    public static string Combine(string a, string b) => Path.Combine(a, b);

    /// <summary>用系统分隔符拼接三个路径段。</summary>
    public static string Combine(string a, string b, string c) => Path.Combine(a, b, c);

    /// <summary>取文件名（不含目录）。</summary>
    public static string GetFileName(string path) => Path.GetFileName(path);

    /// <summary>取文件名（不含目录和扩展名）。</summary>
    public static string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

    /// <summary>取扩展名（含点，如 ".png"）。</summary>
    public static string GetExtension(string path) => Path.GetExtension(path);

    /// <summary>取目录部分。</summary>
    public static string GetDirectoryName(string path) => Path.GetDirectoryName(path);

    /// <summary>统一路径分隔符为正斜杠（Unity 资源路径习惯用 /）。</summary>
    public static string ToUnityPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return path.Replace('\\', '/');
    }
}
