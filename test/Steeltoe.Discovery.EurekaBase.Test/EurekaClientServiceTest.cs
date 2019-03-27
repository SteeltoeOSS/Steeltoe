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

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaClientServiceTest
    {
        [Fact]
        public void ConfigureClientOptions_ConfiguresCorrectly()
        {
            var values = new Dictionary<string, string>()
            {
                { "eureka:client:serviceUrl", "http://foo.bar:8761/eureka/" }
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            var config = builder.Build();

            var options = EurekaClientService.ConfigureClientOptions(config);
            Assert.Equal("http://foo.bar:8761/eureka/", options.EurekaServerServiceUrls);
            Assert.True(options.ShouldFetchRegistry);
            Assert.False(options.ShouldRegisterWithEureka);
        }

        [Fact]
        public void GetLookupClient_ConfiguresClient()
        {
            var values = new Dictionary<string, string>()
            {
                { "eureka:client:serviceUrl", "http://foo.bar:8761/eureka/" }
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            var config = builder.Build();

            var options = EurekaClientService.ConfigureClientOptions(config);
            var lookupClient = EurekaClientService.GetLookupClient(options, null);
            Assert.NotNull(lookupClient);
            Assert.NotNull(lookupClient.ClientConfig);
            Assert.Equal("http://foo.bar:8761/eureka/", lookupClient.ClientConfig.EurekaServerServiceUrls);
            Assert.True(lookupClient.ClientConfig.ShouldFetchRegistry);
            Assert.False(lookupClient.ClientConfig.ShouldRegisterWithEureka);
            Assert.Null(lookupClient.HeartBeatTimer);
            Assert.Null(lookupClient.CacheRefreshTimer);
            Assert.NotNull(lookupClient.HttpClient);
        }

        [Fact]
        public void GetInstances_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "eureka:client:serviceUrl", "http://foo.bar:8761/eureka/" }
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            var config = builder.Build();
            var result = EurekaClientService.GetInstances(config, "testService", null);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetServices_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "eureka:client:serviceUrl", "http://foo.bar:8761/eureka/" }
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            var config = builder.Build();
            var result = EurekaClientService.GetServices(config, null);
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
