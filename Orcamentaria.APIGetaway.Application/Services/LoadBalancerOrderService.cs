using Microsoft.Extensions.DependencyInjection;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.APIGetaway.Application.Services
{
    public class LoadBalancerOrderService : ILoadBalancer
    {
        private readonly ICacheService _cache;

        public LoadBalancerOrderService([FromKeyedServices("memory")] ICacheService cache)
        {
            _cache = cache;
        }

        public async Task<ServiceRegistry?> GetNextServiceAsync(IEnumerable<ServiceRegistry> services)
        {
            if (!services.Any())
                return null;

            var lastOrder = await _cache.GetAsync<string>(services.First().ServiceName);
            if (lastOrder is null)
                return services.OrderBy(x => x.Order).First();

            var serviceSelected = services.FirstOrDefault(x => x.Order > int.Parse(lastOrder));

            return serviceSelected is null ? services.OrderBy(x => x.Order).First() : serviceSelected;
        }

        public async Task RegisterUsedServiceAsync<T>(ServiceRegistry service, HttpResponse<T> response)
        {
            await _cache.SetAsync(service.ServiceName, response.Endpoint.Order.ToString());
        }
    }
}
