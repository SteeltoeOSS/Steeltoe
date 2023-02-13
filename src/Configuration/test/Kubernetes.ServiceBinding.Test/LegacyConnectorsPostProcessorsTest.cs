// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
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
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingTypeKey, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        configurationData["rabbitmq:client:uri"].Should().Be("test-addresses");
        configurationData["rabbitmq:client:server"].Should().Be("test-host");
        configurationData["rabbitmq:client:password"].Should().Be("test-password");
        configurationData["rabbitmq:client:port"].Should().Be("test-port");
        configurationData["rabbitmq:client:username"].Should().Be("test-username");
        configurationData["rabbitmq:client:virtualhost"].Should().Be("test-virtual-host");
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
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingTypeKey, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        configurationData["mysql:client:database"].Should().Be("test-database");
        configurationData["mysql:client:server"].Should().Be("test-host");
        configurationData["mysql:client:password"].Should().Be("test-password");
        configurationData["mysql:client:port"].Should().Be("test-port");
        configurationData["mysql:client:username"].Should().Be("test-username");
        configurationData["mysql:client:jdbcUrl"].Should().Be("test-jdbc-url");
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
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, PostgreSqlPostProcessor.BindingTypeKey, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        configurationData["postgres:client:database"].Should().Be("test-database");
        configurationData["postgres:client:host"].Should().Be("test-host");
        configurationData["postgres:client:password"].Should().Be("test-password");
        configurationData["postgres:client:port"].Should().Be("test-port");
        configurationData["postgres:client:username"].Should().Be("test-username");
        configurationData["postgres:client:jdbcUrl"].Should().Be("test-jdbc-url");
        configurationData["postgres:client:sslmode"].Should().Be("verify-full");
        configurationData["postgres:client:sslrootcert"].Should().Be("root.cert");
        configurationData["postgres:client:options"].Should().Be("--cluster=routing-id&opt=val1");
    }
}
