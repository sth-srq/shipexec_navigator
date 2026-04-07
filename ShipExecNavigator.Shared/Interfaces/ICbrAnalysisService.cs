namespace ShipExecNavigator.Shared.Interfaces;

public interface ICbrAnalysisService
{
    /// <summary>
    /// Sends <paramref name="cbrScript"/> and the CBRHelper.min.js library to the AI
    /// and returns refactored JavaScript that uses helper methods and includes
    /// comments highlighting the changes, with fixed variable naming and brace style.
    /// </summary>
    Task<string> AnalyzeCbrAsync(string cbrScript, CancellationToken ct = default);
}
