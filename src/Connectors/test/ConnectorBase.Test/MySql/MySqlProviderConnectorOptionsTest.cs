// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.MySql.Test
{
    public class MySqlProviderConnectorOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => new MySqlProviderConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>
            {
                ["mysql:client:server"] = "localhost",
                ["mysql:client:port"] = "1234",
                ["mysql:client:PersistSecurityInfo"] = "true",
                ["mysql:client:password"] = "password",
                ["mysql:client:username"] = "username"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new MySqlProviderConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Server);
            Assert.Equal(1234, sconfig.Port);
            Assert.True(sconfig.PersistSecurityInfo);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("username", sconfig.Username);
            Assert.Null(sconfig.ConnectionString);
        }

        [Fact]
        public void ConnectionString_Returned_AsConfigured()
        {
            var appsettings = new Dictionary<string, string>
            {
                ["mysql:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new MySqlProviderConnectorOptions(config);

            Assert.Equal(appsettings["mysql:client:ConnectionString"], sconfig.ToString());
        }

        [Fact]
        public void ConnectionString_Overridden_By_CloudFoundryConfig()
        {
            // simulate an appsettings file
            var appsettings = new Dictionary<string, string>
            {
                ["mysql:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };

            // add environment variables as Cloud Foundry would
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerVCAP);

            // add settings to config
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            var sconfig = new MySqlProviderConnectorOptions(config);

            Assert.NotEqual(appsettings["mysql:client:ConnectionString"], sconfig.ToString());

            // NOTE: for this test, we don't expect VCAP_SERVICES to be parsed,
            //          this test is only here to demonstrate that when a binding is present,
            //          a pre-supplied connectionString is not returned
        }
    }
}
