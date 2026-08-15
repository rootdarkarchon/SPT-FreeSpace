using EFT.InventoryLogic;

namespace SPTFreeSpace.UI;

internal static class PlayerOwnership
{
    internal static bool IsPlayerOwned(
        TraderControllerClass? bindController,
        IItemOwner? bindOwner)
    {
        if (bindController is not InventoryController ||
            bindOwner is not InventoryController)
        {
            return false;
        }

        return ReferenceEquals(bindOwner, bindController) &&
               bindOwner.OwnerType == EOwnerType.Profile;
    }
}
