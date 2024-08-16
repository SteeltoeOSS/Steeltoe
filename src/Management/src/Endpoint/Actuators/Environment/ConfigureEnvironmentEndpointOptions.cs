// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Environment;

internal sealed class ConfigureEnvironmentEndpointOptions : ConfigureEndpointOptions<EnvironmentEndpointOptions>
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

    public ConfigureEnvironmentEndpointOptions(IConfiguration configuration)
        : base(configuration, ManagementInfoPrefix, "env")
    {
    }

    public override void Configure(EnvironmentEndpointOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        base.Configure(options);

        // It's not possible to distinguish between null and an empty list in configuration.
        // See https://github.com/dotnet/extensions/issues/1341.
        if (options.KeysToSanitize.Count == 0)
        {
            options.KeysToSanitize = DefaultKeysToSanitize;
        }
    }
}
