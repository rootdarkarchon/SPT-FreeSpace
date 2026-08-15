using System;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPTFreeSpace.UI;

namespace SPTFreeSpace.Patches;

internal static class GridItemViewBindPatch
{
    private static readonly Type[] TargetParameterTypes =
    {
        typeof(Item),
        typeof(ItemContextAbstractClass),
        typeof(ItemRotation),
        typeof(TraderControllerClass),
        typeof(IItemOwner),
        typeof(FilterPanel),
        typeof(global::IContainer),
        typeof(ItemUiContext),
        typeof(InsuranceCompanyClass),
        typeof(GClass2067),
    };

    internal static MethodInfo? ResolveTarget()
    {
        return AccessTools.DeclaredMethod(
            typeof(GridItemView),
            nameof(GridItemView.NewGridItemView),
            TargetParameterTypes);
    }

    internal static void Enable(Harmony harmony, MethodInfo target)
    {
        MethodInfo postfix = AccessTools.DeclaredMethod(
            typeof(GridItemViewBindPatch),
            nameof(Postfix)) ?? throw new MissingMethodException(nameof(Postfix));
        harmony.Patch(target, postfix: new HarmonyMethod(postfix));
    }

    private static void Postfix(
        GridItemView __instance,
        TraderControllerClass itemController,
        IItemOwner itemOwner)
    {
        try
        {
            FreeSpaceOverlayFactory.Bind(
                __instance,
                __instance.Item,
                itemController,
                itemOwner);
        }
        catch (Exception exception)
        {
            Plugin.ThrottledLog.Error(
                "overlay-bind",
                $"Failed to bind a free-space overlay: {exception}");
        }
    }
}
