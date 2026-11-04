using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Orcamentaria.APIGetaway.Test.Fixtures;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Services;
using Xunit;

namespace Orcamentaria.APIGetaway.Test.Services
{
    [Collection(nameof(ServiceRegistryCollection))]
    public class LoadBalancerResponseTimeServiceTest
    {
        private readonly ServiceRegistryFixture _fixture;
        private readonly AutoMocker _mocker;
        private readonly Application.Services.LoadBalancerResponseTimeService _service;

        public LoadBalancerResponseTimeServiceTest(ServiceRegistryFixture fixture)
        {
            _fixture = fixture;
            _mocker = new AutoMocker();
            _service = _mocker.CreateInstance<Application.Services.LoadBalancerResponseTimeService>();
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
        public async Task GetNextServiceAsync_WhenFirstServiceCacheMiss_ReturnsThatServiceImmediately()
        {
            var service1 = _fixture.CreateEntity(1);
            var service2 = _fixture.CreateEntity(2);
            var service3 = _fixture.CreateEntity(3);
            var services = new List<ServiceRegistry> { service1, service2, service3 };

            _mocker.GetMock<ICacheService>()
                .Setup(m => m.GetAsync<string>($"{service1.Id}_{service1.ServiceName}", It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            var response = await _service.GetNextServiceAsync(services);

            response.Should().Be(service1);
            _mocker.GetMock<ICacheService>()
                .Verify(m => m.GetAsync<string>($"{service2.Id}_{service2.ServiceName}", It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetNextServiceAsync_WhenAllServicesCacheHit_ReturnsServiceWithLowestResponseTime()
        {
            var service1 = _fixture.CreateEntity(1);
            var service2 = _fixture.CreateEntity(2);
            var service3 = _fixture.CreateEntity(3);
            var services = new List<ServiceRegistry> { service1, service2, service3 };

            _mocker.GetMock<ICacheService>()
                .Setup(m => m.GetAsync<string>($"{service1.Id}_{service1.ServiceName}", It.IsAny<CancellationToken>()))
                .ReturnsAsync("50");
            _mocker.GetMock<ICacheService>()
                .Setup(m => m.GetAsync<string>($"{service2.Id}_{service2.ServiceName}", It.IsAny<CancellationToken>()))
                .ReturnsAsync("10");
            _mocker.GetMock<ICacheService>()
                .Setup(m => m.GetAsync<string>($"{service3.Id}_{service3.ServiceName}", It.IsAny<CancellationToken>()))
                .ReturnsAsync("30");

            var response = await _service.GetNextServiceAsync(services);

            response.Should().Be(service2);
        }

        #endregion

        #region RegisterUsedServiceAsync

        [Fact]
        public async Task RegisterUsedServiceAsync_WhenCalled_SetsMemoryCacheWithIdAndServiceNameKeyAndResponseTimeMilliseconds()
        {
            var service = _fixture.CreateEntity(1);
            var httpResponse = new HttpResponse<object>
            {
                ResponseTime = TimeSpan.FromMilliseconds(123)
            };

            await _service.RegisterUsedServiceAsync(service, httpResponse);

            _mocker.GetMock<ICacheService>()
                .Verify(m => m.SetAsync($"{service.Id}_{service.ServiceName}", httpResponse.ResponseTime.TotalMilliseconds.ToString(), null, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}
