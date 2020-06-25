// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class CloudFoundryManagementOptions : ManagementEndpointOptions
    {
        private const string DEFAULT_ACTUATOR_PATH = "/cloudfoundryapplication";

        public CloudFoundryManagementOptions()
        {
            Path = DEFAULT_ACTUATOR_PATH;
        }

        public CloudFoundryManagementOptions(IConfiguration config)
            : base(config)
        {
             Path = DEFAULT_ACTUATOR_PATH;
        }
    }
}