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

using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Steeltoe.Management.OpenTelemetry.Trace
{
    public static class SpanExtensions
    {
        public static TelemetrySpan PutClientSpanKindAttribute(this TelemetrySpan span)
        {
            span.SetAttribute(SpanAttributeConstants.SpanKindKey, SpanAttributeConstants.ClientSpanKind);
            return span;
        }

        public static TelemetrySpan PutServerSpanKindAttribute(this TelemetrySpan span)
        {
            span.SetAttribute(SpanAttributeConstants.SpanKindKey, SpanAttributeConstants.ServerSpanKind);
            return span;
        }

        ////public static ISpan PutHttpUrlAttribute(this ISpan span, string url)
        ////{
        ////    span.PutAttribute(SpanAttributeConstants.HttpUrlKey, AttributeValue.StringAttributeValue(url));
        ////    return span;
        ////}

        ////public static ISpan PutHttpMethodAttribute(this ISpan span, string method)
        ////{
        ////    span.PutAttribute(SpanAttributeConstants.HttpMethodKey, AttributeValue.StringAttributeValue(method));
        ////    return span;
        ////}

        ////public static ISpan PutHttpStatusCodeAttribute(this ISpan span, int statusCode)
        ////{
        ////    span.PutAttribute(SpanAttributeConstants.HttpStatusCodeKey, AttributeValue.LongAttributeValue(statusCode));
        ////    return span;
        ////}

        ////public static ISpan PutHttpHostAttribute(this ISpan span, string hostName)
        ////{
        ////    span.PutAttribute(SpanAttributeConstants.HttpHostKey, AttributeValue.StringAttributeValue(hostName));
        ////    return span;
        ////}

        ////public static ISpan PutHttpPathAttribute(this ISpan span, string path)
        ////{
        ////    span.PutAttribute(SpanAttributeConstants.HttpPathKey, AttributeValue.StringAttributeValue(path));
        ////    return span;
        ////}

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
