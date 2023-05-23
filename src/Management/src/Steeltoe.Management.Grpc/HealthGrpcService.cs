using Steeltoe.Common.Attributes;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Options;
using System.ServiceModel;

namespace Steeltoe.Management.Grpc;

[ServiceContract]
public interface IHealthGrpc
{
    [OperationContract]
    ValueTask<HealthGrpcResult> GetHealth();
}

public class HealthGrpcService : IHealthGrpc
{
    private readonly IHealthEndpoint _endpoint;

    public HealthGrpcService(IHealthEndpoint endpoint)
    {
        _endpoint = endpoint;
    }

    public async ValueTask<HealthGrpcResult> GetHealth()
    {
        var request = new HealthEndpointRequest() { GroupName = string.Empty, HasClaim = true };
        HealthEndpointResponse result = await ((HealthEndpointHandler)_endpoint).InvokeAsync(request, CancellationToken.None);
        return new HealthGrpcResult(result);

    }
}
