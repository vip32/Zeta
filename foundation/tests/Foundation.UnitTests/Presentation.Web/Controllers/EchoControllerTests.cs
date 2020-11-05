namespace Zeta.Foundation.Presentation.Web.UnitTests
{
    using Shouldly;
    using Xunit;
    using Xunit.Abstractions;

    public class EchoControllerTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public EchoControllerTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void GetTest()
        {
            // arrange
            var logger = XUnitLogger.Create<EchoController>(this.testOutputHelper);
            var sut = new EchoController(logger);

            // act
            var result = sut.Get();

            // assert
            result.ShouldNotBeNull();
        }
    }
}
