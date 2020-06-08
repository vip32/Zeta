namespace Zeta.Orders.Presentation.Web.Controllers
{
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using EnsureThat;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [Route("api/echo")]
    [Authorize/*(Roles = "admin")*/] // maps to jwt groups
    [ApiController]
    public class EchoController : ControllerBase
    {
        private readonly ILogger<EchoController> logger;

        public EchoController(ILogger<EchoController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.InternalServerError)]
        [Description("Echo")]
        public ActionResult Get()
        {
            this.logger.LogInformation($"GET echo: {this.GetType().Namespace}");

            return this.Ok(
                new
                {
                    message = $"echo {this.GetType().Assembly.GetName().Name}",
                    authenticated = this.HttpContext.User?.Identity?.IsAuthenticated,
                    name = this.HttpContext.User.Identity.Name,
                    idToken = this.HttpContext.GetTokenAsync("id_token").Result,
                    accessToken = this.HttpContext.GetTokenAsync("access_token").Result,
                    claims = this.HttpContext.User?.Claims?.Any() == true ? this.HttpContext.User?.Claims?.Select(h => $"CLAIM {h.Type}: {h.Value}").Aggregate((i, j) => i + " | " + j) : null,
                    headers = this.HttpContext.Request.Headers.Select(h => $"HEADER {h.Key}: {h.Value}").Aggregate((i, j) => i + " | " + j)
                });
        }
    }
}
