// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;
using Xunit;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.Test;

public sealed class PostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void PostgreSqlTest_BindingTypeDisabled()
    {
        var postProcessor = new PostgreSqlCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:username", "test-username")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(PostgreSqlCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, PostgreSqlCloudFoundryPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, PostgreSqlCloudFoundryPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:username");
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeEnabled()
    {
        var postProcessor = new PostgreSqlCloudFoundryPostProcessor();

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

        Dictionary<string, string> configurationData =
            GetConfigurationData(PostgreSqlCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, PostgreSqlCloudFoundryPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, PostgreSqlCloudFoundryPostProcessor.BindingType);
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
        var postProcessor = new MySqlCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:username", "test-username")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(MySqlCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MySqlCloudFoundryPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MySqlCloudFoundryPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:username");
    }

    [Fact]
    public void MySqlTest_BindingTypeEnabled()
    {
        var postProcessor = new MySqlCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:hostname", "test-host"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:name", "test-database"),
            Tuple.Create("credentials:username", "test-username"),
            Tuple.Create("credentials:password", "test-password")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(MySqlCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MySqlCloudFoundryPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MySqlCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
    }

    [Fact]
    public void SqlServerTest_BindingTypeDisabled()
    {
        var postProcessor = new SqlServerCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:password", "test-password")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(SqlServerCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SqlServerCloudFoundryPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SqlServerCloudFoundryPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:password");
    }

    [Fact]
    public void SqlServerTest_BindingTypeEnabled()
    {
        var postProcessor = new SqlServerCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:hostname", "test-host"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:name", "test-database"),
            Tuple.Create("credentials:username", "test-username"),
            Tuple.Create("credentials:password", "test-password")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(SqlServerCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SqlServerCloudFoundryPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SqlServerCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:Data Source"].Should().Be("test-host,test-port");
        configurationData[$"{keyPrefix}:Initial Catalog"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:User ID"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:Password"].Should().Be("test-password");
    }

    [Fact]
    public void MongoDbTest_BindingTypeDisabled()
    {
        var postProcessor = new MongoDbCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:uri", "test-uri")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(MongoDbCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MongoDbCloudFoundryPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MongoDbCloudFoundryPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:url");
    }

    [Fact]
    public void MongoDbTest_BindingTypeEnabled()
    {
        var postProcessor = new MongoDbCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:uri", "mongodb://localhost:27017/auth-db?appname=sample")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(MongoDbCloudFoundryPostProcessor.BindingType, "csb-azure-mongodb", TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MongoDbCloudFoundryPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MongoDbCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:url"].Should().Be("mongodb://localhost:27017/auth-db?appname=sample");
        configurationData[$"{keyPrefix}:database"].Should().Be("auth-db");
    }

    [Fact]
    public void CosmosDbTest_BindingTypeDisabled()
    {
        var postProcessor = new CosmosDbCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:cosmosdb_host_endpoint", "test-endpoint")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(CosmosDbCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, CosmosDbCloudFoundryPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, CosmosDbCloudFoundryPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:accountEndpoint");
    }

    [Fact]
    public void CosmosDbTest_BindingTypeEnabled()
    {
        var postProcessor = new CosmosDbCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:cosmosdb_host_endpoint", "test-endpoint"),
            Tuple.Create("credentials:cosmosdb_master_key", "test-key"),
            Tuple.Create("credentials:cosmosdb_database_id", "test-database")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(CosmosDbCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, CosmosDbCloudFoundryPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, CosmosDbCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:accountEndpoint"].Should().Be("test-endpoint");
        configurationData[$"{keyPrefix}:accountKey"].Should().Be("test-key");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
    }

    [Fact]
    public void RabbitMQTest_BindingTypeDisabled()
    {
        var postProcessor = new RabbitMQCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:ssl", "false")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(RabbitMQCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQCloudFoundryPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RabbitMQCloudFoundryPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:useTls");
    }

    [Fact]
    public void RabbitMQTest_BindingTypeEnabled()
    {
        var postProcessor = new RabbitMQCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:ssl", "false"),
            Tuple.Create("credentials:protocols:amqp:host", "test-host"),
            Tuple.Create("credentials:protocols:amqp:port", "test-port"),
            Tuple.Create("credentials:protocols:amqp:username", "test-username"),
            Tuple.Create("credentials:protocols:amqp:password", "test-password"),
            Tuple.Create("credentials:protocols:amqp:vhost", "test-vhost")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(RabbitMQCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQCloudFoundryPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RabbitMQCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:useTls"].Should().Be("false");
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:virtualHost"].Should().Be("test-vhost");
    }

    [Fact]
    public void RabbitMQTest_BindingTypeEnabled_Tls()
    {
        var postProcessor = new RabbitMQCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:ssl", "true"),
            Tuple.Create("credentials:protocols:amqp+ssl:host", "test-host"),
            Tuple.Create("credentials:protocols:amqp+ssl:port", "test-port"),
            Tuple.Create("credentials:protocols:amqp+ssl:username", "test-username"),
            Tuple.Create("credentials:protocols:amqp+ssl:password", "test-password"),
            Tuple.Create("credentials:protocols:amqp+ssl:vhost", "test-vhost")
        };

        Dictionary<string, string> configurationData =
            GetConfigurationData(RabbitMQCloudFoundryPostProcessor.BindingType, TestProviderName, TestBindingName, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQCloudFoundryPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RabbitMQCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:useTls"].Should().Be("true");
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:virtualHost"].Should().Be("test-vhost");
    }
}
