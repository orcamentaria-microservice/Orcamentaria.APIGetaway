using Orcamentaria.Lib.Domain.Models;

namespace Orcamentaria.APIGetaway.Domain.Services
{
    public interface IServiceRecordDiscoveryService
    {
        Task<Response<IEnumerable<ServiceRegistry>>> DiscoverServiceRegistryWithEnpoint(string serviceName, string endpointName);
        Task<Response<IEnumerable<ServiceRegistry>>> DiscoverServiceRegistry(string serviceName);
    }
}
