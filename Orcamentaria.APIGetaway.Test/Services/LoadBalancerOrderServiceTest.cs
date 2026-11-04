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

        #region GetNextServiceAsync

        [Fact]
        public async Task GetNextServiceAsync_WhenServicesIsEmpty_ReturnsNull()
        {
            var services = new List<ServiceRegistry>();

            var response = await _service.GetNextServiceAsync(services);

            response.Should().BeNull();
        }

        [Fact]
        public async Task GetNextServiceAsync_WhenCacheMiss_ReturnsFirstServiceOrderedByOrder()
        {
            var services = new List<ServiceRegistry>
            {
                _fixture.CreateEntity(1, 3, StateEnum.UP),
                _fixture.CreateEntity(2, 1, StateEnum.UP),
                _fixture.CreateEntity(3, 2, StateEnum.UP)
            };

            _mocker.GetMock<ICacheService>()
                .Setup(m => m.GetAsync<string>(services.First().ServiceName, It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            var response = await _service.GetNextServiceAsync(services);

            response.Should().NotBeNull();
            response!.Order.Should().Be(1);
        }

        [Fact]
        public async Task GetNextServiceAsync_WhenCacheHitAndHigherOrderExists_ReturnsFirstServiceWithOrderGreaterThanCached()
        {
            var services = new List<ServiceRegistry>
            {
                _fixture.CreateEntity(1, 1, StateEnum.UP),
                _fixture.CreateEntity(2, 2, StateEnum.UP),
                _fixture.CreateEntity(3, 3, StateEnum.UP),
                _fixture.CreateEntity(4, 4, StateEnum.UP)
            };

            _mocker.GetMock<ICacheService>()
                .Setup(m => m.GetAsync<string>(services.First().ServiceName, It.IsAny<CancellationToken>()))
                .ReturnsAsync("2");

            var response = await _service.GetNextServiceAsync(services);

            response.Should().NotBeNull();
            response!.Order.Should().Be(3);
        }

        [Fact]
        public async Task GetNextServiceAsync_WhenCacheHitButNoHigherOrderExists_ReturnsFirstServiceOrderedByOrder()
        {
            var services = new List<ServiceRegistry>
            {
                _fixture.CreateEntity(1, 2, StateEnum.UP),
                _fixture.CreateEntity(2, 3, StateEnum.UP),
                _fixture.CreateEntity(3, 1, StateEnum.UP)
            };

            _mocker.GetMock<ICacheService>()
                .Setup(m => m.GetAsync<string>(services.First().ServiceName, It.IsAny<CancellationToken>()))
                .ReturnsAsync("3");

            var response = await _service.GetNextServiceAsync(services);

            response.Should().NotBeNull();
            response!.Order.Should().Be(1);
        }

        #endregion

        #region RegisterUsedServiceAsync

        [Fact]
        public async Task RegisterUsedServiceAsync_WhenCalled_SetsMemoryCacheWithServiceNameAndEndpointOrder()
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

            await _service.RegisterUsedServiceAsync(service, response);

            _mocker.GetMock<ICacheService>()
                .Verify(m => m.SetAsync(service.ServiceName, endpoint.Order.ToString(), null, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}
