// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public class HypermediaEndpointOptions : AbstractEndpointOptions, IActuatorHypermediaOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:actuator";

        public HypermediaEndpointOptions()
            : base()
        {
            Id = string.Empty;
        }

        public HypermediaEndpointOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
        }
    }
}