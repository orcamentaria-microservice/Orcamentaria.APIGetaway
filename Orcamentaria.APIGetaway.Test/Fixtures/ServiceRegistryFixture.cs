using Bogus;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Test.Fixtures;

namespace Orcamentaria.APIGetaway.Test.Fixtures
{
    [CollectionDefinition(nameof(ServiceRegistryCollection))]
    public class ServiceRegistryCollection : ICollectionFixture<ServiceRegistryFixture> { }

    public class ServiceRegistryFixture : BaseFixture<ServiceRegistry>
    {
        override
        public ServiceRegistry CreateEntity(long id)
        {
            return new ServiceRegistry
            {
                Id = id.ToString(),
                Order = (int)id,
                ServiceName = Faker.Commerce.Department(),
                BaseUrl = Faker.Internet.Url(),
                State = CreateState(StateEnum.UP),
                Endpoints = new List<EndpointRequest> { CreateEndpoint() }
            };
        }

        public ServiceRegistry CreateEntity(long id, int order, StateEnum state)
        {
            var entity = CreateEntity(id);
            entity.Order = order;
            entity.State = CreateState(state);

            return entity;
        }

        public StateService CreateState(StateEnum state)
        {
            return new StateService
            {
                StateId = state,
                StateName = state.ToString()
            };
        }

        public EndpointRequest CreateEndpoint(string? name = null, string? route = null, string? method = null)
        {
            return new EndpointRequest
            {
                Name = name ?? Faker.Random.Word(),
                Route = route ?? $"/{Faker.Random.Word()}",
                Method = method ?? "GET"
            };
        }
    }
}
