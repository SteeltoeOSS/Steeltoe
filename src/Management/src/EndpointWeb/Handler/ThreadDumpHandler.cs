// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.Endpoint.ThreadDump;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class ThreadDumpHandler : ActuatorHandler<ThreadDumpEndpoint, List<ThreadInfo>>
    {
        public ThreadDumpHandler(ThreadDumpEndpoint endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ThreadDumpHandler> logger = null)
          : base(endpoint, securityServices, mgmtOptions, null, true, logger)
        {
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public ThreadDumpHandler(ThreadDumpEndpoint endpoint, IEnumerable<ISecurityService> securityServices, ILogger<ThreadDumpHandler> logger = null)
            : base(endpoint, securityServices, null, true, logger)
        {
        }
    }
}
