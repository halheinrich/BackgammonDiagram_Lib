using BgDataTypes_Lib;

namespace BackgammonDiagram_Lib;

/// <summary>
/// The display facts of a board that is not a decision's, drawn through
/// <see cref="DiagramRequest.ForBoard"/>: what the diagram draws and prints
/// for it beyond its checkers — the names on the rails, the title, the dice
/// shown and their order, the cube's value and where it sits, and the score
/// on the rails.
/// </summary>
/// <remarks>
/// <para>
/// <b>Presentation-owned facts, not copies of a decision's</b> (Hal's rulings
/// of 2026-09-28 on halheinrich/backgammon#273). A board's presentation may
/// carry the domain facts it needs to reproduce that presentation, and no
/// more: the score's Crawford status for a match and Jacoby rule for money
/// (<see cref="RailScore"/>). Nothing here is a session, a decision kind, a
/// cube's legality, an analysis, a candidate, an error or a ranking — so
/// <c>"Cube Action?"</c>, which words a decision's kind, is never a board's.
/// The diagram consumes these facts and neither derives nor validates their
/// domain legality: it validates only what it needs to draw each one. A
/// cube's limit, a legal standing or a legal roll is the domain's rule, not
/// the diagram's — so the cube shows the value it is given and the rails the
/// score they are given, a match won 0-away and a Crawford status at any
/// score included.
/// </para>
/// <para>
/// <b>The diagram words them, as it words a decision's.</b> A caller states a
/// value and the diagram draws it by the rule a decision's goes through — the
/// cube's starting value 1 reads <c>64</c>, the dice shown become the title's
/// <c>"3-1 to play"</c>, a <see cref="MatchRailScore"/> reads
/// <c>"{name} needs {n}"</c> with <c>" Crawford"</c> and a <c>Cr</c> cube in
/// the Crawford game, and double match point reads <c>Dmp</c> on the cube,
/// worded from the away scores rather than stated — so one score draws one
/// way on either path, and a caller never formats text the diagram draws.
/// </para>
/// <para>
/// <b>What is not here.</b> The board is the request's. The orientation, the
/// panel side and the position number are the request's options, the same
/// for every request. The pip counts are read off the board by
/// <c>BgDataTypes_Lib</c>'s one pip rule, never stated. And a decision's own
/// presentation is never restated as display facts: a decision is drawn from
/// its record (<see cref="DiagramRequest.ForDecision"/>), and a working board
/// during its entry keeps it (<see cref="DiagramRequest.WithWorkingBoard"/>).
/// </para>
/// <para>
/// <b>None is <see langword="null"/>.</b> A text member states text or
/// nothing: empty or white-space text is refused. <c>new DisplayFacts()</c>
/// is a bare board — no names, no title, no dice, the cube centred at its
/// start, no score. Value equality; <c>with</c> re-validates what it sets.
/// </para>
/// </remarks>
public sealed record DisplayFacts
{
    private readonly string? _onRollName;
    private readonly string? _opponentName;
    private readonly string? _title;
    private readonly CubeOwner _cubeOwner = CubeOwner.Centered;

    /// <summary>
    /// The name on the rail of the side whose checkers are positive in the
    /// board's frame — the side <see cref="BoardPosition"/> calls the player
    /// on roll. <see langword="null"/> for none; never empty.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is empty or white space.</exception>
    public string? OnRollName
    {
        get => _onRollName;
        init => _onRollName = Stated(value, nameof(OnRollName));
    }

    /// <summary>The name on the other side's rail. <see langword="null"/> for none; never empty.</summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is empty or white space.</exception>
    public string? OpponentName
    {
        get => _opponentName;
        init => _opponentName = Stated(value, nameof(OpponentName));
    }

    /// <summary>
    /// The title, drawn verbatim in the title strip's middle cell — where a
    /// decision's diagram draws its source file's name.
    /// <see langword="null"/> for none; never empty.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is empty or white space.</exception>
    public string? Title
    {
        get => _title;
        init => _title = Stated(value, nameof(Title));
    }

    /// <summary>
    /// The dice shown, in the order shown, or <see langword="null"/> for no
    /// dice. Dice shown also head the title strip, <c>"{left}-{right} to
    /// play"</c>, as a checker play's do.
    /// </summary>
    public DiceFaces? Dice { get; init; }

    /// <summary>
    /// The cube's value, drawn on its face; the starting value 1 reads
    /// <c>64</c>, as on a real cube. Drawn as given: whether a value is a
    /// cube's legal value is the domain's rule. Defaults to 1.
    /// </summary>
    public int CubeValue { get; init; } = 1;

    /// <summary>
    /// Where the cube sits: in the middle of the rail, or turned to a side's
    /// end of it. Defaults to <see cref="CubeOwner.Centered"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is not a defined <see cref="BgDataTypes_Lib.CubeOwner"/>: there is no place to draw it.</exception>
    public CubeOwner CubeOwner
    {
        get => _cubeOwner;
        init => _cubeOwner = Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(CubeOwner), value, "The cube is drawn in the middle or at a side's end of the rail.");
    }

    /// <summary>
    /// The score the rails show beside the names, or <see langword="null"/>
    /// for none — the rails then show the names alone.
    /// </summary>
    public RailScore? Score { get; init; }

    /// <summary><paramref name="value"/>, when it states text or nothing.</summary>
    private static string? Stated(string? value, string member) =>
        value is not null && string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Display text states text or, for none, null; never empty text.", member)
            : value;
}
