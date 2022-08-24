// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Info;

public class InfoEndpointOptions : AbstractEndpointOptions, IInfoOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:info";

    public InfoEndpointOptions()
    {
        Id = "info";
        RequiredPermissions = Permissions.Restricted;
    }

    public InfoEndpointOptions(IConfiguration configuration)
        : base(ManagementInfoPrefix, configuration)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "info";
        }

        if (RequiredPermissions == Permissions.Undefined)
        {
            RequiredPermissions = Permissions.Restricted;
        }
    }
}
