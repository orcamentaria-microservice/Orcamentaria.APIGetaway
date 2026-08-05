using FluentAssertions;
using Xunit;

namespace Orcamentaria.APIGetaway.Test.Providers
{
    public class TokenProviderTest
    {
        private readonly Application.Providers.TokenProvider _service;

        public TokenProviderTest()
        {
            _service = new Application.Providers.TokenProvider();
        }

        #region GetTokenAsync

        [Fact]
        public async Task GetTokenAsync_WhenCalledWithDefaultArgument_ReturnsPlaceholderToken()
        {
            var result = await _service.GetTokenAsync();

            result.Should().Be("_blanck");
        }

        [Fact]
        public async Task GetTokenAsync_WhenForceTokenGenerationIsTrue_StillReturnsPlaceholderToken()
        {
            var result = await _service.GetTokenAsync(true);

            result.Should().Be("_blanck");
        }

        [Fact]
        public async Task GetTokenAsync_WhenCalledMultipleTimes_ReturnsSameValueEveryTime()
        {
            var firstResult = await _service.GetTokenAsync();
            var secondResult = await _service.GetTokenAsync();

            firstResult.Should().Be(secondResult);
            firstResult.Should().Be("_blanck");
        }

        #endregion
    }
}
