// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.CosmosDb.Test
{
    public class CosmosDbConnectorOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => new CosmosDbConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["cosmosdb:client:host"] = "https://localhost:443",
                ["cosmosdb:client:masterkey"] = "masterKey",
                ["cosmosdb:client:readonlykey"] = "readOnlyKey",
                ["cosmosdb:client:databaseId"] = "databaseId",
                ["cosmosdb:client:databaseLink"] = "databaseLink"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new CosmosDbConnectorOptions(config);
            Assert.Equal("https://localhost:443", sconfig.Host);
            Assert.Equal("masterKey", sconfig.MasterKey);
            Assert.Equal("readOnlyKey", sconfig.ReadOnlyKey);
            Assert.Equal("databaseId", sconfig.DatabaseId);
            Assert.Equal("databaseLink", sconfig.DatabaseLink);
            Assert.Null(sconfig.ConnectionString);
        }

        [Fact]
        public void ConnectionString_Returned_AsConfigured()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", string.Empty);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", string.Empty);

            var appsettings = new Dictionary<string, string>()
            {
                ["cosmosdb:client:ConnectionString"] = "notEvenValidConnectionString-iHopeYouKnowBestWhatWorksForYou!"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new CosmosDbConnectorOptions(config);

            Assert.Equal(appsettings["cosmosdb:client:ConnectionString"], sconfig.ToString());
        }

        [Fact]
        public void ConnectionString_Overridden_By_CosmosDbInCloudFoundryConfig()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["cosmosdb:client:ConnectionString"] = "notEvenValidConnectionString-iHopeYouKnowBestWhatWorksForYou!"
            };

            // add environment variables as Cloud Foundry would
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", CosmosDbTestHelpers.SingleVCAPBinding);

            // add settings to config
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            var sconfig = new CosmosDbConnectorOptions(config);

            Assert.NotEqual(appsettings["cosmosdb:client:ConnectionString"], sconfig.ToString());

            // NOTE: for this test, we don't expect VCAP_SERVICES to be parsed,
            //          this test is only here to demonstrate that when a binding is present,
            //          a pre-supplied connectionString is not returned
        }
    }
}
