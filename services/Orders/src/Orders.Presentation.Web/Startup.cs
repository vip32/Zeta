namespace Zeta.Orders.Presentation.Web
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Hellang.Middleware.ProblemDetails;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Versioning;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.IdentityModel.Logging;
    using Microsoft.IdentityModel.Tokens;
    using NSwag;
    using NSwag.AspNetCore;
    using NSwag.Generation.AspNetCore;
    using NSwag.Generation.Processors;
    using NSwag.Generation.Processors.Security;
    using Zeta.Foundation;

    public class Startup // WARN: CA1506: >100
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

            services.AddAuthentication(options => this.ConfigureAuthentication(options))
                .AddJwtBearer(options => this.ConfigureJwtBearer(options));
            services.AddAuthorization();

            services.AddApiVersioning(options => this.ConfigureApiVersioning(options));
            services.AddVersionedApiExplorer(options => options.SubstituteApiVersionInUrl = true);

            services.AddProblemDetails(this.ConfigureProblemDetails);

            services.AddOpenApiDocument(document => this.ConfigureOpenApiDocument(this.Configuration, document));

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) // WARN: CA1506: >50
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
            app.UseSwaggerUi3(settings => this.ConfigureSwaggerUI(settings));
            app.UseCorrelationId();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync(@$"
<html>
<body>
    <h1>{this.GetType().Namespace.Replace("Zeta", "&zeta;eta", StringComparison.OrdinalIgnoreCase)} ({Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")})</h1>
    <p>
        <a href='/api/v1/_systeminformation'>info</a>&nbsp;
        <a href='/health'>health</a>&nbsp;
        <a href='/health/live'>liveness</a>&nbsp;
        <a href='/api/v1/_echo'>echo</a>&nbsp;
        <a href='/swagger/index.html' target='_blank'>swagger</a>&nbsp;
        <a href='http://localhost:5340' target='_blank'>logs</a>
    </p>
</body>
</html>").ConfigureAwait(false);
                });
                endpoints.MapHealthChecks();
                endpoints.MapControllers();
            });
        }

        private void ConfigureAuthentication(AuthenticationOptions options)
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }

        private void ConfigureJwtBearer(JwtBearerOptions options)
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
        }

        private void ConfigureApiVersioning(ApiVersioningOptions options)
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }

        private void ConfigureOpenApiDocument(IConfiguration configuration, AspNetCoreOpenApiDocumentGeneratorSettings settings)
        {
            settings.DocumentName = "v1";
            settings.Version = "v1";
            settings.Title = this.GetType().Namespace;
            settings.AddSecurity(
                "bearer",
                Enumerable.Empty<string>(),
                new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.OAuth2,
                    Flow = OpenApiOAuth2Flow.Implicit,
                    Description = "Oidc Authentication",
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = $"{configuration["Oidc:Authority"]}/protocol/openid-connect/auth",
                            TokenUrl = $"{configuration["Oidc:Authority"]}/protocol/openid-connect/token",
                            Scopes = new Dictionary<string, string>
                            {
                                //{"openid", "openid"},
                            }
                        }
                    },
                });
            settings.OperationProcessors.Add(new ApiVersionProcessor());
            settings.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("bearer"));
            settings.OperationProcessors.Add(new AuthorizationOperationProcessor("bearer"));
            settings.PostProcess = document =>
            {
                document.Info.Version = "v1";
                document.Info.Title = this.GetType().Namespace;
                document.Info.Description = $"{this.GetType().Namespace} API";
            };
        }

        private void ConfigureProblemDetails(ProblemDetailsOptions options)
        {
            options.IncludeExceptionDetails = (ctx, ex) => true;
            options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);
            options.MapToStatusCode<HttpRequestException>(StatusCodes.Status503ServiceUnavailable);
            options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
        }

        private void ConfigureSwaggerUI(SwaggerUi3Settings settings)
        {
            settings.OAuth2Client = new OAuth2ClientSettings
            {
                ClientId = this.Configuration["Oidc:ClientId"],
                AppName = this.GetType().Namespace,
            };
            settings.SwaggerRoutes.Add(new SwaggerUi3Route(this.GetType().Namespace, "swagger/v1/swagger.json"));
        }
    }
}