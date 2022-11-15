// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connector.Redis;
using Xunit;

namespace Steeltoe.Connector.Test.Redis;

[Collection("Redis")]
public class RedisCacheConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new RedisCacheConnectorOptions(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_BindsValues()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:host"] = "localhost",
            ["redis:client:port"] = "1234",
            ["redis:client:password"] = "password",
            ["redis:client:instancename"] = "instanceid",
            ["redis:client:allowAdmin"] = "true",
            ["redis:client:clientName"] = "foobar",
            ["redis:client:connectRetry"] = "100",
            ["redis:client:connectTimeout"] = "100",
            ["redis:client:abortOnConnectFail"] = "true",
            ["redis:client:keepAlive"] = "100",
            ["redis:client:resolveDns"] = "true",
            ["redis:client:serviceName"] = "foobar",
            ["redis:client:ssl"] = "true",
            ["redis:client:sslHost"] = "foobar",
            ["redis:client:writeBuffer"] = "100",
            ["redis:client:tieBreaker"] = "foobar",
            ["redis:client:syncTimeout"] = "100"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new RedisCacheConnectorOptions(configurationRoot);
        Assert.Equal("localhost", options.Host);
        Assert.Equal(1234, options.Port);
        Assert.Equal("password", options.Password);
        Assert.Equal("instanceid", options.InstanceName);

        Assert.True(options.AllowAdmin);
        Assert.Equal("foobar", options.ClientName);
        Assert.Equal(100, options.ConnectRetry);
        Assert.Equal(100, options.ConnectTimeout);
        Assert.True(options.AbortOnConnectFail);
        Assert.Equal(100, options.KeepAlive);
        Assert.True(options.ResolveDns);
        Assert.Equal("foobar", options.ServiceName);
        Assert.True(options.Ssl);
        Assert.Equal("foobar", options.SslHost);
        Assert.Equal("foobar", options.TieBreaker);
        Assert.Equal(100, options.WriteBuffer);
        Assert.Equal(100, options.SyncTimeout);

        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void ConnectionString_Returned_AsConfigured()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new RedisCacheConnectorOptions(configurationRoot);

        Assert.Equal(appsettings["redis:client:ConnectionString"], options.ToString());
    }

    [Fact]
    public void ConnectionString_Overridden_By_CloudFoundryConfig()
    {
        // simulate an appsettings file
        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
        };

        // add environment variables as Cloud Foundry would
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.SingleServerVcap);

        // add settings to configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new RedisCacheConnectorOptions(configurationRoot);

        Assert.NotEqual(appsettings["redis:client:ConnectionString"], options.ToString());
    }
}
