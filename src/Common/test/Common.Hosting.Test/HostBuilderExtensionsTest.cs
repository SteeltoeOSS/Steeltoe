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
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System;
using Xunit;

namespace Steeltoe.Common.Hosting.Test
{
    public class HostBuilderExtensionsTest
    {
        [Fact]
        public void UseCloudHosting_Web_ThrowsIfHostBuilderNull()
        {
            // Arrange
            IWebHostBuilder webHostBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => HostBuilderExtensions.UseCloudHosting(webHostBuilder));
            Assert.Contains(nameof(webHostBuilder), ex.Message);
        }

        [Fact]
        public void UseCloudHosting_Default8080()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            // Act
            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            // Assert
            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:8080", addresses.Addresses);
        }

        [Fact]
        public void UseCloudHosting_MakeSureThePortIsSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", "42");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            // Act
            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            // Assert
            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:42", addresses.Addresses);
        }

        [Fact]
        public void UseCloudHosting_UsesServerPort()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SERVER_PORT", "42");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            // Act
            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            // Assert
            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:42", addresses.Addresses);

            Environment.SetEnvironmentVariable("SERVER_PORT", null);
        }

        [Fact]
        public void UseCloudHosting_UsesLocalPortSettings()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            // Act
            hostBuilder.UseCloudHosting(5000, 5001);
            var server = hostBuilder.Build();

            // Assert
            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:5000", addresses.Addresses);
            Assert.Contains("https://*:5001", addresses.Addresses);
        }

#if NETCOREAPP3_0
        [Fact]
        public void UseCloudHosting_GenericHost_Default8080()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(configure =>
                {
                    configure.UseStartup<TestServerStartupDefault>();
                    configure.UseKestrel();
                });

            // Act and Assert
            hostBuilder.UseCloudHosting();
            using var host = hostBuilder.Build();
            host.Start();
        }

        [Fact]
        public void UseCloudHosting_GenericHost_MakeSureThePortIsSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", "5042");
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(configure =>
                {
                    configure.UseStartup<TestServerStartup42>();
                    configure.UseKestrel();
                });

            // Act and Assert
            hostBuilder.UseCloudHosting();
            using var host = hostBuilder.Build();
            host.Start();
        }

        [Fact]
        public void UseCloudHosting_GenericHost_UsesLocalPortSettings()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(configure =>
                {
                    configure.UseStartup<TestServerStartupLocals>();
                    configure.UseKestrel();
                });

            // Act and Assert
            hostBuilder.UseCloudHosting(5000, 5001);
            using var host = hostBuilder.Build();
            host.Start();
        }
#endif
    }
}
