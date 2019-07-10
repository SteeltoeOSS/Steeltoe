﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

        protected internal string GetRequestUri(HttpRequestBase request)
        {
            string scheme = request.IsSecureConnection ? "https" : "http";
            string headerScheme = request.Headers.Get("X-Forwarded-Proto");

            if (headerScheme != null)
            {
                scheme = headerScheme;
            }

            return scheme + "://" + request.Url.Host + request.Path.ToString();
        }
    }
}
