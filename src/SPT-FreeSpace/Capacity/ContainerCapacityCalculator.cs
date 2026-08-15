using System;
using System.Collections.Generic;

namespace SPTFreeSpace.Capacity;

internal sealed class ContainerCapacityCalculator<TContainer>
    where TContainer : class
{
    internal const int MaximumDepth = 64;

    private readonly IContainerCapacityAdapter<TContainer> _adapter;

    internal ContainerCapacityCalculator(IContainerCapacityAdapter<TContainer> adapter)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    internal CapacityResult Calculate(
        TContainer container,
        CapacityCalculationContext context)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return TryCalculate(container, context, 0, string.Empty, out CapacityResult result)
            ? result
            : default;
    }

    private bool TryCalculate(
        TContainer container,
        CapacityCalculationContext context,
        int depth,
        string parentId,
        out CapacityResult result)
    {
        string containerId = _adapter.GetStableId(container) ?? string.Empty;
        if (context.Memo.TryGetValue(containerId, out result))
        {
            return true;
        }

        if (depth > MaximumDepth)
        {
            context.ReportGuardFailure(parentId, containerId, CapacityGuardReason.MaximumDepth);
            result = default;
            return false;
        }

        if (!context.Active.Add(containerId))
        {
            context.ReportGuardFailure(parentId, containerId, CapacityGuardReason.Cycle);
            result = default;
            return false;
        }

        try
        {
            long ownTotal = 0;
            long ownOccupied = 0;
            long nestedFootprint = 0;
            long childAvailable = 0;
            long childTotal = 0;
            var directItemIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (CapacityGrid<TContainer> grid in _adapter.GetGrids(container))
            {
                ownTotal = AddClamped(ownTotal, Area(grid.Width, grid.Height));

                foreach (CapacityItem<TContainer> item in grid.DirectItems)
                {
                    if (!directItemIds.Add(item.StableId))
                    {
                        continue;
                    }

                    int footprint = Math.Max(0, item.Footprint);
                    ownOccupied = AddClamped(ownOccupied, footprint);

                    if (item.NestedContainer == null)
                    {
                        continue;
                    }

                    nestedFootprint = AddClamped(nestedFootprint, footprint);
                    if (TryCalculate(
                            item.NestedContainer,
                            context,
                            depth + 1,
                            containerId,
                            out CapacityResult childResult))
                    {
                        childAvailable = AddClamped(childAvailable, childResult.Available);
                        childTotal = AddClamped(childTotal, childResult.Total);
                    }
                }
            }

            long total = ownTotal + childTotal;
            if (!context.CountNestedContainersAsUsed)
            {
                total -= nestedFootprint;
            }

            total = ClampToCapacity(total);
            long available = ownTotal - ownOccupied + childAvailable;
            available = Math.Max(0, Math.Min(total, available));

            result = new CapacityResult((int)available, (int)total);
            context.Memo[containerId] = result;
            return true;
        }
        finally
        {
            context.Active.Remove(containerId);
        }
    }

    private static long Area(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return 0;
        }

        return Math.Min((long)width * height, int.MaxValue);
    }

    private static long AddClamped(long left, long right)
    {
        return Math.Min(int.MaxValue, left + Math.Max(0, right));
    }

    private static long ClampToCapacity(long value)
    {
        return Math.Max(0, Math.Min(int.MaxValue, value));
    }
}
