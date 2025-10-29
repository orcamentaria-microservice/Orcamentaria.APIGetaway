using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.APIGetaway.Application.Services
{
    public class LoadBalancerResponseTimeService : ILoadBalancer
    {
        private readonly IMemoryCacheService _memoryCacheService;

        public LoadBalancerResponseTimeService(IMemoryCacheService memoryCacheService)
        {
            _memoryCacheService = memoryCacheService;
        }

        public ServiceRegistry? GetNextService(IEnumerable<ServiceRegistry> services)
        {
            if (!services.Any())
                return null;

            IDictionary<string, float> servicesResponseTime = new Dictionary<string, float>();

            foreach (var service in services)
            {
                if (!_memoryCacheService.GetMemoryCache(FormatKey(service), out string responseTimeService))
                    return service;

                servicesResponseTime.Add(service.Id, float.Parse(responseTimeService));
            }

            var serviceIdSeletected = servicesResponseTime.OrderBy(x => x.Value).FirstOrDefault().Key;

            return services.First(x => x.Id == serviceIdSeletected);
        }

        public void RegisterUsedService<T>(ServiceRegistry service, HttpResponse<T> response)
        {
            _memoryCacheService.SetMemoryCache(FormatKey(service), response.ResponseTime.TotalMilliseconds.ToString());
        }

        private string FormatKey(ServiceRegistry service)
            => $"{service.Id}_{service.ServiceName}";
    }
}
