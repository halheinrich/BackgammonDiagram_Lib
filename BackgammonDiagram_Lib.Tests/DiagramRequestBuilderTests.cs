// DiagramRequestBuilderTests.cs  (new file)
namespace BackgammonDiagram_Lib.Tests;

public class DiagramRequestBuilderTests
{
    [Fact]
    public void Build_ValidCheckerPlay_Succeeds()
    {
        var req = TestFixtures.MinimalRequest();
        Assert.NotNull(req);
    }

    [Fact]
    public void Build_MopWrongLength_Throws()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = new int[10];
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Build_DiceWrongLength_Throws()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Dice = [1, 2, 3];
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Build_IsCubeFalse_DieOutOfRange_Throws()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Dice = [0, 3];   // 0 is invalid for a checker play
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Build_IsCubeFalse_DieOver6_Throws()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Dice = [3, 7];
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Build_IsCubeTrue_DiceNotZero_Throws()
    {
        var b = TestFixtures.MinimalBuilder();
        b.IsCube = true;
        b.Dice = [3, 1];
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Build_IsCubeTrue_DiceZero_Succeeds()
    {
        var b = TestFixtures.MinimalBuilder();
        b.IsCube = true;
        b.Dice = [0, 0];
        var req = b.Build();
        Assert.True(req.IsCube);
    }

    [Fact]
    public void Build_CubeSizeNotPowerOfTwo_Throws()
    {
        var b = TestFixtures.MinimalBuilder();
        b.CubeSize = 3;
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Build_CubeSizeZero_Throws()
    {
        var b = TestFixtures.MinimalBuilder();
        b.CubeSize = 0;
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Build_CubeSizeOver4096_Throws()
    {
        var b = TestFixtures.MinimalBuilder();
        b.CubeSize = 8192;
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Build_CubeSize4096_Succeeds()
    {
        var b = TestFixtures.MinimalBuilder();
        b.CubeSize = 4096;
        var req = b.Build();
        Assert.Equal(4096, req.CubeSize);
    }

    [Fact]
    public void Build_CubeSize1_Succeeds()
    {
        var b = TestFixtures.MinimalBuilder();
        b.CubeSize = 1;
        var req = b.Build();
        Assert.Equal(1, req.CubeSize);
    }
}