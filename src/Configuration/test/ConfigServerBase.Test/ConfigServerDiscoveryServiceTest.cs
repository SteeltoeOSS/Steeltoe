// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class ConfigServerDiscoveryServiceTest
    {
        [Fact]
        public void FindGetInstancesMethod_FindsMethod()
        {
            var values = new Dictionary<string, string>()
            {
                { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" }
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            var config = builder.Build();
            var settings = new ConfigServerClientSettings();

            var service = new ConfigServerDiscoveryService(config, settings);
            Assert.NotNull(service.FindGetInstancesMethod());
        }

        [Fact]
        public void InvokeGetInstances_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" },
                { "eureka:client:eurekaServer:retryCount", "0" }
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            var config = builder.Build();
            var settings = new ConfigServerClientSettings();

            var service = new ConfigServerDiscoveryService(config, settings);
            var method = service.FindGetInstancesMethod();

            var result = service.InvokeGetInstances(method);
            Assert.Empty(result);
        }

        [Fact]
        public void InvokeGetInstances_RetryEnabled_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "eureka:client:serviceUrl", "https://foo.bar:8761/eureka/" },
                { "eureka:client:eurekaServer:retryCount", "1" }
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            var config = builder.Build();
            var settings = new ConfigServerClientSettings()
            {
                RetryEnabled = true,
                Timeout = 10,
                RetryAttempts = 1
            };
            var service = new ConfigServerDiscoveryService(config, settings);
            var method = service.FindGetInstancesMethod();

            var result = service.InvokeGetInstances(method);
            Assert.Empty(result);
        }

        [Fact]
        public void GetConfigServerInstances_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" },
                { "eureka:client:eurekaServer:retryCount", "0" }
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            var config = builder.Build();
            var settings = new ConfigServerClientSettings() { RetryEnabled = false, Timeout = 10 };

            var service = new ConfigServerDiscoveryService(config, settings);
            var result = service.GetConfigServerInstances();
            Assert.Empty(result);
        }
    }
}
