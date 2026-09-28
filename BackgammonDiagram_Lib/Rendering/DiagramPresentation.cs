using System.Globalization;
using BgDataTypes_Lib;

namespace BackgammonDiagram_Lib.Rendering;

/// <summary>
/// What a diagram draws beyond its checkers and its analysis panel, worded:
/// the title strip's action and source cells, the dice, the cube's face and
/// place, and the player label on each rail. Resolved once per render from a
/// request (<see cref="Of"/>), and read by <see cref="DiagramRenderer.RenderSvg"/>
/// and <see cref="DiagramRenderer.GetHitRegions"/> alike.
/// </summary>
/// <remarks>
/// <para>
/// Two sources, one wording. A decision's presentation is derived from its
/// record, and a board's from the display facts its request states. The
/// score is the one both carry into the same rule: a record's session is
/// read as the <see cref="RailScore"/> a board would state for it
/// (<see cref="ScoreOf"/>), and <see cref="Scored"/> words either — the
/// rails, and the cube's face, double match point and the Crawford game
/// included — so one score cannot draw two ways by the path it came by. A
/// caller never supplies drawn text. The title's cube prompt stays a
/// decision's: it words the decision's kind, which no display fact states.
/// </para>
/// <para>
/// The pip counts are not here: the rails read them off the drawn board,
/// whichever source presents it (see <see cref="DiagramRenderer.RenderSvg"/>).
/// </para>
/// </remarks>
/// <param name="TitleAction">The title strip's first cell — <c>"3-1 to play"</c>, <c>"Cube Action?"</c> — or empty.</param>
/// <param name="TitleSource">The title strip's middle cell — a decision's source file stem, or a board's title — or empty.</param>
/// <param name="Dice">The dice drawn, in order, or <see langword="null"/> for none.</param>
/// <param name="CubeFace">The text on the cube's face.</param>
/// <param name="CubeOwner">Where the cube sits.</param>
/// <param name="OnRollLabel">The player label on the rail of the side positive in the board's frame.</param>
/// <param name="OpponentLabel">The player label on the other side's rail.</param>
internal sealed record DiagramPresentation(
    string TitleAction,
    string TitleSource,
    DiceFaces? Dice,
    string CubeFace,
    CubeOwner CubeOwner,
    string OnRollLabel,
    string OpponentLabel)
{
    /// <summary>The title's action cell for a cube decision.</summary>
    internal const string CubeActionPrompt = "Cube Action?";

    /// <summary>The cube's face at double match point, where the cube is dead.</summary>
    internal const string DoubleMatchPointFace = "Dmp";

    /// <summary>The cube's face in the Crawford game, played without the cube.</summary>
    internal const string CrawfordFace = "Cr";

    /// <summary>The presentation <paramref name="request"/> draws.</summary>
    internal static DiagramPresentation Of(DiagramRequest request) =>
        request.Present(OfDecision, OfFacts);

    /// <summary>
    /// A decision's presentation, from its record: the names and the source
    /// file, the roll (a checker play's, in <paramref name="diceOrder"/>) or
    /// the cube prompt, the cube's face and place, and the rails' score from
    /// the session.
    /// </summary>
    private static DiagramPresentation OfDecision(BgDecisionData decision, DiceOrder diceOrder)
    {
        var dice = decision.Match<DiceFaces?>(
            play => diceOrder == DiceOrder.Reversed
                ? new DiceFaces(play.Decision.Dice[1], play.Decision.Dice[0])
                : new DiceFaces(play.Decision.Dice[0], play.Decision.Dice[1]),
            _ => null);

        var (cubeFace, onRollLabel, opponentLabel) = Scored(
            ScoreOf(decision.Session), decision.Position.CubeSize,
            decision.Descriptive.OnRollName, decision.Descriptive.OpponentName);

        return new DiagramPresentation(
            dice is null ? CubeActionPrompt : RollPrompt(dice),
            StripLastExtension(decision.SourceFile),
            dice,
            cubeFace,
            decision.Position.CubeOwner,
            onRollLabel,
            opponentLabel);
    }

    /// <summary>
    /// A board's presentation, from the display facts its request states:
    /// each drawn as stated, through the same wording a decision's goes
    /// through.
    /// </summary>
    private static DiagramPresentation OfFacts(DisplayFacts facts)
    {
        var (cubeFace, onRollLabel, opponentLabel) = Scored(
            facts.Score, facts.CubeValue, facts.OnRollName, facts.OpponentName);

        return new DiagramPresentation(
            facts.Dice is { } dice ? RollPrompt(dice) : string.Empty,
            facts.Title ?? string.Empty,
            facts.Dice,
            cubeFace,
            facts.CubeOwner,
            onRollLabel,
            opponentLabel);
    }

    /// <summary>
    /// A record's session as the score a board would state for it: a match's
    /// away scores and Crawford status, a money session's Jacoby rule. The
    /// session holds more (a match's length, a money session's scores and
    /// limits); the rails and the cube's face show only these.
    /// </summary>
    private static RailScore ScoreOf(Session session) => session.Match<RailScore>(
        money => new MoneyRailScore(money.Terms.IsJacoby),
        match => new MatchRailScore(match.OnRollNeeds, match.OpponentNeeds, match.IsCrawford));

    // -----------------------------------------------------------------------
    //  The wording — one statement each, for both sources
    // -----------------------------------------------------------------------

    /// <summary>
    /// The cube's face and both rails' player labels for
    /// <paramref name="score"/> — the one rule, whichever source states the
    /// score. A match's cube reads <c>Dmp</c> at double match point (both
    /// sides 1-away, where the cube is dead and a value would mislead), taking
    /// precedence over the Crawford game's <c>Cr</c> (a game played without
    /// the cube); otherwise, and for money or no score, the cube shows its
    /// value. Each rail reads the name and the side's score, where stated.
    /// </summary>
    private static (string CubeFace, string OnRollLabel, string OpponentLabel) Scored(
        RailScore? score, int cubeValue, string? onRollName, string? opponentName) => score switch
    {
        null => (CubeValueFace(cubeValue), PlayerLabel(onRollName, null), PlayerLabel(opponentName, null)),
        MatchRailScore match => (
            match.OnRollNeeds == 1 && match.OpponentNeeds == 1 ? DoubleMatchPointFace
            : match.IsCrawford ? CrawfordFace
            : CubeValueFace(cubeValue),
            PlayerLabel(onRollName, NeedsText(match.OnRollNeeds, match.IsCrawford)),
            PlayerLabel(opponentName, NeedsText(match.OpponentNeeds, match.IsCrawford))),
        MoneyRailScore money => (
            CubeValueFace(cubeValue),
            PlayerLabel(onRollName, MoneyText(money.IsJacoby)),
            PlayerLabel(opponentName, MoneyText(money.IsJacoby))),
        _ => throw new ArgumentOutOfRangeException(nameof(score), score, "Not a rail score this library defines."),
    };

    /// <summary>The title's action cell for dice shown: <c>"3-1 to play"</c>, left die first.</summary>
    private static string RollPrompt(DiceFaces dice) =>
        string.Create(CultureInfo.InvariantCulture, $"{dice.Left}-{dice.Right} to play");

    /// <summary>
    /// The face of a cube of <paramref name="value"/>: its value, except the
    /// starting value 1, which reads <c>64</c> as on a real cube.
    /// </summary>
    private static string CubeValueFace(int value) =>
        value == 1 ? "64" : value.ToString(CultureInfo.InvariantCulture);

    /// <summary>A match score on one rail: <c>"needs 3"</c>, with <c>" Crawford"</c> in the Crawford game.</summary>
    private static string NeedsText(int needs, bool crawford) =>
        string.Create(CultureInfo.InvariantCulture, $"needs {needs}{(crawford ? " Crawford" : string.Empty)}");

    /// <summary>
    /// The money-game label: <c>"(Money Game, Jacoby)"</c> or <c>"(Money Game,
    /// No Jacoby)"</c> with the rule stated, and the bare <c>"(Money Game)"</c>
    /// when none is (a board's source may state none).
    /// </summary>
    private static string MoneyText(bool? isJacoby) => isJacoby switch
    {
        true => "(Money Game, Jacoby)",
        false => "(Money Game, No Jacoby)",
        null => "(Money Game)",
    };

    /// <summary>A rail's player label: the name and the score, each where stated.</summary>
    private static string PlayerLabel(string? name, string? score) =>
        name is null ? score ?? string.Empty
        : score is null ? name
        : $"{name} {score}";

    /// <summary>
    /// Drops the last dot-extension from a file name, preserving any earlier
    /// dots: <c>"abc.xg"</c> → <c>"abc"</c>, <c>"abc.weird.xg"</c> →
    /// <c>"abc.weird"</c>; a leading-dot-only name passes through rather than
    /// degenerating to empty.
    /// </summary>
    private static string StripLastExtension(string filename)
    {
        int dot = filename.LastIndexOf('.');
        return dot > 0 ? filename[..dot] : filename;
    }
}
