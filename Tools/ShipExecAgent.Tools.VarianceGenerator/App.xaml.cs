using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ShipExecAgent.BusinessLogic.CompanyBuilder;
using ShipExecAgent.BusinessLogic.Tools;
using System.IO;

namespace ShipExecAgent.Tools.VarianceGenerator;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        if (e.Args.Length >= 2)
        {
            RunHeadless(e.Args[0], e.Args[1], e.Args.Length >= 3 ? e.Args[2] : null);
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
    }

    private static void RunHeadless(string beforePath, string afterPath, string? outputPath)
    {
        var dest = outputPath ?? Path.ChangeExtension(beforePath, ".variances.json");
        try
        {
            var beforeXml = File.ReadAllText(beforePath);
            var afterXml = File.ReadAllText(afterPath);

            var existing = CompanyExtractor.GetCompany(beforeXml);
            var modified = CompanyExtractor.GetCompany(afterXml);

            var manager = new CompanyBuilderManager("http://localhost", existing.Id, string.Empty);
            var variances = manager.GetVariances(existing, modified);

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var json = JsonConvert.SerializeObject(variances, settings);
            File.WriteAllText(dest, json);
        }
        catch (Exception ex)
        {
            File.WriteAllText(dest + ".error.txt", $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}");
        }
    }
}
