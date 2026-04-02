using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ShipExecNavigator.BusinessLogic.Logging;

/// <summary>
/// Static logger gateway for BusinessLogic classes that are not constructed
/// through DI.  Call <see cref="Initialize"/> once in Program.cs.
/// </summary>
internal static class LoggerProvider
{
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    internal static void Initialize(ILoggerFactory factory)
        => _factory = factory ?? NullLoggerFactory.Instance;

    internal static ILogger<T> CreateLogger<T>() => _factory.CreateLogger<T>();
}
