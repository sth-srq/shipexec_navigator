using System.IO;
using System.Text;

namespace ShipExecAgent.Tools.EnvironmentMapper;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        if (e.Args.Length >= 2)
        {
            var outputFile = e.Args.Length >= 3 ? e.Args[2] : null;
            RunConsoleMode(e.Args[0], e.Args[1], outputFile);
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
    }

    private static void RunConsoleMode(string file1, string file2, string? outputFile)
    {
        var sb = new StringBuilder();
        void log(string msg) => sb.AppendLine(msg);

        var comparer = new CompanyComparer(log);
        var mappings = comparer.Compare(file1, file2);

        log("");
        log(new string('=', 90));
        log("  ID MAPPING RESULTS");
        log(new string('=', 90));
        log("");

        string? currentKey = null;
        foreach (var m in mappings)
        {
            var key = $"{m.EntityPath}|{m.DisplayName}";
            if (key != currentKey)
            {
                currentKey = key;
                int depth = m.EntityPath.Split(" > ").Length - 1;
                var indent = new string(' ', depth * 2);
                var entityType = m.EntityPath.Split(" > ").Last();
                log($"{indent}{entityType}: {m.DisplayName}");
            }

            int fieldDepth = m.EntityPath.Split(" > ").Length - 1;
            var fieldIndent = new string(' ', fieldDepth * 2 + 2);
            var marker = m.IsSame ? "  (same)" : "";
            log($"{fieldIndent}{m.FieldName}: {m.File1Value} --> {m.File2Value}{marker}");
        }

        int total = mappings.Count;
        int changed = mappings.Count(m => !m.IsSame);
        int same = mappings.Count(m => m.IsSame);

        log("");
        log(new string('-', 90));
        log($"  Summary: {total} mapping(s)  |  {changed} changed  |  {same} same");
        log(new string('-', 90));

        var output = sb.ToString();
        if (outputFile != null)
            File.WriteAllText(outputFile, output);
        else
            File.WriteAllText(Path.ChangeExtension(file1, ".mapping.txt"), output);
    }
}
