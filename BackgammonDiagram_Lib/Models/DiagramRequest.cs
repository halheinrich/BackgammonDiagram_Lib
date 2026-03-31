// DiagramRequest.cs  (full replacement — record → class + inner Builder)
namespace BackgammonDiagram_Lib;

public class DiagramRequest
{
    // Private constructor — callers must use Builder.Build()
    private DiagramRequest() { }

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
    public int[] Mop { get; private set; } = new int[26];

    public int OnRollNeeds { get; private set; }
    public int OpponentNeeds { get; private set; }
    public int OnRollPipCount { get; private set; }
    public int OpponentPipCount { get; private set; }
    public string OnRollName { get; private set; } = string.Empty;
    public string OpponentName { get; private set; } = string.Empty;
    public int CubeSize { get; private set; } = 1;
    public CubeOwner CubeOwner { get; private set; }
    public bool IsCube { get; private set; }

    /// <summary>Always length 2. Ignored when IsCube is true.</summary>
    public int[] Dice { get; private set; } = new int[2];

    public DiagramMode Mode { get; private set; }

    /// <summary>Controls which player's home board is on the right.</summary>
    public DiagramOrientation Orientation { get; private set; }

    /// <summary>
    /// True if the on-roll player is shown at the bottom of the diagram.
    /// False if the opponent is shown at the bottom.
    /// </summary>
    public bool OnRollAtBottom { get; private set; } = true;

    public PanelPosition AnalysisPanelPosition { get; private set; }

    // -----------------------------------------------------------------------
    //  Solution — cube fields (IsCube=true, Mode=Solution)
    // -----------------------------------------------------------------------

    /// <summary>Optional slide title for PowerPoint output. Null = no title rendered.</summary>
    public string? Title { get; private set; }
    public double NoDoubleEquity { get; private set; }
    public double DoubleTakeEquity { get; private set; }
    public double WinPctAfterNoDouble { get; private set; }
    public double GammonPctAfterNoDouble { get; private set; }
    public double BgPctAfterNoDouble { get; private set; }
    public double LosePctAfterNoDouble { get; private set; }
    public double LoseGammonPctAfterNoDouble { get; private set; }
    public double LoseBgPctAfterNoDouble { get; private set; }
    public double WinPctAfterDoubleTake { get; private set; }
    public double GammonPctAfterDoubleTake { get; private set; }
    public double BgPctAfterDoubleTake { get; private set; }
    public double LosePctAfterDoubleTake { get; private set; }
    public double LoseGammonPctAfterDoubleTake { get; private set; }
    public double LoseBgPctAfterDoubleTake { get; private set; }
    public double ProbOfOpponentErrorJustifyingDouble { get; private set; }

    // -----------------------------------------------------------------------
    //  Solution — play fields (IsCube=false, Mode=Solution)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Index into Plays identifying the best play.
    /// Drives the crown icon in the analysis panel.
    /// </summary>
    public int BestPlayIndex { get; private set; }

    /// <summary>Index into Plays identifying the user's play. -1 if not applicable.</summary>
    public int UserPlayIndex { get; private set; } = -1;

    public List<PlayCandidate> Plays { get; private set; } = [];
    public List<AnalysisDepthEntry> AnalysisDepths { get; private set; } = [];

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
        public CubeOwner CubeOwner { get; set; }
        public bool IsCube { get; set; }
        public int[] Dice { get; set; } = new int[2];
        public DiagramMode Mode { get; set; }
        public DiagramOrientation Orientation { get; set; }
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
                Mop = Mop,
                OnRollNeeds = OnRollNeeds,
                OpponentNeeds = OpponentNeeds,
                OnRollPipCount = OnRollPipCount,
                OpponentPipCount = OpponentPipCount,
                OnRollName = OnRollName,
                OpponentName = OpponentName,
                CubeSize = CubeSize,
                CubeOwner = CubeOwner,
                IsCube = IsCube,
                Dice = Dice,
                Mode = Mode,
                Orientation = Orientation,
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
                Plays = Plays,
                AnalysisDepths = AnalysisDepths,
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