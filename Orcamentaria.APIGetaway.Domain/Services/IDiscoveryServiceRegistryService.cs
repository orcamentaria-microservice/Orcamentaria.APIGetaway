using Orcamentaria.Lib.Domain.Models;

namespace Orcamentaria.APIGetaway.Domain.Services
{
    public interface IDiscoveryServiceRegistryService
    {
        Task<Response<IEnumerable<ServiceRegistry>>> DiscoverServiceRegistry(string serviceName, string endpointName);
        Task<Response<IEnumerable<ServiceRegistry>>> GetService(string serviceName);
    }
}
