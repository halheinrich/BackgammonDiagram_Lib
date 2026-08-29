using System.Collections;
using System.Reflection;
using BackgammonDiagram_Lib;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Enforces <see cref="DiagramRequest.Builder"/>'s full-copy invariant: the
/// Builder re-spells <see cref="PositionData"/>, <see cref="DecisionData"/>,
/// and <see cref="DescriptiveData"/> field-by-field rather than holding the
/// instances, so it is a second enumeration of the data layer's facts that
/// nothing else checks — it goes stale silently on every field addition
/// (halheinrich/backgammon#122, whose latent drop of
/// <see cref="PositionData.IsJacoby"/> became visible as
/// halheinrich/backgammon#143).
///
/// <para>
/// Rather than restate the rule in prose, these tests reflect over the three
/// record types and assert that every carriable member survives both
/// <c>Builder.From</c> overloads. A field added to any of those records fails
/// here until it joins the copy in <c>From</c> and <c>Build</c>. The
/// renderer-specific members of <see cref="DiagramRequest"/> itself get the
/// same treatment in <see cref="Builder_CarriesEveryRendererSpecificField"/> —
/// they are hand-carried by <c>Builder.From(DiagramRequest)</c> only, which
/// the record-level reflection cannot see.
/// </para>
///
/// <para>
/// <b>Where the "carriable" line is drawn:</b> public instance properties with
/// <see cref="PropertyInfo.CanWrite"/> <c>== true</c>. Init-only accessors
/// report <c>CanWrite == true</c> (<c>init</c> is a setter carrying a modreq
/// that reflection ignores), so they are included — they are producer-supplied
/// state and must be carried. Computed get-only properties report
/// <c>false</c> and are excluded: they are derived from carried state, so
/// carrying their inputs carries them.
/// </para>
/// </summary>
public class BuilderFieldCarriageTests
{
    [Fact]
    public void Builder_CarriesEveryPositionDataField()
        => AssertEveryFieldCarried(typeof(PositionData), new PositionData(), r => r.Position);

    [Fact]
    public void Builder_CarriesEveryDecisionDataField()
        => AssertEveryFieldCarried(typeof(DecisionData), new DecisionData(), r => r.Decision);

    [Fact]
    public void Builder_CarriesEveryDescriptiveDataField()
        => AssertEveryFieldCarried(typeof(DescriptiveData), new DescriptiveData(), r => r.Descriptive);

    /// <summary>
    /// The same completeness net over <see cref="DiagramRequest"/>'s own
    /// renderer-specific members — everything outside the three data records.
    /// Those are hand-carried by <c>Builder.From(DiagramRequest)</c> (the
    /// three-record overload deliberately leaves them at their defaults, per
    /// the <c>SecondaryPlayIndex</c> precedent), so the record-reflection
    /// facts above cannot see a drop. Two guards per member: the fixture must
    /// set it away from its default (a new field fails here until it joins
    /// the fixture — and joining the fixture requires the Builder property),
    /// and it must survive the <c>From(DiagramRequest).Build()</c> round-trip
    /// (fails until it joins the copy).
    /// </summary>
    [Fact]
    public void Builder_CarriesEveryRendererSpecificField()
    {
        // Property defaults, via the internal parameterless constructor the
        // Builder itself instantiates through.
        var pristine = new DiagramRequest();

        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;                          // default Problem
        b.HomeBoardOnRight = false;                             // default true
        b.OnRollAtBottom = false;                               // default true
        b.AnalysisPanelPosition = PanelPosition.Right;          // default Left
        b.PositionNumber = 7;                                   // default null
        b.Xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:31:0:0:0:0:10";  // default ""
        b.SecondaryPlayIndex = 3;                               // default −1
        b.CandidateOrdering = CandidateOrdering.DepthFirst;     // default Equity
        b.MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply4;   // default null
        var built = b.Build();

        var cloned = DiagramRequest.Builder.From(built).Build();

        var recordMembers = new[]
        {
            nameof(DiagramRequest.Position),
            nameof(DiagramRequest.Decision),
            nameof(DiagramRequest.Descriptive),
        };
        var members = CarriableMembers(typeof(DiagramRequest))
            .Where(m => !recordMembers.Contains(m.Name))
            .ToList();
        Assert.NotEmpty(members);

        foreach (var m in members)
        {
            Assert.True(
                !SameValue(m.GetValue(built), m.GetValue(pristine)),
                $"DiagramRequest.{m.Name} is left at its default by this test's fixture, so "
                + "the round-trip assertion below cannot fail when it is dropped. Set it "
                + "away from its default in the fixture above.");

            Assert.True(
                SameValue(m.GetValue(built), m.GetValue(cloned)),
                $"Builder.From(DiagramRequest).Build() drops DiagramRequest.{m.Name}. "
                + "Renderer-specific members are hand-carried — copy it in "
                + "Builder.From(DiagramRequest) and emit it in Build().");
        }
    }

    // -----------------------------------------------------------------------
    //  The assertion
    // -----------------------------------------------------------------------

    /// <param name="recordType">The data record whose members must all survive.</param>
    /// <param name="pristine">
    /// A default-constructed instance, supplying each member's own default —
    /// the baseline for "the fixture actually set this to something". Compared
    /// against the record's defaults, not <c>default(T)</c>, because several
    /// members declare their own (<c>CubeSize = 1</c>, <c>Comment = ""</c>).
    /// </param>
    /// <param name="select">Picks this record out of a built request.</param>
    private static void AssertEveryFieldCarried(
        Type recordType,
        object pristine,
        Func<DiagramRequest, object> select)
    {
        var members = CarriableMembers(recordType).ToList();
        Assert.NotEmpty(members);

        foreach (var m in members)
        {
            // A member counts as exercised if at least one fixture gives it a
            // non-default value. Two fixtures are needed because IsCube and
            // Dice are mutually exclusive under Build()'s validation: a cube
            // decision must carry [0, 0] dice, which is Dice's own default, so
            // no single fixture can hold both away from their defaults.
            bool exercisedSomewhere = false;

            foreach (var fixture in Fixtures())
            {
                object source = select(fixture.Source);
                object carried = select(fixture.Built);
                object cloned = select(fixture.Cloned);

                if (!SameValue(m.GetValue(source), m.GetValue(pristine)))
                    exercisedSomewhere = true;

                Assert.True(
                    SameValue(m.GetValue(source), m.GetValue(carried)),
                    $"Builder.From(position, decision, descriptive).Build() drops "
                    + $"{recordType.Name}.{m.Name} (fixture '{fixture.Name}'). Carry it in "
                    + "both Builder.From and Builder.Build — see the Builder's full-copy invariant.");

                Assert.True(
                    SameValue(m.GetValue(source), m.GetValue(cloned)),
                    $"Builder.From(DiagramRequest).Build() drops {recordType.Name}.{m.Name} "
                    + $"(fixture '{fixture.Name}'). The cloning overload forwards to the "
                    + "three-record one, so a drop here is a drop in the shared copy site.");
            }

            Assert.True(
                exercisedSomewhere,
                $"{recordType.Name}.{m.Name} is left at its default by every fixture in this "
                + "test, so the carriage assertions above cannot fail when it is dropped. "
                + "Give it a non-default value in the FullyPopulated* fixtures below.");
        }
    }

    // -----------------------------------------------------------------------
    //  Reflection
    // -----------------------------------------------------------------------

    private static IEnumerable<PropertyInfo> CarriableMembers(Type t) =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
         .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
         .OrderBy(p => p.Name, StringComparer.Ordinal);

    /// <summary>
    /// Structural equality that also handles the records' collection members
    /// (<c>Mop</c>, <c>Dice</c>, <c>Plays</c>), which <c>Build()</c> copies
    /// into fresh instances and so are never reference-equal.
    /// </summary>
    private static bool SameValue(object? a, object? b)
    {
        if (a is null || b is null)
            return a is null && b is null;
        if (a is not string && a is IEnumerable ea && b is IEnumerable eb)
            return ea.Cast<object?>().SequenceEqual(eb.Cast<object?>());
        return a.Equals(b);
    }

    // -----------------------------------------------------------------------
    //  Fixtures — every carriable member set away from its default
    // -----------------------------------------------------------------------

    private sealed record Fixture(
        string Name,
        DiagramRequest Source,
        DiagramRequest Built,
        DiagramRequest Cloned);

    private static IEnumerable<Fixture> Fixtures()
    {
        yield return MakeFixture("checker decision", isCube: false);
        yield return MakeFixture("cube decision", isCube: true);
    }

    /// <summary>
    /// Builds the three records by hand, then runs them through both
    /// <c>Builder.From</c> overloads. The hand-built records — not a
    /// Builder-produced request — are the comparison baseline: a request built
    /// through the Builder would already have lost any dropped field, so the
    /// round-trip would compare a default against a default and pass.
    /// </summary>
    private static Fixture MakeFixture(string name, bool isCube)
    {
        var position = FullyPopulatedPosition();
        var decision = FullyPopulatedDecision(isCube);
        var descriptive = FullyPopulatedDescriptive();

        // The baseline request holds exactly the hand-built records, so the
        // members read off it are the fixture's own values.
        var source = new DiagramRequest
        {
            Position = position,
            Decision = decision,
            Descriptive = descriptive,
        };

        var built = DiagramRequest.Builder.From(position, decision, descriptive).Build();
        var cloned = DiagramRequest.Builder.From(built).Build();

        return new Fixture(name, source, built, cloned);
    }

    private static PositionData FullyPopulatedPosition() => new()
    {
        Mop = TestFixtures.StartingMop(),
        OnRollNeeds = 7,
        OpponentNeeds = 9,
        OnRollPipCount = 131,
        OpponentPipCount = 142,
        CubeSize = 4,                       // default is 1
        CubeOwner = CubeOwner.Opponent,     // default is OnRoll (0)
        IsCrawford = true,
        IsJacoby = true,                    // default is null
    };

    private static DecisionData FullyPopulatedDecision(bool isCube) => new()
    {
        IsCube = isCube,
        // Build() validates the pairing: a cube decision carries [0, 0], a
        // checker decision carries real dice.
        Dice = isCube ? new[] { 0, 0 } : new[] { 6, 5 },
        Plays = [new PlayCandidate { MoveNotation = "24/18 13/11", DepthRank = 3 }],
        CubeDepth = "XG Roller+",
        CubeDepthAbbreviation = "R+",
        CubeDepthRank = 45,
        CubeAnalysisMode = AnalysisMode.Rollout,        // default is Unknown (0)
        CubeAnalysisLevel = AnalysisLevel.XgRollerPlus, // default is Unknown (0)
        BestPlayIndex = 2,
        UserPlayIndex = 1,
        UserPlayError = 0.0123,
        NoDoubleEquity = 0.4321,
        DoubleTakeEquity = 0.6543,
        CubelessNoDoubleEquity = 0.4111,
        CubelessDoubleTakeEquity = 0.6222,
        WinPctAfterNoDouble = 0.71,
        GammonPctAfterNoDouble = 0.12,
        BgPctAfterNoDouble = 0.013,
        LosePctAfterNoDouble = 0.29,
        LoseGammonPctAfterNoDouble = 0.09,
        LoseBgPctAfterNoDouble = 0.003,
        WinPctAfterDoubleTake = 0.72,
        GammonPctAfterDoubleTake = 0.13,
        BgPctAfterDoubleTake = 0.014,
        LosePctAfterDoubleTake = 0.28,
        LoseGammonPctAfterDoubleTake = 0.08,
        LoseBgPctAfterDoubleTake = 0.002,
        ProbOfOpponentErrorJustifyingDouble = 0.05,
        UserDoubleError = 0.006,
        UserTakeError = 0.007,
        // Guarded to their own action halves by DecisionData; cross-half and
        // IsCube consistency are producer contracts it deliberately does not
        // enforce, so this fixture sets them on both flavours. It is
        // field-complete rather than semantically realistic on purpose — its
        // job is to prove carriage, not to model a real decision.
        UserDoublerAction = CubeAction.Double,
        UserTakerAction = CubeAction.Take,
    };

    private static DescriptiveData FullyPopulatedDescriptive() => new()
    {
        OnRollName = "Alice",
        OpponentName = "Bob",
        Title = "Pivotal decision",
        MatchLength = 11,
        Date = new DateOnly(2026, 4, 16),
        Event = "Club championship",
        SourceFile = "mochy-falafel.xg",
        Game = 3,
        MoveNumber = 17,
        IsStandardStart = true,
        Comment = "Blitz or prime?",   // default is string.Empty
        Flagged = true,
    };
}
