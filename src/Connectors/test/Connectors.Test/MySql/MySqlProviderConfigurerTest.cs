// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.Services;
using Xunit;

namespace Steeltoe.Connectors.Test.MySql;

public class MySqlProviderConfigurerTest
{
    [Fact]
    public void UpdateConfiguration_WithNullMySqlServiceInfo_ReturnsExpected()
    {
        var configurer = new MySqlProviderConfigurer();

        var options = new MySqlProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        configurer.UpdateConfiguration(null, options);

        Assert.Equal("localhost", options.Server);
        Assert.Equal(1234, options.Port);
        Assert.Equal("username", options.Username);
        Assert.Equal("password", options.Password);
        Assert.Equal("database", options.Database);
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void UpdateConfiguration_WithMySqlServiceInfo_ReturnsExpected()
    {
        var configurer = new MySqlProviderConfigurer();

        var options = new MySqlProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var si = new MySqlServiceInfo("MyId", "mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        configurer.UpdateConfiguration(si, options);

        Assert.Equal("192.168.0.90", options.Server);
        Assert.Equal(3306, options.Port);
        Assert.Equal("Dd6O1BPXUHdrmzbP", options.Username);
        Assert.Equal("7E1LxXnlH2hhlPVt", options.Password);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", options.Database);
    }

    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var options = new MySqlProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var configurer = new MySqlProviderConfigurer();
        string opts = configurer.Configure(null, options);
        Assert.Contains("Server=localhost;", opts, StringComparison.Ordinal);
        Assert.Contains("Port=1234;", opts, StringComparison.Ordinal);
        Assert.Contains("Username=username;", opts, StringComparison.Ordinal);
        Assert.Contains("Password=password;", opts, StringComparison.Ordinal);
        Assert.Contains("Database=database", opts, StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_ServiceInfoOverridesConfig_ReturnsExpected()
    {
        var options = new MySqlProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var configurer = new MySqlProviderConfigurer();
        var si = new MySqlServiceInfo("MyId", "mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        string configuration = configurer.Configure(si, options);

        Assert.Contains("Server=192.168.0.90;", configuration, StringComparison.Ordinal);
        Assert.Contains("Port=3306;", configuration, StringComparison.Ordinal);
        Assert.Contains("Username=Dd6O1BPXUHdrmzbP;", configuration, StringComparison.Ordinal);
        Assert.Contains("Password=7E1LxXnlH2hhlPVt;", configuration, StringComparison.Ordinal);
        Assert.Contains("Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", configuration, StringComparison.Ordinal);
    }
}
