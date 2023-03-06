using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Mappings;
internal class ConfigureMappingsEndpointOptions : ConfigureEndpointOptions<MappingsEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:mappings";
    public ConfigureMappingsEndpointOptions(IConfiguration configuration) : base(configuration, ManagementInfoPrefix, "mappings")
    {
    }
}
