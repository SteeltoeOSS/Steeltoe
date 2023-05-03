// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Steeltoe.Connector.SqlServer;
using Xunit;

namespace Steeltoe.Connector.Test.SqlServer;

public class SqlServerProviderConfigurerTest
{
    // shared variable to hold configuration (like from a source such as appsettings)
    private readonly SqlServerProviderConnectorOptions _options = new()
    {
        Server = "localhost",
        Port = 1433,
        Username = "username",
        Password = "password",
        Database = "database"
    };

    [Fact]
    public void UpdateConfiguration_WithNullSqlServerServiceInfo_ReturnsExpected()
    {
        var configurer = new SqlServerProviderConfigurer();
        configurer.UpdateConfiguration(null, _options);

        Assert.Equal("localhost", _options.Server);
        Assert.Equal("username", _options.Username);
        Assert.Equal("password", _options.Password);
        Assert.Equal("database", _options.Database);
        Assert.Null(_options.ConnectionString);
    }

    [Fact]
    public void Update_With_ServiceInfo_Updates_Config()
    {
        var configurer = new SqlServerProviderConfigurer();
        var si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://updatedserver:1433/databaseName=updateddb", "updateduser", "updatedpassword");

        configurer.UpdateConfiguration(si, _options);

        Assert.Equal("updatedserver", _options.Server);
        Assert.Equal("updateddb", _options.Database);
        Assert.Equal("updateduser", _options.Username);
        Assert.Equal("updatedpassword", _options.Password);
    }

    [Fact]
    public void Update_With_ServiceInfo_CredentialsInUrl_Updates_Config()
    {
        var configurer = new SqlServerProviderConfigurer();
        var si = new SqlServerServiceInfo("MyId", "sqlserver://updateduser:updatedpassword@updatedserver:1433;databaseName=updateddb");

        configurer.UpdateConfiguration(si, _options);

        Assert.Equal("updatedserver", _options.Server);
        Assert.Equal("updateddb", _options.Database);
        Assert.Equal("updateduser", _options.Username);
        Assert.Equal("updatedpassword", _options.Password);
    }

    [Fact]
    public void Configure_Without_ServiceInfo_Returns_Config()
    {
        var configurer = new SqlServerProviderConfigurer();
        string opts = configurer.Configure(null, _options);
        Assert.Contains("Data Source=localhost,1433", opts, StringComparison.Ordinal);
        Assert.Contains("User Id=username;", opts, StringComparison.Ordinal);
        Assert.Contains("Password=password", opts, StringComparison.Ordinal);
        Assert.Contains("Initial Catalog=database;", opts, StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_With_ServiceInfo_Overrides_Config()
    {
        var configurer = new SqlServerProviderConfigurer();

        // override provided by environment
        var si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://servername:1433/databaseName=de5aa3a747c134b3d8780f8cc80be519e", "Dd6O1BPXUHdrmzbP",
            "7E1LxXnlH2hhlPVt");

        // apply override
        string opts = configurer.Configure(si, _options);

        // resulting options should contain values parsed from environment
        Assert.Contains("Data Source=servername,1433", opts, StringComparison.Ordinal);
        Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts, StringComparison.Ordinal);
        Assert.Contains("User Id=Dd6O1BPXUHdrmzbP;", opts, StringComparison.Ordinal);
        Assert.Contains("Password=7E1LxXnlH2hhlPVt", opts, StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_With_ServiceInfo_CredentialsInUrl_Overrides_Config()
    {
        var configurer = new SqlServerProviderConfigurer();

        // override provided by environment
        var si = new SqlServerServiceInfo("MyId",
            "jdbc:sqlserver://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@servername:1433/databaseName=de5aa3a747c134b3d8780f8cc80be519e");

        // apply override
        string opts = configurer.Configure(si, _options);

        // resulting options should contain values parsed from environment
        Assert.Contains("Data Source=servername,1433", opts, StringComparison.Ordinal);
        Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts, StringComparison.Ordinal);
        Assert.Contains("User Id=Dd6O1BPXUHdrmzbP;", opts, StringComparison.Ordinal);
        Assert.Contains("Password=7E1LxXnlH2hhlPVt", opts, StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_With_ServiceInfo_NamedInstance_Overrides_Config()
    {
        var configurer = new SqlServerProviderConfigurer();

        // override provided by environment
        var si = new SqlServerServiceInfo("MyId",
            "jdbc:sqlserver://servername/databaseName=de5aa3a747c134b3d8780f8cc80be519e;instanceName=someInstance;integratedSecurity=true");

        // apply override
        string opts = configurer.Configure(si, _options);

        // resulting options should contain values parsed from environment
        Assert.Contains("Data Source=servername\\someInstance", opts, StringComparison.Ordinal);
        Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts, StringComparison.Ordinal);
        Assert.Contains("integratedSecurity=true", opts, StringComparison.Ordinal);
    }
}
