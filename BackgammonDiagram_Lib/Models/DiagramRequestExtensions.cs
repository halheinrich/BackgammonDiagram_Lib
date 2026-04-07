using BgDataTypes_Lib;

namespace BackgammonDiagram_Lib;

public static class DiagramRequestExtensions
{
    public static (DiagramRequest Problem, DiagramRequest Solution)
        ToProblemSolutionPair(this DiagramRequest request)
    {
        string? title = request.Descriptive.Title;
        string prefix = string.IsNullOrWhiteSpace(title) ? "" : title + " \u2014 ";

        var problem = new DiagramRequest.Builder
        {
            Mop = request.Position.Mop.ToArray(),
            OnRollNeeds = request.Position.OnRollNeeds,
            OpponentNeeds = request.Position.OpponentNeeds,
            OnRollPipCount = request.Position.OnRollPipCount,
            OpponentPipCount = request.Position.OpponentPipCount,
            CubeSize = request.Position.CubeSize,
            CubeOwner = request.Position.CubeOwner,
            IsCrawford = request.Position.IsCrawford,
            IsCube = request.Decision.IsCube,
            Dice = request.Decision.Dice.ToArray(),
            Plays = new List<PlayCandidate>(request.Decision.Plays),
            AnalysisDepths = new List<AnalysisDepthEntry>(request.Decision.AnalysisDepths),
            BestPlayIndex = request.Decision.BestPlayIndex,
            UserPlayIndex = request.Decision.UserPlayIndex,
            NoDoubleEquity = request.Decision.NoDoubleEquity,
            DoubleTakeEquity = request.Decision.DoubleTakeEquity,
            WinPctAfterNoDouble = request.Decision.WinPctAfterNoDouble,
            GammonPctAfterNoDouble = request.Decision.GammonPctAfterNoDouble,
            BgPctAfterNoDouble = request.Decision.BgPctAfterNoDouble,
            LosePctAfterNoDouble = request.Decision.LosePctAfterNoDouble,
            LoseGammonPctAfterNoDouble = request.Decision.LoseGammonPctAfterNoDouble,
            LoseBgPctAfterNoDouble = request.Decision.LoseBgPctAfterNoDouble,
            WinPctAfterDoubleTake = request.Decision.WinPctAfterDoubleTake,
            GammonPctAfterDoubleTake = request.Decision.GammonPctAfterDoubleTake,
            BgPctAfterDoubleTake = request.Decision.BgPctAfterDoubleTake,
            LosePctAfterDoubleTake = request.Decision.LosePctAfterDoubleTake,
            LoseGammonPctAfterDoubleTake = request.Decision.LoseGammonPctAfterDoubleTake,
            LoseBgPctAfterDoubleTake = request.Decision.LoseBgPctAfterDoubleTake,
            ProbOfOpponentErrorJustifyingDouble = request.Decision.ProbOfOpponentErrorJustifyingDouble,
            OnRollName = request.Descriptive.OnRollName,
            OpponentName = request.Descriptive.OpponentName,
            MatchLength = request.Descriptive.MatchLength,
            Date = request.Descriptive.Date,
            Event = request.Descriptive.Event,
            HomeBoardOnRight = request.HomeBoardOnRight,
            OnRollAtBottom = request.OnRollAtBottom,
            AnalysisPanelPosition = request.AnalysisPanelPosition,
            Mode = DiagramMode.Problem,
            Title = prefix + "Problem",
        }.Build();

        var solution = new DiagramRequest.Builder
        {
            Mop = request.Position.Mop.ToArray(),
            OnRollNeeds = request.Position.OnRollNeeds,
            OpponentNeeds = request.Position.OpponentNeeds,
            OnRollPipCount = request.Position.OnRollPipCount,
            OpponentPipCount = request.Position.OpponentPipCount,
            CubeSize = request.Position.CubeSize,
            CubeOwner = request.Position.CubeOwner,
            IsCrawford = request.Position.IsCrawford,
            IsCube = request.Decision.IsCube,
            Dice = request.Decision.Dice.ToArray(),
            Plays = new List<PlayCandidate>(request.Decision.Plays),
            AnalysisDepths = new List<AnalysisDepthEntry>(request.Decision.AnalysisDepths),
            BestPlayIndex = request.Decision.BestPlayIndex,
            UserPlayIndex = request.Decision.UserPlayIndex,
            NoDoubleEquity = request.Decision.NoDoubleEquity,
            DoubleTakeEquity = request.Decision.DoubleTakeEquity,
            WinPctAfterNoDouble = request.Decision.WinPctAfterNoDouble,
            GammonPctAfterNoDouble = request.Decision.GammonPctAfterNoDouble,
            BgPctAfterNoDouble = request.Decision.BgPctAfterNoDouble,
            LosePctAfterNoDouble = request.Decision.LosePctAfterNoDouble,
            LoseGammonPctAfterNoDouble = request.Decision.LoseGammonPctAfterNoDouble,
            LoseBgPctAfterNoDouble = request.Decision.LoseBgPctAfterNoDouble,
            WinPctAfterDoubleTake = request.Decision.WinPctAfterDoubleTake,
            GammonPctAfterDoubleTake = request.Decision.GammonPctAfterDoubleTake,
            BgPctAfterDoubleTake = request.Decision.BgPctAfterDoubleTake,
            LosePctAfterDoubleTake = request.Decision.LosePctAfterDoubleTake,
            LoseGammonPctAfterDoubleTake = request.Decision.LoseGammonPctAfterDoubleTake,
            LoseBgPctAfterDoubleTake = request.Decision.LoseBgPctAfterDoubleTake,
            ProbOfOpponentErrorJustifyingDouble = request.Decision.ProbOfOpponentErrorJustifyingDouble,
            OnRollName = request.Descriptive.OnRollName,
            OpponentName = request.Descriptive.OpponentName,
            MatchLength = request.Descriptive.MatchLength,
            Date = request.Descriptive.Date,
            Event = request.Descriptive.Event,
            HomeBoardOnRight = request.HomeBoardOnRight,
            OnRollAtBottom = request.OnRollAtBottom,
            AnalysisPanelPosition = request.AnalysisPanelPosition,
            Mode = DiagramMode.Solution,
            Title = prefix + "Solution",
        }.Build();

        return (problem, solution);
    }
}