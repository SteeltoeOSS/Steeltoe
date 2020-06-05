// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using System.Collections.Generic;
using System.Linq;

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
            var headers = new RequestHeaders(requestHeaders);
            var acceptMediaTypes = headers.Accept?.Select(x => x.MediaType.Value).ToList();

            var contentType = ActuatorMediaTypes.GetContentHeaders(acceptMediaTypes, version);

            responseHeaders.Add("Content-Type", contentType);

            logger?.LogContentType(requestHeaders, contentType);
        }
    }
}
