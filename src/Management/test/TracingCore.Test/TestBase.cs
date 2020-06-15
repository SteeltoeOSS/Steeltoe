// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;
using Steeltoe.Management.Tracing;
using System.Collections.Generic;

namespace Steeltoe.Management.Tracing.Test
{
    public class TestBase
    {
        public virtual TracingOptions GetOptions()
        {
            TracingOptions opts = new TracingOptions(null, GetConfiguration());
            return opts;
        }

        public virtual IConfiguration GetConfiguration()
        {
            var settings = new Dictionary<string, string>()
            {
                ["management:tracing:name"] = "foobar",
                ["management:tracing:alwaysSample"] = "true",
                ["management:tracing:useShortTraceIds"] = "true",
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            return builder.Build();
        }

        protected TelemetrySpan GetCurrentSpan(Tracer tracer)
        {
            var span = tracer.CurrentSpan;
            return span.Context.IsValid ? span : null;
        }
    }
}
