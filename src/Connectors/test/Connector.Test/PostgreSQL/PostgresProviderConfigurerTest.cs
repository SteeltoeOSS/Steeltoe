// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.PostgreSql.Test;

public class PostgresProviderConfigurerTest
{
    [Fact]
    public void UpdateConfiguration_WithNullPostgresServiceInfo_ReturnsExpected()
    {
        var configurer = new PostgresProviderConfigurer();

        var options = new PostgresProviderConnectorOptions
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
    public void UpdateConfiguration_WithPostgresServiceInfo_ReturnsExpected()
    {
        var configurer = new PostgresProviderConfigurer();

        var options = new PostgresProviderConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var si = new PostgresServiceInfo("MyId", "postgres://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        configurer.UpdateConfiguration(si, options);

        Assert.Equal("192.168.0.90", options.Host);
        Assert.Equal(5432, options.Port);
        Assert.Equal("Dd6O1BPXUHdrmzbP", options.Username);
        Assert.Equal("7E1LxXnlH2hhlPVt", options.Password);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", options.Database);
    }

    [Fact]
    public void UpdateConfiguration_WithPostgresServiceInfo_UriEncoded_ReturnsExpected()
    {
        var configurer = new PostgresProviderConfigurer();

        var options = new PostgresProviderConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var si = new PostgresServiceInfo("MyId", "postgres://Dd6O1BPXUHdrmzbP:%247E1LxXnlH2hhlPVt@192.168.0.90:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

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
        var options = new PostgresProviderConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var configurer = new PostgresProviderConfigurer();
        string opts = configurer.Configure(null, options);
        Assert.Contains("Host=localhost;", opts);
        Assert.Contains("Port=1234;", opts);
        Assert.Contains("Username=username;", opts);
        Assert.Contains("Password=password;", opts);
        Assert.Contains("Database=database;", opts);
    }

    [Fact]
    public void Configure_ServiceInfoOverridesConfig_ReturnsExpected()
    {
        var options = new PostgresProviderConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var configurer = new PostgresProviderConfigurer();
        var si = new PostgresServiceInfo("MyId", "postgres://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        string configuration = configurer.Configure(si, options);

        Assert.Contains("Host=192.168.0.90;", configuration);
        Assert.Contains("Port=5432;", configuration);
        Assert.Contains("Username=Dd6O1BPXUHdrmzbP;", configuration);
        Assert.Contains("Password=7E1LxXnlH2hhlPVt;", configuration);
        Assert.Contains("Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355;", configuration);
    }
}
