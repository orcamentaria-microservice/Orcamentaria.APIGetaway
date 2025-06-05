using Orcamentaria.APIGetaway.Domain.Models;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.APIGetaway.Application.Services
{
    public class LoadBalancerOrderService : ILoadBalancer
    {
        private readonly IMemoryCacheService _memoryCacheService;

        public LoadBalancerOrderService(IMemoryCacheService memoryCacheService)
        {
            _memoryCacheService = memoryCacheService;
        }

        public ServiceRegistry? GetNextService(IEnumerable<ServiceRegistry> services)
        {
            if (!services.Any())
                return null;

            if (!_memoryCacheService.GetMemoryCache(services.First().ServiceName, out string lastOrder))
                return services.OrderBy(x => x.Order).First();

            var serviceSelected = services.FirstOrDefault(x => x.Order > int.Parse(lastOrder));

            return serviceSelected is null ? services.OrderBy(x => x.Order).First() : serviceSelected;
        }

        public void RegisterUsedService<T>(ServiceRegistry service, HttpResponse<T> response)
        {
            _memoryCacheService.SetMemoryCache(service.ServiceName, response.Endpoint.Order.ToString());
        }
    }
}
