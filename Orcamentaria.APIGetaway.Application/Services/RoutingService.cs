using Orcamentaria.APIGetaway.Domain.DTOs;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Contexts;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.APIGetaway.Application.Services
{
    public class RoutingService : IRoutingService
    {
        private readonly IDiscoveryServiceRegistryService _discoveryServiceRegistryService;
        private readonly IHttpClientService _httpClientService;
        private readonly IUserAuthContext _userAuthContext;
        private readonly ILoadBalancer _loadBalancer;
        private readonly IRequestContext _requestContext;

        public RoutingService(
            IDiscoveryServiceRegistryService discoveryServiceRegistryService,
            IHttpClientService httpClientService,
            IUserAuthContext userAuthContext,
            ILoadBalancer loadBalancer,
            IRequestContext requestContext)
        {
            _discoveryServiceRegistryService = discoveryServiceRegistryService;
            _httpClientService = httpClientService;
            _userAuthContext = userAuthContext;
            _loadBalancer = loadBalancer;
            _requestContext = requestContext;
        }

        public async Task<Response<dynamic>> RoutingRequest(RequestDTO dto)
        {
            try
            {
                var responseDiscover = await _discoveryServiceRegistryService.DiscoverServiceRegistry(dto.ServiceName, dto.EndpointName);

                if (!responseDiscover.Success)
                    throw new Exception(responseDiscover.SimpleMessage);

                var service = _loadBalancer.GetNextService(responseDiscover.Data.Where(x => x.State.StateId == StateEnum.UP));

                if (service is null)
                    return new Response<dynamic>(ErrorCodeEnum.NotFound, "Nenhum serviço ativo.");

                var endpoint = service.Endpoints.First();
                endpoint.Order = service.Order;

                foreach (var param in dto.Params)
                {
                    endpoint.Route = endpoint.Route.Replace($"{{{param.ParamName}}}", param.ParamValue);
                }

                if (endpoint.Route.Contains("{") || endpoint.Route.Contains("}"))
                    return new Response<dynamic>(ErrorCodeEnum.ValidationFailed, "Parâmetros inválidos.");

                var response = await _httpClientService.SendAsync<Response<dynamic>>(
                    baseUrl: service.BaseUrl,
                    endpoint: endpoint,
                    options: new OptionsRequest
                    {
                        TokenAuth = _userAuthContext.UserToken,
                        Content = dto.Content
                    });

                _loadBalancer.RegisterUsedService(service, response);

                return response.Content;
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }
    }
}
