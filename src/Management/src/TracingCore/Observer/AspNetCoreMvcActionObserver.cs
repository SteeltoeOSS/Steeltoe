// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Trace;
using System.Threading;

namespace Steeltoe.Management.Tracing.Observer
{
    public class AspNetCoreMvcActionObserver : AspNetCoreTracingObserver
    {
        internal const string MVC_BEFOREACTION_EVENT = "Microsoft.AspNetCore.Mvc.BeforeAction";
        internal const string MVC_AFTERACTION_EVENT = "Microsoft.AspNetCore.Mvc.AfterAction";

        private const string OBSERVER_NAME = "AspNetCoreMvcActionDiagnosticObserver";

        private static readonly AsyncLocal<TelemetrySpan> ActiveValue = new AsyncLocal<TelemetrySpan>();

        public AspNetCoreMvcActionObserver(ITracingOptions options, ITracing tracing, ILogger<AspNetCoreMvcActionObserver> logger = null)
            : base(OBSERVER_NAME, options, tracing, logger)
        {
        }

        protected internal TelemetrySpan Active
        {
            get
            {
                return ActiveValue.Value;
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

                Logger?.LogTrace("HandleAfterActionEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal virtual void HandleBeforeActionEvent(HttpContext context, ActionDescriptor descriptor)
        {
            if (ShouldIgnoreRequest(context.Request.Path))
            {
                Logger?.LogDebug("HandleBeforeActionEvent: Ignoring path: {path}", context.Request.Path.ToString());
                return;
            }

            if (ActiveValue.Value != null)
            {
                Logger?.LogDebug("HandleBeforeActionEvent: Continuing existing span!");
                return;
            }

            var current = GetCurrentSpan();
            if (current == null)
            {
                Logger?.LogDebug("HandleBeforeActionEvent: No CurrentSpan!");
                return;
            }

            var spanName = ExtractSpanName(descriptor);
            Tracer.StartActiveSpan(spanName, SpanKind.Server, out var span);

            span.PutMvcControllerClass(ExtractControllerName(descriptor))
                .PutMvcControllerAction(ExtractActionName(descriptor));

            ActiveValue.Value = span;
        }

        protected internal virtual void HandleAfterActionEvent()
        {
            var span = ActiveValue.Value;
            if (span == null)
            {
                Logger?.LogDebug("HandleAfterActionEvent: Missing span context");
                return;
            }

            span.End();

            ActiveValue.Value = null;
        }

        protected internal string ExtractSpanName(ActionDescriptor descriptor)
        {
            if (descriptor is ControllerActionDescriptor controllerActionDescriptor)
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
            if (descriptor is ControllerActionDescriptor controllerActionDescriptor)
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
            if (descriptor is ControllerActionDescriptor controllerActionDescriptor)
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
