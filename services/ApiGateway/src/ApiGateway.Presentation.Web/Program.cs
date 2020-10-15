namespace Zeta.ApiGateway.Presentation.Web
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Serilog;

    public static class Program
    {
        public static readonly string AppName = typeof(Program).Namespace.Replace("Zeta.", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(".Presentation.Web", string.Empty, StringComparison.OrdinalIgnoreCase);

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((context, builder) =>
               {
                   builder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context?.HostingEnvironment?.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("ocelot.json")
                    .AddEnvironmentVariables();

                   var config = builder.Build();
                   if (config.GetValue("AzureAppConfiguration:Enabled", false))
                   {
                       builder.AddAzureAppConfiguration(config["AzureAppConfiguration:ConnectionString"]);
                   }

                   if (config.GetValue("AzureKeyVault:Enabled", false))
                   {
                       builder.AddAzureKeyVault(
                           $"https://{config["AzureKeyVault:Name"]}.vault.azure.net/",
                           config["AzureKeyVault:ClientId"],
                           config["AzureKeyVault:ClientSecret"]);
                   }
               })
               .ConfigureLogging((context, builder) =>
               {
                   if (context.Configuration.GetValue("AzureApplicationInsights:Enabled", false))
                   {
                       builder.AddApplicationInsights(
                           context.Configuration["AzureApplicationInsights:InstrumentationKey"]);
                   }
               })
               .UseSerilog((context, builder) =>
               {
                   builder.ReadFrom.Configuration(context.Configuration)
                     .MinimumLevel.Verbose()
                     .Enrich.WithProperty("ServiceName", AppName)
                     .Enrich.FromLogContext()
                     .WriteTo.Trace()
                     .WriteTo.Console()
                     .WriteTo.Seq(string.IsNullOrWhiteSpace(context.Configuration["Serilog:SeqServerUrl"]) ? "http://localhost:5340" /*"http://seq"*/ : context.Configuration["Serilog:SeqServerUrl"]);
               })
               .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
