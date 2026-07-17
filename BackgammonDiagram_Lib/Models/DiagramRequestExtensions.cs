namespace BackgammonDiagram_Lib;

/// <summary>Extension helpers over <see cref="DiagramRequest"/>.</summary>
public static class DiagramRequestExtensions
{
    /// <summary>
    /// Expands a single request into a matched Problem/Solution pair by
    /// flipping the Mode. Title is no longer touched — the renderer composes
    /// the title strip from context (dice-to-play / Cube Action? + optional
    /// Position counter) rather than from Descriptive.Title.
    /// </summary>
    public static (DiagramRequest Problem, DiagramRequest Solution)
        ToProblemSolutionPair(this DiagramRequest request)
    {
        var problemBuilder = DiagramRequest.Builder.From(request);
        problemBuilder.Mode = DiagramMode.Problem;

        var solutionBuilder = DiagramRequest.Builder.From(request);
        solutionBuilder.Mode = DiagramMode.Solution;

        return (problemBuilder.Build(), solutionBuilder.Build());
    }
}
