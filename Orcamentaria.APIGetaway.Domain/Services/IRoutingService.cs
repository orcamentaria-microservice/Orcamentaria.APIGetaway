using Orcamentaria.APIGetaway.Domain.DTOs;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Responses;

namespace Orcamentaria.APIGetaway.Domain.Services
{
    public interface IRoutingService
    {
        Task<Response<dynamic>> RoutingRequest(RequestDTO dto);
    }
}
