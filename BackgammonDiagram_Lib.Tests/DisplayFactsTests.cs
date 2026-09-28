using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// <see cref="DisplayFacts"/> and its values: what a board's diagram draws,
/// validated only for what drawing needs — a die's face it can draw, a place
/// for the cube, text that is text or nothing — and never for a rule of the
/// game.
/// </summary>
public class DisplayFactsTests
{
    [Fact]
    public void Default_IsABareBoard()
    {
        var facts = new DisplayFacts();

        Assert.Null(facts.OnRollName);
        Assert.Null(facts.OpponentName);
        Assert.Null(facts.Title);
        Assert.Null(facts.Dice);
        Assert.Equal(1, facts.CubeValue);
        Assert.Equal(CubeOwner.Centered, facts.CubeOwner);
        Assert.Null(facts.Score);
    }

    // -----------------------------------------------------------------------
    //  Text: stated, or null — the one spelling of none
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Text_RefusesEmptyOrWhiteSpace(string empty)
    {
        Assert.Equal(nameof(DisplayFacts.OnRollName),
            Assert.Throws<ArgumentException>(() => new DisplayFacts { OnRollName = empty }).ParamName);
        Assert.Equal(nameof(DisplayFacts.OpponentName),
            Assert.Throws<ArgumentException>(() => new DisplayFacts { OpponentName = empty }).ParamName);
        Assert.Equal(nameof(DisplayFacts.Title),
            Assert.Throws<ArgumentException>(() => new DisplayFacts { Title = empty }).ParamName);
    }

    [Fact]
    public void With_RevalidatesWhatItSets()
    {
        var facts = new DisplayFacts { Title = "Replay" };

        Assert.Throws<ArgumentException>(() => facts with { Title = "" });
    }

    // -----------------------------------------------------------------------
    //  What drawing needs, and nothing the game rules
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, 3)]
    [InlineData(3, 7)]
    [InlineData(-1, 1)]
    public void DiceFaces_RefuseAFaceNoDieShows(int left, int right) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiceFaces(left, right));

    [Fact]
    public void DiceFaces_KeepTheirOrder()
    {
        // Unlike a canonical roll, the faces shown keep the order given.
        var faces = new DiceFaces(1, 3);

        Assert.Equal(1, faces.Left);
        Assert.Equal(3, faces.Right);
        Assert.NotEqual(new DiceFaces(3, 1), faces);
    }

    [Fact]
    public void CubeOwner_RefusesAPlaceThatIsNotOne() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DisplayFacts { CubeOwner = (CubeOwner)99 });

    [Theory]
    [InlineData(3)]       // not a power of two
    [InlineData(8192)]    // above any money limit
    public void CubeValue_IsDrawnAsGiven_NoLegalityApplied(int value)
    {
        // A cube's legal values are the domain's rule, not the diagram's.
        var request = DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts { CubeValue = value });

        Assert.Equal(value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            TestFixtures.CubeFace(TestFixtures.Render(request)));
    }

    [Fact]
    public void MatchRailScore_ShowsAMatchWonZeroAway()
    {
        // A match won 0-away is no valid match session; its rails still say so.
        var request = DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts
        {
            OnRollName = "One",
            OpponentName = "Two",
            Score = new MatchRailScore(onRollNeeds: 0, opponentNeeds: 2, isCrawford: false),
        });

        var svg = TestFixtures.Render(request);
        Assert.Contains(">One needs 0</text>", svg);
        Assert.Contains(">Two needs 2</text>", svg);
    }

    // -----------------------------------------------------------------------
    //  RailScore — a closed pair with value equality
    // -----------------------------------------------------------------------

    [Fact]
    public void RailScore_EqualWithinAKind()
    {
        Assert.Equal(new MatchRailScore(3, 5, isCrawford: false), new MatchRailScore(3, 5, isCrawford: false));
        Assert.True(new MatchRailScore(3, 5, isCrawford: false) == new MatchRailScore(3, 5, isCrawford: false));
        Assert.NotEqual(new MatchRailScore(3, 5, isCrawford: false), new MatchRailScore(5, 3, isCrawford: false));
        Assert.NotEqual(new MatchRailScore(3, 5, isCrawford: true), new MatchRailScore(3, 5, isCrawford: false));
        Assert.Equal<RailScore>(new MoneyRailScore(isJacoby: null), new MoneyRailScore(isJacoby: null));
        Assert.Equal<RailScore>(new MoneyRailScore(isJacoby: true), new MoneyRailScore(isJacoby: true));
        Assert.NotEqual<RailScore>(new MoneyRailScore(isJacoby: true), new MoneyRailScore(isJacoby: false));
        Assert.NotEqual<RailScore>(new MoneyRailScore(isJacoby: false), new MoneyRailScore(isJacoby: null));
        Assert.NotEqual<RailScore>(new MoneyRailScore(isJacoby: null), new MatchRailScore(1, 1, isCrawford: false));
        Assert.Equal(new MatchRailScore(3, 5, isCrawford: false).GetHashCode(), new MatchRailScore(3, 5, isCrawford: false).GetHashCode());
    }

    [Fact]
    public void DisplayFacts_HaveValueEquality()
    {
        DisplayFacts Facts() => new()
        {
            OnRollName = "One",
            Dice = new DiceFaces(6, 2),
            CubeValue = 4,
            CubeOwner = CubeOwner.Opponent,
            Score = new MoneyRailScore(isJacoby: null),
        };

        Assert.Equal(Facts(), Facts());
        Assert.NotEqual(Facts(), Facts() with { Dice = new DiceFaces(2, 6) });
    }
}
