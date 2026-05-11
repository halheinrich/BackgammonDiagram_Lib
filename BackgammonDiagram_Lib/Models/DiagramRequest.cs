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

    /// <summary>
    /// True when the analysis panel sits to the left of the board. Single
    /// declaration site for this derivation — both <c>RenderSvg</c> and
    /// <c>GetHitRegions</c> read it so the two functions share one
    /// coordinate-system rule and cannot drift.
    /// </summary>
    public bool PanelOnLeft => AnalysisPanelPosition == PanelPosition.Left;

    /// <summary>
    /// Optional counter surfaced right-justified in the title strip as
    /// "Position {N}". Callers emitting a deck of decisions typically set
    /// this to a running 1-based counter so readers can cross-reference
    /// the slide back to a source list. Null hides the right title cell.
    /// </summary>
    public int? PositionNumber { get; init; }

    // -----------------------------------------------------------------------
    //  Factory: BgDecisionData → DiagramRequest
    // -----------------------------------------------------------------------

    /// <summary>
    /// Convenience entry point for the <see cref="BgDecisionData"/> →
    /// <see cref="DiagramRequest"/> mapping, taking the renderer-specific
    /// parameters that aren't carried by the data layer (display mode,
    /// board orientation, panel side). Delegates to <see cref="Builder.From"/>
    /// which holds the field-by-field copy logic; callers (tests, Blazor
    /// apps, PPTX exporters) should use this entry point rather than
    /// open-coding the mapping.
    /// </summary>
    public static DiagramRequest FromDecisionData(
        BgDecisionData data,
        DiagramMode mode = DiagramMode.Solution,
        bool homeBoardOnRight = true,
        bool onRollAtBottom = true,
        PanelPosition analysisPanelPosition = PanelPosition.Left)
    {
        return Builder.From(data.Position, data.Decision, data.Descriptive,
            mode, homeBoardOnRight, onRollAtBottom, analysisPanelPosition).Build();
    }

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
        public string CubeDepth { get; set; } = string.Empty;
        public string CubeDepthAbbreviation { get; set; } = string.Empty;
        public int CubeDepthRank { get; set; }
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
        public string? SourceFile { get; set; }

        // Renderer-specific
        public DiagramMode Mode { get; set; }
        public bool HomeBoardOnRight { get; set; } = true;
        public bool OnRollAtBottom { get; set; } = true;
        public PanelPosition AnalysisPanelPosition { get; set; }
        public int? PositionNumber { get; set; }

        // -------------------------------------------------------------------
        //  Factories — single field-mapping site for data → builder
        // -------------------------------------------------------------------

        /// <summary>
        /// Starts a Builder pre-populated from the three BgDataTypes_Lib
        /// records plus renderer-specific parameters. Single field-mapping
        /// site for the data-layer → rendering-layer copy, shared by
        /// <see cref="FromDecisionData"/> and
        /// <see cref="DiagramRequestExtensions.ToProblemSolutionPair"/> —
        /// adding a new DecisionData/PositionData field only requires one
        /// edit here.
        /// </summary>
        public static Builder From(
            PositionData position,
            DecisionData decision,
            DescriptiveData descriptive,
            DiagramMode mode = DiagramMode.Solution,
            bool homeBoardOnRight = true,
            bool onRollAtBottom = true,
            PanelPosition analysisPanelPosition = PanelPosition.Left)
        {
            return new Builder
            {
                // Position
                Mop = position.Mop.ToArray(),
                OnRollNeeds = position.OnRollNeeds,
                OpponentNeeds = position.OpponentNeeds,
                OnRollPipCount = position.OnRollPipCount,
                OpponentPipCount = position.OpponentPipCount,
                CubeSize = position.CubeSize,
                CubeOwner = position.CubeOwner,
                IsCrawford = position.IsCrawford,

                // Decision
                IsCube = decision.IsCube,
                Dice = decision.Dice.ToArray(),
                Plays = new List<PlayCandidate>(decision.Plays),
                CubeDepth = decision.CubeDepth,
                CubeDepthAbbreviation = decision.CubeDepthAbbreviation,
                CubeDepthRank = decision.CubeDepthRank,
                BestPlayIndex = decision.BestPlayIndex,
                UserPlayIndex = decision.UserPlayIndex,
                NoDoubleEquity = decision.NoDoubleEquity,
                DoubleTakeEquity = decision.DoubleTakeEquity,
                WinPctAfterNoDouble = decision.WinPctAfterNoDouble,
                GammonPctAfterNoDouble = decision.GammonPctAfterNoDouble,
                BgPctAfterNoDouble = decision.BgPctAfterNoDouble,
                LosePctAfterNoDouble = decision.LosePctAfterNoDouble,
                LoseGammonPctAfterNoDouble = decision.LoseGammonPctAfterNoDouble,
                LoseBgPctAfterNoDouble = decision.LoseBgPctAfterNoDouble,
                WinPctAfterDoubleTake = decision.WinPctAfterDoubleTake,
                GammonPctAfterDoubleTake = decision.GammonPctAfterDoubleTake,
                BgPctAfterDoubleTake = decision.BgPctAfterDoubleTake,
                LosePctAfterDoubleTake = decision.LosePctAfterDoubleTake,
                LoseGammonPctAfterDoubleTake = decision.LoseGammonPctAfterDoubleTake,
                LoseBgPctAfterDoubleTake = decision.LoseBgPctAfterDoubleTake,
                ProbOfOpponentErrorJustifyingDouble = decision.ProbOfOpponentErrorJustifyingDouble,
                UserDoubleError = decision.UserDoubleError,
                UserTakeError = decision.UserTakeError,

                // Descriptive
                OnRollName = descriptive.OnRollName,
                OpponentName = descriptive.OpponentName,
                Title = descriptive.Title,
                MatchLength = descriptive.MatchLength,
                Date = descriptive.Date,
                Event = descriptive.Event,
                SourceFile = descriptive.SourceFile,

                // Renderer-specific
                Mode = mode,
                HomeBoardOnRight = homeBoardOnRight,
                OnRollAtBottom = onRollAtBottom,
                AnalysisPanelPosition = analysisPanelPosition,
            };
        }

        /// <summary>
        /// Starts a Builder pre-populated from an existing DiagramRequest.
        /// Forwards to the three-record overload so both factories share
        /// one field-mapping site — enables ToProblemSolutionPair and any
        /// other "tweak a request" caller to be drift-free.
        /// </summary>
        public static Builder From(DiagramRequest existing)
        {
            var b = From(existing.Position, existing.Decision, existing.Descriptive,
                existing.Mode, existing.HomeBoardOnRight, existing.OnRollAtBottom,
                existing.AnalysisPanelPosition);
            b.PositionNumber = existing.PositionNumber;
            return b;
        }

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
                    CubeDepth = CubeDepth,
                    CubeDepthAbbreviation = CubeDepthAbbreviation,
                    CubeDepthRank = CubeDepthRank,
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
                    SourceFile = SourceFile,
                },
                Mode = Mode,
                HomeBoardOnRight = HomeBoardOnRight,
                OnRollAtBottom = OnRollAtBottom,
                AnalysisPanelPosition = AnalysisPanelPosition,
                PositionNumber = PositionNumber,
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