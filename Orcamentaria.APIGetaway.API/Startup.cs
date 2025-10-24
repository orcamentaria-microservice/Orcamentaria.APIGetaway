using Orcamentaria.APIGetaway.Application.Providers;
using Orcamentaria.APIGetaway.Application.Services;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Providers;
using Orcamentaria.Lib.Domain.Services;
using Orcamentaria.Lib.Infrastructure;

namespace Orcamentaria.APIGetaway.API
{
    public class Startup
    {
        private readonly string _serviceName = "Orcamentaria.APIGetaway";
        private readonly string _apiVersion = "v1";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            CommonDI.ResolveCommonServices(_serviceName, _apiVersion, services, Configuration, () =>
            {
                services.AddScoped<ITokenProvider, TokenProvider>();
                services.AddScoped<IDiscoveryServiceRegistryService, DiscoveryServiceRegistryService>();
                services.AddScoped<IRoutingService, RoutingService>();
                services.AddScoped<ILoadBalancer, LoadBalancerResponseTimeService>();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            => CommonDI.ConfigureCommon(_serviceName, _apiVersion, app, env);
    }
}
