// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

namespace Steeltoe.Configuration.CloudFoundry.Test.ServiceBindings;

public sealed class PostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void Processes_MySql_configuration()
    {
        var postProcessor = new MySqlCloudFoundryPostProcessor();

        Tuple<string, string>[] secrets =
        [
            Tuple.Create("credentials:hostname", "test-host"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:name", "test-database"),
            Tuple.Create("credentials:username", "test-username"),
            Tuple.Create("credentials:password", "test-password")
        ];

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [MySqlCloudFoundryPostProcessor.BindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MySqlCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
    }

    [Fact]
    public void Processes_PostgreSql_configuration()
    {
        var postProcessor = new PostgreSqlCloudFoundryPostProcessor();

        Tuple<string, string>[] secrets =
        [
            Tuple.Create("credentials:hostname", "test-host"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:name", "test-database"),
            Tuple.Create("credentials:username", "test-username"),
            Tuple.Create("credentials:password", "test-password"),
            Tuple.Create("credentials:sslCert", "test-ssl-cert"),
            Tuple.Create("credentials:sslKey", "test-ssl-key"),
            Tuple.Create("credentials:sslrootcert", "test-ssl-root-cert") // tests case-insensitivity
        ];

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [PostgreSqlCloudFoundryPostProcessor.BindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

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
    public void Processes_RabbitMQ_configuration()
    {
        var postProcessor = new RabbitMQCloudFoundryPostProcessor();

        Tuple<string, string>[] secrets =
        [
            Tuple.Create("credentials:ssl", "false"),
            Tuple.Create("credentials:protocols:amqp:host", "test-host"),
            Tuple.Create("credentials:protocols:amqp:port", "test-port"),
            Tuple.Create("credentials:protocols:amqp:username", "test-username"),
            Tuple.Create("credentials:protocols:amqp:password", "test-password"),
            Tuple.Create("credentials:protocols:amqp:vhost", "test-vhost")
        ];

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [RabbitMQCloudFoundryPostProcessor.BindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

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
    public void Processes_Redis_configuration()
    {
        var postProcessor = new RedisCloudFoundryPostProcessor();

        Tuple<string, string>[] secrets =
        [
            Tuple.Create("credentials:host", "test-host"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:password", "test-password")
        ];

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [RedisCloudFoundryPostProcessor.BindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RedisCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
    }

    [Fact]
    public void Processes_Redis_configuration_AzureBroker()
    {
        var postProcessor = new RedisCloudFoundryPostProcessor();

        Tuple<string, string>[] secrets =
        [
            Tuple.Create("credentials:host", "test-host"),
            Tuple.Create("credentials:tls_port", "test-port"),
            Tuple.Create("credentials:password", "test-password")
        ];

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [RedisCloudFoundryPostProcessor.BindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RedisCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:ssl"].Should().Be("true");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
    }

    [Fact]
    public void Processes_SqlServer_configuration()
    {
        var postProcessor = new SqlServerCloudFoundryPostProcessor();

        Tuple<string, string>[] secrets =
        [
            Tuple.Create("credentials:hostname", "test-host"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:name", "test-database"),
            Tuple.Create("credentials:username", "test-username"),
            Tuple.Create("credentials:password", "test-password")
        ];

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [SqlServerCloudFoundryPostProcessor.BindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SqlServerCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:Data Source"].Should().Be("test-host,test-port");
        configurationData[$"{keyPrefix}:Initial Catalog"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:User ID"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:Password"].Should().Be("test-password");
    }

    [Fact]
    public void Processes_MongoDb_configuration()
    {
        var postProcessor = new MongoDbCloudFoundryPostProcessor();

        Tuple<string, string>[] secrets = [Tuple.Create("credentials:uri", "mongodb://localhost:27017/auth-db?appname=sample")];

        Dictionary<string, string?> configurationData =
            GetConfigurationData("csb-azure-mongodb", TestBindingName, [MongoDbCloudFoundryPostProcessor.BindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MongoDbCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:url"].Should().Be("mongodb://localhost:27017/auth-db?appname=sample");
        configurationData[$"{keyPrefix}:database"].Should().Be("auth-db");
    }

    [Fact]
    public void Processes_CosmosDb_configuration()
    {
        var postProcessor = new CosmosDbCloudFoundryPostProcessor();

        Tuple<string, string>[] secrets =
        [
            Tuple.Create("credentials:cosmosdb_host_endpoint", "test-endpoint"),
            Tuple.Create("credentials:cosmosdb_master_key", "test-key"),
            Tuple.Create("credentials:cosmosdb_database_id", "test-database")
        ];

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [CosmosDbCloudFoundryPostProcessor.BindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, CosmosDbCloudFoundryPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:accountEndpoint"].Should().Be("test-endpoint");
        configurationData[$"{keyPrefix}:accountKey"].Should().Be("test-key");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
    }

    [Fact]
    public void Processes_Eureka_configuration()
    {
        var postProcessor = new EurekaCloudFoundryPostProcessor(NullLogger<EurekaCloudFoundryPostProcessor>.Instance);

        Tuple<string, string>[] secrets =
        [
            Tuple.Create("credentials:uri", "test-uri"),
            Tuple.Create("credentials:client_id", "test-client-id"),
            Tuple.Create("credentials:client_secret", "test-client-secret"),
            Tuple.Create("credentials:access_token_uri", "test-access-token-uri")
        ];

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [EurekaCloudFoundryPostProcessor.BindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        const string keyPrefix = EurekaCloudFoundryPostProcessor.EurekaConfigurationKeyPrefix;
        configurationData[$"{keyPrefix}:ServiceUrl"].Should().Be("test-uri/eureka/");
        configurationData[$"{keyPrefix}:ClientId"].Should().Be("test-client-id");
        configurationData[$"{keyPrefix}:ClientSecret"].Should().Be("test-client-secret");
        configurationData[$"{keyPrefix}:AccessTokenUri"].Should().Be("test-access-token-uri");
        configurationData[$"{keyPrefix}:Enabled"].Should().Be("true");
    }

    [Fact]
    public void Processes_Identity_configuration()
    {
        var postProcessor = new IdentityCloudFoundryPostProcessor(NullLogger<IdentityCloudFoundryPostProcessor>.Instance);

        Tuple<string, string>[] secrets =
        [
            Tuple.Create("credentials:auth_domain", "test-domain"),
            Tuple.Create("credentials:client_id", "test-id"),
            Tuple.Create("credentials:client_secret", "test-secret")
        ];

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [], IdentityCloudFoundryPostProcessor.BindingType, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        foreach (string scheme in IdentityCloudFoundryPostProcessor.AuthenticationSchemes)
        {
            string keyPrefix = $"{IdentityCloudFoundryPostProcessor.AuthenticationConfigurationKeyPrefix}:{scheme}";
            configurationData[$"{keyPrefix}:Authority"].Should().Be("test-domain");
            configurationData[$"{keyPrefix}:ClientId"].Should().Be("test-id");
            configurationData[$"{keyPrefix}:ClientSecret"].Should().Be("test-secret");
        }
    }
}
