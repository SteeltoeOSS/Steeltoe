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

using Microsoft.AspNetCore.Builder;
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
            static void CustomCors(CorsPolicyBuilder myPolicy) => myPolicy.WithOrigins("http://google.com");
            var hostBuilder = new WebHostBuilder().Configure(config => { });

            // act
            var host = hostBuilder.ConfigureServices((context, services) => services.AddCloudFoundryActuators(context.Configuration, CustomCors)).Build();
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
