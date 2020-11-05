namespace Zeta.Foundation
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Xunit.Abstractions;

    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
        where TStartup : class
        // https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-3.1#basic-tests-with-the-default-webapplicationfactory
    {
        private readonly ITestOutputHelper testOutputHelper;

        public CustomWebApplicationFactory(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<TStartup>();
                    webBuilder.ConfigureLogging(loggingBuilder => loggingBuilder
                        .Services.AddSingleton<ILoggerProvider>(sp => new XUnitLoggerProvider(this.testOutputHelper)));
                    webBuilder.ConfigureTestServices(services => services
                        .AddAuthentication(options => // add a fake authentication handler
                        {
                            options.DefaultAuthenticateScheme = FakeAuthenticationHandler.SchemeName; // use the fake handler instead of the jwt handler (Startup)
                            options.DefaultScheme = FakeAuthenticationHandler.SchemeName;
                        })
                        .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>(FakeAuthenticationHandler.SchemeName, null));
                });
        }
    }
}
