// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Connector.Redis.Test;

public class RedisCacheConfigurationExtensionsTest
{
    public RedisCacheConfigurationExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void CreateRedisServiceConnectorFactory_ThrowsIfConfigurationNull()
    {
        const IConfigurationRoot configuration = null;
        IConfigurationRoot connectorConfiguration = new ConfigurationBuilder().Build();
        var options = new RedisCacheConnectorOptions();

        var ex = Assert.Throws<ArgumentNullException>(() => configuration.CreateRedisServiceConnectorFactory("foobar"));
        Assert.Contains(nameof(configuration), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => configuration.CreateRedisServiceConnectorFactory(connectorConfiguration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => configuration.CreateRedisServiceConnectorFactory(options, "foobar"));
        Assert.Contains(nameof(configuration), ex3.Message);
    }

    [Fact]
    public void CreateRedisServiceConnectorFactory_ThrowsIfConnectorConfigurationNull()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        const IConfigurationRoot connectorConfiguration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => configurationRoot.CreateRedisServiceConnectorFactory(connectorConfiguration, "foobar"));
        Assert.Contains(nameof(connectorConfiguration), ex.Message);
    }

    [Fact]
    public void CreateRedisServiceConnectorFactory_ThrowsIfConnectorOptionsNull()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        const RedisCacheConnectorOptions connectorOptions = null;

        var ex = Assert.Throws<ArgumentNullException>(() => configurationRoot.CreateRedisServiceConnectorFactory(connectorOptions, "foobar"));
        Assert.Contains(nameof(connectorOptions), ex.Message);
    }

    [Fact]
    public void CreateRedisServiceConnectorFactory_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        var options = new RedisCacheConnectorOptions();

        var ex = Assert.Throws<ConnectorException>(() => configurationRoot.CreateRedisServiceConnectorFactory("foobar"));
        Assert.Contains("foobar", ex.Message);

        var ex2 = Assert.Throws<ConnectorException>(() => configurationRoot.CreateRedisServiceConnectorFactory(configurationRoot, "foobar"));
        Assert.Contains("foobar", ex2.Message);

        var ex3 = Assert.Throws<ConnectorException>(() => configurationRoot.CreateRedisServiceConnectorFactory(options, "foobar"));
        Assert.Contains("foobar", ex3.Message);
    }

    [Fact]
    public void CreateRedisServiceConnectorFactory_NoVCAPs_CreatesFactory()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:host"] = "127.0.0.1",
            ["redis:client:port"] = "1234",
            ["redis:client:password"] = "password",
            ["redis:client:abortOnConnectFail"] = "false"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        var options = new RedisCacheConnectorOptions(configurationRoot);

        Assert.NotNull(configurationRoot.CreateRedisServiceConnectorFactory());
        Assert.NotNull(new ConfigurationBuilder().Build().CreateRedisServiceConnectorFactory(configurationRoot));
        Assert.NotNull(new ConfigurationBuilder().Build().CreateRedisServiceConnectorFactory(options));
    }

    [Fact]
    public void CreateRedisServiceConnectorFactory_MultipleRedisServices_ThrowsConnectorException()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.TwoServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();
        var options = new RedisCacheConnectorOptions();

        var ex = Assert.Throws<ConnectorException>(() => configurationRoot.CreateRedisServiceConnectorFactory());
        Assert.Contains("Multiple", ex.Message);

        var ex2 = Assert.Throws<ConnectorException>(() => configurationRoot.CreateRedisServiceConnectorFactory(configurationRoot));
        Assert.Contains("Multiple", ex2.Message);

        var ex3 = Assert.Throws<ConnectorException>(() => configurationRoot.CreateRedisServiceConnectorFactory(options));
        Assert.Contains("Multiple", ex3.Message);
    }
}
