// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.MongoDb;
using Xunit;

namespace Steeltoe.Connectors.Test.MongoDb;

public class MongoDbConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new MongoDbConnectorOptions(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_BindsValues()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["mongodb:client:server"] = "localhost",
            ["mongodb:client:port"] = "1234",
            ["mongodb:client:password"] = "password",
            ["mongodb:client:username"] = "username"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new MongoDbConnectorOptions(configurationRoot);
        Assert.Equal("localhost", options.Server);
        Assert.Equal(1234, options.Port);
        Assert.Equal("password", options.Password);
        Assert.Equal("username", options.Username);
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void Constructor_BindsOptions()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["mongodb:client:options:someKey"] = "someValue",
            ["mongodb:client:options:someOtherKey"] = "someOtherValue",
            ["mongodb:client:options:stillAnotherKey"] = "stillAnotherValue",
            ["mongodb:client:options:yetOneMoreKey"] = "yetOneMoreValue"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new MongoDbConnectorOptions(configurationRoot);

        Assert.Equal("someValue", options.Options["someKey"]);
        Assert.Equal("someOtherValue", options.Options["someOtherKey"]);
        Assert.Equal("stillAnotherValue", options.Options["stillAnotherKey"]);
        Assert.Equal("yetOneMoreValue", options.Options["yetOneMoreKey"]);
    }

    [Fact]
    public void Options_Included_InConnectionString()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["mongodb:client:options:someKey"] = "someValue",
            ["mongodb:client:options:someOtherKey"] = "someOtherValue"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new MongoDbConnectorOptions(configurationRoot);

        Assert.Equal("mongodb://localhost:27017?someKey=someValue&someOtherKey=someOtherValue", options.ToString());
    }

    [Fact]
    public void ConnectionString_Returned_AsConfigured()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["mongodb:client:ConnectionString"] = "notEvenValidConnectionString-iHopeYouKnowBestWhatWorksForYou!"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new MongoDbConnectorOptions(configurationRoot);

        Assert.Equal(appsettings["mongodb:client:ConnectionString"], options.ToString());
    }

    [Fact]
    public void ConnectionString_OverriddenByVCAP()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["mongodb:client:ConnectionString"] = "notEvenValidConnectionString-iHopeYouKnowBestWhatWorksForYou!"
        };

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.SingleBindingA9SSingleServerVcap);

        // add settings to configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new MongoDbConnectorOptions(configurationRoot);

        Assert.NotEqual(appsettings["mongodb:client:ConnectionString"], options.ToString());
    }

    [Fact]
    public void ConnectionString_Overridden_By_EnterpriseMongoInCloudFoundryConfig()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["mongodb:client:ConnectionString"] = "notEvenValidConnectionString-iHopeYouKnowBestWhatWorksForYou!"
        };

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.SingleServerEnterpriseVcap);

        // add settings to configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new MongoDbConnectorOptions(configurationRoot);

        Assert.NotEqual(appsettings["mongodb:client:ConnectionString"], options.ToString());

        // NOTE: for this test, we don't expect VCAP_SERVICES to be parsed,
        //          this test is only here to demonstrate that when a binding is present,
        //          a pre-supplied connectionString is not returned
    }
}
