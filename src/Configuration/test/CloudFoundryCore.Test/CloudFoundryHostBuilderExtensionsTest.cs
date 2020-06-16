// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryHostBuilderExtensionsTest
    {
        [Fact]
        [Obsolete]
        public void UseCloudFoundryHosting_Web_ThrowsIfHostBuilderNull()
        {
            // Arrange
            IWebHostBuilder webHostBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryHostBuilderExtensions.UseCloudFoundryHosting(webHostBuilder));
            Assert.Contains(nameof(webHostBuilder), ex.Message);
        }

        [Fact]
        [Obsolete]
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
        [Obsolete]
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

#if NETCOREAPP3_1
        [Fact]
        [Obsolete]
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
        [Obsolete]
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

        [Fact]
        public void HostAddCloudFoundryConfiguration_Adds()
        {
            // arrange
            var hostbuilder = new HostBuilder();

            // act
            hostbuilder.AddCloudFoundryConfiguration();
            var host = hostbuilder.Build();

            // assert
            var cfg = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
            Assert.Contains(cfg.Providers, ctype => ctype is CloudFoundryConfigurationProvider);
        }
#endif

        [Fact]
        public void WebHostAddCloudFoundryConfiguration_Adds()
        {
            // arrange
            var hostbuilder = new WebHostBuilder();
            hostbuilder.Configure(builder => { });

            // act
            hostbuilder.AddCloudFoundryConfiguration();
            var host = hostbuilder.Build();

            // assert
            var cfg = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
            Assert.Contains(cfg.Providers, ctype => ctype is CloudFoundryConfigurationProvider);
        }
    }
}
