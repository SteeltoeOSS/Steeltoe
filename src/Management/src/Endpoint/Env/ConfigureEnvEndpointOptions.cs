using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Env;
internal class ConfigureEnvEndpointOptions : IConfigureOptions<EnvEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:env";

    private static readonly string[] DefaultKeysToSanitize =
    {
        "password",
        "secret",
        "key",
        "token",
        ".*credentials.*",
        "vcap_services"
    };
    private readonly IConfiguration configuration;

    public ConfigureEnvEndpointOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(EnvEndpointOptions options)
    {
        configuration.GetSection(ManagementInfoPrefix).Bind(options);

        options.Id ??= "env";

        if(options.RequiredPermissions == Permissions.Undefined)
        {
            options.RequiredPermissions = Permissions.Restricted;
        }

        options.KeysToSanitize ??= DefaultKeysToSanitize;
    }
}
