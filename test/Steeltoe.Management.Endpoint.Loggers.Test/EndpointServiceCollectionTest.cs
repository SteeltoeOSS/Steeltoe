// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class EndpointServiceCollectionTest : BaseTest
    {
        [Fact]
        public void AddLoggersActuator_ThrowsOnNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddLoggersActuator(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddLoggersActuator(services2, config));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddLoggersActuator_AddsCorrectServices()
        {
            ServiceCollection services = new ServiceCollection();
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:loggers:enabled"] = "true",
                ["management:endpoints:loggers:sensitive"] = "false",
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            services.AddLoggersActuator(config);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<ILoggersOptions>();
            Assert.NotNull(options);
            var ep = serviceProvider.GetService<LoggersEndpoint>();
            Assert.NotNull(ep);
        }
    }
}
