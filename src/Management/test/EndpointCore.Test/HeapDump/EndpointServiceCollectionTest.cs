// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.HeapDump.Test
{
    public class EndpointServiceCollectionTest : BaseTest
    {
        [Fact]
        public void AddHeapDumpActuator_ThrowsOnNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddHeapDumpActuator(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddHeapDumpActuator(services2, config));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddHeapDumpActuator_AddsCorrectServices()
        {
            if (EndpointServiceCollectionExtensions.IsHeapDumpSupported())
            {
                var services = new ServiceCollection();
                var appSettings = new Dictionary<string, string>()
                {
                    ["management:endpoints:enabled"] = "false",
                    ["management:endpoints:path"] = "/cloudfoundryapplication",
                    ["management:endpoints:heapdump:enabled"] = "false",
                    ["management:endpoints:heapdump:HeapDumpType"] = "Normal"
                };
                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddInMemoryCollection(appSettings);
                var config = configurationBuilder.Build();

                services.AddHeapDumpActuator(config);

                var serviceProvider = services.BuildServiceProvider();
                var options = serviceProvider.GetService<IHeapDumpOptions>();
                Assert.NotNull(options);
                Assert.Equal("Normal", options.HeapDumpType);
                var repo = serviceProvider.GetService<IHeapDumper>();
                Assert.NotNull(repo);
                var ep = serviceProvider.GetService<HeapDumpEndpoint>();
                Assert.NotNull(ep);
            }
        }
    }
}
