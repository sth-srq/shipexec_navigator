using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ShipExecAgent.Tools.EnvironmentMapper;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BrowseFile1_Click(object sender, RoutedEventArgs e)
    {
        var path = ChooseXmlFile();
        if (path is not null)
            File1Path.Text = path;
    }

    private void BrowseFile2_Click(object sender, RoutedEventArgs e)
    {
        var path = ChooseXmlFile();
        if (path is not null)
            File2Path.Text = path;
    }

    private void Compare_Click(object sender, RoutedEventArgs e)
    {
        ConsoleOutput.Clear();

        if (string.IsNullOrWhiteSpace(File1Path.Text) || string.IsNullOrWhiteSpace(File2Path.Text))
        {
            AppendLine("Please select both source and target environment XML files.");
            return;
        }

        try
        {
            Cursor = Cursors.Wait;
            var comparer = new CompanyComparer(AppendLine);
            var mappings = comparer.Compare(File1Path.Text, File2Path.Text);
            RenderResults(mappings);
        }
        catch (Exception ex)
        {
            AppendLine($"ERROR: {ex.Message}");
        }
        finally
        {
            Cursor = Cursors.Arrow;
        }
    }

    private void RenderResults(List<IdMapping> mappings)
    {
        AppendLine(new string('═', 90));
        AppendLine("  ID MAPPING RESULTS");
        AppendLine(new string('═', 90));
        AppendLine("");

        // Group by entity path + display name for hierarchical display
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
                AppendLine($"{indent}{entityType}: {m.DisplayName}");
            }

            int fieldDepth = m.EntityPath.Split(" > ").Length - 1;
            var fieldIndent = new string(' ', fieldDepth * 2 + 2);
            var marker = m.IsSame ? "  (same)" : "";
            AppendLine($"{fieldIndent}{m.FieldName}: {m.File1Value} ──► {m.File2Value}{marker}");
        }

        // Summary
        int total = mappings.Count;
        int changed = mappings.Count(m => !m.IsSame);
        int same = mappings.Count(m => m.IsSame);

        AppendLine("");
        AppendLine(new string('─', 90));
        AppendLine($"  Summary: {total} mapping(s)  |  {changed} changed  |  {same} same");
        AppendLine(new string('─', 90));
    }

    private void AppendLine(string text)
    {
        ConsoleOutput.AppendText(text + Environment.NewLine);
        ConsoleOutput.ScrollToEnd();
    }

    private static string? ChooseXmlFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
