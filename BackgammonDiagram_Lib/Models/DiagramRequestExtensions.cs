namespace BackgammonDiagram_Lib;

/// <summary>Extension helpers over <see cref="DiagramRequest"/>.</summary>
public static class DiagramRequestExtensions
{
    /// <summary>
    /// Expands a decision's request into a matched Problem/Solution pair: the
    /// same request twice, with <see cref="DiagramRequest.Mode"/> set to
    /// <see cref="DiagramMode.Problem"/> and to
    /// <see cref="DiagramMode.Solution"/>. Every other option — the ranking,
    /// the ceiling, the marks, the orientation, the position number — rides
    /// both, since each side is <paramref name="request"/> <c>with</c> its
    /// mode. The title strip is composed by the renderer from context, so
    /// nothing else changes.
    /// </summary>
    /// <param name="request">A decision's request; it is not changed.</param>
    /// <returns>The problem and the solution.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="request"/> draws a board — a working board included —
    /// which has no analysis, so no solution.
    /// </exception>
    public static (DiagramRequest Problem, DiagramRequest Solution)
        ToProblemSolutionPair(this DiagramRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return (request with { Mode = DiagramMode.Problem }, request with { Mode = DiagramMode.Solution });
    }
}
