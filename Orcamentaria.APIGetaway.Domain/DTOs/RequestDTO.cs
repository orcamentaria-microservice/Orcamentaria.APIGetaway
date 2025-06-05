namespace Orcamentaria.APIGetaway.Domain.DTOs
{
    public class RequestDTO
    {
        public required string ServiceName { get; set; }
        public required string EndpointName { get; set; }
        public IEnumerable<RequestParamDTO>? Params { get; set; }
        public object? Content { get; set; }
    }
}
