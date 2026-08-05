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

        #region GetNextService

        [Fact]
        public void GetNextService_WhenServicesIsEmpty_ReturnsNull()
        {
            var services = new List<ServiceRegistry>();

            var response = _service.GetNextService(services);

            response.Should().BeNull();
        }

        [Fact]
        public void GetNextService_WhenFirstServiceCacheMiss_ReturnsThatServiceImmediately()
        {
            var service1 = _fixture.CreateEntity(1);
            var service2 = _fixture.CreateEntity(2);
            var service3 = _fixture.CreateEntity(3);
            var services = new List<ServiceRegistry> { service1, service2, service3 };

            string? cachedValue = null;
            _mocker.GetMock<IMemoryCacheService>()
                .Setup(m => m.GetMemoryCache($"{service1.Id}_{service1.ServiceName}", out cachedValue))
                .Returns(false);

            var response = _service.GetNextService(services);

            response.Should().Be(service1);
            _mocker.GetMock<IMemoryCacheService>()
                .Verify(m => m.GetMemoryCache($"{service2.Id}_{service2.ServiceName}", out cachedValue), Times.Never);
        }

        [Fact]
        public void GetNextService_WhenAllServicesCacheHit_ReturnsServiceWithLowestResponseTime()
        {
            var service1 = _fixture.CreateEntity(1);
            var service2 = _fixture.CreateEntity(2);
            var service3 = _fixture.CreateEntity(3);
            var services = new List<ServiceRegistry> { service1, service2, service3 };

            string? cachedValue1 = "50";
            string? cachedValue2 = "10";
            string? cachedValue3 = "30";

            _mocker.GetMock<IMemoryCacheService>()
                .Setup(m => m.GetMemoryCache($"{service1.Id}_{service1.ServiceName}", out cachedValue1))
                .Returns(true);
            _mocker.GetMock<IMemoryCacheService>()
                .Setup(m => m.GetMemoryCache($"{service2.Id}_{service2.ServiceName}", out cachedValue2))
                .Returns(true);
            _mocker.GetMock<IMemoryCacheService>()
                .Setup(m => m.GetMemoryCache($"{service3.Id}_{service3.ServiceName}", out cachedValue3))
                .Returns(true);

            var response = _service.GetNextService(services);

            response.Should().Be(service2);
        }

        #endregion

        #region RegisterUsedService

        [Fact]
        public void RegisterUsedService_WhenCalled_SetsMemoryCacheWithIdAndServiceNameKeyAndResponseTimeMilliseconds()
        {
            var service = _fixture.CreateEntity(1);
            var httpResponse = new HttpResponse<object>
            {
                ResponseTime = TimeSpan.FromMilliseconds(123)
            };

            _service.RegisterUsedService(service, httpResponse);

            _mocker.GetMock<IMemoryCacheService>()
                .Verify(m => m.SetMemoryCache($"{service.Id}_{service.ServiceName}", httpResponse.ResponseTime.TotalMilliseconds.ToString()), Times.Once);
        }

        #endregion
    }
}
