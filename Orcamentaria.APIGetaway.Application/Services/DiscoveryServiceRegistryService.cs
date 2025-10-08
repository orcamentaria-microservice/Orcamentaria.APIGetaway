using Microsoft.Extensions.Options;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Services;
using System.Net;

namespace Orcamentaria.APIGetaway.Application.Services
{
    public class DiscoveryServiceRegistryService : IDiscoveryServiceRegistryService
    {
        private readonly IServiceRegistryService _serviceRegistryService;
        private readonly IOptions<ServiceRegistryConfiguration> _serviceRegistryConfiguration;
        private readonly IHttpClientService _httpClientService;

        public DiscoveryServiceRegistryService(
            IServiceRegistryService serviceRegistryService, 
            IOptions<ServiceRegistryConfiguration> serviceRegistryConfiguration,
            IHttpClientService httpClientService)
        {
            _serviceRegistryService = serviceRegistryService;
            _serviceRegistryConfiguration = serviceRegistryConfiguration;
            _httpClientService = httpClientService;
        }

        public async Task<Response<IEnumerable<ServiceRegistry>>> GetService(string serviceName)
        {
            try
            {
                var endpointListEndpoints = _serviceRegistryConfiguration.Value.Endpoints
                    .FirstOrDefault(x => x.Name == "ListEndpoints");

                if (endpointListEndpoints is null)
                    throw new Exception();

                var newendpointListEndpoints = new ServiceRegistryConfigurationEndpoint
                {
                    Name = endpointListEndpoints.Name,
                    Method = endpointListEndpoints.Method,
                    Order = endpointListEndpoints.Order,
                    Route = endpointListEndpoints.Route,
                    RequiredAuthorization = endpointListEndpoints.RequiredAuthorization
                };

            endpointListEndpoints.Route = endpointListEndpoints.Route.Replace("{serviceName}", serviceName);

                var response = await _serviceRegistryService.SendServiceRegister<IEnumerable<ServiceRegistry>>(
                        baseUrl: _serviceRegistryConfiguration.Value.BaseUrl,
                        endpoint: endpointListEndpoints);

                return response;
            }
            catch (Exception ex)
            {
                return new Response<IEnumerable<ServiceRegistry>>(ErrorCodeEnum.InternalError, ex.Message);
            }
        }

        public async Task<Response<IEnumerable<ServiceRegistry>>> DiscoverServiceRegistry(string serviceName, string endpointName)
        {
            var endpointDiscovery = _serviceRegistryConfiguration.Value.Endpoints.First(x => x.Name == "Discovery");

            var newEndpointDiscovery = new ServiceRegistryConfigurationEndpoint
            {
                Name = endpointDiscovery.Name,
                Method = endpointDiscovery.Method,
                Order = endpointDiscovery.Order,
                Route = endpointDiscovery.Route,
                RequiredAuthorization = endpointDiscovery.RequiredAuthorization
            };

            newEndpointDiscovery.Route = newEndpointDiscovery.Route.Replace("{serviceName}", serviceName);
            newEndpointDiscovery.Route = newEndpointDiscovery.Route.Replace("{endpointName}", endpointName);

            try
            {
                var response = await _serviceRegistryService.SendServiceRegister<IEnumerable<ServiceRegistry>?>(
                        baseUrl: _serviceRegistryConfiguration.Value.BaseUrl,
                        endpoint: newEndpointDiscovery);

                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    
    }
}
