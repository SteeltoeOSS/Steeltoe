// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Management.Tracing.Test
{
    public class TestBase
    {
        public virtual TracingOptions GetOptions()
        {
            var opts = new TracingOptions(null, GetConfiguration());
            return opts;
        }

        public virtual IConfiguration GetConfiguration()
            => new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> { { "management:tracing:name", "foobar" } })
                    .Build();

        protected TelemetrySpan GetCurrentSpan(Tracer tracer)
        {
            var span = Tracer.CurrentSpan;
            return span.Context.IsValid ? span : null;
        }

        protected object GetPrivateField(object baseObject, string fieldName)
             => baseObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(baseObject);
    }
}
