// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenCensus.Exporter.Zipkin;
using OpenCensus.Trace;
using OpenCensus.Trace.Config;
using OpenCensus.Trace.Export;
using OpenCensus.Trace.Propagation;
using Steeltoe.Common;
using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Exporter.Tracing.Test
{
    public class ZipkinExporterServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddZipkinExporter_ThrowsOnNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ZipkinExporterServiceCollectionExtensions.AddZipkinExporter(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => ZipkinExporterServiceCollectionExtensions.AddZipkinExporter(services2, config));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddZipkinExporter_AddsCorrectServices()
        {
            var services = new ServiceCollection();
            var config = GetConfiguration();

            services.AddOptions();
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());
            services.AddSingleton<ITracing>(new TestTracing());

            services.AddZipkinExporter(config);

            var serviceProvider = services.BuildServiceProvider();

            var tracing = serviceProvider.GetService<ZipkinTraceExporter>();
            Assert.NotNull(tracing);
        }

        private IConfiguration GetConfiguration()
        {
            var settings = new Dictionary<string, string>()
            {
                ["management:tracing:exporter:zipkin:serviceName"] = "foobar",
                ["management:tracing:exporter:zipkin:validateCertificates"] = "false",
                ["management:tracing:exporter:zipkin:timeoutSeconds"] = "100",
                ["management:tracing:exporter:zipkin:useShortTraceIds"] = "true",
                ["management:tracing:exporter:zipkin:endpoint"] = "https://foo.com/api/v2/spans"
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            return builder.Build();
        }

        private class TestTracing : ITracing
        {
            public ITracer Tracer => null;

            public IPropagationComponent PropagationComponent => null;

            public IExportComponent ExportComponent => null;

            public ITraceConfig TraceConfig => null;
        }
    }
}
