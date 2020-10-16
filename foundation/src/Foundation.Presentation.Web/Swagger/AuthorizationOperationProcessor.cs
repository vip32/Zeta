namespace Zeta.Foundation
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Authorization;
    using NSwag;
    using NSwag.Generation.Processors;
    using NSwag.Generation.Processors.Contexts;

    public class AuthorizationOperationProcessor : IOperationProcessor
    {
        private readonly string name;

        public AuthorizationOperationProcessor(string name)
        {
            this.name = name;
        }

        public bool Process(OperationProcessorContext context)
        {
            if (this.name != null
                && context.MethodInfo.DeclaringType != null
                && (context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
                    || context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()))
            {
                context.OperationDescription.Operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                context.OperationDescription.Operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
                context.OperationDescription.Operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement { [this.name] = new List<string>() }
                };
            }

            return true;
        }
    }
}