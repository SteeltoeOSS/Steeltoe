//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Steeltoe.Management.Endpoint.Trace
{

    class TraceObserver : IObserver<KeyValuePair<string, object>>, ITraceRepository
    {
        private ILogger<TraceObserver> _logger;
        private ITraceOptions _options;
        private DiagnosticListener _listener;
        private ConcurrentDictionary<string, PendingTrace> _pending = new ConcurrentDictionary<string, PendingTrace>();
        private ConcurrentQueue<Trace> _queue = new ConcurrentQueue<Trace>();
        private long ticksPerMilli = Stopwatch.Frequency/1000;

        public TraceObserver(DiagnosticListener listener, ITraceOptions options, ILogger<TraceObserver> logger)
        {
            _logger = logger;
            _listener = listener;
            _options = options;
            _listener.Subscribe(this);
        }

        public List<Trace> GetTraces()
        {
            Trace[] traces = _queue.ToArray();
            return new List<Trace>(traces);
        }

        public void OnCompleted()
        {
            _queue = null;
            _pending = null;
            _logger.LogInformation("TraceObserver Shutdown");
        }

        public void OnError(Exception error)
        {
            _logger.LogError("TraceObserver Exception: {0}", error);
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (_queue == null)
            {
                return;
            }
            if (value.Key.Equals("Microsoft.AspNetCore.Hosting.BeginRequest"))
            {
  
                HttpContext context = GetProperty<HttpContext>(value.Value, "httpContext");
                long timeStamp = GetProperty<long>(value.Value, "timestamp");
                if (context != null && timeStamp != 0)
                {
                    if (!_pending.TryAdd(context.TraceIdentifier, new PendingTrace(timeStamp)))
                    {
                        _logger.LogDebug("BeginRequest - Dropped trace");
                    }
                }

            } else if (value.Key.Equals("Microsoft.AspNetCore.Hosting.EndRequest"))
            {
     
                HttpContext context = GetProperty<HttpContext>(value.Value, "httpContext");
                long timeStamp = GetProperty<long>(value.Value, "timestamp");
                if (context != null && timeStamp != 0)
                {
                    PendingTrace pendingTrace;
                    if (_pending.TryRemove(context.TraceIdentifier, out pendingTrace))
                    {
                        Trace trace = MakeTrace(context, pendingTrace.StartTime, timeStamp);
                        _queue.Enqueue(trace);
                        if (_queue.Count >= _options.Capacity)
                        {
                            Trace discard;
                            if (!_queue.TryDequeue(out discard))
                            {
                                _logger.LogDebug("EndRequest - Unable to remove trace");
                                _queue = new ConcurrentQueue<Trace>();
                            }
                        }

                    } else
                    {
                        _logger.LogDebug("EndRequest - Dropped trace");
                    }
                }
            }
        }

        private Trace MakeTrace(HttpContext context, long startTime, long endTime)
        {
            var request = context.Request;
            var response = context.Response;
            var principal = context?.User?.Identity?.Name;

            Dictionary<string, object> details = new Dictionary<string, object>();
            details.Add("method", request.Method);
            details.Add("path", request.Path.Value);

            
            Dictionary<string, object> headers = new Dictionary<string, object>();
            details.Add("headers", headers);

            if (_options.AddRequestHeaders)
            {
                headers.Add("request", GetHeaders(request.Headers));
           
            }

            if (_options.AddResponseHeaders)
            {
                headers.Add("response", GetHeaders(response.StatusCode, response.Headers));
            }

            if (_options.AddPathInfo)
            {
                details.Add("pathInfo", request.Path.Value);
            }

            if (_options.AddUserPrincipal)
            {
                details.Add("userPrincipal", principal);
            }
            if (_options.AddParameters)
            {
                details.Add("parameters", GetRequestParameters(request));
            }
            if (_options.AddQueryString)
            {
                details.Add("query", request.QueryString.Value);
            }
            if (_options.AddAuthType)
            {
                details.Add("authType", GetAuthType(request)); // TODO
            }
            if (_options.AddRemoteAddress)
            {
                details.Add("remoteAddress", context.Connection.RemoteIpAddress.ToString());
            }
            if (_options.AddSessionId)
            {
                details.Add("sessionId", GetSessionId(context));
            }

            if (_options.AddTimeTaken)
            {
                details.Add("timeTaken", GetMilliSeconds(endTime - startTime));
            }

            return new Trace(GetJavaTime(DateTime.Now.Ticks), details);

        }
        private static DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private long GetJavaTime(long ticks)
        {
            long javaTicks = ticks - baseTime.Ticks;
            return javaTicks / 10000;
        }
        private string GetSessionId(HttpContext context)
        {
            var sessionFeature = context.Features.Get<ISessionFeature>();
            return sessionFeature == null ? null : context.Session.Id;
        }
        private string GetMilliSeconds(long v)
        {
            long timeInMilli = v / ticksPerMilli;
            return timeInMilli.ToString();
        }
        private string GetAuthType(HttpRequest request)
        {
            return string.Empty;
        }
        private Dictionary<string, string[]> GetRequestParameters(HttpRequest request)
        {
            Dictionary<string, string[]> parameters = new Dictionary<string, string[]>();
            var query = request.Query;
            foreach(var p in query)
            {
                parameters.Add(p.Key, p.Value.ToArray());
            }
  
            if (request.HasFormContentType && request.Form != null)
            {
                var formData = request.Form;
                foreach(var p in formData)
                {
                    parameters.Add(p.Key, p.Value.ToArray());
                }
            }

            return parameters;
        }
        private string GetRequestUri(HttpRequest request)
        {
            return request.Scheme + "://" + request.Host.Value + request.Path.Value;
        }
        private Dictionary<string,object> GetHeaders(int status, IHeaderDictionary headers)
        {
            var result = GetHeaders(headers);
            result.Add("status", status.ToString());
            return result;
        }
        private Dictionary<string, object> GetHeaders(IHeaderDictionary headers)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach(var h in headers)
            {
                // Add filtering
                result.Add(h.Key.ToLowerInvariant(), GetHeaderValue(h.Value));
            }
            return result;
        }
        private object GetHeaderValue(StringValues values)
        {
            List<string> result = new List<string>();
            foreach(var v in values)
            {
                result.Add(v);
            }
            if (result.Count == 1)
            {
                return result[0];
            }
            if (result.Count == 0)
            {
                return string.Empty;
            }
            return result;
        }
        private T GetProperty<T>(object o, string name)
        {
            var property = o.GetType().GetTypeInfo().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                return default(T);
            }
            return (T)property.GetValue(o);
        }
        class PendingTrace
        {
            public PendingTrace(long startTime)
            {
                StartTime = startTime;
            }
            public long StartTime { get; }
        }
    }
}
