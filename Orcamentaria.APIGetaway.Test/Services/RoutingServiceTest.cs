using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Orcamentaria.APIGetaway.Domain.DTOs;
using Orcamentaria.APIGetaway.Domain.Services;
using Orcamentaria.APIGetaway.Test.Fixtures;
using Orcamentaria.Lib.Domain.Contexts;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Responses;
using Orcamentaria.Lib.Domain.Services;
using Xunit;

namespace Orcamentaria.APIGetaway.Test.Services
{
    [Collection(nameof(ServiceRegistryCollection))]
    public class RoutingServiceTest
    {
        private readonly ServiceRegistryFixture _fixture;
        private readonly AutoMocker _mocker;
        private readonly Application.Services.RoutingService _service;

        public RoutingServiceTest(ServiceRegistryFixture fixture)
        {
            _fixture = fixture;
            _mocker = new AutoMocker();
            _service = _mocker.CreateInstance<Application.Services.RoutingService>();
        }

        #region RoutingRequest

        private RequestDTO CreateDto(IEnumerable<RequestParamDTO>? parameters = null)
        {
            return new RequestDTO
            {
                ServiceName = "service",
                EndpointName = "endpoint",
                Params = parameters
            };
        }

        private void SetupDiscovery(ServiceRegistry service, RequestDTO dto)
        {
            _mocker.GetMock<IServiceRecordDiscoveryService>()
                .Setup(d => d.DiscoverServiceRegistryWithEnpoint(dto.ServiceName, dto.EndpointName))
                .ReturnsAsync(new Response<IEnumerable<ServiceRegistry>>(new List<ServiceRegistry> { service }));
        }

        private HttpResponse<Response<dynamic>> CreateHttpResponse(EndpointRequest endpoint)
        {
            return new HttpResponse<Response<dynamic>>
            {
                Endpoint = endpoint,
                ResponseTime = TimeSpan.FromMilliseconds(10),
                Content = new Response<dynamic>((object)"ok")
            };
        }

        [Fact]
        public async Task RoutingRequest_WhenSuccessful_ReturnsResponseContent()
        {
            var service = _fixture.CreateEntity(1, 1, StateEnum.UP);
            var dto = CreateDto();

            SetupDiscovery(service, dto);

            _mocker.GetMock<ILoadBalancer>()
                .Setup(l => l.GetNextService(It.IsAny<IEnumerable<ServiceRegistry>>()))
                .Returns(service);

            var httpResponse = CreateHttpResponse(service.Endpoints.First());

            _mocker.GetMock<IHttpClientService>()
                .Setup(h => h.SendAsync<dynamic>(It.IsAny<string>(), It.IsAny<EndpointRequest>(), It.IsAny<OptionsRequest>()))
                .ReturnsAsync(httpResponse);

            var result = await _service.RoutingRequest(dto);

            result.Should().BeSameAs(httpResponse.Content);

            _mocker.GetMock<ILoadBalancer>()
                .Verify(l => l.RegisterUsedService(service, httpResponse), Times.Once);
        }

        [Fact]
        public async Task RoutingRequest_WhenParamsProvided_ReplacesRouteParametersBeforeSending()
        {
            var endpoint = _fixture.CreateEndpoint(route: "/orders/{orderId}");
            var service = _fixture.CreateEntity(1, 1, StateEnum.UP);
            service.Endpoints = new List<EndpointRequest> { endpoint };

            var dto = CreateDto(new List<RequestParamDTO>
            {
                new RequestParamDTO { ParamName = "orderId", ParamValue = "123" }
            });

            SetupDiscovery(service, dto);

            _mocker.GetMock<ILoadBalancer>()
                .Setup(l => l.GetNextService(It.IsAny<IEnumerable<ServiceRegistry>>()))
                .Returns(service);

            var httpResponse = CreateHttpResponse(endpoint);

            _mocker.GetMock<IHttpClientService>()
                .Setup(h => h.SendAsync<dynamic>(It.IsAny<string>(), It.IsAny<EndpointRequest>(), It.IsAny<OptionsRequest>()))
                .ReturnsAsync(httpResponse);

            await _service.RoutingRequest(dto);

            _mocker.GetMock<IHttpClientService>()
                .Verify(h => h.SendAsync<dynamic>(
                    It.IsAny<string>(),
                    It.Is<EndpointRequest>(e => e.Route == "/orders/123"),
                    It.IsAny<OptionsRequest>()), Times.Once);
        }

        [Fact]
        public async Task RoutingRequest_WhenDiscoveryFails_ThrowsUnexpectedException()
        {
            var dto = CreateDto();

            _mocker.GetMock<IServiceRecordDiscoveryService>()
                .Setup(d => d.DiscoverServiceRegistryWithEnpoint(dto.ServiceName, dto.EndpointName))
                .ReturnsAsync(new Response<IEnumerable<ServiceRegistry>> { Success = false, Message = "erro" });

            Func<Task> act = async () => await _service.RoutingRequest(dto);

            await act.Should().ThrowAsync<UnexpectedException>();
        }

        [Fact]
        public async Task RoutingRequest_WhenNoServiceAvailable_ThrowsServiceUnavailableException()
        {
            var service = _fixture.CreateEntity(1, 1, StateEnum.UP);
            var dto = CreateDto();

            SetupDiscovery(service, dto);

            _mocker.GetMock<ILoadBalancer>()
                .Setup(l => l.GetNextService(It.IsAny<IEnumerable<ServiceRegistry>>()))
                .Returns((ServiceRegistry?)null);

            Func<Task> act = async () => await _service.RoutingRequest(dto);

            await act.Should().ThrowAsync<ServiceUnavailableException>();
        }

        [Fact]
        public async Task RoutingRequest_WhenRouteHasUnresolvedParams_ThrowsValidationException()
        {
            var endpoint = _fixture.CreateEndpoint(route: "/orders/{orderId}");
            var service = _fixture.CreateEntity(1, 1, StateEnum.UP);
            service.Endpoints = new List<EndpointRequest> { endpoint };

            var dto = CreateDto();

            SetupDiscovery(service, dto);

            _mocker.GetMock<ILoadBalancer>()
                .Setup(l => l.GetNextService(It.IsAny<IEnumerable<ServiceRegistry>>()))
                .Returns(service);

            Func<Task> act = async () => await _service.RoutingRequest(dto);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task RoutingRequest_WhenUserBearerTokenIsNull_UsesServiceBearerToken()
        {
            var service = _fixture.CreateEntity(1, 1, StateEnum.UP);
            var dto = CreateDto();

            SetupDiscovery(service, dto);

            _mocker.GetMock<ILoadBalancer>()
                .Setup(l => l.GetNextService(It.IsAny<IEnumerable<ServiceRegistry>>()))
                .Returns(service);

            _mocker.GetMock<IUserAuthContext>()
                .Setup(c => c.BearerToken)
                .Returns((string)null!);

            _mocker.GetMock<IServiceAuthContext>()
                .Setup(c => c.BearerToken)
                .Returns("serviceToken");

            var httpResponse = CreateHttpResponse(service.Endpoints.First());

            _mocker.GetMock<IHttpClientService>()
                .Setup(h => h.SendAsync<dynamic>(It.IsAny<string>(), It.IsAny<EndpointRequest>(), It.IsAny<OptionsRequest>()))
                .ReturnsAsync(httpResponse);

            await _service.RoutingRequest(dto);

            _mocker.GetMock<IHttpClientService>()
                .Verify(h => h.SendAsync<dynamic>(
                    It.IsAny<string>(),
                    It.IsAny<EndpointRequest>(),
                    It.Is<OptionsRequest>(o => o.TokenAuth == "serviceToken")), Times.Once);
        }

        [Fact]
        public async Task RoutingRequest_WhenUserBearerTokenPresent_UsesUserBearerToken()
        {
            var service = _fixture.CreateEntity(1, 1, StateEnum.UP);
            var dto = CreateDto();

            SetupDiscovery(service, dto);

            _mocker.GetMock<ILoadBalancer>()
                .Setup(l => l.GetNextService(It.IsAny<IEnumerable<ServiceRegistry>>()))
                .Returns(service);

            _mocker.GetMock<IUserAuthContext>()
                .Setup(c => c.BearerToken)
                .Returns("userToken");

            var httpResponse = CreateHttpResponse(service.Endpoints.First());

            _mocker.GetMock<IHttpClientService>()
                .Setup(h => h.SendAsync<dynamic>(It.IsAny<string>(), It.IsAny<EndpointRequest>(), It.IsAny<OptionsRequest>()))
                .ReturnsAsync(httpResponse);

            await _service.RoutingRequest(dto);

            _mocker.GetMock<IHttpClientService>()
                .Verify(h => h.SendAsync<dynamic>(
                    It.IsAny<string>(),
                    It.IsAny<EndpointRequest>(),
                    It.Is<OptionsRequest>(o => o.TokenAuth == "userToken")), Times.Once);
        }

        [Fact]
        public async Task RoutingRequest_WhenDiscoveryServiceThrowsDefaultException_RethrowsSameExceptionType()
        {
            var dto = CreateDto();

            _mocker.GetMock<IServiceRecordDiscoveryService>()
                .Setup(d => d.DiscoverServiceRegistryWithEnpoint(dto.ServiceName, dto.EndpointName))
                .ThrowsAsync(new ServiceUnavailableException("down"));

            Func<Task> act = async () => await _service.RoutingRequest(dto);

            await act.Should().ThrowAsync<ServiceUnavailableException>();
        }

        [Fact]
        public async Task RoutingRequest_WhenUnexpectedExceptionOccurs_ThrowsUnexpectedException()
        {
            var service = _fixture.CreateEntity(1, 1, StateEnum.UP);
            var dto = CreateDto();

            SetupDiscovery(service, dto);

            _mocker.GetMock<ILoadBalancer>()
                .Setup(l => l.GetNextService(It.IsAny<IEnumerable<ServiceRegistry>>()))
                .Returns(service);

            _mocker.GetMock<IHttpClientService>()
                .Setup(h => h.SendAsync<dynamic>(It.IsAny<string>(), It.IsAny<EndpointRequest>(), It.IsAny<OptionsRequest>()))
                .ThrowsAsync(new Exception("boom"));

            Func<Task> act = async () => await _service.RoutingRequest(dto);

            await act.Should().ThrowAsync<UnexpectedException>();
        }

        #endregion
    }
}
