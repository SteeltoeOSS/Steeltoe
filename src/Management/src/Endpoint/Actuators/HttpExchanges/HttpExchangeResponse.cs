// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Primitives;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

public sealed class HttpExchangeResponse
{
    public int Status { get; }
    public IDictionary<string, StringValues> Headers { get; }

    public HttpExchangeResponse(int status, IDictionary<string, StringValues> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        Status = status;
        Headers = headers;
    }
}
