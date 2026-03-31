namespace ShipExecNavigator.Services;

public class AlertService
{
    public event Func<string, Task>? OnAlert;

    public async Task ShowAlertAsync(string message)
    {
        if (OnAlert is not null)
            await OnAlert.Invoke(message);
    }
}
