// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.Oracle.Test;

public class OracleProviderConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new OracleProviderConnectorOptions(config));
        Assert.Contains(nameof(config), ex.Message);
    }

    [Fact]
    public void Constructor_BindsValues()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["oracle:client:server"] = "localhost",
            ["oracle:client:port"] = "1234",
            ["oracle:client:password"] = "password",
            ["oracle:client:username"] = "username"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var sconfig = new OracleProviderConnectorOptions(config);
        Assert.Equal("localhost", sconfig.Server);
        Assert.Equal(1234, sconfig.Port);
        Assert.Equal("password", sconfig.Password);
        Assert.Equal("username", sconfig.Username);
        Assert.Null(sconfig.ConnectionString);
    }

    [Fact]
    public void ConnectionString_Returned_AsConfigured()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["oracle:client:ConnectionString"] = "Data Source=localhost:1521/orclpdb1;User Id=hr;Password=hr;"
        };
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var sconfig = new OracleProviderConnectorOptions(config);

        Assert.Equal(appsettings["oracle:client:ConnectionString"], sconfig.ToString());
    }
}
