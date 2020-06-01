// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class InfoHandler : ActuatorHandler<InfoEndpoint, Dictionary<string, object>>
    {
        public InfoHandler(InfoEndpoint endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<InfoHandler> logger = null)
           : base(endpoint, securityServices, mgmtOptions, null, true, logger)
        {
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public InfoHandler(InfoEndpoint endpoint, IEnumerable<ISecurityService> securityServices, ILogger<InfoHandler> logger = null)
            : base(endpoint, securityServices, null, true, logger)
        {
        }
    }
}
