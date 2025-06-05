using Orcamentaria.APIGetaway.Domain.DTOs;
using Orcamentaria.Lib.Domain.Models;

namespace Orcamentaria.APIGetaway.Domain.Services
{
    public interface IRoutingService
    {
        Task<Response<dynamic>> RoutingRequest(RequestDTO dto);
    }
}
