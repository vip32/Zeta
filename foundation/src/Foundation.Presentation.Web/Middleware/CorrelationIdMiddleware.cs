namespace Zeta.Foundation
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate next;
        private readonly CorrelationIdOptions options;
        private readonly ILogger<CorrelationIdMiddleware> logger;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            IOptions<CorrelationIdOptions> options,
            ILogger<CorrelationIdMiddleware> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.next = next ?? throw new ArgumentNullException(nameof(next));

            this.options = options.Value;
            this.logger = logger;
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(this.options.Header, out var correlationId))
            {
                context.TraceIdentifier = correlationId;
            }

            this.logger.LogInformation("Request CorrelationId={CorrelationId}", context.TraceIdentifier);

            if (this.options.IncludeInResponse)
            {
                context.Response.OnStarting(() =>
                {
                    if(!context.Response.Headers.ContainsKey(this.options.Header))
                    {
                        context.Response.Headers.Add(this.options.Header, new[] { context.TraceIdentifier });
                    }

                    return Task.CompletedTask;
                });
            }

            return this.next(context);
        }
    }
}