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
    'discovery': {
        'services': [
            { 'serviceId': 'fruitService', 'host': 'fruitball', 'port': 443, 'isSecure': true },
            { 'serviceId': 'fruitService', 'host': 'fruitballer', 'port': 8081 },
            { 'serviceId': 'vegetableService', 'host': 'vegemite', 'port': 443, 'isSecure': true },
            { 'serviceId': 'vegetableService', 'host': 'carrot', 'port': 8081 },
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
