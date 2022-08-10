// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Trace;

public class HttpTrace
{
    public long Timestamp { get; }

    public Principal Principal { get; }

    public Session Session { get; }

    public Request Request { get; }

    public Response Response { get; }

    public long TimeTaken { get; }

    public HttpTrace(Request request, Response response, long timestamp, Principal principal, Session session, double timeTaken)
    {
        Request = request;
        Response = response;
        Timestamp = timestamp;
        Principal = principal;
        Session = session;
        TimeTaken = (long)timeTaken;
    }
}
