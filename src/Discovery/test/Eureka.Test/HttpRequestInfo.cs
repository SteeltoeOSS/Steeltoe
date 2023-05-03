// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class HttpRequestInfo
{
    public string Method { get; }

    public HostString Host { get; }

    public PathString Path { get; }

    public QueryString QueryString { get; }

    public Stream Body { get; } = new MemoryStream();

    private HttpRequestInfo(HttpRequest request)
    {
        Method = request.Method;
        Host = request.Host;
        Path = request.Path;
        QueryString = request.QueryString;
    }

    public static async Task<HttpRequestInfo> CreateAsync(HttpRequest request)
    {
        var info = new HttpRequestInfo(request);

        await request.Body.CopyToAsync(info.Body);
        info.Body.Seek(0, SeekOrigin.Begin);

        return info;
    }
}
