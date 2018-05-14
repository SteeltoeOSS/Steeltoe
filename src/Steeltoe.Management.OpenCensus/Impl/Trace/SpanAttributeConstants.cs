using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
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
