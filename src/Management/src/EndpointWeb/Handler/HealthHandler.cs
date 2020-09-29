// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.EndpointWeb.Security;
using System;
using System.Collections.Generic;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class HealthHandler : ActuatorHandler<HealthEndpoint, HealthCheckResult, ISecurityContext>
    {
        public HealthHandler(IEndpoint<HealthCheckResult, ISecurityContext> endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<HealthHandler> logger = null)
           : base(endpoint, securityServices, mgmtOptions, null, true, logger)
        {
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public HealthHandler(IEndpoint<HealthCheckResult, ISecurityContext> endpoint, IEnumerable<ISecurityService> securityServices, ILogger<HealthHandler> logger = null)
            : base(endpoint, securityServices, mgmtOptions: null, allowedMethods: null, exactRequestPathMatching: true, logger: logger)
        {
        }

        public override void HandleRequest(HttpContextBase context)
        {
            _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(HealthHandler));

            var result = _endpoint.Invoke(new WebSecurityContext(context));
            context.Response.Headers.Set("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
            context.Response.Write(Serialize(result));
            var managementOptions = _mgmtOptions.OptionsForContext(context.Request.Path, _logger);
            if (managementOptions.UseStatusCodeFromResponse)
            {
                context.Response.StatusCode = ((HealthEndpoint)_endpoint).GetStatusCode(result);
            }
        }
    }
}
