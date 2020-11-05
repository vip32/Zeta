namespace Zeta.Foundation
{
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public sealed class XUnitLogger<T> : XUnitLogger, ILogger<T>
    {
        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider)
            : base(testOutputHelper, scopeProvider, typeof(T).FullName)
        {
        }
    }
}
