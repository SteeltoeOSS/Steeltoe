// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Trace
{
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

#pragma warning disable SA1402 // File may only contain a single class
    public class HttpTrace
#pragma warning restore SA1402 // File may only contain a single class
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

#pragma warning disable SA1402 // File may only contain a single class
    public class Request
#pragma warning restore SA1402 // File may only contain a single class
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

#pragma warning disable SA1402 // File may only contain a single class
    public class Response
#pragma warning restore SA1402 // File may only contain a single class
    {
        public int Status { get; }

        public IDictionary<string, string[]> Headers { get; }

        public Response(int status, IDictionary<string, string[]> headers)
        {
            Status = status;
            Headers = headers;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class Session
#pragma warning restore SA1402 // File may only contain a single class
    {
        public string Id { get; }

        public Session(string id)
        {
            Id = id;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class Principal
#pragma warning restore SA1402 // File may only contain a single class
    {
        public string Name { get; }

        public Principal(string name)
        {
            Name = name;
        }
    }
}
