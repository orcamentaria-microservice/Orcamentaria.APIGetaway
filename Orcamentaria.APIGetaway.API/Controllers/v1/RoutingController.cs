using Microsoft.AspNetCore.Mvc;
using Orcamentaria.APIGetaway.Domain.DTOs;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Models;

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
            => await _service.RoutingRequest(dto);

        //[HttpPost("Test", Name = "Test")]
        //public async Task<Response<dynamic>> Test([FromBody] RequestDTO dto)
        //    => await _service.Test(dto);

    }
}
