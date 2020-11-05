namespace Zeta.Foundation
{
    using System;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper testOutputHelper;
        private readonly string categoryName;
        private readonly LoggerExternalScopeProvider scopeProvider;

        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName)
        {
            this.testOutputHelper = testOutputHelper;
            this.scopeProvider = scopeProvider;
            this.categoryName = categoryName;
        }

        public static ILogger Create(ITestOutputHelper testOutputHelper) =>
            new XUnitLogger(testOutputHelper, new LoggerExternalScopeProvider(), string.Empty);

        public static ILogger<T> Create<T>(ITestOutputHelper testOutputHelper) =>
            new XUnitLogger<T>(testOutputHelper, new LoggerExternalScopeProvider());

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable BeginScope<TState>(TState state) => this.scopeProvider.Push(state);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var sb = new StringBuilder()
                .Append(GetLogLevelString(logLevel))
                .Append(" [").Append(this.categoryName).Append("] ")
                .Append(formatter(state, exception));

            if (exception != null)
            {
                sb.Append('\n').Append(exception);
            }

            // Append scopes
            this.scopeProvider.ForEachScope((scope, state) =>
            {
                state.Append("\n => ");
                state.Append(scope);
            }, sb);

            this.testOutputHelper.WriteLine(sb.ToString());
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }
    }
}
