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

using System;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public static class SpanAttributeConstants
    {
        public const string SpanKindKey = "span.kind";

        public const string ServerSpanKind = "server";
        public const string ClientSpanKind = "client";
        public const string ProducerSpanKind = "producer";
        public const string ConsumerSpanKind = "consumer";

        public const string HttpUrlKey = "http.url";
        public const string HttpMethodKey = "http.method";
        public const string HttpStatusCodeKey = "http.status_code";
        public const string HttpPathKey = "http.path";
        public const string HttpHostKey = "http.host";
        public const string HttpRequestSizeKey = "http.request.size";
        public const string HttpResponseSizeKey = "http.response.size";
        public const string HttpRouteKey = "http.route";

        public const string MvcControllerMethod = "mvc.controller.method";
        public const string MvcControllerClass = "mvc.controller.class";

        public const string MvcViewFilePath = "mvc.view.FilePath";

        public const string ErrorKey = "error";
        public const string ErrorStackTrace = "error.stack.trace";
    }
}
