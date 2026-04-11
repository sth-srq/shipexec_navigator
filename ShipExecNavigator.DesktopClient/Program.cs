using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.ServiceProcess;
using System.Text;
using Microsoft.Data.SqlClient;

namespace ShipExecNavigator.DesktopClient;

internal static class Program
{
    private static readonly StringBuilder s_reportBody = new();
    private static string s_outputFolder = string.Empty;
    private static int s_passCount;
    private static int s_failCount;
    private static int s_warnCount;

    static async Task Main()
    {
        Console.Title = "ShipExec System Analyzer";
        Console.OutputEncoding = Encoding.UTF8;

        PrintBanner();

        string appDb = PromptInput("AppDb Connection String");
        string soxDb = PromptInput("SoxDb Connection String");
        Console.WriteLine();

        // Create output folder on the Desktop with current datetime
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        s_outputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"ShipExec_Analysis_{timestamp}");
        Directory.CreateDirectory(s_outputFolder);

        LogInfo($"Output folder: {s_outputFolder}");
        LogDivider();

        // ───── ANALYSIS ─────

        // 1. Test connection strings
        LogSection("CONNECTION STRING TESTS");
        await TestConnectionAsync("AppDb", appDb);
        await TestConnectionAsync("SoxDb", soxDb);

        // 2. PMC Versions
        LogSection("PMC VERSIONS");
        GetDllVersions(
            @"C:\Program Files (x86)\ConnectShip\Progistics\Bin",
            "Progistics.");

        // 3. ShipExec Versions
        LogSection("SHIPEXEC VERSIONS");
        GetDllVersions(
            @"C:\Program Files\UPS Professional Services Inc\ShipExec\Core",
            "PSI.");

        // 4. Management Studio API Version
        LogSection("MANAGEMENT STUDIO API VERSION");
        GetSingleDllVersion(
            @"C:\Program Files\UPS Professional Services Inc\ShipExec\Management Studio Api\bin\ShipExecManagementStudio.Api.dll");

        // 5. Management Studio Version
        LogSection("MANAGEMENT STUDIO VERSION");
        GetSingleDllVersion(
            @"C:\Program Files\UPS Professional Services Inc\ShipExec\Management Studio\bin\ShipExecManagementStudio.dll");

        // 6. Thin Client API Version
        LogSection("THIN CLIENT API VERSION");
        GetSingleDllVersion(
            @"C:\Program Files\UPS Professional Services Inc\ShipExec\Thin Client API\bin\ShipExecThinClient.Api.dll");

        // 7. Thin Client Version
        LogSection("THIN CLIENT VERSION");
        GetSingleDllVersion(
            @"C:\Program Files\UPS Professional Services Inc\ShipExec\Thin Client\bin\ShipExecThinClient.dll");

        // 8. Certificate Testing
        LogSection("CERTIFICATE TESTING");
        LogWarn("Certificate Testing Not Available");

        // 9. IIS
        LogSection("IIS STATUS");
        CheckService("W3SVC", "IIS (World Wide Web Publishing Service)");

        // 10. Windows Services
        LogSection("WINDOWS SERVICES");
        CheckService("AMPService", "AMPService");
        CheckService("ShipExecWcfService", "ShipExecWcfService");
        CheckService("ShipExecSchedulerService", "ShipExecSchedulerService");
        CheckService("ShipExecClientService", "ShipExecClientService");

        LogDivider();

        // ───── FILE COLLECTION ─────

        LogSection("FILE COLLECTION");

        CollectPmcTemplates();
        CollectShipExecLogs();
        CollectSchedulerLogs();
        CollectEventLogErrors();
        CollectConfigs();

        // ───── REPORT ─────

        WriteReport();

        LogDivider();
        LogPass("Analysis complete! Report and files saved.");
        LogInfo("Opening output folder...");

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{s_outputFolder}\"",
            UseShellExecute = true
        });

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  Press any key to exit...");
        Console.ResetColor();
        Console.ReadKey(true);
    }

    // ──────────────────────────────────────────────────────────────
    //  Connection Testing
    // ──────────────────────────────────────────────────────────────

    private static async Task TestConnectionAsync(string name, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            LogFail($"{name}: No connection string provided");
            return;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (builder.ConnectTimeout > 10)
                builder.ConnectTimeout = 10;

            await using var conn = new SqlConnection(builder.ConnectionString);
            await conn.OpenAsync();
            LogPass($"{name}: Connected successfully (Server: {conn.DataSource}, Database: {conn.Database})");
        }
        catch (Exception ex)
        {
            LogFail($"{name}: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  DLL Version Checks
    // ──────────────────────────────────────────────────────────────

    private static void GetDllVersions(string directory, string prefix)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                LogFail($"Directory not found: {directory}");
                return;
            }

            var dlls = Directory.GetFiles(directory, $"{prefix}*.dll");
            if (dlls.Length == 0)
            {
                LogWarn($"No DLLs matching '{prefix}*' found in {directory}");
                return;
            }

            foreach (var dll in dlls.OrderBy(Path.GetFileName))
            {
                PrintDllInfo(dll);
            }
        }
        catch (Exception ex)
        {
            LogFail($"Error scanning {directory}: {ex.Message}");
        }
    }

    private static void GetSingleDllVersion(string dllPath)
    {
        try
        {
            if (!File.Exists(dllPath))
            {
                LogFail($"File not found: {dllPath}");
                return;
            }

            PrintDllInfo(dllPath);
        }
        catch (Exception ex)
        {
            LogFail($"Error reading {dllPath}: {ex.Message}");
        }
    }

    private static void PrintDllInfo(string dllPath)
    {
        var info = FileVersionInfo.GetVersionInfo(dllPath);
        string name = Path.GetFileName(dllPath);

        LogInfo($"  {name}");
        LogDetail($"    File Version:    {info.FileVersion ?? "N/A"}");
        LogDetail($"    Product Version: {info.ProductVersion ?? "N/A"}");
        LogDetail($"    Description:     {info.FileDescription ?? "N/A"}");
        LogDetail($"    Company:         {info.CompanyName ?? "N/A"}");
        LogDetail($"    Product:         {info.ProductName ?? "N/A"}");
    }

    // ──────────────────────────────────────────────────────────────
    //  Service Checks
    // ──────────────────────────────────────────────────────────────

    private static void CheckService(string serviceName, string displayName)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            var status = sc.Status;

            if (status == ServiceControllerStatus.Running)
                LogPass($"{displayName}: Running");
            else
                LogWarn($"{displayName}: {status}");
        }
        catch (InvalidOperationException)
        {
            LogFail($"{displayName}: Service not found");
        }
        catch (Exception ex)
        {
            LogFail($"{displayName}: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  File Collection
    // ──────────────────────────────────────────────────────────────

    private static void CollectPmcTemplates()
    {
        const string source =
            @"C:\Program Files (x86)\ConnectShip\Progistics\AdditionalComponents\DocumentProviders\ConnectShip";
        string dest = Path.Combine(s_outputFolder, "PMCTemplates");

        try
        {
            if (!Directory.Exists(source))
            {
                LogWarn($"PMC Templates source not found: {source}");
                return;
            }

            CopyDirectory(source, dest);
            LogPass("PMC Templates collected");
        }
        catch (Exception ex)
        {
            LogFail($"PMC Templates: {ex.Message}");
        }
    }

    private static void CollectShipExecLogs()
    {
        const string source =
            @"C:\Program Files\UPS Professional Services Inc\ShipExec\Core\logs";
        string dest = Path.Combine(s_outputFolder, "ShipExecLogs");

        try
        {
            if (!Directory.Exists(source))
            {
                LogWarn($"ShipExec Logs source not found: {source}");
                return;
            }

            Directory.CreateDirectory(dest);

            var files = new DirectoryInfo(source)
                .GetFiles("*ShipExecAPILog*")
                .OrderByDescending(f => f.LastWriteTime)
                .Take(5);

            int count = 0;
            foreach (var file in files)
            {
                File.Copy(file.FullName, Path.Combine(dest, file.Name), true);
                count++;
            }

            LogPass($"ShipExec Logs: {count} file(s) collected");
        }
        catch (Exception ex)
        {
            LogFail($"ShipExec Logs: {ex.Message}");
        }
    }

    private static void CollectSchedulerLogs()
    {
        const string source =
            @"C:\Program Files\UPS Professional Services Inc\ShipExec\SchedulerService\logs";
        string dest = Path.Combine(s_outputFolder, "SchedulerLogs");

        try
        {
            if (!Directory.Exists(source))
            {
                LogWarn($"Scheduler Logs source not found: {source}");
                return;
            }

            Directory.CreateDirectory(dest);

            var latest = new DirectoryInfo(source)
                .GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (latest != null)
            {
                File.Copy(latest.FullName, Path.Combine(dest, latest.Name), true);
                LogPass($"Scheduler Logs: Collected {latest.Name}");
            }
            else
            {
                LogWarn("Scheduler Logs: No files found");
            }
        }
        catch (Exception ex)
        {
            LogFail($"Scheduler Logs: {ex.Message}");
        }
    }

    private static void CollectEventLogErrors()
    {
        string dest = Path.Combine(s_outputFolder, "EventLog");

        try
        {
            Directory.CreateDirectory(dest);

            // Level=2 is Error
            var query = new EventLogQuery("Application", PathType.LogName, "*[System/Level=2]")
            {
                ReverseDirection = true
            };

            var sb = new StringBuilder();
            sb.AppendLine("Windows Application Event Log - Last 20 Errors");
            sb.AppendLine(new string('=', 60));

            using var reader = new EventLogReader(query);
            int count = 0;
            EventRecord? record;

            while (count < 20 && (record = reader.ReadEvent()) != null)
            {
                using (record)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Time:     {record.TimeCreated}");
                    sb.AppendLine($"Source:   {record.ProviderName}");
                    sb.AppendLine($"Event ID: {record.Id}");
                    sb.AppendLine($"Message:  {record.FormatDescription() ?? "(no message)"}");
                    sb.AppendLine(new string('-', 60));
                    count++;
                }
            }

            if (count == 0)
                sb.AppendLine("No error entries found.");

            File.WriteAllText(Path.Combine(dest, "ApplicationErrors.txt"), sb.ToString());
            LogPass($"Event Log: {count} error(s) exported");
        }
        catch (Exception ex)
        {
            LogFail($"Event Log: {ex.Message}");
        }
    }

    private static void CollectConfigs()
    {
        string dest = Path.Combine(s_outputFolder, "Configs");
        Directory.CreateDirectory(dest);

        var configs = new (string SourcePath, string OutputName)[]
        {
            (@"C:\Program Files\UPS Professional Services Inc\ShipExec\Management Studio Api\bin\ShipExecManagementStudio.Api.dll.config",
                "ManagementStudioApi_ShipExecManagementStudio.Api.dll.config"),

            (@"C:\Program Files\UPS Professional Services Inc\ShipExec\Management Studio Api\web.config",
                "ManagementStudioApi_web.config"),

            (@"C:\Program Files\UPS Professional Services Inc\ShipExec\Management Studio\web.config",
                "ManagementStudio_web.config"),

            (@"C:\Program Files\UPS Professional Services Inc\ShipExec\Management Studio\config.json",
                "ManagementStudio_config.json"),

            (@"C:\Program Files\UPS Professional Services Inc\ShipExec\Thin Client API\web.config",
                "ThinClientApi_web.config"),

            (@"C:\Program Files\UPS Professional Services Inc\ShipExec\Thin Client\web.config",
                "ThinClient_web.config"),

            (@"C:\Program Files\UPS Professional Services Inc\ShipExec\Thin Client\config.json",
                "ThinClient_config.json"),

            (@"C:\Program Files\UPS Professional Services Inc\ShipExec\Web Service\web.config",
                "WebService_web.config"),
        };

        foreach (var (sourcePath, outputName) in configs)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, Path.Combine(dest, outputName), true);
                    LogPass($"  Config copied: {outputName}");
                }
                else
                {
                    LogWarn($"  Config not found: {sourcePath}");
                }
            }
            catch (Exception ex)
            {
                LogFail($"  Config {outputName}: {ex.Message}");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Report
    // ──────────────────────────────────────────────────────────────

    private static void WriteReport()
    {
        var report = new StringBuilder();
        report.AppendLine("========================================================");
        report.AppendLine("          ShipExec System Analysis Report");
        report.AppendLine("========================================================");
        report.AppendLine();
        report.AppendLine($"  Generated : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"  Machine   : {Environment.MachineName}");
        report.AppendLine($"  User      : {Environment.UserDomainName}\\{Environment.UserName}");
        report.AppendLine();
        report.AppendLine("── SUMMARY ─────────────────────────────────────────────");
        report.AppendLine($"  Passed   : {s_passCount}");
        report.AppendLine($"  Failed   : {s_failCount}");
        report.AppendLine($"  Warnings : {s_warnCount}");
        report.AppendLine();
        report.AppendLine("── DETAILS ─────────────────────────────────────────────");
        report.AppendLine();
        report.Append(s_reportBody);

        string path = Path.Combine(s_outputFolder, "AnalysisReport.txt");
        File.WriteAllText(path, report.ToString());
        LogInfo($"Report saved: {path}");
    }

    // ──────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);

        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }

    // ──────────────────────────────────────────────────────────────
    //  Console + Report Logging
    // ──────────────────────────────────────────────────────────────

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("  ╔══════════════════════════════════════════════╗");
        Console.WriteLine("  ║        ShipExec System Analyzer             ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static string PromptInput(string label)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"  {label}: ");
        Console.ResetColor();
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static void LogSection(string title)
    {
        int pad = Math.Max(0, 50 - title.Length);
        string line = $"── {title} {new string('─', pad)}";

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {line}");
        Console.ResetColor();

        s_reportBody.AppendLine();
        s_reportBody.AppendLine(line);
    }

    private static void LogDivider()
    {
        string line = new string('─', 56);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {line}");
        Console.ResetColor();

        s_reportBody.AppendLine(line);
    }

    private static void LogPass(string message)
    {
        s_passCount++;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  [PASS] {message}");
        Console.ResetColor();

        s_reportBody.AppendLine($"  [PASS] {message}");
    }

    private static void LogFail(string message)
    {
        s_failCount++;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [FAIL] {message}");
        Console.ResetColor();

        s_reportBody.AppendLine($"  [FAIL] {message}");
    }

    private static void LogWarn(string message)
    {
        s_warnCount++;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  [WARN] {message}");
        Console.ResetColor();

        s_reportBody.AppendLine($"  [WARN] {message}");
    }

    private static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  {message}");
        Console.ResetColor();

        s_reportBody.AppendLine($"  {message}");
    }

    private static void LogDetail(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {message}");
        Console.ResetColor();

        s_reportBody.AppendLine($"  {message}");
    }
}
