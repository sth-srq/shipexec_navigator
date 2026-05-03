using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ShipExecAgent.ClientSpecificLogic.Logging;

/// <summary>
/// Static logger gateway for ClientSpecificLogic classes that are not
/// constructed through DI.  Call <see cref="Initialize"/> once in Program.cs.
/// </summary>
public static class LoggerProvider
{
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    public static void Initialize(ILoggerFactory factory)
        => _factory = factory ?? NullLoggerFactory.Instance;

    public static ILogger<T> CreateLogger<T>() => _factory.CreateLogger<T>();
}
