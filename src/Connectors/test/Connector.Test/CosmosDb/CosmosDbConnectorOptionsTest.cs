// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connector.CosmosDb;
using Xunit;

namespace Steeltoe.Connector.Test.CosmosDb;

public class CosmosDbConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new CosmosDbConnectorOptions(configuration));
        Assert.Contains(nameof(configuration), ex.Message);
    }

    [Fact]
    public void Constructor_BindsValues()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["cosmosdb:client:host"] = "https://localhost:443",
            ["cosmosdb:client:masterkey"] = "masterKey",
            ["cosmosdb:client:readonlykey"] = "readOnlyKey",
            ["cosmosdb:client:databaseId"] = "databaseId",
            ["cosmosdb:client:databaseLink"] = "databaseLink"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new CosmosDbConnectorOptions(configurationRoot);
        Assert.Equal("https://localhost:443", options.Host);
        Assert.Equal("masterKey", options.MasterKey);
        Assert.Equal("readOnlyKey", options.ReadOnlyKey);
        Assert.Equal("databaseId", options.DatabaseId);
        Assert.Equal("databaseLink", options.DatabaseLink);
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void ConnectionString_Returned_AsConfigured()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", string.Empty);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", string.Empty);

        var appsettings = new Dictionary<string, string>
        {
            ["cosmosdb:client:ConnectionString"] = "notEvenValidConnectionString-iHopeYouKnowBestWhatWorksForYou!"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new CosmosDbConnectorOptions(configurationRoot);

        Assert.Equal(appsettings["cosmosdb:client:ConnectionString"], options.ToString());
    }

    [Fact]
    public void ConnectionString_Overridden_By_CosmosDbInCloudFoundryConfig()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["cosmosdb:client:ConnectionString"] = "notEvenValidConnectionString-iHopeYouKnowBestWhatWorksForYou!"
        };

        // add environment variables as Cloud Foundry would
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", CosmosDbTestHelpers.SingleVcapBinding);

        // add settings to configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new CosmosDbConnectorOptions(configurationRoot);

        Assert.NotEqual(appsettings["cosmosdb:client:ConnectionString"], options.ToString());

        // NOTE: for this test, we don't expect VCAP_SERVICES to be parsed,
        //          this test is only here to demonstrate that when a binding is present,
        //          a pre-supplied connectionString is not returned
    }
}
