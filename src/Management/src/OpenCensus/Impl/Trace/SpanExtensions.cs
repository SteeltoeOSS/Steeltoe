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
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public static class SpanExtensions
    {
        public static ISpan PutClientSpanKindAttribute(this ISpan span)
        {
            span.PutAttribute(SpanAttributeConstants.SpanKindKey, AttributeValue.StringAttributeValue(SpanAttributeConstants.ClientSpanKind));
            return span;
        }

        public static ISpan PutServerSpanKindAttribute(this ISpan span)
        {
            span.PutAttribute(SpanAttributeConstants.SpanKindKey, AttributeValue.StringAttributeValue(SpanAttributeConstants.ServerSpanKind));
            return span;
        }

        public static ISpan PutHttpUrlAttribute(this ISpan span, string url)
        {
            span.PutAttribute(SpanAttributeConstants.HttpUrlKey, AttributeValue.StringAttributeValue(url));
            return span;
        }

        public static ISpan PutHttpMethodAttribute(this ISpan span, string method)
        {
            span.PutAttribute(SpanAttributeConstants.HttpMethodKey, AttributeValue.StringAttributeValue(method));
            return span;
        }

        public static ISpan PutHttpStatusCodeAttribute(this ISpan span, int statusCode)
        {
            span.PutAttribute(SpanAttributeConstants.HttpStatusCodeKey, AttributeValue.LongAttributeValue(statusCode));
            return span;
        }

        public static ISpan PutHttpHostAttribute(this ISpan span, string hostName)
        {
            span.PutAttribute(SpanAttributeConstants.HttpHostKey, AttributeValue.StringAttributeValue(hostName));
            return span;
        }

        public static ISpan PutHttpPathAttribute(this ISpan span, string path)
        {
            span.PutAttribute(SpanAttributeConstants.HttpPathKey, AttributeValue.StringAttributeValue(path));
            return span;
        }

        public static ISpan PutHttpResponseSizeAttribute(this ISpan span, long size)
        {
            span.PutAttribute(SpanAttributeConstants.HttpResponseSizeKey, AttributeValue.LongAttributeValue(size));
            return span;
        }

        public static ISpan PutHttpRequestSizeAttribute(this ISpan span, long size)
        {
            span.PutAttribute(SpanAttributeConstants.HttpRequestSizeKey, AttributeValue.LongAttributeValue(size));
            return span;
        }

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

        public static ISpan PutHttpResponseHeadersAttribute(this ISpan span, List<KeyValuePair<string, IEnumerable<string>>> headers)
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

        public static ISpan PutHttpRequestHeadersAttribute(this ISpan span, NameValueCollection headers)
        {
            PutHeadersAttribute(span, "http.request.", headers);
            return span;
        }

        public static ISpan PutHttpResponseHeadersAttribute(this ISpan span, NameValueCollection headers)
        {
            PutHeadersAttribute(span, "http.response.", headers);
            return span;
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
