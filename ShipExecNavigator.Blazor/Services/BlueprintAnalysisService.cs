using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Services;

/// <summary>
/// Two-pass blueprint analysis service:
/// Pass 1 — Sends blueprint + shipexec-hooks.md to AI for CBR/SBR identification.
/// Pass 2 — Sends blueprint + shipexec-templates.md to AI for template modifications.
/// Then copies the template project, applies proposed changes, and validates.
/// </summary>
public sealed class BlueprintAnalysisService(
    IConfiguration configuration,
    IWebHostEnvironment env,
    IHttpClientFactory httpClientFactory,
    ILogger<BlueprintAnalysisService> logger) : IBlueprintAnalysisService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<BlueprintAnalysisResult> AnalyzeAsync(
        string blueprintText,
        string fileName,
        Action<string>? onProgress = null,
        CancellationToken ct = default)
    {
        var result = new BlueprintAnalysisResult();

        var endpoint = configuration["AzureOpenAI:Endpoint"] ?? string.Empty;
        var apiKey = configuration["AzureOpenAI:ApiKey"] ?? string.Empty;
        var deployment = configuration["AzureOpenAI:ChatDeployment"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.Errors.Add("Azure OpenAI is not configured. Set AzureOpenAI:ApiKey in appsettings.json.");
            return result;
        }

        // Load instruction files from AIHelpers folder
        var templateProjectPath = Path.Combine(env.ContentRootPath, "..", "CodeStandards", "TemplateCodeShipExec20BusinessRules");
        var aiHelpersPath = Path.Combine(env.ContentRootPath, "..", "AIHelpers");
        var hooksDocPath = Path.Combine(aiHelpersPath, "shipexec-hooks.md");
        var templatesDocPath = Path.Combine(aiHelpersPath, "shipexec-templates.md");
        var typeRefPath = Path.Combine(aiHelpersPath, "PSI_Sox_Type_Reference.md");
        var typeDefsPath = Path.Combine(aiHelpersPath, "psi-sox-type-definitions.md");

        if (!File.Exists(hooksDocPath))
        {
            result.Errors.Add($"shipexec-hooks.md not found at: {hooksDocPath}");
            return result;
        }
        if (!File.Exists(templatesDocPath))
        {
            result.Errors.Add($"shipexec-templates.md not found at: {templatesDocPath}");
            return result;
        }

        var hooksDoc = await File.ReadAllTextAsync(hooksDocPath, ct);
        var templatesDoc = await File.ReadAllTextAsync(templatesDocPath, ct);
        var typeRefDoc = File.Exists(typeRefPath) ? await File.ReadAllTextAsync(typeRefPath, ct) : "";
        var typeDefsDoc = File.Exists(typeDefsPath) ? await File.ReadAllTextAsync(typeDefsPath, ct) : "";

        // Load system prompts from AIHelpers folder
        var sbrPromptPath = Path.Combine(aiHelpersPath, "sbr-system-prompt.md");
        var cbrPromptPath = Path.Combine(aiHelpersPath, "cbr-system-prompt.md");
        var templatePromptPath = Path.Combine(aiHelpersPath, "template-system-prompt.md");
        var sbrSystemPrompt = File.Exists(sbrPromptPath) ? await File.ReadAllTextAsync(sbrPromptPath, ct) : throw new FileNotFoundException($"sbr-system-prompt.md not found at: {sbrPromptPath}");
        var cbrSystemPrompt = File.Exists(cbrPromptPath) ? await File.ReadAllTextAsync(cbrPromptPath, ct) : throw new FileNotFoundException($"cbr-system-prompt.md not found at: {cbrPromptPath}");
        var templateSystemPrompt = File.Exists(templatePromptPath) ? await File.ReadAllTextAsync(templatePromptPath, ct) : throw new FileNotFoundException($"template-system-prompt.md not found at: {templatePromptPath}");

        // Track all activity for context in repair passes
        var activityLog = new StringBuilder();

        // Helper to call AI and log the interaction
        async Task<string> CallAiAndLog(string stepName, string systemPrompt, string userMsg, bool jsonFormat)
        {
            result.AiInteractions.Add(new AiInteractionLog
            {
                Step = stepName,
                PromptSent = $"[SYSTEM]\n{systemPrompt}\n\n[USER]\n{userMsg}",
                ResponseReceived = "(pending...)",
                Timestamp = DateTime.Now
            });
            var resp = await CallAzureOpenAiAsync(deployment, endpoint, apiKey, systemPrompt, userMsg, jsonFormat, ct);
            result.AiInteractions[^1].ResponseReceived = resp;
            return resp;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Initial Analysis — Develop a plan of action
        // ═══════════════════════════════════════════════════════════════════════
        onProgress?.Invoke("Step 1: Developing plan of action...");
        logger.LogInformation("Blueprint Step1 (Plan) | File={File}", fileName);

        var planSystemPrompt =
            """
            You are an expert ShipExec implementation architect. You have been given:
            1. A reference document describing all ShipExec hooks (CBR and SBR), their execution order, and implementation patterns.
            2. A reference document describing all ShipExec HTML templates and their structure.
            3. A company blueprint document describing custom shipping logic requirements.

            ## Your task
            Analyze the blueprint and produce a detailed implementation plan BEFORE any code is written.
            Identify:
            - What the blueprint is asking for (summarize the business requirements)
            - Which SBR hooks will need code and what each should do at a high level
            - Which CBR hooks will need code and what each should do at a high level
            - Which HTML templates will need modification and what changes are needed
            - Any helper/manager classes that will be needed
            - Any potential pitfalls or complications
            - The recommended order of implementation

            ## Required response format
            Respond with EXACTLY ONE valid JSON object — no markdown, no code fences.
            {
              "summary": "<1-2 paragraph summary of what the blueprint requires>",
              "sbrPlan": [
                { "method": "<hook name>", "purpose": "<what this hook should do>" }
              ],
              "cbrPlan": [
                { "method": "<hook name>", "purpose": "<what this hook should do>" }
              ],
              "templatePlan": [
                { "file": "<template filename>", "changes": "<description of changes>" }
              ],
              "managerClasses": [
                { "name": "<class name>", "responsibility": "<what it handles>" }
              ],
              "risks": ["<potential issues to watch for>"],
              "implementationOrder": ["<step 1>", "<step 2>", "..."]
            }
            """;

        var planMessage = new StringBuilder();
        planMessage.AppendLine("=== SHIPEXEC HOOKS REFERENCE ===");
        planMessage.AppendLine(hooksDoc);
        planMessage.AppendLine();
        planMessage.AppendLine("=== SHIPEXEC TEMPLATES REFERENCE ===");
        planMessage.AppendLine(templatesDoc);
        planMessage.AppendLine();
        if (!string.IsNullOrEmpty(typeRefDoc))
        {
            planMessage.AppendLine("=== PSI.SOX TYPE REFERENCE ===");
            planMessage.AppendLine(typeRefDoc);
            planMessage.AppendLine();
        }
        if (!string.IsNullOrEmpty(typeDefsDoc))
        {
            planMessage.AppendLine("=== PSI.SOX TYPE DEFINITIONS ===");
            planMessage.AppendLine(typeDefsDoc);
            planMessage.AppendLine();
        }
        planMessage.AppendLine("=== COMPANY BLUEPRINT ===");
        planMessage.AppendLine(blueprintText);

        var planRaw = await CallAiAndLog("Step 1: Initial Analysis", planSystemPrompt, planMessage.ToString(), true);

        if (planRaw.StartsWith("// ⚠️"))
        {
            result.Errors.Add(planRaw);
            return result;
        }

        result.PlanOfAction = planRaw;
        activityLog.AppendLine("=== STEP 1: PLAN OF ACTION ===");
        activityLog.AppendLine(planRaw);
        activityLog.AppendLine();

        // ═══════════════════════════════════════════════════════════════════════
        // STEPS 2-5: Single pass of SBR → CBR → Templates → Reports
        // ═══════════════════════════════════════════════════════════════════════
        string sbrRaw = string.Empty;
        string cbrRaw = string.Empty;
        string templateRaw = string.Empty;
        string reportRaw = string.Empty;

        for (var iteration = 0; iteration < 1; iteration++)
        {
            var stepBase = 2 + (iteration * 4);

            // ── SBR pass (C#) ───────────────────────────────────────────────
            onProgress?.Invoke($"Step {stepBase}: SBR analysis (C#)...");
            logger.LogInformation("Blueprint Step{Step} (SBR) | File={File}", stepBase, fileName);

            var sbrMessage = new StringBuilder();
            sbrMessage.AppendLine("=== IMPLEMENTATION PLAN ===");
            sbrMessage.AppendLine(planRaw);
            sbrMessage.AppendLine();
            sbrMessage.AppendLine("=== SHIPEXEC HOOKS REFERENCE ===");
            sbrMessage.AppendLine(hooksDoc);
            sbrMessage.AppendLine();
            if (!string.IsNullOrEmpty(typeRefDoc))
            {
                sbrMessage.AppendLine("=== PSI.SOX TYPE REFERENCE ===");
                sbrMessage.AppendLine(typeRefDoc);
                sbrMessage.AppendLine();
            }
            if (!string.IsNullOrEmpty(typeDefsDoc))
            {
                sbrMessage.AppendLine("=== PSI.SOX TYPE DEFINITIONS ===");
                sbrMessage.AppendLine(typeDefsDoc);
                sbrMessage.AppendLine();
            }
            sbrMessage.AppendLine("=== COMPANY BLUEPRINT ===");
            sbrMessage.AppendLine(blueprintText);

            if (iteration > 0)
            {
                sbrMessage.AppendLine();
                sbrMessage.AppendLine("=== PREVIOUS SBR ANALYSIS (refine and improve this) ===");
                sbrMessage.AppendLine(sbrRaw);
                if (!string.IsNullOrEmpty(cbrRaw))
                {
                    sbrMessage.AppendLine();
                    sbrMessage.AppendLine("=== PREVIOUS CBR ANALYSIS (for context) ===");
                    sbrMessage.AppendLine(cbrRaw);
                }
            }

            sbrRaw = await CallAiAndLog("SBR Analysis", sbrSystemPrompt, sbrMessage.ToString(), true);

            if (sbrRaw.StartsWith("// ⚠️"))
            {
                result.Errors.Add(sbrRaw);
                return result;
            }

            activityLog.AppendLine($"=== STEP {stepBase}: SBR ===");
            activityLog.AppendLine(sbrRaw);
            activityLog.AppendLine();

            // ── CBR pass (JS) ───────────────────────────────────────────────
            onProgress?.Invoke($"Step {stepBase + 1}: CBR analysis (JS)...");
            logger.LogInformation("Blueprint Step{Step} (CBR) | File={File}", stepBase + 1, fileName);

            var cbrMessage = new StringBuilder();
            cbrMessage.AppendLine("=== IMPLEMENTATION PLAN ===");
            cbrMessage.AppendLine(planRaw);
            cbrMessage.AppendLine();
            cbrMessage.AppendLine("=== SHIPEXEC HOOKS REFERENCE ===");
            cbrMessage.AppendLine(hooksDoc);
            cbrMessage.AppendLine();
            if (!string.IsNullOrEmpty(typeRefDoc))
            {
                cbrMessage.AppendLine("=== PSI.SOX TYPE REFERENCE ===");
                cbrMessage.AppendLine(typeRefDoc);
                cbrMessage.AppendLine();
            }
            if (!string.IsNullOrEmpty(typeDefsDoc))
            {
                cbrMessage.AppendLine("=== PSI.SOX TYPE DEFINITIONS ===");
                cbrMessage.AppendLine(typeDefsDoc);
                cbrMessage.AppendLine();
            }
            cbrMessage.AppendLine("=== COMPANY BLUEPRINT ===");
            cbrMessage.AppendLine(blueprintText);
            cbrMessage.AppendLine();
            cbrMessage.AppendLine("=== SBR ANALYSIS (from this iteration, for coordination) ===");
            cbrMessage.AppendLine(sbrRaw);

            if (iteration > 0)
            {
                cbrMessage.AppendLine();
                cbrMessage.AppendLine("=== PREVIOUS CBR ANALYSIS (refine and improve this) ===");
                cbrMessage.AppendLine(cbrRaw);
            }

            cbrRaw = await CallAiAndLog("CBR Analysis", cbrSystemPrompt, cbrMessage.ToString(), true);

            if (cbrRaw.StartsWith("// ⚠️"))
            {
                result.Errors.Add(cbrRaw);
                return result;
            }

            activityLog.AppendLine($"=== STEP {stepBase + 1}: CBR ===");
            activityLog.AppendLine(cbrRaw);
            activityLog.AppendLine();

            // ── Templates pass (HTML) ───────────────────────────────────────
            onProgress?.Invoke($"Step {stepBase + 2}: Template analysis...");
            logger.LogInformation("Blueprint Step{Step} (templates) | File={File}", stepBase + 2, fileName);

            var templateMessage = new StringBuilder();
            templateMessage.AppendLine("=== IMPLEMENTATION PLAN ===");
            templateMessage.AppendLine(planRaw);
            templateMessage.AppendLine();
            templateMessage.AppendLine("=== SHIPEXEC TEMPLATES REFERENCE ===");
            templateMessage.AppendLine(templatesDoc);
            templateMessage.AppendLine();
            if (!string.IsNullOrEmpty(typeRefDoc))
            {
                templateMessage.AppendLine("=== PSI.SOX TYPE REFERENCE ===");
                templateMessage.AppendLine(typeRefDoc);
                templateMessage.AppendLine();
            }
            if (!string.IsNullOrEmpty(typeDefsDoc))
            {
                templateMessage.AppendLine("=== PSI.SOX TYPE DEFINITIONS ===");
                templateMessage.AppendLine(typeDefsDoc);
                templateMessage.AppendLine();
            }
            templateMessage.AppendLine("=== COMPANY BLUEPRINT ===");
            templateMessage.AppendLine(blueprintText);
            templateMessage.AppendLine();
            templateMessage.AppendLine("=== SBR ANALYSIS (for context) ===");
            templateMessage.AppendLine(sbrRaw);
            templateMessage.AppendLine();
            templateMessage.AppendLine("=== CBR ANALYSIS (for context) ===");
            templateMessage.AppendLine(cbrRaw);

            if (iteration > 0)
            {
                templateMessage.AppendLine();
                templateMessage.AppendLine("=== PREVIOUS TEMPLATE ANALYSIS (refine and improve this) ===");
                templateMessage.AppendLine(templateRaw);
            }

            templateRaw = await CallAiAndLog("Template Analysis", templateSystemPrompt, templateMessage.ToString(), true);

            if (templateRaw.StartsWith("// ⚠️"))
            {
                result.Errors.Add(templateRaw);
                return result;
            }

            activityLog.AppendLine($"=== STEP {stepBase + 2}: TEMPLATES ===");
            activityLog.AppendLine(templateRaw);
            activityLog.AppendLine();

            // ── Reports pass ────────────────────────────────────────────────
            onProgress?.Invoke($"Step {stepBase + 3}: Report analysis...");
            logger.LogInformation("Blueprint Step{Step} (reports) | File={File}", stepBase + 3, fileName);

            var reportMessage = new StringBuilder();
            reportMessage.AppendLine("=== IMPLEMENTATION PLAN ===");
            reportMessage.AppendLine(planRaw);
            reportMessage.AppendLine();
            reportMessage.AppendLine("=== COMPANY BLUEPRINT ===");
            reportMessage.AppendLine(blueprintText);
            reportMessage.AppendLine();
            reportMessage.AppendLine("=== SBR ANALYSIS (for context) ===");
            reportMessage.AppendLine(sbrRaw);
            reportMessage.AppendLine();
            reportMessage.AppendLine("=== CBR ANALYSIS (for context) ===");
            reportMessage.AppendLine(cbrRaw);
            reportMessage.AppendLine();
            reportMessage.AppendLine("=== TEMPLATE ANALYSIS (for context) ===");
            reportMessage.AppendLine(templateRaw);

            if (iteration > 0)
            {
                reportMessage.AppendLine();
                reportMessage.AppendLine("=== PREVIOUS REPORT ANALYSIS (refine and improve this) ===");
                reportMessage.AppendLine(reportRaw);
            }

            var reportSystemPrompt =
                """
                You are a ShipExec reporting expert. Given the blueprint, SBR/CBR analysis, and template analysis,
                identify any custom reports, labels, documents, or print-related customizations needed.

                Consider:
                - Custom label formats (ZPL, EPL, thermal printer configurations)
                - Custom packing slips or shipping documents
                - Custom manifest reports or batch reports
                - Print hook integrations (PrePrint, Print, PostPrint)
                - Document templates that need modification

                Return ONLY valid JSON:
                {
                  "reports": [
                    {
                      "name": "<report/label name>",
                      "type": "<label|document|manifest|packingSlip|custom>",
                      "description": "<what it does>",
                      "hookIntegration": "<which print hooks are involved>",
                      "implementation": "<implementation notes>"
                    }
                  ],
                  "printHookCode": {
                    "prePrint": "<C# code for PrePrint if needed, or empty string>",
                    "print": "<C# code for Print if needed, or empty string>",
                    "postPrint": "<C# code for PostPrint if needed, or empty string>"
                  },
                  "noReportsNeeded": <true if blueprint has no custom reporting requirements>
                }
                Do NOT include markdown fences. Return ONLY the JSON object.
                """;

            reportRaw = await CallAiAndLog("Report Analysis", reportSystemPrompt, reportMessage.ToString(), true);

            if (reportRaw.StartsWith("// ⚠️"))
            {
                result.Errors.Add(reportRaw);
                return result;
            }

            activityLog.AppendLine($"=== STEP {stepBase + 3}: REPORTS ===");
            activityLog.AppendLine(reportRaw);
            activityLog.AppendLine();
        }

        result.HooksAnalysis = $"=== SBR (C#) ===\n{sbrRaw}\n\n=== CBR (JS) ===\n{cbrRaw}";
        result.TemplatesAnalysis = templateRaw;
        result.ReportsAnalysis = reportRaw;

        // ── Copy template project to timestamped output folder ─────────────
        onProgress?.Invoke("Copying template project...");

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputRoot = Path.Combine(env.ContentRootPath, "..", "BlueprintOutput");
        var outputFolder = Path.Combine(outputRoot, $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}");
        Directory.CreateDirectory(outputFolder);

        CopyDirectory(templateProjectPath, outputFolder, skipTemplates: true);
        result.OutputFolder = Path.GetFullPath(outputFolder);

        // ── Remove .md prompt references from .csproj (they stay in CodeStandards, not output) ──
        await RemoveMdFromCsproj(outputFolder, ct);

        // ── Copy PSI.Sox DLLs to References folder and update .csproj ───────
        onProgress?.Invoke("Setting up References folder...");
        var workspaceRefsPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "References"));
        await CopyReferenceDlls(outputFolder, workspaceRefsPath, ct);

        // ── Save blueprint document to output folder ────────────────────────
        var blueprintOutputPath = Path.Combine(outputFolder, fileName);
        await File.WriteAllTextAsync(blueprintOutputPath, blueprintText, ct);
        await AddSolutionItemToCsproj(outputFolder, fileName, ct);

        // ── Generate README.md ──────────────────────────────────────────────
        onProgress?.Invoke("Generating README.md...");
        var readmeSystemPrompt =
            """
            You are a technical documentation writer. Given a blueprint document and analysis results,
            create a detailed README.md that explains:
            1. A summary of the blueprint's business requirements
            2. How the blueprint was translated into code (methodology)
            3. Code flow — how SBR hooks, CBR hooks, and templates interact
            4. Design patterns used (manager classes, delegation, etc.)
            5. File-by-file breakdown of what was generated and why
            6. Suggestions for testing, deployment, and future enhancements
            7. Any caveats or manual steps needed

            Write in clear markdown with headers, bullet points, and code references where helpful.
            Do NOT include markdown code fences around the entire response — return raw markdown directly.
            """;

        var readmeMessage = new StringBuilder();
        readmeMessage.AppendLine("=== BLUEPRINT DOCUMENT ===");
        readmeMessage.AppendLine(blueprintText);
        readmeMessage.AppendLine();
        readmeMessage.AppendLine("=== IMPLEMENTATION PLAN ===");
        readmeMessage.AppendLine(planRaw);
        readmeMessage.AppendLine();
        readmeMessage.AppendLine("=== SBR ANALYSIS ===");
        readmeMessage.AppendLine(sbrRaw);
        readmeMessage.AppendLine();
        readmeMessage.AppendLine("=== CBR ANALYSIS ===");
        readmeMessage.AppendLine(cbrRaw);
        readmeMessage.AppendLine();
        readmeMessage.AppendLine("=== TEMPLATE ANALYSIS ===");
        readmeMessage.AppendLine(templateRaw);
        readmeMessage.AppendLine();
        readmeMessage.AppendLine("=== REPORTS ANALYSIS ===");
        readmeMessage.AppendLine(reportRaw);

        var readmeContent = await CallAiAndLog("Generate README.md", readmeSystemPrompt, readmeMessage.ToString(), false);
        if (!readmeContent.StartsWith("// ⚠️"))
        {
            await File.WriteAllTextAsync(Path.Combine(outputFolder, "README.md"), readmeContent, ct);
            result.ModifiedFiles.Add("README.md");
            await AddSolutionItemToCsproj(outputFolder, "README.md", ct);
        }

        // ── Apply SBR changes (C#) ─────────────────────────────────────────
        onProgress?.Invoke("Applying SBR changes...");

        try
        {
            using var sbrDoc = JsonDocument.Parse(sbrRaw);
            var sbrRoot = sbrDoc.RootElement;

            if (sbrRoot.TryGetProperty("sbrMethods", out var sbrMethods))
            {
                var sbrPath = Path.Combine(outputFolder, "SoxBusinessRules.cs");
                if (File.Exists(sbrPath))
                {
                    var sbrContent = await File.ReadAllTextAsync(sbrPath, ct);
                    sbrContent = ApplySbrMethods(sbrContent, sbrMethods);
                    await File.WriteAllTextAsync(sbrPath, sbrContent, ct);
                    result.ModifiedFiles.Add("SoxBusinessRules.cs");
                }
            }

            // Write helper/manager classes if provided
            if (sbrRoot.TryGetProperty("helperClasses", out var helpers))
            {
                var helperText = helpers.GetString();
                if (!string.IsNullOrWhiteSpace(helperText))
                {
                    // Split into individual class files by detecting class declarations
                    var classFiles = SplitHelperClasses(helperText);
                    foreach (var (className, classContent) in classFiles)
                    {
                        // Never overwrite the existing Tools.cs — it is copied verbatim from the template
                        if (string.Equals(className, "Tools", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var helperFileName = $"{className}.cs";
                        var helperPath = Path.Combine(outputFolder, helperFileName);
                        await File.WriteAllTextAsync(helperPath, classContent, ct);
                        result.ModifiedFiles.Add(helperFileName);

                        // Add to .csproj if not already present
                        await AddCompileItemToCsproj(outputFolder, helperFileName, ct);
                    }

                    // Detect the custom namespace from helper classes and add using to SoxBusinessRules.cs
                    var nsMatch = System.Text.RegularExpressions.Regex.Match(helperText, @"namespace\s+(ShipExec\.\w+)");
                    if (nsMatch.Success)
                    {
                        var customNs = nsMatch.Groups[1].Value;
                        var sbrPath2 = Path.Combine(outputFolder, "SoxBusinessRules.cs");
                        if (File.Exists(sbrPath2))
                        {
                            var sbrContent2 = await File.ReadAllTextAsync(sbrPath2, ct);
                            var usingLine = $"using {customNs};";
                            if (!sbrContent2.Contains(usingLine, StringComparison.Ordinal))
                            {
                                // Insert the using statement after the last existing using line
                                var lastUsing = sbrContent2.LastIndexOf("using ", sbrContent2.IndexOf("namespace", StringComparison.Ordinal), StringComparison.Ordinal);
                                if (lastUsing >= 0)
                                {
                                    var endOfLine = sbrContent2.IndexOf('\n', lastUsing);
                                    if (endOfLine >= 0)
                                    {
                                        sbrContent2 = sbrContent2[..(endOfLine + 1)] + usingLine + "\r\n" + sbrContent2[(endOfLine + 1)..];
                                    }
                                }
                                await File.WriteAllTextAsync(sbrPath2, sbrContent2, ct);
                            }
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning("SBR JSON parse error: {Error}", ex.Message);
            result.Errors.Add($"SBR response was not valid JSON: {ex.Message}");
        }

        // ── Apply CBR changes (JS) ──────────────────────────────────────────
        onProgress?.Invoke("Applying CBR changes...");

        try
        {
            using var cbrDoc = JsonDocument.Parse(cbrRaw);
            var cbrRoot = cbrDoc.RootElement;

            if (cbrRoot.TryGetProperty("cbrMethods", out var cbrMethods))
            {
                var cbrPath = Path.Combine(outputFolder, "CBR", "ClientBusinessRulesTemplate.js");
                if (File.Exists(cbrPath))
                {
                    var cbrContent = BuildCbrScript(cbrMethods);
                    await File.WriteAllTextAsync(cbrPath, cbrContent, ct);
                    result.ModifiedFiles.Add("CBR/ClientBusinessRulesTemplate.js");
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning("CBR JSON parse error: {Error}", ex.Message);
            result.Errors.Add($"CBR response was not valid JSON: {ex.Message}");
        }

        // ── Apply Template changes (HTML) ─────────────────────────────────
        onProgress?.Invoke("Applying template changes...");

        try
        {
            using var templateDoc = JsonDocument.Parse(templateRaw);
            var templateRoot = templateDoc.RootElement;

            if (templateRoot.TryGetProperty("templateChanges", out var changes))
            {
                foreach (var change in changes.EnumerateArray())
                {
                    var file = change.GetProperty("file").GetString();
                    var content = change.GetProperty("fullContent").GetString();
                    if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(content))
                        continue;

                    var templatePath = Path.Combine(outputFolder, "Templates", file);
                    Directory.CreateDirectory(Path.GetDirectoryName(templatePath)!);
                    await File.WriteAllTextAsync(templatePath, content, ct);
                    result.ModifiedFiles.Add($"Templates/{file}");
                }
            }

            // Append any additional CBR from template pass
            if (templateRoot.TryGetProperty("cbrAdditions", out var cbrAdditions))
            {
                var additionsText = cbrAdditions.GetString();
                if (!string.IsNullOrWhiteSpace(additionsText))
                {
                    var cbrPath = Path.Combine(outputFolder, "CBR", "ClientBusinessRulesTemplate.js");
                    if (File.Exists(cbrPath))
                    {
                        var existing = await File.ReadAllTextAsync(cbrPath, ct);
                        // Insert additional methods before the closing brace
                        var insertPos = existing.LastIndexOf('}');
                        if (insertPos > 0)
                        {
                            existing = existing[..insertPos] + "\n" + additionsText + "\n}\n";
                            await File.WriteAllTextAsync(cbrPath, existing, ct);
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Pass2 JSON parse error: {Error}", ex.Message);
            result.Errors.Add($"Pass 2 response was not valid JSON: {ex.Message}");
        }

        // ── Add template Content references to .csproj for only the templates that were generated ──
        await AddTemplateContentItemsToCsproj(outputFolder, ct);

        // ── CBR Validation + Repair (multi-turn conversation) ────────────────
        const int maxCbrRepairAttempts = 7;
        var cbrRepairMessages = new List<object>
        {
            new { role = "system", content = "You are a JavaScript repair agent. Fix the validation errors in the CBR files. Do NOT change method signatures. Do NOT remove any existing comments — preserve ALL comments and add more if needed. Every method must have a block comment explaining its purpose, the blueprint requirement it fulfills, and numbered step comments inside the body. Return JSON: { \"fixes\": [{ \"file\": \"<relative path>\", \"fullContent\": \"<complete corrected file>\" }] }. No markdown fences." },
            new { role = "user", content = $"Here is the blueprint context and plan for reference:\n\n=== PLAN ===\n{planRaw}\n\nI will send you validation errors and the affected files. Fix them and return the corrected files." },
            new { role = "assistant", content = (object)"Understood. Send me the validation errors and affected CBR files, and I will return the corrected versions as JSON." }
        };

        for (var cbrAttempt = 0; cbrAttempt <= maxCbrRepairAttempts; cbrAttempt++)
        {
            onProgress?.Invoke(cbrAttempt == 0
                ? "CBR Validation: checking JavaScript validity..."
                : $"CBR re-validation after repair {cbrAttempt}/{maxCbrRepairAttempts}...");

            var cbrValidationSb = new StringBuilder();
            var cbrDir2 = Path.Combine(outputFolder, "CBR");
            if (Directory.Exists(cbrDir2))
            {
                foreach (var jsFile in Directory.GetFiles(cbrDir2, "*.js"))
                {
                    var js = await File.ReadAllTextAsync(jsFile, ct);
                    var fname = Path.GetFileName(jsFile);

                    var openBraces = js.Count(c => c == '{');
                    var closeBraces = js.Count(c => c == '}');
                    if (openBraces != closeBraces)
                        cbrValidationSb.AppendLine($"⚠️ {fname}: brace mismatch (open={openBraces}, close={closeBraces})");

                    var openParens = js.Count(c => c == '(');
                    var closeParens = js.Count(c => c == ')');
                    if (openParens != closeParens)
                        cbrValidationSb.AppendLine($"⚠️ {fname}: parenthesis mismatch (open={openParens}, close={closeParens})");

                    var openBrackets = js.Count(c => c == '[');
                    var closeBrackets = js.Count(c => c == ']');
                    if (openBrackets != closeBrackets)
                        cbrValidationSb.AppendLine($"⚠️ {fname}: bracket mismatch (open={openBrackets}, close={closeBrackets})");

                    await ValidateJsWithNode(jsFile, fname, cbrValidationSb, ct);
                }
            }

            if (!cbrValidationSb.ToString().Contains("⚠️"))
                break;

            if (cbrAttempt == maxCbrRepairAttempts)
            {
                result.Errors.Add("CBR validation failed after max repair attempts.");
                result.CbrValidationErrors = cbrValidationSb.ToString();
                result.ValidationOutput += "\n" + cbrValidationSb.ToString();
                break;
            }

            // Build targeted user message with only errors + affected files
            activityLog.AppendLine($"=== CBR VALIDATION ATTEMPT {cbrAttempt + 1} — ERRORS ===\n{cbrValidationSb}");
            var cbrRepairUserMsg = new StringBuilder();
            cbrRepairUserMsg.AppendLine("=== VALIDATION ERRORS (fix these) ===");
            cbrRepairUserMsg.AppendLine(cbrValidationSb.ToString());
            cbrRepairUserMsg.AppendLine("Fix ALL JavaScript issues. Do NOT change method signatures — only fix the body code.");
            cbrRepairUserMsg.AppendLine();

            // Include only the files that have errors
            if (Directory.Exists(cbrDir2))
            {
                var errorText = cbrValidationSb.ToString();
                foreach (var jsFile in Directory.GetFiles(cbrDir2, "*.js"))
                {
                    var fname = Path.GetFileName(jsFile);
                    if (errorText.Contains(fname))
                    {
                        cbrRepairUserMsg.AppendLine($"=== FILE: {fname} ===");
                        cbrRepairUserMsg.AppendLine(await File.ReadAllTextAsync(jsFile, ct));
                        cbrRepairUserMsg.AppendLine();
                    }
                }
            }

            // Add to multi-turn conversation and call AI
            cbrRepairMessages.Add(new { role = "user", content = (object)cbrRepairUserMsg.ToString() });

            result.AiInteractions.Add(new AiInteractionLog
            {
                Step = $"CBR Repair (attempt {cbrAttempt + 1})",
                PromptSent = $"[MULTI-TURN message #{cbrRepairMessages.Count}]\n{cbrRepairUserMsg}",
                ResponseReceived = "(pending...)",
                Timestamp = DateTime.Now
            });

            var cbrRepairRaw = await CallAzureOpenAiMultiTurnAsync(deployment, endpoint, apiKey, cbrRepairMessages, true, ct);
            result.AiInteractions[^1].ResponseReceived = cbrRepairRaw;

            // Add assistant response to history for next iteration
            cbrRepairMessages.Add(new { role = "assistant", content = (object)cbrRepairRaw });

            if (!cbrRepairRaw.StartsWith("// ⚠️"))
            {
                try
                {
                    using var repDoc = JsonDocument.Parse(cbrRepairRaw);
                    if (repDoc.RootElement.TryGetProperty("fixes", out var fixes))
                    {
                        foreach (var fix in fixes.EnumerateArray())
                        {
                            var file = fix.GetProperty("file").GetString();
                            var content = fix.GetProperty("fullContent").GetString();
                            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(content)) continue;
                            if (IsImmutableFile(file)) continue;
                            var fixPath = Path.Combine(outputFolder, file.Replace('/', Path.DirectorySeparatorChar));
                            Directory.CreateDirectory(Path.GetDirectoryName(fixPath)!);
                            await File.WriteAllTextAsync(fixPath, content, ct);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    activityLog.AppendLine($"CBR repair {cbrAttempt + 1} JSON error: {ex.Message}");
                }
            }
        }


        // ── Template/Report HTML Validation + Repair (multi-turn) ────────────────
        const int maxHtmlRepairAttempts = 7;
        var htmlRepairMessages = new List<object>
        {
            new { role = "system", content = "You are an HTML repair agent. Fix the validation errors in the template/report HTML files. Do NOT remove any existing HTML comments — preserve ALL comments. Return JSON: { \"fixes\": [{ \"file\": \"<relative path>\", \"fullContent\": \"<complete corrected file>\" }] }. No markdown fences." },
            new { role = "user", content = (object)"I will send you HTML validation errors and the affected template files. Fix the tag balancing issues and return corrected files." },
            new { role = "assistant", content = (object)"Understood. Send me the validation errors and affected HTML files, and I will return the corrected versions as JSON." }
        };

        for (var htmlAttempt = 0; htmlAttempt <= maxHtmlRepairAttempts; htmlAttempt++)
        {
            onProgress?.Invoke(htmlAttempt == 0
                ? "Template/Report Validation: checking HTML validity..."
                : $"Template/Report re-validation after repair {htmlAttempt}/{maxHtmlRepairAttempts}...");

            var htmlValidationSb = new StringBuilder();
            var templatesDir2 = Path.Combine(outputFolder, "Templates");
            if (Directory.Exists(templatesDir2))
            {
                foreach (var htmlFile in Directory.GetFiles(templatesDir2, "*.html"))
                {
                    var html = await File.ReadAllTextAsync(htmlFile, ct);
                    var fname = Path.GetFileName(htmlFile);

                    var openDivs = CountOccurrences(html, "<div");
                    var closeDivs = CountOccurrences(html, "</div>");
                    if (openDivs != closeDivs)
                        htmlValidationSb.AppendLine($"⚠️ {fname}: <div> mismatch (open={openDivs}, close={closeDivs})");

                    var openSpans = CountOccurrences(html, "<span");
                    var closeSpans = CountOccurrences(html, "</span>");
                    if (openSpans != closeSpans)
                        htmlValidationSb.AppendLine($"⚠️ {fname}: <span> mismatch (open={openSpans}, close={closeSpans})");

                    var openTr = CountOccurrences(html, "<tr");
                    var closeTr = CountOccurrences(html, "</tr>");
                    if (openTr != closeTr)
                        htmlValidationSb.AppendLine($"⚠️ {fname}: <tr> mismatch (open={openTr}, close={closeTr})");

                    var openTabset = CountOccurrences(html, "<tabset");
                    var closeTabset = CountOccurrences(html, "</tabset>");
                    if (openTabset != closeTabset)
                        htmlValidationSb.AppendLine($"⚠️ {fname}: <tabset> mismatch (open={openTabset}, close={closeTabset})");

                    var openTab = CountOccurrences(html, "<tab ");
                    var closeTab = CountOccurrences(html, "</tab>");
                    if (openTab != closeTab)
                        htmlValidationSb.AppendLine($"⚠️ {fname}: <tab> mismatch (open={openTab}, close={closeTab})");

                    if (html.Contains("ng-=\"") || html.Contains("ng-=\'"))
                        htmlValidationSb.AppendLine($"⚠️ {fname}: contains broken ng- attribute binding");
                }
            }

            if (!htmlValidationSb.ToString().Contains("⚠️"))
                break;

            if (htmlAttempt == maxHtmlRepairAttempts)
            {
                result.Errors.Add("Template/Report HTML validation failed after max repair attempts.");
                result.HtmlValidationErrors = htmlValidationSb.ToString();
                result.ValidationOutput += "\n" + htmlValidationSb.ToString();
                break;
            }

            // Build targeted user message with only errors + affected files
            activityLog.AppendLine($"=== HTML VALIDATION ATTEMPT {htmlAttempt + 1} — ERRORS ===\n{htmlValidationSb}");
            var htmlRepairUserMsg = new StringBuilder();
            htmlRepairUserMsg.AppendLine("=== VALIDATION ERRORS (fix these) ===");
            htmlRepairUserMsg.AppendLine(htmlValidationSb.ToString());
            htmlRepairUserMsg.AppendLine("Fix ALL HTML issues. Ensure all tags are properly balanced.");
            htmlRepairUserMsg.AppendLine();

            if (Directory.Exists(templatesDir2))
            {
                var errorText = htmlValidationSb.ToString();
                foreach (var htmlFile in Directory.GetFiles(templatesDir2, "*.html"))
                {
                    var fname = Path.GetFileName(htmlFile);
                    if (errorText.Contains(fname))
                    {
                        htmlRepairUserMsg.AppendLine($"=== FILE: Templates/{fname} ===");
                        htmlRepairUserMsg.AppendLine(await File.ReadAllTextAsync(htmlFile, ct));
                        htmlRepairUserMsg.AppendLine();
                    }
                }
            }

            // Add to multi-turn conversation and call AI
            htmlRepairMessages.Add(new { role = "user", content = (object)htmlRepairUserMsg.ToString() });

            result.AiInteractions.Add(new AiInteractionLog
            {
                Step = $"HTML Repair (attempt {htmlAttempt + 1})",
                PromptSent = $"[MULTI-TURN message #{htmlRepairMessages.Count}]\n{htmlRepairUserMsg}",
                ResponseReceived = "(pending...)",
                Timestamp = DateTime.Now
            });

            var htmlRepairRaw = await CallAzureOpenAiMultiTurnAsync(deployment, endpoint, apiKey, htmlRepairMessages, true, ct);
            result.AiInteractions[^1].ResponseReceived = htmlRepairRaw;

            // Add assistant response to history for next iteration
            htmlRepairMessages.Add(new { role = "assistant", content = (object)htmlRepairRaw });

            if (!htmlRepairRaw.StartsWith("// ⚠️"))
            {
                try
                {
                    using var repDoc = JsonDocument.Parse(htmlRepairRaw);
                    if (repDoc.RootElement.TryGetProperty("fixes", out var fixes))
                    {
                        foreach (var fix in fixes.EnumerateArray())
                        {
                            var file = fix.GetProperty("file").GetString();
                            var content = fix.GetProperty("fullContent").GetString();
                            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(content)) continue;
                            if (IsImmutableFile(file)) continue;
                            var fixPath = Path.Combine(outputFolder, file.Replace('/', Path.DirectorySeparatorChar));
                            Directory.CreateDirectory(Path.GetDirectoryName(fixPath)!);
                            await File.WriteAllTextAsync(fixPath, content, ct);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    activityLog.AppendLine($"HTML repair {htmlAttempt + 1} JSON error: {ex.Message}");
                }
            }
        }

        // ── Project Validation + Repair Loop (multi-turn conversation) ──────────
        const int maxRepairAttempts = 7;
        var projectRepairSystemPrompt =
            """
            You are a code repair agent. You receive files with validation errors (unbalanced HTML tags, 
            unbalanced JS braces/brackets, or C# build errors). 

            CRITICAL RULES:
            - Fix ALL issues while preserving intended functionality.
            - NEVER remove existing comments — preserve ALL comments in C#, JS, and HTML files. If a fix changes code, UPDATE the associated comments to match but NEVER delete them.
            - NEVER remove any existing using statements from any C# file. Only ADD using statements if needed — never delete them.
            - NEVER alter these template files — they must remain unchanged: CreateBatchRequest.cs, DataService.cs, LoadShipment.cs, Tools.cs. If a fix references them, skip them.
            - If a method has no comments, ADD comments explaining the business logic, program flow, and blueprint requirement.
            - NEVER change C# method signatures in SoxBusinessRules.cs — they implement IBusinessObject and are immutable.
            - All business logic belongs in a separate Manager class. SoxBusinessRules only delegates to it.
            - Ensure all HTML tags are properly balanced (every <div> has </div>, etc.)
            - Ensure all JS braces {}, parentheses (), and brackets [] are balanced.
            - For C# build errors, fix the code so it compiles. Use correct types and parameter names.
            - THINK HARD ABOUT TYPE MATCHING: The method signatures are the SOURCE OF TRUTH for types. If a signature says PackageRequest, use PackageRequest — NOT Package, NOT PackageInfo. If it says ShipmentRequest, use ShipmentRequest — NOT Shipment. Cross-reference EVERY type against the actual signatures.
            - Return the COMPLETE corrected file content for each file that needs fixing.

            Return ONLY valid JSON with this structure:
            {
              "fixes": [
                { "file": "<relative path>", "fullContent": "<complete corrected file content>" }
              ]
            }
            Do NOT include markdown fences. Return ONLY the JSON object.
            """;

        var projectRepairMessages = new List<object>
        {
            new { role = "system", content = projectRepairSystemPrompt },
            new { role = "user", content = (object)$"Here is the project context:\n\n=== PLAN ===\n{planRaw}\n\nI will send you build/validation errors and the affected files. Fix them and return the corrected files." },
            new { role = "assistant", content = (object)"Understood. Send me the validation errors and affected files, and I will return the corrected versions as JSON." }
        };

        for (var attempt = 0; attempt <= maxRepairAttempts; attempt++)
        {
            onProgress?.Invoke(attempt == 0
                ? "Project Validation: building generated project..."
                : $"Re-validating after repair attempt {attempt}/{maxRepairAttempts}...");

            var validationSb = new StringBuilder();

            // Validate HTML templates
            var templatesDir = Path.Combine(outputFolder, "Templates");
            if (Directory.Exists(templatesDir))
            {
                foreach (var htmlFile in Directory.GetFiles(templatesDir, "*.html"))
                {
                    var html = await File.ReadAllTextAsync(htmlFile, ct);
                    var fname = Path.GetFileName(htmlFile);

                    var openDivs = CountOccurrences(html, "<div");
                    var closeDivs = CountOccurrences(html, "</div>");
                    if (openDivs != closeDivs)
                        validationSb.AppendLine($"⚠️ {fname}: <div> mismatch (open={openDivs}, close={closeDivs})");

                    var openSpans = CountOccurrences(html, "<span");
                    var closeSpans = CountOccurrences(html, "</span>");
                    if (openSpans != closeSpans)
                        validationSb.AppendLine($"⚠️ {fname}: <span> mismatch (open={openSpans}, close={closeSpans})");

                    var openTr = CountOccurrences(html, "<tr");
                    var closeTr = CountOccurrences(html, "</tr>");
                    if (openTr != closeTr)
                        validationSb.AppendLine($"⚠️ {fname}: <tr> mismatch (open={openTr}, close={closeTr})");

                    var openTabset = CountOccurrences(html, "<tabset");
                    var closeTabset = CountOccurrences(html, "</tabset>");
                    if (openTabset != closeTabset)
                        validationSb.AppendLine($"⚠️ {fname}: <tabset> mismatch (open={openTabset}, close={closeTabset})");

                    var openTab = CountOccurrences(html, "<tab ");
                    var closeTab = CountOccurrences(html, "</tab>");
                    if (openTab != closeTab)
                        validationSb.AppendLine($"⚠️ {fname}: <tab> mismatch (open={openTab}, close={closeTab})");

                    if (html.Contains("ng-=\"") || html.Contains("ng-=\'"))
                        validationSb.AppendLine($"⚠️ {fname}: contains broken ng- attribute binding");
                }
            }

            // Validate JS files
            var cbrDir = Path.Combine(outputFolder, "CBR");
            if (Directory.Exists(cbrDir))
            {
                foreach (var jsFile in Directory.GetFiles(cbrDir, "*.js"))
                {
                    var js = await File.ReadAllTextAsync(jsFile, ct);
                    var fname = Path.GetFileName(jsFile);

                    var openBraces = js.Count(c => c == '{');
                    var closeBraces = js.Count(c => c == '}');
                    if (openBraces != closeBraces)
                        validationSb.AppendLine($"⚠️ {fname}: brace mismatch (open={openBraces}, close={closeBraces})");

                    var openParens = js.Count(c => c == '(');
                    var closeParens = js.Count(c => c == ')');
                    if (openParens != closeParens)
                        validationSb.AppendLine($"⚠️ {fname}: parenthesis mismatch (open={openParens}, close={closeParens})");

                    var openBrackets = js.Count(c => c == '[');
                    var closeBrackets = js.Count(c => c == ']');
                    if (openBrackets != closeBrackets)
                        validationSb.AppendLine($"⚠️ {fname}: bracket mismatch (open={openBrackets}, close={closeBrackets})");

                    await ValidateJsWithNode(jsFile, fname, validationSb, ct);
                }
            }

            // Validate C# - attempt dotnet build if .csproj exists
            string buildOutput = string.Empty;
            var csprojFiles = Directory.GetFiles(outputFolder, "*.csproj");
            if (csprojFiles.Length > 0)
            {
                onProgress?.Invoke(attempt == 0 ? "Building generated C# project..." : $"Re-building C# project (attempt {attempt + 1})...");
                try
                {
                    var psi = new ProcessStartInfo("dotnet", $"build \"{csprojFiles[0]}\" --nologo --verbosity quiet")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc is not null)
                    {
                        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
                        var stderr = await proc.StandardError.ReadToEndAsync(ct);
                        await proc.WaitForExitAsync(ct);

                        if (proc.ExitCode != 0)
                        {
                            buildOutput = stderr.Length > 0 ? stderr : stdout;
                            validationSb.AppendLine("⚠️ C# build failed:");
                            validationSb.AppendLine(buildOutput);
                            result.BuildErrors = buildOutput;
                        }
                        else
                        {
                            validationSb.AppendLine("✅ C# project built successfully.");
                            result.BuildErrors = string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    validationSb.AppendLine($"⚠️ Could not run dotnet build: {ex.Message}");
                }
            }
            else
            {
                foreach (var csFile in Directory.GetFiles(outputFolder, "*.cs"))
                {
                    var cs = await File.ReadAllTextAsync(csFile, ct);
                    var fname = Path.GetFileName(csFile);
                    var openBraces = cs.Count(c => c == '{');
                    var closeBraces = cs.Count(c => c == '}');
                    if (openBraces != closeBraces)
                        validationSb.AppendLine($"⚠️ {fname}: C# brace mismatch (open={openBraces}, close={closeBraces})");
                }
            }

            var hasIssues = validationSb.ToString().Contains("⚠️");

            if (!hasIssues)
            {
                validationSb.AppendLine("✅ All validations passed.");
                result.ValidationOutput = validationSb.ToString();
                result.ValidationPassed = true;
                break;
            }

            result.ValidationOutput = validationSb.ToString();
            result.ValidationPassed = false;

            // If we've exhausted repair attempts, stop
            if (attempt >= maxRepairAttempts)
            {
                result.Errors.Add($"Validation still failing after {maxRepairAttempts} repair attempts.");
                break;
            }

            // ── Repair: send only errors + targeted files via multi-turn ─────────
            onProgress?.Invoke($"Repair pass {attempt + 1}/{maxRepairAttempts}: fixing validation issues...");

            activityLog.AppendLine($"=== REPAIR ATTEMPT {attempt + 1} — ERRORS FOUND ===");
            activityLog.AppendLine(validationSb.ToString());
            activityLog.AppendLine();

            var repairUserMsg = new StringBuilder();
            repairUserMsg.AppendLine("=== VALIDATION ERRORS (fix these) ===");
            repairUserMsg.AppendLine(validationSb.ToString());
            repairUserMsg.AppendLine();
            repairUserMsg.AppendLine("You MUST fix ALL issues. The project MUST build and all HTML/JS must be syntactically valid.");
            repairUserMsg.AppendLine("Do NOT change method signatures in C# files — only fix the method bodies and ensure braces/syntax are correct.");
            repairUserMsg.AppendLine();

            // For C# build failures, use ExtractFilesFromBuildOutput to target only affected files
            var erroredCsFiles = !string.IsNullOrEmpty(buildOutput)
                ? ExtractFilesFromBuildOutput(buildOutput)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(templatesDir))
            {
                var errorText = validationSb.ToString();
                foreach (var htmlFile in Directory.GetFiles(templatesDir, "*.html"))
                {
                    var fname = Path.GetFileName(htmlFile);
                    if (errorText.Contains(fname))
                    {
                        repairUserMsg.AppendLine($"=== FILE: Templates/{fname} ===");
                        repairUserMsg.AppendLine(await File.ReadAllTextAsync(htmlFile, ct));
                        repairUserMsg.AppendLine();
                    }
                }
            }

            if (Directory.Exists(cbrDir))
            {
                var errorText = validationSb.ToString();
                foreach (var jsFile in Directory.GetFiles(cbrDir, "*.js"))
                {
                    var fname = Path.GetFileName(jsFile);
                    if (errorText.Contains(fname))
                    {
                        repairUserMsg.AppendLine($"=== FILE: CBR/{fname} ===");
                        repairUserMsg.AppendLine(await File.ReadAllTextAsync(jsFile, ct));
                        repairUserMsg.AppendLine();
                    }
                }
            }

            // Include only C# files referenced in build errors (or all if none parsed)
            foreach (var csFile in Directory.GetFiles(outputFolder, "*.cs"))
            {
                var fname = Path.GetFileName(csFile);
                if (erroredCsFiles.Count == 0 || erroredCsFiles.Contains(fname))
                {
                    var content = await File.ReadAllTextAsync(csFile, ct);
                    repairUserMsg.AppendLine($"=== FILE: {fname} ===");
                    repairUserMsg.AppendLine(content);
                    repairUserMsg.AppendLine();
                }
            }

            // Add to multi-turn conversation and call AI
            projectRepairMessages.Add(new { role = "user", content = (object)repairUserMsg.ToString() });

            result.AiInteractions.Add(new AiInteractionLog
            {
                Step = $"Project Repair (attempt {attempt + 1})",
                PromptSent = $"[MULTI-TURN message #{projectRepairMessages.Count}]\n{repairUserMsg}",
                ResponseReceived = "(pending...)",
                Timestamp = DateTime.Now
            });

            var repairRaw = await CallAzureOpenAiMultiTurnAsync(deployment, endpoint, apiKey, projectRepairMessages, true, ct);
            result.AiInteractions[^1].ResponseReceived = repairRaw;

            // Add assistant response to history for next iteration
            projectRepairMessages.Add(new { role = "assistant", content = (object)repairRaw });

            if (!repairRaw.StartsWith("// ⚠️"))
            {
                try
                {
                    using var repairDoc = JsonDocument.Parse(repairRaw);
                    if (repairDoc.RootElement.TryGetProperty("fixes", out var fixes))
                    {
                        foreach (var fix in fixes.EnumerateArray())
                        {
                            var file = fix.GetProperty("file").GetString();
                            var content = fix.GetProperty("fullContent").GetString();
                            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(content))
                                continue;
                            if (IsImmutableFile(file))
                                continue;

                            var fixPath = Path.Combine(outputFolder, file.Replace('/', Path.DirectorySeparatorChar));
                            Directory.CreateDirectory(Path.GetDirectoryName(fixPath)!);
                            await File.WriteAllTextAsync(fixPath, content, ct);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogWarning("Repair pass {Attempt} JSON parse error: {Error}", attempt + 1, ex.Message);
                    result.Errors.Add($"Repair pass {attempt + 1} response was not valid JSON: {ex.Message}");
                    activityLog.AppendLine($"=== REPAIR {attempt + 1} FAILED: JSON parse error: {ex.Message} ===");
                    activityLog.AppendLine();
                }
            }
            else
            {
                result.Errors.Add($"Repair pass {attempt + 1} API error: {repairRaw}");
                activityLog.AppendLine($"=== REPAIR {attempt + 1} FAILED: API error ===");
                activityLog.AppendLine(repairRaw);
                activityLog.AppendLine();
            }
        }

        // ── Generate analysis_chat.html ─────────────────────────────────────
        onProgress?.Invoke("Generating analysis chat log...");
        var chatHtml = BuildChatLogHtml(result.AiInteractions, fileName);
        var chatPath = Path.Combine(outputFolder, "analysis_chat.html");
        await File.WriteAllTextAsync(chatPath, chatHtml, ct);
        result.ModifiedFiles.Add("analysis_chat.html");
        await AddSolutionItemToCsproj(outputFolder, "analysis_chat.html", ct);

        onProgress?.Invoke("Complete!");
        logger.LogInformation("Blueprint analysis complete | Output={Output} Files={Count} Valid={Valid}",
            result.OutputFolder, result.ModifiedFiles.Count, result.ValidationPassed);

        return result;
    }

    // ── Apply SBR method bodies into the existing SoxBusinessRules.cs ────────
    private static string ApplySbrMethods(string sbrContent, JsonElement sbrMethods)
    {
        foreach (var prop in sbrMethods.EnumerateObject())
        {
            var methodName = prop.Name;
            var methodBody = prop.Value.GetString();
            if (string.IsNullOrWhiteSpace(methodBody)) continue;

            // Find the method and replace its body
            // Pattern: look for the method signature region and replace empty body
            // This is a simplified approach - looks for the method with empty braces
            var patterns = new[]
            {
                $"public ShipmentRequest {methodName}(",
                $"public void {methodName}(",
                $"public ShipmentResponse {methodName}(",
                $"public List<ShipmentResponse> {methodName}(",
                $"public List<BatchReference> {methodName}(",
                $"public BatchRequest {methodName}(",
                $"public Package {methodName}(",
                $"public CloseManifestResult {methodName}(",
                $"public List<TransmitItemResult> {methodName}(",
                $"public DocumentResponse {methodName}(",
                $"public string {methodName}(",
                $"public object {methodName}(",
                $"public List<BoxType> {methodName}(",
                $"public List<CommodityContent> {methodName}(",
                $"public List<HazmatContent> {methodName}(",
                $"public List<ShipmentRequest> {methodName}(",
                $"public List<NameAddressValidationCandidate> {methodName}("
            };

            foreach (var pattern in patterns)
            {
                var methodStart = sbrContent.IndexOf(pattern, StringComparison.Ordinal);
                if (methodStart < 0) continue;

                // Find the opening brace of the method body
                var braceStart = sbrContent.IndexOf('{', methodStart);
                if (braceStart < 0) continue;

                // Find the matching closing brace
                var braceEnd = FindMatchingBrace(sbrContent, braceStart);
                if (braceEnd < 0) continue;

                // Replace content between braces
                var indent = "        ";
                var formattedBody = string.Join("\n",
                    methodBody.Split('\n').Select(line => indent + "    " + line.TrimStart()));

                sbrContent = sbrContent[..(braceStart + 1)]
                    + "\n" + formattedBody + "\n" + indent
                    + sbrContent[braceEnd..];
                break;
            }
        }

        return sbrContent;
    }

    // ── Build complete CBR script from method definitions ─────────────────────
    private static string BuildCbrScript(JsonElement cbrMethods)
    {
        // All CBR methods that must appear in the output file (even if empty)
        var allMethods = new[]
        {
            "PageLoaded", "NewShipment", "Keystroke",
            "PreLoad", "PostLoad", "PreShip", "PostShip",
            "PreRate", "PostRate", "PreVoid", "PostVoid",
            "PrePrint", "PostPrint", "PreBuildShipment", "PostBuildShipment",
            "RepeatShipment", "PreProcessBatch", "PostProcessBatch",
            "PreSearchHistory", "PostSearchHistory",
            "PreCloseManifest", "PostCloseManifest",
            "PreTransmit", "PostTransmit",
            "PreCreateGroup", "PostCreateGroup",
            "PreModifyGroup", "PostModifyGroup",
            "PreCloseGroup", "PostCloseGroup",
            "AddPackage", "CopyPackage", "RemovePackage",
            "PostSelectAddressBook"
        };

        // Collect AI-provided bodies
        var bodies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in cbrMethods.EnumerateObject())
        {
            var body = prop.Value.GetString();
            if (!string.IsNullOrWhiteSpace(body))
                bodies[prop.Name] = body;
        }

        var sb = new StringBuilder();
        sb.AppendLine("function ClientBusinessRules() {");
        sb.AppendLine();

        foreach (var method in allMethods)
        {
            var parms = GetCbrMethodParams(method);
            sb.AppendLine($"    this.{method} = function({parms}) {{");

            if (bodies.TryGetValue(method, out var body))
            {
                foreach (var line in body.Split('\n'))
                    sb.AppendLine("        " + line.TrimStart());
            }

            sb.AppendLine("    };");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GetCbrMethodParams(string methodName) => methodName switch
    {
        "PageLoaded" => "location",
        "Keystroke" => "shipmentRequest, vm, event",
        "PreShip" => "shipmentRequest, userParams",
        "PostShip" => "shipmentRequest, shipmentResponse",
        "PreLoad" => "loadValue, shipmentRequest, userParams",
        "PostLoad" => "loadValue, shipmentRequest",
        "PreRate" => "shipmentRequest, userParams",
        "PostRate" => "shipmentRequest, rateResults",
        "PreVoid" => "pkg, userParams",
        "PostVoid" => "pkg",
        "PrePrint" => "document, localPort",
        "PostPrint" => "document",
        "NewShipment" => "shipmentRequest",
        "PreBuildShipment" => "shipmentRequest",
        "PostBuildShipment" => "shipmentRequest",
        "RepeatShipment" => "currentShipment",
        "PreProcessBatch" => "batchReference, actions, params, vm",
        "PostProcessBatch" => "batchResponse, vm",
        "PreSearchHistory" => "searchCriteria",
        "PostSearchHistory" => "packages",
        "PreCloseManifest" => "manifestItem, userParams",
        "PostCloseManifest" => "manifestItem",
        "PreTransmit" => "transmitItem, userParams",
        "PostTransmit" => "transmitItem",
        "PreCreateGroup" => "groupRequest, userParams",
        "PostCreateGroup" => "groupRequest",
        "PreModifyGroup" => "groupRequest, userParams",
        "PostModifyGroup" => "groupRequest",
        "PreCloseGroup" => "groupRequest, userParams",
        "PostCloseGroup" => "groupRequest",
        "AddPackage" => "shipmentRequest, packageIndex",
        "CopyPackage" => "shipmentRequest, packageIndex",
        "RemovePackage" => "shipmentRequest, packageIndex",
        "PostSelectAddressBook" => "shipmentRequest, nameaddress",
        _ => ""
    };

    private static int FindMatchingBrace(string text, int openPos)
    {
        var depth = 0;
        for (var i = openPos; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }

    /// <summary>
    /// Parses MSBuild error output to extract the set of file names referenced in errors/warnings.
    /// MSBuild format: FileName.cs(line,col): error CS####: message
    /// </summary>
    private static HashSet<string> ExtractFilesFromBuildOutput(string buildOutput)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Text.RegularExpressions.Match m in
            System.Text.RegularExpressions.Regex.Matches(buildOutput, @"([\w.-]+\.cs)\(\d+,\d+\)"))
        {
            files.Add(m.Groups[1].Value);
        }
        return files;
    }

    private static void CopyDirectory(string source, string destination, bool skipTemplates = false)
    {
        var dir = new DirectoryInfo(source);
        if (!dir.Exists) return;

        Directory.CreateDirectory(destination);

        foreach (var file in dir.GetFiles())
        {
            // Skip the instruction .md files and sample files from the copy
            if (file.Name.Equals("shipexec-hooks.md", StringComparison.OrdinalIgnoreCase) ||
                file.Name.Equals("shipexec-templates.md", StringComparison.OrdinalIgnoreCase) ||
                file.Name.Equals("SampleCBRCode.js", StringComparison.OrdinalIgnoreCase))
                continue;

            file.CopyTo(Path.Combine(destination, file.Name), true);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            // Skip bin/obj
            if (subDir.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                subDir.Name.Equals("obj", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip Templates folder — only customized templates will be written
            if (skipTemplates && subDir.Name.Equals("Templates", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip CustomTemplates and CustomDocuments folders — not needed in output
            if (subDir.Name.Equals("CustomTemplates", StringComparison.OrdinalIgnoreCase) ||
                subDir.Name.Equals("CustomDocuments", StringComparison.OrdinalIgnoreCase))
                continue;

            CopyDirectory(subDir.FullName, Path.Combine(destination, subDir.Name));
        }
    }

    /// <summary>
    /// Returns true if the file is one of the immutable template files that must never be altered.
    /// </summary>
    private static bool IsImmutableFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.Equals("CreateBatchRequest.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("DataService.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("LoadShipment.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("Tools.cs", StringComparison.OrdinalIgnoreCase);
    }

    // ── Validate JS file using Node.js --check if available ─────────────────
    private static async Task ValidateJsWithNode(string filePath, string displayName, StringBuilder sb, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo("node", $"--check \"{filePath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc is null) return;

            var stderr = await proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode != 0)
            {
                var firstLine = stderr.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "syntax error";
                sb.AppendLine($"⚠️ {displayName}: JS syntax error — {firstLine.Trim()}");
            }
        }
        catch
        {
            // Node not available — skip
        }
    }

    // ── Azure OpenAI call ────────────────────────────────────────────────────
    private async Task<string> CallAzureOpenAiAsync(
        string deployment,
        string endpoint,
        string apiKey,
        string systemPrompt,
        string userMessage,
        bool responseFormatJson,
        CancellationToken ct)
    {
        var messages = new object[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        };

        return await CallAzureOpenAiMultiTurnAsync(deployment, endpoint, apiKey, messages, responseFormatJson, ct);
    }

    // ── Azure OpenAI multi-turn call ─────────────────────────────────────────
    private async Task<string> CallAzureOpenAiMultiTurnAsync(
        string deployment,
        string endpoint,
        string apiKey,
        IReadOnlyList<object> messages,
        bool responseFormatJson,
        CancellationToken ct)
    {
        object body = responseFormatJson
            ? new { model = deployment, messages, response_format = new { type = "json_object" } }
            : new { model = deployment, messages };

        var bodyJson = JsonSerializer.Serialize(body, _json);

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/");
        client.DefaultRequestHeaders.Add("api-key", apiKey);
        client.Timeout = TimeSpan.FromMinutes(10);

        var sw = Stopwatch.StartNew();
        using var response = await client.PostAsync(
            "chat/completions?api-version=2025-01-01-preview",
            new StringContent(bodyJson, Encoding.UTF8, "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Blueprint API error | StatusCode={StatusCode} DurationMs={DurationMs} Body={Body}",
                (int)response.StatusCode, sw.ElapsedMilliseconds,
                raw.Length > 500 ? raw[..500] : raw);
            return $"// ⚠️ API error {(int)response.StatusCode} — check server logs.";
        }

        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        logger.LogInformation(
            "Blueprint API response | Deployment={Deployment} ContentLength={Length} DurationMs={DurationMs}",
            deployment, content.Length, sw.ElapsedMilliseconds);

        return content;
    }

    // ── Split helper text into individual class files ─────────────────────────
    /// <summary>
    /// Standard PSI.Sox using statements that every manager/helper class file must include.
    /// </summary>
    private static readonly string _psiSoxUsings =
        "using System;\r\n" +
        "using System.Collections.Generic;\r\n" +
        "using System.Linq;\r\n" +
        "using System.Data;\r\n" +
        "using System.IO;\r\n" +
        "using System.Text.RegularExpressions;\r\n" +
        "using System.Globalization;\r\n" +
        "using PSI.Sox;\r\n" +
        "using PSI.Sox.Interfaces;\r\n" +
        "using PSI.Sox.Configuration;\r\n" +
        "using PSI.Sox.Data;\r\n" +
        "using PSI.Sox.Licensing;\r\n" +
        "using PSI.Sox.ML;\r\n" +
        "using PSI.Sox.Print;\r\n" +
        "using PSI.Sox.Resources;\r\n" +
        "using PSI.Sox.Tools;\r\n" +
        "using PSI.Sox.Wcf;\r\n";

    private static List<(string ClassName, string Content)> SplitHelperClasses(string helperText)
    {
        var results = new List<(string, string)>();
        var lines = helperText.Split('\n');
        var currentClass = string.Empty;
        var currentContent = new StringBuilder();
        var braceDepth = 0;
        var inClass = false;

        foreach (var line in lines)
        {
            // Detect class declaration
            var trimmed = line.TrimStart();
            if (!inClass && (trimmed.StartsWith("public class ") || trimmed.StartsWith("public static class ") ||
                             trimmed.StartsWith("internal class ") || trimmed.StartsWith("public sealed class ")))
            {
                // Extract class name
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var classIdx = Array.IndexOf(parts, "class");
                if (classIdx >= 0 && classIdx + 1 < parts.Length)
                {
                    if (currentContent.Length > 0 && !string.IsNullOrEmpty(currentClass))
                    {
                        results.Add((currentClass, currentContent.ToString()));
                    }
                    currentClass = parts[classIdx + 1].TrimEnd(':', '{', ' ');
                    currentContent.Clear();
                    inClass = true;
                    braceDepth = 0;
                }
            }

            currentContent.AppendLine(line);

            foreach (var c in line)
            {
                if (c == '{') braceDepth++;
                else if (c == '}') braceDepth--;
            }

            if (inClass && braceDepth <= 0 && currentContent.Length > 10)
            {
                inClass = false;
            }
        }

        if (currentContent.Length > 0 && !string.IsNullOrEmpty(currentClass))
        {
            results.Add((currentClass, currentContent.ToString()));
        }

        // Fallback: if no classes detected, write as single file
        if (results.Count == 0)
        {
            results.Add(("CustomHelpers", helperText));
        }

        // Ensure every class file has PSI.Sox using statements at the top
        for (var i = 0; i < results.Count; i++)
        {
            var (className, content) = results[i];
            if (!content.Contains("using PSI.Sox;", StringComparison.OrdinalIgnoreCase))
            {
                results[i] = (className, _psiSoxUsings + "\r\n" + content);
            }
        }

        return results;
    }

    // ── Add a Compile item to the .csproj ────────────────────────────────────
    private static async Task AddCompileItemToCsproj(string outputFolder, string fileName, CancellationToken ct)
    {
        var csprojFiles = Directory.GetFiles(outputFolder, "*.csproj");
        if (csprojFiles.Length == 0) return;

        var csprojPath = csprojFiles[0];
        var csprojContent = await File.ReadAllTextAsync(csprojPath, ct);

        // Check if already included
        if (csprojContent.Contains($"Include=\"{fileName}\"", StringComparison.OrdinalIgnoreCase))
            return;

        // Find the Compile ItemGroup and insert
        var compileMarker = "<Compile Include=\"";
        var insertPos = csprojContent.LastIndexOf(compileMarker, StringComparison.OrdinalIgnoreCase);
        if (insertPos < 0) return;

        // Find end of that line
        var lineEnd = csprojContent.IndexOf('\n', insertPos);
        if (lineEnd < 0) return;

        var newLine = $"    <Compile Include=\"{fileName}\" />\r\n";
        csprojContent = csprojContent[..(lineEnd + 1)] + newLine + csprojContent[(lineEnd + 1)..];
        await File.WriteAllTextAsync(csprojPath, csprojContent, ct);
    }

    // ── Add a None/SolutionItem to the .csproj ──────────────────────────────
    private static async Task AddSolutionItemToCsproj(string outputFolder, string fileName, CancellationToken ct)
    {
        var csprojFiles = Directory.GetFiles(outputFolder, "*.csproj");
        if (csprojFiles.Length == 0) return;

        var csprojPath = csprojFiles[0];
        var csprojContent = await File.ReadAllTextAsync(csprojPath, ct);

        if (csprojContent.Contains($"Include=\"{fileName}\"", StringComparison.OrdinalIgnoreCase))
            return;

        // Find existing None ItemGroup or create one before </Project>
        var noneMarker = "<None Include=\"";
        var insertPos = csprojContent.LastIndexOf(noneMarker, StringComparison.OrdinalIgnoreCase);
        if (insertPos >= 0)
        {
            var lineEnd = csprojContent.IndexOf('\n', insertPos);
            if (lineEnd >= 0)
            {
                var newLine = $"    <None Include=\"{fileName}\" />\r\n";
                csprojContent = csprojContent[..(lineEnd + 1)] + newLine + csprojContent[(lineEnd + 1)..];
            }
        }
        else
        {
            // No None ItemGroup — insert before </Project>
            var projectEnd = csprojContent.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase);
            if (projectEnd >= 0)
            {
                var itemGroup = $"  <ItemGroup>\r\n    <None Include=\"{fileName}\" />\r\n  </ItemGroup>\r\n";
                csprojContent = csprojContent[..projectEnd] + itemGroup + csprojContent[projectEnd..];
            }
        }

        await File.WriteAllTextAsync(csprojPath, csprojContent, ct);
    }

    // ── Copy PSI.Sox DLLs to References folder and update HintPaths ──────────
    private static async Task CopyReferenceDlls(string outputFolder, string refsSourcePath, CancellationToken ct)
    {
        // Primary source: workspace-root References folder (same DLLs this project uses)
        string? soxSourcePath = null;
        if (Directory.Exists(refsSourcePath) && Directory.GetFiles(refsSourcePath, "PSI.Sox*.dll").Length > 0)
            soxSourcePath = refsSourcePath;
        else
        {
            // Fallback: ShipExec Core install path
            const string soxCorePath = @"C:\Program Files\UPS Professional Services Inc\ShipExec\Core";
            if (Directory.Exists(soxCorePath))
                soxSourcePath = soxCorePath;
        }

        if (soxSourcePath is null) return;

        var refsFolder = Path.Combine(outputFolder, "References");
        Directory.CreateDirectory(refsFolder);

        // Copy all PSI.Sox DLLs
        foreach (var dll in Directory.GetFiles(soxSourcePath, "PSI.Sox*.dll"))
        {
            var destPath = Path.Combine(refsFolder, Path.GetFileName(dll));
            File.Copy(dll, destPath, overwrite: true);
        }

        // Update .csproj to reference from local References folder
        var csprojFiles = Directory.GetFiles(outputFolder, "*.csproj");
        if (csprojFiles.Length == 0) return;

        var csprojPath = csprojFiles[0];
        var csprojContent = await File.ReadAllTextAsync(csprojPath, ct);

        // Replace existing PSI.Sox HintPath (base DLL only) and update to local References folder
        csprojContent = System.Text.RegularExpressions.Regex.Replace(
            csprojContent,
            @"<HintPath>[^<]*PSI\.Sox\.dll</HintPath>",
            @"<HintPath>References\PSI.Sox.dll</HintPath>");

        // Ensure the base PSI.Sox reference has <Private>true</Private> so it copies to output
        csprojContent = System.Text.RegularExpressions.Regex.Replace(
            csprojContent,
            @"(<Reference Include=""PSI\.Sox"">\s*<HintPath>[^<]+</HintPath>)\s*</Reference>",
            @"$1" + "\r\n      <Private>true</Private>\r\n    </Reference>",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        // Add references for ALL PSI.Sox DLLs copied to the References folder
        var dllFiles = Directory.GetFiles(refsFolder, "PSI.Sox*.dll");
        foreach (var dll in dllFiles)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(dll);
            if (csprojContent.Contains($"Include=\"{assemblyName}\"", StringComparison.OrdinalIgnoreCase)) continue;

            // Insert after existing PSI.Sox reference
            var marker = "</Reference>";
            var psiRef = csprojContent.IndexOf("\"PSI.Sox\"", StringComparison.OrdinalIgnoreCase);
            if (psiRef < 0) psiRef = csprojContent.IndexOf("PSI.Sox", StringComparison.OrdinalIgnoreCase);
            if (psiRef >= 0)
            {
                var afterRef = csprojContent.IndexOf(marker, psiRef, StringComparison.OrdinalIgnoreCase);
                if (afterRef >= 0)
                {
                    var insertAt = afterRef + marker.Length;
                    var newRef = $"\r\n    <Reference Include=\"{assemblyName}\">\r\n      <HintPath>References\\{Path.GetFileName(dll)}</HintPath>\r\n      <Private>true</Private>\r\n    </Reference>";
                    csprojContent = csprojContent[..insertAt] + newRef + csprojContent[insertAt..];
                }
            }
        }

        await File.WriteAllTextAsync(csprojPath, csprojContent, ct);
    }

    // ── Build rich HTML chat log from AI interactions ─────────────────────────
    private static string BuildChatLogHtml(List<AiInteractionLog> interactions, string fileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine($"  <title>Analysis Chat Log — {System.Net.WebUtility.HtmlEncode(fileName)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 1200px; margin: 0 auto; padding: 2rem; background: #f8f9fa; color: #333; }");
        sb.AppendLine("    h1 { color: #1565c0; border-bottom: 2px solid #1565c0; padding-bottom: 0.5rem; }");
        sb.AppendLine("    .meta { color: #666; font-size: 0.85rem; margin-bottom: 2rem; }");
        sb.AppendLine("    .stats { display: flex; gap: 2rem; margin-bottom: 1.5rem; flex-wrap: wrap; }");
        sb.AppendLine("    .stat-box { background: #fff; border: 1px solid #ddd; border-radius: 8px; padding: 1rem; text-align: center; flex: 1; min-width: 120px; }");
        sb.AppendLine("    .stat-box .value { font-size: 1.8rem; font-weight: 700; color: #1565c0; }");
        sb.AppendLine("    .stat-box .label { font-size: 0.8rem; color: #666; }");
        sb.AppendLine("    details { margin: 0.8rem 0; border: 1px solid #ddd; border-radius: 8px; overflow: hidden; background: #fff; box-shadow: 0 2px 4px rgba(0,0,0,0.05); }");
        sb.AppendLine("    details > summary { cursor: pointer; padding: 0.7rem 1rem; font-weight: 600; font-size: 0.9rem; background: #1565c0; color: #fff; list-style: none; display: flex; justify-content: space-between; align-items: center; }");
        sb.AppendLine("    details > summary::-webkit-details-marker { display: none; }");
        sb.AppendLine("    details > summary::after { content: '▶'; font-size: 0.7rem; transition: transform 0.2s; }");
        sb.AppendLine("    details[open] > summary::after { transform: rotate(90deg); }");
        sb.AppendLine("    details.toc-details > summary { background: #fff; color: #333; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("    details.prompt-details > summary { background: #e3f2fd; color: #1565c0; font-size: 0.8rem; font-weight: 500; padding: 0.5rem 1rem; }");
        sb.AppendLine("    details.response-details > summary { background: #e8f5e9; color: #2e7d32; font-size: 0.8rem; font-weight: 500; padding: 0.5rem 1rem; }");
        sb.AppendLine("    pre { margin: 0; padding: 0.8rem 1rem; font-size: 0.78rem; white-space: pre-wrap; word-wrap: break-word; max-height: 600px; overflow-y: auto; line-height: 1.4; background: #fafafa; }");
        sb.AppendLine("    details.prompt-details pre { background: #e3f2fd; }");
        sb.AppendLine("    details.response-details pre { background: #e8f5e9; }");
        sb.AppendLine("    .interaction-body { padding: 0.5rem; }");
        sb.AppendLine("    .toc ol { padding-left: 1.5rem; }");
        sb.AppendLine("    .toc li { margin: 0.3rem 0; }");
        sb.AppendLine("    .toc a { color: #1565c0; text-decoration: none; }");
        sb.AppendLine("    .toc a:hover { text-decoration: underline; }");
        sb.AppendLine("    .char-count { font-size: 0.7rem; opacity: 0.7; margin-left: 0.5rem; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"  <h1>&#x1F4AC; Analysis Chat Log</h1>");
        sb.AppendLine($"  <div class=\"meta\">Blueprint: <strong>{System.Net.WebUtility.HtmlEncode(fileName)}</strong> | Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Total Interactions: {interactions.Count}</div>");

        // Stats
        sb.AppendLine("  <details open><summary>&#x1F4CA; Statistics</summary><div style=\"padding:1rem;\">");
        sb.AppendLine("  <div class=\"stats\">");
        sb.AppendLine($"    <div class=\"stat-box\"><div class=\"value\">{interactions.Count}</div><div class=\"label\">AI Calls</div></div>");
        var totalPromptChars = interactions.Sum(i => i.PromptSent?.Length ?? 0);
        var totalResponseChars = interactions.Sum(i => i.ResponseReceived?.Length ?? 0);
        sb.AppendLine($"    <div class=\"stat-box\"><div class=\"value\">{totalPromptChars / 1024:N0}K</div><div class=\"label\">Chars Sent</div></div>");
        sb.AppendLine($"    <div class=\"stat-box\"><div class=\"value\">{totalResponseChars / 1024:N0}K</div><div class=\"label\">Chars Received</div></div>");
        if (interactions.Count >= 2)
        {
            var duration = interactions[^1].Timestamp - interactions[0].Timestamp;
            sb.AppendLine($"    <div class=\"stat-box\"><div class=\"value\">{duration.TotalMinutes:F1}m</div><div class=\"label\">Duration</div></div>");
        }
        sb.AppendLine("  </div>");
        sb.AppendLine("  </div></details>");

        // Table of contents
        sb.AppendLine("  <details class=\"toc-details\"><summary>&#x1F4D1; Table of Contents</summary><div class=\"toc\" style=\"padding:1rem;\">");
        sb.AppendLine("    <ol>");
        for (var i = 0; i < interactions.Count; i++)
        {
            sb.AppendLine($"      <li><a href=\"#interaction-{i + 1}\">{System.Net.WebUtility.HtmlEncode(interactions[i].Step)}</a> <span style=\"color:#888; font-size:0.8rem;\">({interactions[i].Timestamp:HH:mm:ss})</span></li>");
        }
        sb.AppendLine("    </ol>");
        sb.AppendLine("  </div></details>");

        // Interactions
        for (var i = 0; i < interactions.Count; i++)
        {
            var interaction = interactions[i];
            var promptLen = interaction.PromptSent?.Length ?? 0;
            var responseLen = interaction.ResponseReceived?.Length ?? 0;

            sb.AppendLine($"  <details id=\"interaction-{i + 1}\">");
            sb.AppendLine($"    <summary>#{i + 1} — {System.Net.WebUtility.HtmlEncode(interaction.Step)} <span class=\"char-count\">({interaction.Timestamp:HH:mm:ss} | sent: {promptLen:N0} chars | received: {responseLen:N0} chars)</span></summary>");
            sb.AppendLine("    <div class=\"interaction-body\">");

            // Prompt (collapsible)
            sb.AppendLine("      <details class=\"prompt-details\">");
            sb.AppendLine($"        <summary>&#x2709; Prompt Sent ({promptLen:N0} chars)</summary>");
            sb.AppendLine($"        <pre>{System.Net.WebUtility.HtmlEncode(interaction.PromptSent ?? "(empty)")}</pre>");
            sb.AppendLine("      </details>");

            // Response (collapsible)
            sb.AppendLine("      <details class=\"response-details\" open>");
            sb.AppendLine($"        <summary>&#x1F4E9; Response Received ({responseLen:N0} chars)</summary>");
            sb.AppendLine($"        <pre>{System.Net.WebUtility.HtmlEncode(interaction.ResponseReceived ?? "(empty)")}</pre>");
            sb.AppendLine("      </details>");

            sb.AppendLine("    </div>");
            sb.AppendLine("  </details>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    // ── Remove the .md prompt files from the output .csproj ──────────────────
    private static async Task RemoveMdFromCsproj(string outputFolder, CancellationToken ct)
    {
        var csprojFiles = Directory.GetFiles(outputFolder, "*.csproj");
        if (csprojFiles.Length == 0) return;

        var csprojPath = csprojFiles[0];
        var content = await File.ReadAllTextAsync(csprojPath, ct);

        // Remove the None Include lines for the .md files
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\s*<None Include=""shipexec-hooks\.md""\s*/?>[\s\S]*?(?:</None>)?\s*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\s*<None Include=""shipexec-templates\.md""\s*/?>[\s\S]*?(?:</None>)?\s*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove the Content Include for SampleCBRCode.js (not copied to output)
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\s*<Content Include=""CBR\\SampleCBRCode\.js""\s*/?>[\s\S]*?(?:</Content>)?\s*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove ALL Templates\*.html Content references (only changed templates will be re-added)
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\s*<Content Include=""Templates\\[^""]+\.html""\s*/?>[\s\S]*?(?:</Content>)?\s*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Clean up any empty ItemGroups left behind
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\s*<ItemGroup>\s*</ItemGroup>\s*",
            "\r\n");

        await File.WriteAllTextAsync(csprojPath, content, ct);
    }

    /// <summary>
    /// Adds Content references to the .csproj for all .html files present in the Templates folder.
    /// </summary>
    private static async Task AddTemplateContentItemsToCsproj(string outputFolder, CancellationToken ct)
    {
        var templatesDir = Path.Combine(outputFolder, "Templates");
        if (!Directory.Exists(templatesDir)) return;

        var htmlFiles = Directory.GetFiles(templatesDir, "*.html");
        if (htmlFiles.Length == 0) return;

        var csprojFiles = Directory.GetFiles(outputFolder, "*.csproj");
        if (csprojFiles.Length == 0) return;

        var csprojPath = csprojFiles[0];
        var content = await File.ReadAllTextAsync(csprojPath, ct);

        var newItems = new StringBuilder();
        foreach (var htmlFile in htmlFiles)
        {
            var relativePath = $"Templates\\{Path.GetFileName(htmlFile)}";
            if (content.Contains($"Include=\"{relativePath}\"", StringComparison.OrdinalIgnoreCase))
                continue;
            newItems.AppendLine($"    <Content Include=\"{relativePath}\" />");
        }

        if (newItems.Length == 0) return;

        // Insert into existing Content ItemGroup or create one
        var contentMarker = "<Content Include=\"CBR\\ClientBusinessRulesTemplate.js\"";
        var markerPos = content.IndexOf(contentMarker, StringComparison.OrdinalIgnoreCase);
        if (markerPos >= 0)
        {
            var insertAt = content.IndexOf('\n', markerPos);
            if (insertAt >= 0)
            {
                content = content[..(insertAt + 1)] + newItems.ToString() + content[(insertAt + 1)..];
            }
        }
        else
        {
            // Fallback: insert before </Project>
            var projectEnd = content.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase);
            if (projectEnd >= 0)
            {
                var itemGroup = $"  <ItemGroup>\r\n{newItems}  </ItemGroup>\r\n";
                content = content[..projectEnd] + itemGroup + content[projectEnd..];
            }
        }

        await File.WriteAllTextAsync(csprojPath, content, ct);
    }
}
