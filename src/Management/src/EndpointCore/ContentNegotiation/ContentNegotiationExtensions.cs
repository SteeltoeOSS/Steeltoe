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
        public static void HandleContentNegotiation(this HttpContext context, ILogger logger)
        {
            context.Response.Headers.SetContentType(context.Request.Headers, logger);
        }

        public static void LogContentType(this ILogger logger, IHeaderDictionary requestHeaders,  string contentType)
        {
            logger?.LogTrace("setting contentType to {0}", contentType);
            var logTrace = logger?.IsEnabled(LogLevel.Trace);

            if (logTrace.GetValueOrDefault())
            {
                foreach (var header in requestHeaders)
                {
                    logger.LogTrace("Header: {0} - {1}", header.Key, header.Value);
                }
            }
        }

        public static void SetContentType(this IHeaderDictionary responseHeaders, IHeaderDictionary requestHeaders, ILogger logger, MediaTypeVersion version = MediaTypeVersion.V2)
        {
            var acceptMediaTypes = new List<string>();
            if (requestHeaders.TryGetValue("Accept", out var acceptHeader))
            {
                acceptMediaTypes = acceptHeader.ToString().Split(';').ToList();
            }

            var contentType = ActuatorMediaTypes.GetContentHeaders(acceptMediaTypes, version);

            responseHeaders.Add("Content-Type", contentType);

            logger?.LogContentType(requestHeaders, contentType);
        }

        public static bool HasSupportedAcceptHeaders(this IHeaderDictionary requestHeaders, MediaTypeVersion version = MediaTypeVersion.V2)
        {
            var acceptHeaders = requestHeaders["Accept"].ToList();
            return acceptHeaders.Count == 0
                || ActuatorMediaTypes.AllowedAcceptHeaders(version).Intersect(acceptHeaders).Any();
        }
    }
}
