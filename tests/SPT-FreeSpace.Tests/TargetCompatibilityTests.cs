using NUnit.Framework;
using SPTFreeSpace.Configuration;

namespace SPTFreeSpace.Tests;

internal sealed class TargetCompatibilityTests
{
    [TestCase("4.1.0")]
    [TestCase("4.1.5")]
    [TestCase("4.1.6")]
    [TestCase("4.1.6.0")]
    [TestCase("4.1.99")]
    public void AcceptsSpt41PatchesOnEft40743(string sptVersion)
    {
        Assert.That(TargetCompatibility.Validate(sptVersion, 0, 16, 9, 40743, out string error), Is.True);
        Assert.That(error, Is.Empty);
    }

    [TestCase("4.0.13")]
    [TestCase("4.2.0")]
    [TestCase("5.1.0")]
    [TestCase("4.1")]
    [TestCase("4.1.-1")]
    [TestCase("4.1.6-preview")]
    [TestCase("invalid")]
    [TestCase("")]
    [TestCase(null)]
    public void RejectsUnsupportedOrMalformedSpt(string? sptVersion)
    {
        Assert.That(TargetCompatibility.Validate(sptVersion, 0, 16, 9, 40743, out string error), Is.False);
        Assert.That(error, Does.Contain("expected SPT 4.1.x"));
    }

    [TestCase(0, 16, 9, 40087)]
    [TestCase(0, 16, 9, 4074)]
    [TestCase(0, 16, 9, 40744)]
    [TestCase(1, 16, 9, 40743)]
    [TestCase(0, 17, 9, 40743)]
    [TestCase(0, 16, 10, 40743)]
    [TestCase(0, 0, 0, 0)]
    [TestCase(0, 16, 9, -1)]
    public void RejectsOtherOrMissingNumericEftVersions(int major, int minor, int build, int revision)
    {
        Assert.That(TargetCompatibility.Validate("4.1.6", major, minor, build, revision, out string error), Is.False);
        Assert.That(error, Does.Contain("expected EFT executable file version 0.16.9.40743"));
    }
}
