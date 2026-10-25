/// <summary>
/// 数学工具：纯 C#，不依赖 UnityEngine。
/// </summary>
public static class MathUtil
{
    /// <summary>把 value 限制在 [min, max] 区间。</summary>
    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    /// <summary>把 value 限制在 [min, max] 区间。</summary>
    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    /// <summary>判断 value 是否在 [min, max] 区间内。</summary>
    public static bool InRange(float value, float min, float max) => value >= min && value <= max;

    /// <summary>线性插值（t=0 返回 a，t=1 返回 b）。</summary>
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>把 t 限制到 [0,1] 后线性插值。</summary>
    public static float LerpClamped(float a, float b, float t) => Lerp(a, b, Clamp(t, 0f, 1f));

    /// <summary>角度归一化到 [-180, 180)。</summary>
    public static float NormalizeAngle(float degrees)
    {
        degrees %= 360f;
        if (degrees > 180f) degrees -= 360f;
        if (degrees < -180f) degrees += 360f;
        return degrees;
    }
}
