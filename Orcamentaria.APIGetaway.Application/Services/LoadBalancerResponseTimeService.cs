using Microsoft.Extensions.DependencyInjection;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.APIGetaway.Application.Services
{
    public class LoadBalancerResponseTimeService : ILoadBalancer
    {
        private readonly ICacheService _cache;

        public LoadBalancerResponseTimeService([FromKeyedServices("memory")] ICacheService cache)
        {
            _cache = cache;
        }

        public async Task<ServiceRegistry?> GetNextServiceAsync(IEnumerable<ServiceRegistry> services)
        {
            if (!services.Any())
                return null;

            IDictionary<string, float> servicesResponseTime = new Dictionary<string, float>();

            foreach (var service in services)
            {
                var responseTimeStr = await _cache.GetAsync<string>(FormatKey(service));
                if (responseTimeStr is null)
                    return service;

                servicesResponseTime.Add(service.Id, float.Parse(responseTimeStr));
            }

            var serviceIdSeletected = servicesResponseTime.OrderBy(x => x.Value).FirstOrDefault().Key;

            return services.First(x => x.Id == serviceIdSeletected);
        }

        public async Task RegisterUsedServiceAsync<T>(ServiceRegistry service, HttpResponse<T> response)
        {
            await _cache.SetAsync(FormatKey(service), response.ResponseTime.TotalMilliseconds.ToString());
        }

        private string FormatKey(ServiceRegistry service)
            => $"{service.Id}_{service.ServiceName}";
    }
}
