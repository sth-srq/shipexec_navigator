using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace ShipExecNavigator.DesktopClient;

public partial class MainWindow : Window
{
    private static readonly HttpClient _httpClient = new();
    private const string NavigatorBaseUrl = "http://localhost:5114";

    public MainWindow()
    {
        InitializeComponent();
    }

    private void LaunchNavigator_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Navigator launched!", "ShipExec Navigator",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AlertMessageBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        SendAlertButton.IsEnabled = !string.IsNullOrWhiteSpace(AlertMessageBox.Text);
    }

    private async void SendAlert_Click(object sender, RoutedEventArgs e)
    {
        var message = AlertMessageBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(message)) return;

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{NavigatorBaseUrl}/api/show-alert",
                new { Message = message });

            if (!response.IsSuccessStatusCode)
                MessageBox.Show($"Navigator returned: {response.StatusCode}", "Send Alert",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not reach Navigator:\n{ex.Message}", "Send Alert",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
