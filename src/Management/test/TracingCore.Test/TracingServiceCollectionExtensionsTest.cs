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
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Trace;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Tracing.Test
{
    public class TracingServiceCollectionExtensionsTest : TestBase
    {
        [Fact]
        public void AddDistributedTracing_ThrowsOnNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => TracingServiceCollectionExtensions.AddDistributedTracing(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => TracingServiceCollectionExtensions.AddDistributedTracing(services2, config));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddDistributedTracing_AddsCorrectServices()
        {
            var services = new ServiceCollection();
            var config = GetConfiguration();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddOptions();
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());
            services.AddDistributedTracing(config);

            var serviceProvider = services.BuildServiceProvider();

            var mgr = serviceProvider.GetService<IDiagnosticsManager>();
            Assert.NotNull(mgr);
            var hst = serviceProvider.GetService<IHostedService>();
            Assert.NotNull(hst);
            var opts = serviceProvider.GetService<ITracingOptions>();
            Assert.NotNull(opts);

            var observers = serviceProvider.GetServices<IDiagnosticObserver>();
            var list = observers.ToList();
            Assert.Equal(5, list.Count);

            var tracing = serviceProvider.GetService<ITracing>();
            Assert.NotNull(tracing);

            var processer = serviceProvider.GetService<IDynamicMessageProcessor>();
            Assert.NotNull(processer);
        }
    }
}
