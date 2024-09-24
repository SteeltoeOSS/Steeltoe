// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Json;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

public sealed class HttpExchangeRequest
{
    public string Method { get; }
    public Uri Uri { get; }

    [JsonIgnoreEmptyCollection]
    public IDictionary<string, StringValues> Headers { get; }

    public string? RemoteAddress { get; }

    public HttpExchangeRequest(string method, Uri uri, IDictionary<string, StringValues> headers, string? remoteAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(headers);

        Method = method;
        Uri = uri;
        Headers = headers;
        RemoteAddress = remoteAddress;
    }
}
