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

    private const string _sbrSystemPrompt =
        """
        You are an expert ShipExec Server Business Rules (SBR) engineer working in C#. You have been given:
        1. A reference document describing all ShipExec hooks, their execution order, and implementation patterns.
        2. A company blueprint document describing custom shipping logic requirements.

        ## Your task
        Analyze the blueprint and determine which SBR (C#) hooks are needed and what code goes in each.
        Also identify any BusinessRuleSettings keys that should be configured.

        ## Required response format
        Respond with EXACTLY ONE valid JSON object — no markdown, no code fences.
        Use this structure:
        {
          "analysis": "<human-readable summary of which SBR hooks to use and why>",
          "sbrMethods": {
            "Load": "<complete C# method body or null> // signature: ShipmentRequest Load(string value, ShipmentRequest shipmentRequest, SerializableDictionary userParams)",
            "PreShip": "<body or null> // signature: void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)",
            "Ship": "<body or null> // signature: ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)",
            "PostShip": "<body or null> // signature: void PostShip(ShipmentRequest shipmentRequest, ShipmentResponse shipmentResponse, SerializableDictionary userParams)",
            "PreReprocess": "<body or null> // signature: void PreReprocess(string carrier, List<long> globalMsns, SerializableDictionary userParams)",
            "PostReprocess": "<body or null> // signature: void PostReprocess(string carrier, List<long> globalMsns, ReProcessResult reProcessResponse, SerializableDictionary userParams)",
            "PrePrint": "<body or null> // signature: void PrePrint(DocumentRequest documentRequest, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)",
            "Print": "<body or null> // signature: DocumentResponse Print(DocumentRequest document, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)",
            "PostPrint": "<body or null> // signature: void PostPrint(DocumentRequest document, DocumentResponse documentResponse, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)",
            "ErrorLabel": "<body or null> // signature: string ErrorLabel(Package package, SerializableDictionary userParams)",
            "PreRate": "<body or null> // signature: void PreRate(ShipmentRequest shipmentRequest, List<Service> services, SortType sortType, SerializableDictionary userParams)",
            "Rate": "<body or null> // signature: List<ShipmentResponse> Rate(ShipmentRequest shipmentRequest, List<Service> services, SortType sortType, SerializableDictionary userParams)",
            "PostRate": "<body or null> // signature: void PostRate(ShipmentRequest shipmentRequest, List<ShipmentResponse> shipmentResponses, List<Service> services, SortType sortType, SerializableDictionary userParams)",
            "PreCloseManifest": "<body or null> // signature: void PreCloseManifest(string carrier, string shipper, ManifestItem manifestItem, SerializableDictionary userParams)",
            "CloseManifest": "<body or null> // signature: CloseManifestResult CloseManifest(string carrier, string shipper, ManifestItem manifestItem, bool print, SerializableDictionary userParams)",
            "PostCloseManifest": "<body or null> // signature: void PostCloseManifest(string carrier, string shipper, ManifestItem manifestItem, CloseManifestResult closeOutResult, List<Package> packages, SerializableDictionary userParams)",
            "PreVoid": "<body or null> // signature: void PreVoid(Package package, SerializableDictionary userParams)",
            "VoidPackage": "<body or null> // signature: Package VoidPackage(Package package, SerializableDictionary userParams)",
            "PostVoid": "<body or null> // signature: void PostVoid(Package package, SerializableDictionary userParams)",
            "PreCloseGroup": "<body or null> // signature: void PreCloseGroup(string carrier, string groupType, SerializableDictionary userParams)",
            "PostCloseGroup": "<body or null> // signature: void PostCloseGroup(string carrier, string groupType, Group group, SerializableDictionary userParams)",
            "PreCreateGroup": "<body or null> // signature: void PreCreateGroup(string carrier, string groupType, PackageRequest packageRequest, SerializableDictionary userParams)",
            "PostCreateGroup": "<body or null> // signature: void PostCreateGroup(string carrier, string groupType, Group group, PackageRequest packageRequest, SerializableDictionary userParams)",
            "PreModifyGroup": "<body or null> // signature: void PreModifyGroup(string carrier, long groupId, string groupType, PackageRequest packageRequest, SerializableDictionary userParams)",
            "PostModifyGroup": "<body or null> // signature: void PostModifyGroup(string carrier, Group group, string groupType, SerializableDictionary userParams)",
            "PreModifyPackageList": "<body or null> // signature: void PreModifyPackageList(string carrier, List<long> globalMsns, Package package, SerializableDictionary userParams)",
            "PostModifyPackageList": "<body or null> // signature: void PostModifyPackageList(string carrier, ModifyPackageListResult modifyPackageListResult, Package package, SerializableDictionary userParams)",
            "PreTransmit": "<body or null> // signature: void PreTransmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams)",
            "Transmit": "<body or null> // signature: List<TransmitItemResult> Transmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams)",
            "PostTransmit": "<body or null> // signature: void PostTransmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams)",
            "GetBatchReferences": "<body or null> // signature: List<BatchReference> GetBatchReferences(SerializableDictionary userParams)",
            "LoadBatch": "<body or null> // signature: BatchRequest LoadBatch(string batchReference, SerializableDictionary userParams)",
            "ParseBatchFile": "<body or null> // signature: BatchRequest ParseBatchFile(string batchReference, System.IO.Stream fileStream, SerializableDictionary userParams)",
            "PreProcessBatch": "<body or null> // signature: void PreProcessBatch(BatchRequest batchRequest, ProcessBatchActions batchActions, SerializableDictionary userParams)",
            "PostProcessBatch": "<body or null> // signature: void PostProcessBatch(BatchRequest batchRequest, ProcessBatchActions batchActions, ProcessBatchResult processBatchResult, SerializableDictionary userParams)",
            "PrePackRate": "<body or null> // signature: void PrePackRate(PackingRateRequest packingRateRequest, SerializableDictionary userParams)",
            "PostPackRate": "<body or null> // signature: void PostPackRate(PackingRateRequest packingRateRequest, PackingRateResponse packingRateResponse, SerializableDictionary userParams)",
            "PrePack": "<body or null> // signature: void PrePack(PackingRequest packingRequest, SerializableDictionary userParams)",
            "PostPack": "<body or null> // signature: void PostPack(PackingRequest packingRequest, PackingResponse packingResponse, SerializableDictionary userParams)",
            "PreAddressValidation": "<body or null> // signature: void PreAddressValidation(NameAddress nameAddress, bool useSimpleNameAddress, SerializableDictionary userParams)",
            "AddressValidation": "<body or null> // signature: List<NameAddressValidationCandidate> AddressValidation(NameAddress nameAddress, bool useSimpleNameAddress, SerializableDictionary userParams)",
            "PostAddressValidation": "<body or null> // signature: void PostAddressValidation(NameAddress nameAddress, List<NameAddressValidationCandidate> addressValidationCandidates, SerializableDictionary userParams)",
            "GetBoxTypes": "<body or null> // signature: List<BoxType> GetBoxTypes(List<BoxType> definedBoxTypes)",
            "LoadDistributionList": "<body or null> // signature: List<ShipmentRequest> LoadDistributionList(string value, ShipmentRequest shipmentRequest, SerializableDictionary userParams)",
            "UserMethod": "<body or null> // signature: object UserMethod(object userObject)",
            "GetCommodityContents": "<body or null> // signature: List<CommodityContent> GetCommodityContents(List<CommodityContent> definedCommodityContents)",
            "GetHazmatContents": "<body or null> // signature: List<HazmatContent> GetHazmatContents(List<HazmatContent> definedHazmatContents)"
          },
          "businessRuleSettings": [
            { "key": "settingName", "value": "description of expected value" }
          ],
          "helperClasses": "<any additional C# helper/manager classes needed as a single string — ALL business logic goes here>"
        }

        CRITICAL RULES:
        - The method body you provide MUST be ONLY the code that goes INSIDE the existing method braces.
        - Do NOT include the method signature — only the body statements.
        - The method signatures are FIXED and MUST NOT be changed under any circumstances. SoxBusinessRules implements IBusinessObject — all signatures are dictated by that interface.
        - Use only parameter names as shown in the signature comments above.
        - Only include methods that have actual implementation (not null/empty bodies).
        - Omit keys with null values from sbrMethods.
        - Create a separate Manager class (e.g. ShipmentManager, BatchManager) in helperClasses for all business logic. SoxBusinessRules methods should only instantiate the manager and delegate to it.
        - Do NOT generate a Tools class — Tools.cs already exists in the project and is copied verbatim. You may USE the existing Tools class (e.g. call Tools.GetStringValueFromBusinessRuleSettings) but never redefine it.
        - Example pattern for a method body: "var mgr = new ShipmentManager(Logger, BusinessObjectApi, BusinessRuleSettings); return mgr.Load(value, shipmentRequest, userParams);"
        - ALL custom/helper/manager classes MUST include the PSI.Sox namespaces (using PSI.Sox; using PSI.Sox.Api; using PSI.Sox.Client; etc.) at the top of the file.
        - Use the correct PSI.Sox types — Weight is an OBJECT (not a number), dimensions are objects, enums must use PSI.Sox enum types (e.g. ServiceType, PackageType, etc.).
        - Always check the type before variable assignment — do not assign a string to a Weight property or a number to an enum property. Use the proper constructors or parse methods.
        - THINK HARD ABOUT TYPE MATCHING: The method signatures above are the ABSOLUTE SOURCE OF TRUTH for parameter types. If a signature says PackageRequest, you MUST use PackageRequest — NOT Package, NOT PackageInfo, NOT ShipmentPackage. If it says ShipmentRequest, use ShipmentRequest — NOT Shipment, NOT ShipRequest. Cross-reference EVERY variable and parameter type against the exact signatures provided. A type mismatch (e.g. using 'Package' where 'PackageRequest' is required) will cause build failures.
        - The helperClasses string MUST start with the appropriate using statements for the PSI.Sox namespace.
        - COMMENTING IS CRITICAL: Include extensive, detailed comments that a junior developer can understand. For EVERY method and class:
          * Add XML summary comments on all public methods explaining WHAT and WHY
          * Reference the specific blueprint requirement each piece of code fulfills
          * Include numbered process lists (// Step 1: ..., // Step 2: ...) showing the logical flow
          * Explain HOW each hook relates to other hooks in the execution chain
          * Comment every non-obvious line of code
          * Add a file-level comment block explaining the class purpose, responsibilities, and how it fits in the overall architecture
          * The code should read like a tutorial — a junior dev should understand the full picture without asking questions
        """;

    private const string _cbrSystemPrompt =
        """
        You are an expert ShipExec Client Business Rules (CBR) engineer working in JavaScript. You have been given:
        1. A reference document describing all ShipExec hooks, their execution order, and implementation patterns.
        2. A company blueprint document describing custom UI/client-side logic requirements.

        ## Your task
        Analyze the blueprint and determine which CBR (JavaScript) hooks are needed and what code goes in each.

        ## Required response format
        Respond with EXACTLY ONE valid JSON object — no markdown, no code fences.
        Use this structure:
        {
          "analysis": "<human-readable summary of which CBR hooks to use and why>",
          "cbrMethods": {
            "PageLoaded": "<complete JS method body or null>",
            "NewShipment": "<complete JS method body or null>",
            "Keystroke": "<complete JS method body or null>",
            "PreLoad": "<complete JS method body or null>",
            "PostLoad": "<complete JS method body or null>",
            "PreShip": "<complete JS method body or null>",
            "PostShip": "<complete JS method body or null>",
            "PreRate": "<complete JS method body or null>",
            "PostRate": "<complete JS method body or null>",
            "PreVoid": "<complete JS method body or null>",
            "PostVoid": "<complete JS method body or null>",
            "PrePrint": "<complete JS method body or null>",
            "PostPrint": "<complete JS method body or null>",
            "PreProcessBatch": "<complete JS method body or null>",
            "PostProcessBatch": "<complete JS method body or null>",
            "PreSearchHistory": "<complete JS method body or null>",
            "PostSearchHistory": "<complete JS method body or null>",
            "PreCloseManifest": "<complete JS method body or null>",
            "PostCloseManifest": "<complete JS method body or null>",
            "PreTransmit": "<complete JS method body or null>",
            "PostTransmit": "<complete JS method body or null>",
            "PreBuildShipment": "<complete JS method body or null>",
            "PostBuildShipment": "<complete JS method body or null>",
            "RepeatShipment": "<complete JS method body or null>",
            "PreCreateGroup": "<complete JS method body or null>",
            "PostCreateGroup": "<complete JS method body or null>",
            "PreModifyGroup": "<complete JS method body or null>",
            "PostModifyGroup": "<complete JS method body or null>",
            "PreCloseGroup": "<complete JS method body or null>",
            "PostCloseGroup": "<complete JS method body or null>",
            "AddPackage": "<complete JS method body or null>",
            "CopyPackage": "<complete JS method body or null>",
            "RemovePackage": "<complete JS method body or null>",
            "PostSelectAddressBook": "<complete JS method body or null>"
          }
        }

        CRITICAL RULES:
        - Only include methods that have actual implementation (not null/empty bodies).
        - Omit keys with null values from cbrMethods.
        - The method body is the JavaScript code INSIDE the function — do not include the function wrapper.
        - The method signatures are FIXED and MUST NOT be changed under any circumstances. The CBR template defines the exact function signatures — you may only provide the body code.
        - THINK HARD ABOUT TYPE MATCHING: The parameter names in the hook signatures are the ABSOLUTE SOURCE OF TRUTH. If a signature uses 'packageRequest', your code must use 'packageRequest' — NOT 'package', NOT 'pkg', NOT 'shipmentPackage'. Match every variable name and object property access EXACTLY to what the signatures and ViewModel provide. Mismatched names will cause runtime errors.
        - CBR hooks interact with the ViewModel (vm) and shipmentRequest objects on the client side.
        - COMMENTING IS CRITICAL: Include extensive, detailed comments that a junior developer can understand. For EVERY method with code:
          * Add a block comment at the top explaining WHAT this hook does and WHY it exists
          * Reference the specific blueprint requirement it fulfills
          * Include a numbered process list (// Step 1: ..., // Step 2: ...) showing the logical flow
          * Explain HOW it interacts with other hooks in the chain
          * Comment every non-obvious line of code
          * The code should read like a tutorial — a junior dev should understand the full picture without asking questions
        """;

    private const string _pass2SystemPrompt =
        """
        You are an expert ShipExec Thin Client UI engineer. You have been given:
        1. A reference document describing all ShipExec HTML templates, their structure, AngularJS directives, and ViewModel bindings.
        2. A company blueprint document describing custom UI/layout requirements.

        ## Your task
        Analyze the blueprint and determine which HTML templates need modifications and what changes to make.

        ## Required response format
        Respond with EXACTLY ONE valid JSON object — no markdown, no code fences.
        Use this structure:
        {
          "analysis": "<human-readable summary of which templates to modify and why>",
          "templateChanges": [
            {
              "file": "<template filename, e.g. shippingTemplate.html>",
              "description": "<what is being changed>",
              "fullContent": "<the COMPLETE modified HTML template content>"
            }
          ],
          "cbrAdditions": "<any additional CBR JavaScript needed specifically for template interactions (or null)>"
        }

        Only include templates that actually need changes.
        The fullContent must be the COMPLETE file content — not a diff or partial snippet.
        If no template changes are needed, return an empty templateChanges array.
        """;

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

        // Load instruction files
        var templateProjectPath = Path.Combine(env.ContentRootPath, "..", "CodeStandards", "TemplateCodeShipExec20BusinessRules");
        var hooksDocPath = Path.Combine(templateProjectPath, "shipexec-hooks.md");
        var templatesDocPath = Path.Combine(templateProjectPath, "shipexec-templates.md");

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

            sbrRaw = await CallAiAndLog("SBR Analysis", _sbrSystemPrompt, sbrMessage.ToString(), true);

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

            cbrRaw = await CallAiAndLog("CBR Analysis", _cbrSystemPrompt, cbrMessage.ToString(), true);

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

            templateRaw = await CallAiAndLog("Template Analysis", _pass2SystemPrompt, templateMessage.ToString(), true);

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
        await CopyReferenceDlls(outputFolder, ct);

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

        // ── CBR Validation + Repair ─────────────────────────────────────────────
        const int maxCbrRepairAttempts = 5;
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

            // Send CBR errors + history to AI for repair
            activityLog.AppendLine($"=== CBR VALIDATION ATTEMPT {cbrAttempt + 1} — ERRORS ===\n{cbrValidationSb}");
            var cbrRepairPrompt = new StringBuilder();
            cbrRepairPrompt.AppendLine("=== ACTIVITY LOG ===");
            cbrRepairPrompt.AppendLine(activityLog.ToString());
            cbrRepairPrompt.AppendLine("=== CBR VALIDATION ERRORS (fix these) ===");
            cbrRepairPrompt.AppendLine(cbrValidationSb.ToString());
            cbrRepairPrompt.AppendLine("Fix ALL JavaScript issues. Do NOT change method signatures — only fix the body code.");
            cbrRepairPrompt.AppendLine();

            // Read broken files
            if (Directory.Exists(cbrDir2))
            {
                foreach (var jsFile in Directory.GetFiles(cbrDir2, "*.js"))
                {
                    cbrRepairPrompt.AppendLine($"=== FILE: {Path.GetFileName(jsFile)} ===");
                    cbrRepairPrompt.AppendLine(await File.ReadAllTextAsync(jsFile, ct));
                    cbrRepairPrompt.AppendLine();
                }
            }

            var cbrRepairSystem = "You are a JavaScript repair agent. Fix the validation errors in the CBR files. Do NOT change method signatures. Return JSON: { \"fixes\": [{ \"file\": \"<relative path>\", \"fullContent\": \"<complete corrected file>\" }] }. No markdown fences.";
            var cbrRepairRaw = await CallAiAndLog($"CBR Repair (attempt {cbrAttempt + 1})", cbrRepairSystem, cbrRepairPrompt.ToString(), true);

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

        // ── Template/Report HTML Validation + Repair ─────────────────────────────
        const int maxHtmlRepairAttempts = 5;
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

            // Send HTML errors + history to AI for repair
            activityLog.AppendLine($"=== HTML VALIDATION ATTEMPT {htmlAttempt + 1} — ERRORS ===\n{htmlValidationSb}");
            var htmlRepairPrompt = new StringBuilder();
            htmlRepairPrompt.AppendLine("=== ACTIVITY LOG ===");
            htmlRepairPrompt.AppendLine(activityLog.ToString());
            htmlRepairPrompt.AppendLine("=== HTML VALIDATION ERRORS (fix these) ===");
            htmlRepairPrompt.AppendLine(htmlValidationSb.ToString());
            htmlRepairPrompt.AppendLine("Fix ALL HTML issues. Ensure all tags are properly balanced.");
            htmlRepairPrompt.AppendLine();

            if (Directory.Exists(templatesDir2))
            {
                foreach (var htmlFile in Directory.GetFiles(templatesDir2, "*.html"))
                {
                    htmlRepairPrompt.AppendLine($"=== FILE: {Path.GetFileName(htmlFile)} ===");
                    htmlRepairPrompt.AppendLine(await File.ReadAllTextAsync(htmlFile, ct));
                    htmlRepairPrompt.AppendLine();
                }
            }

            var htmlRepairSystem = "You are an HTML repair agent. Fix the validation errors in the template/report HTML files. Return JSON: { \"fixes\": [{ \"file\": \"<relative path>\", \"fullContent\": \"<complete corrected file>\" }] }. No markdown fences.";
            var htmlRepairRaw = await CallAiAndLog($"HTML Repair (attempt {htmlAttempt + 1})", htmlRepairSystem, htmlRepairPrompt.ToString(), true);

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

        // ── Project Validation + Repair Loop: iterate until project builds or max attempts ──
        const int maxRepairAttempts = 5;
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
                            var buildOutput = stderr.Length > 0 ? stderr : stdout;
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

            // ── Repair: send issues + broken files + full history to AI ─────────
            onProgress?.Invoke($"Repair pass {attempt + 1}/{maxRepairAttempts}: fixing validation issues...");

            activityLog.AppendLine($"=== REPAIR ATTEMPT {attempt + 1} — ERRORS FOUND ===");
            activityLog.AppendLine(validationSb.ToString());
            activityLog.AppendLine();

            var repairPrompt = new StringBuilder();
            repairPrompt.AppendLine("=== ACTIVITY LOG (what has happened so far) ===");
            repairPrompt.AppendLine(activityLog.ToString());
            repairPrompt.AppendLine();
            repairPrompt.AppendLine("=== CURRENT VALIDATION ISSUES (you MUST fix these) ===");
            repairPrompt.AppendLine(validationSb.ToString());
            repairPrompt.AppendLine();
            repairPrompt.AppendLine("You MUST fix ALL issues. The project MUST build and all HTML/JS must be syntactically valid.");
            repairPrompt.AppendLine("Do NOT change method signatures in C# files — only fix the method bodies and ensure braces/syntax are correct.");
            repairPrompt.AppendLine();

            var filesWithIssues = new Dictionary<string, string>();

            if (Directory.Exists(templatesDir))
            {
                foreach (var htmlFile in Directory.GetFiles(templatesDir, "*.html"))
                {
                    var fname = Path.GetFileName(htmlFile);
                    if (validationSb.ToString().Contains(fname))
                    {
                        var content = await File.ReadAllTextAsync(htmlFile, ct);
                        filesWithIssues[$"Templates/{fname}"] = content;
                    }
                }
            }

            if (Directory.Exists(cbrDir))
            {
                foreach (var jsFile in Directory.GetFiles(cbrDir, "*.js"))
                {
                    var fname = Path.GetFileName(jsFile);
                    if (validationSb.ToString().Contains(fname))
                    {
                        var content = await File.ReadAllTextAsync(jsFile, ct);
                        filesWithIssues[$"CBR/{fname}"] = content;
                    }
                }
            }

            // For C# build failures, include ALL .cs files so the AI has full context
            if (validationSb.ToString().Contains("C# build failed"))
            {
                foreach (var csFile in Directory.GetFiles(outputFolder, "*.cs"))
                {
                    var fname = Path.GetFileName(csFile);
                    var content = await File.ReadAllTextAsync(csFile, ct);
                    filesWithIssues[fname] = content;
                }
            }
            else
            {
                foreach (var csFile in Directory.GetFiles(outputFolder, "*.cs"))
                {
                    var fname = Path.GetFileName(csFile);
                    if (validationSb.ToString().Contains(fname))
                    {
                        var content = await File.ReadAllTextAsync(csFile, ct);
                        filesWithIssues[fname] = content;
                    }
                }
            }

            foreach (var kvp in filesWithIssues)
            {
                repairPrompt.AppendLine($"=== FILE: {kvp.Key} ===");
                repairPrompt.AppendLine(kvp.Value);
                repairPrompt.AppendLine();
            }

            var repairSystemPrompt =
                """
                You are a code repair agent. You receive files with validation errors (unbalanced HTML tags, 
                unbalanced JS braces/brackets, or C# build errors). 

                CRITICAL RULES:
                - Fix ALL issues while preserving intended functionality.
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

            var repairRaw = await CallAiAndLog($"Project Repair (attempt {attempt + 1})", repairSystemPrompt, repairPrompt.ToString(), true);

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

            CopyDirectory(subDir.FullName, Path.Combine(destination, subDir.Name));
        }
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
    private static async Task CopyReferenceDlls(string outputFolder, CancellationToken ct)
    {
        const string soxCorePath = @"C:\Program Files\UPS Professional Services Inc\ShipExec\Core";
        if (!Directory.Exists(soxCorePath)) return;

        var refsFolder = Path.Combine(outputFolder, "References");
        Directory.CreateDirectory(refsFolder);

        // Copy all PSI.Sox DLLs
        foreach (var dll in Directory.GetFiles(soxCorePath, "PSI.Sox*.dll"))
        {
            var destPath = Path.Combine(refsFolder, Path.GetFileName(dll));
            File.Copy(dll, destPath, overwrite: true);
        }

        // Update .csproj to reference from local References folder
        var csprojFiles = Directory.GetFiles(outputFolder, "*.csproj");
        if (csprojFiles.Length == 0) return;

        var csprojPath = csprojFiles[0];
        var csprojContent = await File.ReadAllTextAsync(csprojPath, ct);

        // Replace existing PSI.Sox HintPath and ensure Private=true for copy to output
        csprojContent = System.Text.RegularExpressions.Regex.Replace(
            csprojContent,
            @"<HintPath>[^<]*PSI\.Sox[^<]*</HintPath>",
            @"<HintPath>References\PSI.Sox.dll</HintPath>");

        // Ensure all PSI.Sox references have <Private>true</Private> so they copy to output
        csprojContent = System.Text.RegularExpressions.Regex.Replace(
            csprojContent,
            @"(<Reference Include=""PSI\.Sox[^""]*"">\s*<HintPath>[^<]+</HintPath>)\s*</Reference>",
            @"$1" + "\r\n      <Private>true</Private>\r\n    </Reference>");

        // Add references for other PSI.Sox DLLs if not already present
        var dllFiles = Directory.GetFiles(refsFolder, "PSI.Sox*.dll");
        foreach (var dll in dllFiles)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(dll);
            if (assemblyName == "PSI.Sox") continue; // already referenced
            if (csprojContent.Contains($"Include=\"{assemblyName}\"", StringComparison.OrdinalIgnoreCase)) continue;

            // Insert after existing PSI.Sox reference
            var marker = "</Reference>";
            var psiRef = csprojContent.IndexOf("PSI.Sox", StringComparison.OrdinalIgnoreCase);
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

        // Clean up any empty ItemGroups left behind
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\s*<ItemGroup>\s*</ItemGroup>\s*",
            "\r\n");

        await File.WriteAllTextAsync(csprojPath, content, ct);
    }
}
