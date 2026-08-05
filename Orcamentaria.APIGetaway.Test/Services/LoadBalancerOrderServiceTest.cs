using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Orcamentaria.APIGetaway.Test.Fixtures;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Services;
using Xunit;

namespace Orcamentaria.APIGetaway.Test.Services
{
    [Collection(nameof(ServiceRegistryCollection))]
    public class LoadBalancerOrderServiceTest
    {
        private readonly ServiceRegistryFixture _fixture;
        private readonly AutoMocker _mocker;
        private readonly Application.Services.LoadBalancerOrderService _service;

        public LoadBalancerOrderServiceTest(ServiceRegistryFixture fixture)
        {
            _fixture = fixture;
            _mocker = new AutoMocker();
            _service = _mocker.CreateInstance<Application.Services.LoadBalancerOrderService>();
        }

        #region GetNextService

        [Fact]
        public void GetNextService_WhenServicesIsEmpty_ReturnsNull()
        {
            var services = new List<ServiceRegistry>();

            var response = _service.GetNextService(services);

            response.Should().BeNull();
        }

        [Fact]
        public void GetNextService_WhenCacheMiss_ReturnsFirstServiceOrderedByOrder()
        {
            var services = new List<ServiceRegistry>
            {
                _fixture.CreateEntity(1, 3, StateEnum.UP),
                _fixture.CreateEntity(2, 1, StateEnum.UP),
                _fixture.CreateEntity(3, 2, StateEnum.UP)
            };

            string? cachedValue = null;
            _mocker.GetMock<IMemoryCacheService>()
                .Setup(m => m.GetMemoryCache(services.First().ServiceName, out cachedValue))
                .Returns(false);

            var response = _service.GetNextService(services);

            response.Should().NotBeNull();
            response!.Order.Should().Be(1);
        }

        [Fact]
        public void GetNextService_WhenCacheHitAndHigherOrderExists_ReturnsFirstServiceWithOrderGreaterThanCached()
        {
            var services = new List<ServiceRegistry>
            {
                _fixture.CreateEntity(1, 1, StateEnum.UP),
                _fixture.CreateEntity(2, 2, StateEnum.UP),
                _fixture.CreateEntity(3, 3, StateEnum.UP),
                _fixture.CreateEntity(4, 4, StateEnum.UP)
            };

            string? cachedValue = "2";
            _mocker.GetMock<IMemoryCacheService>()
                .Setup(m => m.GetMemoryCache(services.First().ServiceName, out cachedValue))
                .Returns(true);

            var response = _service.GetNextService(services);

            response.Should().NotBeNull();
            response!.Order.Should().Be(3);
        }

        [Fact]
        public void GetNextService_WhenCacheHitButNoHigherOrderExists_ReturnsFirstServiceOrderedByOrder()
        {
            var services = new List<ServiceRegistry>
            {
                _fixture.CreateEntity(1, 2, StateEnum.UP),
                _fixture.CreateEntity(2, 3, StateEnum.UP),
                _fixture.CreateEntity(3, 1, StateEnum.UP)
            };

            string? cachedValue = "3";
            _mocker.GetMock<IMemoryCacheService>()
                .Setup(m => m.GetMemoryCache(services.First().ServiceName, out cachedValue))
                .Returns(true);

            var response = _service.GetNextService(services);

            response.Should().NotBeNull();
            response!.Order.Should().Be(1);
        }

        #endregion

        #region RegisterUsedService

        [Fact]
        public void RegisterUsedService_WhenCalled_SetsMemoryCacheWithServiceNameAndEndpointOrder()
        {
            var service = _fixture.CreateEntity(1);
            var endpoint = new EndpointRequest
            {
                Name = "endpoint",
                Route = "/endpoint",
                Method = "GET",
                Order = 5
            };
            var response = new HttpResponse<object>
            {
                Endpoint = endpoint
            };

            _service.RegisterUsedService(service, response);

            _mocker.GetMock<IMemoryCacheService>()
                .Verify(m => m.SetMemoryCache(service.ServiceName, endpoint.Order.ToString()), Times.Once);
        }

        #endregion
    }
}
