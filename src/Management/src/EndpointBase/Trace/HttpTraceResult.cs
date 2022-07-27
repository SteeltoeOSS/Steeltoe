// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Trace;

public class HttpTraceResult
{
    private readonly List<TraceResult> _list;

    [JsonPropertyName("traces")]
    public List<HttpTrace> Traces { get; }

    public HttpTraceResult(List<HttpTrace> traces)
    {
        Traces = traces;
    }

    public HttpTraceResult(List<TraceResult> list)
    {
        _list = list;
    }
}

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

public class Response
{
    public int Status { get; }

    public IDictionary<string, string[]> Headers { get; }

    public Response(int status, IDictionary<string, string[]> headers)
    {
        Status = status;
        Headers = headers;
    }
}

public class Session
{
    public string Id { get; }

    public Session(string id)
    {
        Id = id;
    }
}

public class Principal
{
    public string Name { get; }

    public Principal(string name)
    {
        Name = name;
    }
}