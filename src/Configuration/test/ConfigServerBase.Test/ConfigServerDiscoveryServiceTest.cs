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
