// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.Endpoint.Trace;
using System;
using System.Collections.Generic;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class TraceHandler : ActuatorHandler<TraceEndpoint, List<TraceResult>>
    {
        public TraceHandler(TraceEndpoint endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<TraceHandler> logger = null)
          : base(endpoint, securityServices, mgmtOptions, null, true, logger)
        {
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public TraceHandler(TraceEndpoint endpoint, IEnumerable<ISecurityService> securityServices, ILogger<TraceHandler> logger = null)
            : base(endpoint, securityServices, null, true, logger)
        {
        }

        public override void HandleRequest(HttpContextBase context)
        {
            _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(TraceEndpoint));
            var result = _endpoint.Invoke();
            context.Response.Headers.Set("Content-Type", "application/vnd.spring-boot.actuator.v1+json");
            context.Response.Write(Serialize(result));
        }
    }
}
