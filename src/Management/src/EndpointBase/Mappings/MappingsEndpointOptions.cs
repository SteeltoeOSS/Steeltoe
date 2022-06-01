// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Mappings;

public class MappingsEndpointOptions : AbstractEndpointOptions, IMappingsOptions
{
    private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:mappings";

    public MappingsEndpointOptions()
    {
        Id = "mappings";
        RequiredPermissions = Permissions.RESTRICTED;
    }

    public MappingsEndpointOptions(IConfiguration config)
        : base(MANAGEMENT_INFO_PREFIX, config)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "mappings";
        }

        if (RequiredPermissions == Permissions.UNDEFINED)
        {
            RequiredPermissions = Permissions.RESTRICTED;
        }
    }
}
