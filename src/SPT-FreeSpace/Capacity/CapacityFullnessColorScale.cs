using System;

namespace SPTFreeSpace.Capacity;

internal readonly struct CapacityColor
{
    internal CapacityColor(float red, float green, float blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }

    internal float Red { get; }

    internal float Green { get; }

    internal float Blue { get; }
}

internal static class CapacityFullnessColorScale
{
    internal static CapacityColor GetColor(CapacityResult result)
    {
        double fullness = GetFullness(result);
        if (fullness <= 0.5d)
        {
            return new CapacityColor((float)(fullness * 2d), 1f, 0f);
        }

        return new CapacityColor(
            1f,
            (float)(1d - ((fullness - 0.5d) * 2d)),
            0f);
    }

    internal static double GetFullness(CapacityResult result)
    {
        if (result.Total <= 0)
        {
            return 0d;
        }

        return Math.Max(0d, Math.Min(1d, (double)result.Used / result.Total));
    }
}
