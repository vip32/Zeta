namespace Zeta.Foundation
{
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public sealed class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper testOutputHelper;
        private readonly LoggerExternalScopeProvider scopeProvider = new LoggerExternalScopeProvider();

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(this.testOutputHelper, this.scopeProvider, categoryName);
        }

        public void Dispose()
        {
        }
    }
}
