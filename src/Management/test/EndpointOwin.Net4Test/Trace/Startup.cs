// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Owin;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.EndpointOwin.Diagnostics;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwin.Trace.Test
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var builder = new ConfigurationBuilder();

            var appSettings = new Dictionary<string, string>(OwinTestHelpers.Appsettings)
            {
            };

            builder.AddInMemoryCollection(appSettings);
            var config = builder.Build();

            app.UseDiagnosticSourceMiddleware();
#pragma warning disable CS0612 // Type or member is obsolete
            app.UseTraceActuator(config);
#pragma warning restore CS0612 // Type or member is obsolete

            DiagnosticsManager.Instance.Start();
        }
    }
}
