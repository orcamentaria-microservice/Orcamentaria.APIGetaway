using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using Orcamentaria.APIGetaway.Test.Fixtures;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Models.Responses;
using Orcamentaria.Lib.Domain.Services;
using Xunit;

namespace Orcamentaria.APIGetaway.Test.Services
{
    [Collection(nameof(ServiceRegistryCollection))]
    public class ServiceRecordDiscoveryServiceTest
    {
        private readonly ServiceRegistryFixture _fixture;
        private readonly AutoMocker _mocker;
        private readonly Application.Services.ServiceRecordDiscoveryService _service;
        private readonly string _baseUrl = "https://service-registry";

        public ServiceRecordDiscoveryServiceTest(ServiceRegistryFixture fixture)
        {
            _fixture = fixture;
            _mocker = new AutoMocker();

            _mocker.GetMock<IOptions<ServiceRegistryConfiguration>>()
                .Setup(o => o.Value)
                .Returns(new ServiceRegistryConfiguration
                {
                    BaseUrl = _baseUrl,
                    Endpoints = new List<ServiceRegistryConfigurationEndpoint>()
                });

            _service = _mocker.CreateInstance<Application.Services.ServiceRecordDiscoveryService>();
        }

        #region DiscoverServiceRegistry

        [Fact]
        public async Task DiscoverServiceRegistry_WhenSuccessful_ReturnsDataFromRegistry()
        {
            var serviceName = "orcamentaria-auth-service";
            var registryData = new List<ServiceRegistry>
            {
                _fixture.CreateEntity(1),
                _fixture.CreateEntity(2)
            };
            var registryResponse = new Response<IEnumerable<ServiceRegistry>>(registryData);

            _mocker.GetMock<IServiceRegistryService>()
                .Setup(s => s.SendServiceRegister<IEnumerable<ServiceRegistry>>(
                    _baseUrl,
                    It.Is<ServiceRegistryConfigurationEndpoint>(e =>
                        e.Name == "ListEndpoints" &&
                        e.Method == "GET" &&
                        e.Route == $"/v1/service/{serviceName}" &&
                        e.RequiredAuthorization == false),
                    null,
                    false))
                .ReturnsAsync(registryResponse);

            var response = await _service.DiscoverServiceRegistry(serviceName);

            response.Success.Should().BeTrue();
            response.Data.Should().BeEquivalentTo(registryData);

            _mocker.GetMock<IServiceRegistryService>()
                .Verify(s => s.SendServiceRegister<IEnumerable<ServiceRegistry>>(
                    _baseUrl,
                    It.Is<ServiceRegistryConfigurationEndpoint>(e =>
                        e.Name == "ListEndpoints" &&
                        e.Method == "GET" &&
                        e.Route == $"/v1/service/{serviceName}"),
                    null,
                    false), Times.Once);
        }

        [Fact]
        public async Task DiscoverServiceRegistry_WhenRegistryReturnsFailure_ReturnsSameFailureResponse()
        {
            var serviceName = "orcamentaria-auth-service";
            var failureResponse = new Response<IEnumerable<ServiceRegistry>>(ErrorCodeEnum.ExternalServiceFailure, "registry unreachable");

            _mocker.GetMock<IServiceRegistryService>()
                .Setup(s => s.SendServiceRegister<IEnumerable<ServiceRegistry>>(
                    It.IsAny<string>(),
                    It.IsAny<ServiceRegistryConfigurationEndpoint>(),
                    null,
                    false))
                .ReturnsAsync(failureResponse);

            var response = await _service.DiscoverServiceRegistry(serviceName);

            response.Success.Should().BeFalse();
            response.Should().BeSameAs(failureResponse);
            response.Error.Should().NotBeNull();
            response.Error!.ErrorCode.Should().Be(ErrorCodeEnum.ExternalServiceFailure);
        }

        [Fact]
        public async Task DiscoverServiceRegistry_WhenExceptionOccurs_ReturnsResponseWithInternalErrorCode()
        {
            var serviceName = "orcamentaria-auth-service";

            _mocker.GetMock<IServiceRegistryService>()
                .Setup(s => s.SendServiceRegister<IEnumerable<ServiceRegistry>>(
                    It.IsAny<string>(),
                    It.IsAny<ServiceRegistryConfigurationEndpoint>(),
                    null,
                    false))
                .ThrowsAsync(new Exception("boom"));

            var response = await _service.DiscoverServiceRegistry(serviceName);

            response.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.ErrorCode.Should().Be(ErrorCodeEnum.InternalError);
            response.Error!.MessageErrors.Should().Contain(m => m.Message.Contains("boom"));
        }

        #endregion

        #region DiscoverServiceRegistryWithEnpoint

        [Fact]
        public async Task DiscoverServiceRegistryWithEnpoint_WhenSuccessful_ReturnsDataFromRegistry()
        {
            var serviceName = "orcamentaria-auth-service";
            var endpointName = "ListEndpoints";
            var registryData = new List<ServiceRegistry>
            {
                _fixture.CreateEntity(1)
            };
            var registryResponse = new Response<IEnumerable<ServiceRegistry>?>(registryData);

            _mocker.GetMock<IServiceRegistryService>()
                .Setup(s => s.SendServiceRegister<IEnumerable<ServiceRegistry>?>(
                    _baseUrl,
                    It.Is<ServiceRegistryConfigurationEndpoint>(e =>
                        e.Name == "Discovery" &&
                        e.Method == "GET" &&
                        e.Route == $"/api/v1/service/{serviceName}/{endpointName}" &&
                        e.RequiredAuthorization == false),
                    null,
                    false))
                .ReturnsAsync(registryResponse);

            var response = await _service.DiscoverServiceRegistryWithEnpoint(serviceName, endpointName);

            response.Success.Should().BeTrue();
            response.Data.Should().BeEquivalentTo(registryData);

            _mocker.GetMock<IServiceRegistryService>()
                .Verify(s => s.SendServiceRegister<IEnumerable<ServiceRegistry>?>(
                    _baseUrl,
                    It.Is<ServiceRegistryConfigurationEndpoint>(e =>
                        e.Route == $"/api/v1/service/{serviceName}/{endpointName}"),
                    null,
                    false), Times.Once);
        }

        [Fact]
        public async Task DiscoverServiceRegistryWithEnpoint_WhenExceptionOccurs_RethrowsSameException()
        {
            var serviceName = "orcamentaria-auth-service";
            var endpointName = "ListEndpoints";

            _mocker.GetMock<IServiceRegistryService>()
                .Setup(s => s.SendServiceRegister<IEnumerable<ServiceRegistry>?>(
                    It.IsAny<string>(),
                    It.IsAny<ServiceRegistryConfigurationEndpoint>(),
                    null,
                    false))
                .ThrowsAsync(new InvalidOperationException("registry down"));

            Func<Task> act = async () => await _service.DiscoverServiceRegistryWithEnpoint(serviceName, endpointName);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be("registry down");
        }

        #endregion
    }
}
