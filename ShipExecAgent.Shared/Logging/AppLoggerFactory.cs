using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ShipExecAgent.Shared.Logging;

/// <summary>
/// Static gateway so non-DI classes can obtain a real ILogger once the host
/// is built.  Call <see cref="Initialize"/> once in Program.cs, before
/// app.Run().  Until then every call returns a NullLogger (no-op).
/// </summary>
public static class AppLoggerFactory
{
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    public static void Initialize(ILoggerFactory factory)
        => _factory = factory ?? NullLoggerFactory.Instance;

    public static ILogger<T> CreateLogger<T>() => _factory.CreateLogger<T>();
    public static ILogger CreateLogger(string categoryName) => _factory.CreateLogger(categoryName);
}
