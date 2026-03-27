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
        var problem = request with { Mode = DiagramMode.Problem, Title = base_ + "Problem" };
        var solution = request with { Mode = DiagramMode.Solution, Title = base_ + "Solution" };
        return (problem, solution);
    }
}