// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connectors.PostgreSql;
using Steeltoe.Connectors.Services;
using Xunit;

namespace Steeltoe.Connectors.Test.PostgreSql;

public class PostgreSqlProviderConfigurerTest
{
    [Fact]
    public void UpdateConfiguration_WithNullPostgreSqlServiceInfo_ReturnsExpected()
    {
        var configurer = new PostgreSqlProviderConfigurer();

        var options = new PostgreSqlProviderConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        configurer.UpdateConfiguration(null, options);

        Assert.Equal("localhost", options.Host);
        Assert.Equal(1234, options.Port);
        Assert.Equal("username", options.Username);
        Assert.Equal("password", options.Password);
        Assert.Equal("database", options.Database);
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void UpdateConfiguration_WithPostgreSqlServiceInfo_ReturnsExpected()
    {
        var configurer = new PostgreSqlProviderConfigurer();

        var options = new PostgreSqlProviderConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var si = new PostgreSqlServiceInfo("MyId", "postgres://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        configurer.UpdateConfiguration(si, options);

        Assert.Equal("192.168.0.90", options.Host);
        Assert.Equal(5432, options.Port);
        Assert.Equal("Dd6O1BPXUHdrmzbP", options.Username);
        Assert.Equal("7E1LxXnlH2hhlPVt", options.Password);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", options.Database);
    }

    [Fact]
    public void UpdateConfiguration_WithPostgreSqlServiceInfo_UriEncoded_ReturnsExpected()
    {
        var configurer = new PostgreSqlProviderConfigurer();

        var options = new PostgreSqlProviderConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var si = new PostgreSqlServiceInfo("MyId", "postgres://Dd6O1BPXUHdrmzbP:%247E1LxXnlH2hhlPVt@192.168.0.90:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        configurer.UpdateConfiguration(si, options);

        Assert.Equal("192.168.0.90", options.Host);
        Assert.Equal(5432, options.Port);
        Assert.Equal("Dd6O1BPXUHdrmzbP", options.Username);
        Assert.Equal("$7E1LxXnlH2hhlPVt", options.Password);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", options.Database);
    }

    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var options = new PostgreSqlProviderConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var configurer = new PostgreSqlProviderConfigurer();
        string opts = configurer.Configure(null, options);
        Assert.Contains("Host=localhost;", opts, StringComparison.Ordinal);
        Assert.Contains("Port=1234;", opts, StringComparison.Ordinal);
        Assert.Contains("Username=username;", opts, StringComparison.Ordinal);
        Assert.Contains("Password=password;", opts, StringComparison.Ordinal);
        Assert.Contains("Database=database;", opts, StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_ServiceInfoOverridesConfig_ReturnsExpected()
    {
        var options = new PostgreSqlProviderConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var configurer = new PostgreSqlProviderConfigurer();
        var si = new PostgreSqlServiceInfo("MyId", "postgres://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        string configuration = configurer.Configure(si, options);

        Assert.Contains("Host=192.168.0.90;", configuration, StringComparison.Ordinal);
        Assert.Contains("Port=5432;", configuration, StringComparison.Ordinal);
        Assert.Contains("Username=Dd6O1BPXUHdrmzbP;", configuration, StringComparison.Ordinal);
        Assert.Contains("Password=7E1LxXnlH2hhlPVt;", configuration, StringComparison.Ordinal);
        Assert.Contains("Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355;", configuration, StringComparison.Ordinal);
    }
}
