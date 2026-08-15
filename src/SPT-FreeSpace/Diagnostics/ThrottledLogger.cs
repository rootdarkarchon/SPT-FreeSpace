using System;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx.Logging;

namespace SPTFreeSpace.Diagnostics;

internal sealed class ThrottledLogger
{
    private readonly ManualLogSource _log;
    private readonly Dictionary<string, double> _nextAllowed =
        new Dictionary<string, double>(StringComparer.Ordinal);

    internal ThrottledLogger(ManualLogSource log)
    {
        _log = log;
    }

    internal void Warning(string key, string message, double intervalSeconds = 30d)
    {
        if (CanLog(key, intervalSeconds))
        {
            _log.LogWarning(message);
        }
    }

    internal void Error(string key, string message, double intervalSeconds = 30d)
    {
        if (CanLog(key, intervalSeconds))
        {
            _log.LogError(message);
        }
    }

    private bool CanLog(string key, double intervalSeconds)
    {
        double now = (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        if (_nextAllowed.TryGetValue(key, out double next) && now < next)
        {
            return false;
        }

        _nextAllowed[key] = now + Math.Max(0d, intervalSeconds);
        return true;
    }
}
