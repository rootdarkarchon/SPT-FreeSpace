using System.Globalization;
using SPTFreeSpace.Configuration;

namespace SPTFreeSpace.Capacity;

internal static class CapacityDisplayFormatter
{
    internal static string Format(CapacityResult result, CapacityDisplayMode mode)
    {
        int value = mode == CapacityDisplayMode.AvailableTotal
            ? result.Available
            : result.Used;

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1}",
            value,
            result.Total);
    }
}
