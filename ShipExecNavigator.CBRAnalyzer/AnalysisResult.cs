namespace ShipExecNavigator.CBRAnalyzer;

/// <summary>
/// Returned by CbrAnalyzer.AnalyzeAsync for each JS file processed.
/// </summary>
public sealed class CbrPassResult
{
    /// <summary>The full path of the updated helper JS file saved this pass.</summary>
    public string UpdatedHelperPath { get; init; } = string.Empty;

    /// <summary>Brief summary of what was changed in the helper this pass.</summary>
    public string ChangeSummary { get; init; } = string.Empty;
}

public sealed class AnalysisResult
{
    public string Summary { get; init; } = string.Empty;
    public List<HookAnalysis> ImplementedHooks { get; init; } = [];
    public List<string> Patterns { get; init; } = [];
}

public sealed class HookAnalysis
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
