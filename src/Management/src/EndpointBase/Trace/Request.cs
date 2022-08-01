// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Trace;

public class Request
{
    public string Method { get; }

    public string Uri { get; }

    public IDictionary<string, string[]> Headers { get; }

    public string RemoteAddress { get; }

    public Request(string method, string uri, IDictionary<string, string[]> headers, string remoteAddress)
    {
        Method = method;
        Uri = uri;
        Headers = headers;
        RemoteAddress = remoteAddress;
    }
}
