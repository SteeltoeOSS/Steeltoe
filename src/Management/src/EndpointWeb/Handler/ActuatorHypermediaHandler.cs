// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class ActuatorHypermediaHandler : ActuatorHandler<ActuatorEndpoint, Links, string>
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public ActuatorHypermediaHandler(ActuatorEndpoint endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ActuatorHypermediaHandler> logger = null)
           : base(endpoint, securityServices, mgmtOptions?.OfType<ActuatorManagementOptions>(), null, true, logger)
        {
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public ActuatorHypermediaHandler(ActuatorEndpoint endpoint, IEnumerable<ISecurityService> securityServices, ILogger<ActuatorHypermediaHandler> logger = null)
          : base(endpoint, securityServices, null, true, logger)
        {
        }

        public override void HandleRequest(HttpContextBase context)
        {
            _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(CloudFoundryEndpoint));
            if (context.Request.HttpMethod == "GET")
            {
                var result = _endpoint.Invoke(GetRequestUri(context.Request));
                context.Response.Headers.Set("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
                context.Response.Write(Serialize(result));
            }
        }
    }
}
