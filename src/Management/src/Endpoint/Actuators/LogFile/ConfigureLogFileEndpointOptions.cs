// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

internal sealed class ConfigureLogFileEndpointOptions(IConfiguration configuration)
    : ConfigureEndpointOptions<LogFileEndpointOptions>(configuration, "Management:Endpoints:LogFile", "logfile")
{
    public override void Configure(LogFileEndpointOptions options)
    {
        base.Configure(options);

        if (string.IsNullOrWhiteSpace(options.FilePath))
        {
            // Like in Spring Boot, there's nothing to expose until a log file has been configured.
            options.Enabled ??= false;
        }
    }
}
