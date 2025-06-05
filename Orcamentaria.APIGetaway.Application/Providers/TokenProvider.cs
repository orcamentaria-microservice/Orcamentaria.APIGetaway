using Orcamentaria.Lib.Domain.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orcamentaria.APIGetaway.Application.Providers
{
    public class TokenProvider : ITokenProvider
    {
        public Task<string> GetTokenServiceAsync()
            => Task.FromResult("_blanck");
    }
}
