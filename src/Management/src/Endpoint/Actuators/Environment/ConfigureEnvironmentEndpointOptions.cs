// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Environment;

internal sealed class ConfigureEnvironmentEndpointOptions(IConfiguration configuration)
    : ConfigureEndpointOptions<EnvironmentEndpointOptions>(configuration, "Management:Endpoints:Env", "env")
{
    private static readonly string[] DefaultKeysToSanitize =
    [
        "password",
        "secret",
        "key",
        "token",
        ".*credentials.*",
        "vcap_services"
    ];

    public override void Configure(EnvironmentEndpointOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        base.Configure(options);

        if (options.KeysToSanitize.Count == 0)
        {
            foreach (string defaultKey in DefaultKeysToSanitize)
            {
                options.KeysToSanitize.Add(defaultKey);
            }
        }
        else if (options.KeysToSanitize is [""])
        {
            // When binding a collection property from a JSON configuration file, setting the key to null or an empty
            // collection is ignored. As a workaround, we interpret a single empty string element to clear the defaults.

            options.KeysToSanitize.Clear();
        }
    }
}
