// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Trace;
using System.Threading;

namespace Steeltoe.Management.Tracing.Observer
{
    public class AspNetCoreMvcViewObserver : AspNetCoreTracingObserver
    {
        internal const string MVC_BEFOREVIEW_EVENT = "Microsoft.AspNetCore.Mvc.BeforeView";
        internal const string MVC_AFTERVIEW_EVENT = "Microsoft.AspNetCore.Mvc.AfterView";

        private const string OBSERVER_NAME = "AspNetCoreMvcViewDiagnosticObserver";

        private static readonly AsyncLocal<TelemetrySpan> ActiveContext = new AsyncLocal<TelemetrySpan>();

        public AspNetCoreMvcViewObserver(ITracingOptions options, ITracing tracing, ILogger<AspNetCoreMvcViewObserver> logger = null)
            : base(OBSERVER_NAME, options, tracing, logger)
        {
        }

        protected internal TelemetrySpan Active
        {
            get
            {
                return ActiveContext.Value;
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

            if (ActiveContext.Value != null)
            {
                Logger?.LogDebug("HandleBeforeViewEvent: Continuing existing span!");
                return;
            }

            var current = GetCurrentSpan();
            if (current == null)
            {
                Logger?.LogDebug("HandleBeforeActionEvent: No CurrentSpan!");
                return;
            }

            string spanName = ExtractSpanName(viewContext);
            Tracer.StartActiveSpan(spanName, SpanKind.Server, out var span);

            span.PutMvcViewExecutingFilePath(ExtractViewPath(viewContext));

            ActiveContext.Value = span;
        }

        protected internal virtual void HandleAfterViewEvent()
        {
            var span = ActiveContext.Value;
            if (span == null)
            {
                Logger?.LogDebug("HandleAfterViewEvent: Missing span context");
                return;
            }

            span.End();

            ActiveContext.Value = null;
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
