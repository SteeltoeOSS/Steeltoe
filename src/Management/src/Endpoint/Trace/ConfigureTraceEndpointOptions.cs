using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Trace;

internal class ConfigureTraceEndpointOptions : IConfigureNamedOptions<TraceEndpointOptions>
{

    private const string ManagementInfoPrefixV1 = "management:endpoints:trace";
    private const string ManagementInfoPrefix = "management:endpoints:httptrace";
    private readonly IConfiguration _configuration;
    private const int DefaultCapacity = 100;

    public enum TraceEndpointOptionNames
    {
        V1,
        V2
    }


    public ConfigureTraceEndpointOptions(IConfiguration configuration)
    {
        this._configuration = configuration;
    }

    public void Configure(string name, TraceEndpointOptions options)
    {
        if (name == TraceEndpointOptionNames.V2.ToString()
            || name == string.Empty)
        {
            Configure(options);
        }
        else
        {
            _configuration.GetSection(ManagementInfoPrefixV1).Bind(options);
            if (string.IsNullOrEmpty(options.Id))
            {
                options.Id = "trace";
            }
        }

        if (options.Capacity == -1)
        {
            options.Capacity = DefaultCapacity;
        }
    }

    public void Configure(TraceEndpointOptions options)
    {
        _configuration.GetSection(ManagementInfoPrefix).Bind(options);

        if (string.IsNullOrEmpty(options.Id))
        {
            options.Id = "httptrace";
        }
    }
}