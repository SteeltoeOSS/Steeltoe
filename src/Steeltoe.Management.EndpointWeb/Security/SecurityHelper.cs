// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Security
{
    public class SecurityHelper : SecurityBase
    {
        public ILogger _logger;

        public SecurityHelper(ICloudFoundryOptions options, ILogger logger = null)
            : base(options, logger)
        {
            _logger = logger;
        }

        public async Task ReturnError(HttpContextBase context, SecurityResult error)
        {
            LogError(context, error);
            context.Response.Headers.Set("Content-Type", "application/json;charset=UTF-8");
            context.Response.StatusCode = (int)error.Code;
            await context.Response.Output.WriteAsync(Serialize(error));
        }

        public void LogError(HttpContextBase context, SecurityResult error)
        {
            _logger?.LogError("Actuator Security Error: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            if (_logger?.IsEnabled(LogLevel.Trace) == true)
            {
                foreach (var header in context.Request.Headers.AllKeys)
                {
                    _logger?.LogTrace("Header: {HeaderKey} - {HeaderValue}", header, context.Request.Headers[header]);
                }
            }
        }
    }
}
