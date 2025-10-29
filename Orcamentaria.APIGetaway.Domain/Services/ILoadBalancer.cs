using Orcamentaria.Lib.Domain.Models;

namespace Orcamentaria.APIGetaway.Domain.Services
{
    public interface ILoadBalancer
    {
        ServiceRegistry? GetNextService(IEnumerable<ServiceRegistry> services);
        void RegisterUsedService<T>(ServiceRegistry service, HttpResponse<T> response);
    }
}
