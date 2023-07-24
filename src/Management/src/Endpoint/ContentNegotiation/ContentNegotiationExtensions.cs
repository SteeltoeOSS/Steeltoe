// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ContentNegotiation;

internal static class ContentNegotiationExtensions
{
    internal static void HandleContentNegotiation(this HttpContext context, ILogger logger)
    {
        context.Response.Headers.SetContentType(context.Request.Headers, logger);
    }

    private static void LogContentType(this ILogger logger, IHeaderDictionary requestHeaders, string contentType)
    {
        ArgumentGuard.NotNull(logger);

        logger.LogTrace("setting contentType to {type}", contentType);
        bool? logTrace = logger.IsEnabled(LogLevel.Trace);

        if (logTrace.GetValueOrDefault())
        {
            foreach (KeyValuePair<string, StringValues> header in requestHeaders)
            {
                logger.LogTrace("Header: {key} - {value}", header.Key, header.Value);
            }
        }
    }

    private static void SetContentType(this IHeaderDictionary responseHeaders, IHeaderDictionary requestHeaders, ILogger logger,
        MediaTypeVersion version = MediaTypeVersion.V2)
    {
        var headers = new RequestHeaders(requestHeaders);
        List<string> acceptMediaTypes = headers.Accept.Select(x => x.MediaType.Value).ToList();

        string contentType = ActuatorMediaTypes.GetContentHeaders(acceptMediaTypes, version);

        responseHeaders.Add("Content-Type", contentType);

        logger.LogContentType(requestHeaders, contentType);
    }
}
