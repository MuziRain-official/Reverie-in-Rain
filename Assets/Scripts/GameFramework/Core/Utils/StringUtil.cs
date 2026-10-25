using System.Text;

/// <summary>
/// 字符串工具：纯 C#，不依赖 UnityEngine。
/// </summary>
public static class StringUtil
{
    /// <summary>判断字符串是否为空或空白。</summary>
    public static bool IsNullOrEmpty(string s) => string.IsNullOrEmpty(s);

    /// <summary>判断字符串是否为 null 或仅空白字符。</summary>
    public static bool IsNullOrWhiteSpace(string s) => string.IsNullOrWhiteSpace(s);

    /// <summary>首字母大写。</summary>
    public static string FirstUpper(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }

    /// <summary>首字母小写。</summary>
    public static string FirstLower(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToLower(s[0]) + s.Substring(1);
    }

    /// <summary>把下划线/连字符命名转成驼峰（如 "player_hp" -> "playerHp"）。</summary>
    public static string ToCamelCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        var sb = new StringBuilder(s.Length);
        bool upperNext = false;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '_' || c == '-')
            {
                upperNext = true;
                continue;
            }
            sb.Append(upperNext ? char.ToUpper(c) : c);
            upperNext = false;
        }
        return sb.ToString();
    }
}
