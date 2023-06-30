using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Options;
internal static class OptionsMonitorExtensions
{
    internal static ManagementEndpointOptions Get(this IOptionsMonitor<ManagementEndpointOptions> optionsMonitor, EndpointContext platforms) => optionsMonitor.Get(platforms.ToString());
        
}
