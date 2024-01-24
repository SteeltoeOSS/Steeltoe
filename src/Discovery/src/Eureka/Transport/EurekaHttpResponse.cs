// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net;
using System.Net.Http.Headers;
using Steeltoe.Common;

namespace Steeltoe.Discovery.Eureka.Transport;

public class EurekaHttpResponse
{
    public HttpStatusCode StatusCode { get; }
    public HttpResponseHeaders Headers { get; }

    public EurekaHttpResponse(HttpStatusCode statusCode, HttpResponseHeaders headers)
    {
        ArgumentGuard.NotNull(headers);

        StatusCode = statusCode;
        Headers = headers;
    }
}

public sealed class EurekaHttpResponse<T> : EurekaHttpResponse
{
    public T Response { get; }

    public EurekaHttpResponse(HttpStatusCode statusCode, HttpResponseHeaders headers, T response)
        : base(statusCode, headers)
    {
        Response = response;
    }
}
