using Microsoft.Extensions.Options;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.APIGetaway.Application.Services
{
    public class ServiceRecordDiscoveryService : IServiceRecordDiscoveryService
    {
        private readonly IServiceRegistryService _serviceRegistryService;
        private ServiceRegistryConfiguration _serviceRegistryConfiguration;

        public ServiceRecordDiscoveryService(
            IServiceRegistryService serviceRegistryService, 
            IOptions<ServiceRegistryConfiguration> serviceRegistryConfiguration)
        {
            _serviceRegistryService = serviceRegistryService;
            _serviceRegistryConfiguration = serviceRegistryConfiguration.Value;
        }

        public async Task<Response<IEnumerable<ServiceRegistry>>> DiscoverServiceRegistry(string serviceName)
        {
            try
            {
                var endpoint = new ServiceRegistryConfigurationEndpoint
                {
                    Name = "ListEndpoints",
                    Method = "GET",
                    Route = "/v1/service/{serviceName}",
                    Order = 0,
                    RequiredAuthorization = false
                };

                endpoint.Route = endpoint.Route.Replace("{serviceName}", serviceName);

                var response = await _serviceRegistryService.SendServiceRegister<IEnumerable<ServiceRegistry>>(
                        baseUrl: _serviceRegistryConfiguration.BaseUrl,
                        endpoint: endpoint);

                return response;
            }
            catch (Exception ex)
            {
                return new Response<IEnumerable<ServiceRegistry>>(ErrorCodeEnum.InternalError, ex.Message);
            }
        }

        public async Task<Response<IEnumerable<ServiceRegistry>>> DiscoverServiceRegistryWithEnpoint(string serviceName, string endpointName)
        {
            var endpoint = new ServiceRegistryConfigurationEndpoint
            {
                Name = "Discovery",
                Method = "GET",
                Route = "/api/v1/service/{serviceName}/{endpointName}",
                RequiredAuthorization = false
            };

            endpoint.Route = endpoint.Route.Replace("{serviceName}", serviceName);
            endpoint.Route = endpoint.Route.Replace("{endpointName}", endpointName);

            try
            {
                var response = await _serviceRegistryService.SendServiceRegister<IEnumerable<ServiceRegistry>?>(
                        baseUrl: _serviceRegistryConfiguration.BaseUrl,
                        endpoint: endpoint);

                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    
    }
}
