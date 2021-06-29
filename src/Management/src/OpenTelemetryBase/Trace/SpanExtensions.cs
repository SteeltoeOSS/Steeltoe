// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Steeltoe.Management.OpenTelemetry.Trace
{
    public static class SpanExtensions
    {
        public static TelemetrySpan PutHttpResponseSizeAttribute(this TelemetrySpan span, long size)
        {
            span.SetAttribute(SpanAttributeConstants.HttpResponseSizeKey, size);
            return span;
        }

        public static TelemetrySpan PutHttpRequestSizeAttribute(this TelemetrySpan span, long size)
        {
            span.SetAttribute(SpanAttributeConstants.HttpRequestSizeKey, size);
            return span;
        }

        public static TelemetrySpan PutErrorAttribute(this TelemetrySpan span, string errorMessage)
        {
            span.SetAttribute(SpanAttributeConstants.ErrorKey, errorMessage);
            return span;
        }

        public static TelemetrySpan PutErrorStackTraceAttribute(this TelemetrySpan span, string errorStackTrace)
        {
            span.SetAttribute(SpanAttributeConstants.ErrorStackTrace, errorStackTrace);
            return span;
        }

        public static TelemetrySpan PutMvcControllerClass(this TelemetrySpan span, string className)
        {
            span.SetAttribute(SpanAttributeConstants.MvcControllerClass, className);
            return span;
        }

        public static TelemetrySpan PutMvcControllerAction(this TelemetrySpan span, string actionName)
        {
            span.SetAttribute(SpanAttributeConstants.MvcControllerMethod, actionName);
            return span;
        }

        public static TelemetrySpan PutMvcViewExecutingFilePath(this TelemetrySpan span, string actionName)
        {
            span.SetAttribute(SpanAttributeConstants.MvcViewFilePath, actionName);
            return span;
        }

        public static TelemetrySpan PutHttpRequestHeadersAttribute(this TelemetrySpan span, List<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            PutHeadersAttribute(span, "http.request.", headers);
            return span;
        }

        public static TelemetrySpan PutHttpRequestHeadersAttribute(this TelemetrySpan span, NameValueCollection headers)
        {
            PutHeadersAttribute(span, "http.request.", headers);
            return span;
        }

        public static TelemetrySpan PutHttpResponseHeadersAttribute(this TelemetrySpan span, List<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            PutHeadersAttribute(span, "http.response.", headers);
            return span;
        }

        public static TelemetrySpan PutHttpResponseHeadersAttribute(this TelemetrySpan span, NameValueCollection headers)
        {
            PutHeadersAttribute(span, "http.response.", headers);
            return span;
        }

        private static void PutHeadersAttribute(TelemetrySpan span, string key, List<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var header in headers)
            {
                span.SetAttribute(key + header.Key, string.Join(",", header.Value));
            }
        }

        private static void PutHeadersAttribute(TelemetrySpan span, string key, NameValueCollection headers)
        {
            foreach (var header in headers.AllKeys)
            {
                var val = string.Join(",", headers.GetValues(header));
                span.SetAttribute(key + header, val);
            }
        }
    }
}
