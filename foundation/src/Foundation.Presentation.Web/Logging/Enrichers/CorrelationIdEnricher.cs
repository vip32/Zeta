namespace Zeta.Foundation
{
    using Microsoft.AspNetCore.Http;
    using Serilog.Core;
    using Serilog.Events;

    public class CorrelationIdEnricher : ILogEventEnricher
    {
        private const string PropertyName = "CorrelationId";
        private readonly IHttpContextAccessor contextAccessor;

        public CorrelationIdEnricher()
            : this(new HttpContextAccessor())
        {
        }

        public CorrelationIdEnricher(IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty(PropertyName, this.contextAccessor.HttpContext?.TraceIdentifier ?? "-"));
        }
    }
}