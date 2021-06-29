// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.ThreadDump;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.CloudFoundry.Test
{
    [Obsolete]
    public class CloudFoundryHostBuilderExtensionsTest
    {
        private static readonly Dictionary<string, string> ManagementSettings = new Dictionary<string, string>()
        {
            ["management:endpoints:path"] = "/testing",
        };

        private Action<IWebHostBuilder> testServerWithRouting = builder => builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());

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

            if (Platform.IsWindows)
            {
                Assert.Single(host.Services.GetServices<ThreadDumpEndpoint_v2>());
            }
            else
            {
                Assert.Empty(host.Services.GetServices<ThreadDumpEndpoint_v2>());
            }

            if (Endpoint.HeapDump.EndpointServiceCollectionExtensions.IsHeapDumpSupported())
            {
                Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());
            }
            else
            {
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
                .ConfigureLogging(logging => logging.AddDynamicSerilog());

            // Act
            var host = hostBuilder.AddCloudFoundryActuators().Build();
            var managementOptions = host.Services.GetServices<IManagementOptions>();

            var filters = host.Services.GetServices<IStartupFilter>();

            // Assert
            Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));

            if (Platform.IsWindows)
            {
                Assert.Single(host.Services.GetServices<ThreadDumpEndpoint_v2>());
            }
            else
            {
                Assert.Empty(host.Services.GetServices<ThreadDumpEndpoint_v2>());
            }

            if (Endpoint.HeapDump.EndpointServiceCollectionExtensions.IsHeapDumpSupported())
            {
                Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());
            }
            else
            {
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
            var host = hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1).Build();
            var managementOptions = host.Services.GetServices<IManagementOptions>();

            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));

            if (Platform.IsWindows)
            {
                Assert.Single(host.Services.GetServices<ThreadDumpEndpoint>());
            }
            else
            {
                Assert.Empty(host.Services.GetServices<ThreadDumpEndpoint>());
            }

            if (Endpoint.HeapDump.EndpointServiceCollectionExtensions.IsHeapDumpSupported())
            {
                Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());
            }
            else
            {
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
                .ConfigureWebHost(testServerWithRouting)
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings));

            // Act
            var host = await hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V2).StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
            response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/info");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
            response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/httptrace");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public async Task AddCloudFoundryActuatorsV1_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(testServerWithRouting)
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings));

            // Act
            var host = await hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1).StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
            response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/info");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
            response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/trace");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddCloudFoundryActuators_IHostBuilder_Serilog()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.AddDynamicSerilog())
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings))
                .ConfigureWebHost(testServerWithRouting)
                .AddCloudFoundryActuators();

            // Act
            var host = hostBuilder.Build();
            var managementOptions = host.Services.GetServices<IManagementOptions>();

            var filters = host.Services.GetServices<IStartupFilter>();

            // Assert
            Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));

            if (Platform.IsWindows)
            {
                Assert.Single(host.Services.GetServices<ThreadDumpEndpoint_v2>());
            }
            else
            {
                Assert.Empty(host.Services.GetServices<ThreadDumpEndpoint_v2>());
            }

            if (Endpoint.HeapDump.EndpointServiceCollectionExtensions.IsHeapDumpSupported())
            {
                Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());
            }
            else
            {
                Assert.Empty(host.Services.GetServices<HeapDumpEndpoint>());
            }

            Assert.NotNull(filters);
            Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
        }
    }
}
