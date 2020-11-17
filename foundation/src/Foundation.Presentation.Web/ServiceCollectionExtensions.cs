namespace Zeta.Foundation
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddClaimsTransformation<T>(this IServiceCollection services)
            where T : class, IClaimsTransformation
        {
            return services.AddTransient<IClaimsTransformation, T>();
        }
    }
}
