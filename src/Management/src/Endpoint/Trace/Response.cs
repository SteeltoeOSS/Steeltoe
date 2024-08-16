// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Trace;

public sealed class Response
{
    public int Status { get; }
    public IDictionary<string, IList<string?>> Headers { get; }

    public Response(int status, IDictionary<string, IList<string?>> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        Status = status;
        Headers = headers;
    }
}
