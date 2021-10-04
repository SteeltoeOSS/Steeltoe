// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Tracing.Test
{
    public class TracingBaseServiceCollectionExtensionsTest : TestBase
    {
        [Fact]
        public void AddDistributedTracing_ThrowsOnNulls()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => TracingBaseServiceCollectionExtensions.AddDistributedTracing(null));
            Assert.Equal("services", ex.ParamName);
        }

        [Fact]
        public void AddDistributedTracing_ConfiguresExpectedDefaults()
        {
            var services = new ServiceCollection().AddSingleton(GetConfiguration());

            var serviceProvider = services.AddDistributedTracing().BuildServiceProvider();

            // confirm Steeltoe types were registered
            Assert.NotNull(serviceProvider.GetService<ITracingOptions>());
            Assert.IsType<TracingLogProcessor>(serviceProvider.GetService<IDynamicMessageProcessor>());

            // confirm OpenTelemetry types were registered
            var tracerProvider = serviceProvider.GetService<TracerProvider>();
            Assert.NotNull(tracerProvider);
            var hst = serviceProvider.GetService<IHostedService>();
            Assert.NotNull(hst);

            // confirm instrumentation(s) were added as expected
            var instrumentations = GetPrivateField(tracerProvider, "instrumentations") as List<object>;
            Assert.NotNull(instrumentations);
            Assert.Single(instrumentations);
            Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http"));

            Assert.IsType<CompositeTextMapPropagator>(Propagators.DefaultTextMapPropagator);
            var comp = Propagators.DefaultTextMapPropagator as CompositeTextMapPropagator;
            var props = GetPrivateField(comp, "propagators") as List<TextMapPropagator>;
            Assert.Equal(2, props.Count);
            Assert.Contains(props, p => p is B3Propagator);
            Assert.Contains(props, p => p is BaggagePropagator);
        }

        // this test should find Jaeger and Zipkin exporters, see TracingCore.Test for OTLP
        [Fact]
        public void AddDistributedTracing_WiresIncludedExporters()
        {
            var services = new ServiceCollection().AddSingleton(GetConfiguration());

            var serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider();
            var hst = serviceProvider.GetService<IHostedService>();
            Assert.NotNull(hst);
            var tracerProvider = serviceProvider.GetService<TracerProvider>();
            Assert.NotNull(tracerProvider);

            Assert.NotNull(serviceProvider.GetService<IOptions<ZipkinExporterOptions>>());
            Assert.NotNull(serviceProvider.GetService<IOptions<JaegerExporterOptions>>());
        }

        [Fact]
        public void AddDistributedTracing_ConfiguresSamplers()
        {
            // test AlwaysOn
            var services = new ServiceCollection().AddSingleton(GetConfiguration(new Dictionary<string, string> { { "Management:Tracing:AlwaysSample", "true" } }));
            var serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider();
            var hst = serviceProvider.GetService<IHostedService>();
            Assert.NotNull(hst);
            var tracerProvider = serviceProvider.GetService<TracerProvider>();
            Assert.NotNull(tracerProvider);

            // test AlwaysOff
            services = new ServiceCollection().AddSingleton(GetConfiguration(new Dictionary<string, string> { { "Management:Tracing:NeverSample", "true" } }));
            serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider();
            hst = serviceProvider.GetService<IHostedService>();
            Assert.NotNull(hst);
            tracerProvider = serviceProvider.GetService<TracerProvider>();
            Assert.NotNull(tracerProvider);
        }
    }
}
