using NUnit.Framework;
using SPTFreeSpace.Capacity;

namespace SPTFreeSpace.Tests;

internal sealed class CapacityFullnessColorScaleTests
{
    [TestCase(10, 10, 0.0, 1.0, 0.0)]
    [TestCase(7, 10, 0.6, 1.0, 0.0)]
    [TestCase(5, 10, 1.0, 1.0, 0.0)]
    [TestCase(2, 10, 1.0, 0.4, 0.0)]
    [TestCase(0, 10, 1.0, 0.0, 0.0)]
    public void ColorUsesFullnessAcrossGreenYellowRed(
        int available,
        int total,
        double expectedRed,
        double expectedGreen,
        double expectedBlue)
    {
        CapacityColor color = CapacityFullnessColorScale.GetColor(
            new CapacityResult(available, total));

        Assert.Multiple(() =>
        {
            Assert.That(color.Red, Is.EqualTo(expectedRed).Within(0.0001));
            Assert.That(color.Green, Is.EqualTo(expectedGreen).Within(0.0001));
            Assert.That(color.Blue, Is.EqualTo(expectedBlue).Within(0.0001));
        });
    }

    [TestCase(0, 0, 0.0)]
    [TestCase(15, 10, 0.0)]
    [TestCase(-5, 10, 1.0)]
    public void FullnessIsSafeAndClamped(
        int available,
        int total,
        double expected)
    {
        Assert.That(
            CapacityFullnessColorScale.GetFullness(
                new CapacityResult(available, total)),
            Is.EqualTo(expected));
    }
}
