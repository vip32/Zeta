﻿namespace Zeta.Orders.IntegrationTests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;
    using Xunit.Abstractions;
    using Zeta.Foundation;
    using Zeta.Orders.Presentation.Web;

    public class EndpointTests
    {
        private readonly ITestOutputHelper testOutputHelper;
        private readonly HttpClient client;

        public EndpointTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
#pragma warning disable CA2000 // Dispose objects before losing scope
            var factory = new CustomWebApplicationFactory<Startup>(
                testOutputHelper,
                s => s.AddSingleton(
                    new FakeAuthenticationHandlerOptions()
                    {
                        Claims = new Dictionary<string, string>
                        {
                            ["origin"] = "endpointtest",
                        }
                    }));
#pragma warning restore CA2000 // Dispose objects before losing scope
            this.client = factory.CreateClient();
        }

        [Theory]
        [InlineData("/swagger/v1/swagger.json")]
        public async Task CreateSwaggerJsonTest(string route)
        {
            // arrange + act
            var response = await this.client.GetAsync(route).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // assert
            Assert.NotEmpty(stringResponse);

            using (var outputFile = new StreamWriter("../../../../../src/Orders.Presentation.Web/swagger.json"))
            {
                outputFile.WriteLine("//----------------------");
                outputFile.WriteLine("// <auto-generated>");
                outputFile.WriteLine($"//     Generated by {this.GetType().Namespace}.CreateSwaggerJsonTest");
                outputFile.WriteLine("// </auto-generated>");
                outputFile.WriteLine("//----------------------");
                outputFile.WriteLine(string.Empty);

                outputFile.WriteLine(stringResponse);
            }
        }

        // TODO: create swagger.json based typed httpclient/ts client >> https://stu.dev/generating-typed-client-for-httpclientfactory-with-nswag/

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

        //        [Fact]
        //        public async Task ValuesPostTest()
        //        {
        //            var command = new WeatherForecastCreateCommand("??", "??");
        //#pragma warning disable CA2000 // Dispose objects before losing scope
        //            var response = await this.client.PostAsync("/users", this.CreateJsonContent(command)).ConfigureAwait(false);
        //#pragma warning restore CA2000 // Dispose objects before losing scope

        //            response.EnsureSuccessStatusCode();
        //            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        //            var result = JsonConvert.DeserializeObject<UserCreateCommandResponse>(stringResponse);

        //            Assert.True(response.Headers.Contains("Location"));
        //            Assert.NotEmpty(response.Headers.GetValues("Location"));
        //            Assert.Null(result);
        //        }

        //        private HttpContent CreateJsonContent(object obj)
        //        {
        //            var json = JsonConvert.SerializeObject(obj);
        //            var content = new StringContent(json, Encoding.UTF8, "application/json");

        //#pragma warning disable IDE0059 // Unnecessary assignment of a value
        //            // required due to https://github.com/dotnet/aspnetcore/issues/18463
        //            var contentLenth = content.Headers.ContentLength;
        //#pragma warning restore IDE0059 // Unnecessary assignment of a value

        //            return content;
        //        }
    }
}
