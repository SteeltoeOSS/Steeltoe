﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Common.Discovery.Test
{
    public class ConfigurationServiceInstanceProviderTest
    {
        [Fact]
        public void Returns_ConfiguredServices()
        {
            // arrange
            var services = new List<ConfigurationServiceInstance>
            {
                new ConfigurationServiceInstance { ServiceId = "fruitService", Host = "fruitball", Port = 443, IsSecure = true },
                new ConfigurationServiceInstance { ServiceId = "fruitService", Host = "fruitballer", Port = 8081 },
                new ConfigurationServiceInstance { ServiceId = "fruitService", Host = "fruitballerz", Port = 8082 },
                new ConfigurationServiceInstance { ServiceId = "vegetableService", Host = "vegemite", Port = 443, IsSecure = true },
                new ConfigurationServiceInstance { ServiceId = "vegetableService", Host = "carrot", Port = 8081 },
                new ConfigurationServiceInstance { ServiceId = "vegetableService", Host = "beet", Port = 8082 },
            };
            var serviceOptions = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(services);

            // act
            var provider = new ConfigurationServiceInstanceProvider(serviceOptions);

            // assert
            Assert.Equal(3, provider.GetInstances("fruitService").Count);
            Assert.Equal(3, provider.GetInstances("vegetableService").Count);
            Assert.Equal(2, provider.Services.Count);
        }

        [Fact]
        public void ReceivesUpdatesTo_ConfiguredServices()
        {
            // arrange
            var services = new List<ConfigurationServiceInstance>
            {
                new ConfigurationServiceInstance { ServiceId = "fruitService", Host = "fruitball", Port = 443, IsSecure = true },
            };
            var serviceOptions = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(services);
            var provider = new ConfigurationServiceInstanceProvider(serviceOptions);
            Assert.Single(provider.GetInstances("fruitService"));
            Assert.Equal("fruitball", provider.GetInstances("fruitService").First().Host);

            // act
            services.First().Host = "updatedValue";

            // assert
            Assert.Equal("updatedValue", provider.GetInstances("fruitService").First().Host);
        }
    }
}
