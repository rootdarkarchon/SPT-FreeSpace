using NUnit.Framework;
using SPTFreeSpace.Capacity;
using SPTFreeSpace.Configuration;

namespace SPTFreeSpace.Tests;

[TestFixture]
internal sealed class CapacityDisplayFormatterTests
{
    [Test]
    public void UsedTotal_ShowsConsumedRecursiveCapacity()
    {
        Assert.That(
            CapacityDisplayFormatter.Format(
                new CapacityResult(7, 10),
                CapacityDisplayMode.UsedTotal),
            Is.EqualTo("3/10"));
    }

    [Test]
    public void AvailableTotal_ShowsRemainingRecursiveCapacity()
    {
        Assert.That(
            CapacityDisplayFormatter.Format(
                new CapacityResult(7, 10),
                CapacityDisplayMode.AvailableTotal),
            Is.EqualTo("7/10"));
    }

    [Test]
    public void UnknownMode_FallsBackToUsedTotal()
    {
        Assert.That(
            CapacityDisplayFormatter.Format(
                new CapacityResult(7, 10),
                (CapacityDisplayMode)999),
            Is.EqualTo("3/10"));
    }
}
