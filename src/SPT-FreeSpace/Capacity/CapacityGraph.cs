using System;
using System.Collections.Generic;

namespace SPTFreeSpace.Capacity;

internal interface IContainerCapacityAdapter<TContainer>
    where TContainer : class
{
    string GetStableId(TContainer container);

    IEnumerable<CapacityGrid<TContainer>> GetGrids(TContainer container);
}

internal readonly struct CapacityGrid<TContainer>
    where TContainer : class
{
    internal CapacityGrid(
        int width,
        int height,
        IEnumerable<CapacityItem<TContainer>> directItems)
    {
        Width = width;
        Height = height;
        DirectItems = directItems ?? throw new ArgumentNullException(nameof(directItems));
    }

    internal int Width { get; }

    internal int Height { get; }

    internal IEnumerable<CapacityItem<TContainer>> DirectItems { get; }
}

internal readonly struct CapacityItem<TContainer>
    where TContainer : class
{
    internal CapacityItem(string stableId, int footprint, TContainer? nestedContainer)
    {
        StableId = stableId ?? throw new ArgumentNullException(nameof(stableId));
        Footprint = footprint;
        NestedContainer = nestedContainer;
    }

    internal string StableId { get; }

    internal int Footprint { get; }

    internal TContainer? NestedContainer { get; }
}

internal enum CapacityGuardReason
{
    Cycle,
    MaximumDepth,
}

internal readonly struct CapacityGuardFailure
{
    internal CapacityGuardFailure(
        string parentId,
        string containerId,
        CapacityGuardReason reason)
    {
        ParentId = parentId;
        ContainerId = containerId;
        Reason = reason;
    }

    internal string ParentId { get; }

    internal string ContainerId { get; }

    internal CapacityGuardReason Reason { get; }
}
