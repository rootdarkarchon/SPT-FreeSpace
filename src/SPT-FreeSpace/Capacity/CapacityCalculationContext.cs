using System;
using System.Collections.Generic;

namespace SPTFreeSpace.Capacity;

internal sealed class CapacityCalculationContext
{
    private readonly HashSet<string> _reportedGuardFailures =
        new HashSet<string>(StringComparer.Ordinal);

    internal CapacityCalculationContext(Action<CapacityGuardFailure>? guardFailure = null)
    {
        GuardFailure = guardFailure;
    }

    internal Dictionary<string, CapacityResult> Memo { get; } =
        new Dictionary<string, CapacityResult>(StringComparer.Ordinal);

    internal HashSet<string> Active { get; } = new HashSet<string>(StringComparer.Ordinal);

    internal int MemoCount => Memo.Count;

    private Action<CapacityGuardFailure>? GuardFailure { get; }

    internal void ReportGuardFailure(
        string parentId,
        string containerId,
        CapacityGuardReason reason)
    {
        string key = $"{reason}:{parentId}:{containerId}";
        if (_reportedGuardFailures.Add(key))
        {
            GuardFailure?.Invoke(new CapacityGuardFailure(parentId, containerId, reason));
        }
    }
}
