using System;
using System.Collections.Generic;
using EFT.InventoryLogic;

namespace SPTFreeSpace.Capacity;

internal sealed class ItemGridAdapter : IContainerCapacityAdapter<CompoundItem>
{
    internal static readonly ItemGridAdapter Instance = new ItemGridAdapter();

    private ItemGridAdapter()
    {
    }

    public string GetStableId(CompoundItem container)
    {
        return container.Id;
    }

    public IEnumerable<CapacityGrid<CompoundItem>> GetGrids(CompoundItem container)
    {
        StashGridClass[]? grids = container.Grids;
        if (grids == null)
        {
            yield break;
        }

        foreach (StashGridClass? grid in grids)
        {
            if (grid == null)
            {
                continue;
            }

            yield return new CapacityGrid<CompoundItem>(
                grid.GridWidth,
                grid.GridHeight,
                EnumerateDirectItems(grid));
        }
    }

    internal static bool IsEligibleContainer(CompoundItem? container)
    {
        StashGridClass[]? grids = container?.Grids;
        if (grids == null)
        {
            return false;
        }

        foreach (StashGridClass? grid in grids)
        {
            if (grid != null && grid.GridWidth > 0 && grid.GridHeight > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<CapacityItem<CompoundItem>> EnumerateDirectItems(
        StashGridClass grid)
    {
        foreach (Item? item in grid.Items)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
            {
                continue;
            }

            ItemAddress? address = item.CurrentAddress;
            if (address == null || !ReferenceEquals(address.Container, grid))
            {
                continue;
            }

            LocationInGrid? location = grid.GetItemLocation(item);
            if (location == null)
            {
                continue;
            }

            XYCellSizeStruct size = item.CalculateRotatedSize(location.r);
            int footprint = CalculateFootprint(size.X, size.Y);
            CompoundItem? child = item as CompoundItem;
            if (!IsEligibleContainer(child))
            {
                child = null;
            }

            yield return new CapacityItem<CompoundItem>(item.Id, footprint, child);
        }
    }

    private static int CalculateFootprint(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return 0;
        }

        return (int)Math.Min((long)width * height, int.MaxValue);
    }
}
