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
            IWebHostBuilder webHostBuilder = null;

            var ex = Assert.Throws<ArgumentNullException>(() => HostBuilderExtensions.UseCloudHosting(webHostBuilder));
            Assert.Contains(nameof(webHostBuilder), ex.Message);
        }

        [Fact]
        public void UseCloudHosting_Default8080()
        {
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:8080", addresses.Addresses);
        }

        [Fact]
        public void UseCloudHosting_MakeSureThePortIsSet()
        {
            Environment.SetEnvironmentVariable("PORT", "42");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:42", addresses.Addresses);
        }

        [Fact]
        public void UseCloudHosting_ReadsTyePorts()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
            Environment.SetEnvironmentVariable("PORT", "80;443");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:80", addresses.Addresses);
            Assert.Contains("https://*:443", addresses.Addresses);
        }

        [Fact]
        public void UseCloudHosting_SeesTyePortsAndUsesAspNetCoreURL()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://*:80;https://*:443");
            Environment.SetEnvironmentVariable("PORT", "88;4443");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:80", addresses.Addresses);
            Assert.Contains("https://*:443", addresses.Addresses);
        }

        [Fact]
        public void UseCloudHosting_UsesServerPort()
        {
            Environment.SetEnvironmentVariable("SERVER_PORT", "42");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:42", addresses.Addresses);

            Environment.SetEnvironmentVariable("SERVER_PORT", null);
        }

        [Fact]
        public void UseCloudHosting_UsesLocalPortSettings()
        {
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            hostBuilder.UseCloudHosting(5000, 5001);
            var server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:5000", addresses.Addresses);
            Assert.Contains("https://*:5001", addresses.Addresses);
        }

        [Fact]
        public void UseCloudHosting_GenericHost_Default8080()
        {
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(configure =>
                {
                    configure.UseStartup<TestServerStartupDefault>();
                    configure.UseKestrel();
                });

            hostBuilder.UseCloudHosting();
            using var host = hostBuilder.Build();
            host.Start();
        }

        [Fact]
        public void UseCloudHosting_GenericHost_MakeSureThePortIsSet()
        {
            Environment.SetEnvironmentVariable("PORT", "5042");
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(configure =>
                {
                    configure.UseStartup<TestServerStartup42>();
                    configure.UseKestrel();
                });

            hostBuilder.UseCloudHosting();
            using var host = hostBuilder.Build();
            host.Start();
        }

        [Fact]
#if NET6_0
        [Trait("Category", "SkipOnMacOS")] // for .NET 5, this test produces an admin prompt on OSX
#endif
        public void UseCloudHosting_GenericHost_UsesLocalPortSettings()
        {
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(configure =>
                {
                    configure.UseStartup<TestServerStartupLocals>();
                    configure.UseKestrel();
                });

            hostBuilder.UseCloudHosting(5001, 5002);
            using var host = hostBuilder.Build();
            host.Start();
        }
    }
}
