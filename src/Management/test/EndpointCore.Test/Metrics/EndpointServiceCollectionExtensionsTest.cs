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

#if !NETCOREAPP3_0
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if NETCOREAPP3_0
using Microsoft.Extensions.Hosting.Internal;
#endif
using Microsoft.Extensions.FileProviders;
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
            ServiceCollection services = new ServiceCollection();
            var config = GetConfiguration();

            services.AddOptions();
            services.AddLogging();
#if NETCOREAPP3_0
            services.AddSingleton<IHostEnvironment>(new TestHost());
#else
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IHostingEnvironment>(new TestHost());
#endif
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
            ConfigurationBuilder builder = new ConfigurationBuilder();
            return builder.Build();
        }

#if NETCOREAPP3_0
        private class TestHost : IHostEnvironment
#else
        private class TestHost : Microsoft.AspNetCore.Hosting.IHostingEnvironment
#endif
        {
            public string EnvironmentName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public string ApplicationName { get => "foobar"; set => throw new NotImplementedException(); }

            public string WebRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IFileProvider WebRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public string ContentRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }
    }
}
