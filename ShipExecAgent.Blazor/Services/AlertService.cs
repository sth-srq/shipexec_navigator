using Microsoft.Extensions.Logging;

namespace ShipExecAgent.Services;

/// <summary>
/// Singleton service that broadcasts alert messages to all active Blazor circuits via
/// a server-side event.
/// <para>
/// Components subscribe to <see cref="OnAlert"/> and display a toast or modal when it fires.
/// The service also exposes a minimal HTTP API endpoint
/// (<c>POST /api/show-alert</c>) registered in <c>Program.cs</c> so that external
/// processes (e.g. background jobs) can trigger UI alerts without holding a circuit reference.
/// </para>
/// <para>
/// <b>Registration:</b> <c>Singleton</c> so the same event bus is shared across all circuits.
/// </para>
/// </summary>
public class AlertService(ILogger<AlertService> logger)
{
    /// <summary>
    /// Raised when an alert should be displayed.  Subscriber receives the alert message string.
    /// The event is asynchronous — subscribers return <see cref="Task"/> so they can
    /// perform UI updates on the SignalR circuit thread.
    /// </summary>
    public event Func<string, Task>? OnAlert;

    public async Task ShowAlertAsync(string message)
    {
        logger.LogTrace(">> ShowAlertAsync | Message={Message}", message);
        if (OnAlert is not null)
            await OnAlert.Invoke(message);
        logger.LogTrace("<< ShowAlertAsync");
    }
}
