using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ShipExecAgent.BusinessLogic.CompanyBuilder;
using ShipExecAgent.BusinessLogic.EntityComparison;
using ShipExecAgent.BusinessLogic.Tools;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ShipExecAgent.Tools.VarianceGenerator;

public partial class MainWindow : Window
{
    private List<Variance>? _lastVariances;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void BrowseBefore_Click(object sender, RoutedEventArgs e)
    {
        var path = ChooseXmlFile("Select BEFORE (original) Company XML");
        if (path is not null) BeforePath.Text = path;
    }

    private void BrowseAfter_Click(object sender, RoutedEventArgs e)
    {
        var path = ChooseXmlFile("Select AFTER (modified) Company XML");
        if (path is not null) AfterPath.Text = path;
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        JsonOutput.Clear();
        SummaryText.Text = string.Empty;
        _lastVariances = null;

        if (string.IsNullOrWhiteSpace(BeforePath.Text) || string.IsNullOrWhiteSpace(AfterPath.Text))
        {
            JsonOutput.Text = "Please select both Before and After XML files.";
            return;
        }

        try
        {
            Cursor = Cursors.Wait;

            var beforeXml = File.ReadAllText(BeforePath.Text);
            var afterXml = File.ReadAllText(AfterPath.Text);

            var existingCompany = CompanyExtractor.GetCompany(beforeXml);
            var modifiedCompany = CompanyExtractor.GetCompany(afterXml);

            // CompanyBuilderManager requires URL/GUID/JWT for its HTTP methods,
            // but GetVariances is purely in-memory comparison — pass dummy values.
            var manager = new CompanyBuilderManager("http://localhost", existingCompany.Id, string.Empty);
            var variances = manager.GetVariances(existingCompany, modifiedCompany);

            _lastVariances = variances;

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var json = JsonConvert.SerializeObject(variances, settings);
            JsonOutput.Text = json;

            int adds = variances.Count(v => v.IsAdd);
            int updates = variances.Count(v => v.IsUpdated);
            int removes = variances.Count(v => v.IsRemove);
            SummaryText.Text = $"{variances.Count} variance(s):  {adds} add  |  {updates} update  |  {removes} remove";
        }
        catch (Exception ex)
        {
            JsonOutput.Text = $"ERROR: {ex.Message}\n\n{ex.StackTrace}";
        }
        finally
        {
            Cursor = Cursors.Arrow;
        }
    }

    private void CopyJson_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(JsonOutput.Text))
            Clipboard.SetText(JsonOutput.Text);
    }

    private static string? ChooseXmlFile(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
