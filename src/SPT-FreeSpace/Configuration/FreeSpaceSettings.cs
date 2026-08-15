using BepInEx.Configuration;

namespace SPTFreeSpace.Configuration;

internal sealed class FreeSpaceSettings
{
    internal const float DefaultRefreshInterval = 0.25f;
    internal const float MinimumRefreshInterval = 0.10f;
    internal const float MaximumRefreshInterval = 2.00f;

    private FreeSpaceSettings(
        ConfigEntry<bool> enabled,
        ConfigEntry<CapacityDisplayMode> displayMode,
        ConfigEntry<float> refreshInterval,
        ConfigEntry<bool> debugLogging)
    {
        Enabled = enabled;
        DisplayMode = displayMode;
        RefreshInterval = refreshInterval;
        DebugLogging = debugLogging;
    }

    internal ConfigEntry<bool> Enabled { get; }

    internal ConfigEntry<CapacityDisplayMode> DisplayMode { get; }

    internal ConfigEntry<float> RefreshInterval { get; }

    internal ConfigEntry<bool> DebugLogging { get; }

    internal static FreeSpaceSettings Bind(ConfigFile config)
    {
        return new FreeSpaceSettings(
            config.Bind(
                "General",
                "Enabled",
                true,
                "Show recursive free-space overlays on player-owned grid containers."),
            config.Bind(
                "General",
                "Display mode",
                CapacityDisplayMode.UsedTotal,
                "Show used or available recursive capacity before the total."),
            config.Bind(
                "General",
                "Refresh interval",
                DefaultRefreshInterval,
                new ConfigDescription(
                    "Seconds between overlay refreshes.",
                    new AcceptableValueRange<float>(
                        MinimumRefreshInterval,
                        MaximumRefreshInterval))),
            config.Bind(
                "Diagnostics",
                "Debug logging",
                false,
                "Log changed item results and periodic refresh timing diagnostics."));
    }

    internal static float ClampRefreshInterval(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return DefaultRefreshInterval;
        }

        return System.Math.Max(
            MinimumRefreshInterval,
            System.Math.Min(MaximumRefreshInterval, value));
    }
}
