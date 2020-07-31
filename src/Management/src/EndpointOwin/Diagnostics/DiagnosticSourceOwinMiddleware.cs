// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Diagnostics
{
    public class DiagnosticSourceOwinMiddleware : OwinMiddleware
    {
        private const string ActivityName = "Steeltoe.Owin.Hosting.HttpRequestIn";
        private const string ActivityStartKey = "Steeltoe.Owin.Hosting.HttpRequestIn.Start";

        private readonly ILogger<DiagnosticSourceOwinMiddleware> _logger;

        private readonly DiagnosticListener _listener = new DiagnosticListener("Steeltoe.Owin");

        public DiagnosticSourceOwinMiddleware(OwinMiddleware next, ILogger<DiagnosticSourceOwinMiddleware> logger = null)
            : base(next)
        {
            _logger = logger;
        }

        public override async Task Invoke(IOwinContext context)
        {
            var started = BeginRequest(context);

            try
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                EndRequest(context, started, e);
                throw;
            }

            EndRequest(context, started);
        }

        private Activity BeginRequest(IOwinContext context)
        {
            var activity = new Activity(ActivityName);
            if (_listener.IsEnabled(ActivityStartKey))
            {
                _listener.StartActivity(activity, new { OwinContext = context });
            }
            else
            {
                activity.Start();
            }

            return activity;
        }

        private void EndRequest(IOwinContext context, Activity activity, Exception exception = null)
        {
            if (exception != null)
            {
                context.Set("Steeltoe.Owin.Exception", exception);
            }

            _listener.StopActivity(activity, new { OwinContext = context });
        }
    }
}
