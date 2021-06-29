﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            tracing.Tracer.StartActiveSpan("spanName", out var span);

            var processor = new TracingLogProcessor(opts, tracing);
            var result = processor.Process("InputLogMessage");

            Assert.Contains("InputLogMessage", result);
            Assert.Contains("[", result);
            Assert.Contains("]", result);
            Assert.Contains(span.Context.TraceId.ToHexString(), result);
            Assert.Contains(span.Context.SpanId.ToHexString(), result);
            Assert.Contains("foobar", result);

            tracing.Tracer.StartActiveSpan("spanName2", span, out var childSpan);

            result = processor.Process("InputLogMessage2");

            Assert.Contains("InputLogMessage2", result);
            Assert.Contains("[", result);
            Assert.Contains("]", result);
            Assert.Contains(childSpan.Context.TraceId.ToHexString(), result);
            Assert.Contains(childSpan.Context.SpanId.ToHexString(), result);

            // Assert.Contains(span.Context.SpanId.ToHexString(), result);  TODO: ParentID not supported
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
