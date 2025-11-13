using Orcamentaria.APIGetaway.Application.Providers;
using Orcamentaria.APIGetaway.Application.Services;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.Lib.Domain.Providers;
using Orcamentaria.Lib.Infrastructure.Configures;

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

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ResolveCommonServices(configuration: Configuration,
                serviceName: _serviceName,
                apiVersion: _apiVersion,
                customServices: () =>
            {
                services.AddScoped<ITokenProvider, TokenProvider>();
                services.AddScoped<IServiceRecordDiscoveryService, ServiceRecordDiscoveryService>();
                services.AddScoped<IRoutingService, RoutingService>();
                services.AddScoped<ILoadBalancer, LoadBalancerResponseTimeService>();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            => app.ConfigureCommon(env, _serviceName, _apiVersion);
    }
}
