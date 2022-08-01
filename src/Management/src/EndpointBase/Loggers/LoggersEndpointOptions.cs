// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Loggers;

public class LoggersEndpointOptions : AbstractEndpointOptions, ILoggersOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:loggers";

    public LoggersEndpointOptions()
    {
        Id = "loggers";
        AllowedVerbs = new List<string> { "Get", "Post" };
        ExactMatch = false;
    }

    public LoggersEndpointOptions(IConfiguration config)
        : base(ManagementInfoPrefix, config)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "loggers";
        }

        AllowedVerbs = new List<string> { "Get", "Post" };
        ExactMatch = false;
    }
}
