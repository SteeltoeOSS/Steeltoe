// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.ThreadDump.Test
{
    public class EndpointServiceCollectionTest : BaseTest
    {
        [Fact]
        public void AddThreadDumpActuator_ThrowsOnNulls()
        {
            const IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            const IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddThreadDumpActuator(config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => services2.AddThreadDumpActuator(config));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddThreadDumpActuator_AddsCorrectServices()
        {
            var services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:dump:enabled"] = "false",
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();

            services.AddThreadDumpActuator(config);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IThreadDumpOptions>();
            Assert.NotNull(options);
            var repo = serviceProvider.GetService<IThreadDumper>();
            Assert.NotNull(repo);
            var ep = serviceProvider.GetService<ThreadDumpEndpoint_v2>();
            Assert.NotNull(ep);
        }
    }
}
