﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Test;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test
{
    public class ManagementHostBuilderExtensionsTest
    {
        [Fact]
        public void AddDbMigrationsActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddDbMigrationsActuator().Build();
            var managementEndpoint = host.Services.GetServices<DbMigrationsEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<DbMigrationsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddDbMigrationsActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddDbMigrationsActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<DbMigrationsEndpoint>());
        }

        [Fact]
        public void AddEnvActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddEnvActuator().Build();
            var managementEndpoint = host.Services.GetServices<EnvEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<EnvStartupFilter>(filter);
        }

        [Fact]
        public async Task AddEnvActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddEnvActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<EnvEndpoint>());
        }

        [Fact]
        public void AddHealthActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddHealthActuator().Build();
            var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<HealthStartupFilter>(filter);
        }

        [Fact]
        public void AddHealthActuator_IHostBuilder_WithTypes()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddHealthActuator(new Type[] { typeof(DownContributor) }).Build();
            var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<HealthStartupFilter>(filter);
        }

        [Fact]
        public void AddHealthActuator_IHostBuilder_WithAggregator()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddHealthActuator(new DefaultHealthAggregator(), new Type[] { typeof(DownContributor) }).Build();
            var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<HealthStartupFilter>(filter);
        }

        [Fact]
        public async Task AddHealthActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddHealthActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<HealthEndpointCore>());
        }

        [Fact]
        public void AddHeapDumpActuator_IHostBuilder()
        {
            if (Platform.IsWindows)
            {
                // Arrange
                var hostBuilder = new HostBuilder();

                // Act
                var host = hostBuilder.AddHeapDumpActuator().Build();
                var managementEndpoint = host.Services.GetServices<HeapDumpEndpoint>();
                var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

                // Assert
                Assert.Single(managementEndpoint);
                Assert.NotNull(filter);
                Assert.IsType<HeapDumpStartupFilter>(filter);
            }
        }

        [Fact]
        public async Task AddHeapDumpActuator_IHostBuilder_IStartupFilterFires()
        {
            if (Platform.IsWindows)
            {
                // Arrange
                var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

                // Act
                var host = await hostBuilder.AddHeapDumpActuator().StartAsync();

                // Assert general success...
                //   not sure how to actually validate the StartupFilter worked,
                //   but debug through and you'll see it. Also the code coverage report should provide validation
                Assert.NotNull(host.Services.GetService<HeapDumpEndpoint>());
            }
        }

        [Fact]
        public void AddHypermediaActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddHypermediaActuator().Build();
            var managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<HypermediaStartupFilter>(filter);
        }

        [Fact]
        public async Task AddHypermediaActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddHypermediaActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<ActuatorEndpoint>());
        }

        [Fact]
        public void AddInfoActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddInfoActuator().Build();
            var managementEndpoint = host.Services.GetServices<InfoEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<InfoStartupFilter>(filter);
        }

        [Fact]
        public void AddInfoActuator_IHostBuilder_WithTypes()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddInfoActuator(new IInfoContributor[] { new AppSettingsInfoContributor(new ConfigurationBuilder().Build()) }).Build();
            var managementEndpoint = host.Services.GetServices<InfoEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<InfoStartupFilter>(filter);
        }

        [Fact]
        public async Task AddInfoActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddInfoActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<InfoEndpoint>());
        }

        [Fact]
        public void AddLoggersActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddLoggersActuator().Build();
            var managementEndpoint = host.Services.GetServices<LoggersEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<LoggersStartupFilter>(filter);
        }

        [Fact]
        public async Task AddLoggersActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddLoggersActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<LoggersEndpoint>());
        }

        [Fact]
        public void AddMappingsActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddMappingsActuator().Build();
            var managementEndpoint = host.Services.GetServices<IRouteMappings>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<MappingsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddMappingsActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddMappingsActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<IRouteMappings>());
        }

        [Fact]
        public void AddMetricsActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddMetricsActuator().Build();
            var managementEndpoint = host.Services.GetServices<MetricsEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<MetricsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddMetricsActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddMetricsActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<MetricsEndpoint>());
        }

        [Fact]
        public void AddRefreshActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddRefreshActuator().Build();
            var managementEndpoint = host.Services.GetServices<RefreshEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<RefreshStartupFilter>(filter);
        }

        [Fact]
        public async Task AddRefreshActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddRefreshActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<RefreshEndpoint>());
        }

        [Fact]
        public void AddThreadDumpActuator_IHostBuilder()
        {
            if (Platform.IsWindows)
            {
                // Arrange
                var hostBuilder = new HostBuilder();

                // Act
                var host = hostBuilder.AddThreadDumpActuator().Build();
                var managementEndpoint = host.Services.GetServices<ThreadDumpEndpoint_v2>();
                var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

                // Assert
                Assert.Single(managementEndpoint);
                Assert.NotNull(filter);
                Assert.IsType<ThreadDumpStartupFilter>(filter);
            }
        }

        [Fact]
        public async Task AddThreadDumpActuator_IHostBuilder_IStartupFilterFires()
        {
            if (Platform.IsWindows)
            {
                // Arrange
                var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

                // Act
                var host = await hostBuilder.AddThreadDumpActuator().StartAsync();

                // Assert general success...
                //   not sure how to actually validate the StartupFilter worked,
                //   but debug through and you'll see it. Also the code coverage report should provide validation
                Assert.NotNull(host.Services.GetService<ThreadDumpEndpoint_v2>());
            }
        }

        [Fact]
        public void AddTraceActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddTraceActuator().Build();
            var managementEndpoint = host.Services.GetServices<HttpTraceEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<TraceStartupFilter>(filter);
        }

        [Fact]
        public async Task AddTraceActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddTraceActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<HttpTraceEndpoint>());
        }

        [Fact]
        public void AddCloudFoundryActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddCloudFoundryActuator().Build();
            var managementEndpoint = host.Services.GetServices<CloudFoundryEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<CloudFoundryActuatorStartupFilter>(filter);
        }

        [Fact]
        public async Task AddCloudFoundryActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddCloudFoundryActuator().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.NotNull(host.Services.GetService<CloudFoundryEndpoint>());
        }

        private Action<IWebHostBuilder> testServerWithRouting = builder => builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());
    }
}
