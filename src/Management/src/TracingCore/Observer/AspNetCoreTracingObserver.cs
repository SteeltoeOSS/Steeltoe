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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Trace;
using System;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Tracing.Observer
{
    public abstract class AspNetCoreTracingObserver : DiagnosticObserver
    {
        private const string DIAGNOSTIC_NAME = "Microsoft.AspNetCore";

        protected ITracing Tracing { get; }

        protected ITextFormat Propagation { get; }

        protected Tracer Tracer { get; }

        protected ITracingOptions Options { get; }

        protected Regex PathMatcher { get; }

        protected AspNetCoreTracingObserver(string observerName, ITracingOptions options, ITracing tracing, ILogger logger)
            : base(observerName, DIAGNOSTIC_NAME, logger)
        {
            Options = options;
            Tracing = tracing;
            Propagation = tracing.TextFormat;
            Tracer = tracing.Tracer;
            PathMatcher = new Regex(options.IngressIgnorePattern);
        }

        protected internal virtual bool ShouldIgnoreRequest(PathString pathString)
        {
            string path = pathString.Value;
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

        //public class SpanContext
        //{
        //    public SpanContext(ISpan active, IScope activeScope)
        //    {
        //        Active = active;
        //        ActiveScope = activeScope;
        //    }

        //    public ISpan Active { get; }

        //    public IScope ActiveScope { get; }
        //}
    }
}
