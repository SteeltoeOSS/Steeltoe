// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class PostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void ArtemisTest_BindingTypeDisabled()
    {
        var postProcessor = new ArtemisPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("mode", "EMBEDDED"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("user", "test-user"),
            Tuple.Create("password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ArtemisPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, ArtemisPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, ArtemisPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:host");
    }

    [Fact]
    public void ArtemisTest_BindingTypeEnabled()
    {
        var postProcessor = new ArtemisPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("mode", "EMBEDDED"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("user", "test-user"),
            Tuple.Create("password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ArtemisPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, ArtemisPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, ArtemisPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:mode"].Should().Be("EMBEDDED");
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:user"].Should().Be("test-user");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
    }

    [Fact]
    public void CassandraTest_BindingTypeDisabled()
    {
        var postProcessor = new CassandraPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("cluster-name", "test-cluster-name"),
            Tuple.Create("compression", "test-compression"),
            Tuple.Create("contact-points", "test-contact-points"),
            Tuple.Create("keyspace-name", "test-keyspace-name"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, CassandraPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, CassandraPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, CassandraPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:ssl");
    }

    [Fact]
    public void CassandraTest_BindingTypeEnabled()
    {
        var postProcessor = new CassandraPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("cluster-name", "test-cluster-name"),
            Tuple.Create("compression", "test-compression"),
            Tuple.Create("contact-points", "test-contact-points"),
            Tuple.Create("keyspace-name", "test-keyspace-name"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, CassandraPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, CassandraPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, CassandraPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:clusterName"].Should().Be("test-cluster-name");
        configurationData[$"{keyPrefix}:compression"].Should().Be("test-compression");
        configurationData[$"{keyPrefix}:contactPoints"].Should().Be("test-contact-points");
        configurationData[$"{keyPrefix}:keyspaceName"].Should().Be("test-keyspace-name");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:ssl"].Should().Be("test-ssl");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
    }

    [Fact]
    public void ConfigServerTest_BindingTypeDisabled()
    {
        var postProcessor = new ConfigServerPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("client-id", "test-client-id"),
            Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ConfigServerPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, ConfigServerPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, ConfigServerPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:uri");
    }

    [Fact]
    public void ConfigServerTest_BindingTypeEnabled()
    {
        var postProcessor = new ConfigServerPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("client-id", "test-client-id"),
            Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ConfigServerPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, ConfigServerPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, ConfigServerPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:oauth2:clientId"].Should().Be("test-client-id");
        configurationData[$"{keyPrefix}:oauth2:clientSecret"].Should().Be("test-client-secret");
        configurationData[$"{keyPrefix}:oauth2:accessTokenUri"].Should().Be("test-access-token-uri");
    }

    [Fact]
    public void CouchbaseTest_BindingTypeDisabled()
    {
        var postProcessor = new CouchbasePostProcessor();

        var secrets = new[]
        {
            Tuple.Create("bootstrap-hosts", "test-bootstrap-hosts"),
            Tuple.Create("bucket.name", "test-bucket-name"),
            Tuple.Create("bucket.password", "test-bucket-password"),
            Tuple.Create("env.bootstrap.http-direct-port", "test-env-bootstrap-http-direct-port"),
            Tuple.Create("env.bootstrap.http-ssl-port", "test-env-bootstrap-http-ssl-port"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, CouchbasePostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, CouchbasePostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, CouchbasePostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:bootstrapHosts");
    }

    [Fact]
    public void CouchbaseTest_BindingTypeEnabled()
    {
        var postProcessor = new CouchbasePostProcessor();

        var secrets = new[]
        {
            Tuple.Create("bootstrap-hosts", "test-bootstrap-hosts"),
            Tuple.Create("bucket.name", "test-bucket-name"),
            Tuple.Create("bucket.password", "test-bucket-password"),
            Tuple.Create("env.bootstrap.http-direct-port", "test-env-bootstrap-http-direct-port"),
            Tuple.Create("env.bootstrap.http-ssl-port", "test-env-bootstrap-http-ssl-port"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, CouchbasePostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, CouchbasePostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, CouchbasePostProcessor.BindingType);
        configurationData[$"{keyPrefix}:bootstrapHosts"].Should().Be("test-bootstrap-hosts");
        configurationData[$"{keyPrefix}:bucketName"].Should().Be("test-bucket-name");
        configurationData[$"{keyPrefix}:bucketPassword"].Should().Be("test-bucket-password");
        configurationData[$"{keyPrefix}:envBootstrapHttpDirectPort"].Should().Be("test-env-bootstrap-http-direct-port");
        configurationData[$"{keyPrefix}:envBootstrapHttpSslPort"].Should().Be("test-env-bootstrap-http-ssl-port");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
    }

    [Fact]
    public void DB2Test_BindingTypeDisabled()
    {
        var postProcessor = new DB2PostProcessor();

        var secrets = new[]
        {
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, DB2PostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, DB2PostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, DB2PostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:database");
    }

    [Fact]
    public void DB2Test_BindingTypeEnabled()
    {
        var postProcessor = new DB2PostProcessor();

        var secrets = new[]
        {
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, DB2PostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, DB2PostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, DB2PostProcessor.BindingType);
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:jdbcUrl"].Should().Be("test-jdbc-url");
    }

    [Fact]
    public void ElasticSearchTest_BindingTypeDisabled()
    {
        var postProcessor = new ElasticSearchPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("endpoints", "test-endpoints"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("use-ssl", "test-use-ssl"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("proxy.host", "test-proxy-host"),
            Tuple.Create("proxy.port", "test-proxy-port"),
            Tuple.Create("uris", "test-uris")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ElasticSearchPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, ElasticSearchPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, ElasticSearchPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:endpoints");
    }

    [Fact]
    public void ElasticSearchTest_BindingTypeEnabled()
    {
        var postProcessor = new ElasticSearchPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("endpoints", "test-endpoints"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("use-ssl", "test-use-ssl"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("proxy.host", "test-proxy-host"),
            Tuple.Create("proxy.port", "test-proxy-port"),
            Tuple.Create("uris", "test-uris")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ElasticSearchPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, ElasticSearchPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, ElasticSearchPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:endpoints"].Should().Be("test-endpoints");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:useSsl"].Should().Be("test-use-ssl");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:proxyHost"].Should().Be("test-proxy-host");
        configurationData[$"{keyPrefix}:proxyPort"].Should().Be("test-proxy-port");
        configurationData[$"{keyPrefix}:uris"].Should().Be("test-uris");
    }

    [Fact]
    public void EurekaTest_BindingTypeDisabled()
    {
        var postProcessor = new EurekaPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("client-id", "test-client-id"),
            Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, EurekaPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, EurekaPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, EurekaPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:uri");
    }

    [Fact]
    public void EurekaTest_BindingTypeEnabled()
    {
        var postProcessor = new EurekaPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("client-id", "test-client-id"),
            Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, EurekaPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, EurekaPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, EurekaPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:region"].Should().Be("default");
        configurationData[$"{keyPrefix}:oauth2:clientId"].Should().Be("test-client-id");
        configurationData[$"{keyPrefix}:oauth2:clientSecret"].Should().Be("test-client-secret");
        configurationData[$"{keyPrefix}:oauth2:accessTokenUri"].Should().Be("test-access-token-uri");
    }

    [Fact]
    public void KafkaTest_BindingTypeDisabled()
    {
        var postProcessor = new KafkaPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("bootstrap-servers", "test-bootstrap-servers"),
            Tuple.Create("consumer.bootstrap-servers", "test-consumer-bootstrap-servers"),
            Tuple.Create("producer.bootstrap-servers", "test-producer-bootstrap-servers"),
            Tuple.Create("streams.bootstrap-servers", "test-streams-bootstrap-servers")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, KafkaPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, KafkaPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, KafkaPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:bootstrapServers");
    }

    [Fact]
    public void KafkaTest_BindingTypeEnabled()
    {
        var postProcessor = new KafkaPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("bootstrap-servers", "test-bootstrap-servers"),
            Tuple.Create("consumer.bootstrap-servers", "test-consumer-bootstrap-servers"),
            Tuple.Create("producer.bootstrap-servers", "test-producer-bootstrap-servers"),
            Tuple.Create("streams.bootstrap-servers", "test-streams-bootstrap-servers")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, KafkaPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, KafkaPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, KafkaPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:bootstrapServers"].Should().Be("test-bootstrap-servers");
        configurationData[$"{keyPrefix}:consumerBootstrapServers"].Should().Be("test-consumer-bootstrap-servers");
        configurationData[$"{keyPrefix}:producerBootstrapServers"].Should().Be("test-producer-bootstrap-servers");
        configurationData[$"{keyPrefix}:streamsBootstrapServers"].Should().Be("test-streams-bootstrap-servers");
    }

    [Fact]
    public void LdapTest_BindingTypeDisabled()
    {
        var postProcessor = new LdapPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("urls", "test-urls"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("base", "test-base")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, LdapPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, LdapPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, LdapPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:urls");
    }

    [Fact]
    public void LdapTest_BindingTypeEnabled()
    {
        var postProcessor = new LdapPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("urls", "test-urls"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("base", "test-base")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, LdapPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, LdapPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, LdapPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:urls"].Should().Be("test-urls");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:base"].Should().Be("test-base");
    }

    [Fact]
    public void MongoDbTest_BindingTypeDisabled()
    {
        var postProcessor = new MongoDbPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MongoDbPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MongoDbPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:username");
    }

    [Fact]
    public void MongoDbTest_BindingTypeEnabled()
    {
        var postProcessor = new MongoDbPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("authentication-database", "test-authentication-database"),
            Tuple.Create("database", "test-database")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MongoDbPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, MongoDbPostProcessor.BindingType);

        configurationData[$"{keyPrefix}:url"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:server"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:authenticationDatabase"].Should().Be("test-authentication-database");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
    }

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
    public void Neo4JTest_BindingTypeDisabled()
    {
        var postProcessor = new Neo4JPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("password", "test-password"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, Neo4JPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, Neo4JPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, Neo4JPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:password");
    }

    [Fact]
    public void Neo4JTest_BindingTypeEnabled()
    {
        var postProcessor = new Neo4JPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("password", "test-password"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, Neo4JPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, Neo4JPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, Neo4JPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
    }

    [Fact]
    public void OracleTest_BindingTypeDisabled()
    {
        var postProcessor = new OraclePostProcessor();

        var secrets = new[]
        {
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, OraclePostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, OraclePostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, OraclePostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:database");
    }

    [Fact]
    public void OracleTest_BindingTypeEnabled()
    {
        var postProcessor = new OraclePostProcessor();

        var secrets = new[]
        {
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, OraclePostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, OraclePostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, OraclePostProcessor.BindingType);
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:jdbcUrl"].Should().Be("test-jdbc-url");
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
            Tuple.Create("addresses", "test-addresses"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("virtual-host", "test-virtual-host")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RabbitMQPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RabbitMQPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:addresses");
    }

    [Fact]
    public void RabbitMQTest_BindingTypeEnabled()
    {
        var postProcessor = new RabbitMQPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("addresses", "test-addresses"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("virtual-host", "test-virtual-host")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RabbitMQPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RabbitMQPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:addresses"].Should().Be("test-addresses");
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:virtualHost"].Should().Be("test-virtual-host");
    }

    [Fact]
    public void RedisTest_BindingTypeDisabled()
    {
        var postProcessor = new RedisPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("client-name", "test-client-name"),
            Tuple.Create("cluster.max-redirects", "test-cluster-max-redirects"),
            Tuple.Create("cluster.nodes", "test-cluster-nodes"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("sentinel.master", "test-sentinel-master"),
            Tuple.Create("sentinel.nodes", "test-sentinel-nodes"),
            Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("url", "test-url")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RedisPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RedisPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:clientName");
    }

    [Fact]
    public void RedisTest_BindingTypeEnabled()
    {
        var postProcessor = new RedisPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("client-name", "test-client-name"),
            Tuple.Create("cluster.max-redirects", "test-cluster-max-redirects"),
            Tuple.Create("cluster.nodes", "test-cluster-nodes"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("sentinel.master", "test-sentinel-master"),
            Tuple.Create("sentinel.nodes", "test-sentinel-nodes"),
            Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("url", "test-url")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RedisPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, RedisPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:clientName"].Should().Be("test-client-name");
        configurationData[$"{keyPrefix}:clusterMaxRedirects"].Should().Be("test-cluster-max-redirects");
        configurationData[$"{keyPrefix}:clusterNodes"].Should().Be("test-cluster-nodes");
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:sentinelMaster"].Should().Be("test-sentinel-master");
        configurationData[$"{keyPrefix}:sentinelNodes"].Should().Be("test-sentinel-nodes");
        configurationData[$"{keyPrefix}:ssl"].Should().Be("test-ssl");
        configurationData[$"{keyPrefix}:url"].Should().Be("test-url");
    }

    [Fact]
    public void SapHanaTest_BindingTypeDisabled()
    {
        var postProcessor = new SapHanaPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SapHanaPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SapHanaPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SapHanaPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:database");
    }

    [Fact]
    public void SapHanaTest_BindingTypeEnabled()
    {
        var postProcessor = new SapHanaPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SapHanaPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SapHanaPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SapHanaPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:database"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:host"].Should().Be("test-host");
        configurationData[$"{keyPrefix}:password"].Should().Be("test-password");
        configurationData[$"{keyPrefix}:port"].Should().Be("test-port");
        configurationData[$"{keyPrefix}:username"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:jdbcUrl"].Should().Be("test-jdbc-url");
    }

    [Fact]
    public void SpringSecurityOauth2Test_BindingTypeDisabled()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        var secrets1 = new[]
        {
            Tuple.Create("client-id", "github-client-id"),
            Tuple.Create("client-secret", "github-client-secret")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName1, SpringSecurityOAuth2PostProcessor.BindingType, "github",
            secrets1);

        var secrets2 = new[]
        {
            Tuple.Create("client-id", "okta-client-id"),
            Tuple.Create("client-secret", "okta-client-secret"),
            Tuple.Create("issuer-uri", "okta-issuer-uri")
        };

        AddConfigurationData(configurationData, TestBindingName2, SpringSecurityOAuth2PostProcessor.BindingType, "okta", secrets2);

        var secrets3 = new[]
        {
            Tuple.Create("client-id", "my-provider-client-id"),
            Tuple.Create("client-secret", "my-provider-client-secret"),
            Tuple.Create("client-authentication-method", "my-provider-client-authentication-method"),
            Tuple.Create("authorization-grant-type", "my-provider-authorization-grant-type"),
            Tuple.Create("redirect-uri", "my-provider-redirect-uri"),
            Tuple.Create("scope", "my-provider-scope1,my-provider-scope2"),
            Tuple.Create("client-name", "my-provider-client-name"),
            Tuple.Create("authorization-uri", "my-provider-authorization-uri"),
            Tuple.Create("token-uri", "my-provider-token-uri"),
            Tuple.Create("user-info-uri", "my-provider-user-info-uri"),
            Tuple.Create("user-info-authentication-method", "my-provider-user-info-authentication-method"),
            Tuple.Create("jwk-set-uri", "my-provider-jwk-set-uri"),
            Tuple.Create("user-name-attribute", "my-provider-user-name-attribute")
        };

        AddConfigurationData(configurationData, TestBindingName3, SpringSecurityOAuth2PostProcessor.BindingType, "my-provider", secrets3);

        var secrets4 = new[]
        {
            Tuple.Create("client-id", "my-provider-client-id")
        };

        AddConfigurationData(configurationData, TestMissingProvider, SpringSecurityOAuth2PostProcessor.BindingType, secrets4);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SpringSecurityOAuth2PostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:registration:clientId");
    }

    [Fact]
    public void SpringSecurityOauth2Test_CommonProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        var secrets1 = new[]
        {
            Tuple.Create("client-id", "github-client-id"),
            Tuple.Create("client-secret", "github-client-secret")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName1, SpringSecurityOAuth2PostProcessor.BindingType, "github",
            secrets1);

        var secrets2 = new[]
        {
            Tuple.Create("client-id", "okta-client-id"),
            Tuple.Create("client-secret", "okta-client-secret"),
            Tuple.Create("issuer-uri", "okta-issuer-uri")
        };

        AddConfigurationData(configurationData, TestBindingName2, SpringSecurityOAuth2PostProcessor.BindingType, "okta", secrets2);

        var secrets3 = new[]
        {
            Tuple.Create("client-id", "my-provider-client-id"),
            Tuple.Create("client-secret", "my-provider-client-secret"),
            Tuple.Create("client-authentication-method", "my-provider-client-authentication-method"),
            Tuple.Create("authorization-grant-type", "my-provider-authorization-grant-type"),
            Tuple.Create("redirect-uri", "my-provider-redirect-uri"),
            Tuple.Create("scope", "my-provider-scope1,my-provider-scope2"),
            Tuple.Create("client-name", "my-provider-client-name"),
            Tuple.Create("authorization-uri", "my-provider-authorization-uri"),
            Tuple.Create("token-uri", "my-provider-token-uri"),
            Tuple.Create("user-info-uri", "my-provider-user-info-uri"),
            Tuple.Create("user-info-authentication-method", "my-provider-user-info-authentication-method"),
            Tuple.Create("jwk-set-uri", "my-provider-jwk-set-uri"),
            Tuple.Create("user-name-attribute", "my-provider-user-name-attribute")
        };

        AddConfigurationData(configurationData, TestBindingName3, SpringSecurityOAuth2PostProcessor.BindingType, "my-provider", secrets3);

        var secrets4 = new[]
        {
            Tuple.Create("client-id", "my-provider-client-id")
        };

        AddConfigurationData(configurationData, TestMissingProvider, SpringSecurityOAuth2PostProcessor.BindingType, secrets4);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName1, SpringSecurityOAuth2PostProcessor.BindingType);
        configurationData[$"{keyPrefix}:registration:clientId"].Should().Be("github-client-id");
        configurationData[$"{keyPrefix}:registration:clientSecret"].Should().Be("github-client-secret");
        configurationData[$"{keyPrefix}:registration:provider"].Should().Be("github");
    }

    [Fact]
    public void SpringSecurityOauth2Test_OIDCProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        var secrets1 = new[]
        {
            Tuple.Create("client-id", "github-client-id"),
            Tuple.Create("client-secret", "github-client-secret")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName1, SpringSecurityOAuth2PostProcessor.BindingType, "github",
            secrets1);

        var secrets2 = new[]
        {
            Tuple.Create("client-id", "okta-client-id"),
            Tuple.Create("client-secret", "okta-client-secret"),
            Tuple.Create("issuer-uri", "okta-issuer-uri")
        };

        AddConfigurationData(configurationData, TestBindingName2, SpringSecurityOAuth2PostProcessor.BindingType, "okta", secrets2);

        var secrets3 = new[]
        {
            Tuple.Create("client-id", "my-provider-client-id"),
            Tuple.Create("client-secret", "my-provider-client-secret"),
            Tuple.Create("client-authentication-method", "my-provider-client-authentication-method"),
            Tuple.Create("authorization-grant-type", "my-provider-authorization-grant-type"),
            Tuple.Create("redirect-uri", "my-provider-redirect-uri"),
            Tuple.Create("scope", "my-provider-scope1,my-provider-scope2"),
            Tuple.Create("client-name", "my-provider-client-name"),
            Tuple.Create("authorization-uri", "my-provider-authorization-uri"),
            Tuple.Create("token-uri", "my-provider-token-uri"),
            Tuple.Create("user-info-uri", "my-provider-user-info-uri"),
            Tuple.Create("user-info-authentication-method", "my-provider-user-info-authentication-method"),
            Tuple.Create("jwk-set-uri", "my-provider-jwk-set-uri"),
            Tuple.Create("user-name-attribute", "my-provider-user-name-attribute")
        };

        AddConfigurationData(configurationData, TestBindingName3, SpringSecurityOAuth2PostProcessor.BindingType, "my-provider", secrets3);

        var secrets4 = new[]
        {
            Tuple.Create("client-id", "my-provider-client-id")
        };

        AddConfigurationData(configurationData, TestMissingProvider, SpringSecurityOAuth2PostProcessor.BindingType, secrets4);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName2, SpringSecurityOAuth2PostProcessor.BindingType);
        configurationData[$"{keyPrefix}:registration:clientId"].Should().Be("okta-client-id");
        configurationData[$"{keyPrefix}:registration:clientSecret"].Should().Be("okta-client-secret");
        configurationData[$"{keyPrefix}:registration:provider"].Should().Be("okta");
        configurationData[$"{keyPrefix}:provider:issuerUri"].Should().Be("okta-issuer-uri");
    }

    [Fact]
    public void SpringSecurityOauth2Test_TestProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        var secrets1 = new[]
        {
            Tuple.Create("client-id", "github-client-id"),
            Tuple.Create("client-secret", "github-client-secret")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName1, SpringSecurityOAuth2PostProcessor.BindingType, "github",
            secrets1);

        var secrets2 = new[]
        {
            Tuple.Create("client-id", "okta-client-id"),
            Tuple.Create("client-secret", "okta-client-secret"),
            Tuple.Create("issuer-uri", "okta-issuer-uri")
        };

        AddConfigurationData(configurationData, TestBindingName2, SpringSecurityOAuth2PostProcessor.BindingType, "okta", secrets2);

        var secrets3 = new[]
        {
            Tuple.Create("client-id", "my-provider-client-id"),
            Tuple.Create("client-secret", "my-provider-client-secret"),
            Tuple.Create("client-authentication-method", "my-provider-client-authentication-method"),
            Tuple.Create("authorization-grant-type", "my-provider-authorization-grant-type"),
            Tuple.Create("redirect-uri", "my-provider-redirect-uri"),
            Tuple.Create("scope", "my-provider-scope1,my-provider-scope2"),
            Tuple.Create("client-name", "my-provider-client-name"),
            Tuple.Create("authorization-uri", "my-provider-authorization-uri"),
            Tuple.Create("token-uri", "my-provider-token-uri"),
            Tuple.Create("user-info-uri", "my-provider-user-info-uri"),
            Tuple.Create("user-info-authentication-method", "my-provider-user-info-authentication-method"),
            Tuple.Create("jwk-set-uri", "my-provider-jwk-set-uri"),
            Tuple.Create("user-name-attribute", "my-provider-user-name-attribute")
        };

        AddConfigurationData(configurationData, TestBindingName3, SpringSecurityOAuth2PostProcessor.BindingType, "my-provider", secrets3);

        var secrets4 = new[]
        {
            Tuple.Create("client-id", "my-provider-client-id")
        };

        AddConfigurationData(configurationData, TestMissingProvider, SpringSecurityOAuth2PostProcessor.BindingType, secrets4);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName3, SpringSecurityOAuth2PostProcessor.BindingType);
        configurationData[$"{keyPrefix}:registration:provider"].Should().Be("my-provider");
        configurationData[$"{keyPrefix}:registration:clientId"].Should().Be("my-provider-client-id");
        configurationData[$"{keyPrefix}:registration:clientSecret"].Should().Be("my-provider-client-secret");
        configurationData[$"{keyPrefix}:registration:clientAuthenticationMethod"].Should().Be("my-provider-client-authentication-method");
        configurationData[$"{keyPrefix}:registration:authorizationGrantType"].Should().Be("my-provider-authorization-grant-type");
        configurationData[$"{keyPrefix}:registration:redirectUri"].Should().Be("my-provider-redirect-uri");
        configurationData[$"{keyPrefix}:registration:scope"].Should().Be("my-provider-scope1,my-provider-scope2");
        configurationData[$"{keyPrefix}:registration:clientName"].Should().Be("my-provider-client-name");
        configurationData[$"{keyPrefix}:provider:authorizationUri"].Should().Be("my-provider-authorization-uri");
        configurationData[$"{keyPrefix}:provider:tokenUri"].Should().Be("my-provider-token-uri");
        configurationData[$"{keyPrefix}:provider:userInfoUri"].Should().Be("my-provider-user-info-uri");
        configurationData[$"{keyPrefix}:provider:userInfoAuthenticationMethod"].Should().Be("my-provider-user-info-authentication-method");
        configurationData[$"{keyPrefix}:provider:jwkSetUri"].Should().Be("my-provider-jwk-set-uri");
        configurationData[$"{keyPrefix}:provider:userNameAttribute"].Should().Be("my-provider-user-name-attribute");
    }

    [Fact]
    public void SpringSecurityOauth2Test_AuthGrantTypes()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        var secrets = new[]
        {
            Tuple.Create("provider", "some-provider"),
            Tuple.Create("authorization-grant-types", "authorization_code,client_credentials")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SpringSecurityOAuth2PostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SpringSecurityOAuth2PostProcessor.BindingType);
        configurationData[$"{keyPrefix}:registration:authorizationGrantTypes"].Should().Be("authorization_code,client_credentials");
    }

    [Fact]
    public void SpringSecurityOauth2Test_RedirectUris()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        var secrets = new[]
        {
            Tuple.Create("provider", "some-provider"),
            Tuple.Create("redirect-uris", "https://app.example.com/authorized,https://other-app.example.com/login")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SpringSecurityOAuth2PostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SpringSecurityOAuth2PostProcessor.BindingType);
        configurationData[$"{keyPrefix}:registration:redirectUris"].Should().Be("https://app.example.com/authorized,https://other-app.example.com/login");
    }

    [Fact]
    public void SqlServerTest_BindingTypeDisabled()
    {
        var postProcessor = new SqlServerPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("password", "test-password")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SqlServerPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SqlServerPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:Password");
    }

    [Fact]
    public void SqlServerTest_BindingTypeEnabled()
    {
        var postProcessor = new SqlServerPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("username", "test-username")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SqlServerPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, SqlServerPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:Data Source"].Should().Be("test-host,test-port");
        configurationData[$"{keyPrefix}:Initial Catalog"].Should().Be("test-database");
        configurationData[$"{keyPrefix}:User ID"].Should().Be("test-username");
        configurationData[$"{keyPrefix}:Password"].Should().Be("test-password");
    }

    [Fact]
    public void VaultTest_BindingTypeDisabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "token"),
            Tuple.Create("token", "test-token")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:token");
    }

    [Fact]
    public void VaultTokenAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "token"),
            Tuple.Create("token", "test-token")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:token"].Should().Be("test-token");
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("token");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
    }

    [Fact]
    public void VaultAppRoleAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("app-role-path", "test-app-role-path"),
            Tuple.Create("authentication-method", "approle"),
            Tuple.Create("role", "test-role"),
            Tuple.Create("role-id", "test-role-id"),
            Tuple.Create("secret-id", "test-secret-id")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("approle");
        configurationData[$"{keyPrefix}:approle:roleId"].Should().Be("test-role-id");
        configurationData[$"{keyPrefix}:approle:secretId"].Should().Be("test-secret-id");
        configurationData[$"{keyPrefix}:approle:role"].Should().Be("test-role");
        configurationData[$"{keyPrefix}:approle:appRolePath"].Should().Be("test-app-role-path");
    }

    [Fact]
    public void VaultCubbyHoleAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "cubbyhole"),
            Tuple.Create("token", "test-token")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("cubbyhole");
        configurationData[$"{keyPrefix}:token"].Should().Be("test-token");
    }

    [Fact]
    public void VaultCertAuthenticationTest_BindingTypeEnabled()
    {
        // Note: This will likely need to be revisited.  See the VaultPostProcessor for more comments
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "cert"),
            Tuple.Create("cert-auth-path", "test-cert-auth-path"),
            Tuple.Create("key-store-password", "test-key-store-password"),
            Tuple.Create("key-store", "key store contents!")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("cert");
        configurationData[$"{keyPrefix}:ssl:certAuthPath"].Should().Be("test-cert-auth-path");
        configurationData[$"{keyPrefix}:ssl:keyStorePassword"].Should().Be("test-key-store-password");
        configurationData[$"{keyPrefix}:ssl:keyStore"].Should().Be("key store contents!");
    }

    [Fact]
    public void VaultAwsEc2AuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "aws_ec2"),
            Tuple.Create("aws-ec2-instance-identity-document", "test-identity-document"),
            Tuple.Create("nonce", "test-nonce"),
            Tuple.Create("aws-ec2-path", "test-aws-ec2-path"),
            Tuple.Create("role", "test-role")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("aws_ec2");
        configurationData[$"{keyPrefix}:awsEc2:role"].Should().Be("test-role");
        configurationData[$"{keyPrefix}:awsEc2:awsEc2Path"].Should().Be("test-aws-ec2-path");
        configurationData[$"{keyPrefix}:awsEc2:identityDocument"].Should().Be("test-identity-document");
        configurationData[$"{keyPrefix}:awsEc2:nonce"].Should().Be("test-nonce");
    }

    [Fact]
    public void VaultAwsIamAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "aws_iam"),
            Tuple.Create("aws-iam-server-id", "test-server-id"),
            Tuple.Create("aws-path", "test-aws-path"),
            Tuple.Create("aws-sts-endpoint-uri", "test-endpoint-uri"),
            Tuple.Create("role", "test-role")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("aws_iam");
        configurationData[$"{keyPrefix}:awsIam:role"].Should().Be("test-role");
        configurationData[$"{keyPrefix}:awsIam:awsPath"].Should().Be("test-aws-path");
        configurationData[$"{keyPrefix}:awsIam:serverId"].Should().Be("test-server-id");
        configurationData[$"{keyPrefix}:awsIam:endpointUri"].Should().Be("test-endpoint-uri");
    }

    [Fact]
    public void VaultAzureMsiAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "azure_msi"),
            Tuple.Create("azure-path", "test-azure-path"),
            Tuple.Create("role", "test-role")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("azure_msi");
        configurationData[$"{keyPrefix}:azureMsi:role"].Should().Be("test-role");
        configurationData[$"{keyPrefix}:azureMsi:azurePath"].Should().Be("test-azure-path");
    }

    [Fact]
    public void VaultGcpGceAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "gcp_gce"),
            Tuple.Create("gcp-path", "test-gcp-path"),
            Tuple.Create("gcp-service-account", "test-service-account"),
            Tuple.Create("role", "test-role")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("gcp_gce");
        configurationData[$"{keyPrefix}:gcpGce:role"].Should().Be("test-role");
        configurationData[$"{keyPrefix}:gcpGce:gcpPath"].Should().Be("test-gcp-path");
        configurationData[$"{keyPrefix}:gcpGce:gcpServiceAccount"].Should().Be("test-service-account");
    }

    [Fact]
    public void VaultGcpIamAuthenticationTest_BindingTypeEnabled()
    {
        // Note: This will likely need to be revisited.  See the VaultPostProcessor for more comments
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "gcp_iam"),
            Tuple.Create("credentials.json", "credentials JSON contents!"),
            Tuple.Create("encoded-key", "test-encoded-key"),
            Tuple.Create("gcp-path", "test-gcp-path"),
            Tuple.Create("gcp-project-id", "test-project-id"),
            Tuple.Create("gcp-service-account", "test-service-account"),
            Tuple.Create("jwt-validity", "test-jwt-validity"),
            Tuple.Create("role", "test-role")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("gcp_iam");
        configurationData[$"{keyPrefix}:gcpIam:credentialsJson"].Should().Be("credentials JSON contents!");
        configurationData[$"{keyPrefix}:gcpIam:encodedKey"].Should().Be("test-encoded-key");
        configurationData[$"{keyPrefix}:gcpIam:gcpPath"].Should().Be("test-gcp-path");
        configurationData[$"{keyPrefix}:gcpIam:gcpProjectId"].Should().Be("test-project-id");
        configurationData[$"{keyPrefix}:gcpIam:gcpServiceAccount"].Should().Be("test-service-account");
        configurationData[$"{keyPrefix}:gcpIam:jwtValidity"].Should().Be("test-jwt-validity");
        configurationData[$"{keyPrefix}:gcpIam:role"].Should().Be("test-role");
    }

    [Fact]
    public void VaultK8sAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "kubernetes"),
            Tuple.Create("kubernetes-path", "test-kubernetes-path"),
            Tuple.Create("role", "test-role")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData[$"{keyPrefix}:authentication"].Should().Be("kubernetes");
        configurationData[$"{keyPrefix}:kubernetes:role"].Should().Be("test-role");
        configurationData[$"{keyPrefix}:kubernetes:kubernetesPath"].Should().Be("test-kubernetes-path");
    }

    [Fact]
    public void VaultMissingProviderTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, VaultPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:namespace"].Should().Be("test-namespace");
        configurationData.Should().NotContainKey($"{keyPrefix}:authentication");
    }

    [Fact]
    public void WavefrontTest_BindingTypeDisabled()
    {
        var postProcessor = new WavefrontPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("api-token", "test-api-token"),
            Tuple.Create("uri", "test-uri")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, WavefrontPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, WavefrontPostProcessor.BindingType, false);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, WavefrontPostProcessor.BindingType);
        configurationData.Should().NotContainKey($"{keyPrefix}:uri");
    }

    [Fact]
    public void WavefrontTest_BindingTypeEnabled()
    {
        var postProcessor = new WavefrontPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("api-token", "test-api-token"),
            Tuple.Create("uri", "test-uri")
        };

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, WavefrontPostProcessor.BindingType, secrets);
        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, WavefrontPostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, WavefrontPostProcessor.BindingType);
        configurationData[$"{keyPrefix}:uri"].Should().Be("test-uri");
        configurationData[$"{keyPrefix}:apiToken"].Should().Be("test-api-token");
    }
}
