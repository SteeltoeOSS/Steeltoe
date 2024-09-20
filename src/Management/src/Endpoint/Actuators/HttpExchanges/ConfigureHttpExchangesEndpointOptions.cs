// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

internal sealed class ConfigureHttpExchangesEndpointOptions(IConfiguration configuration)
    : ConfigureEndpointOptions<HttpExchangesEndpointOptions>(configuration, ManagementInfoPrefix, "httpexchanges")
{
    private const string ManagementInfoPrefix = "management:endpoints:httpexchanges";
    private const int DefaultCapacity = 100;

    private static readonly string[] DefaultAllowedRequestHeaders =
    [
        HeaderNames.Accept,
        HeaderNames.AcceptCharset,
        HeaderNames.AcceptEncoding,
        HeaderNames.AcceptLanguage,
        HeaderNames.Allow,
        HeaderNames.CacheControl,
        HeaderNames.Connection,
        HeaderNames.ContentEncoding,
        HeaderNames.ContentLength,
        HeaderNames.ContentType,
        HeaderNames.Date,
        HeaderNames.DNT,
        HeaderNames.Expect,
        HeaderNames.Host,
        HeaderNames.MaxForwards,
        HeaderNames.Range,
        HeaderNames.SecWebSocketExtensions,
        HeaderNames.SecWebSocketVersion,
        HeaderNames.TE,
        HeaderNames.Trailer,
        HeaderNames.TransferEncoding,
        HeaderNames.Upgrade,
        HeaderNames.UserAgent,
        HeaderNames.Warning,
        HeaderNames.XRequestedWith,
        HeaderNames.XUACompatible
    ];

    private static readonly string[] DefaultAllowedResponseHeaders =
    [
        HeaderNames.AcceptRanges,
        HeaderNames.Age,
        HeaderNames.Allow,
        HeaderNames.AltSvc,
        HeaderNames.Connection,
        HeaderNames.ContentDisposition,
        HeaderNames.ContentLanguage,
        HeaderNames.ContentLength,
        HeaderNames.ContentLocation,
        HeaderNames.ContentRange,
        HeaderNames.ContentType,
        HeaderNames.Date,
        HeaderNames.Expires,
        HeaderNames.LastModified,
        HeaderNames.Location,
        HeaderNames.Server,
        HeaderNames.TransferEncoding,
        HeaderNames.Upgrade,
        HeaderNames.XPoweredBy
    ];

    public override void Configure(HttpExchangesEndpointOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        base.Configure(options);

        if (options.Capacity < 1)
        {
            options.Capacity = DefaultCapacity;
        }

        foreach (string defaultKey in DefaultAllowedRequestHeaders)
        {
            options.RequestHeaders.Add(defaultKey);
        }

        foreach (string defaultKey in DefaultAllowedResponseHeaders)
        {
            options.ResponseHeaders.Add(defaultKey);
        }
    }
}
