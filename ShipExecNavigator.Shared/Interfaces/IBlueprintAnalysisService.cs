using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Shared.Interfaces;

/// <summary>
/// Analyzes a company blueprint document against ShipExec hook and template references,
/// produces a modified template project copy with proposed changes.
/// </summary>
public interface IBlueprintAnalysisService
{
    /// <summary>
    /// Runs two-pass AI analysis on the blueprint content:
    /// Pass 1 — CBR/SBR hook identification using shipexec-hooks.md instructions.
    /// Pass 2 — Template modifications using shipexec-templates.md instructions.
    /// Then copies the template project, applies changes, and validates.
    /// </summary>
    /// <param name="blueprintText">The raw text content of the blueprint document.</param>
    /// <param name="fileName">Original file name (for labeling).</param>
    /// <param name="onProgress">Callback for progress messages.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Analysis result with output folder path and details.</returns>
    Task<BlueprintAnalysisResult> AnalyzeAsync(
        string blueprintText,
        string fileName,
        Action<string>? onProgress = null,
        CancellationToken ct = default);
}
