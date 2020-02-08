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
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Trace;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Tracing.Test
{
    public class TracingLogProcessorTest
    {
        [Fact]
        public void Process_NoCurrentSpan_DoesNothing()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:name"] = "foobar",
                ["management:tracing:ingressIgnorePattern"] = "pattern",
                ["management:tracing:egressIgnorePattern"] = "pattern",
                ["management:tracing:maxNumberOfAttributes"] = "100",
                ["management:tracing:maxNumberOfAnnotations"] = "100",
                ["management:tracing:maxNumberOfMessageEvents"] = "100",
                ["management:tracing:maxNumberOfLinks"] = "100",
                ["management:tracing:alwaysSample"] = "true",
                ["management:tracing:neverSample"] = "true",
                ["management:tracing:useShortTraceIds"] = "true",
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            var opts = new TracingOptions(null, builder.Build());

            var tracing = new OpenTelemetryTracing(opts);
            var processor = new TracingLogProcessor(opts, tracing);
            var result = processor.Process("InputLogMessage");
            Assert.Equal("InputLogMessage", result);
        }

        [Fact]
        public void Process_CurrentSpan_ReturnsExpected()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:name"] = "foobar",
                ["management:tracing:ingressIgnorePattern"] = "pattern",
                ["management:tracing:egressIgnorePattern"] = "pattern",
                ["management:tracing:maxNumberOfAttributes"] = "100",
                ["management:tracing:maxNumberOfAnnotations"] = "100",
                ["management:tracing:maxNumberOfMessageEvents"] = "100",
                ["management:tracing:maxNumberOfLinks"] = "100",
                ["management:tracing:alwaysSample"] = "true",
                ["management:tracing:neverSample"] = "false",
                ["management:tracing:useShortTraceIds"] = "false",
            };

            var config = TestHelpers.GetConfigurationFromDictionary(appsettings);
            var opts = new TracingOptions(new ApplicationInstanceInfo(config), config);

            var tracing = new OpenTelemetryTracing(opts);
            //tracing.Tracer.SpanBuilder("spanName").StartScopedSpan(out var span);
            tracing.Tracer.StartActiveSpan("spanName", out var span);

            var processor = new TracingLogProcessor(opts, tracing);
            var result = processor.Process("InputLogMessage");

            Assert.Contains("InputLogMessage", result);
            Assert.Contains("[", result);
            Assert.Contains("]", result);
            Assert.Contains(span.Context.TraceId.ToHexString(), result);
            Assert.Contains(span.Context.SpanId.ToHexString(), result);
            Assert.Contains("foobar", result);

            //tracing.Tracer.SpanBuilderWithExplicitParent("spanName2", span).StartScopedSpan(out var childSpan);
            tracing.Tracer.StartActiveSpan("spanName2", span, out var childSpan);


            result = processor.Process("InputLogMessage2");

            Assert.Contains("InputLogMessage2", result);
            Assert.Contains("[", result);
            Assert.Contains("]", result);
            Assert.Contains(childSpan.Context.TraceId.ToHexString(), result);
            Assert.Contains(childSpan.Context.SpanId.ToHexString(), result);
   //         Assert.Contains(span.Context.SpanId.ToHexString(), result);
            Assert.Contains("foobar", result);
        }

        [Fact]
        public void Process_UseShortTraceIds()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:name"] = "foobar",
                ["management:tracing:ingressIgnorePattern"] = "pattern",
                ["management:tracing:egressIgnorePattern"] = "pattern",
                ["management:tracing:maxNumberOfAttributes"] = "100",
                ["management:tracing:maxNumberOfAnnotations"] = "100",
                ["management:tracing:maxNumberOfMessageEvents"] = "100",
                ["management:tracing:maxNumberOfLinks"] = "100",
                ["management:tracing:alwaysSample"] = "true",
                ["management:tracing:neverSample"] = "false",
                ["management:tracing:useShortTraceIds"] = "true",
            };

            var config = TestHelpers.GetConfigurationFromDictionary(appsettings);
            var opts = new TracingOptions(new ApplicationInstanceInfo(config), config);

            var tracing = new OpenTelemetryTracing(opts);
            //tracing.Tracer.SpanBuilder("spanName").StartScopedSpan(out var span);
            tracing.Tracer.StartActiveSpan("spanName", out var span);

            var processor = new TracingLogProcessor(opts, tracing);
            var result = processor.Process("InputLogMessage");

            Assert.Contains("InputLogMessage", result);
            Assert.Contains("[", result);
            Assert.Contains("]", result);

            var full = span.Context.TraceId.ToHexString();
            var shorty = full.Substring(full.Length - 16, 16);

            Assert.Contains(shorty, result);
            Assert.DoesNotContain(full, result);

            Assert.Contains(span.Context.SpanId.ToHexString(), result);
            Assert.Contains("foobar", result);
        }
    }
}
