// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Trace;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Steeltoe.Management.Census.Trace
{
    public static class SpanExtensions
    {
        ////public static ISpan PutClientSpanKindAttribute(this ISpan span)
        ////{
        ////    span.PutAttribute(SpanAttributeConstants.SpanKindKey, AttributeValue.StringAttributeValue(SpanAttributeConstants.ClientSpanKind));
        ////    return span;
        ////}

        ////public static ISpan PutServerSpanKindAttribute(this ISpan span)
        ////{
        ////    span.PutAttribute(SpanAttributeConstants.SpanKindKey, AttributeValue.StringAttributeValue(SpanAttributeConstants.ServerSpanKind));
        ////    return span;
        ////}

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

        ////public static ISpan PutHttpResponseSizeAttribute(this ISpan span, long size)
        ////{
        ////    span.PutAttribute(SpanAttributeConstants.HttpResponseSizeKey, AttributeValue.LongAttributeValue(size));
        ////    return span;
        ////}

        ////public static ISpan PutHttpRequestSizeAttribute(this ISpan span, long size)
        ////{
        ////    span.PutAttribute(SpanAttributeConstants.HttpRequestSizeKey, AttributeValue.LongAttributeValue(size));
        ////    return span;
        ////}

        public static ISpan PutErrorAttribute(this ISpan span, string errorMessage)
        {
            span.PutAttribute(SpanAttributeConstants.ErrorKey, AttributeValue.StringAttributeValue(errorMessage));
            return span;
        }

        public static ISpan PutErrorStackTraceAttribute(this ISpan span, string errorStackTrace)
        {
            span.PutAttribute(SpanAttributeConstants.ErrorStackTrace, AttributeValue.StringAttributeValue(errorStackTrace));
            return span;
        }

        public static ISpan PutMvcControllerClass(this ISpan span, string className)
        {
            span.PutAttribute(SpanAttributeConstants.MvcControllerClass, AttributeValue.StringAttributeValue(className));
            return span;
        }

        public static ISpan PutMvcControllerAction(this ISpan span, string actionName)
        {
            span.PutAttribute(SpanAttributeConstants.MvcControllerMethod, AttributeValue.StringAttributeValue(actionName));
            return span;
        }

        public static ISpan PutMvcViewExecutingFilePath(this ISpan span, string actionName)
        {
            span.PutAttribute(SpanAttributeConstants.MvcViewFilePath, AttributeValue.StringAttributeValue(actionName));
            return span;
        }

        public static ISpan PutHttpRequestHeadersAttribute(this ISpan span, List<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            PutHeadersAttribute(span, "http.request.", headers);
            return span;
        }

        public static ISpan PutHttpRequestHeadersAttribute(this ISpan span, NameValueCollection headers)
        {
            PutHeadersAttribute(span, "http.request.", headers);
            return span;
        }

        public static ISpan PutHttpResponseHeadersAttribute(this ISpan span, List<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            PutHeadersAttribute(span, "http.response.", headers);
            return span;
        }

        public static ISpan PutHttpResponseHeadersAttribute(this ISpan span, NameValueCollection headers)
        {
            PutHeadersAttribute(span, "http.response.", headers);
            return span;
        }

        private static void PutHeadersAttribute(ISpan span, string key, List<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var header in headers)
            {
                span.PutAttribute(key + header.Key, ToCommaDelimitedStringAttribute(header.Value));
            }
        }

        private static void PutHeadersAttribute(ISpan span, string key, NameValueCollection headers)
        {
            foreach (var header in headers.AllKeys)
            {
                IAttributeValue values = ToCommaDelimitedStringAttribute(headers.GetValues(header));
                span.PutAttribute(key + header, values);
            }
        }

        private static IAttributeValue ToCommaDelimitedStringAttribute(IEnumerable<string> values)
        {
            var list = string.Join(",", values);
            return AttributeValue.StringAttributeValue(list);
        }
    }
}
