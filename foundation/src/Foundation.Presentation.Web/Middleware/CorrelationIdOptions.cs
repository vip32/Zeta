namespace Zeta.Foundation
{
    public class CorrelationIdOptions
    {
        public string Header { get; set; } = "X-Correlation-ID";

        public bool IncludeInResponse { get; set; } = true;
    }
}
