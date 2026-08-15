using NUnit.Framework;
using SPTFreeSpace.Capacity;

namespace SPTFreeSpace.Tests;

internal sealed class BuildVersionTests
{
    [Test]
    public void PluginMetadataVersionMatchesAssemblyVersion()
    {
        string assemblyVersion =
            typeof(CapacityResult).Assembly.GetName().Version!.ToString(3);

        Assert.That(BuildVersion.Value, Is.EqualTo(assemblyVersion));
    }
}
