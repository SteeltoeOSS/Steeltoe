// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Redis.Test;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading;
using Xunit;

namespace Steeltoe.Connector.Test
{
    public class ConnectionStringConfigurationProviderTest
    {
        [Fact]
        public void ConstructorThrowsOnNullProviders()
        {
            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConnectionStringConfigurationProvider(null));
            Assert.Equal("providers", ex.ParamName);
        }

        [Fact]
        public void TryGetReadsBasicConnectorConnectionString()
        {
            var appSettings = new Dictionary<string, string> { { "redis:client:host", "testHost" } };
            var provider = new ConnectionStringConfigurationProvider(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build().Providers);
            Assert.True(provider.TryGet("connectionstrings:redis", out var connectionString));
            Assert.Equal("testHost:6379,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionString);
        }

        [Fact]
        public void TryGetReadsServiceBindingConnectionString()
        {
            var provider = new ConnectionStringConfigurationProvider(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(RedisCacheTestHelpers.SingleServerAsDictionary)
                    .Build()
                    .Providers);
            Assert.True(provider.TryGet("connectionstrings:myRedisService", out var connectionString));
            Assert.Equal("192.168.0.103:60287,password=133de7c8-9f3a-4df1-8a10-676ba7ddaa10,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionString);
        }

        [Fact]
        public void TryGetReturnsFalseWhenNotFound()
        {
            var provider = new ConnectionStringConfigurationProvider(new ConfigurationBuilder().Build().Providers);
            Assert.False(provider.TryGet("connectionstrings:myRedisService", out _));
        }

        [Fact]
        public void ProviderSeesConfigUpdates()
        {
            var appSettings1 = @"{ ""redis"": { ""client"": { ""host"": ""testHost"" } } }";
            var appSettings2 = @"{ ""redis"": { ""client"": { ""host"": ""updatedTestHost"" } } }";
            var path = TestHelpers.CreateTempFile(appSettings1);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            var baseProviders = new ConfigurationBuilder().SetBasePath(directory).AddJsonFile(fileName, false, true).Build().Providers;
            var provider = new ConnectionStringConfigurationProvider(baseProviders);
            var token = provider.GetReloadToken();

            Assert.True(provider.TryGet("connectionstrings:redis", out var connectionString));
            Assert.Equal("testHost:6379,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionString);

            File.WriteAllText(path, appSettings2);
            Thread.Sleep(2000);

            Assert.True(token.HasChanged);
            Assert.True(provider.TryGet("connectionstrings:redis", out connectionString));
            Assert.Equal("updatedTestHost:6379,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionString);
        }
    }
}
