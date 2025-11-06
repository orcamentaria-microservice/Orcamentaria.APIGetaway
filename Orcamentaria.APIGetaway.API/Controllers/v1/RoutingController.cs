using Microsoft.AspNetCore.Mvc;
using Orcamentaria.APIGetaway.Domain.DTOs;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Responses;

namespace Orcamentaria.AuthService.API.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RoutingController : Controller
    {
        private readonly IRoutingService _service;

        public RoutingController(IRoutingService service)
        {
            _service = service;
        }

        [HttpPost(Name = "Routing")]
        public async Task<Response<dynamic>> Discovery([FromBody] RequestDTO dto)
        {
            try
            {
                return await _service.RoutingRequest(dto);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
