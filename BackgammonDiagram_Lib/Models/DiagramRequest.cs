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
    /// Whether the analysis panel renders blank
    /// (<see cref="DiagramMode.Problem"/>) or filled
    /// (<see cref="DiagramMode.Solution"/>). Under the panel-bearing canvas
    /// presets both modes share identical overall dimensions — the panel
    /// region is allocated either way — so swapping modes never reflows
    /// surrounding content. <see cref="AspectPreset.BoardOnly"/> is the ruled
    /// exception: it drops the panel allocation and the title strip for
    /// Problem renders (and is rejected for Solution renders), so a consumer
    /// opting into it accepts that its canvas differs from the Solution
    /// canvas.
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

    /// <summary>
    /// Row order of the Solution-mode play panel's candidate list. The
    /// default, <see cref="BackgammonDiagram_Lib.CandidateOrdering.Equity"/>,
    /// renders the caller's (assumed equity-sorted) order unchanged — exactly
    /// the pre-existing behaviour.
    /// <see cref="BackgammonDiagram_Lib.CandidateOrdering.DepthFirst"/> orders
    /// by analysis depth, deepest first, keeping equity order within a depth
    /// tier; see the enum for the ordering rule and its rationale
    /// (halheinrich/backgammon#150).
    /// <para>
    /// A <b>display option the consumer sets directly</b>, like
    /// <see cref="SecondaryPlayIndex"/>: the data-sourcing factories
    /// (<see cref="FromDecisionData"/> and the three-record
    /// <c>Builder.From</c>) leave it at the default, so every export path is
    /// visually unchanged; only <c>Builder.From(DiagramRequest)</c> carries it
    /// across. Reordering moves whole rows: each candidate keeps its own rank
    /// number, play marker, and rank-inversion italics from its position in
    /// the source list — marks follow candidates, not row positions.
    /// </para>
    /// </summary>
    public CandidateOrdering CandidateOrdering { get; init; }

    /// <summary>
    /// Optional display floor for the Solution-mode play panel: hides
    /// candidates analysed below this level (halheinrich/backgammon#66).
    /// Null (the default) shows every candidate. The floor is inclusive — a
    /// candidate evaluated <em>at</em> the floor level still renders — so
    /// "4-ply and lower should not be displayed" is
    /// <see cref="AnalysisLevel.Ply5"/>.
    /// <para>
    /// The floor reads the two-axis depth taxonomy the producer stamped
    /// (<see cref="BgDataTypes_Lib.PlayCandidate.AnalysisMode"/> ×
    /// <see cref="BgDataTypes_Lib.PlayCandidate.AnalysisLevel"/>): a candidate
    /// is hidden iff its numbers came from a direct evaluation
    /// (<see cref="AnalysisMode.Evaluation"/>) whose stamped level sits below
    /// the floor on the level axis's declared ascending-rigor order — in which
    /// the ply family and the XG Roller family <em>interleave</em> (…3-ply,
    /// XG Roller, 4-ply, XG Roller+, 5-ply…) rather than forming two blocks,
    /// so a ply floor catches the Roller levels beneath it: a
    /// <see cref="AnalysisLevel.Ply5"/> floor hides
    /// <see cref="AnalysisLevel.XgRoller"/> and
    /// <see cref="AnalysisLevel.XgRollerPlus"/> along with the shallow plies,
    /// and only <see cref="AnalysisLevel.XgRollerPlusPlus"/> outranks every
    /// ply. Rollout-family candidates are never hidden:
    /// for those modes <c>AnalysisLevel</c> records the rollout's
    /// <em>inner</em> evaluation level, not the analysis's own depth, and a
    /// rollout is deeper than any evaluation floor. Unstamped candidates
    /// (<see cref="AnalysisMode.Unknown"/> or
    /// <see cref="AnalysisLevel.Unknown"/>) are never hidden either — Unknown
    /// means "not recorded", never "shallow", so the panel degrades to
    /// showing the row rather than guessing a depth. That is clause (a) of
    /// the <see cref="AnalysisLevel"/> contract: Unknown sits outside the
    /// rigor scale, so it is never excluded by a rigor floor. Its zero
    /// numbering would compare below every real level, so the exclusion is
    /// explicit here rather than a consequence of the ordinal.
    /// </para>
    /// <para>
    /// <b>Contract: the best-play row
    /// (<see cref="BgDataTypes_Lib.DecisionData.BestPlayIndex"/>), the user's
    /// actual-play row
    /// (<see cref="BgDataTypes_Lib.DecisionData.UserPlayIndex"/>), and an
    /// active secondary-play row (<see cref="SecondaryPlayIndex"/>) are never
    /// hidden by the floor, whatever their depth</b> — review must always
    /// show what was best and what was played. Like
    /// <see cref="CandidateOrdering"/>, this is a consumer-set display option
    /// the data-sourcing factories leave unset;
    /// <see cref="AnalysisLevel.Unknown"/> is rejected at
    /// <see cref="Builder.Build"/> (it means "level not recorded", not a
    /// depth — use null to show all).
    /// </para>
    /// </summary>
    public AnalysisLevel? MinimumCandidateAnalysisLevel { get; init; }

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
    /// <para>
    /// <b>Full-copy invariant.</b> The Builder carries <em>every</em> carriable
    /// member of <see cref="PositionData"/>, <see cref="DecisionData"/>, and
    /// <see cref="DescriptiveData"/>; a new field added to any of those records
    /// joins the copy in the same change. Because the Builder re-spells each
    /// record field-by-field rather than holding the instance, this is a second
    /// enumeration of the data layer's facts — so it is enforced by test, not
    /// by convention: <c>BuilderFieldCarriageTests</c> reflects over the three
    /// record types and fails until the new field is carried
    /// (halheinrich/backgammon#122).
    /// </para>
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
        /// <inheritdoc cref="PositionData.IsJacoby"/>
        public bool? IsJacoby { get; set; }

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
        /// <inheritdoc cref="DecisionData.CubeAnalysisMode"/>
        public AnalysisMode CubeAnalysisMode { get; set; }
        /// <inheritdoc cref="DecisionData.CubeAnalysisLevel"/>
        public AnalysisLevel CubeAnalysisLevel { get; set; }
        /// <inheritdoc cref="DecisionData.BestPlayIndex"/>
        public int BestPlayIndex { get; set; }
        /// <inheritdoc cref="DecisionData.UserPlayIndex"/>
        public int UserPlayIndex { get; set; } = -1;
        /// <inheritdoc cref="DecisionData.UserPlayError"/>
        public double? UserPlayError { get; set; }
        /// <inheritdoc cref="DecisionData.NoDoubleEquity"/>
        public double NoDoubleEquity { get; set; }
        /// <inheritdoc cref="DecisionData.DoubleTakeEquity"/>
        public double DoubleTakeEquity { get; set; }
        /// <inheritdoc cref="DecisionData.CubelessNoDoubleEquity"/>
        public double CubelessNoDoubleEquity { get; set; }
        /// <inheritdoc cref="DecisionData.CubelessDoubleTakeEquity"/>
        public double CubelessDoubleTakeEquity { get; set; }
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
        /// decision or no data is recorded.
        /// </summary>
        public double? UserDoubleError { get; set; }

        /// <summary>
        /// Equity loss from the user's take / pass decision vs. the correct
        /// response (>= 0). Null when the user did not face a take decision
        /// or no data is recorded.
        /// </summary>
        public double? UserTakeError { get; set; }

        // The played cube actions drive the "Actual" banner row. Carried as
        // plain nullable setters: the half-guard (doubler half is Double or
        // NoDouble, taker half is Take or Pass) lives on DecisionData's init
        // accessors and fires when Build() constructs the record, so the
        // Builder must not re-encode it.

        /// <inheritdoc cref="DecisionData.UserDoublerAction"/>
        public CubeAction? UserDoublerAction { get; set; }

        /// <inheritdoc cref="DecisionData.UserTakerAction"/>
        public CubeAction? UserTakerAction { get; set; }

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
        /// <inheritdoc cref="DescriptiveData.Game"/>
        public int Game { get; set; }
        /// <inheritdoc cref="DescriptiveData.MoveNumber"/>
        public int MoveNumber { get; set; }
        /// <inheritdoc cref="DescriptiveData.IsStandardStart"/>
        public bool IsStandardStart { get; set; }
        /// <inheritdoc cref="DescriptiveData.Comment"/>
        public string Comment { get; set; } = string.Empty;
        /// <inheritdoc cref="DescriptiveData.Flagged"/>
        public bool Flagged { get; set; }

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

        /// <summary>
        /// Row order of the play panel's candidate list. See
        /// <see cref="DiagramRequest.CandidateOrdering"/>. Sits with the
        /// renderer-specific fields because it is set by the consumer, not
        /// copied from the data records. Defaults to
        /// <see cref="BackgammonDiagram_Lib.CandidateOrdering.Equity"/>
        /// (caller order, unchanged).
        /// </summary>
        public CandidateOrdering CandidateOrdering { get; set; }

        /// <summary>
        /// Optional display floor for the play panel's candidate list. See
        /// <see cref="DiagramRequest.MinimumCandidateAnalysisLevel"/> for the
        /// hiding rule and the never-hidden contract. Sits with the
        /// renderer-specific fields because it is set by the consumer, not
        /// copied from the data records. Defaults to null (show all);
        /// <see cref="AnalysisLevel.Unknown"/> is rejected by
        /// <see cref="Build"/>.
        /// </summary>
        public AnalysisLevel? MinimumCandidateAnalysisLevel { get; set; }

        // -------------------------------------------------------------------
        //  Factories — single field-mapping site for data → builder
        // -------------------------------------------------------------------

        /// <summary>
        /// Starts a Builder pre-populated from the three BgDataTypes_Lib
        /// records plus renderer-specific parameters. Single field-mapping
        /// site for the data-layer → rendering-layer copy, shared by
        /// <see cref="FromDecisionData"/> and
        /// <see cref="DiagramRequestExtensions.ToProblemSolutionPair"/> —
        /// so a new <see cref="PositionData"/> / <see cref="DecisionData"/> /
        /// <see cref="DescriptiveData"/> field is carried by adding it here and
        /// in <see cref="Build"/>, per the Builder's full-copy invariant.
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
                IsJacoby = position.IsJacoby,

                // Decision
                IsCube = decision.IsCube,
                Dice = decision.Dice.ToArray(),
                Plays = new List<PlayCandidate>(decision.Plays),
                CubeDepth = decision.CubeDepth,
                CubeDepthAbbreviation = decision.CubeDepthAbbreviation,
                CubeDepthRank = decision.CubeDepthRank,
                CubeAnalysisMode = decision.CubeAnalysisMode,
                CubeAnalysisLevel = decision.CubeAnalysisLevel,
                BestPlayIndex = decision.BestPlayIndex,
                UserPlayIndex = decision.UserPlayIndex,
                UserPlayError = decision.UserPlayError,
                NoDoubleEquity = decision.NoDoubleEquity,
                DoubleTakeEquity = decision.DoubleTakeEquity,
                CubelessNoDoubleEquity = decision.CubelessNoDoubleEquity,
                CubelessDoubleTakeEquity = decision.CubelessDoubleTakeEquity,
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
                UserDoublerAction = decision.UserDoublerAction,
                UserTakerAction = decision.UserTakerAction,

                // Descriptive
                OnRollName = descriptive.OnRollName,
                OpponentName = descriptive.OpponentName,
                Title = descriptive.Title,
                MatchLength = descriptive.MatchLength,
                Date = descriptive.Date,
                Event = descriptive.Event,
                SourceFile = descriptive.SourceFile,
                Game = descriptive.Game,
                MoveNumber = descriptive.MoveNumber,
                IsStandardStart = descriptive.IsStandardStart,
                Comment = descriptive.Comment,
                Flagged = descriptive.Flagged,

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
            // Render-only overlay and display options — not carried by the
            // three-record From, so copy them here to keep this "reproduce a
            // request" factory faithful.
            b.SecondaryPlayIndex = existing.SecondaryPlayIndex;
            b.CandidateOrdering = existing.CandidateOrdering;
            b.MinimumCandidateAnalysisLevel = existing.MinimumCandidateAnalysisLevel;
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
        /// die), <see cref="CubeSize"/> is not a power of two in 1..4096,
        /// <see cref="CandidateOrdering"/> or a non-null
        /// <see cref="MinimumCandidateAnalysisLevel"/> is an undefined enum
        /// value, or <see cref="MinimumCandidateAnalysisLevel"/> is
        /// <see cref="AnalysisLevel.Unknown"/> (which means "level not
        /// recorded", not a depth — use null to show all candidates).
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
                    IsJacoby = IsJacoby,
                },
                Decision = new DecisionData
                {
                    IsCube = IsCube,
                    Dice = Dice.ToArray(),
                    Plays = new List<PlayCandidate>(Plays),
                    CubeDepth = CubeDepth,
                    CubeDepthAbbreviation = CubeDepthAbbreviation,
                    CubeDepthRank = CubeDepthRank,
                    CubeAnalysisMode = CubeAnalysisMode,
                    CubeAnalysisLevel = CubeAnalysisLevel,
                    BestPlayIndex = BestPlayIndex,
                    UserPlayIndex = UserPlayIndex,
                    UserPlayError = UserPlayError,
                    NoDoubleEquity = NoDoubleEquity,
                    DoubleTakeEquity = DoubleTakeEquity,
                    CubelessNoDoubleEquity = CubelessNoDoubleEquity,
                    CubelessDoubleTakeEquity = CubelessDoubleTakeEquity,
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
                    UserDoublerAction = UserDoublerAction,
                    UserTakerAction = UserTakerAction,
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
                    Game = Game,
                    MoveNumber = MoveNumber,
                    IsStandardStart = IsStandardStart,
                    Comment = Comment,
                    Flagged = Flagged,
                },
                Mode = Mode,
                HomeBoardOnRight = HomeBoardOnRight,
                OnRollAtBottom = OnRollAtBottom,
                AnalysisPanelPosition = AnalysisPanelPosition,
                PositionNumber = PositionNumber,
                Xgid = Xgid,
                SecondaryPlayIndex = SecondaryPlayIndex,
                CandidateOrdering = CandidateOrdering,
                MinimumCandidateAnalysisLevel = MinimumCandidateAnalysisLevel,
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
            // The display options are caller configuration, not producer data,
            // so they get the validate-don't-tolerate register: reject
            // undefined enum values, and reject an Unknown floor — Unknown
            // means "level not recorded", never a depth, so it cannot bound
            // one (null is the show-all state).
            if (!Enum.IsDefined(CandidateOrdering))
                throw new InvalidOperationException("CandidateOrdering must be a defined CandidateOrdering value.");
            if (MinimumCandidateAnalysisLevel is AnalysisLevel floor)
            {
                if (!Enum.IsDefined(floor))
                    throw new InvalidOperationException("MinimumCandidateAnalysisLevel must be a defined AnalysisLevel value.");
                if (floor == AnalysisLevel.Unknown)
                    throw new InvalidOperationException(
                        "MinimumCandidateAnalysisLevel must not be AnalysisLevel.Unknown — Unknown means "
                        + "\"level not recorded\", not a depth; use null to show all candidates.");
            }
        }
    }
}