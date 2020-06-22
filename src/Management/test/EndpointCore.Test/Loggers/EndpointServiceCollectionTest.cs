﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
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
            // arrange
            ServiceCollection services = new ServiceCollection();
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:loggers:enabled"] = "true"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(config);
                builder.AddDynamicConsole();
            });
            services.AddLoggersActuator(config);
            var serviceProvider = services.BuildServiceProvider();

            // act
            var options = serviceProvider.GetService<ILoggersOptions>();
            var ep = serviceProvider.GetService<LoggersEndpoint>();

            // assert
            Assert.NotNull(options);
            Assert.NotNull(ep);
        }
    }
}
