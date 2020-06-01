// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Logging.SerilogDynamicLogger;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Hypermedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.CloudFoundry.Test
{
    public class CloudFoundryHostBuilderExtensionsTest
    {
        private static Dictionary<string, string> managementSettings = new Dictionary<string, string>()
        {
            ["management:endpoints:path"] = "/testing",
        };

        [Fact]
        public void AddCloudFoundryActuators_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(managementSettings)).Configure(configureApp => { });

            // Act
            var host = hostBuilder.AddCloudFoundryActuators().Build();
            var managementOptions = host.Services.GetServices<IManagementOptions>();

            var filters = host.Services.GetServices<IStartupFilter>();

            // Assert
            Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Assert.Single(host.Services.GetServices<ThreadDumpEndpoint>());
                Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());
            }
            else
            {
                Assert.Empty(host.Services.GetServices<ThreadDumpEndpoint>());
                Assert.Empty(host.Services.GetServices<HeapDumpEndpoint>());
            }

            Assert.NotNull(filters);
            Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
        }

        [Fact]
        public void AddCloudFoundryActuators_IWebHostBuilder_Serilog()
        {
            // Arrange
            var hostBuilder = WebHost.CreateDefaultBuilder()
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(managementSettings))
                .Configure(configureApp => { })
                .ConfigureLogging(logging => logging.AddSerilogDynamicConsole());

            // Act
            var host = hostBuilder.AddCloudFoundryActuators().Build();
            var managementOptions = host.Services.GetServices<IManagementOptions>();

            var filters = host.Services.GetServices<IStartupFilter>();

            // Assert
            Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Assert.Single(host.Services.GetServices<ThreadDumpEndpoint>());
                Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());
            }
            else
            {
                Assert.Empty(host.Services.GetServices<ThreadDumpEndpoint>());
                Assert.Empty(host.Services.GetServices<HeapDumpEndpoint>());
            }

            Assert.NotNull(filters);
            Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
        }

        [Fact]
        public void AddCloudFoundryActuators_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(managementSettings));

            // Act
            var host = hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1, ActuatorContext.CloudFoundry).Build();
            var managementOptions = host.Services.GetServices<IManagementOptions>();

            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Assert.Single(host.Services.GetServices<ThreadDumpEndpoint>());
                Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());
            }
            else
            {
                Assert.Empty(host.Services.GetServices<ThreadDumpEndpoint>());
                Assert.Empty(host.Services.GetServices<HeapDumpEndpoint>());
            }

            Assert.NotNull(filter);
            Assert.IsType<CloudFoundryActuatorsStartupFilter>(filter);
        }

#if NETCOREAPP3_0
        [Fact]
        public async Task AddCloudFoundryActuators_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(c => c.UseTestServer().Configure(app => { }))
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(managementSettings));

            // Act
            var host = await hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1, ActuatorContext.ActuatorAndCloudFoundry).StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.True(true);
        }

        [Fact]
        public void AddCloudFoundryActuators_IHostBuilder_Serilog()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.AddSerilogDynamicConsole())
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(managementSettings))
                .ConfigureWebHost(configureApp => configureApp.UseTestServer())
                .AddCloudFoundryActuators();

            // Act
            var host = hostBuilder.Build();
            var managementOptions = host.Services.GetServices<IManagementOptions>();

            var filters = host.Services.GetServices<IStartupFilter>();

            // Assert
            Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Assert.Single(host.Services.GetServices<ThreadDumpEndpoint>());
                Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());
            }
            else
            {
                Assert.Empty(host.Services.GetServices<ThreadDumpEndpoint>());
                Assert.Empty(host.Services.GetServices<HeapDumpEndpoint>());
            }

            Assert.NotNull(filters);
            Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
        }
#endif
    }
}
