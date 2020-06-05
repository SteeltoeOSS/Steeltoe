// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
