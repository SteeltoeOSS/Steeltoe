// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Census.Trace
{
    internal static class SpanAttributeConstants
    {
        public const string SpanKindKey = "span.kind";

        public const string ServerSpanKind = "Server";
        public const string ClientSpanKind = "Client";
        public const string ProducerSpanKind = "producer";
        public const string ConsumerSpanKind = "consumer";

        public const string MvcControllerMethod = "mvc.controller.method";
        public const string MvcControllerClass = "mvc.controller.class";

        public const string MvcViewFilePath = "mvc.view.FilePath";

        public const string ErrorKey = "error";
        public const string ErrorStackTrace = "error.stack.trace";

        // Note: These have to continue to match the OpenCensus versions (used for testing)
        public const string HttpUrlKey = "http.url";
        public const string HttpMethodKey = "http.method";
        public const string HttpStatusCodeKey = "http.status_code";
        public const string HttpPathKey = "http.path";
        public const string HttpHostKey = "http.host";
        public const string HttpRequestSizeKey = "http.request.size";
        public const string HttpResponseSizeKey = "http.response.size";
        public const string HttpRouteKey = "http.route";
    }
}
