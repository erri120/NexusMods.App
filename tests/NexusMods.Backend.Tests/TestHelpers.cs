using Microsoft.Extensions.Logging;

namespace NexusMods.Backend.Tests;

internal static class TestHelpers
{
    public static readonly ILoggerFactory LoggerFactory = new LoggerFactoryImpl();
    public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

    private class LoggerFactoryImpl : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName)
        {
            var testContext = TestContext.Current;
            if (testContext is null) throw new InvalidOperationException("Not running inside a test!");

            var defaultLogger = testContext.GetDefaultLogger();
            return new LoggerImpl(defaultLogger);
        }

        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }

    private class LoggerImpl : ILogger
    {
        private readonly TUnit.Core.Logging.DefaultLogger _logger;

        public LoggerImpl(TUnit.Core.Logging.DefaultLogger logger)
        {
            _logger = logger;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _logger.Log(Map(logLevel), state, exception, formatter);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(Map(logLevel));
        }

        private static TUnit.Core.Logging.LogLevel Map(LogLevel level) => (TUnit.Core.Logging.LogLevel)((int)level);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
