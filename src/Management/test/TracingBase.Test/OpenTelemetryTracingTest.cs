// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Samplers;
using Steeltoe.Management.OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Steeltoe.Management.Tracing.Test
{
    public class OpenTelemetryTracingTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            var builder = new ConfigurationBuilder();
            var opts = new TracingOptions(null, builder.Build());

            var tracing = new OpenTelemetryTracing(opts);
            Assert.NotNull(tracing.Tracer);
            Assert.NotNull(tracing.TracerConfiguration);
            var p = tracing.TracerConfiguration;
            Assert.Equal(32, p.MaxNumberOfAttributes);
            Assert.Equal(32, p.MaxNumberOfLinks);
            Assert.Equal(128, p.MaxNumberOfEvents);
            Assert.NotEqual(tracing.ConfiguredSampler, new AlwaysOnSampler());
            Assert.NotEqual(tracing.ConfiguredSampler, new AlwaysOffSampler());
        }

        [Fact]
        public void BindsConfigurationCorrectly()
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

            Assert.NotNull(tracing.Tracer);
            Assert.NotNull(tracing.TracerConfiguration);
            var p = tracing.TracerConfiguration;
            Assert.Equal(100, p.MaxNumberOfAttributes);
            Assert.Equal(100, p.MaxNumberOfLinks);
            Assert.Equal(100, p.MaxNumberOfEvents);

            appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:name"] = "foobar",
                ["management:tracing:ingressIgnorePattern"] = "pattern",
                ["management:tracing:egressIgnorePattern"] = "pattern",
                ["management:tracing:maxNumberOfAttributes"] = "100",
                ["management:tracing:maxNumberOfAnnotations"] = "100",
                ["management:tracing:maxNumberOfMessageEvents"] = "100",
                ["management:tracing:maxNumberOfLinks"] = "100",
                ["management:tracing:alwaysSample"] = "true",
                ["management:tracing:useShortTraceIds"] = "true",
            };

            builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            opts = new TracingOptions(null, builder.Build());

            tracing = new OpenTelemetryTracing(opts);
            Assert.NotNull(tracing.Tracer);
            Assert.NotNull(tracing.TracerConfiguration);
            p = tracing.TracerConfiguration;
            Assert.Equal(100, p.MaxNumberOfAttributes);
            Assert.Equal(100, p.MaxNumberOfLinks);
            Assert.Equal(100, p.MaxNumberOfEvents);
            Assert.IsType<AlwaysOnSampler>(tracing.ConfiguredSampler);
        }

        [Fact]
        public void BindsCorrectSampler()
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

            var tracing = new OpenTelemetryTracing(opts, builder => builder.SetSampler(new TestSampler()));

            Assert.Throws<TestSamplerException>(
                () => tracing.Tracer.StartActiveSpan("mySpan", out var span));
        }

        private class TestSampler : Sampler
        {
            public override string Description => throw new System.NotImplementedException();

            public override SamplingResult ShouldSample(in SpanContext parentContext, in ActivityTraceId traceId, in ActivitySpanId spanId, string name, SpanKind spanKind, IDictionary<string, object> attributes, IEnumerable<Link> links)
            {
                throw new TestSamplerException();
            }
        }
    }
}
