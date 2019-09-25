// Copyright 2017 the original author or authors.
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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.EndpointBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointCore.ContentNegotiation
{
    public static class ContentNegotiationExtensions
    {
        public static async Task HandleContentNegotiation(this HttpContext context, ILogger logger, Action<HttpContext> onSuccess)
        {
            if (context.Request.Headers.HasSupportedAcceptHeaders())
            {
                context.Response.Headers.SetContentType(context.Request.Headers);
                onSuccess(context);
            }
            else
            {
                string errorMessage = "Unsupported accept headers received";
                context.LogError(logger, errorMessage);
                context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");
                context.Response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                await context.Response.WriteAsync(errorMessage).ConfigureAwait(false);
            }
        }

        public static void LogError(this HttpContext context, ILogger logger, string error)
        {
            logger?.LogError("Unsupported headers error {0}", error);
            var logTrace = logger?.IsEnabled(LogLevel.Trace);

            if (logTrace.GetValueOrDefault())
            {
                foreach (var header in context.Request.Headers)
                {
                    logger.LogTrace("Header: {0} - {1}", header.Key, header.Value);
                }
            }
        }

        public static void SetContentType(this IHeaderDictionary responseHeaders, IHeaderDictionary requestHeaders, MediaTypeVersion version = MediaTypeVersion.V2)
        {
            var contentType = ActuatorMediaTypes.GetContentHeaders(requestHeaders["Accept"].ToList(), version);

            responseHeaders.Add("Content-Type", contentType);
        }

        public static bool HasSupportedAcceptHeaders(this IHeaderDictionary requestHeaders, MediaTypeVersion version = MediaTypeVersion.V2)
        {
            var acceptHeaders = requestHeaders["Accept"].ToList();
            return acceptHeaders.Count == 0
                || ActuatorMediaTypes.AllowedAcceptHeaders(version).Intersect(acceptHeaders).Any();
        }
    }
}
