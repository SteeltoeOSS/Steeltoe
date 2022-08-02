// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class HttpRequestInfo
{
    public string Method { get; set; }

    public HostString Host { get; set; }

    public PathString Path { get; set; }

    public QueryString QueryString { get; set; }

    public Stream Body { get; set; } = new MemoryStream();

    public HttpRequestInfo(HttpRequest request)
    {
        Method = request.Method;
        Host = request.Host;
        Path = request.Path;
        QueryString = request.QueryString;
        request.Body.CopyToAsync(Body).GetAwaiter().GetResult();
        Body.Seek(0, SeekOrigin.Begin);
    }
}
