using System;
using System.Diagnostics;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using SPTFreeSpace.Configuration;
using SPTFreeSpace.Diagnostics;
using SPTFreeSpace.Patches;
using SPTFreeSpace.UI;

namespace SPTFreeSpace;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(SptCoreGuid, SptVersion)]
[BepInProcess("EscapeFromTarkov.exe")]
internal sealed class Plugin : BaseUnityPlugin
{
    internal const string PluginGuid = "com.rootdarkarchon.spt-freespace";
    internal const string PluginName = "SPT-FreeSpace";
    internal const string PluginVersion = "1.0.0";
    internal const string SptCoreGuid = "com.SPT.core";
    internal const string SptVersion = "4.0.13";
    internal const string EftFileVersion = "0.16.9.4008";

    internal static ManualLogSource Log { get; private set; } = null!;

    internal static FreeSpaceSettings Settings { get; private set; } = null!;

    internal static ThrottledLogger ThrottledLog { get; private set; } = null!;

    internal static FreeSpaceRefreshService? RefreshService { get; private set; }

    private Harmony? _harmony;

    private void Awake()
    {
        Log = Logger;
        ThrottledLog = new ThrottledLogger(Logger);
        Settings = FreeSpaceSettings.Bind(Config);

        if (!ValidateTargetEnvironment(out string targetError))
        {
            Logger.LogFatal(targetError);
            enabled = false;
            return;
        }

        MethodInfo? target = GridItemViewBindPatch.ResolveTarget();
        if (target == null)
        {
            Logger.LogFatal(
                "SPT-FreeSpace disabled: exact EFT 0.16.9.0.40087 " +
                "GridItemView.NewGridItemView target was not found.");
            enabled = false;
            return;
        }

        try
        {
            RefreshService = gameObject.AddComponent<FreeSpaceRefreshService>();
            RefreshService.Initialize(Settings);

            _harmony = new Harmony(PluginGuid);
            GridItemViewBindPatch.Enable(_harmony, target);
        }
        catch (Exception exception)
        {
            RefreshService?.Shutdown();
            RefreshService = null;
            _harmony?.UnpatchSelf();
            Logger.LogFatal(
                $"SPT-FreeSpace disabled because its item-view postfix could not be applied: " +
                exception);
            enabled = false;
            return;
        }

        Logger.LogInfo($"Resolved item-view bind hook: {target.DeclaringType?.FullName}.{target.Name}");
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded for SPT {SptVersion} / EFT 0.16.9.0.40087.");
    }

    private void OnDestroy()
    {
        RefreshService?.Shutdown();
        RefreshService = null;
        _harmony?.UnpatchSelf();
    }

    private static bool ValidateTargetEnvironment(out string error)
    {
        if (!Chainloader.PluginInfos.TryGetValue(SptCoreGuid, out PluginInfo sptCore))
        {
            error = $"SPT-FreeSpace disabled: required plugin '{SptCoreGuid}' is not loaded.";
            return false;
        }

        if (!string.Equals(sptCore.Metadata.Version.ToString(3), SptVersion, StringComparison.Ordinal))
        {
            error =
                $"SPT-FreeSpace disabled: expected SPT {SptVersion}, " +
                $"found {sptCore.Metadata.Version}.";
            return false;
        }

        string executableVersion =
            FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FileVersion ?? string.Empty;
        if (!string.Equals(executableVersion, EftFileVersion, StringComparison.Ordinal))
        {
            error =
                $"SPT-FreeSpace disabled: expected EFT executable file version {EftFileVersion}, " +
                $"found '{executableVersion}'.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
