// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Diagnostics.Tracing;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    public class EndpointServiceCollectionExtensionsTest : BaseTest
    {
        [Fact]
        public void AddMetricsActuator_ThrowsOnNulls()
        {
            const IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            const IConfiguration config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddMetricsActuator(config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => services2.AddMetricsActuator(config));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddMetricsActuator_AddsCorrectServices()
        {
            var services = new ServiceCollection();
            var config = GetConfiguration();

            services.AddOptions();
            services.AddLogging();
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());
            services.AddSingleton(config);
            services.AddMetricsActuator();

            var serviceProvider = services.BuildServiceProvider();

            var mgr = serviceProvider.GetService<IDiagnosticsManager>();
            Assert.NotNull(mgr);
            var hst = serviceProvider.GetService<IHostedService>();
            Assert.NotNull(hst);
            var opts = serviceProvider.GetService<IMetricsObserverOptions>();
            Assert.NotNull(opts);

            var observers = serviceProvider.GetServices<IDiagnosticObserver>();
            var list = observers.ToList();
            Assert.Single(list);

            var ep = serviceProvider.GetService<MetricsEndpoint>();
            Assert.NotNull(ep);
        }

        [Fact]
        public void AddWavefront_ThrowsWhenNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddWavefrontMetrics(null));
            Assert.Contains("services", ex.Message);
        }

        private IConfiguration GetConfiguration()
        {
            var builder = new ConfigurationBuilder();
            return builder.Build();
        }
    }
}
