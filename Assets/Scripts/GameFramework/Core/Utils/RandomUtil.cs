using System;

/// <summary>
/// 随机数工具：封装 System.Random，支持设种子（相同种子产生相同序列，便于复现 bug）。
/// 纯 C#，不依赖 UnityEngine，可在 Core 中单测。
/// </summary>
public static class RandomUtil
{
    private static Random _random = new Random();

    /// <summary>设置随机种子（相同种子产生相同序列，便于复现问题）。</summary>
    public static void SetSeed(int seed) => _random = new Random(seed);

    /// <summary>[min, max) 区间的随机整数（含 min，不含 max）。</summary>
    public static int Range(int min, int max) => _random.Next(min, max);

    /// <summary>[0, max) 区间的随机整数。</summary>
    public static int Range(int max) => _random.Next(max);

    /// <summary>[0.0, 1.0) 区间的随机浮点数。</summary>
    public static float Value() => (float)_random.NextDouble();

    /// <summary>[min, max) 区间的随机浮点数。</summary>
    public static float Range(float min, float max) => min + (max - min) * Value();

    /// <summary>以概率 chance 返回 true（chance ∈ [0,1]）。</summary>
    public static bool Chance(float chance) => Value() < chance;

    /// <summary>从数组里随机取一个元素。</summary>
    public static T Pick<T>(T[] array)
    {
        if (array == null || array.Length == 0)
            return default;
        return array[_random.Next(array.Length)];
    }
}
