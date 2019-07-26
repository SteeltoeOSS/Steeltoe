﻿// Copyright 2017 the original author or authors.
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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using OpenCensus.Common;
using OpenCensus.Trace;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Trace;
using System.Threading;

namespace Steeltoe.Management.Tracing.Observer
{
    public class AspNetCoreMvcViewObserver : AspNetCoreTracingObserver
    {
        internal const string MVC_BEFOREVIEW_EVENT = "Microsoft.AspNetCore.Mvc.BeforeView";
        internal const string MVC_AFTERVIEW_EVENT = "Microsoft.AspNetCore.Mvc.AfterView";

        private const string OBSERVER_NAME = "AspNetCoreMvcViewDiagnosticObserver";

        private static readonly AsyncLocal<SpanContext> ActiveContext = new AsyncLocal<SpanContext>();

        public AspNetCoreMvcViewObserver(ITracingOptions options, ITracing tracing, ILogger<AspNetCoreMvcViewObserver> logger = null)
            : base(OBSERVER_NAME, options, tracing, logger)
        {
        }

        protected internal SpanContext Active
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
            IScope scope = Tracer.SpanBuilder(spanName).StartScopedSpan(out ISpan span);

            span.PutMvcViewExecutingFilePath(ExtractViewPath(viewContext))
                .PutServerSpanKindAttribute();

            ActiveContext.Value = new SpanContext(span, scope);
        }

        protected internal virtual void HandleAfterViewEvent()
        {
            var spanContext = ActiveContext.Value;
            if (spanContext == null)
            {
                Logger?.LogDebug("HandleAfterViewEvent: Missing span context");
                return;
            }

            IScope scope = spanContext.ActiveScope;
            if (scope != null)
            {
                scope.Dispose();
            }

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
