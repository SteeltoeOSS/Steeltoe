// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public class PostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void ArtemisTest_BindingTypeDisabled()
    {
        var postProcessor = new ArtemisPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ArtemisPostProcessor.BindingTypeKey,
            Tuple.Create("mode", "EMBEDDED"), Tuple.Create("host", "test-host"), Tuple.Create("port", "test-port"), Tuple.Create("user", "test-user"),
            Tuple.Create("password", "test-password"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ArtemisPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:host", out _));
    }

    [Fact]
    public void ArtemisTest_BindingTypeEnabled()
    {
        var postProcessor = new ArtemisPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ArtemisPostProcessor.BindingTypeKey,
            Tuple.Create("mode", "EMBEDDED"), Tuple.Create("host", "test-host"), Tuple.Create("port", "test-port"), Tuple.Create("user", "test-user"),
            Tuple.Create("password", "test-password"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ArtemisPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("EMBEDDED", configurationData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:mode"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-user", configurationData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:user"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
    }

    [Fact]
    public void CassandraTest_BindingTypeDisabled()
    {
        var postProcessor = new CassandraPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, CassandraPostProcessor.BindingTypeKey,
            Tuple.Create("cluster-name", "test-cluster-name"), Tuple.Create("compression", "test-compression"),
            Tuple.Create("contact-points", "test-contact-points"), Tuple.Create("keyspace-name", "test-keyspace-name"),
            Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, CassandraPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:ssl", out _));
    }

    [Fact]
    public void CassandraTest_BindingTypeEnabled()
    {
        var postProcessor = new CassandraPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, CassandraPostProcessor.BindingTypeKey,
            Tuple.Create("cluster-name", "test-cluster-name"), Tuple.Create("compression", "test-compression"),
            Tuple.Create("contact-points", "test-contact-points"), Tuple.Create("keyspace-name", "test-keyspace-name"),
            Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, CassandraPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-cluster-name", configurationData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:clusterName"]);
        Assert.Equal("test-compression", configurationData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:compression"]);
        Assert.Equal("test-contact-points", configurationData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:contactPoints"]);
        Assert.Equal("test-keyspace-name", configurationData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:keyspaceName"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-ssl", configurationData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:ssl"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
    }

    [Fact]
    public void ConfigServerTest_BindingTypeDisabled()
    {
        var postProcessor = new ConfigServerPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ConfigServerPostProcessor.BindingTypeKey,
            Tuple.Create("uri", "test-uri"), Tuple.Create("client-id", "test-client-id"), Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ConfigServerPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:uri", out _));
    }

    [Fact]
    public void ConfigServerTest_BindingTypeEnabled()
    {
        var postProcessor = new ConfigServerPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ConfigServerPostProcessor.BindingTypeKey,
            Tuple.Create("uri", "test-uri"), Tuple.Create("client-id", "test-client-id"), Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ConfigServerPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-client-id", configurationData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:clientId"]);
        Assert.Equal("test-client-secret", configurationData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:clientSecret"]);

        Assert.Equal("test-access-token-uri",
            configurationData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:accessTokenUri"]);
    }

    [Fact]
    public void CouchbaseTest_BindingTypeDisabled()
    {
        var postProcessor = new CouchbasePostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, CouchbasePostProcessor.BindingTypeKey,
            Tuple.Create("bootstrap-hosts", "test-bootstrap-hosts"), Tuple.Create("bucket.name", "test-bucket-name"),
            Tuple.Create("bucket.password", "test-bucket-password"), Tuple.Create("env.bootstrap.http-direct-port", "test-env-bootstrap-http-direct-port"),
            Tuple.Create("env.bootstrap.http-ssl-port", "test-env-bootstrap-http-ssl-port"), Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, CouchbasePostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:ssl", out _));
    }

    [Fact]
    public void CouchbaseTest_BindingTypeEnabled()
    {
        var postProcessor = new CouchbasePostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, CouchbasePostProcessor.BindingTypeKey,
            Tuple.Create("bootstrap-hosts", "test-bootstrap-hosts"), Tuple.Create("bucket.name", "test-bucket-name"),
            Tuple.Create("bucket.password", "test-bucket-password"), Tuple.Create("env.bootstrap.http-direct-port", "test-env-bootstrap-http-direct-port"),
            Tuple.Create("env.bootstrap.http-ssl-port", "test-env-bootstrap-http-ssl-port"), Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, CouchbasePostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-bootstrap-hosts", configurationData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:bootstrapHosts"]);
        Assert.Equal("test-bucket-name", configurationData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:bucketName"]);
        Assert.Equal("test-bucket-password", configurationData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:bucketPassword"]);

        Assert.Equal("test-env-bootstrap-http-direct-port",
            configurationData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:envBootstrapHttpDirectPort"]);

        Assert.Equal("test-env-bootstrap-http-ssl-port",
            configurationData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:envBootstrapHttpSslPort"]);

        Assert.Equal("test-password", configurationData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
    }

    [Fact]
    public void DB2Test_BindingTypeDisabled()
    {
        var postProcessor = new DB2PostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, DB2PostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, DB2PostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void DB2Test_BindingTypeEnabled()
    {
        var postProcessor = new DB2PostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, DB2PostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, DB2PostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-database", configurationData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configurationData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void ElasticSearchTest_BindingTypeDisabled()
    {
        var postProcessor = new ElasticSearchPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ElasticSearchPostProcessor.BindingTypeKey,
            Tuple.Create("endpoints", "test-endpoints"), Tuple.Create("password", "test-password"), Tuple.Create("use-ssl", "test-use-ssl"),
            Tuple.Create("username", "test-username"), Tuple.Create("proxy.host", "test-proxy-host"), Tuple.Create("proxy.port", "test-proxy-port"),
            Tuple.Create("uris", "test-uris"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ElasticSearchPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:endpoints", out _));
    }

    [Fact]
    public void ElasticSearchTest_BindingTypeEnabled()
    {
        var postProcessor = new ElasticSearchPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, ElasticSearchPostProcessor.BindingTypeKey,
            Tuple.Create("endpoints", "test-endpoints"), Tuple.Create("password", "test-password"), Tuple.Create("use-ssl", "test-use-ssl"),
            Tuple.Create("username", "test-username"), Tuple.Create("proxy.host", "test-proxy-host"), Tuple.Create("proxy.port", "test-proxy-port"),
            Tuple.Create("uris", "test-uris"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ElasticSearchPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-endpoints", configurationData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:endpoints"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-use-ssl", configurationData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:useSsl"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-proxy-host", configurationData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:proxyHost"]);
        Assert.Equal("test-proxy-port", configurationData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:proxyPort"]);
        Assert.Equal("test-uris", configurationData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:uris"]);
    }

    [Fact]
    public void EurekaTest_BindingTypeDisabled()
    {
        var postProcessor = new EurekaPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, EurekaPostProcessor.BindingTypeKey,
            Tuple.Create("uri", "test-uri"), Tuple.Create("client-id", "test-client-id"), Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, EurekaPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:uri", out _));
    }

    [Fact]
    public void EurekaTest_BindingTypeEnabled()
    {
        var postProcessor = new EurekaPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, EurekaPostProcessor.BindingTypeKey,
            Tuple.Create("uri", "test-uri"), Tuple.Create("client-id", "test-client-id"), Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, EurekaPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("default", configurationData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:region"]);
        Assert.Equal("test-client-id", configurationData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:clientId"]);
        Assert.Equal("test-client-secret", configurationData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:clientSecret"]);
        Assert.Equal("test-access-token-uri", configurationData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:accessTokenUri"]);
    }

    [Fact]
    public void KafkaTest_BindingTypeDisabled()
    {
        var postProcessor = new KafkaPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, KafkaPostProcessor.BindingTypeKey,
            Tuple.Create("bootstrap-servers", "test-bootstrap-servers"), Tuple.Create("consumer.bootstrap-servers", "test-consumer-bootstrap-servers"),
            Tuple.Create("producer.bootstrap-servers", "test-producer-bootstrap-servers"),
            Tuple.Create("streams.bootstrap-servers", "test-streams-bootstrap-servers"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, KafkaPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:bootstrapServers", out _));
    }

    [Fact]
    public void KafkaTest_BindingTypeEnabled()
    {
        var postProcessor = new KafkaPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, KafkaPostProcessor.BindingTypeKey,
            Tuple.Create("bootstrap-servers", "test-bootstrap-servers"), Tuple.Create("consumer.bootstrap-servers", "test-consumer-bootstrap-servers"),
            Tuple.Create("producer.bootstrap-servers", "test-producer-bootstrap-servers"),
            Tuple.Create("streams.bootstrap-servers", "test-streams-bootstrap-servers"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, KafkaPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-bootstrap-servers", configurationData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:bootstrapServers"]);

        Assert.Equal("test-consumer-bootstrap-servers",
            configurationData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:consumerBootstrapServers"]);

        Assert.Equal("test-producer-bootstrap-servers",
            configurationData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:producerBootstrapServers"]);

        Assert.Equal("test-streams-bootstrap-servers",
            configurationData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:streamsBootstrapServers"]);
    }

    [Fact]
    public void LdapTest_BindingTypeDisabled()
    {
        var postProcessor = new LdapPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, LdapPostProcessor.BindingTypeKey,
            Tuple.Create("urls", "test-urls"), Tuple.Create("password", "test-password"), Tuple.Create("username", "test-username"),
            Tuple.Create("base", "test-base"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, LdapPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:urls", out _));
    }

    [Fact]
    public void LdapTest_BindingTypeEnabled()
    {
        var postProcessor = new LdapPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, LdapPostProcessor.BindingTypeKey,
            Tuple.Create("urls", "test-urls"), Tuple.Create("password", "test-password"), Tuple.Create("username", "test-username"),
            Tuple.Create("base", "test-base"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, LdapPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-urls", configurationData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:urls"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-base", configurationData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:base"]);
    }

    [Fact]
    public void MongoDbTest_BindingTypeDisabled()
    {
        var postProcessor = new MongoDbPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MongoDbPostProcessor.BindingTypeKey,
            Tuple.Create("authentication-database", "test-authentication-database"), Tuple.Create("database", "test-database"),
            Tuple.Create("grid-fs-database", "test-grid-fs-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("uri", "test-uri"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:authenticationDatabase", out _));
    }

    [Fact]
    public void MongoDbTest_BindingTypeEnabled()
    {
        var postProcessor = new MongoDbPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MongoDbPostProcessor.BindingTypeKey,
            Tuple.Create("authentication-database", "test-authentication-database"), Tuple.Create("database", "test-database"),
            Tuple.Create("grid-fs-database", "test-grid-fs-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("uri", "test-uri"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingTypeKey, true), configurationData);

        Assert.Equal("test-authentication-database",
            configurationData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:authenticationDatabase"]);

        Assert.Equal("test-database", configurationData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-grid-fs-database", configurationData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:gridfsDatabase"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
    }

    [Fact]
    public void MySqlTest_BindingTypeDisabled()
    {
        var postProcessor = new MySqlPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MySqlPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void MySqlTest_BindingTypeEnabled()
    {
        var postProcessor = new MySqlPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, MySqlPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-database", configurationData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configurationData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void Neo4JTest_BindingTypeDisabled()
    {
        var postProcessor = new Neo4JPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, Neo4JPostProcessor.BindingTypeKey,
            Tuple.Create("password", "test-password"), Tuple.Create("uri", "test-uri"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, Neo4JPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{TestBindingName}:password", out _));
    }

    [Fact]
    public void Neo4JTest_BindingTypeEnabled()
    {
        var postProcessor = new Neo4JPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, Neo4JPostProcessor.BindingTypeKey,
            Tuple.Create("password", "test-password"), Tuple.Create("uri", "test-uri"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, Neo4JPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-password", configurationData[$"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
    }

    [Fact]
    public void OracleTest_BindingTypeDisabled()
    {
        var postProcessor = new OraclePostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, OraclePostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, OraclePostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void OracleTest_BindingTypeEnabled()
    {
        var postProcessor = new OraclePostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, OraclePostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, OraclePostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-database", configurationData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configurationData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeDisabled()
    {
        var postProcessor = new PostgreSqlPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, PostgreSqlPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"),
            Tuple.Create("sslmode", "verify-full"), Tuple.Create("sslrootcert", "root.cert"), Tuple.Create("options", "--cluster=routing-id&opt=val1"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, PostgreSqlPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeEnabled()
    {
        var postProcessor = new PostgreSqlPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, PostgreSqlPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"),
            Tuple.Create("sslmode", "verify-full"), Tuple.Create("sslrootcert", "root.cert"), Tuple.Create("options", "--cluster=routing-id&opt=val1"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, PostgreSqlPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-database", configurationData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configurationData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
        Assert.Equal("verify-full", configurationData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:sslmode"]);
        Assert.Equal("root.cert", configurationData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:sslrootcert"]);
        Assert.Equal("--cluster=routing-id&opt=val1", configurationData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:options"]);
    }

    [Fact]
    public void RabbitMQTest_BindingTypeDisabled()
    {
        var postProcessor = new RabbitMQPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RabbitMQPostProcessor.BindingTypeKey,
            Tuple.Create("addresses", "test-addresses"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("username", "test-username"), Tuple.Create("virtual-host", "test-virtual-host"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:addresses", out _));
    }

    [Fact]
    public void RabbitMQTest_BindingTypeEnabled()
    {
        var postProcessor = new RabbitMQPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RabbitMQPostProcessor.BindingTypeKey,
            Tuple.Create("addresses", "test-addresses"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("username", "test-username"), Tuple.Create("virtual-host", "test-virtual-host"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-addresses", configurationData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:addresses"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-virtual-host", configurationData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:virtualHost"]);
    }

    [Fact]
    public void RedisTest_BindingTypeDisabled()
    {
        var postProcessor = new RedisPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RedisPostProcessor.BindingTypeKey,
            Tuple.Create("client-name", "test-client-name"), Tuple.Create("cluster.max-redirects", "test-cluster-max-redirects"),
            Tuple.Create("cluster.nodes", "test-cluster-nodes"), Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("sentinel.master", "test-sentinel-master"),
            Tuple.Create("sentinel.nodes", "test-sentinel-nodes"), Tuple.Create("ssl", "test-ssl"), Tuple.Create("url", "test-url"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:clientname", out _));
    }

    [Fact]
    public void RedisTest_BindingTypeEnabled()
    {
        var postProcessor = new RedisPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, RedisPostProcessor.BindingTypeKey,
            Tuple.Create("client-name", "test-client-name"), Tuple.Create("cluster.max-redirects", "test-cluster-max-redirects"),
            Tuple.Create("cluster.nodes", "test-cluster-nodes"), Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("sentinel.master", "test-sentinel-master"),
            Tuple.Create("sentinel.nodes", "test-sentinel-nodes"), Tuple.Create("ssl", "test-ssl"), Tuple.Create("url", "test-url"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-client-name", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:clientName"]);
        Assert.Equal("test-cluster-max-redirects", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:clusterMaxRedirects"]);
        Assert.Equal("test-cluster-nodes", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:clusterNodes"]);
        Assert.Equal("test-database", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-sentinel-master", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:sentinelMaster"]);
        Assert.Equal("test-sentinel-nodes", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:sentinelNodes"]);
        Assert.Equal("test-ssl", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:ssl"]);
        Assert.Equal("test-url", configurationData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:url"]);
    }

    [Fact]
    public void SapHanaTest_BindingTypeDisabled()
    {
        var postProcessor = new SapHanaPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SapHanaPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SapHanaPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void SapHanaTest_BindingTypeEnabled()
    {
        var postProcessor = new SapHanaPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SapHanaPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SapHanaPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-database", configurationData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configurationData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_BindingTypeDisabled()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName1, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "github",
            Tuple.Create("client-id", "github-client-id"), Tuple.Create("client-secret", "github-client-secret"));

        GetConfigurationData(configurationData, TestBindingName2, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "okta",
            Tuple.Create("client-id", "okta-client-id"), Tuple.Create("client-secret", "okta-client-secret"), Tuple.Create("issuer-uri", "okta-issuer-uri"));

        GetConfigurationData(configurationData, TestBindingName3, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "my-provider",
            Tuple.Create("client-id", "my-provider-client-id"), Tuple.Create("client-secret", "my-provider-client-secret"),
            Tuple.Create("client-authentication-method", "my-provider-client-authentication-method"),
            Tuple.Create("authorization-grant-type", "my-provider-authorization-grant-type"), Tuple.Create("redirect-uri", "my-provider-redirect-uri"),
            Tuple.Create("scope", "my-provider-scope1,my-provider-scope2"), Tuple.Create("client-name", "my-provider-client-name"),
            Tuple.Create("authorization-uri", "my-provider-authorization-uri"), Tuple.Create("token-uri", "my-provider-token-uri"),
            Tuple.Create("user-info-uri", "my-provider-user-info-uri"),
            Tuple.Create("user-info-authentication-method", "my-provider-user-info-authentication-method"),
            Tuple.Create("jwk-set-uri", "my-provider-jwk-set-uri"), Tuple.Create("user-name-attribute", "my-provider-user-name-attribute"));

        GetConfigurationData(configurationData, TestMissingProvider, SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, false),
            configurationData);

        Assert.False(configurationData.TryGetValue($"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName1}:registration:clientId",
            out _));
    }

    [Fact]
    public void SpringSecurityOauth2Test_CommonProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName1, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "github",
            Tuple.Create("client-id", "github-client-id"), Tuple.Create("client-secret", "github-client-secret"));

        GetConfigurationData(configurationData, TestBindingName2, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "okta",
            Tuple.Create("client-id", "okta-client-id"), Tuple.Create("client-secret", "okta-client-secret"), Tuple.Create("issuer-uri", "okta-issuer-uri"));

        GetConfigurationData(configurationData, TestBindingName3, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "my-provider",
            Tuple.Create("client-id", "my-provider-client-id"), Tuple.Create("client-secret", "my-provider-client-secret"),
            Tuple.Create("client-authentication-method", "my-provider-client-authentication-method"),
            Tuple.Create("authorization-grant-type", "my-provider-authorization-grant-type"), Tuple.Create("redirect-uri", "my-provider-redirect-uri"),
            Tuple.Create("scope", "my-provider-scope1,my-provider-scope2"), Tuple.Create("client-name", "my-provider-client-name"),
            Tuple.Create("authorization-uri", "my-provider-authorization-uri"), Tuple.Create("token-uri", "my-provider-token-uri"),
            Tuple.Create("user-info-uri", "my-provider-user-info-uri"),
            Tuple.Create("user-info-authentication-method", "my-provider-user-info-authentication-method"),
            Tuple.Create("jwk-set-uri", "my-provider-jwk-set-uri"), Tuple.Create("user-name-attribute", "my-provider-user-name-attribute"));

        GetConfigurationData(configurationData, TestMissingProvider, SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true),
            configurationData);

        Assert.Equal("github-client-id",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName1}:registration:clientId"]);

        Assert.Equal("github-client-secret",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName1}:registration:clientSecret"]);

        Assert.Equal("github", configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName1}:registration:provider"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_OIDCProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName1, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "github",
            Tuple.Create("client-id", "github-client-id"), Tuple.Create("client-secret", "github-client-secret"));

        GetConfigurationData(configurationData, TestBindingName2, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "okta",
            Tuple.Create("client-id", "okta-client-id"), Tuple.Create("client-secret", "okta-client-secret"), Tuple.Create("issuer-uri", "okta-issuer-uri"));

        GetConfigurationData(configurationData, TestBindingName3, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "my-provider",
            Tuple.Create("client-id", "my-provider-client-id"), Tuple.Create("client-secret", "my-provider-client-secret"),
            Tuple.Create("client-authentication-method", "my-provider-client-authentication-method"),
            Tuple.Create("authorization-grant-type", "my-provider-authorization-grant-type"), Tuple.Create("redirect-uri", "my-provider-redirect-uri"),
            Tuple.Create("scope", "my-provider-scope1,my-provider-scope2"), Tuple.Create("client-name", "my-provider-client-name"),
            Tuple.Create("authorization-uri", "my-provider-authorization-uri"), Tuple.Create("token-uri", "my-provider-token-uri"),
            Tuple.Create("user-info-uri", "my-provider-user-info-uri"),
            Tuple.Create("user-info-authentication-method", "my-provider-user-info-authentication-method"),
            Tuple.Create("jwk-set-uri", "my-provider-jwk-set-uri"), Tuple.Create("user-name-attribute", "my-provider-user-name-attribute"));

        GetConfigurationData(configurationData, TestMissingProvider, SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true),
            configurationData);

        Assert.Equal("okta-client-id",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName2}:registration:clientId"]);

        Assert.Equal("okta-client-secret",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName2}:registration:clientSecret"]);

        Assert.Equal("okta", configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName2}:registration:provider"]);

        Assert.Equal("okta-issuer-uri",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName2}:provider:issuerUri"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_TestProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName1, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "github",
            Tuple.Create("client-id", "github-client-id"), Tuple.Create("client-secret", "github-client-secret"));

        GetConfigurationData(configurationData, TestBindingName2, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "okta",
            Tuple.Create("client-id", "okta-client-id"), Tuple.Create("client-secret", "okta-client-secret"), Tuple.Create("issuer-uri", "okta-issuer-uri"));

        GetConfigurationData(configurationData, TestBindingName3, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "my-provider",
            Tuple.Create("client-id", "my-provider-client-id"), Tuple.Create("client-secret", "my-provider-client-secret"),
            Tuple.Create("client-authentication-method", "my-provider-client-authentication-method"),
            Tuple.Create("authorization-grant-type", "my-provider-authorization-grant-type"), Tuple.Create("redirect-uri", "my-provider-redirect-uri"),
            Tuple.Create("scope", "my-provider-scope1,my-provider-scope2"), Tuple.Create("client-name", "my-provider-client-name"),
            Tuple.Create("authorization-uri", "my-provider-authorization-uri"), Tuple.Create("token-uri", "my-provider-token-uri"),
            Tuple.Create("user-info-uri", "my-provider-user-info-uri"),
            Tuple.Create("user-info-authentication-method", "my-provider-user-info-authentication-method"),
            Tuple.Create("jwk-set-uri", "my-provider-jwk-set-uri"), Tuple.Create("user-name-attribute", "my-provider-user-name-attribute"));

        GetConfigurationData(configurationData, TestMissingProvider, SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true),
            configurationData);

        Assert.Equal("my-provider", configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:provider"]);

        Assert.Equal("my-provider-client-id",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:clientId"]);

        Assert.Equal("my-provider-client-secret",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:clientSecret"]);

        Assert.Equal("my-provider-client-authentication-method",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:clientAuthenticationMethod"]);

        Assert.Equal("my-provider-authorization-grant-type",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:authorizationGrantType"]);

        Assert.Equal("my-provider-redirect-uri",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:redirectUri"]);

        Assert.Equal("my-provider-scope1,my-provider-scope2",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:scope"]);

        Assert.Equal("my-provider-client-name",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:clientName"]);

        Assert.Equal("my-provider-authorization-uri",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:authorizationUri"]);

        Assert.Equal("my-provider-token-uri",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:tokenUri"]);

        Assert.Equal("my-provider-user-info-uri",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:userInfoUri"]);

        Assert.Equal("my-provider-user-info-authentication-method",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:userInfoAuthenticationMethod"]);

        Assert.Equal("my-provider-jwk-set-uri",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:jwkSetUri"]);

        Assert.Equal("my-provider-user-name-attribute",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:userNameAttribute"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_AuthGrantTypes()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("provider", "some-provider"), Tuple.Create("authorization-grant-types", "authorization_code,client_credentials"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true),
            configurationData);

        Assert.Equal("authorization_code,client_credentials",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName}:registration:authorizationGrantTypes"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_RedirectUris()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("provider", "some-provider"), Tuple.Create("redirect-uris", "https://app.example.com/authorized,https://other-app.example.com/login"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true),
            configurationData);

        Assert.Equal("https://app.example.com/authorized,https://other-app.example.com/login",
            configurationData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName}:registration:redirectUris"]);
    }

    [Fact]
    public void SqlServerTest_BindingTypeDisabled()
    {
        var postProcessor = new SqlServerPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SqlServerPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void SqlServerTest_BindingTypeEnabled()
    {
        var postProcessor = new SqlServerPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, SqlServerPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-database", configurationData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configurationData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configurationData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configurationData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configurationData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configurationData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void VaultTest_BindingTypeDisabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "token"),
            Tuple.Create("token", "test-token"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:token", out _));
    }

    [Fact]
    public void VaultTokenAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "token"),
            Tuple.Create("token", "test-token"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-token", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:token"]);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("token", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
    }

    [Fact]
    public void VaultAppRoleAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("app-role-path", "test-app-role-path"),
            Tuple.Create("authentication-method", "approle"), Tuple.Create("role", "test-role"), Tuple.Create("role-id", "test-role-id"),
            Tuple.Create("secret-id", "test-secret-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("approle", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role-id", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:approle:roleId"]);
        Assert.Equal("test-secret-id", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:approle:secretId"]);
        Assert.Equal("test-role", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:approle:role"]);
        Assert.Equal("test-app-role-path", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:approle:appRolePath"]);
    }

    [Fact]
    public void VaultCubbyHoleAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "cubbyhole"),
            Tuple.Create("token", "test-token"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("cubbyhole", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-token", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:token"]);
    }

    [Fact]
    public void VaultCertAuthenticationTest_BindingTypeEnabled()
    {
        // Note: This will likely need to be revisited.  See the VaultPostProcessor for more comments
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "cert"),
            Tuple.Create("cert-auth-path", "test-cert-auth-path"), Tuple.Create("key-store-password", "test-key-store-password"),
            Tuple.Create("key-store", "key store contents!"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("cert", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-cert-auth-path", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:ssl:certAuthPath"]);
        Assert.Equal("test-key-store-password", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:ssl:keyStorePassword"]);
        Assert.Equal("key store contents!", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:ssl:keyStore"]);
    }

    [Fact]
    public void VaultAwsEc2AuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "aws_ec2"),
            Tuple.Create("aws-ec2-instance-identity-document", "test-identity-document"), Tuple.Create("nonce", "test-nonce"),
            Tuple.Create("aws-ec2-path", "test-aws-ec2-path"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("aws_ec2", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsEc2:role"]);
        Assert.Equal("test-aws-ec2-path", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsEc2:awsEc2Path"]);
        Assert.Equal("test-identity-document", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsEc2:identityDocument"]);
        Assert.Equal("test-nonce", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsEc2:nonce"]);
    }

    [Fact]
    public void VaultAwsIamAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "aws_iam"),
            Tuple.Create("aws-iam-server-id", "test-server-id"), Tuple.Create("aws-path", "test-aws-path"),
            Tuple.Create("aws-sts-endpoint-uri", "test-endpoint-uri"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("aws_iam", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsIam:role"]);
        Assert.Equal("test-aws-path", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsIam:awsPath"]);
        Assert.Equal("test-server-id", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsIam:serverId"]);
        Assert.Equal("test-endpoint-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsIam:endpointUri"]);
    }

    [Fact]
    public void VaultAzureMsiAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "azure_msi"),
            Tuple.Create("azure-path", "test-azure-path"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("azure_msi", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:azureMsi:role"]);
        Assert.Equal("test-azure-path", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:azureMsi:azurePath"]);
    }

    [Fact]
    public void VaultGcpGceAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "gcp_gce"),
            Tuple.Create("gcp-path", "test-gcp-path"), Tuple.Create("gcp-service-account", "test-service-account"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("gcp_gce", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpGce:role"]);
        Assert.Equal("test-gcp-path", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpGce:gcpPath"]);
        Assert.Equal("test-service-account", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpGce:gcpServiceAccount"]);
    }

    [Fact]
    public void VaultGcpIamAuthenticationTest_BindingTypeEnabled()
    {
        // Note: This will likely need to be revisited.  See the VaultPostProcessor for more comments
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "gcp_iam"),
            Tuple.Create("credentials.json", "credentials JSON contents!"), Tuple.Create("encoded-key", "test-encoded-key"),
            Tuple.Create("gcp-path", "test-gcp-path"), Tuple.Create("gcp-project-id", "test-project-id"),
            Tuple.Create("gcp-service-account", "test-service-account"), Tuple.Create("jwt-validity", "test-jwt-validity"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("gcp_iam", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("credentials JSON contents!", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:credentialsJson"]);
        Assert.Equal("test-encoded-key", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:encodedKey"]);
        Assert.Equal("test-gcp-path", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:gcpPath"]);
        Assert.Equal("test-project-id", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:gcpProjectId"]);
        Assert.Equal("test-service-account", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:gcpServiceAccount"]);
        Assert.Equal("test-jwt-validity", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:jwtValidity"]);
        Assert.Equal("test-role", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:role"]);
    }

    [Fact]
    public void VaultK8sAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "kubernetes"),
            Tuple.Create("kubernetes-path", "test-kubernetes-path"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("kubernetes", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:kubernetes:role"]);
        Assert.Equal("test-kubernetes-path", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:kubernetes:kubernetesPath"]);
    }

    [Fact]
    public void VaultMissingProviderTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configurationData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.False(configurationData.TryGetValue($"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication", out _));
    }

    [Fact]
    public void WavefrontTest_BindingTypeDisabled()
    {
        var postProcessor = new WavefrontPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, WavefrontPostProcessor.BindingTypeKey,
            Tuple.Create("api-token", "test-api-token"), Tuple.Create("uri", "test-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, WavefrontPostProcessor.BindingTypeKey, false), configurationData);
        Assert.False(configurationData.TryGetValue($"steeltoe:{WavefrontPostProcessor.BindingTypeKey}:{TestBindingName}:uri", out _));
    }

    [Fact]
    public void WavefrontTest_BindingTypeEnabled()
    {
        var postProcessor = new WavefrontPostProcessor();

        Dictionary<string, string> configurationData = GetConfigurationData(TestBindingName, WavefrontPostProcessor.BindingTypeKey,
            Tuple.Create("api-token", "test-api-token"), Tuple.Create("uri", "test-uri"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, WavefrontPostProcessor.BindingTypeKey, true), configurationData);
        Assert.Equal("test-uri", configurationData[$"steeltoe:{WavefrontPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-api-token", configurationData[$"steeltoe:{WavefrontPostProcessor.BindingTypeKey}:{TestBindingName}:apiToken"]);
    }
}
