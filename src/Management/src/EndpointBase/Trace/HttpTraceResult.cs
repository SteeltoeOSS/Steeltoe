// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Trace
{
    public class HttpTraceResult
    {
        private readonly List<TraceResult> list;

        [JsonProperty("traces")]
        public List<HttpTrace> Traces { get; }

        public HttpTraceResult(List<HttpTrace> traces)
        {
            Traces = traces;
        }

        public HttpTraceResult(List<TraceResult> list)
        {
            this.list = list;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class HttpTrace
#pragma warning restore SA1402 // File may only contain a single class
    {
        public long Timestamp { get; }

        public Principal Principal { get;  }

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
