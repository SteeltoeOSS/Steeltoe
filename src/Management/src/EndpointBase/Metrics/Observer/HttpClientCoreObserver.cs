//// Copyright 2017 the original author or authors.
////
//// Licensed under the Apache License, Version 2.0 (the "License");
//// you may not use this file except in compliance with the License.
//// You may obtain a copy of the License at
////
//// https://www.apache.org/licenses/LICENSE-2.0
////
//// Unless required by applicable law or agreed to in writing, software
//// distributed under the License is distributed on an "AS IS" BASIS,
//// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//// See the License for the specific language governing permissions and
//// limitations under the License.

//using Microsoft.Extensions.Logging;
//using Steeltoe.Common;
//using Steeltoe.Common.Diagnostics;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Net.Http;
//using System.Text.RegularExpressions;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Steeltoe.Management.Endpoint.Metrics.Observer
//{
//    public class HttpClientCoreObserver : MetricsObserver
//    {
//        internal const string DIAGNOSTIC_NAME = "HttpHandlerDiagnosticListener";
//        internal const string OBSERVER_NAME = "HttpClientCoreObserver";

//        internal const string STOP_EVENT = "System.Net.Http.HttpRequestOut.Stop";
//        internal const string EXCEPTION_EVENT = "System.Net.Http.Exception";

//        private readonly ITagKey statusTagKey = TagKey.Create("status");
//        private readonly ITagKey uriTagKey = TagKey.Create("uri");
//        private readonly ITagKey methodTagKey = TagKey.Create("method");
//        private readonly ITagKey clientTagKey = TagKey.Create("clientName");

//        private readonly IMeasureDouble clientTimeMeasure;
//        private readonly IMeasureLong clientCountMeasure;

//        public HttpClientCoreObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<HttpClientCoreObserver> logger)
//            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
//        {
//            PathMatcher = new Regex(options.EgressIgnorePattern);

//            clientTimeMeasure = MeasureDouble.Create("client.core.totalTime", "Total request time", MeasureUnit.MilliSeconds);
//            clientCountMeasure = MeasureLong.Create("client.core.totalRequests", "Total request count", "count");

//            var view = View.Create(
//                    ViewName.Create("http.client.request.time"),
//                    "Total request time",
//                    clientTimeMeasure,
//                    Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
//                    new List<ITagKey>() { statusTagKey, uriTagKey, methodTagKey, clientTagKey });
//            ViewManager.RegisterView(view);

//            view = View.Create(
//                ViewName.Create("http.client.request.count"),
//                "Total request counts",
//                clientCountMeasure,
//                Sum.Create(),
//                new List<ITagKey>() { statusTagKey, uriTagKey, methodTagKey, clientTagKey });

//            ViewManager.RegisterView(view);
//        }

//        public override void ProcessEvent(string evnt, object arg)
//        {
//            if (arg == null)
//            {
//                return;
//            }

//            Activity current = Activity.Current;
//            if (current == null)
//            {
//                return;
//            }

//            var request = DiagnosticHelpers.GetProperty<HttpRequestMessage>(arg, "Request");
//            if (request == null)
//            {
//                return;
//            }

//            if (evnt == STOP_EVENT)
//            {
//                Logger?.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

//                var response = DiagnosticHelpers.GetProperty<HttpResponseMessage>(arg, "Response");
//                var requestStatus = DiagnosticHelpers.GetProperty<TaskStatus>(arg, "RequestTaskStatus");
//                HandleStopEvent(current, request, response, requestStatus);

//                Logger?.LogTrace("HandleStopEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
//            }
//            else if (evnt == EXCEPTION_EVENT)
//            {
//                Logger?.LogTrace("HandleExceptionEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

//                HandleExceptionEvent(current, request);

//                Logger?.LogTrace("HandleExceptionEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
//            }
//        }

//        protected internal void HandleExceptionEvent(Activity current, HttpRequestMessage request)
//        {
//            HandleStopEvent(current, request, null, TaskStatus.Faulted);
//        }

//        protected internal void HandleStopEvent(Activity current, HttpRequestMessage request, HttpResponseMessage response, TaskStatus taskStatus)
//        {
//            if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
//            {
//                Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", SecurityUtilities.SanitizeInput(request.RequestUri.AbsolutePath));
//                return;
//            }

//            if (current.Duration.TotalMilliseconds > 0)
//            {
//                ITagContext tagContext = GetTagContext(request, response, taskStatus);
//                StatsRecorder
//                    .NewMeasureMap()
//                    .Put(clientTimeMeasure, current.Duration.TotalMilliseconds)
//                    .Put(clientCountMeasure, 1)
//                    .Record(tagContext);
//            }
//        }

//        protected internal ITagContext GetTagContext(HttpRequestMessage request, HttpResponseMessage response, TaskStatus taskStatus)
//        {
//            var uri = request.RequestUri.ToString();
//            var statusCode = GetStatusCode(response, taskStatus);

//            return Tagger
//                .EmptyBuilder
//                .Put(uriTagKey, TagValue.Create(uri))
//                .Put(statusTagKey, TagValue.Create(statusCode))
//                .Put(clientTagKey, TagValue.Create(request.RequestUri.Host))
//                .Put(methodTagKey, TagValue.Create(request.Method.ToString()))
//                .Build();
//        }

//        protected internal string GetStatusCode(HttpResponseMessage response, TaskStatus taskStatus)
//        {
//            if (response != null)
//            {
//                int val = (int)response.StatusCode;
//                return val.ToString();
//            }

//            if (taskStatus == TaskStatus.Faulted)
//            {
//                return "CLIENT_FAULT";
//            }

//            if (taskStatus == TaskStatus.Canceled)
//            {
//                return "CLIENT_CANCELED";
//            }

//            return "CLIENT_ERROR";
//        }
//    }
//}
