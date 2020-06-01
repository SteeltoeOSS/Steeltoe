// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    public class EndpointServiceCollectionExtensionsTest : BaseTest
    {
        [Fact]
        public void AddMetricsActuator_ThrowsOnNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddMetricsActuator(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddMetricsActuator(services2, config));
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
            services.AddMetricsActuator(config);

            var serviceProvider = services.BuildServiceProvider();

            var mgr = serviceProvider.GetService<IDiagnosticsManager>();
            Assert.NotNull(mgr);
            var hst = serviceProvider.GetService<IHostedService>();
            Assert.NotNull(hst);
            var opts = serviceProvider.GetService<IMetricsOptions>();
            Assert.NotNull(opts);

            var observers = serviceProvider.GetServices<IDiagnosticObserver>();
            var list = observers.ToList();
            Assert.Equal(2, list.Count);

            var polled = serviceProvider.GetServices<IPolledDiagnosticSource>();
            var list2 = polled.ToList();
            Assert.Single(list2);

            var stats = serviceProvider.GetService<IStats>();
            Assert.NotNull(stats);

            var tags = serviceProvider.GetService<ITags>();
            Assert.NotNull(tags);

            var ep = serviceProvider.GetService<MetricsEndpoint>();
            Assert.NotNull(ep);
        }

        private IConfiguration GetConfiguration()
        {
            var builder = new ConfigurationBuilder();
            return builder.Build();
        }
    }
}
