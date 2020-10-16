namespace Zeta.Foundation
{
    using System.ComponentModel;
    using System.Net;
    using System.Reflection;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    //[Authorize/*(Roles = "admin")*/] // maps to jwt groups
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/_echo")]
    public class EchoController : ControllerBase
    {
        private readonly ILogger<EchoController> logger;

        public EchoController(ILogger<EchoController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.InternalServerError)]
        [Description("Echo")]
        public ActionResult Get()
        {
            this.logger.LogInformation($"GET echo: {this.GetType().Namespace}");

            return this.Ok(
                new
                {
                    message = $"echo {Assembly.GetEntryAssembly().GetName().Name}",
                    //authenticated = this.HttpContext.User?.Identity?.IsAuthenticated,
                    //name = this.HttpContext.User.Identity.Name,
                    //idToken = this.HttpContext.GetTokenAsync("id_token").Result,
                    //accessToken = this.HttpContext.GetTokenAsync("access_token").Result,
                    //claims = this.HttpContext.User?.Claims?.Any() == true ? this.HttpContext.User?.Claims?.Select(h => $"CLAIM {h.Type}: {h.Value}").Aggregate((i, j) => i + " | " + j) : null,
                    //headers = this.HttpContext.Request.Headers.Select(h => $"HEADER {h.Key}: {h.Value}").Aggregate((i, j) => i + " | " + j)
                });
        }
    }
}
