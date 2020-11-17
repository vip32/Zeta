namespace Zeta.Foundation
{
    using System;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ReverseProxy.Configuration.Contract;

    public static class ReverseProxyBuilderExtensions
    {
        //public static IReverseProxyBuilder LoadFromConfig(this IReverseProxyBuilder source, IConfiguration configuration)
        //{
        //    return source.LoadFromConfig(configuration.GetSection("ReverseProxy"));
        //}

        public static IReverseProxyBuilder AddHealthChecks(
            this IReverseProxyBuilder source,
            IConfiguration configuration,
            IServiceCollection services)
        {
            foreach (var cluster in
                configuration.Get<ConfigurationData>()?.Clusters.Safe())
            {
                foreach (var destination in cluster.Value?.Destinations.Safe())
                {
                    if (!string.IsNullOrEmpty(destination.Value?.Address))
                    {
                        services.AddHealthChecks()
                            .AddUrlGroup(
                                new Uri(destination.Value?.Address),
                                name: destination.Key,
                                tags: new string[] { destination.Key });
                    }
                }
            }

            return source;
        }
    }
}
