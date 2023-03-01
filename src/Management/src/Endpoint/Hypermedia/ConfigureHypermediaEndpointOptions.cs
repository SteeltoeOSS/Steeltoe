using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Hypermedia;
internal class ConfigureHypermediaEndpointOptions : IConfigureOptions<HypermediaEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:actuator";
    private readonly IConfiguration configuration;

    public ConfigureHypermediaEndpointOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(HypermediaEndpointOptions options)
    {
        configuration.GetSection(ManagementInfoPrefix).Bind(options);

        options.Id ??= string.Empty;
    }
}
