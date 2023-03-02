using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.ThreadDump;
internal class ConfigureThreadDumpEndpointOptions : ConfigureEndpointOptions<ThreadDumpEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:dump";

    public ConfigureThreadDumpEndpointOptions(IConfiguration configuration)
        :base(configuration, ManagementInfoPrefix, "threaddump")
    {
    }
}
