using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.ThreadDump;

internal class ConfigureThreadDumpEndpointOptionsV1 : ConfigureEndpointOptions<ThreadDumpEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:dump";

    public ConfigureThreadDumpEndpointOptionsV1(IConfiguration configuration)
        : base(configuration, ManagementInfoPrefix, "dump")
    {
    }
}