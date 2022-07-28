// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.PostgreSql.Test;

public class PostgresProviderConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new PostgresProviderConnectorOptions(config));
        Assert.Contains(nameof(config), ex.Message);
    }

    [Fact]
    public void Constructor_BindsValues()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:host"] = "localhost",
            ["postgres:client:port"] = "1234",
            ["postgres:client:password"] = "password",
            ["postgres:client:username"] = "username",
            ["postgres:client:searchpath"] = "searchpath"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var options = new PostgresProviderConnectorOptions(config);
        Assert.Equal("localhost", options.Host);
        Assert.Equal(1234, options.Port);
        Assert.Equal("password", options.Password);
        Assert.Equal("username", options.Username);
        Assert.Equal("searchpath", options.SearchPath);
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void ConnectionString_Returned_AsConfigured()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:ConnectionString"] = "Server=fake;Database=test;User Id=steeltoe;Password=password;"
        };
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var options = new PostgresProviderConnectorOptions(config);

        Assert.StartsWith(appsettings["postgres:client:ConnectionString"], options.ToString());
    }

    [Fact]
    public void ConnectionString_Returned_BuildFromConfig()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:Host"] = "fake-db.host",
            ["postgres:client:Port"] = "3000",
            ["postgres:client:Username"] = "fakeUsername",
            ["postgres:client:Password"] = "fakePassword",
            ["postgres:client:Database"] = "fakeDB",
            ["postgres:client:SearchPath"] = "fakeSchema",
        };
        var expected = "Host=fake-db.host;Port=3000;Username=fakeUsername;Password=fakePassword;Database=fakeDB;Timeout=15;Command Timeout=30;Search Path=fakeSchema;";
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var options = new PostgresProviderConnectorOptions(config);

        Assert.Equal(expected, options.ToString());
    }

    [Fact]
    public void ConnectionString_Overridden_By_CloudFoundryConfig()
    {
        // simulate an appsettings file
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:ConnectionString"] = "Server=fake;Database=test;User Id=steeltoe;Password=password;"
        };

        // add environment variables as Cloud Foundry would
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVcapEdb);

        // add settings to config
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        var config = configurationBuilder.Build();

        var options = new PostgresProviderConnectorOptions(config);

        Assert.NotEqual(appsettings["postgres:client:ConnectionString"], options.ToString());
    }

    [Fact]
    public void ConnectionString_Overridden_By_CloudFoundryConfig_Use_SearchPath()
    {
        // simulate an appsettings file
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:ConnectionString"] = "Server=fake;Database=test;User Id=steeltoe;Password=password;",
            ["postgres:client:SearchPath"] = "SomeSchema"
        };

        // add environment variables as Cloud Foundry would
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVcapEdb);

        // add settings to config
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        var config = configurationBuilder.Build();

        var options = new PostgresProviderConnectorOptions(config);

        Assert.DoesNotContain(appsettings["postgres:client:ConnectionString"], options.ToString());
        Assert.EndsWith($"Search Path={options.SearchPath};", options.ToString());
    }
}
