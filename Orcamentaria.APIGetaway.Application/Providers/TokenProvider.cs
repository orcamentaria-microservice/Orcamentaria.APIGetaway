using Orcamentaria.Lib.Domain.Providers;

namespace Orcamentaria.APIGetaway.Application.Providers
{
    public class TokenProvider : ITokenProvider
    {
        public Task<string> GetTokenAsync(bool forceTokenGeneration = false)
            => Task.FromResult("_blanck");
    }
}
