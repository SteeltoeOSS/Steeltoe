// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

public sealed class HttpExchangesEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets a value indicating how many HTTP exchanges should be stored. Default value: 100.
    /// </summary>
    public int Capacity { get; set; } = -1;

    /// <summary>
    /// Gets or sets a value indicating whether HTTP headers from the request should be included in traces. Default value: true.
    /// <para>
    /// If a request header is not present in the <see cref="RequestHeaders" />, the header name will be logged with a redacted value. Request headers can
    /// contain authentication tokens, or private information which may have regulatory concerns under GDPR and other laws. Arbitrary request headers should
    /// not be logged unless logs are secure and access controlled and the privacy impact assessed.
    /// </para>
    /// </summary>
    public bool IncludeRequestHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether HTTP headers from the response should be included in traces. Default value: true.
    /// <para>
    /// If a response header is not present in the <see cref="ResponseHeaders" />, the header name will be logged with a redacted value.
    /// </para>
    /// </summary>
    public bool IncludeResponseHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the request path should be included in traces. Default value: true.
    /// </summary>
    public bool IncludePathInfo { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the request querystring should be included in traces. Default value: true.
    /// </summary>
    public bool IncludeQueryString { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the name of the user principal making the request should be included in traces. Default value: false.
    /// </summary>
    public bool IncludeUserPrincipal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the IP address of the request's sender should be included in traces. Default value: false.
    /// </summary>
    public bool IncludeRemoteAddress { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user's session ID should be included in traces. Default value: false.
    /// </summary>
    public bool IncludeSessionId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the time taken to process the request should be included in traces. Default value: true.
    /// </summary>
    public bool IncludeTimeTaken { get; set; } = true;

    /// <summary>
    /// Gets request header values that are allowed to be logged.
    /// <para>
    /// If a request header is not present in the <see cref="RequestHeaders" />, the header name will be logged with a redacted value. Request headers can
    /// contain authentication tokens, or private information which may have regulatory concerns under GDPR and other laws. Arbitrary request headers should
    /// not be logged unless logs are secure and access controlled and the privacy impact assessed.
    /// </para>
    /// </summary>
    public HashSet<string> RequestHeaders { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets response header values that are allowed to be logged.
    /// <para>
    /// If a response header is not present in the <see cref="ResponseHeaders" />, the header name will be logged with a redacted value.
    /// </para>
    /// </summary>
    public HashSet<string> ResponseHeaders { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether to return HTTP exchanges in reverse order (newest exchanges first). Default value: true.
    /// </summary>
    public bool Reverse { get; set; } = true;
}
