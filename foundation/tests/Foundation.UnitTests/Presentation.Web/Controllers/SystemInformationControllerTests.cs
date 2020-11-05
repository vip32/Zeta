namespace Zeta.Foundation.Presentation.Web.UnitTests
{
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;
    using Xunit.Abstractions;

    public class SystemInformationControllerTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public SystemInformationControllerTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task GetTest()
        {
            // arrange
            var logger = XUnitLogger.Create<SystemInformationController>(this.testOutputHelper);
            var sut = new SystemInformationController(logger);

            // act
            var result = await sut.Get().AnyContext();

            // assert
            result.ShouldNotBeNull();
        }
    }
}
