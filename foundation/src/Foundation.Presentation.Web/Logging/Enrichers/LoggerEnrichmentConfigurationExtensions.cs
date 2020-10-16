namespace Zeta.Foundation
{
    using System;
    using Serilog;
    using Serilog.Configuration;

    /// <summary>
    /// Extends <see cref="LoggerConfiguration"/> to add enrichers for the current TraceIdentifier/>.
    /// capabilities.
    /// </summary>
    public static class LoggerEnrichmentConfigurationExtensions
    {
        /// <summary>
        /// Enrich log events with a TraceIdentifier property containing the current TraceIdentifier/>.
        /// </summary>
        /// <param name="configuration">Logger enrichment configuration.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration WithCorrelationId(
           this LoggerEnrichmentConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return configuration.With<CorrelationIdEnricher>();
        }
    }
}
