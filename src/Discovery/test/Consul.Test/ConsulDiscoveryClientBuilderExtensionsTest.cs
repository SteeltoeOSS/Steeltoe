// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Consul;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test
{
    public class ConsulDiscoveryClientBuilderExtensionsTest
    {
        private Dictionary<string, string> appsettings = new ()
        {
                { "spring:application:name", "myName" },
                { "spring:cloud:inet:defaulthostname", "fromtest" },
                { "spring:cloud:inet:skipReverseDnsLookup", "true" },
                { "consul:discovery:useNetUtils", "true" },
                { "consul:discovery:register", "false" },
                { "consul:discovery:deregister", "false" },
                { "consul:host", "http://testhost:8500" }
            };

        [Fact]
        public void UseConsulUsesConsul()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(config);
            serviceCollection.AddServiceDiscovery(options => options.UseConsul());

            var provider = serviceCollection.BuildServiceProvider();

            var client = provider.GetRequiredService<IDiscoveryClient>();
            Assert.NotNull(client);
            Assert.IsType<Consul.Discovery.ConsulDiscoveryClient>(client);
        }
    }
}
