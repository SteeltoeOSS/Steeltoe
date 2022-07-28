// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System;

namespace Steeltoe.Management.Endpoint.Hypermedia;

public class ActuatorManagementOptions : ManagementEndpointOptions
{
    private const string DefaultActuatorPath = "/actuator";

    public Exposure Exposure { get; set; }

    public ActuatorManagementOptions()
    {
        Path = DefaultActuatorPath;
        Exposure = new Exposure();
    }

    public ActuatorManagementOptions(IConfiguration config)
        : base(config)
    {
        if (string.IsNullOrEmpty(Path))
        {
            Path = DefaultActuatorPath;
        }

        if (Platform.IsCloudFoundry && Path.StartsWith("/cloudfoundryapplication", StringComparison.OrdinalIgnoreCase))
        {
            Path = DefaultActuatorPath; // Override path set to /cloudfoundryapplication since it will be hidden by the cloudfoundry context actuators
        }

        Exposure = new Exposure(config);
    }
}
