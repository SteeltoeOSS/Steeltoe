// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;
public class PostProcessorsTest
{
    private const string TestBindingName = "test-name";
    private const string TestBindingName1 = "test-name-1";
    private const string TestBindingName2 = "test-name-2";
    private const string TestBindingName3 = "test-name-3";
    private const string TestMissingProvider = "test-missing-provider";

    [Fact]
    public void ArtemisTest_BindingTypeDisabled()
    {
        var postProcessor = new ArtemisPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            ArtemisPostProcessor.BindingTypeKey,
            Tuple.Create("mode", "EMBEDDED"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("user", "test-user"),
            Tuple.Create("password", "test-password"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ArtemisPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:host", out _));
    }

    [Fact]
    public void ArtemisTest_BindingTypeEnabled()
    {
        var postProcessor = new ArtemisPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            ArtemisPostProcessor.BindingTypeKey,
            Tuple.Create("mode", "EMBEDDED"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("user", "test-user"),
            Tuple.Create("password", "test-password"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ArtemisPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("EMBEDDED", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:mode"]);
        Assert.Equal("test-host", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-port", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-user", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:user"]);
        Assert.Equal("test-password", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
    }


    [Fact]
    public void CassandraTest_BindingTypeDisabled()
    {
        var postProcessor = new CassandraPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            CassandraPostProcessor.BindingTypeKey,
            Tuple.Create("cluster-name", "test-cluster-name"),
            Tuple.Create("compression", "test-compression"),
            Tuple.Create("contact-points", "test-contact-points"),
            Tuple.Create("keyspace-name", "test-keyspace-name"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, CassandraPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:ssl", out _));
    }

    [Fact]
    public void CassandraTest_BindingTypeEnabled()
    {
        var postProcessor = new CassandraPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            CassandraPostProcessor.BindingTypeKey,
            Tuple.Create("cluster-name", "test-cluster-name"),
            Tuple.Create("compression", "test-compression"),
            Tuple.Create("contact-points", "test-contact-points"),
            Tuple.Create("keyspace-name", "test-keyspace-name"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("ssl", "test-ssl"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, CassandraPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-cluster-name", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:clusterName"]);
        Assert.Equal("test-compression", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:compression"]);
        Assert.Equal("test-contact-points", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:contactPoints"]);
        Assert.Equal("test-keyspace-name", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:keyspaceName"]);
        Assert.Equal("test-password", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-ssl", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:ssl"]);
        Assert.Equal("test-username", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
    }

    [Fact]
    public void ConfigServerTest_BindingTypeDisabled()
    {
        var postProcessor = new ConfigServerPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            ConfigServerPostProcessor.BindingTypeKey,
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("client-id", "test-client-id"),
            Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ConfigServerPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:uri", out _));

    }

    [Fact]
    public void ConfigServerTest_BindingTypeEnabled()
    {
        var postProcessor = new ConfigServerPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            ConfigServerPostProcessor.BindingTypeKey,
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("client-id", "test-client-id"),
            Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ConfigServerPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-client-id", configData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:clientId"]);
        Assert.Equal("test-client-secret", configData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:clientSecret"]);
        Assert.Equal("test-access-token-uri", configData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:accessTokenUri"]);

    }

    [Fact]
    public void CouchbaseTest_BindingTypeDisabled()
    {
        var postProcessor = new CouchbasePostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            CouchbasePostProcessor.BindingTypeKey,
            Tuple.Create("bootstrap-hosts", "test-bootstrap-hosts"),
            Tuple.Create("bucket.name", "test-bucket-name"),
            Tuple.Create("bucket.password", "test-bucket-password"),
            Tuple.Create("env.bootstrap.http-direct-port", "test-env-bootstrap-http-direct-port"),
            Tuple.Create("env.bootstrap.http-ssl-port", "test-env-bootstrap-http-ssl-port"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, CouchbasePostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:ssl", out _));
    }

    [Fact]
    public void CouchbaseTest_BindingTypeEnabled()
    {
        var postProcessor = new CouchbasePostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            CouchbasePostProcessor.BindingTypeKey,
            Tuple.Create("bootstrap-hosts", "test-bootstrap-hosts"),
            Tuple.Create("bucket.name", "test-bucket-name"),
            Tuple.Create("bucket.password", "test-bucket-password"),
            Tuple.Create("env.bootstrap.http-direct-port", "test-env-bootstrap-http-direct-port"),
            Tuple.Create("env.bootstrap.http-ssl-port", "test-env-bootstrap-http-ssl-port"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, CouchbasePostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-bootstrap-hosts", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:bootstrapHosts"]);
        Assert.Equal("test-bucket-name", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:bucketName"]);
        Assert.Equal("test-bucket-password", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:bucketPassword"]);
        Assert.Equal("test-env-bootstrap-http-direct-port", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:envBootstrapHttpDirectPort"]);
        Assert.Equal("test-env-bootstrap-http-ssl-port", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:envBootstrapHttpSslPort"]);
        Assert.Equal("test-password", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-username", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
    }


    [Fact]
    public void DB2Test_BindingTypeDisabled()
    {
        var postProcessor = new DB2PostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            DB2PostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, DB2PostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void DB2Test_BindingTypeEnabled()
    {
        var postProcessor = new DB2PostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            DB2PostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, DB2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void ElasticSearchTest_BindingTypeDisabled()
    {
        var postProcessor = new ElasticSearchPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            ElasticSearchPostProcessor.BindingTypeKey,
            Tuple.Create("endpoints", "test-endpoints"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("use-ssl", "test-use-ssl"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("proxy.host", "test-proxy-host"),
            Tuple.Create("proxy.port", "test-proxy-port"),
            Tuple.Create("uris", "test-uris"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ElasticSearchPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:endpoints", out _));
    }

    [Fact]
    public void ElasticSearchTest_BindingTypeEnabled()
    {
        var postProcessor = new ElasticSearchPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            ElasticSearchPostProcessor.BindingTypeKey,
            Tuple.Create("endpoints", "test-endpoints"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("use-ssl", "test-use-ssl"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("proxy.host", "test-proxy-host"),
            Tuple.Create("proxy.port", "test-proxy-port"),
            Tuple.Create("uris", "test-uris"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ElasticSearchPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-endpoints", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:endpoints"]);
        Assert.Equal("test-password", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-use-ssl", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:useSsl"]);
        Assert.Equal("test-username", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-proxy-host", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:proxyHost"]);
        Assert.Equal("test-proxy-port", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:proxyPort"]);
        Assert.Equal("test-uris", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{TestBindingName}:uris"]);
    }


    [Fact]
    public void EurekaTest_BindingTypeDisabled()
    {
        var postProcessor = new EurekaPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            EurekaPostProcessor.BindingTypeKey,
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("client-id", "test-client-id"),
            Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, EurekaPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:uri", out _));
    }

    [Fact]
    public void EurekaTest_BindingTypeEnabled()
    {
        var postProcessor = new EurekaPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            EurekaPostProcessor.BindingTypeKey,
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("client-id", "test-client-id"),
            Tuple.Create("client-secret", "test-client-secret"),
            Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, EurekaPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("default", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:region"]);
        Assert.Equal("test-client-id", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:clientId"]);
        Assert.Equal("test-client-secret", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:clientSecret"]);
        Assert.Equal("test-access-token-uri", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{TestBindingName}:oauth2:accessTokenUri"]);
    }

    [Fact]
    public void KafkaTest_BindingTypeDisabled()
    {
        var postProcessor = new KafkaPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            KafkaPostProcessor.BindingTypeKey,
            Tuple.Create("bootstrap-servers", "test-bootstrap-servers"),
            Tuple.Create("consumer.bootstrap-servers", "test-consumer-bootstrap-servers"),
            Tuple.Create("producer.bootstrap-servers", "test-producer-bootstrap-servers"),
            Tuple.Create("streams.bootstrap-servers", "test-streams-bootstrap-servers"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, KafkaPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:bootstrapServers", out _));
    }

    [Fact]
    public void KafkaTest_BindingTypeEnabled()
    {
        var postProcessor = new KafkaPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            KafkaPostProcessor.BindingTypeKey,
            Tuple.Create("bootstrap-servers", "test-bootstrap-servers"),
            Tuple.Create("consumer.bootstrap-servers", "test-consumer-bootstrap-servers"),
            Tuple.Create("producer.bootstrap-servers", "test-producer-bootstrap-servers"),
            Tuple.Create("streams.bootstrap-servers", "test-streams-bootstrap-servers"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, KafkaPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-bootstrap-servers", configData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:bootstrapServers"]);
        Assert.Equal("test-consumer-bootstrap-servers", configData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:consumerBootstrapServers"]);
        Assert.Equal("test-producer-bootstrap-servers", configData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:producerBootstrapServers"]);
        Assert.Equal("test-streams-bootstrap-servers", configData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{TestBindingName}:streamsBootstrapServers"]);
    }


    [Fact]
    public void LdapTest_BindingTypeDisabled()
    {
        var postProcessor = new LdapPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            LdapPostProcessor.BindingTypeKey,
            Tuple.Create("urls", "test-urls"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("base", "test-base"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, LdapPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:urls", out _));
    }

    [Fact]
    public void LdapTest_BindingTypeEnabled()
    {
        var postProcessor = new LdapPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            LdapPostProcessor.BindingTypeKey,
            Tuple.Create("urls", "test-urls"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("base", "test-base"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, LdapPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-urls", configData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:urls"]);
        Assert.Equal("test-password", configData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-username", configData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-base", configData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{TestBindingName}:base"]);
    }

    [Fact]
    public void MongoDbTest_BindingTypeDisabled()
    {
        var postProcessor = new MongoDbPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            MongoDbPostProcessor.BindingTypeKey,
            Tuple.Create("authentication-database", "test-authentication-database"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("grid-fs-database", "test-grid-fs-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:authenticationDatabase", out _));
    }

    [Fact]
    public void MongoDbTest_BindingTypeEnabled()
    {
        var postProcessor = new MongoDbPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            MongoDbPostProcessor.BindingTypeKey,
            Tuple.Create("authentication-database", "test-authentication-database"),
            Tuple.Create("database", "test-database"),
            Tuple.Create("grid-fs-database", "test-grid-fs-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-authentication-database", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:authenticationDatabase"]);
        Assert.Equal("test-database", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-grid-fs-database", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:gridfsDatabase"]);
        Assert.Equal("test-host", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-uri", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-username", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
    }

    [Fact]
    public void MySqlTest_BindingTypeDisabled()
    {
        var postProcessor = new MySqlPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            MySqlPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void MySqlTest_BindingTypeEnabled()
    {
        var postProcessor = new MySqlPostProcessor();
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
        Assert.Equal("test-database", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void Neo4JTest_BindingTypeDisabled()
    {
        var postProcessor = new Neo4JPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            Neo4JPostProcessor.BindingTypeKey,
            Tuple.Create("password", "test-password"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, Neo4JPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{TestBindingName}:password", out _));
    }

    [Fact]
    public void Neo4JTest_BindingTypeEnabled()
    {
        var postProcessor = new Neo4JPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            Neo4JPostProcessor.BindingTypeKey,
            Tuple.Create("password", "test-password"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, Neo4JPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-password", configData[$"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-uri", configData[$"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-username", configData[$"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
    }

    [Fact]
    public void OracleTest_BindingTypeDisabled()
    {
        var postProcessor = new OraclePostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            OraclePostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, OraclePostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void OracleTest_BindingTypeEnabled()
    {
        var postProcessor = new OraclePostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            OraclePostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, OraclePostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }


    [Fact]
    public void PostgreSqlTest_BindingTypeDisabled()
    {
        var postProcessor = new PostgreSqlPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            PostgreSqlPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("sslmode", "verify-full"),
            Tuple.Create("sslrootcert", "root.cert"),
            Tuple.Create("options", "--cluster=routing-id&opt=val1"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, PostgreSqlPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeEnabled()
    {
        var postProcessor = new PostgreSqlPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            PostgreSqlPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("sslmode", "verify-full"),
            Tuple.Create("sslrootcert", "root.cert"),
            Tuple.Create("options", "--cluster=routing-id&opt=val1"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, PostgreSqlPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
        Assert.Equal("verify-full", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:sslmode"]);
        Assert.Equal("root.cert", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:sslrootcert"]);
        Assert.Equal("--cluster=routing-id&opt=val1", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{TestBindingName}:options"]);
    }

    [Fact]
    public void RabbitMQTest_BindingTypeDisabled()
    {
        var postProcessor = new RabbitMQPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            RabbitMQPostProcessor.BindingTypeKey,
            Tuple.Create("addresses", "test-addresses"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("username", "test-username"),
            Tuple.Create("virtual-host", "test-virtual-host"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:addresses", out _));
    }

    [Fact]
    public void RabbitMQTest_BindingTypeEnabled()
    {
        var postProcessor = new RabbitMQPostProcessor();
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
        Assert.Equal("test-addresses", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:addresses"]);
        Assert.Equal("test-host", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-virtual-host", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{TestBindingName}:virtualHost"]);
    }


    [Fact]
    public void RedisTest_BindingTypeDisabled()
    {
        var postProcessor = new RedisPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            RedisPostProcessor.BindingTypeKey,
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
            Tuple.Create("url", "test-url"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:clientname", out _));
    }

    [Fact]
    public void RedisTest_BindingTypeEnabled()
    {
        var postProcessor = new RedisPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            RedisPostProcessor.BindingTypeKey,
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
            Tuple.Create("url", "test-url"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-client-name", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:clientName"]);
        Assert.Equal("test-cluster-max-redirects", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:clusterMaxRedirects"]);
        Assert.Equal("test-cluster-nodes", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:clusterNodes"]);
        Assert.Equal("test-database", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-sentinel-master", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:sentinelMaster"]);
        Assert.Equal("test-sentinel-nodes", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:sentinelNodes"]);
        Assert.Equal("test-ssl", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:ssl"]);
        Assert.Equal("test-url", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{TestBindingName}:url"]);
    }


    [Fact]
    public void SapHanaTest_BindingTypeDisabled()
    {
        var postProcessor = new SapHanaPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            SapHanaPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SapHanaPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void SapHanaTest_BindingTypeEnabled()
    {
        var postProcessor = new SapHanaPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            SapHanaPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SapHanaPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_BindingTypeDisabled()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName1,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "github",
            Tuple.Create("client-id", "github-client-id"),
            Tuple.Create("client-secret", "github-client-secret"));
        GetConfigData(configData,
            TestBindingName2,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "okta",
            Tuple.Create("client-id", "okta-client-id"),
            Tuple.Create("client-secret", "okta-client-secret"),
            Tuple.Create("issuer-uri", "okta-issuer-uri"));
        GetConfigData(configData,
            TestBindingName3,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "my-provider",
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
            Tuple.Create("user-name-attribute", "my-provider-user-name-attribute"));
        GetConfigData(configData,
            TestMissingProvider,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("client-id", "my-provider-client-id"));


        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName1}:registration:clientId", out _));
    }

    [Fact]
    public void SpringSecurityOauth2Test_CommonProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName1,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "github",
            Tuple.Create("client-id", "github-client-id"),
            Tuple.Create("client-secret", "github-client-secret"));
        GetConfigData(configData,
            TestBindingName2,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "okta",
            Tuple.Create("client-id", "okta-client-id"),
            Tuple.Create("client-secret", "okta-client-secret"),
            Tuple.Create("issuer-uri", "okta-issuer-uri"));
        GetConfigData(configData,
            TestBindingName3,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "my-provider",
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
            Tuple.Create("user-name-attribute", "my-provider-user-name-attribute"));
        GetConfigData(configData,
            TestMissingProvider,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("github-client-id", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName1}:registration:clientId"]);
        Assert.Equal("github-client-secret", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName1}:registration:clientSecret"]);
        Assert.Equal("github", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName1}:registration:provider"]);
    }
    [Fact]
    public void SpringSecurityOauth2Test_OIDCProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName1,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "github",
            Tuple.Create("client-id", "github-client-id"),
            Tuple.Create("client-secret", "github-client-secret"));
        GetConfigData(configData,
            TestBindingName2,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "okta",
            Tuple.Create("client-id", "okta-client-id"),
            Tuple.Create("client-secret", "okta-client-secret"),
            Tuple.Create("issuer-uri", "okta-issuer-uri"));
        GetConfigData(configData,
            TestBindingName3,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "my-provider",
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
            Tuple.Create("user-name-attribute", "my-provider-user-name-attribute"));
        GetConfigData(configData,
            TestMissingProvider,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("okta-client-id", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName2}:registration:clientId"]);
        Assert.Equal("okta-client-secret", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName2}:registration:clientSecret"]);
        Assert.Equal("okta", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName2}:registration:provider"]);
        Assert.Equal("okta-issuer-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName2}:provider:issuerUri"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_TestProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName1,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "github",
            Tuple.Create("client-id", "github-client-id"),
            Tuple.Create("client-secret", "github-client-secret"));
        GetConfigData(configData,
            TestBindingName2,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "okta",
            Tuple.Create("client-id", "okta-client-id"),
            Tuple.Create("client-secret", "okta-client-secret"),
            Tuple.Create("issuer-uri", "okta-issuer-uri"));
        GetConfigData(configData,
            TestBindingName3,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "my-provider",
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
            Tuple.Create("user-name-attribute", "my-provider-user-name-attribute"));
        GetConfigData(configData,
            TestMissingProvider,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("my-provider", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:provider"]);
        Assert.Equal("my-provider-client-id", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:clientId"]);
        Assert.Equal("my-provider-client-secret", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:clientSecret"]);
        Assert.Equal("my-provider-client-authentication-method", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:clientAuthenticationMethod"]);
        Assert.Equal("my-provider-authorization-grant-type", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:authorizationGrantType"]);
        Assert.Equal("my-provider-redirect-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:redirectUri"]);
        Assert.Equal("my-provider-scope1,my-provider-scope2", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:scope"]);
        Assert.Equal("my-provider-client-name", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:registration:clientName"]);
        Assert.Equal("my-provider-authorization-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:authorizationUri"]);
        Assert.Equal("my-provider-token-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:tokenUri"]);
        Assert.Equal("my-provider-user-info-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:userInfoUri"]);
        Assert.Equal("my-provider-user-info-authentication-method", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:userInfoAuthenticationMethod"]);
        Assert.Equal("my-provider-jwk-set-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:jwkSetUri"]);
        Assert.Equal("my-provider-user-name-attribute", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName3}:provider:userNameAttribute"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_AuthGrantTypes()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("provider", "some-provider"),
            Tuple.Create("authorization-grant-types", "authorization_code,client_credentials"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("authorization_code,client_credentials", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName}:registration:authorizationGrantTypes"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_RedirectUris()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            Tuple.Create("provider", "some-provider"),
            Tuple.Create("redirect-uris", "https://app.example.com/authorized,https://other-app.example.com/login"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("https://app.example.com/authorized,https://other-app.example.com/login", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{TestBindingName}:registration:redirectUris"]);
    }

    [Fact]
    public void SqlServerTest_BindingTypeDisabled()
    {
        var postProcessor = new SqlServerPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            SqlServerPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:database", out _));
    }

    [Fact]
    public void SqlServerTest_BindingTypeEnabled()
    {
        var postProcessor = new SqlServerPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            SqlServerPostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{TestBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void VaultTest_BindingTypeDisabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "token"),
            Tuple.Create("token", "test-token"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:token", out _));
    }

    [Fact]
    public void VaultTokenAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "token"),
            Tuple.Create("token", "test-token"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-token", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:token"]);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("token", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
    }

    [Fact]
    public void VaultAppRoleAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("app-role-path", "test-app-role-path"),
            Tuple.Create("authentication-method", "approle"),
            Tuple.Create("role", "test-role"),
            Tuple.Create("role-id", "test-role-id"),
            Tuple.Create("secret-id", "test-secret-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("approle", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role-id", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:approle:roleId"]);
        Assert.Equal("test-secret-id", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:approle:secretId"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:approle:role"]);
        Assert.Equal("test-app-role-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:approle:appRolePath"]);
    }

    [Fact]
    public void VaultCubbyHoleAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "cubbyhole"),
            Tuple.Create("token", "test-token"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("cubbyhole", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-token", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:token"]);
    }

    [Fact]
    public void VaultCertAuthenticationTest_BindingTypeEnabled()
    {
        // Note: This will likely need to be revisited.  See the VaultPostProcessor for more comments
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "cert"),
            Tuple.Create("cert-auth-path", "test-cert-auth-path"),
            Tuple.Create("key-store-password", "test-key-store-password"),
            Tuple.Create("key-store", "key store contents!"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("cert", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-cert-auth-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:ssl:certAuthPath"]);
        Assert.Equal("test-key-store-password", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:ssl:keyStorePassword"]);
        Assert.Equal("key store contents!", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:ssl:keyStore"]);
    }

    [Fact]
    public void VaultAwsEc2AuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "aws_ec2"),
            Tuple.Create("aws-ec2-instance-identity-document", "test-identity-document"),
            Tuple.Create("nonce", "test-nonce"),
            Tuple.Create("aws-ec2-path", "test-aws-ec2-path"),
            Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("aws_ec2", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsEc2:role"]);
        Assert.Equal("test-aws-ec2-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsEc2:awsEc2Path"]);
        Assert.Equal("test-identity-document", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsEc2:identityDocument"]);
        Assert.Equal("test-nonce", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsEc2:nonce"]);
    }

    [Fact]
    public void VaultAwsIamAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "aws_iam"),
            Tuple.Create("aws-iam-server-id", "test-server-id"),
            Tuple.Create("aws-path", "test-aws-path"),
            Tuple.Create("aws-sts-endpoint-uri", "test-endpoint-uri"),
            Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("aws_iam", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsIam:role"]);
        Assert.Equal("test-aws-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsIam:awsPath"]);
        Assert.Equal("test-server-id", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsIam:serverId"]);
        Assert.Equal("test-endpoint-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:awsIam:endpointUri"]);
    }

    [Fact]
    public void VaultAzureMsiAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "azure_msi"),
            Tuple.Create("azure-path", "test-azure-path"),
            Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("azure_msi", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:azureMsi:role"]);
        Assert.Equal("test-azure-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:azureMsi:azurePath"]);
    }

    [Fact]
    public void VaultGcpGceAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "gcp_gce"),
            Tuple.Create("gcp-path", "test-gcp-path"),
            Tuple.Create("gcp-service-account", "test-service-account"),
            Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("gcp_gce", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpGce:role"]);
        Assert.Equal("test-gcp-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpGce:gcpPath"]);
        Assert.Equal("test-service-account", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpGce:gcpServiceAccount"]);
    }

    [Fact]
    public void VaultGcpIamAuthenticationTest_BindingTypeEnabled()
    {
        // Note: This will likely need to be revisited.  See the VaultPostProcessor for more comments
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "gcp_iam"),
            Tuple.Create("credentials.json", "credentials JSON contents!"),
            Tuple.Create("encoded-key", "test-encoded-key"),
            Tuple.Create("gcp-path", "test-gcp-path"),
            Tuple.Create("gcp-project-id", "test-project-id"),
            Tuple.Create("gcp-service-account", "test-service-account"),
            Tuple.Create("jwt-validity", "test-jwt-validity"),
            Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("gcp_iam", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("credentials JSON contents!", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:credentialsJson"]);
        Assert.Equal("test-encoded-key", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:encodedKey"]);
        Assert.Equal("test-gcp-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:gcpPath"]);
        Assert.Equal("test-project-id", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:gcpProjectId"]);
        Assert.Equal("test-service-account", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:gcpServiceAccount"]);
        Assert.Equal("test-jwt-validity", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:jwtValidity"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:gcpIam:role"]);
    }

    [Fact]
    public void VaultK8sAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"),
            Tuple.Create("authentication-method", "kubernetes"),
            Tuple.Create("kubernetes-path", "test-kubernetes-path"),
            Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.Equal("kubernetes", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:kubernetes:role"]);
        Assert.Equal("test-kubernetes-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:kubernetes:kubernetesPath"]);
    }

    [Fact]
    public void VaultMissingProviderTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            VaultPostProcessor.BindingTypeKey,
            Tuple.Create("namespace", "test-namespace"),
            Tuple.Create("uri", "test-uri"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:namespace"]);
        Assert.False(configData.TryGetValue($"steeltoe:{VaultPostProcessor.BindingTypeKey}:{TestBindingName}:authentication", out _));
    }

    [Fact]
    public void WavefrontTest_BindingTypeDisabled()
    {
        var postProcessor = new WavefrontPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            WavefrontPostProcessor.BindingTypeKey,
            Tuple.Create("api-token", "test-api-token"),
            Tuple.Create("uri", "test-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, WavefrontPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{WavefrontPostProcessor.BindingTypeKey}:{TestBindingName}:uri", out _));
    }

    [Fact]
    public void WavefrontTest_BindingTypeEnabled()
    {
        var postProcessor = new WavefrontPostProcessor();
        Dictionary<string, string> configData = GetConfigData(TestBindingName,
            WavefrontPostProcessor.BindingTypeKey,
            Tuple.Create("api-token", "test-api-token"),
            Tuple.Create("uri", "test-uri"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, WavefrontPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{WavefrontPostProcessor.BindingTypeKey}:{TestBindingName}:uri"]);
        Assert.Equal("test-api-token", configData[$"steeltoe:{WavefrontPostProcessor.BindingTypeKey}:{TestBindingName}:apiToken"]);
    }


    private Dictionary<string, string> GetConfigData(Dictionary<string, string> dictionary, string bindingName, string bindingType, params Tuple<string, string>[] secrets)
    {
        foreach (var kv in secrets)
        {
            dictionary.Add(MakeSecretKey(bindingName, kv.Item1), kv.Item2);
        }
        dictionary.Add(MakeTypeKey(bindingName), bindingType);
        return dictionary;
    }

    private Dictionary<string, string> GetConfigData(string bindingName, string bindingType, params Tuple<string, string>[] secrets)
    {
        var dictionary = new Dictionary<string, string>();
        GetConfigData(dictionary, bindingName, bindingType, secrets);
        return dictionary;
    }

    private void GetConfigData(Dictionary<string, string> dictionary, string bindingName, string bindingType, string bindingProvider, params Tuple<string, string>[] secrets)
    {
        foreach (var kv in secrets)
        {
            dictionary.Add(MakeSecretKey(bindingName, kv.Item1), kv.Item2);
        }
        dictionary.Add(MakeTypeKey(bindingName), bindingType);
        dictionary.Add(MakeProviderKey(bindingName), bindingProvider);
    }

    private Dictionary<string, string> GetConfigData(string bindingName, string bindingType, string bindingProvider, params Tuple<string, string>[] secrets)
    {
        var dictionary = new Dictionary<string, string>();
        GetConfigData(dictionary, bindingName, bindingType, bindingProvider, secrets);
        return dictionary;
    }

    private string MakeTypeKey(string bindingName)
    {
        return ServiceBindingConfigurationProvider.KubernetesBindingsPrefix + ConfigurationPath.KeyDelimiter + bindingName + ConfigurationPath.KeyDelimiter + ServiceBindingConfigurationProvider.TypeKey;
    }

    private string MakeProviderKey(string bindingName)
    {
        return ServiceBindingConfigurationProvider.KubernetesBindingsPrefix + ConfigurationPath.KeyDelimiter + bindingName + ConfigurationPath.KeyDelimiter + ServiceBindingConfigurationProvider.ProviderKey;
    }

    private string MakeSecretKey(string bindingName, string key)
    {
        return ServiceBindingConfigurationProvider.KubernetesBindingsPrefix + ConfigurationPath.KeyDelimiter + bindingName + ConfigurationPath.KeyDelimiter + key;
    }

    private PostProcessorConfigurationProvider GetConfigurationProvider(IConfigurationPostProcessor postProcessor, string bindingTypeKey, bool bindingTypeKeyValue)
    {
        var source = new TestPostProcessorConfigurationSource();
        source.ParentConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>() { { $"steeltoe:kubernetes:bindings:{bindingTypeKey}:enable", bindingTypeKeyValue.ToString() } })
            .Build();
        source.RegisterPostProcessor(postProcessor);

        return new TestPostProcessorConfigurationProvider(source);
    }

    private class TestPostProcessorConfigurationProvider : PostProcessorConfigurationProvider
    {
        public TestPostProcessorConfigurationProvider(PostProcessorConfigurationSource source)
            : base(source)
        {
        }
    }

    private class TestPostProcessorConfigurationSource : PostProcessorConfigurationSource
    {
    }
}
