namespace Zeta.Customers.Presentation.Web
{
    using System;
    using HealthChecks.UI.Client;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.IdentityModel.Logging;
    using Microsoft.IdentityModel.Tokens;
    using NSwag.AspNetCore;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
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

            services.AddAuthorization();

            services.AddSwaggerDocument(document => document.Title = this.GetType().Namespace);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                IdentityModelEventSource.ShowPII = true;
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseOpenApi();
            app.UseSwaggerUi3(c =>
            {
                c.OAuth2Client = new OAuth2ClientSettings
                {
                    ClientId = this.Configuration["Oidc:ClientId"],
                    ClientSecret = this.Configuration["Oidc:ClientSecret"], // TODO: get from Configuration
                };
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapControllers();
            });
        }
    }
}
