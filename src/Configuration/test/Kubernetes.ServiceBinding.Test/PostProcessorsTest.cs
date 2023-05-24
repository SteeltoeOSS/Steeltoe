// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class PostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void MySqlTest_BindingTypeDisabled()
    {
        var postProcessor = new MySqlKubernetesPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MySqlKubernetesPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MySqlKubernetesPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MySqlKubernetesPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:username");
    }

    [Fact]
    public void MySqlTest_BindingTypeEnabled()
    {
        var postProcessor = new MySqlKubernetesPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MySqlKubernetesPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MySqlKubernetesPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MySqlKubernetesPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeDisabled()
    {
        var postProcessor = new PostgreSqlKubernetesPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, PostgreSqlKubernetesPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, PostgreSqlKubernetesPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, PostgreSqlKubernetesPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:username");
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeEnabled()
    {
        var postProcessor = new PostgreSqlKubernetesPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, PostgreSqlKubernetesPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, PostgreSqlKubernetesPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, PostgreSqlKubernetesPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
    }

    [Fact]
    public void RabbitMQTest_BindingTypeDisabled()
    {
        var postProcessor = new RabbitMQKubernetesPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RabbitMQKubernetesPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQKubernetesPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RabbitMQKubernetesPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:username");
    }

    [Fact]
    public void RabbitMQTest_BindingTypeEnabled()
    {
        var postProcessor = new RabbitMQKubernetesPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("virtual-host", "test-virtual-host")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RabbitMQKubernetesPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQKubernetesPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RabbitMQKubernetesPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:virtualHost"].Should().Be("test-virtual-host");
    }

    [Fact]
    public void RedisTest_BindingTypeDisabled()
    {
        var postProcessor = new RedisKubernetesPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RedisKubernetesPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RedisKubernetesPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RedisKubernetesPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:host");
    }

    [Fact]
    public void RedisTest_BindingTypeEnabled()
    {
        var postProcessor = new RedisKubernetesPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("client-name", "test-client-name")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RedisKubernetesPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RedisKubernetesPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RedisKubernetesPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:ssl"].Should().Be("test-ssl");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:defaultDatabase"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:name"].Should().Be("test-client-name");
    }
}
