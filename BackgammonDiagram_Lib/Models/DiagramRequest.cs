// DiagramRequest.cs  (full replacement — record → class + inner Builder)
namespace BackgammonDiagram_Lib;

public class DiagramRequest
{
    // Private constructor — callers must use Builder.Build()
    internal DiagramRequest() { }

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
    public IReadOnlyList<int> Mop { get; init; } = new int[26];
    public int OnRollNeeds { get; init; }
    public int OpponentNeeds { get; init; }
    public int OnRollPipCount { get; init; }
    public int OpponentPipCount { get; init; }
    public string OnRollName { get; init; } = string.Empty;
    public string OpponentName { get; init; } = string.Empty;
    public int CubeSize { get; init; } = 1;
    public CubeOwner CubeOwner { get; init; }
    public bool IsCube { get; init; }

    /// <summary>Always length 2. Ignored when IsCube is true.</summary>
    public IReadOnlyList<int> Dice { get; init; } = new int[2];

    public DiagramMode Mode { get; init; }
    public bool HomeBoardOnRight { get; init; } = true;

    /// <summary>
    /// True if the on-roll player is shown at the bottom of the diagram.
    /// False if the opponent is shown at the bottom.
    /// </summary>
    public bool OnRollAtBottom { get; init; } = true;

    public PanelPosition AnalysisPanelPosition { get; init; }

    // -----------------------------------------------------------------------
    //  Solution — cube fields (IsCube=true, Mode=Solution)
    // -----------------------------------------------------------------------

    /// <summary>Optional slide title for PowerPoint output. Null = no title rendered.</summary>
    public string? Title { get; init; }
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

    public IReadOnlyList<PlayCandidate> Plays { get; init; } = [];
    public IReadOnlyList<AnalysisDepthEntry> AnalysisDepths { get; init; } = [];

    // -----------------------------------------------------------------------
    //  Builder
    // -----------------------------------------------------------------------

    public class Builder
    {
        public int[] Mop { get; set; } = new int[26];
        public int OnRollNeeds { get; set; }
        public int OpponentNeeds { get; set; }
        public int OnRollPipCount { get; set; }
        public int OpponentPipCount { get; set; }
        public string OnRollName { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
        public int CubeSize { get; set; } = 1;
        public CubeOwner CubeOwner { get; set; } = CubeOwner.Centered;
        public bool IsCube { get; set; }
        public int[] Dice { get; set; } = new int[2];
        public DiagramMode Mode { get; set; }
        public bool HomeBoardOnRight { get; set; } = true;
        public bool OnRollAtBottom { get; set; } = true;
        public PanelPosition AnalysisPanelPosition { get; set; }
        public string? Title { get; set; }
        public double NoDoubleEquity { get; set; }
        public double DoubleTakeEquity { get; set; }
        public double WinPctAfterNoDouble { get; set; }
        public double GammonPctAfterNoDouble { get; set; }
        public double BgPctAfterNoDouble { get; set; }
        public double LosePctAfterNoDouble { get; set; }
        public double LoseGammonPctAfterNoDouble { get; set; }
        public double LoseBgPctAfterNoDouble { get; set; }
        public double WinPctAfterDoubleTake { get; set; }
        public double GammonPctAfterDoubleTake { get; set; }
        public double BgPctAfterDoubleTake { get; set; }
        public double LosePctAfterDoubleTake { get; set; }
        public double LoseGammonPctAfterDoubleTake { get; set; }
        public double LoseBgPctAfterDoubleTake { get; set; }
        public double ProbOfOpponentErrorJustifyingDouble { get; set; }
        public int BestPlayIndex { get; set; }
        public int UserPlayIndex { get; set; } = -1;
        public List<PlayCandidate> Plays { get; set; } = [];
        public List<AnalysisDepthEntry> AnalysisDepths { get; set; } = [];

        public DiagramRequest Build()
        {
            Validate();
            return new DiagramRequest
            {
                Mop = Mop.ToArray(),
                OnRollNeeds = OnRollNeeds,
                OpponentNeeds = OpponentNeeds,
                OnRollPipCount = OnRollPipCount,
                OpponentPipCount = OpponentPipCount,
                OnRollName = OnRollName,
                OpponentName = OpponentName,
                CubeSize = CubeSize,
                CubeOwner = CubeOwner,
                IsCube = IsCube,
                Dice = Dice.ToArray(),
                Mode = Mode,
                HomeBoardOnRight = HomeBoardOnRight,
                OnRollAtBottom = OnRollAtBottom,
                AnalysisPanelPosition = AnalysisPanelPosition,
                Title = Title,
                NoDoubleEquity = NoDoubleEquity,
                DoubleTakeEquity = DoubleTakeEquity,
                WinPctAfterNoDouble = WinPctAfterNoDouble,
                GammonPctAfterNoDouble = GammonPctAfterNoDouble,
                BgPctAfterNoDouble = BgPctAfterNoDouble,
                LosePctAfterNoDouble = LosePctAfterNoDouble,
                LoseGammonPctAfterNoDouble = LoseGammonPctAfterNoDouble,
                LoseBgPctAfterNoDouble = LoseBgPctAfterNoDouble,
                WinPctAfterDoubleTake = WinPctAfterDoubleTake,
                GammonPctAfterDoubleTake = GammonPctAfterDoubleTake,
                BgPctAfterDoubleTake = BgPctAfterDoubleTake,
                LosePctAfterDoubleTake = LosePctAfterDoubleTake,
                LoseGammonPctAfterDoubleTake = LoseGammonPctAfterDoubleTake,
                LoseBgPctAfterDoubleTake = LoseBgPctAfterDoubleTake,
                ProbOfOpponentErrorJustifyingDouble = ProbOfOpponentErrorJustifyingDouble,
                BestPlayIndex = BestPlayIndex,
                UserPlayIndex = UserPlayIndex,
                Plays = new List<PlayCandidate>(Plays),
                AnalysisDepths = new List<AnalysisDepthEntry>(AnalysisDepths),
            };
        }

        private void Validate()
        {
            if (Mop == null || Mop.Length != 26)
                throw new InvalidOperationException("Mop must be a 26-element array.");

            if (Dice == null || Dice.Length != 2)
                throw new InvalidOperationException("Dice must be a 2-element array.");

            if (IsCube)
            {
                if (Dice[0] != 0 || Dice[1] != 0)
                    throw new InvalidOperationException("When IsCube is true, Dice must be [0, 0].");
            }
            else
            {
                if (Dice[0] < 1 || Dice[0] > 6 || Dice[1] < 1 || Dice[1] > 6)
                    throw new InvalidOperationException("When IsCube is false, each die value must be 1–6.");
            }

            if (!IsPowerOfTwo(CubeSize) || CubeSize < 1 || CubeSize > 4096)
                throw new InvalidOperationException("CubeSize must be a power of 2 from 1 to 4096.");
        }

        private static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;
    }
}