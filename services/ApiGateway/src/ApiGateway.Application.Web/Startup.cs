namespace Zeta.ApiGateway.Application.Web
{
    using System;
    using HealthChecks.UI.Client;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.IdentityModel.Logging;
    using Microsoft.IdentityModel.Tokens;
    using Ocelot.DependencyInjection;
    using Ocelot.Middleware;

    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("ocelot.json")
                .AddEnvironmentVariables();

            this.Configuration = builder.Build();
        }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
                .AddUrlGroup(new Uri("http://customers.application.web/health"), name: "customers.application.web", tags: new string[] { "customers.application.web" })
                .AddUrlGroup(new Uri("http://orders.application.web/health"), name: "orders.application.web", tags: new string[] { "orders.application.web" });
            // TODO: get hosts from ocelot file?

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Authority = this.Configuration["Oidc:Authority"];
                    options.MetadataAddress = $"{this.Configuration["Oidc:Authority"].Replace("localhost", "keycloak", StringComparison.OrdinalIgnoreCase)}/.well-known/openid-configuration";
                    options.IncludeErrorDetails = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "groups",
                        ValidateAudience = false,
                        //ValidAudiences = new[] { "master-realm", "account" },
                        ValidateIssuer = true,
                        ValidIssuer = this.Configuration["Oidc:Authority"],
                        ValidateLifetime = false
                    };
                });

            services.AddControllers();
            services.AddAuthorization();

            services.AddOcelot(this.Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                IdentityModelEventSource.ShowPII = true;
            }
            else
            {
                app.UseHsts();
            }

            app.MapWhen(c => c.Request.Path == "/", a =>
            {
                a.Run(async x =>
                    await x.Response.WriteAsync($"<html><body><h1>{this.GetType().Namespace} ({Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")})</h1><p><a href='/health'>health</a>&nbsp;<a href='/health/live'>liveness</a>&nbsp;<a href='/api/echo'>echo</a>&nbsp;<a href='http://localhost:5340' target='_blank'>logs</a></p></body></html>").ConfigureAwait(false));
            });

            app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.UseHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.UseHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            // TODO: auth https://github.com/catcherwong-archive/APIGatewayDemo/tree/master/APIGatewayJWTAuthenticationDemo

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(e => e.MapControllers());

            app.UseOcelot().Wait(); // useendpoints?
        }
    }
}
