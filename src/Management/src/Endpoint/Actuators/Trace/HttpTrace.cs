// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Trace;

public sealed class HttpTrace
{
    public long Timestamp { get; }
    public TracePrincipal? Principal { get; }
    public TraceSession? Session { get; }
    public TraceRequest Request { get; }
    public TraceResponse Response { get; }
    public long TimeTaken { get; }

    public HttpTrace(TraceRequest request, TraceResponse response, long timestamp, TracePrincipal? principal, TraceSession? session, double timeTaken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);

        Request = request;
        Response = response;
        Timestamp = timestamp;
        Principal = principal;
        Session = session;
        TimeTaken = (long)timeTaken;
    }
}
