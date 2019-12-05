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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    [Obsolete]
    public class CloudFoundryHostBuilderExtensionsTest
    {
        [Fact]
        public void UseCloudFoundryHosting_Web_ThrowsIfHostBuilderNull()
        {
            // Arrange
            IWebHostBuilder webHostBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryHostBuilderExtensions.UseCloudFoundryHosting(webHostBuilder));
            Assert.Contains(nameof(webHostBuilder), ex.Message);
        }

        [Fact]
        public void UseCloudFoundryHosting_DoNotSetUrlsIfNull()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", null);
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            // Act
            hostBuilder.UseCloudFoundryHosting();
            var server = new TestServer(hostBuilder);

            // Assert
            var addresses = server.Host.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Null(addresses);
        }

        [Fact]
        public void UseCloudFoundryHosting_MakeSureThePortIsSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", "42");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            // Act
            hostBuilder.UseCloudFoundryHosting();
            var server = hostBuilder.Build();

            // Assert
            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:42", addresses.Addresses);
        }

#if NETCOREAPP3_0
        [Fact]
        public void UseCloudFoundryHosting_GenericHost_DoNotSetUrlsIfNull()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", null);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(configure =>
                {
                    configure.UseStartup<TestServerStartup>();
                    configure.UseKestrel();
                });

            // Act and Assert
            hostBuilder.UseCloudFoundryHosting();
            using var host = hostBuilder.Build();
            host.Start();
        }

        [Fact]
        public void UseCloudFoundryHosting_GenericHost_MakeSureThePortIsSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", "5042");
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(configure =>
                {
                    configure.UseStartup<TestServerStartup>();
                    configure.UseKestrel();
                });

            // Act and Assert
            hostBuilder.UseCloudFoundryHosting();
            using var host = hostBuilder.Build();
            host.Start();
        }
#endif
    }
}
