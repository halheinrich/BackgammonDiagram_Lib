namespace BackgammonDiagram_Lib;

public class PlayCandidate
{
    /// <summary>Move notation, e.g. "8/5(2) 6/3(2)".</summary>
    public string MoveNotation { get; init; } = string.Empty;

    /// <summary>Primary equity value, displayed top-right in the analysis panel.</summary>
    public double Equity { get; init; }

    /// <summary>
    /// Equity loss relative to best play, stacked below primary equity.
    /// Null for the best play itself.
    /// </summary>
    public double? EquityLoss { get; init; }

    /// <summary>True if this is the move the user played. Renders a green checkmark.</summary>
    public bool IsUserPlay { get; init; }
}
