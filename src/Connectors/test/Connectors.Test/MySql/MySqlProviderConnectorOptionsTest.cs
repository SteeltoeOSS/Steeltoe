// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.MySql;
using Xunit;

namespace Steeltoe.Connectors.Test.MySql;

public class MySqlProviderConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new MySqlProviderConnectorOptions(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);
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
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new MySqlProviderConnectorOptions(configurationRoot);
        Assert.Equal("localhost", options.Server);
        Assert.Equal(1234, options.Port);
        Assert.True(options.PersistSecurityInfo);
        Assert.Equal("password", options.Password);
        Assert.Equal("username", options.Username);
        Assert.Null(options.ConnectionString);
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
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new MySqlProviderConnectorOptions(configurationRoot);

        Assert.Equal(appsettings["mysql:client:ConnectionString"], options.ToString());
    }

    [Fact]
    public void ConnectionString_Overridden_By_CloudFoundryConfig()
    {
        // simulate an appsettings file
        var appsettings = new Dictionary<string, string>
        {
            ["mysql:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
        };

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MySqlTestHelpers.SingleServerVcap);

        // add settings to configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new MySqlProviderConnectorOptions(configurationRoot);

        Assert.NotEqual(appsettings["mysql:client:ConnectionString"], options.ToString());

        // NOTE: for this test, we don't expect VCAP_SERVICES to be parsed,
        //          this test is only here to demonstrate that when a binding is present,
        //          a pre-supplied connectionString is not returned
    }
}
