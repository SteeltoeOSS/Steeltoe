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
    public static void HandleContentNegotiation(this HttpContext context, ILogger logger)
    {
        ArgumentGuard.NotNull(context);
        ArgumentGuard.NotNull(logger);

        SetContentType(context.Request.Headers, context.Response.Headers, logger);
    }

    private static void SetContentType(IHeaderDictionary requestHeaders, IHeaderDictionary responseHeaders, ILogger logger,
        MediaTypeVersion version = MediaTypeVersion.V2)
    {
        var headers = new RequestHeaders(requestHeaders);
        List<string> acceptMediaTypes = headers.Accept.Select(header => header.MediaType.Value!).ToList();

        string contentType = ActuatorMediaTypes.GetContentHeaders(acceptMediaTypes, version);
        responseHeaders.Append("Content-Type", contentType);

        LogResponseContentTypeWithRequestHeaders(contentType, requestHeaders, logger);
    }

    private static void LogResponseContentTypeWithRequestHeaders(string contentType, IHeaderDictionary requestHeaders, ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Setting Content-Type to {ContentType}", contentType);

            foreach (KeyValuePair<string, StringValues> header in requestHeaders)
            {
                logger.LogTrace("Request header: {Key} = {Value}", header.Key, header.Value);
            }
        }
    }
}
