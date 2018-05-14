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
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Census.Trace.Propagation;
using Steeltoe.Management.Census.Trace.Unsafe;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Tracing.Observer
{
    public class AspNetCoreMvcActionObserver : AspNetCoreTracingObserver
    {
        internal const string MVC_BEFOREACTION_EVENT = "Microsoft.AspNetCore.Mvc.BeforeAction";
        internal const string MVC_AFTERACTION_EVENT = "Microsoft.AspNetCore.Mvc.AfterAction";

        private const string OBSERVER_NAME = "AspNetCoreMvcActionDiagnosticObserver";

        private static AsyncLocal<SpanContext> active = new AsyncLocal<SpanContext>((arg) => HandleValueChangedEvent(arg));

        public AspNetCoreMvcActionObserver(ITracingOptions options, ITracing tracing, ILogger<AspNetCoreMvcActionObserver> logger = null)
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

            if (evnt == MVC_BEFOREACTION_EVENT)
            {
                Logger?.LogTrace("HandleBeforeActionEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                var descriptor = DiagnosticHelpers.GetProperty<ActionDescriptor>(arg, "actionDescriptor");
                var context = DiagnosticHelpers.GetProperty<HttpContext>(arg, "httpContext");

                if (descriptor != null && context != null)
                {
                    HandleBeforeActionEvent(context, descriptor);
                }

                Logger?.LogTrace("HandleBeforeActionEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == MVC_AFTERACTION_EVENT)
            {
                Logger?.LogTrace("HandleAfterActionEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                HandleAfterActionEvent();

                Logger?.LogTrace("HandleAfterActionEvent finsih {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal virtual void HandleBeforeActionEvent(HttpContext context, ActionDescriptor descriptor)
        {
            if (ShouldIgnoreRequest(context.Request.Path))
            {
                Logger?.LogDebug("HandleBeforeActionEvent: Ignoring path: {path}", context.Request.Path.ToString());
                return;
            }

            if (active.Value != null)
            {
                Logger?.LogDebug("HandleBeforeActionEvent: Continuing existing span!");
                return;
            }

            var current = AsyncLocalContext.CurrentSpan;
            if (current == null)
            {
                Logger?.LogDebug("HandleBeforeActionEvent: No CurrentSpan!");
                return;
            }

            string spanName = ExtractSpanName(descriptor);
            ISpan span = Tracer.SpanBuilder(spanName).StartSpan();

            span.PutMvcControllerClass(ExtractControllerName(descriptor))
                .PutServerSpanKindAttribute()
                .PutMvcControllerAction(ExtractActionName(descriptor));

            active.Value = new SpanContext(span, current);
            AsyncLocalContext.CurrentSpan = span;
        }

        protected internal virtual void HandleAfterActionEvent()
        {
            var spanContext = active.Value;
            if (spanContext == null)
            {
                Logger?.LogDebug("HandleAfterActionEvent: Missing span context");
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

        protected internal string ExtractSpanName(ActionDescriptor descriptor)
        {
            ControllerActionDescriptor controllerActionDescriptor = descriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                return "action:" + controllerActionDescriptor.ControllerName + "/" + controllerActionDescriptor.ActionName;
            }
            else
            {
                return "action:" + descriptor.DisplayName;
            }
        }

        protected internal string ExtractControllerName(ActionDescriptor descriptor)
        {
            ControllerActionDescriptor controllerActionDescriptor = descriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                return controllerActionDescriptor.ControllerTypeInfo.FullName;
            }
            else
            {
                return "Unknown";
            }
        }

        protected internal string ExtractActionName(ActionDescriptor descriptor)
        {
            ControllerActionDescriptor controllerActionDescriptor = descriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                return controllerActionDescriptor.MethodInfo.ToString();
            }
            else
            {
                return "Unknown";
            }
        }
    }
}
