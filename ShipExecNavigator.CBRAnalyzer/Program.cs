using Microsoft.Extensions.Configuration;
using ShipExecNavigator.CBRAnalyzer;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .Build();

//if (args.Length == 0)
//{
//    Console.WriteLine("ShipExecNavigator CBR Analyzer");
//    Console.WriteLine("Usage:");
//    Console.WriteLine("  CBRAnalyzer <file1.js> [file2.js ...]");
//    Console.WriteLine("  CBRAnalyzer --folder <path>");
//    Console.WriteLine("  CBRAnalyzer --folder <path> --helper <existing-helper.js>");
//    Console.WriteLine();
//    Console.WriteLine("Options:");
//    Console.WriteLine("  --helper <path>   Seed the first pass with an existing helper JS file.");
//    return 1;
//}

// ── Parse arguments ──────────────────────────────────────────────────────────
List<string> files = [];
string? seedHelperPath = null;

//for (int i = 0; i < args.Length; i++)
//{
//    if (args[i] == "--folder")
//    {
//        if (i + 1 >= args.Length || !Directory.Exists(args[i + 1]))
//        {
//            Console.Error.WriteLine($"Folder not found: {(i + 1 < args.Length ? args[i + 1] : "(none)")}");
//            return 1;
//        }
//        // Exclude any CbrHelper_*.js files that are themselves outputs
//        files.AddRange(Directory.EnumerateFiles(args[i + 1], "*.js", SearchOption.TopDirectoryOnly)
//            .Where(f => !Path.GetFileName(f).StartsWith("CbrHelper_", StringComparison.OrdinalIgnoreCase)));
//        i++;
//    }
//    else if (args[i] == "--helper")
//    {
//        if (i + 1 >= args.Length)
//        {
//            Console.Error.WriteLine("--helper requires a file path argument.");
//            return 1;
//        }
//        seedHelperPath = args[i + 1];
//        if (!File.Exists(seedHelperPath))
//        {
//            Console.Error.WriteLine($"Helper file not found: {seedHelperPath}");
//            return 1;
//        }
//        i++;
//    }
//    else
//    {
//        if (!File.Exists(args[i]))
//        {
//            Console.Error.WriteLine($"File not found: {args[i]}");
//            continue;
//        }
//        files.Add(args[i]);
//    }
//}

//if (files.Count == 0)
//{
//    Console.Error.WriteLine("No JavaScript files found to analyze.");
//    return 1;
//}

// ── Load CBR template ─────────────────────────────────────────────────────────
var templatePath = config["CBRAnalyzer:TemplatePath"] ?? string.Empty;
if (!File.Exists(templatePath))
    templatePath = Path.Combine(Directory.GetCurrentDirectory(), "ClientBusinessRulesTemplate.js");

if (!File.Exists(templatePath))
{
    Console.Error.WriteLine("Template file not found. Set CBRAnalyzer:TemplatePath in appsettings.json.");
    return 1;
}

var templateContent = await File.ReadAllTextAsync(templatePath);

// ── Check API key ─────────────────────────────────────────────────────────────
var apiKey = config["OpenAI:ApiKey"] ?? string.Empty;
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("OpenAI:ApiKey is not configured in appsettings.json.");
    return 1;
}

// ── Run analysis ──────────────────────────────────────────────────────────────
var analyzer         = new CbrAnalyzer(config);
var cts              = new CancellationTokenSource();
string? helperPath   = seedHelperPath;   // starts null (or seeded); updated each pass
int exitCode         = 0;

Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

files.AddRange(new DirectoryInfo("C:\\ShipExecCBR").GetFiles().Select(x => x.FullName));

Console.WriteLine($"ShipExecNavigator CBR Analyzer — {files.Count} file(s)");
if (!string.IsNullOrWhiteSpace(helperPath))
    Console.WriteLine($"Seeding from helper: {Path.GetFileName(helperPath)}");
Console.WriteLine();

var path = @"C:\ShipExecCBR\Analysis\all1.txt";
var text = File.ReadAllText(path);
var topics = text.Split(',');

for (int j = 0; j < topics.Length; j++)
{

    var topic = topics[j].Replace("\\", " and ").Replace("/", " and ");

    for (int i = 0; i < files.Count; i++)
    {
        var filePath = files[i];
        var fileName = Path.GetFileName(filePath).Replace("\\", " and ").Replace("/", " and ");

        Console.WriteLine($"{'─',60}");
        Console.WriteLine($"[{i + 1}/{files.Count}] {fileName}");
        if (!string.IsNullOrWhiteSpace(helperPath))
            Console.WriteLine($"  Helper in:  {Path.GetFileName(helperPath)}");
        Console.WriteLine($"{'─',60}");

        //var all = await CbrAnalyzer.CollectTopicsFromFolderAsync("C:\\ShipExecCBR\\Analysis\\");
        //File.WriteAllText("C:\\ShipExecCBR\\Analysis\\all.txt", all);


        try
        {
            //var result = await analyzer.AnalyzeAsync(
            //    filePath,
            //    templateContent,
            //    helperPath,
            //    totalFileCount: files.Count,
            //    filesAnalyzedSoFar: i,
            //    ct: cts.Token);

            //// The output of this pass becomes the input for the next
            //helperPath = result.UpdatedHelperPath;

            //Console.WriteLine($"  Helper out: {Path.GetFileName(helperPath)}");
            //Console.WriteLine($"  Topics:     {result.ChangeSummary}");

            var result2 = await analyzer.AnalyzeTopicAsync(topic, filePath);

        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Cancelled.");
            break;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  ERROR: {ex.Message}");
            exitCode = 1;
            // Continue with the same helper for the next file rather than aborting
        }

        Console.WriteLine();
    }
}

Console.WriteLine($"{'─',60}");
if (!string.IsNullOrWhiteSpace(helperPath))
    Console.WriteLine($"Final helper: {helperPath}");
Console.WriteLine("Done.");
return exitCode;
