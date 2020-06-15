// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
#if !NETCOREAPP3_1
using Microsoft.AspNetCore.Builder.Internal;
#endif
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using Xunit;

namespace Steeltoe.Management.CloudFoundry.Test
{
    public class CloudFoundryServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddCloudFoundryActuators_ThrowsOnNull_Services()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.AddCloudFoundryActuators(null, config));
            Assert.Equal("services", ex.ParamName);
        }

        [Fact]
        public void AddCloudFoundryActuators_ThrowsOnNull_Config()
        {
            // Arrange
            IServiceCollection services2 = new ServiceCollection();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.AddCloudFoundryActuators(services2, null));

            // Assert
            Assert.Equal("config", ex.ParamName);
        }

        [Fact]
        public void AddCloudFoundryActuators_ConfiguresCorsDefaults()
        {
            // arrange
            var hostBuilder = new WebHostBuilder().Configure(config => { });

            // act
            var host = hostBuilder.ConfigureServices((context, services) => services.AddCloudFoundryActuators(context.Configuration)).Build();
            var options = new ApplicationBuilder(host.Services).ApplicationServices.GetService(typeof(IOptions<CorsOptions>)) as IOptions<CorsOptions>;

            // assert
            Assert.NotNull(options);
            var policy = options.Value.GetPolicy("SteeltoeManagement");
            Assert.True(policy.IsOriginAllowed("*"));
            Assert.Contains(policy.Methods, m => m.Equals("GET"));
            Assert.Contains(policy.Methods, m => m.Equals("POST"));
        }

        [Fact]
        public void AddCloudFoundryActuators_ConfiguresCorsCustom()
        {
            // arrange
            Action<CorsPolicyBuilder> customCors = (myPolicy) => myPolicy.WithOrigins("http://google.com");
            var hostBuilder = new WebHostBuilder().Configure(config => { });

            // act
            var host = hostBuilder.ConfigureServices((context, services) => services.AddCloudFoundryActuators(context.Configuration, customCors)).Build();
            var options = new ApplicationBuilder(host.Services)
                                .ApplicationServices.GetService(typeof(IOptions<CorsOptions>)) as IOptions<CorsOptions>;

            // assert
            Assert.NotNull(options);
            var policy = options.Value.GetPolicy("SteeltoeManagement");
            Assert.True(policy.IsOriginAllowed("http://google.com"));
            Assert.False(policy.IsOriginAllowed("http://bing.com"));
            Assert.False(policy.IsOriginAllowed("*"));
            Assert.Contains(policy.Methods, m => m.Equals("GET"));
            Assert.Contains(policy.Methods, m => m.Equals("POST"));
        }
    }
}
