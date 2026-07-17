using BgDataTypes_Lib;

namespace BackgammonDiagram_Lib;

/// <summary>
/// An immutable, validated rendering request: the board/match state plus the
/// renderer-specific display options a diagram needs. Constructed through the
/// nested <see cref="Builder"/> (directly, or via <see cref="FromDecisionData"/>
/// / <see cref="Builder.From(DiagramRequest)"/>); there is no public
/// constructor, so a <see cref="DiagramRequest"/> in hand is always one that
/// passed <see cref="Builder.Build"/>'s validation. The three data records are
/// defensively copied at build time, so a built request cannot be mutated by
/// later changes to the caller's arrays or lists.
/// </summary>
public class DiagramRequest
{
    internal DiagramRequest() { }

    /// <summary>The board and match state (checkers, cube, scores) to render.</summary>
    public PositionData Position { get; init; } = new();

    /// <summary>The decision data (dice, plays, or cube-equity analysis) to render.</summary>
    public DecisionData Decision { get; init; } = new();

    /// <summary>Descriptive metadata (player names, source file, match length) to render.</summary>
    public DescriptiveData Descriptive { get; init; } = new();

    // Renderer-specific

    /// <summary>
    /// Whether to render board-only (<see cref="DiagramMode.Problem"/>) or the
    /// board plus analysis panel (<see cref="DiagramMode.Solution"/>). Both
    /// modes share identical overall dimensions — the panel region is always
    /// allocated — so swapping modes never reflows surrounding content.
    /// </summary>
    public DiagramMode Mode { get; init; }

    /// <summary>
    /// When <c>true</c> (the default), the on-roll player's home board is drawn
    /// on the right. Purely geometric: the board array is never flipped, only
    /// <c>ColumnCentreX</c> mirrors, so anything indexing <c>Points</c> uses the
    /// unflipped convention regardless of this flag.
    /// </summary>
    public bool HomeBoardOnRight { get; init; } = true;

    /// <summary>
    /// When <c>true</c> (the default), the on-roll half of the board is drawn
    /// at the bottom. Controls only the vertical orientation of the two halves.
    /// </summary>
    public bool OnRollAtBottom { get; init; } = true;

    /// <summary>
    /// Which side of the board the Solution-mode analysis panel occupies.
    /// Read through the derived <see cref="PanelOnLeft"/> so both
    /// <c>RenderSvg</c> and <c>GetHitRegions</c> share one coordinate rule.
    /// </summary>
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

    /// <summary>
    /// The position's XGID (eXtreme Gammon ID) — the canonical string encoding
    /// of the board, cube, and match state. Surfaced top-level on
    /// <see cref="BgDecisionData"/> (not in the three data records), so it is
    /// carried here directly rather than through the record-based mapping.
    /// Consumed by the export formats as an upper-right label: real selectable
    /// text in PDF/PPTX, and — when <see cref="DiagramOptions.ShowXgid"/> is
    /// set — baked pixels in the rendered SVG/PNG. Defaults to empty (no label).
    /// </summary>
    public string Xgid { get; init; } = string.Empty;

    /// <summary>
    /// Optional index of a <em>second</em> play to mark in the solution play
    /// panel, rendered with a bold † to set it apart from the primary play
    /// (marked with a bold * at <see cref="BgDataTypes_Lib.DecisionData.UserPlayIndex"/>).
    /// This lets a quiz solution view show the originally-played move (*) and
    /// the user's differing answer (†) side by side.
    /// <para>
    /// It is a <b>render-only overlay the consumer sets directly</b>, not a
    /// value sourced from <see cref="BgDecisionData"/> —  the "second play" is
    /// a consumer concept (e.g. a quiz answer), not an .xg-derived field. The
    /// data-sourcing factories (<see cref="FromDecisionData"/> and the
    /// three-record <c>Builder.From</c>) leave it at the default −1, so every
    /// export path — which sets only the primary index — is visually
    /// unchanged. Only <c>Builder.From(DiagramRequest)</c> carries it across,
    /// so round-tripping a request (e.g. <c>ToProblemSolutionPair</c>)
    /// preserves the overlay.
    /// </para>
    /// <para>
    /// The renderer draws † only when <c>SecondaryPlayIndex &gt;= 0</c> and it
    /// differs from <see cref="BgDataTypes_Lib.DecisionData.UserPlayIndex"/>; a
    /// secondary that coincides with the primary collapses to a single *. The
    /// producer owns this "don't double-mark a row" suppression, so the
    /// consumer can pass both indices blindly. Both marked rows are always kept
    /// visible: one ranking beyond the panel's visible window is rescued into
    /// view with its real rank, exactly as the primary play already is.
    /// Defaults to −1 (no secondary mark).
    /// </para>
    /// </summary>
    public int SecondaryPlayIndex { get; init; } = -1;

    // -----------------------------------------------------------------------
    //  Factory: BgDecisionData → DiagramRequest
    // -----------------------------------------------------------------------

    /// <summary>
    /// Convenience entry point for the <see cref="BgDecisionData"/> →
    /// <see cref="DiagramRequest"/> mapping, taking the renderer-specific
    /// parameters that aren't carried by the data layer (display mode,
    /// board orientation, panel side). Delegates to
    /// <see cref="Builder.From(PositionData, DecisionData, DescriptiveData, DiagramMode, bool, bool, PanelPosition)"/>
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
        var b = Builder.From(data.Position, data.Decision, data.Descriptive,
            mode, homeBoardOnRight, onRollAtBottom, analysisPanelPosition);
        b.Xgid = data.Xgid;
        return b.Build();
    }

    // -----------------------------------------------------------------------
    //  Builder
    // -----------------------------------------------------------------------

    /// <summary>
    /// Mutable builder for a <see cref="DiagramRequest"/>. Callers set flat
    /// fields — the position, decision, and descriptive values mirror the
    /// <c>BgDataTypes_Lib</c> records field-for-field — and <see cref="Build"/>
    /// assembles the nested records, defensively copying <see cref="Mop"/>,
    /// <see cref="Dice"/>, and <see cref="Plays"/>, then validates. The
    /// data-mirroring fields inherit their documentation from the corresponding
    /// record members, keeping their meaning single-sourced in the data layer.
    /// </summary>
    public class Builder
    {
        // Position
        /// <inheritdoc cref="PositionData.Mop"/>
        public int[] Mop { get; set; } = new int[26];
        /// <inheritdoc cref="PositionData.OnRollNeeds"/>
        public int OnRollNeeds { get; set; }
        /// <inheritdoc cref="PositionData.OpponentNeeds"/>
        public int OpponentNeeds { get; set; }
        /// <inheritdoc cref="PositionData.OnRollPipCount"/>
        public int OnRollPipCount { get; set; }
        /// <inheritdoc cref="PositionData.OpponentPipCount"/>
        public int OpponentPipCount { get; set; }
        /// <inheritdoc cref="PositionData.CubeSize"/>
        public int CubeSize { get; set; } = 1;
        /// <inheritdoc cref="PositionData.CubeOwner"/>
        public CubeOwner CubeOwner { get; set; } = CubeOwner.Centered;
        /// <inheritdoc cref="PositionData.IsCrawford"/>
        public bool IsCrawford { get; set; }

        // Decision
        /// <inheritdoc cref="DecisionData.IsCube"/>
        public bool IsCube { get; set; }
        /// <inheritdoc cref="DecisionData.Dice"/>
        public int[] Dice { get; set; } = new int[2];
        /// <inheritdoc cref="DecisionData.Plays"/>
        public List<PlayCandidate> Plays { get; set; } = [];
        /// <inheritdoc cref="DecisionData.CubeDepth"/>
        public string CubeDepth { get; set; } = string.Empty;
        /// <inheritdoc cref="DecisionData.CubeDepthAbbreviation"/>
        public string CubeDepthAbbreviation { get; set; } = string.Empty;
        /// <inheritdoc cref="DecisionData.CubeDepthRank"/>
        public int CubeDepthRank { get; set; }
        /// <inheritdoc cref="DecisionData.BestPlayIndex"/>
        public int BestPlayIndex { get; set; }
        /// <inheritdoc cref="DecisionData.UserPlayIndex"/>
        public int UserPlayIndex { get; set; } = -1;
        /// <inheritdoc cref="DecisionData.NoDoubleEquity"/>
        public double NoDoubleEquity { get; set; }
        /// <inheritdoc cref="DecisionData.DoubleTakeEquity"/>
        public double DoubleTakeEquity { get; set; }
        /// <inheritdoc cref="DecisionData.WinPctAfterNoDouble"/>
        public double WinPctAfterNoDouble { get; set; }
        /// <inheritdoc cref="DecisionData.GammonPctAfterNoDouble"/>
        public double GammonPctAfterNoDouble { get; set; }
        /// <inheritdoc cref="DecisionData.BgPctAfterNoDouble"/>
        public double BgPctAfterNoDouble { get; set; }
        /// <inheritdoc cref="DecisionData.LosePctAfterNoDouble"/>
        public double LosePctAfterNoDouble { get; set; }
        /// <inheritdoc cref="DecisionData.LoseGammonPctAfterNoDouble"/>
        public double LoseGammonPctAfterNoDouble { get; set; }
        /// <inheritdoc cref="DecisionData.LoseBgPctAfterNoDouble"/>
        public double LoseBgPctAfterNoDouble { get; set; }
        /// <inheritdoc cref="DecisionData.WinPctAfterDoubleTake"/>
        public double WinPctAfterDoubleTake { get; set; }
        /// <inheritdoc cref="DecisionData.GammonPctAfterDoubleTake"/>
        public double GammonPctAfterDoubleTake { get; set; }
        /// <inheritdoc cref="DecisionData.BgPctAfterDoubleTake"/>
        public double BgPctAfterDoubleTake { get; set; }
        /// <inheritdoc cref="DecisionData.LosePctAfterDoubleTake"/>
        public double LosePctAfterDoubleTake { get; set; }
        /// <inheritdoc cref="DecisionData.LoseGammonPctAfterDoubleTake"/>
        public double LoseGammonPctAfterDoubleTake { get; set; }
        /// <inheritdoc cref="DecisionData.LoseBgPctAfterDoubleTake"/>
        public double LoseBgPctAfterDoubleTake { get; set; }
        /// <inheritdoc cref="DecisionData.ProbOfOpponentErrorJustifyingDouble"/>
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
        /// <inheritdoc cref="DescriptiveData.OnRollName"/>
        public string OnRollName { get; set; } = string.Empty;
        /// <inheritdoc cref="DescriptiveData.OpponentName"/>
        public string OpponentName { get; set; } = string.Empty;
        /// <inheritdoc cref="DescriptiveData.Title"/>
        public string? Title { get; set; }
        /// <inheritdoc cref="DescriptiveData.MatchLength"/>
        public int MatchLength { get; set; }
        /// <inheritdoc cref="DescriptiveData.Date"/>
        public DateOnly? Date { get; set; }
        /// <inheritdoc cref="DescriptiveData.Event"/>
        public string? Event { get; set; }
        /// <inheritdoc cref="DescriptiveData.SourceFile"/>
        public string? SourceFile { get; set; }

        // Renderer-specific
        /// <inheritdoc cref="DiagramRequest.Mode"/>
        public DiagramMode Mode { get; set; }
        /// <inheritdoc cref="DiagramRequest.HomeBoardOnRight"/>
        public bool HomeBoardOnRight { get; set; } = true;
        /// <inheritdoc cref="DiagramRequest.OnRollAtBottom"/>
        public bool OnRollAtBottom { get; set; } = true;
        /// <inheritdoc cref="DiagramRequest.AnalysisPanelPosition"/>
        public PanelPosition AnalysisPanelPosition { get; set; }
        /// <inheritdoc cref="DiagramRequest.PositionNumber"/>
        public int? PositionNumber { get; set; }
        /// <inheritdoc cref="DiagramRequest.Xgid"/>
        public string Xgid { get; set; } = string.Empty;

        /// <summary>
        /// Render-only overlay: index of the second play to mark with † in the
        /// solution play panel. See <see cref="DiagramRequest.SecondaryPlayIndex"/>.
        /// Sits with the renderer-specific fields (not the Decision block)
        /// because it is set by the consumer, not copied from
        /// <see cref="DecisionData"/>. Defaults to −1 (no secondary mark).
        /// </summary>
        public int SecondaryPlayIndex { get; set; } = -1;

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
            b.Xgid = existing.Xgid;
            // Render-only overlay — not carried by the three-record From, so
            // copy it here to keep this "reproduce a request" factory faithful.
            b.SecondaryPlayIndex = existing.SecondaryPlayIndex;
            return b;
        }

        /// <summary>
        /// Validates the accumulated fields and constructs the immutable
        /// <see cref="DiagramRequest"/>, defensively copying <see cref="Mop"/>,
        /// <see cref="Dice"/>, and <see cref="Plays"/> into the nested records.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation fails: <see cref="Mop"/> is not length 26,
        /// <see cref="Dice"/> is not length 2, a cube decision does not carry
        /// <c>[0, 0]</c> dice (or a checker decision carries an out-of-range
        /// die), or <see cref="CubeSize"/> is not a power of two in 1..4096.
        /// </exception>
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
                Xgid = Xgid,
                SecondaryPlayIndex = SecondaryPlayIndex,
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