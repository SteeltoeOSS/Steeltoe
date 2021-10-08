// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Mappings.Test
{
    public class EndpointServiceCollectionTest : BaseTest
    {
        [Fact]
        public void AddMappingsActuator_ThrowsOnNulls()
        {
            IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddMappingsActuator(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddMappingsActuator(services2, config));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddMappingsActuator_AddsCorrectServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(HostingHelpers.GetHostingEnvironment());

            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();
            services.AddSingleton<IConfiguration>(config);

            services.AddMappingsActuator(config);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IMappingsOptions>();
            Assert.NotNull(options);

            var routeMappings = serviceProvider.GetService<IRouteMappings>();
            Assert.NotNull(routeMappings);
        }
    }
}
