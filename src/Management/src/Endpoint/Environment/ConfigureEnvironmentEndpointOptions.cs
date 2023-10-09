// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Environment;

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

    private readonly IConfiguration _configuration;

    public ConfigureEnvironmentEndpointOptions(IConfiguration configuration): base(configuration, ManagementInfoPrefix, "env")
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public override void Configure(EnvironmentEndpointOptions options)
    {
        ArgumentGuard.NotNull(options);

        base.Configure(options);

        // It's not possible to distinguish between null and an empty list in configuration.
        // See https://github.com/dotnet/extensions/issues/1341.
        if (options.KeysToSanitize.Count == 0)
        {
            options.KeysToSanitize = DefaultKeysToSanitize;
        }
    }
}
