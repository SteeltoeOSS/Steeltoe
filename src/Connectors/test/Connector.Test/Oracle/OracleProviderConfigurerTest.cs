// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Oracle.Test;

public class OracleProviderConfigurerTest
{
    [Fact]
    public void UpdateConfiguration_WithNullOracleServiceInfo_ReturnsExpected()
    {
        var configurer = new OracleProviderConfigurer();

        var options = new OracleProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            ServiceName = "orcl"
        };

        configurer.UpdateConfiguration(null, options);

        Assert.Equal("localhost", options.Server);
        Assert.Equal(1234, options.Port);
        Assert.Equal("username", options.Username);
        Assert.Equal("password", options.Password);
        Assert.Equal("orcl", options.ServiceName);
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void UpdateConfiguration_WithOracleServiceInfo_ReturnsExpected()
    {
        var configurer = new OracleProviderConfigurer();

        var options = new OracleProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            ServiceName = "orcl"
        };

        var si = new OracleServiceInfo("MyId", "oracle://user:pwd@localhost:1521/orclpdb1");

        configurer.UpdateConfiguration(si, options);

        Assert.Equal("localhost", options.Server);
        Assert.Equal(1521, options.Port);
        Assert.Equal("user", options.Username);
        Assert.Equal("pwd", options.Password);
        Assert.Equal("orclpdb1", options.ServiceName);
    }

    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var options = new OracleProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            ServiceName = "orcl",
            ConnectionTimeout = 10
        };

        var configurer = new OracleProviderConfigurer();
        string configuration = configurer.Configure(null, options);

        string connectionString =
            $"User Id={options.Username};Password={options.Password};Data Source={options.Server}:{options.Port}/{options.ServiceName};Connection Timeout={options.ConnectionTimeout}";

        Assert.StartsWith(connectionString, configuration);
    }

    [Fact]
    public void Configure_ServiceInfoOverridesConfig_ReturnsExpected()
    {
        var options = new OracleProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            ServiceName = "orcl"
        };

        var configurer = new OracleProviderConfigurer();
        var si = new OracleServiceInfo("MyId", "oracle://user:pwd@localhost:1521/orclpdb1");

        _ = configurer.Configure(si, options);

        Assert.Equal("localhost", options.Server);
        Assert.Equal(1521, options.Port);
        Assert.Equal("user", options.Username);
        Assert.Equal("pwd", options.Password);
        Assert.Equal("orclpdb1", options.ServiceName);
    }
}
