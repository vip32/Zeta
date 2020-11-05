namespace Zeta.ApiGateway.IntegrationTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;
    using Xunit.Abstractions;
    using Zeta.ApiGateway.Presentation.Web;
    using Zeta.Foundation;

    public class EndpointTests
    {
        private readonly ITestOutputHelper testOutputHelper;
        private readonly HttpClient client;

        public EndpointTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
#pragma warning disable CA2000 // Dispose objects before losing scope
            var factory = new CustomWebApplicationFactory<Startup>(testOutputHelper);
#pragma warning restore CA2000 // Dispose objects before losing scope
            this.client = factory.CreateClient();
        }

        [Theory]
        [InlineData("api/v1/_echo")]
        public async Task EchoGetTest(string route)
        {
            // arrange + act
            var response = await this.client.GetAsync(route).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<object>(stringResponse);

            // assert
            result.ShouldNotBeNull();
        }

        [Theory]
        [InlineData("api/v1/_systeminformation")]
        public async Task SystemInformationGetTest(string route)
        {
            // arrange + act
            var response = await this.client.GetAsync(route).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<SystemInformation>(stringResponse);

            // assert
            result.ShouldNotBeNull();
        }
    }
}
