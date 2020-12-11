// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
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
        public void UseCloudHosting_ReadsTyePorts()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", "80;443");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            // Act
            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            // Assert
            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:80", addresses.Addresses);
            Assert.Contains("https://*:443", addresses.Addresses);
        }

        [Fact]
        public void UseCloudHosting_SeesTyePortsAndUsesAspNetCoreURL()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://*:80;https://*:443");
            Environment.SetEnvironmentVariable("PORT", "88;4443");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            // Act
            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            // Assert
            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:80", addresses.Addresses);
            Assert.Contains("https://*:443", addresses.Addresses);
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

#if NETCOREAPP3_1
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
