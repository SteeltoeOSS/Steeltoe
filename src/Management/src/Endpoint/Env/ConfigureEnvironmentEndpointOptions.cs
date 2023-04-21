// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Env;

internal sealed class ConfigureEnvironmentEndpointOptions : IConfigureOptions<EnvironmentEndpointOptions>
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

    private readonly IConfiguration _configuration;

    public ConfigureEnvironmentEndpointOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(EnvironmentEndpointOptions options)
    {
        _configuration.GetSection(ManagementInfoPrefix).Bind(options);

        options.Id ??= "env";

        if (options.RequiredPermissions == Permissions.Undefined)
        {
            options.RequiredPermissions = Permissions.Restricted;
        }

        options.KeysToSanitize ??= DefaultKeysToSanitize;
    }
}
