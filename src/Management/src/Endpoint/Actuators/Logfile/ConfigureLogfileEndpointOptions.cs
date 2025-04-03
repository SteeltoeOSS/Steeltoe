// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Logfile;

internal sealed class ConfigureLogfileEndpointOptions(IConfiguration configuration)
    : ConfigureEndpointOptions<LogfileEndpointOptions>(configuration, ManagementInfoPrefix, "logfile")
{
    private const string ManagementInfoPrefix = "management:endpoints:logfile";

    public override void Configure(LogfileEndpointOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        base.Configure(options);

        options.FilePath = configuration.GetValue<string?>($"{ManagementInfoPrefix}:filePath");
    }
}
