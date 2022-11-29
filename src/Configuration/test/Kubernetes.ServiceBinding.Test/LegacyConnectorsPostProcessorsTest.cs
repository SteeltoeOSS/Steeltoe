// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;
public class LegacyConnectorsPostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void RabbitMQTest_BindingTypeEnabled()
    {
        var postProcessor = new RabbitMQLegacyConnectorPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            RabbitMQPostProcessor.BindingTypeKey,
            Tuple.Create("addresses", "test-addresses"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("virtual-host", "test-virtual-host"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-addresses", configData[$"rabbitmq:client:uri"]);
        Assert.Equal("test-host", configData[$"rabbitmq:client:server"]);
        Assert.Equal("test-password", configData[$"rabbitmq:client:password"]);
        Assert.Equal("test-port", configData[$"rabbitmq:client:port"]);
        Assert.Equal("test-username", configData[$"rabbitmq:client:username"]);
        Assert.Equal("test-virtual-host", configData[$"rabbitmq:client:virtualhost"]);
    }

    [Fact]
    public void MySqlTest_BindingTypeEnabled()
    {
        var postProcessor = new MySqlLegacyConnectorPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            MySqlPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"mysql:client:database"]);
        Assert.Equal("test-host", configData[$"mysql:client:server"]);
        Assert.Equal("test-password", configData[$"mysql:client:password"]);
        Assert.Equal("test-port", configData[$"mysql:client:port"]);
        Assert.Equal("test-username", configData[$"mysql:client:username"]);
        Assert.Equal("test-jdbc-url", configData[$"mysql:client:jdbcUrl"]);
    }

}
