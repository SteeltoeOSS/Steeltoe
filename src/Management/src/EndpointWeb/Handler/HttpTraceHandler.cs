// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.Endpoint.Trace;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class HttpTraceHandler : ActuatorHandler<HttpTraceEndpoint, HttpTraceResult>
    {
        public HttpTraceHandler(HttpTraceEndpoint endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<HttpTraceHandler> logger = null)
          : base(endpoint, securityServices, mgmtOptions, null, true, logger)
        {
        }
    }
}
