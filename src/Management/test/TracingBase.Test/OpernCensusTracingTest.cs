// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenCensus.Trace;
using OpenCensus.Trace.Sampler;
using Steeltoe.Management.Census.Trace;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Tracing.Test
{
    public class OpernCensusTracingTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            TracingOptions opts = new TracingOptions(null, builder.Build());

            OpenCensusTracing tracing = new OpenCensusTracing(opts);
            Assert.NotNull(tracing.ExportComponent);
            Assert.NotNull(tracing.PropagationComponent);
            Assert.NotNull(tracing.TraceConfig);
            Assert.NotNull(tracing.Tracer);

            var p = tracing.TraceConfig.ActiveTraceParams;
            Assert.NotNull(p);

            Assert.NotNull(p.Sampler);
            Assert.NotEqual(Samplers.AlwaysSample, p.Sampler);
            Assert.NotEqual(Samplers.NeverSample, p.Sampler);
            Assert.Equal(32, p.MaxNumberOfAnnotations);
            Assert.Equal(32, p.MaxNumberOfAttributes);
            Assert.Equal(128, p.MaxNumberOfLinks);
            Assert.Equal(128, p.MaxNumberOfMessageEvents);
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

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            TracingOptions opts = new TracingOptions(null, builder.Build());

            OpenCensusTracing tracing = new OpenCensusTracing(opts, new Sampler());
            Assert.NotNull(tracing.ExportComponent);
            Assert.NotNull(tracing.PropagationComponent);
            Assert.NotNull(tracing.TraceConfig);
            Assert.NotNull(tracing.Tracer);

            var p = tracing.TraceConfig.ActiveTraceParams;
            Assert.NotNull(p);

            Assert.NotNull(p.Sampler);
            Assert.IsType<Sampler>(p.Sampler);

            Assert.Equal(100, p.MaxNumberOfAnnotations);
            Assert.Equal(100, p.MaxNumberOfAttributes);
            Assert.Equal(100, p.MaxNumberOfLinks);
            Assert.Equal(100, p.MaxNumberOfMessageEvents);

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

            tracing = new OpenCensusTracing(opts, null);
            Assert.NotNull(tracing.ExportComponent);
            Assert.NotNull(tracing.PropagationComponent);
            Assert.NotNull(tracing.TraceConfig);
            Assert.NotNull(tracing.Tracer);

            p = tracing.TraceConfig.ActiveTraceParams;
            Assert.NotNull(p);

            Assert.Equal(Samplers.AlwaysSample, p.Sampler);
            Assert.Equal(100, p.MaxNumberOfAnnotations);
            Assert.Equal(100, p.MaxNumberOfAttributes);
            Assert.Equal(100, p.MaxNumberOfLinks);
            Assert.Equal(100, p.MaxNumberOfMessageEvents);
        }

        private class Sampler : ISampler
        {
            public string Description => throw new System.NotImplementedException();

            public bool ShouldSample(ISpanContext parentContext, bool hasRemoteParent, ITraceId traceId, ISpanId spanId, string name, IEnumerable<ISpan> parentLinks)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
