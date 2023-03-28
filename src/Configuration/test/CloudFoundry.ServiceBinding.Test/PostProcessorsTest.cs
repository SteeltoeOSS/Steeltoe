// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Xunit;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.Test;

public sealed class PostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void PostgreSqlTest_BindingTypeDisabled()
    {
        var postProcessor = new PostgreSqlPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(PostgreSqlPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);
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
            Tuple.Create("credentials:hostname", "test-host"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:name", "test-database"),
            Tuple.Create("credentials:username", "test-username"),
            Tuple.Create("credentials:password", "test-password"),
            Tuple.Create("credentials:sslcert", "test-ssl-cert"),
            Tuple.Create("credentials:sslkey", "test-ssl-key"),
            Tuple.Create("credentials:sslrootcert", "test-ssl-root-cert")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(PostgreSqlPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, PostgreSqlPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, PostgreSqlPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        GetFileContentAtKey(configurationData, $"{keyPrefix}:SSL Certificate").Should().Be("test-ssl-cert");
        GetFileContentAtKey(configurationData, $"{keyPrefix}:SSL Key").Should().Be("test-ssl-key");
        GetFileContentAtKey(configurationData, $"{keyPrefix}:Root Certificate").Should().Be("test-ssl-root-cert");
    }

    [Fact]
    public void MySqlTest_BindingTypeDisabled()
    {
        var postProcessor = new MySqlPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(MySqlPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);
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
            Tuple.Create("credentials:hostname", "test-host"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:name", "test-database"),
            Tuple.Create("credentials:username", "test-username"),
            Tuple.Create("credentials:password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(MySqlPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);
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
    public void SqlServerTest_BindingTypeDisabled()
    {
        var postProcessor = new SqlServerPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(SqlServerPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SqlServerPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:password");
    }

    [Fact]
    public void SqlServerTest_BindingTypeEnabled()
    {
        var postProcessor = new SqlServerPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:hostname", "test-host"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:name", "test-database"),
            Tuple.Create("credentials:username", "test-username"),
            Tuple.Create("credentials:password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(SqlServerPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SqlServerPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:Data Source"].Should().Be("test-host,test-port");
        configurationData[$"{keyPrefix}:Initial Catalog"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:User ID"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:Password"].Should().Be("test-password");
    }

    [Fact]
    public void MongoDbTest_BindingTypeDisabled()
    {
        var postProcessor = new MongoDbPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:uri", "test-uri")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(MongoDbPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MongoDbPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:url");
    }

    [Fact]
    public void MongoDbTest_BindingTypeEnabled()
    {
        var postProcessor = new MongoDbPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:uri", "test-uri")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(MongoDbPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MongoDbPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:url"].Should().Be("test-uri");
    }
}
