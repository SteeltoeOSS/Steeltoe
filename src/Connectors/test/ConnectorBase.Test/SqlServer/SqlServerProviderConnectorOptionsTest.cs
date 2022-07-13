// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.SqlServer.Test;

public class SqlServerProviderConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new SqlServerProviderConnectorOptions(config));
        Assert.Contains(nameof(config), ex.Message);
    }

    [Fact]
    public void Constructor_BindsValues()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["sqlserver:credentials:uid"] = "username",
            ["sqlserver:credentials:uri"] = "jdbc:sqlserver://servername:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e",
            ["sqlserver:credentials:db"] = "de5aa3a747c134b3d8780f8cc80be519e",
            ["sqlserver:credentials:pw"] = "password"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var options = new SqlServerProviderConnectorOptions(config);
        Assert.Equal("servername", options.Server);
        Assert.Equal(1433, options.Port);
        Assert.Equal("password", options.Password);
        Assert.Equal("username", options.Username);
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void ConnectionString_Returned_AsConfigured()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["sqlserver:credentials:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
        };
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var options = new SqlServerProviderConnectorOptions(config);

        Assert.Equal(appsettings["sqlserver:credentials:ConnectionString"], options.ToString());
    }

    [Fact]
    public void ConnectionString_Overridden_By_CloudFoundryConfig()
    {
        // simulate an appsettings file
        var appsettings = new Dictionary<string, string>
        {
            ["sqlserver:credentials:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
        };

        // add environment variables as Cloud Foundry would
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVcap);

        // add settings to config
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        var config = configurationBuilder.Build();

        var options = new SqlServerProviderConnectorOptions(config);

        Assert.NotEqual(appsettings["sqlserver:credentials:ConnectionString"], options.ToString());
    }

    [Fact]
    public void CloudFoundryConfig_Found_By_Name()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVcapNoTag);

        // add settings to config
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        var config = configurationBuilder.Build();

        var options = new SqlServerProviderConnectorOptions(config);

        Assert.NotEqual("192.168.0.80", options.Server);
        Assert.NotEqual("de5aa3a747c134b3d8780f8cc80be519e", options.Database);
        Assert.NotEqual("uf33b2b30783a4087948c30f6c3b0c90f", options.Username);
        Assert.NotEqual("Pefbb929c1e0945b5bab5b8f0d110c503", options.Password);
    }

    [Fact]
    public void CloudFoundryConfig_Found_By_Tag()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVcapIgnoreName);

        // add settings to config
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        var config = configurationBuilder.Build();

        var options = new SqlServerProviderConnectorOptions(config);

        Assert.NotEqual("192.168.0.80", options.Server);
        Assert.NotEqual("de5aa3a747c134b3d8780f8cc80be519e", options.Database);
        Assert.NotEqual("uf33b2b30783a4087948c30f6c3b0c90f", options.Username);
        Assert.NotEqual("Pefbb929c1e0945b5bab5b8f0d110c503", options.Password);
    }

    [Fact]
    public void ConnectionString_Overridden_By_CloudFoundryConfig_CredentialsInUrl()
    {
        // simulate an appsettings file
        var appsettings = new Dictionary<string, string>
        {
            ["sqlserver:credentials:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
        };

        // add environment variables as Cloud Foundry would
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVcapCredentialsInUrl);

        // add settings to config
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        var config = configurationBuilder.Build();

        var options = new SqlServerProviderConnectorOptions(config);

        Assert.NotEqual(appsettings["sqlserver:credentials:ConnectionString"], options.ToString());
    }
}
