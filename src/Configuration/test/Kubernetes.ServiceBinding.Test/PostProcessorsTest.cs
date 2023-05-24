// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class PostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void MySqlTest_BindingTypeDisabled()
    {
        var postProcessor = new MySqlPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MySqlPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MySqlPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:username");
    }

    [Fact]
    public void MySqlTest_BindingTypeEnabled()
    {
        var postProcessor = new MySqlPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MySqlPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MySqlPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeDisabled()
    {
        var postProcessor = new PostgreSqlPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, PostgreSqlPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, PostgreSqlPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, PostgreSqlPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:username");
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeEnabled()
    {
        var postProcessor = new PostgreSqlPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, PostgreSqlPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, PostgreSqlPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, PostgreSqlPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
    }

    [Fact]
    public void RabbitMQTest_BindingTypeDisabled()
    {
        var postProcessor = new RabbitMQPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RabbitMQPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RabbitMQPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:username");
    }

    [Fact]
    public void RabbitMQTest_BindingTypeEnabled()
    {
        var postProcessor = new RabbitMQPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("virtual-host", "test-virtual-host")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RabbitMQPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RabbitMQPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:virtualHost"].Should().Be("test-virtual-host");
    }

    [Fact]
    public void RedisTest_BindingTypeDisabled()
    {
        var postProcessor = new RedisPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RedisPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RedisPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:host");
    }

    [Fact]
    public void RedisTest_BindingTypeEnabled()
    {
        var postProcessor = new RedisPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("client-name", "test-client-name")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RedisPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RedisPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:ssl"].Should().Be("test-ssl");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:defaultDatabase"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:name"].Should().Be("test-client-name");
    }
}
