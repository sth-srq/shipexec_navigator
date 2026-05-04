namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Result of a two-pass blueprint analysis (CBR/SBR pass + Templates pass).
/// </summary>
public sealed class BlueprintAnalysisResult
{
    /// <summary>Output folder path containing the modified template project.</summary>
    public string OutputFolder { get; set; } = string.Empty;

    /// <summary>Step 1: Initial analysis plan of action.</summary>
    public string PlanOfAction { get; set; } = string.Empty;

    /// <summary>Pass 1 result: CBR/SBR hook analysis and proposed code.</summary>
    public string HooksAnalysis { get; set; } = string.Empty;

    /// <summary>Pass 2 result: Template modifications analysis and proposed HTML/JS changes.</summary>
    public string TemplatesAnalysis { get; set; } = string.Empty;

    /// <summary>Reports/labels analysis output.</summary>
    public string ReportsAnalysis { get; set; } = string.Empty;

    /// <summary>Files that were modified in the output folder.</summary>
    public List<string> ModifiedFiles { get; set; } = [];

    /// <summary>Build/validation output (if any).</summary>
    public string ValidationOutput { get; set; } = string.Empty;

    /// <summary>Whether the final output compiled/validated successfully.</summary>
    public bool ValidationPassed { get; set; }

    /// <summary>Raw build error output from dotnet build (shown to user if build fails).</summary>
    public string BuildErrors { get; set; } = string.Empty;

    /// <summary>CBR (JS) validation errors from the CBR validation step.</summary>
    public string CbrValidationErrors { get; set; } = string.Empty;

    /// <summary>Template/Report (HTML) validation errors from the HTML validation step.</summary>
    public string HtmlValidationErrors { get; set; } = string.Empty;

    /// <summary>Any errors encountered during analysis.</summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>Log of each AI interaction (prompt sent and response received).</summary>
    public List<AiInteractionLog> AiInteractions { get; set; } = [];
}

/// <summary>Records a single AI request/response pair.</summary>
public sealed class AiInteractionLog
{
    public string Step { get; set; } = string.Empty;
    public string PromptSent { get; set; } = string.Empty;
    public string ResponseReceived { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
