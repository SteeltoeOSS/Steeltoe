// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundryCore.Test
{
    public class CloudFoundryHostBuilderExtensionsTest
    {
        [Fact]
        public void WebHostAddCloudConfigurationFoundry_Adds()
        {
            // arrange
            var hostbuilder = new WebHostBuilder();
            hostbuilder.Configure(builder => { });

            // act
            hostbuilder.AddCloudFoundryConfiguration();
            var host = hostbuilder.Build();

            // assert
            var instanceInfo = host.Services.GetApplicationInstanceInfo();
            Assert.IsAssignableFrom<CloudFoundryApplicationOptions>(instanceInfo);
            var cfg = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
            Assert.Contains(cfg.Providers, ctype => ctype is CloudFoundryConfigurationProvider);
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
            var instanceInfo = host.Services.GetApplicationInstanceInfo();
            Assert.IsAssignableFrom<CloudFoundryApplicationOptions>(instanceInfo);
            var cfg = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
            Assert.Contains(cfg.Providers, ctype => ctype is CloudFoundryConfigurationProvider);
        }

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
