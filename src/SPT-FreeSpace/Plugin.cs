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
[BepInDependency(SptCoreGuid, TargetCompatibility.MinimumSptVersion)]
[BepInProcess("EscapeFromTarkov.exe")]
internal sealed class Plugin : BaseUnityPlugin
{
    internal const string PluginGuid = "com.rootdarkarchon.spt-freespace";
    internal const string PluginName = "SPT-FreeSpace";
    internal const string PluginVersion = BuildVersion.Value;
    internal const string SptCoreGuid = "com.SPT.core";

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

        try
        {
            MethodInfo target = GridItemViewBindPatch.ResolveTarget() ??
                throw new MissingMethodException(
                    $"Exact EFT {TargetCompatibility.EftFileVersion} " +
                    "GridItemView.NewGridItemView target was not found.");

            RefreshService = gameObject.AddComponent<FreeSpaceRefreshService>();
            RefreshService.Initialize(Settings);

            _harmony = new Harmony(PluginGuid);
            GridItemViewBindPatch.Enable(_harmony, target);
            Logger.LogInfo($"Resolved item-view bind hook: {target.DeclaringType?.FullName}.{target.Name}");
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

        Logger.LogInfo(
            $"{PluginName} {PluginVersion} loaded for SPT " +
            $"{Chainloader.PluginInfos[SptCoreGuid].Metadata.Version} / EFT {TargetCompatibility.EftFileVersion}.");
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

        FileVersionInfo executableVersion = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath);
        return TargetCompatibility.Validate(
            sptCore.Metadata.Version.ToString(),
            executableVersion.FileMajorPart, executableVersion.FileMinorPart,
            executableVersion.FileBuildPart, executableVersion.FilePrivatePart,
            out error);
    }
}
