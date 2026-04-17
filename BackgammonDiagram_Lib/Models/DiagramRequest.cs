using BgDataTypes_Lib;

namespace BackgammonDiagram_Lib;

public class DiagramRequest
{
    internal DiagramRequest() { }

    public PositionData Position { get; init; } = new();
    public DecisionData Decision { get; init; } = new();
    public DescriptiveData Descriptive { get; init; } = new();

    // Renderer-specific
    public DiagramMode Mode { get; init; }
    public bool HomeBoardOnRight { get; init; } = true;
    public bool OnRollAtBottom { get; init; } = true;
    public PanelPosition AnalysisPanelPosition { get; init; }

    // -----------------------------------------------------------------------
    //  Builder
    // -----------------------------------------------------------------------

    public class Builder
    {
        // Position
        public int[] Mop { get; set; } = new int[26];
        public int OnRollNeeds { get; set; }
        public int OpponentNeeds { get; set; }
        public int OnRollPipCount { get; set; }
        public int OpponentPipCount { get; set; }
        public int CubeSize { get; set; } = 1;
        public CubeOwner CubeOwner { get; set; } = CubeOwner.Centered;
        public bool IsCrawford { get; set; }

        // Decision
        public bool IsCube { get; set; }
        public int[] Dice { get; set; } = new int[2];
        public List<PlayCandidate> Plays { get; set; } = [];
        public List<AnalysisDepthEntry> AnalysisDepths { get; set; } = [];
        public int BestPlayIndex { get; set; }
        public int UserPlayIndex { get; set; } = -1;
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

        /// <summary>
        /// Equity loss from the user's double / no-double decision vs. the
        /// correct cube action (>= 0). Null when the user did not face a cube
        /// decision or no data is recorded. Drives the "Actual" banner row.
        /// </summary>
        public double? UserDoubleError { get; set; }

        /// <summary>
        /// Equity loss from the user's take / pass decision vs. the correct
        /// response (>= 0). Null when the user did not face a take decision
        /// or no data is recorded. Drives the "Actual" banner row.
        /// </summary>
        public double? UserTakeError { get; set; }

        // Descriptive
        public string OnRollName { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public int MatchLength { get; set; }
        public DateOnly? Date { get; set; }
        public string? Event { get; set; }

        // Renderer-specific
        public DiagramMode Mode { get; set; }
        public bool HomeBoardOnRight { get; set; } = true;
        public bool OnRollAtBottom { get; set; } = true;
        public PanelPosition AnalysisPanelPosition { get; set; }

        public DiagramRequest Build()
        {
            Validate();
            return new DiagramRequest
            {
                Position = new PositionData
                {
                    Mop = Mop.ToArray(),
                    OnRollNeeds = OnRollNeeds,
                    OpponentNeeds = OpponentNeeds,
                    OnRollPipCount = OnRollPipCount,
                    OpponentPipCount = OpponentPipCount,
                    CubeSize = CubeSize,
                    CubeOwner = CubeOwner,
                    IsCrawford = IsCrawford,
                },
                Decision = new DecisionData
                {
                    IsCube = IsCube,
                    Dice = Dice.ToArray(),
                    Plays = new List<PlayCandidate>(Plays),
                    AnalysisDepths = new List<AnalysisDepthEntry>(AnalysisDepths),
                    BestPlayIndex = BestPlayIndex,
                    UserPlayIndex = UserPlayIndex,
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
                    UserDoubleError = UserDoubleError,
                    UserTakeError = UserTakeError,
                },
                Descriptive = new DescriptiveData
                {
                    OnRollName = OnRollName,
                    OpponentName = OpponentName,
                    Title = Title,
                    MatchLength = MatchLength,
                    Date = Date,
                    Event = Event,
                },
                Mode = Mode,
                HomeBoardOnRight = HomeBoardOnRight,
                OnRollAtBottom = OnRollAtBottom,
                AnalysisPanelPosition = AnalysisPanelPosition,
            };
        }

        private void Validate()
        {
            if (Mop.Length != 26)
                throw new InvalidOperationException("Mop must be a 26-element array.");
            if (Dice.Length != 2)
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
            if (!MathUtils.IsPowerOfTwo(CubeSize) || CubeSize < 1 || CubeSize > 4096)
                throw new InvalidOperationException("CubeSize must be a power of 2 from 1 to 4096.");
        }
    }
}