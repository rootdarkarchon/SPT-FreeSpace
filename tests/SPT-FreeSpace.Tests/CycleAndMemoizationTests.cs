using NUnit.Framework;
using SPTFreeSpace.Capacity;
using static SPTFreeSpace.Tests.CapacityFormulaTests;

namespace SPTFreeSpace.Tests;

[TestFixture]
internal sealed class CycleAndMemoizationTests
{
    [Test]
    public void Cycle_TerminatesKeepsFootprintAndReportsOnce()
    {
        var parent = new MutableContainer("parent", 20);
        var child = new MutableContainer("child", 10);
        parent.Items.Add(new MutableItem("child-item", 4, child));
        child.Items.Add(new MutableItem("parent-item", 2, parent));
        var failures = new List<CapacityGuardFailure>();
        var calculator = new ContainerCapacityCalculator<MutableContainer>(new MutableAdapter());

        CapacityResult result = calculator.Calculate(
            parent,
            new CapacityCalculationContext(failures.Add));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new CapacityResult(24, 24)));
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(failures[0].Reason, Is.EqualTo(CapacityGuardReason.Cycle));
            Assert.That(failures[0].ParentId, Is.EqualTo("child"));
            Assert.That(failures[0].ContainerId, Is.EqualTo("parent"));
        });
    }

    [Test]
    public void MemoizedChild_IsCalculatedOncePerRefreshContext()
    {
        var child = new MutableContainer("child", 12);
        var parent = new MutableContainer("parent", 20);
        parent.Items.Add(new MutableItem("child-item", 4, child));
        var adapter = new MutableAdapter();
        var calculator = new ContainerCapacityCalculator<MutableContainer>(adapter);
        var context = new CapacityCalculationContext();

        CapacityResult parentResult = calculator.Calculate(parent, context);
        CapacityResult childResult = calculator.Calculate(child, context);

        Assert.Multiple(() =>
        {
            Assert.That(parentResult, Is.EqualTo(new CapacityResult(28, 28)));
            Assert.That(childResult, Is.EqualTo(new CapacityResult(12, 12)));
            Assert.That(adapter.GridEnumerations["child"], Is.EqualTo(1));
        });
    }

    [Test]
    public void MaximumDepth_TerminatesAndReportsSafely()
    {
        var root = new MutableContainer("0", 1);
        MutableContainer current = root;
        for (int index = 1; index <= ContainerCapacityCalculator<MutableContainer>.MaximumDepth + 2; index++)
        {
            var next = new MutableContainer(index.ToString(), 1);
            current.Items.Add(new MutableItem($"item-{index}", 1, next));
            current = next;
        }

        var failures = new List<CapacityGuardFailure>();
        var calculator = new ContainerCapacityCalculator<MutableContainer>(new MutableAdapter());

        CapacityResult result = calculator.Calculate(
            root,
            new CapacityCalculationContext(failures.Add));

        Assert.Multiple(() =>
        {
            Assert.That(result.Available, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Available, Is.LessThanOrEqualTo(result.Total));
            Assert.That(failures.Any(x => x.Reason == CapacityGuardReason.MaximumDepth), Is.True);
        });
    }

    private sealed class MutableContainer
    {
        internal MutableContainer(string id, int cells)
        {
            Id = id;
            Cells = cells;
        }

        internal string Id { get; }

        internal int Cells { get; }

        internal List<MutableItem> Items { get; } = new List<MutableItem>();
    }

    private sealed record MutableItem(string Id, int Footprint, MutableContainer? Child);

    private sealed class MutableAdapter : IContainerCapacityAdapter<MutableContainer>
    {
        internal Dictionary<string, int> GridEnumerations { get; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public string GetStableId(MutableContainer container)
        {
            return container.Id;
        }

        public IEnumerable<CapacityGrid<MutableContainer>> GetGrids(MutableContainer container)
        {
            GridEnumerations.TryGetValue(container.Id, out int count);
            GridEnumerations[container.Id] = count + 1;

            yield return new CapacityGrid<MutableContainer>(
                container.Cells,
                1,
                container.Items.Select(
                    item => new CapacityItem<MutableContainer>(
                        item.Id,
                        item.Footprint,
                        item.Child)));
        }
    }
}
