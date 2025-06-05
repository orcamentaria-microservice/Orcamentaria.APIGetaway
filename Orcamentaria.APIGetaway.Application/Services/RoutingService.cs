using Orcamentaria.APIGetaway.Domain.DTOs;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Contexts;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.APIGetaway.Application.Services
{
    public class RoutingService : IRoutingService
    {
        private readonly IDiscoveryServiceRegistryService _discoveryServiceRegistryService;
        private readonly IHttpClientService _httpClientService;
        private readonly IUserAuthContext _userAuthContext;
        private readonly ILoadBalancer _loadBalancer;

        public RoutingService(
            IDiscoveryServiceRegistryService discoveryServiceRegistryService,
            IHttpClientService httpClientService,
            IUserAuthContext userAuthContext,
            ILoadBalancer loadBalancer)
        {
            _discoveryServiceRegistryService = discoveryServiceRegistryService;
            _httpClientService = httpClientService;
            _userAuthContext = userAuthContext;
            _loadBalancer = loadBalancer;
        }

        public async Task<Response<dynamic>> RoutingRequest(RequestDTO dto)
        {
            var responseDiscover = await _discoveryServiceRegistryService.DiscoverServiceRegistry(dto.ServiceName, dto.EndpointName);

            if (!responseDiscover.Success)
                return new Response<dynamic>(responseDiscover.Error, responseDiscover.SimpleMessage);

            var service = _loadBalancer.GetNextService(responseDiscover.Data.Where(x => x.State.StateId == StateEnum.UP));

            if(service is null)
                return new Response<dynamic>(ResponseErrorEnum.NotFound, "Nenhum serviço ativo.");

            var endpoint = service.Endpoints.First();
            endpoint.Order = service.Order;

            foreach (var param in dto.Params)
            {
                endpoint.Route = endpoint.Route.Replace($"{{{param.ParamName}}}", param.ParamValue);
            }

            if(endpoint.Route.Contains("{") || endpoint.Route.Contains("}"))
                return new Response<dynamic>(ResponseErrorEnum.ValidationFailed, "Parâmetros inválidos.");

            try
            {
                var response = await _httpClientService.SendAsync<Response<dynamic>>(service.BaseUrl, endpoint, _userAuthContext.UserToken, dto.Content);

                _loadBalancer.RegisterUsedService(service, response);

                if (!response.Success)
                {
                    return new Response<dynamic>((ResponseErrorEnum)response.StatusCode, response.MessageError);
                }

                return response.Content;
            }
            catch (Exception ex)
            {
                return new Response<dynamic>(ResponseErrorEnum.InternalError, ex.Message);
            }
        }
    }
}
