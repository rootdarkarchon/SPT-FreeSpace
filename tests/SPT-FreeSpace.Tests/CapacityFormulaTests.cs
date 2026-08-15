using NUnit.Framework;
using SPTFreeSpace.Capacity;

namespace SPTFreeSpace.Tests;

[TestFixture]
internal sealed class CapacityFormulaTests
{
    [Test]
    public void EmptyTenCellContainer_IsTenOfTen()
    {
        Assert.That(Calculate(Container("root", Grid(10, 1))), Is.EqualTo(Result(10, 10)));
    }

    [Test]
    public void OrdinaryThreeCellItem_ReducesOnlyAvailable()
    {
        FakeContainer root = Container("root", Grid(10, 1, Item("ordinary", 3)));

        Assert.That(Calculate(root), Is.EqualTo(Result(7, 10)));
    }

    [Test]
    public void MultipleGrids_AreSummed()
    {
        FakeContainer root = Container("root", Grid(4, 4), Grid(2, 3));

        Assert.That(Calculate(root), Is.EqualTo(Result(22, 22)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RotatedTwoByThreeItem_OccupiesSixCells(bool rotated)
    {
        var item = new FakeItem("ordinary", 2, 3, rotated, null);
        FakeContainer root = Container("root", Grid(10, 1, item));

        Assert.That(Calculate(root), Is.EqualTo(Result(4, 10)));
    }

    [Test]
    public void EmptyTwelveCellChild_CountsFourCellFootprintAsUsedByDefault()
    {
        FakeContainer child = Container("child", Grid(4, 3));
        FakeContainer parent = Container("parent", Grid(5, 4, Nested("child-item", 4, child)));

        Assert.That(Calculate(parent), Is.EqualTo(Result(28, 32)));
    }

    [Test]
    public void ParentOrdinaryItem_ReducesRecursiveAvailableOnly()
    {
        FakeContainer child = Container("child", Grid(4, 3));
        FakeContainer parent = Container(
            "parent",
            Grid(5, 4, Nested("child-item", 4, child), Item("ordinary", 3)));

        Assert.That(Calculate(parent), Is.EqualTo(Result(25, 32)));
    }

    [Test]
    public void FoldedChildContainer_IsAnOrdinaryOccupiedItem()
    {
        FakeContainer foldedChild = Container("folded-child", Grid(4, 3));
        FakeContainer parent = Container(
            "parent",
            Grid(5, 4, Folded("folded-child-item", 4, foldedChild)));

        Assert.That(Calculate(parent), Is.EqualTo(Result(16, 20)));
    }

    [Test]
    public void OccupiedChildPayload_ReducesRecursiveAvailable()
    {
        FakeContainer child = Container("child", Grid(4, 3, Item("payload", 5)));
        FakeContainer parent = Container("parent", Grid(5, 4, Nested("child-item", 4, child)));

        Assert.That(Calculate(parent), Is.EqualTo(Result(23, 32)));
    }

    [Test]
    public void ThreeNestedLevels_AggregateOncePerLevel()
    {
        FakeContainer grandchild = Container("grandchild", Grid(3, 3, Item("payload", 2)));
        FakeContainer child = Container(
            "child",
            Grid(4, 3, Nested("grandchild-item", 2, grandchild)));
        FakeContainer parent = Container("parent", Grid(5, 4, Nested("child-item", 4, child)));

        Assert.That(Calculate(parent), Is.EqualTo(Result(33, 41)));
    }

    [Test]
    public void TwoSiblingContainers_AreBothIncludedOnce()
    {
        FakeContainer first = Container("first", Grid(3, 2));
        FakeContainer second = Container("second", Grid(2, 2, Item("payload", 1)));
        FakeContainer parent = Container(
            "parent",
            Grid(
                5,
                4,
                Nested("first-item", 2, first),
                Nested("second-item", 1, second)));

        Assert.That(Calculate(parent), Is.EqualTo(Result(26, 30)));
    }

    [Test]
    public void DisabledNestedUsedSpace_RestoresNetUsableRecursiveTotal()
    {
        FakeContainer child = Container("child", Grid(4, 3));
        FakeContainer parent = Container("parent", Grid(5, 4, Nested("child-item", 4, child)));

        Assert.That(
            Calculate(parent, countNestedContainersAsUsed: false),
            Is.EqualTo(Result(28, 28)));
    }

    [Test]
    public void MalformedOverOccupancy_ClampsToZeroAvailable()
    {
        FakeContainer root = Container("root", Grid(2, 2, Item("too-large", 10)));

        Assert.That(Calculate(root), Is.EqualTo(Result(0, 4)));
    }

    private static CapacityResult Calculate(
        FakeContainer container,
        bool countNestedContainersAsUsed = true)
    {
        var calculator = new ContainerCapacityCalculator<FakeContainer>(new FakeAdapter());
        return calculator.Calculate(
            container,
            new CapacityCalculationContext(countNestedContainersAsUsed));
    }

    private static CapacityResult Result(int available, int total)
    {
        return new CapacityResult(available, total);
    }

    private static FakeContainer Container(string id, params FakeGrid[] grids)
    {
        return new FakeContainer(id, grids);
    }

    private static FakeGrid Grid(int width, int height, params FakeItem[] items)
    {
        return new FakeGrid(width, height, items);
    }

    private static FakeItem Item(string id, int footprint)
    {
        return new FakeItem(id, footprint, 1, false, null);
    }

    private static FakeItem Nested(string id, int footprint, FakeContainer child)
    {
        return new FakeItem(id, footprint, 1, false, child);
    }

    private static FakeItem Folded(string id, int footprint, FakeContainer ignoredChild)
    {
        _ = ignoredChild;
        return new FakeItem(id, footprint, 1, false, null);
    }

    internal sealed record FakeContainer(string Id, IReadOnlyList<FakeGrid> Grids);

    internal sealed record FakeGrid(int Width, int Height, IReadOnlyList<FakeItem> Items);

    internal sealed record FakeItem(
        string Id,
        int Width,
        int Height,
        bool Rotated,
        FakeContainer? Child)
    {
        internal int Footprint => Rotated ? Height * Width : Width * Height;
    }

    internal sealed class FakeAdapter : IContainerCapacityAdapter<FakeContainer>
    {
        public string GetStableId(FakeContainer container)
        {
            return container.Id;
        }

        public IEnumerable<CapacityGrid<FakeContainer>> GetGrids(FakeContainer container)
        {
            return container.Grids.Select(
                grid => new CapacityGrid<FakeContainer>(
                    grid.Width,
                    grid.Height,
                    grid.Items.Select(
                        item => new CapacityItem<FakeContainer>(
                            item.Id,
                            item.Footprint,
                            item.Child))));
        }
    }
}
