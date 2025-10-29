using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orcamentaria.APIGetaway.Application.Services;
using Orcamentaria.APIGetaway.Domain.DTOs;
using Orcamentaria.Lib.Application.Services;
using Orcamentaria.Lib.Domain.DTOs.ConfigurationBag;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Infrastructure.Contexts;

namespace Orcamentaria.APIGetaway.Infrastructure.Initializers
{
    public class ConfigurationBagInitializer
    {
        private readonly string _serviceName;

        public ConfigurationBagInitializer(string serviceName)
        {
            _serviceName = serviceName;
        }

        public async Task<IConfigurationRoot> InitializeAsync(IConfiguration configuration, IServiceCollection services)
        {
            try
            {
                var httpClient = new HttpClient();
                var httpContextAccessor = new HttpContextAccessor();
                var httpClientService = new HttpClientService(httpClient, httpContextAccessor);

                var config = new MemoryCacheOptions()
                {
                    SizeLimit = 1024 * 1024 * 100
                };
                var memoryOptions = Options.Create(config);
                var memoryCache = new MemoryCache(memoryOptions);
                var memoryCacheService = new MemoryCacheService(memoryCache);

                var provider = services.BuildServiceProvider();
                var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

                var serviceRegistry = new ServiceRegistryService(
                    scopeFactory,
                    httpClientService,
                    memoryCacheService);
                
                var serviceRegistryConfig = new ServiceRegistryConfiguration
                {
                    BaseUrl = configuration.GetSection("BaseUrlServiceRegistry").Value,
                    Endpoints = new List<ServiceRegistryConfigurationEndpoint>()
                    {
                        new ServiceRegistryConfigurationEndpoint
                        {
                            Name = "Discovery",
                            Method = "GET",
                            Route = "/v1/service/{serviceName}/{endpointName}",
                            RequiredAuthorization = false
                        }
                    }
                };

                var ServiceRegistryConfigurationOptions = Options.Create(serviceRegistryConfig);

                var discoveryServiceRegistry= new DiscoveryServiceRegistryService(
                    serviceRegistry,
                    ServiceRegistryConfigurationOptions);

                var loadBalancer = new LoadBalancerResponseTimeService(memoryCacheService);

                var userAuth = new UserAuthContext();
                
                var routingService = new RoutingService(
                    discoveryServiceRegistry,
                    httpClientService,
                    userAuth,
                    loadBalancer);

                var dto = new RequestDTO
                {
                    ServiceName = "AuthService",
                    EndpointName = "AuthenticateBootstrap",
                    Params = new List<RequestParamDTO>
                    {
                        new RequestParamDTO
                        {
                            ParamName = "bootstrapToken",
                            ParamValue = _serviceName
                        }
                    }
                };

                var responsea = await routingService.RoutingRequest(dto);

                var baseUrlApiGetaway = configuration.GetSection("BaseUrlApiGetaway").Value;

                if (string.IsNullOrEmpty(baseUrlApiGetaway))
                    throw new ConfigurationException("BaseUrlApiGetaway não configurado.");

                var configa = new ApiGetawayConfiguration
                {
                    BaseUrl = baseUrlApiGetaway,
                };

                var options = Options.Create(configa);

                var client = new ApiGetawayService(httpClientService, options);

                var token = "";

                var resource = new ResourceConfiguration
                {
                    ServiceName = "ConfigBagService",
                    EndpointName = "ConfigurationBagGetByServiceName",
                };

                IDictionary<string, string> @params = new Dictionary<string, string>();

                @params.Add("serviceName", _serviceName);

                var response = await client.Routing<ConfigurationBagResponseDTO>(
                        baseUrlApiGetaway,
                        resource.ServiceName,
                        resource.EndpointName,
                        token,
                        @params,
                        null);

                if (!response.Success)
                    throw new Lib.Domain.Exceptions.ConfigurationException($"Erro ao buscar configurações do serviço {_serviceName}");

                var dict = ToAppsettingsDictionary(response.Data);

                var newConfig = new ConfigurationBuilder()
                    .AddConfiguration(configuration)
                    .AddInMemoryCollection(dict)
                    .Build();

                return newConfig;
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }

        #region private methods

        private static Dictionary<string, string> ToAppsettingsDictionary(ConfigurationBagResponseDTO bag)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(bag.ServiceName))
                dict["ConfigurationBag:ServiceName"] = bag.ServiceName;
            dict["ConfigurationBag:UpdateAt"] = bag.UpdateAt.ToString("O");

            if (bag.ConnectionStrings is not null)
            {
                foreach (var row in bag.ConnectionStrings)
                {
                    if (row is null) continue;

                    if (row.TryGetValue("Name", out var name) &&
                        (row.TryGetValue("ConnectionString", out var cs) || row.TryGetValue("Value", out cs)))
                    {
                        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(cs))
                            dict[$"ConnectionStrings:{name}"] = cs;
                        continue;
                    }

                    foreach (var kv in row)
                    {
                        if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                            dict[$"ConnectionStrings:{kv.Key}"] = kv.Value;
                    }
                }
            }
            if (bag.Configurations is not null)
            {
                foreach (var item in bag.Configurations)
                {
                    if (item is null) continue;

                    foreach (var (sectionName, sectionValue) in item)
                    {
                        FlattenObject(dict, sectionValue, sectionName);
                    }
                }
            }

            return dict;
        }

        private static void FlattenObject(Dictionary<string, string> bag, object? value, string prefix)
        {
            if (value is null) return;

            switch (value)
            {
                case string s:
                    bag[prefix] = s;
                    return;

                case bool b:
                    bag[prefix] = b.ToString();
                    return;

                case int or long or short or byte or double or float or decimal:
                    bag[prefix] = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;
                    return;

                case DateTime dt:
                    bag[prefix] = dt.ToString("O");
                    return;

                case IDictionary<string, object> dictObj:
                    foreach (var (k, v) in dictObj)
                        FlattenObject(bag, v, $"{prefix}:{k}");
                    return;

                case IEnumerable<object> list:
                    int i = 0;
                    foreach (var item in list)
                        FlattenObject(bag, item, $"{prefix}:{i++}");
                    return;

                default:
                    var json = System.Text.Json.JsonSerializer.Serialize(value);
                    var el = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    FlattenJsonElement(bag, el, prefix);
                    return;
            }
        }

        private static void FlattenJsonElement(Dictionary<string, string> bag, System.Text.Json.JsonElement el, string prefix)
        {
            switch (el.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Object:
                    foreach (var p in el.EnumerateObject())
                        FlattenJsonElement(bag, p.Value, $"{prefix}:{p.Name}");
                    break;

                case System.Text.Json.JsonValueKind.Array:
                    int i = 0;
                    foreach (var item in el.EnumerateArray())
                        FlattenJsonElement(bag, item, $"{prefix}:{i++}");
                    break;

                case System.Text.Json.JsonValueKind.String:
                    bag[prefix] = el.GetString()!;
                    break;

                case System.Text.Json.JsonValueKind.Number:
                    bag[prefix] = el.GetRawText();
                    break;

                case System.Text.Json.JsonValueKind.True:
                case System.Text.Json.JsonValueKind.False:
                    bag[prefix] = el.GetBoolean().ToString();
                    break;
            }
        }

        #endregion
    }
}
