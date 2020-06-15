// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Trace;
using System;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Tracing.Observer
{
    public abstract class HttpClientTracingObserver : DiagnosticObserver
    {
        protected ITracing Tracing { get; }

        protected ITextFormat TextFormat { get; }

        protected Tracer Tracer { get; }

        protected ITracingOptions Options { get; }

        protected Regex PathMatcher { get; }

        protected HttpClientTracingObserver(string observerName, string diagnosticName, ITracingOptions options, ITracing tracing, ILogger logger)
            : base(observerName, diagnosticName, logger)
        {
            Options = options;
            Tracing = tracing;
            TextFormat = tracing.TextFormat;
            Tracer = tracing.Tracer;
            PathMatcher = new Regex(options.EgressIgnorePattern);
        }

        protected internal virtual bool ShouldIgnoreRequest(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return PathMatcher.IsMatch(path);
        }

        protected internal virtual string GetExceptionMessage(Exception exception)
        {
            return exception.GetType().Name + " : " + exception.Message;
        }

        protected internal virtual string GetExceptionStackTrace(Exception exception)
        {
            if (exception.StackTrace != null)
            {
                return exception.StackTrace.ToString();
            }

            return string.Empty;
        }

        protected internal TelemetrySpan GetCurrentSpan()
        {
            var span = Tracer.CurrentSpan;

            return span.Context.IsValid ? span : null;
        }
    }
}
