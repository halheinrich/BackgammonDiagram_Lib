namespace BackgammonDiagram_Lib;

public static class DiagramRequestExtensions
{
    /// <summary>
    /// Expands a single request into a matched Problem/Solution pair. Titles
    /// are suffixed with " — Problem" (play) / " — Cube Action?" (cube) /
    /// " — Solution" if the source request has a non-empty Title; otherwise
    /// the suffix stands alone.
    /// </summary>
    public static (DiagramRequest Problem, DiagramRequest Solution)
        ToProblemSolutionPair(this DiagramRequest request)
    {
        string? title = request.Descriptive.Title;
        string prefix = string.IsNullOrWhiteSpace(title) ? "" : title + " \u2014 ";
        string problemSuffix = request.Decision.IsCube ? "Cube Action?" : "Problem";

        // Builder.From handles the full field mapping — this extension just
        // flips the Mode and swaps the Title. Adding a new DiagramRequest
        // field only requires updating Builder.From, not this method.
        var problemBuilder = DiagramRequest.Builder.From(request);
        problemBuilder.Mode = DiagramMode.Problem;
        problemBuilder.Title = prefix + problemSuffix;

        var solutionBuilder = DiagramRequest.Builder.From(request);
        solutionBuilder.Mode = DiagramMode.Solution;
        solutionBuilder.Title = prefix + "Solution";

        return (problemBuilder.Build(), solutionBuilder.Build());
    }
}
