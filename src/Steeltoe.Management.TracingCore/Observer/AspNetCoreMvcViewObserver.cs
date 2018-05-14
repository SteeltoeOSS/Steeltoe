// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Census.Trace.Unsafe;
using System.Threading;

namespace Steeltoe.Management.Tracing.Observer
{
    public class AspNetCoreMvcViewObserver : AspNetCoreTracingObserver
    {
        internal const string MVC_BEFOREVIEW_EVENT = "Microsoft.AspNetCore.Mvc.BeforeView";
        internal const string MVC_AFTERVIEW_EVENT = "Microsoft.AspNetCore.Mvc.AfterView";

        private const string OBSERVER_NAME = "AspNetCoreMvcViewDiagnosticObserver";

        private static AsyncLocal<SpanContext> active = new AsyncLocal<SpanContext>((arg) => HandleValueChangedEvent(arg));

        public AspNetCoreMvcViewObserver(ITracingOptions options, ITracing tracing, ILogger<AspNetCoreMvcViewObserver> logger = null)
            : base(OBSERVER_NAME, options, tracing, logger)
        {
        }

        protected internal SpanContext Active
        {
            get
            {
                return active.Value;
            }
        }

        public override void ProcessEvent(string evnt, object arg)
        {
            if (arg == null)
            {
                return;
            }

            if (evnt == MVC_BEFOREVIEW_EVENT)
            {
                Logger?.LogTrace("HandleBeforeViewEvent start {thread}", Thread.CurrentThread.ManagedThreadId);
                var viewContext = DiagnosticHelpers.GetProperty<ViewContext>(arg, "viewContext");
                var context = viewContext?.HttpContext;

                if (viewContext != null && context != null)
                {
                    HandleBeforeViewEvent(context, viewContext);
                }

                Logger?.LogTrace("HandleBeforeViewEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == MVC_AFTERVIEW_EVENT)
            {
                Logger?.LogTrace("HandleAfterViewEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                HandleAfterViewEvent();

                Logger?.LogTrace("HandleAfterViewEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal virtual void HandleBeforeViewEvent(HttpContext context, ViewContext viewContext)
        {
            if (ShouldIgnoreRequest(context.Request.Path))
            {
                Logger?.LogDebug("HandleBeforeViewEvent: Ignoring path: {path}", context.Request.Path.ToString());
                return;
            }

            if (active.Value != null)
            {
                Logger?.LogDebug("HandleBeforeViewEvent: Continuing existing span!");
                return;
            }

            var current = AsyncLocalContext.CurrentSpan;
            if (current == null)
            {
                Logger?.LogDebug("HandleBeforeActionEvent: No CurrentSpan!");
                return;
            }

            string spanName = ExtractSpanName(viewContext);
            ISpan span = Tracer.SpanBuilder(spanName).StartSpan();

            span.PutMvcViewExecutingFilePath(ExtractViewPath(viewContext))
                .PutServerSpanKindAttribute();

            active.Value = new SpanContext(span, current);

            AsyncLocalContext.CurrentSpan = span;
        }

        protected internal virtual void HandleAfterViewEvent()
        {
            var spanContext = active.Value;
            if (spanContext == null)
            {
                Logger?.LogDebug("HandleAfterViewEvent: Missing span context");
                return;
            }

            ISpan span = spanContext.Active;
            if (span != null)
            {
                span.End();
            }

            AsyncLocalContext.CurrentSpan = spanContext.Previous;
            active.Value = null;
        }

        protected internal virtual string ExtractSpanName(ViewContext context)
        {
            return "view:" + context.View.Path;
        }

        protected internal string ExtractViewPath(ViewContext context)
        {
            return context.View.Path;
        }
    }
}
