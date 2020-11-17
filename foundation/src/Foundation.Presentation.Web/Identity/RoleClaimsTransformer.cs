namespace Zeta.Foundation
{
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Newtonsoft.Json.Linq;

    public class RoleClaimsTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity;

            // flatten elements because Microsoft identity model doesn't support nested claims
            // by map it to Microsoft identity model, because automatic JWT bearer token mapping already processed here
            TransformRoles(claimsIdentity, "realm_access");
            TransformRoles(claimsIdentity, "resource_access");

            return Task.FromResult(principal);
        }

        private static void TransformRoles(ClaimsIdentity claimsIdentity, string claimType)
        {
            if (claimsIdentity?.IsAuthenticated == true && claimsIdentity.HasClaim((c) => c.Type.Equals(claimType, System.StringComparison.OrdinalIgnoreCase)))
            {
                var claim = claimsIdentity.FindFirst((c) => c.Type.Equals(claimType, System.StringComparison.OrdinalIgnoreCase));
                if (claim == null || string.IsNullOrEmpty(claim.Value))
                {
                    return;
                }

                foreach (var roles in
                    JObject.Parse(claim.Value).SelectTokens("$..roles"))
                {
                    foreach (var role in roles.Values<string>())
                    {
                        if (!string.IsNullOrEmpty(role)
                            && !claimsIdentity.Claims.Any(c =>
                                c.Type.Equals(ClaimTypes.Role, System.StringComparison.OrdinalIgnoreCase) && c.Value.Equals(role, System.StringComparison.OrdinalIgnoreCase)))
                        {
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                        }
                    }
                }
            }
        }
    }
}
