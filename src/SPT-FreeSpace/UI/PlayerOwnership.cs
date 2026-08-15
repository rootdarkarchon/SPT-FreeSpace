using System;
using EFT.InventoryLogic;

namespace SPTFreeSpace.UI;

internal static class PlayerOwnership
{
    internal static bool IsPlayerOwned(Item? item, TraderControllerClass? bindController)
    {
        if (item == null || bindController is not InventoryController)
        {
            return false;
        }

        IItemOwner? owner = item.Owner;
        return ReferenceEquals(owner, bindController) && owner.OwnerType == EOwnerType.Profile;
    }
}
