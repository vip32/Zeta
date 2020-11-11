namespace Zeta.Foundation
{
    using System.Collections.Generic;

    public class FakeAuthenticationHandlerOptions
    {
        public IDictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
    }
}
