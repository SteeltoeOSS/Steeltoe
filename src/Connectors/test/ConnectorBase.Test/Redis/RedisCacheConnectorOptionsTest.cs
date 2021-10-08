// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.Redis.Test
{
    [Collection("Redis")]
    public class RedisCacheConnectorOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => new RedisCacheConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["redis:client:host"] = "localhost",
                ["redis:client:port"] = "1234",
                ["redis:client:password"] = "password",
                ["redis:client:instancename"] = "instanceid",
                ["redis:client:allowAdmin"] = "true",
                ["redis:client:clientName"] = "foobar",
                ["redis:client:connectRetry"] = "100",
                ["redis:client:connectTimeout"] = "100",
                ["redis:client:abortOnConnectFail"] = "true",
                ["redis:client:keepAlive"] = "100",
                ["redis:client:resolveDns"] = "true",
                ["redis:client:serviceName"] = "foobar",
                ["redis:client:ssl"] = "true",
                ["redis:client:sslHost"] = "foobar",
                ["redis:client:writeBuffer"] = "100",
                ["redis:client:tieBreaker"] = "foobar",
                ["redis:client:syncTimeout"] = "100"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new RedisCacheConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Host);
            Assert.Equal(1234, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("instanceid", sconfig.InstanceName);

            Assert.True(sconfig.AllowAdmin);
            Assert.Equal("foobar", sconfig.ClientName);
            Assert.Equal(100, sconfig.ConnectRetry);
            Assert.Equal(100, sconfig.ConnectTimeout);
            Assert.True(sconfig.AbortOnConnectFail);
            Assert.Equal(100, sconfig.KeepAlive);
            Assert.True(sconfig.ResolveDns);
            Assert.Equal("foobar", sconfig.ServiceName);
            Assert.True(sconfig.Ssl);
            Assert.Equal("foobar", sconfig.SslHost);
            Assert.Equal("foobar", sconfig.TieBreaker);
            Assert.Equal(100, sconfig.WriteBuffer);
            Assert.Equal(100, sconfig.SyncTimeout);

            Assert.Null(sconfig.ConnectionString);
        }

        [Fact]
        public void ConnectionString_Returned_AsConfigured()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["redis:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new RedisCacheConnectorOptions(config);

            Assert.Equal(appsettings["redis:client:ConnectionString"], sconfig.ToString());
        }

        [Fact]
        public void ConnectionString_Overridden_By_CloudFoundryConfig()
        {
            // simulate an appsettings file
            var appsettings = new Dictionary<string, string>()
            {
                ["redis:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };

            // add environment variables as Cloud Foundry would
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.SingleServerVCAP);

            // add settings to config
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            var sconfig = new RedisCacheConnectorOptions(config);

            Assert.NotEqual(appsettings["redis:client:ConnectionString"], sconfig.ToString());
        }
    }
}
