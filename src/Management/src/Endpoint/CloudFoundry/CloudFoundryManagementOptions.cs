// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class CloudFoundryManagementOptions : ManagementEndpointOptions
{
    private const string DefaultActuatorPath = "/cloudfoundryapplication";

    public CloudFoundryManagementOptions()
    {
        Path = DefaultActuatorPath;
    }

    public CloudFoundryManagementOptions(IConfiguration configuration)
        : base(configuration)
    {
        Path = DefaultActuatorPath;
    }
}
