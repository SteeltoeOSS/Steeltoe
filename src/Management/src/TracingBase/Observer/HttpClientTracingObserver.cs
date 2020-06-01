// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenCensus.Common;
using OpenCensus.Trace;
using OpenCensus.Trace.Propagation;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Trace;
using System;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Tracing.Observer
{
    public abstract class HttpClientTracingObserver : DiagnosticObserver
    {
        protected ITracing Tracing { get; }

        protected ITextFormat Propagation { get; }

        protected ITracer Tracer { get; }

        protected ITracingOptions Options { get; }

        protected Regex PathMatcher { get; }

        protected HttpClientTracingObserver(string observerName, string diagnosticName, ITracingOptions options, ITracing tracing, ILogger logger)
            : base(observerName, diagnosticName, logger)
        {
            Options = options;
            Tracing = tracing;
            Propagation = tracing.PropagationComponent.TextFormat;
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

        protected internal ISpan GetCurrentSpan()
        {
            var span = Tracer.CurrentSpan;
            if (span.Context == OpenCensus.Trace.SpanContext.Invalid)
            {
                return null;
            }

            return span;
        }

        public class SpanContext
        {
            public SpanContext(ISpan active, IScope activeScope)
            {
                Active = active;
                ActiveScope = activeScope;
            }

            public ISpan Active { get; }

            public IScope ActiveScope { get; }
        }
    }
}
