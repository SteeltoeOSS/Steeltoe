// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Mappings;

public class MappingsEndpointOptions : AbstractEndpointOptions, IMappingsOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:mappings";

    public MappingsEndpointOptions()
    {
        Id = "mappings";
        RequiredPermissions = Permissions.Restricted;
    }

    public MappingsEndpointOptions(IConfiguration configuration)
        : base(ManagementInfoPrefix, configuration)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "mappings";
        }

        if (RequiredPermissions == Permissions.Undefined)
        {
            RequiredPermissions = Permissions.Restricted;
        }
    }
}
