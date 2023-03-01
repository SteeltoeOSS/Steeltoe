using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;
internal class ConfigureHealthEndpointOptions : IConfigureOptions<HealthEndpointOptions>
{
    private readonly IConfiguration configuration;
    private const string HealthOptionsPrefix = "management:endpoints:health";
    public ConfigureHealthEndpointOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(HealthEndpointOptions options)
    {
        configuration.GetSection(HealthOptionsPrefix).Bind(options);
        
        options.Id ??= "health";

        if (options.RequiredPermissions == Permissions.Undefined)
        {
            options.RequiredPermissions = Permissions.Restricted;
        }

        if (options.Claim == null && !string.IsNullOrEmpty(options.Role))
        {
            options.Claim = new EndpointClaim
            {
                Type = ClaimTypes.Role,
                Value = options.Role
            };
        }

        if (!options.Groups.ContainsKey("liveness"))
        {
            options.Groups.Add("liveness", new HealthGroupOptions
            {
                Include = "liveness"
            });
        }

        if (!options.Groups.ContainsKey("readiness"))
        {
            options.Groups.Add("readiness", new HealthGroupOptions
            {
                Include = "readiness"
            });
        }
    }
}
