// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.MongoDb.Test;

public class MongoDbConnectorFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const MongoDbConnectorOptions options = null;
        const MongoDbServiceInfo si = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new MongoDbConnectorFactory(si, options, MongoDbTypeLocator.MongoClient));
        Assert.Contains(nameof(options), ex.Message);
    }

    [Fact]
    public void Create_ReturnsMongoDbConnection()
    {
        var options = new MongoDbConnectorOptions
        {
            Server = "localhost",
            Port = 27016,
            Password = "password",
            Username = "username"
        };

        var si = new MongoDbServiceInfo("MyId", "mongodb://localhost:27017");
        var factory = new MongoDbConnectorFactory(si, options, MongoDbTypeLocator.MongoClient);
        object connection = factory.Create(null);
        Assert.NotNull(connection);
    }

    [Fact]
    public void UpdateConfiguration_WithNullMongoDbServiceInfo_ReturnsExpected()
    {
        var configurer = new MongoDbProviderConfigurer();

        var options = new MongoDbConnectorOptions
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
    public void UpdateConfiguration_WithMongoDbServiceInfo_ReturnsExpected()
    {
        var configurer = new MongoDbProviderConfigurer();

        var options = new MongoDbConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var si = new MongoDbServiceInfo("MyId", "mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        configurer.UpdateConfiguration(si, options);

        Assert.Equal("192.168.0.90", options.Server);
        Assert.Equal(27017, options.Port);
        Assert.Equal("Dd6O1BPXUHdrmzbP", options.Username);
        Assert.Equal("7E1LxXnlH2hhlPVt", options.Password);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", options.Database);
    }

    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var options = new MongoDbConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var configurer = new MongoDbProviderConfigurer();
        string configuration = configurer.Configure(null, options);
        Assert.Equal("mongodb://username:password@localhost:1234/database", configuration);
    }

    [Fact]
    public void Configure_ServiceInfoOverridesConfig_ReturnsExpected()
    {
        var options = new MongoDbConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            Database = "database"
        };

        var configurer = new MongoDbProviderConfigurer();
        var si = new MongoDbServiceInfo("MyId", "mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        string opts = configurer.Configure(si, options);

        Assert.Equal("mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", opts);
        Assert.DoesNotContain("localhost", opts);
        Assert.DoesNotContain("1234", opts);
        Assert.DoesNotContain("username", opts);
        Assert.DoesNotContain("password", opts);
        Assert.DoesNotContain("database", opts);
    }
}
