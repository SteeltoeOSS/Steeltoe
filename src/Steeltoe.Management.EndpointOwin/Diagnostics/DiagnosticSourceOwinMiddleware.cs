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

        private ILogger<DiagnosticSourceOwinMiddleware> _logger;

        private DiagnosticListener _listener = new DiagnosticListener("Steeltoe.Owin");

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
                await Next.Invoke(context);
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
            Activity activity = new Activity(ActivityName);
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
