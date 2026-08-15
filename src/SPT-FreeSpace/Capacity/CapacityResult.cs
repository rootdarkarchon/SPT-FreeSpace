using System;

namespace SPTFreeSpace.Capacity;

internal readonly struct CapacityResult : IEquatable<CapacityResult>
{
    internal CapacityResult(int available, int total)
    {
        Available = available;
        Total = total;
    }

    internal int Available { get; }

    internal int Total { get; }

    internal int Used => Total - Available;

    public bool Equals(CapacityResult other)
    {
        return Available == other.Available && Total == other.Total;
    }

    public override bool Equals(object? obj)
    {
        return obj is CapacityResult other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Available * 397) ^ Total;
        }
    }

    public override string ToString()
    {
        return $"{Available}/{Total}";
    }

    public static bool operator ==(CapacityResult left, CapacityResult right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CapacityResult left, CapacityResult right)
    {
        return !left.Equals(right);
    }
}
