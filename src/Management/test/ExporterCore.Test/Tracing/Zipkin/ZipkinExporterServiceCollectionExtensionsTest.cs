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

            services.AddSingleton(config);

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
