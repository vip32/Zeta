namespace Zeta.Foundation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/_systeminformation")]
    public class SystemInformationController : ControllerBase
    {
        private readonly ILogger<SystemInformationController> logger;

        public SystemInformationController(ILogger<SystemInformationController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(SystemInformation), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.InternalServerError)]
        public async Task<SystemInformation> Get()
        {
            this.logger.LogInformation("gathering system information");

            return new SystemInformation
            {
                Request = new Dictionary<string, object>
                {
                    ["correlationId"] = this.HttpContext?.TraceIdentifier,
                    ["isLocal"] = IsLocal(this.HttpContext?.Request),
                    ["host"] = Dns.GetHostName(),
                    ["ip"] = (await Dns.GetHostAddressesAsync(Dns.GetHostName()).AnyContext()).Select(i => i.ToString()).Where(i => i.Contains(".", StringComparison.OrdinalIgnoreCase)),
                    //["userIdentity"] = this.HttpContext.User?.Identity,
                    //["username"] = this.HttpContext.User?.Identity?.Name
                },
                Runtime = new Dictionary<string, object>
                {
                    ["name"] = Assembly.GetEntryAssembly().GetName().Name,
                    ["version"] = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                    //["versionFile"] = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version,
                    ["versionInformation"] = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                    ["buildDate"] = GetBuildDate(Assembly.GetEntryAssembly()).ToString("o"),
                    ["processName"] = Process.GetCurrentProcess().ProcessName.Equals("dotnet", StringComparison.InvariantCultureIgnoreCase) ? $"{Process.GetCurrentProcess().ProcessName} (kestrel)" : Process.GetCurrentProcess().ProcessName,
                    ["framework"] = RuntimeInformation.FrameworkDescription,
                    ["osDescription"] = RuntimeInformation.OSDescription,
                    ["osArchitecture"] = RuntimeInformation.OSArchitecture.ToString(),
                    ["processorCount"] = Environment.ProcessorCount.ToString(),
                },
                Identity = new Dictionary<string, object>
                {
                    ["authenticated"] = this.HttpContext?.User?.Identity?.IsAuthenticated,
                    ["name"] = this.HttpContext?.User.Identity.Name,
                    ["claims"] = this.HttpContext?.User?.Claims?.Any() == true ? this.HttpContext?.User?.Claims?.Select(h => $"{h.Type}={h.Value}").Aggregate((i, j) => i + " | " + j) : null,
                    //["idToken"] = this.HttpContext.GetTokenAsync("id_token").Result,
                    ["accessToken"] = this.HttpContext?.GetTokenAsync("access_token").Result,
                }
            };
        }

        private static bool IsLocal(HttpRequest source)
        {
            // https://stackoverflow.com/a/41242493/7860424
            var connection = source?.HttpContext?.Connection;
            if (IsIpAddressSet(connection?.RemoteIpAddress))
            {
                return IsIpAddressSet(connection.LocalIpAddress)
                    //if local is same as remote, then we are local
                    ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                    //else we are remote if the remote IP address is not a loopback address
                    : IPAddress.IsLoopback(connection.RemoteIpAddress);
            }

            return true;
            bool IsIpAddressSet(IPAddress address)
            {
                return address != null && address.ToString() != "::1";
            }
        }

        private static DateTime GetBuildDate(Assembly assembly)
        {
            // origin: https://www.meziantou.net/2018/09/24/getting-the-date-of-build-of-a-net-assembly-at-runtime
            // note: project file needs to contain:
            //       <PropertyGroup><SourceRevisionId>build$([System.DateTime]::UtcNow.ToString("yyyyMMddHHmmss"))</SourceRevisionId></PropertyGroup>
            const string BuildVersionMetadataPrefix1 = "+build";
            const string BuildVersionMetadataPrefix2 = ".build"; // TODO: make this an array of allowable prefixes
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var prefix = BuildVersionMetadataPrefix1;
                var index = value.IndexOf(BuildVersionMetadataPrefix1, StringComparison.OrdinalIgnoreCase);
                // fallback for '.build' prefix
                if (index == -1)
                {
                    prefix = BuildVersionMetadataPrefix2;
                    index = value.IndexOf(BuildVersionMetadataPrefix2, StringComparison.OrdinalIgnoreCase);
                }

                if (index > 0)
                {
                    value = value.Substring(index + prefix.Length);
                    if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class SystemInformation
#pragma warning restore SA1402 // File may only contain a single type
    {
        public IDictionary<string, object> Request { get; set; }

        public IDictionary<string, object> Runtime { get; set; }

        public IDictionary<string, object> Identity { get; set; }
    }
}
