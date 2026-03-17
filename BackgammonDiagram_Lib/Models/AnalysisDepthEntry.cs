namespace BackgammonDiagram_Lib;

public class AnalysisDepthEntry
{
    /// <summary>Human-readable analysis depth label, e.g. "3-ply", "Rollout: 1296 trials. 3-ply".</summary>
    public string Label { get; init; } = string.Empty;
}
