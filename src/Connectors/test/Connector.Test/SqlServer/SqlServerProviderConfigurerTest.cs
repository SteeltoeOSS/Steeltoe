// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.SqlServer.Test;

public class SqlServerProviderConfigurerTest
{
    // shared variable to hold config (like from a source such as appsettings)
    private readonly SqlServerProviderConnectorOptions _config = new()
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
        configurer.UpdateConfiguration(null, _config);

        Assert.Equal("localhost", _config.Server);
        Assert.Equal("username", _config.Username);
        Assert.Equal("password", _config.Password);
        Assert.Equal("database", _config.Database);
        Assert.Null(_config.ConnectionString);
    }

    [Fact]
    public void Update_With_ServiceInfo_Updates_Config()
    {
        var configurer = new SqlServerProviderConfigurer();
        var si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://updatedserver:1433/databaseName=updateddb", "updateduser", "updatedpassword");

        configurer.UpdateConfiguration(si, _config);

        Assert.Equal("updatedserver", _config.Server);
        Assert.Equal("updateddb", _config.Database);
        Assert.Equal("updateduser", _config.Username);
        Assert.Equal("updatedpassword", _config.Password);
    }

    [Fact]
    public void Update_With_ServiceInfo_CredentialsInUrl_Updates_Config()
    {
        var configurer = new SqlServerProviderConfigurer();
        var si = new SqlServerServiceInfo("MyId", "sqlserver://updateduser:updatedpassword@updatedserver:1433;databaseName=updateddb");

        configurer.UpdateConfiguration(si, _config);

        Assert.Equal("updatedserver", _config.Server);
        Assert.Equal("updateddb", _config.Database);
        Assert.Equal("updateduser", _config.Username);
        Assert.Equal("updatedpassword", _config.Password);
    }

    [Fact]
    public void Configure_Without_ServiceInfo_Returns_Config()
    {
        var configurer = new SqlServerProviderConfigurer();
        string opts = configurer.Configure(null, _config);
        Assert.Contains("Data Source=localhost,1433", opts);
        Assert.Contains("User Id=username;", opts);
        Assert.Contains("Password=password;", opts);
        Assert.Contains("Initial Catalog=database;", opts);
    }

    [Fact]
    public void Configure_With_ServiceInfo_Overrides_Config()
    {
        var configurer = new SqlServerProviderConfigurer();

        // override provided by environment
        var si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://servername:1433/databaseName=de5aa3a747c134b3d8780f8cc80be519e", "Dd6O1BPXUHdrmzbP",
            "7E1LxXnlH2hhlPVt");

        // apply override
        string opts = configurer.Configure(si, _config);

        // resulting options should contain values parsed from environment
        Assert.Contains("Data Source=servername,1433", opts);
        Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts);
        Assert.Contains("User Id=Dd6O1BPXUHdrmzbP;", opts);
        Assert.Contains("Password=7E1LxXnlH2hhlPVt;", opts);
    }

    [Fact]
    public void Configure_With_ServiceInfo_CredentialsInUrl_Overrides_Config()
    {
        var configurer = new SqlServerProviderConfigurer();

        // override provided by environment
        var si = new SqlServerServiceInfo("MyId",
            "jdbc:sqlserver://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@servername:1433/databaseName=de5aa3a747c134b3d8780f8cc80be519e");

        // apply override
        string opts = configurer.Configure(si, _config);

        // resulting options should contain values parsed from environment
        Assert.Contains("Data Source=servername,1433", opts);
        Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts);
        Assert.Contains("User Id=Dd6O1BPXUHdrmzbP;", opts);
        Assert.Contains("Password=7E1LxXnlH2hhlPVt;", opts);
    }

    [Fact]
    public void Configure_With_ServiceInfo_NamedInstance_Overrides_Config()
    {
        var configurer = new SqlServerProviderConfigurer();

        // override provided by environment
        var si = new SqlServerServiceInfo("MyId",
            "jdbc:sqlserver://servername/databaseName=de5aa3a747c134b3d8780f8cc80be519e;instanceName=someInstance;integratedSecurity=true");

        // apply override
        string opts = configurer.Configure(si, _config);

        // resulting options should contain values parsed from environment
        Assert.Contains("Data Source=servername\\someInstance", opts);
        Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts);
        Assert.Contains("integratedSecurity=true;", opts);
    }
}
