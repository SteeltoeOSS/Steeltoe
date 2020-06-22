﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Xunit;

namespace Steeltoe.Common.Discovery.Test
{
    public class ConfigurationServiceInstanceProviderServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddConfigurationDiscoveryClient_AddsClientWithOptions()
        {
            // arrange
            var appsettings = @"
{
    ""discovery"": {
        ""services"": [
            { ""serviceId"": ""fruitService"", ""host"": ""fruitball"", ""port"": 443, ""isSecure"": true },
            { ""serviceId"": ""fruitService"", ""host"": ""fruitballer"", ""port"": 8081 },
            { ""serviceId"": ""vegetableService"", ""host"": ""vegemite"", ""port"": 443, ""isSecure"": true },
            { ""serviceId"": ""vegetableService"", ""host"": ""carrot"", ""port"": 8081 },
        ]
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            var cbuilder = new ConfigurationBuilder();
            cbuilder.SetBasePath(directory);
            cbuilder.AddJsonFile(fileName);
            var services = new ServiceCollection();

            // act
            services.AddConfigurationDiscoveryClient(cbuilder.Build());
            var serviceProvider = services.BuildServiceProvider();

            // by getting the provider, we're confirming that the options are also available in the container
            var serviceInstanceProvider = serviceProvider.GetRequiredService(typeof(IServiceInstanceProvider)) as IServiceInstanceProvider;

            // assert
            Assert.NotNull(serviceInstanceProvider);
            Assert.IsType<ConfigurationServiceInstanceProvider>(serviceInstanceProvider);
            Assert.Equal(2, serviceInstanceProvider.Services.Count);
            Assert.Equal(2, serviceInstanceProvider.GetInstances("fruitService").Count);
            Assert.Equal(2, serviceInstanceProvider.GetInstances("vegetableService").Count);
        }
    }
}
