using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.ThreadDump;
internal class ConfigureThreadDumpEndpointOptions : IConfigureOptions<ThreadDumpEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:dump";

    private readonly IConfiguration configuration;

    public ConfigureThreadDumpEndpointOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(ThreadDumpEndpointOptions options)
    {
        configuration.GetSection(ManagementInfoPrefix).Bind(options);
        if (string.IsNullOrEmpty(options.Id))
        {
            options.Id = "threaddump";
        }
    }
}
