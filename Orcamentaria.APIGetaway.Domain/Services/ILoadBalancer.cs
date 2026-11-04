using Orcamentaria.Lib.Domain.Models;

namespace Orcamentaria.APIGetaway.Domain.Services
{
    public interface ILoadBalancer
    {
        Task<ServiceRegistry?> GetNextServiceAsync(IEnumerable<ServiceRegistry> services);
        Task RegisterUsedServiceAsync<T>(ServiceRegistry service, HttpResponse<T> response);
    }
}
