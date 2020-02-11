// Copyright 2017 the original author or authors.
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
