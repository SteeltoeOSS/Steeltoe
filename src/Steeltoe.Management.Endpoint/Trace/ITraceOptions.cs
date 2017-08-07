

namespace Steeltoe.Management.Endpoint.Trace
{
    public interface ITraceOptions : IEndpointOptions
    {
        int Capacity { get; }
        bool AddRequestHeaders { get; }
        bool AddResponseHeaders { get; }
        bool AddPathInfo { get; }
        bool AddUserPrincipal { get; }
        bool AddParameters { get; }
        bool AddQueryString { get; }
        bool AddAuthType { get; }
        bool AddRemoteAddress { get; }
        bool AddSessionId { get; }
        bool AddTimeTaken { get; }
    }
}
