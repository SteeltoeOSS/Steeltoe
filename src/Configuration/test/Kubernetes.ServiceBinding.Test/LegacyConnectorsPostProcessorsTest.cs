// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class LegacyConnectorsPostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void RabbitMQTest_BindingTypeEnabled()
    {
        var postProcessor = new RabbitMQLegacyConnectorPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("addresses", "test-addresses"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("virtual-host", "test-virtual-host")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RabbitMQPostProcessor.BindingTypeKey, secrets);

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-addresses", configurationData["rabbitmq:client:uri"]);
        Assert.Equal("test-host", configurationData["rabbitmq:client:server"]);
        Assert.Equal("test-password", configurationData["rabbitmq:client:password"]);
        Assert.Equal("test-port", configurationData["rabbitmq:client:port"]);
        Assert.Equal("test-username", configurationData["rabbitmq:client:username"]);
        Assert.Equal("test-virtual-host", configurationData["rabbitmq:client:virtualhost"]);
    }

    [Fact]
    public void MySqlTest_BindingTypeEnabled()
    {
        var postProcessor = new MySqlLegacyConnectorPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MySqlPostProcessor.BindingTypeKey, secrets);

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-database", configurationData["mysql:client:database"]);
        Assert.Equal("test-host", configurationData["mysql:client:server"]);
        Assert.Equal("test-password", configurationData["mysql:client:password"]);
        Assert.Equal("test-port", configurationData["mysql:client:port"]);
        Assert.Equal("test-username", configurationData["mysql:client:username"]);
        Assert.Equal("test-jdbc-url", configurationData["mysql:client:jdbcUrl"]);
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeEnabled()
    {
        var postProcessor = new PostgreSqlLegacyConnectorPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("sslmode", "verify-full"),
            Tuple.Create("sslrootcert", "root.cert"),
            Tuple.Create("options", "--cluster=routing-id&opt=val1")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, PostgreSqlPostProcessor.BindingTypeKey, secrets);

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, PostgreSqlPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-database", configurationData["postgres:client:database"]);
        Assert.Equal("test-host", configurationData["postgres:client:host"]);
        Assert.Equal("test-password", configurationData["postgres:client:password"]);
        Assert.Equal("test-port", configurationData["postgres:client:port"]);
        Assert.Equal("test-username", configurationData["postgres:client:username"]);
        Assert.Equal("test-jdbc-url", configurationData["postgres:client:jdbcUrl"]);
        Assert.Equal("verify-full", configurationData["postgres:client:sslmode"]);
        Assert.Equal("root.cert", configurationData["postgres:client:sslrootcert"]);
        Assert.Equal("--cluster=routing-id&opt=val1", configurationData["postgres:client:options"]);
    }
}
