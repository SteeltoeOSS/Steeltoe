// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Trace;

public sealed class Request
{
    public string Method { get; }
    public string Uri { get; }
    public IDictionary<string, IList<string?>> Headers { get; }
    public string? RemoteAddress { get; }

    public Request(string method, string uri, IDictionary<string, IList<string?>> headers, string? remoteAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentNullException.ThrowIfNull(headers);

        Method = method;
        Uri = uri;
        Headers = headers;
        RemoteAddress = remoteAddress;
    }
}
