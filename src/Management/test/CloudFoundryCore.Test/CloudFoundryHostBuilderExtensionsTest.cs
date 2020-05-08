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
        private static readonly Dictionary<string, string> ManagementSettings = new Dictionary<string, string>()
        {
            ["management:endpoints:path"] = "/testing",
        };

        [Fact]
        public void AddCloudFoundryActuators_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings)).Configure(configureApp => { });

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
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings))
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
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings));

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

        [Fact]
        public async Task AddCloudFoundryActuators_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(c => c.UseTestServer().Configure(app => { }))
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings));

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
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings))
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
    }
}
