namespace BackgammonDiagram_Lib;

public static class DiagramRequestExtensions
{
    /// <summary>
    /// Returns a (Problem, Solution) pair from a single request.
    /// Titles default to "Problem" / "Solution" when the request has no Title;
    /// when it does, they are suffixed: "{Title} — Problem" / "{Title} — Solution".
    /// </summary>
    public static (DiagramRequest Problem, DiagramRequest Solution)
        ToProblemSolutionPair(this DiagramRequest request)
    {
        string base_ = string.IsNullOrWhiteSpace(request.Title) ? "" : request.Title + " \u2014 ";

        var problem = new DiagramRequest.Builder
        {
            Mop = request.Mop.ToArray(),
            OnRollNeeds = request.OnRollNeeds,
            OpponentNeeds = request.OpponentNeeds,
            OnRollPipCount = request.OnRollPipCount,
            OpponentPipCount = request.OpponentPipCount,
            OnRollName = request.OnRollName,
            OpponentName = request.OpponentName,
            CubeSize = request.CubeSize,
            CubeOwner = request.CubeOwner,
            IsCube = request.IsCube,
            Dice = request.Dice.ToArray(),
            Mode = DiagramMode.Problem,
            HomeBoardOnRight = request.HomeBoardOnRight,
            OnRollAtBottom = request.OnRollAtBottom,
            AnalysisPanelPosition = request.AnalysisPanelPosition,
            Title = base_ + "Problem",
            NoDoubleEquity = request.NoDoubleEquity,
            DoubleTakeEquity = request.DoubleTakeEquity,
            WinPctAfterNoDouble = request.WinPctAfterNoDouble,
            GammonPctAfterNoDouble = request.GammonPctAfterNoDouble,
            BgPctAfterNoDouble = request.BgPctAfterNoDouble,
            LosePctAfterNoDouble = request.LosePctAfterNoDouble,
            LoseGammonPctAfterNoDouble = request.LoseGammonPctAfterNoDouble,
            LoseBgPctAfterNoDouble = request.LoseBgPctAfterNoDouble,
            WinPctAfterDoubleTake = request.WinPctAfterDoubleTake,
            GammonPctAfterDoubleTake = request.GammonPctAfterDoubleTake,
            BgPctAfterDoubleTake = request.BgPctAfterDoubleTake,
            LosePctAfterDoubleTake = request.LosePctAfterDoubleTake,
            LoseGammonPctAfterDoubleTake = request.LoseGammonPctAfterDoubleTake,
            LoseBgPctAfterDoubleTake = request.LoseBgPctAfterDoubleTake,
            ProbOfOpponentErrorJustifyingDouble = request.ProbOfOpponentErrorJustifyingDouble,
            BestPlayIndex = request.BestPlayIndex,
            UserPlayIndex = request.UserPlayIndex,
            Plays = new List<PlayCandidate>(request.Plays),
            AnalysisDepths = new List<AnalysisDepthEntry>(request.AnalysisDepths),
        }.Build();

        var solution = new DiagramRequest.Builder
        {
            Mop = request.Mop.ToArray(),
            OnRollNeeds = request.OnRollNeeds,
            OpponentNeeds = request.OpponentNeeds,
            OnRollPipCount = request.OnRollPipCount,
            OpponentPipCount = request.OpponentPipCount,
            OnRollName = request.OnRollName,
            OpponentName = request.OpponentName,
            CubeSize = request.CubeSize,
            CubeOwner = request.CubeOwner,
            IsCube = request.IsCube,
            Dice = request.Dice.ToArray(),
            Mode = DiagramMode.Solution,
            HomeBoardOnRight = request.HomeBoardOnRight,
            OnRollAtBottom = request.OnRollAtBottom,
            AnalysisPanelPosition = request.AnalysisPanelPosition,
            Title = base_ + "Solution",
            NoDoubleEquity = request.NoDoubleEquity,
            DoubleTakeEquity = request.DoubleTakeEquity,
            WinPctAfterNoDouble = request.WinPctAfterNoDouble,
            GammonPctAfterNoDouble = request.GammonPctAfterNoDouble,
            BgPctAfterNoDouble = request.BgPctAfterNoDouble,
            LosePctAfterNoDouble = request.LosePctAfterNoDouble,
            LoseGammonPctAfterNoDouble = request.LoseGammonPctAfterNoDouble,
            LoseBgPctAfterNoDouble = request.LoseBgPctAfterNoDouble,
            WinPctAfterDoubleTake = request.WinPctAfterDoubleTake,
            GammonPctAfterDoubleTake = request.GammonPctAfterDoubleTake,
            BgPctAfterDoubleTake = request.BgPctAfterDoubleTake,
            LosePctAfterDoubleTake = request.LosePctAfterDoubleTake,
            LoseGammonPctAfterDoubleTake = request.LoseGammonPctAfterDoubleTake,
            LoseBgPctAfterDoubleTake = request.LoseBgPctAfterDoubleTake,
            ProbOfOpponentErrorJustifyingDouble = request.ProbOfOpponentErrorJustifyingDouble,
            BestPlayIndex = request.BestPlayIndex,
            UserPlayIndex = request.UserPlayIndex,
            Plays = new List<PlayCandidate>(request.Plays),
            AnalysisDepths = new List<AnalysisDepthEntry>(request.AnalysisDepths),
        }.Build();

        return (problem, solution);
    }
}