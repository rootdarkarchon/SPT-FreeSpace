using System;
using System.Collections.Generic;
using System.Diagnostics;
using EFT.InventoryLogic;
using SPTFreeSpace.Capacity;
using SPTFreeSpace.Configuration;
using UnityEngine;

namespace SPTFreeSpace.UI;

internal sealed class FreeSpaceRefreshService : MonoBehaviour
{
    private const double SlowRefreshMilliseconds = 10d;
    private const float DebugSummaryInterval = 5f;

    private readonly HashSet<FreeSpaceOverlay> _overlays = new HashSet<FreeSpaceOverlay>();
    private readonly List<FreeSpaceOverlay> _snapshot = new List<FreeSpaceOverlay>();
    private readonly ContainerCapacityCalculator<CompoundItem> _calculator =
        new ContainerCapacityCalculator<CompoundItem>(ItemGridAdapter.Instance);

    private FreeSpaceSettings _settings = null!;
    private float _nextRefresh;
    private float _nextDebugSummary;
    private bool _initialized;

    internal void Initialize(FreeSpaceSettings settings)
    {
        _settings = settings;
        _settings.Enabled.SettingChanged += OnSettingChanged;
        _settings.DisplayMode.SettingChanged += OnSettingChanged;
        _settings.CountNestedContainersAsUsed.SettingChanged += OnSettingChanged;
        _settings.FullnessColorScale.SettingChanged += OnSettingChanged;
        _settings.RefreshInterval.SettingChanged += OnSettingChanged;
        _settings.DebugLogging.SettingChanged += OnSettingChanged;
        _nextRefresh = 0f;
        _nextDebugSummary = 0f;
        _initialized = true;
    }

    internal void Register(FreeSpaceOverlay overlay)
    {
        if (!_initialized || overlay == null)
        {
            return;
        }

        if (_overlays.Add(overlay))
        {
            _nextRefresh = 0f;
        }
    }

    internal void Unregister(FreeSpaceOverlay overlay)
    {
        _overlays.Remove(overlay);
    }

    internal void Shutdown()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        _settings.Enabled.SettingChanged -= OnSettingChanged;
        _settings.DisplayMode.SettingChanged -= OnSettingChanged;
        _settings.CountNestedContainersAsUsed.SettingChanged -= OnSettingChanged;
        _settings.FullnessColorScale.SettingChanged -= OnSettingChanged;
        _settings.RefreshInterval.SettingChanged -= OnSettingChanged;
        _settings.DebugLogging.SettingChanged -= OnSettingChanged;

        SnapshotOverlays();
        foreach (FreeSpaceOverlay overlay in _snapshot)
        {
            if (overlay != null)
            {
                overlay.HideAndClear();
            }
        }

        _snapshot.Clear();
        _overlays.Clear();
    }

    private void Update()
    {
        if (!_initialized)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now < _nextRefresh)
        {
            return;
        }

        float interval = FreeSpaceSettings.ClampRefreshInterval(
            _settings.RefreshInterval.Value);
        _nextRefresh = now + interval;
        RefreshAll(now);
    }

    private void RefreshAll(float unscaledNow)
    {
        long started = Stopwatch.GetTimestamp();
        SnapshotOverlays();

        var context = new CapacityCalculationContext(
            _settings.CountNestedContainersAsUsed.Value,
            OnCapacityGuardFailure);
        int refreshed = 0;
        foreach (FreeSpaceOverlay overlay in _snapshot)
        {
            if (overlay == null)
            {
                _overlays.Remove(overlay!);
                continue;
            }

            if (!overlay.isActiveAndEnabled)
            {
                _overlays.Remove(overlay);
                continue;
            }

            overlay.Refresh(_calculator, context);
            refreshed++;
        }

        double elapsedMilliseconds =
            (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
        if (elapsedMilliseconds > SlowRefreshMilliseconds)
        {
            Plugin.ThrottledLog.Warning(
                "slow-refresh",
                $"Free-space refresh took {elapsedMilliseconds:F2} ms for " +
                $"{refreshed} overlays ({context.MemoCount} unique containers).",
                30d);
        }

        if (_settings.DebugLogging.Value && unscaledNow >= _nextDebugSummary)
        {
            _nextDebugSummary = unscaledNow + DebugSummaryInterval;
            Plugin.Log.LogInfo(
                $"Refresh: {refreshed} overlays, {context.MemoCount} unique containers, " +
                $"{elapsedMilliseconds:F2} ms.");
        }

        _snapshot.Clear();
    }

    private void SnapshotOverlays()
    {
        _snapshot.Clear();
        foreach (FreeSpaceOverlay overlay in _overlays)
        {
            _snapshot.Add(overlay);
        }
    }

    private static void OnCapacityGuardFailure(CapacityGuardFailure failure)
    {
        Plugin.ThrottledLog.Warning(
            $"capacity-{failure.Reason}-{failure.ParentId}-{failure.ContainerId}",
            $"Capacity {failure.Reason} guard for parent '{failure.ParentId}' " +
            $"and container '{failure.ContainerId}'; the child's footprint was retained " +
            "but its capacity contribution was ignored.",
            30d);
    }

    private void OnSettingChanged(object sender, EventArgs eventArgs)
    {
        _nextRefresh = 0f;
        _nextDebugSummary = 0f;
    }

    private void OnDestroy()
    {
        Shutdown();
    }
}
