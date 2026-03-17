namespace BackgammonDiagram_Lib;

public class DiagramRequest
{
    // -----------------------------------------------------------------------
    //  Always required
    // -----------------------------------------------------------------------

    /// <summary>
    /// Men on Point — 26-element board array.
    /// [0]    = opponent's bar  (value always &lt;= 0)
    /// [1-24] = points 1-24 from on-roll player's perspective
    /// [25]   = on-roll player's bar (value always &gt;= 0)
    /// Positive = on-roll player's checkers; negative = opponent's.
    /// </summary>
    public int[] Mop { get; init; } = new int[26];

    public int OnRollNeeds { get; init; }
    public int OpponentNeeds { get; init; }
    public int OnRollPipCount { get; init; }
    public int OpponentPipCount { get; init; }
    public string OnRollName { get; init; } = string.Empty;
    public string OpponentName { get; init; } = string.Empty;
    public int CubeSize { get; init; }
    public CubeOwner CubeOwner { get; init; }
    public bool IsCube { get; init; }

    /// <summary>Always length 2. Ignored when IsCube is true.</summary>
    public int[] Dice { get; init; } = new int[2];

    public DiagramMode Mode { get; init; }
    public DiagramOrientation Orientation { get; init; }
    public PanelPosition AnalysisPanelPosition { get; init; }

    // -----------------------------------------------------------------------
    //  Solution — cube fields (IsCube=true, Mode=Solution)
    // -----------------------------------------------------------------------

    public double NoDoubleEquity { get; init; }
    public double DoubleTakeEquity { get; init; }
    public double WinPctAfterNoDouble { get; init; }
    public double GammonPctAfterNoDouble { get; init; }
    public double BgPctAfterNoDouble { get; init; }
    public double LosePctAfterNoDouble { get; init; }
    public double LoseGammonPctAfterNoDouble { get; init; }
    public double LoseBgPctAfterNoDouble { get; init; }
    public double WinPctAfterDoubleTake { get; init; }
    public double GammonPctAfterDoubleTake { get; init; }
    public double BgPctAfterDoubleTake { get; init; }
    public double LosePctAfterDoubleTake { get; init; }
    public double LoseGammonPctAfterDoubleTake { get; init; }
    public double LoseBgPctAfterDoubleTake { get; init; }
    public double ProbOfOpponentErrorJustifyingDouble { get; init; }

    // -----------------------------------------------------------------------
    //  Solution — play fields (IsCube=false, Mode=Solution)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Index into Plays identifying the best play.
    /// Drives the crown icon in the analysis panel.
    /// </summary>
    public int BestPlayIndex { get; init; }

    /// <summary>Index into Plays identifying the user's play. -1 if not applicable.</summary>
    public int UserPlayIndex { get; init; } = -1;

    public List<PlayCandidate> Plays { get; init; } = [];
    public List<AnalysisDepthEntry> AnalysisDepths { get; init; } = [];
}
