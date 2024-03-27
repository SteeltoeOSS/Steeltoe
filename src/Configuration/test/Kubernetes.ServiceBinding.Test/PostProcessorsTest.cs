// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.ServiceBinding.Test;

public class PostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void Processes_ApplicationConfigurationService_ConfigurationData()
    {
        var postProcessor = new ApplicationConfigurationServicePostProcessor();

        Dictionary<string, string> configurationData = GetConfigData(
            _testBindingName,
            ApplicationConfigurationServicePostProcessor.BindingType,
            Tuple.Create("provider", "acs"),
            Tuple.Create("random", "data"),
            Tuple.Create("from", "some-source"),
            Tuple.Create("secret", "password"),
            Tuple.Create("secret.one", "password1"),
            Tuple.Create("secret__two", "password2"));

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor, ApplicationConfigurationServicePostProcessor.BindingType, true);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        configurationData["random"].Should().Be("data");
        configurationData["from"].Should().Be("some-source");
        configurationData["secret"].Should().Be("password");
        configurationData["secret:one"].Should().Be("password1");
        configurationData["secret:two"].Should().Be("password2");
        configurationData.Should().NotContainKey("type");
        configurationData.Should().NotContainKey("provider");
    }

    [Fact]
    public void PopulatesDotNetFriendlyKeysFromOtherFormats()
    {
        string rootDirectory = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s");
        var source = new ServiceBindingConfigurationSource(rootDirectory);
        var postProcessor = new ApplicationConfigurationServicePostProcessor();
        source.RegisterPostProcessor(postProcessor);

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "steeltoe:kubernetes:bindings:enable", "true" } })
            .Add(source).Build();

        configuration["test-secret-key"].Should().Be("test-secret-value");
        configuration["key:with:periods"].Should().Be("test-secret-value.");
        configuration["key:with:double:underscores"].Should().Be("test-secret-value0");
        configuration["key:with:double:underscores_"].Should().Be("test-secret-value1");
        configuration["key:with:double:underscores:"].Should().Be("test-secret-value2");
    }

    [Fact]
    public void ArtemisTest_BindingTypeDisabled()
    {
        var postProcessor = new ArtemisPostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName,
            ArtemisPostProcessor.BindingTypeKey,
            Tuple.Create("mode", "EMBEDDED"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("user", "test-user"),
            Tuple.Create("password", "test-password"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ArtemisPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{_testBindingName}:host", out _));
    }

    [Fact]
    public void ArtemisTest_BindingTypeEnabled()
    {
        var postProcessor = new ArtemisPostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName,
            ArtemisPostProcessor.BindingTypeKey,
            Tuple.Create("mode", "EMBEDDED"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("user", "test-user"),
            Tuple.Create("password", "test-password"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ArtemisPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("EMBEDDED", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{_testBindingName}:mode"]);
        Assert.Equal("test-host", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-port", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-user", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{_testBindingName}:user"]);
        Assert.Equal("test-password", configData[$"steeltoe:{ArtemisPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
    }

    [Fact]
    public void CassandraTest_BindingTypeDisabled()
    {
        var postProcessor = new CassandraPostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName,
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
        Assert.False(configData.TryGetValue($"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{_testBindingName}:ssl", out _));
    }

    [Fact]
    public void CassandraTest_BindingTypeEnabled()
    {
        var postProcessor = new CassandraPostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName,
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
        Assert.Equal("test-cluster-name", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{_testBindingName}:clusterName"]);
        Assert.Equal("test-compression", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{_testBindingName}:compression"]);
        Assert.Equal("test-contact-points", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{_testBindingName}:contactPoints"]);
        Assert.Equal("test-keyspace-name", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{_testBindingName}:keyspaceName"]);
        Assert.Equal("test-password", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-ssl", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{_testBindingName}:ssl"]);
        Assert.Equal("test-username", configData[$"steeltoe:{CassandraPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
    }

    [Fact]
    public void ConfigServerTest_BindingTypeDisabled()
    {
        var postProcessor = new ConfigServerPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, ConfigServerPostProcessor.BindingTypeKey, Tuple.Create("uri", "test-uri"), Tuple.Create("client-id", "test-client-id"), Tuple.Create("client-secret", "test-client-secret"), Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ConfigServerPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{_testBindingName}:uri", out _));
    }

    [Fact]
    public void ConfigServerTest_BindingTypeEnabled()
    {
        var postProcessor = new ConfigServerPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, ConfigServerPostProcessor.BindingTypeKey, Tuple.Create("uri", "test-uri"), Tuple.Create("client-id", "test-client-id"), Tuple.Create("client-secret", "test-client-secret"), Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ConfigServerPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-client-id", configData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{_testBindingName}:oauth2:clientId"]);
        Assert.Equal("test-client-secret", configData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{_testBindingName}:oauth2:clientSecret"]);
        Assert.Equal("test-access-token-uri", configData[$"steeltoe:{ConfigServerPostProcessor.BindingTypeKey}:{_testBindingName}:oauth2:accessTokenUri"]);
    }

    [Fact]
    public void CouchbaseTest_BindingTypeDisabled()
    {
        var postProcessor = new CouchbasePostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName,
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
        Assert.False(configData.TryGetValue($"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{_testBindingName}:ssl", out _));
    }

    [Fact]
    public void CouchbaseTest_BindingTypeEnabled()
    {
        var postProcessor = new CouchbasePostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName,
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
        Assert.Equal("test-bootstrap-hosts", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{_testBindingName}:bootstrapHosts"]);
        Assert.Equal("test-bucket-name", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{_testBindingName}:bucketName"]);
        Assert.Equal("test-bucket-password", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{_testBindingName}:bucketPassword"]);

        Assert.Equal(
            "test-env-bootstrap-http-direct-port",
            configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{_testBindingName}:envBootstrapHttpDirectPort"]);

        Assert.Equal(
            "test-env-bootstrap-http-ssl-port",
            configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{_testBindingName}:envBootstrapHttpSslPort"]);

        Assert.Equal("test-password", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-username", configData[$"steeltoe:{CouchbasePostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
    }

    [Fact]
    public void DB2Test_BindingTypeDisabled()
    {
        var postProcessor = new DB2PostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName,
            DB2PostProcessor.BindingTypeKey,
            Tuple.Create("database", "test-database"),
            Tuple.Create("host", "test-host"),
            Tuple.Create("password", "test-password"),
            Tuple.Create("port", "test-port"),
            Tuple.Create("jdbc-url", "test-jdbc-url"),
            Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, DB2PostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{DB2PostProcessor.BindingTypeKey}:{_testBindingName}:database", out _));
    }

    [Fact]
    public void DB2Test_BindingTypeEnabled()
    {
        var postProcessor = new DB2PostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, DB2PostProcessor.BindingTypeKey, Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, DB2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{_testBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{DB2PostProcessor.BindingTypeKey}:{_testBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void ElasticSearchTest_BindingTypeDisabled()
    {
        var postProcessor = new ElasticSearchPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, ElasticSearchPostProcessor.BindingTypeKey, Tuple.Create("endpoints", "test-endpoints"), Tuple.Create("password", "test-password"), Tuple.Create("use-ssl", "test-use-ssl"), Tuple.Create("username", "test-username"), Tuple.Create("proxy.host", "test-proxy-host"), Tuple.Create("proxy.port", "test-proxy-port"), Tuple.Create("uris", "test-uris"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ElasticSearchPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{_testBindingName}:endpoints", out _));
    }

    [Fact]
    public void ElasticSearchTest_BindingTypeEnabled()
    {
        var postProcessor = new ElasticSearchPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, ElasticSearchPostProcessor.BindingTypeKey, Tuple.Create("endpoints", "test-endpoints"), Tuple.Create("password", "test-password"), Tuple.Create("use-ssl", "test-use-ssl"), Tuple.Create("username", "test-username"), Tuple.Create("proxy.host", "test-proxy-host"), Tuple.Create("proxy.port", "test-proxy-port"), Tuple.Create("uris", "test-uris"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, ElasticSearchPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-endpoints", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{_testBindingName}:endpoints"]);
        Assert.Equal("test-password", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-use-ssl", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{_testBindingName}:useSsl"]);
        Assert.Equal("test-username", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
        Assert.Equal("test-proxy-host", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{_testBindingName}:proxyHost"]);
        Assert.Equal("test-proxy-port", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{_testBindingName}:proxyPort"]);
        Assert.Equal("test-uris", configData[$"steeltoe:{ElasticSearchPostProcessor.BindingTypeKey}:{_testBindingName}:uris"]);
    }

    [Fact]
    public void EurekaTest_BindingTypeDisabled()
    {
        var postProcessor = new EurekaPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, EurekaPostProcessor.BindingTypeKey, Tuple.Create("uri", "test-uri"), Tuple.Create("client-id", "test-client-id"), Tuple.Create("client-secret", "test-client-secret"), Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, EurekaPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{_testBindingName}:uri", out _));
    }

    [Fact]
    public void EurekaTest_BindingTypeEnabled()
    {
        var postProcessor = new EurekaPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, EurekaPostProcessor.BindingTypeKey, Tuple.Create("uri", "test-uri"), Tuple.Create("client-id", "test-client-id"), Tuple.Create("client-secret", "test-client-secret"), Tuple.Create("access-token-uri", "test-access-token-uri"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, EurekaPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("default", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{_testBindingName}:region"]);
        Assert.Equal("test-client-id", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{_testBindingName}:oauth2:clientId"]);
        Assert.Equal("test-client-secret", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{_testBindingName}:oauth2:clientSecret"]);
        Assert.Equal("test-access-token-uri", configData[$"steeltoe:{EurekaPostProcessor.BindingTypeKey}:{_testBindingName}:oauth2:accessTokenUri"]);
    }

    [Fact]
    public void KafkaTest_BindingTypeDisabled()
    {
        var postProcessor = new KafkaPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, KafkaPostProcessor.BindingTypeKey, Tuple.Create("bootstrap-servers", "test-bootstrap-servers"), Tuple.Create("consumer.bootstrap-servers", "test-consumer-bootstrap-servers"), Tuple.Create("producer.bootstrap-servers", "test-producer-bootstrap-servers"), Tuple.Create("streams.bootstrap-servers", "test-streams-bootstrap-servers"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, KafkaPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{_testBindingName}:bootstrapServers", out _));
    }

    [Fact]
    public void KafkaTest_BindingTypeEnabled()
    {
        var postProcessor = new KafkaPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, KafkaPostProcessor.BindingTypeKey, Tuple.Create("bootstrap-servers", "test-bootstrap-servers"), Tuple.Create("consumer.bootstrap-servers", "test-consumer-bootstrap-servers"), Tuple.Create("producer.bootstrap-servers", "test-producer-bootstrap-servers"), Tuple.Create("streams.bootstrap-servers", "test-streams-bootstrap-servers"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, KafkaPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-bootstrap-servers", configData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{_testBindingName}:bootstrapServers"]);
        Assert.Equal("test-consumer-bootstrap-servers", configData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{_testBindingName}:consumerBootstrapServers"]);
        Assert.Equal("test-producer-bootstrap-servers", configData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{_testBindingName}:producerBootstrapServers"]);
        Assert.Equal("test-streams-bootstrap-servers", configData[$"steeltoe:{KafkaPostProcessor.BindingTypeKey}:{_testBindingName}:streamsBootstrapServers"]);
    }

    [Fact]
    public void LdapTest_BindingTypeDisabled()
    {
        var postProcessor = new LdapPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, LdapPostProcessor.BindingTypeKey, Tuple.Create("urls", "test-urls"), Tuple.Create("password", "test-password"), Tuple.Create("username", "test-username"), Tuple.Create("base", "test-base"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, LdapPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{LdapPostProcessor.BindingTypeKey}:{_testBindingName}:urls", out _));
    }

    [Fact]
    public void LdapTest_BindingTypeEnabled()
    {
        var postProcessor = new LdapPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, LdapPostProcessor.BindingTypeKey, Tuple.Create("urls", "test-urls"), Tuple.Create("password", "test-password"), Tuple.Create("username", "test-username"), Tuple.Create("base", "test-base"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, LdapPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-urls", configData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{_testBindingName}:urls"]);
        Assert.Equal("test-password", configData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-username", configData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
        Assert.Equal("test-base", configData[$"steeltoe:{LdapPostProcessor.BindingTypeKey}:{_testBindingName}:base"]);
    }

    [Fact]
    public void MongoDbTest_BindingTypeDisabled()
    {
        var postProcessor = new MongoDbPostProcessor();
        Dictionary<string, string> configData = GetConfigData(_testBindingName, MongoDbPostProcessor.BindingTypeKey, Tuple.Create("authentication-database", "test-authentication-database"), Tuple.Create("database", "test-database"), Tuple.Create("grid-fs-database", "test-grid-fs-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("uri", "test-uri"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{_testBindingName}:authenticationDatabase", out _));
    }

    [Fact]
    public void MongoDbTest_BindingTypeEnabled()
    {
        var postProcessor = new MongoDbPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, MongoDbPostProcessor.BindingTypeKey, Tuple.Create("authentication-database", "test-authentication-database"), Tuple.Create("database", "test-database"), Tuple.Create("grid-fs-database", "test-grid-fs-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("uri", "test-uri"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MongoDbPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-authentication-database", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{_testBindingName}:authenticationDatabase"]);
        Assert.Equal("test-database", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{_testBindingName}:database"]);
        Assert.Equal("test-grid-fs-database", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{_testBindingName}:gridfsDatabase"]);
        Assert.Equal("test-host", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-uri", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-username", configData[$"steeltoe:{MongoDbPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
    }

    [Fact]
    public void MySqlTest_BindingTypeDisabled()
    {
        var postProcessor = new MySqlPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, MySqlPostProcessor.BindingTypeKey, Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{_testBindingName}:database", out _));
    }

    [Fact]
    public void MySqlTest_BindingTypeEnabled()
    {
        var postProcessor = new MySqlPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, MySqlPostProcessor.BindingTypeKey, Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, MySqlPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{_testBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{MySqlPostProcessor.BindingTypeKey}:{_testBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void Neo4JTest_BindingTypeDisabled()
    {
        var postProcessor = new Neo4JPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, Neo4JPostProcessor.BindingTypeKey, Tuple.Create("password", "test-password"), Tuple.Create("uri", "test-uri"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, Neo4JPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{_testBindingName}:password", out _));
    }

    [Fact]
    public void Neo4JTest_BindingTypeEnabled()
    {
        var postProcessor = new Neo4JPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, Neo4JPostProcessor.BindingTypeKey, Tuple.Create("password", "test-password"), Tuple.Create("uri", "test-uri"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, Neo4JPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-password", configData[$"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-uri", configData[$"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-username", configData[$"steeltoe:{Neo4JPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
    }

    [Fact]
    public void OracleTest_BindingTypeDisabled()
    {
        var postProcessor = new OraclePostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, OraclePostProcessor.BindingTypeKey, Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, OraclePostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{OraclePostProcessor.BindingTypeKey}:{_testBindingName}:database", out _));
    }

    [Fact]
    public void OracleTest_BindingTypeEnabled()
    {
        var postProcessor = new OraclePostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, OraclePostProcessor.BindingTypeKey, Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, OraclePostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{_testBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{OraclePostProcessor.BindingTypeKey}:{_testBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeDisabled()
    {
        var postProcessor = new PostgreSqlPostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName,
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
        Assert.False(configData.TryGetValue($"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:database", out _));
    }

    [Fact]
    public void PostgreSqlTest_BindingTypeEnabled()
    {
        var postProcessor = new PostgreSqlPostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName,
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
        Assert.Equal("test-database", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:jdbcUrl"]);
        Assert.Equal("verify-full", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:sslmode"]);
        Assert.Equal("root.cert", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:sslrootcert"]);
        Assert.Equal("--cluster=routing-id&opt=val1", configData[$"steeltoe:{PostgreSqlPostProcessor.BindingTypeKey}:{_testBindingName}:options"]);
    }

    [Fact]
    public void RabbitMQTest_BindingTypeDisabled()
    {
        var postProcessor = new RabbitMQPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, RabbitMQPostProcessor.BindingTypeKey, Tuple.Create("addresses", "test-addresses"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("username", "test-username"), Tuple.Create("virtual-host", "test-virtual-host"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{_testBindingName}:addresses", out _));
    }

    [Fact]
    public void RabbitMQTest_BindingTypeEnabled()
    {
        var postProcessor = new RabbitMQPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, RabbitMQPostProcessor.BindingTypeKey, Tuple.Create("addresses", "test-addresses"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("username", "test-username"), Tuple.Create("virtual-host", "test-virtual-host"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RabbitMQPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-addresses", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{_testBindingName}:addresses"]);
        Assert.Equal("test-host", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
        Assert.Equal("test-virtual-host", configData[$"steeltoe:{RabbitMQPostProcessor.BindingTypeKey}:{_testBindingName}:virtualHost"]);
    }

    [Fact]
    public void RedisTest_BindingTypeDisabled()
    {
        var postProcessor = new RedisPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, RedisPostProcessor.BindingTypeKey, Tuple.Create("client-name", "test-client-name"), Tuple.Create("cluster.max-redirects", "test-cluster-max-redirects"), Tuple.Create("cluster.nodes", "test-cluster-nodes"), Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("sentinel.master", "test-sentinel-master"), Tuple.Create("sentinel.nodes", "test-sentinel-nodes"), Tuple.Create("ssl", "test-ssl"), Tuple.Create("url", "test-url"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:clientname", out _));
    }

    [Fact]
    public void RedisTest_BindingTypeEnabled()
    {
        var postProcessor = new RedisPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, RedisPostProcessor.BindingTypeKey, Tuple.Create("client-name", "test-client-name"), Tuple.Create("cluster.max-redirects", "test-cluster-max-redirects"), Tuple.Create("cluster.nodes", "test-cluster-nodes"), Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("sentinel.master", "test-sentinel-master"), Tuple.Create("sentinel.nodes", "test-sentinel-nodes"), Tuple.Create("ssl", "test-ssl"), Tuple.Create("url", "test-url"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, RedisPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-client-name", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:clientName"]);
        Assert.Equal("test-cluster-max-redirects", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:clusterMaxRedirects"]);
        Assert.Equal("test-cluster-nodes", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:clusterNodes"]);
        Assert.Equal("test-database", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-sentinel-master", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:sentinelMaster"]);
        Assert.Equal("test-sentinel-nodes", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:sentinelNodes"]);
        Assert.Equal("test-ssl", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:ssl"]);
        Assert.Equal("test-url", configData[$"steeltoe:{RedisPostProcessor.BindingTypeKey}:{_testBindingName}:url"]);
    }

    [Fact]
    public void SapHanaTest_BindingTypeDisabled()
    {
        var postProcessor = new SapHanaPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, SapHanaPostProcessor.BindingTypeKey, Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SapHanaPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{_testBindingName}:database", out _));
    }

    [Fact]
    public void SapHanaTest_BindingTypeEnabled()
    {
        var postProcessor = new SapHanaPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, SapHanaPostProcessor.BindingTypeKey, Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SapHanaPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{_testBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{SapHanaPostProcessor.BindingTypeKey}:{_testBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_BindingTypeDisabled()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName1, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "github", Tuple.Create("client-id", "github-client-id"), Tuple.Create("client-secret", "github-client-secret"));

        GetConfigData(configData, _testBindingName2, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "okta", Tuple.Create("client-id", "okta-client-id"), Tuple.Create("client-secret", "okta-client-secret"), Tuple.Create("issuer-uri", "okta-issuer-uri"));

        GetConfigData(
            configData,
            _testBindingName3,
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

        GetConfigData(configData, _testMissingProvider, SpringSecurityOAuth2PostProcessor.BindingTypeKey, Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName1}:registration:clientId", out _));
    }

    [Fact]
    public void SpringSecurityOauth2Test_CommonProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName1, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "github", Tuple.Create("client-id", "github-client-id"), Tuple.Create("client-secret", "github-client-secret"));

        GetConfigData(configData, _testBindingName2, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "okta", Tuple.Create("client-id", "okta-client-id"), Tuple.Create("client-secret", "okta-client-secret"), Tuple.Create("issuer-uri", "okta-issuer-uri"));

        GetConfigData(configData, _testBindingName3, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "my-provider", Tuple.Create("client-id", "my-provider-client-id"), Tuple.Create("client-secret", "my-provider-client-secret"), Tuple.Create("client-authentication-method", "my-provider-client-authentication-method"), Tuple.Create("authorization-grant-type", "my-provider-authorization-grant-type"), Tuple.Create("redirect-uri", "my-provider-redirect-uri"), Tuple.Create("scope", "my-provider-scope1,my-provider-scope2"), Tuple.Create("client-name", "my-provider-client-name"), Tuple.Create("authorization-uri", "my-provider-authorization-uri"), Tuple.Create("token-uri", "my-provider-token-uri"), Tuple.Create("user-info-uri", "my-provider-user-info-uri"), Tuple.Create("user-info-authentication-method", "my-provider-user-info-authentication-method"), Tuple.Create("jwk-set-uri", "my-provider-jwk-set-uri"), Tuple.Create("user-name-attribute", "my-provider-user-name-attribute"));

        GetConfigData(configData, _testMissingProvider, SpringSecurityOAuth2PostProcessor.BindingTypeKey, Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("github-client-id", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName1}:registration:clientId"]);

        Assert.Equal(
            "github-client-secret",
            configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName1}:registration:clientSecret"]);

        Assert.Equal("github", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName1}:registration:provider"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_OIDCProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configData = GetConfigData(
            _testBindingName1,
            SpringSecurityOAuth2PostProcessor.BindingTypeKey,
            "github",
            Tuple.Create("client-id", "github-client-id"),
            Tuple.Create("client-secret", "github-client-secret"));

        GetConfigData(configData, _testBindingName2, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "okta", Tuple.Create("client-id", "okta-client-id"), Tuple.Create("client-secret", "okta-client-secret"), Tuple.Create("issuer-uri", "okta-issuer-uri"));

        GetConfigData(
            configData,
            _testBindingName3,
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

        GetConfigData(configData, _testMissingProvider, SpringSecurityOAuth2PostProcessor.BindingTypeKey, Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("okta-client-id", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName2}:registration:clientId"]);

        Assert.Equal("okta-client-secret", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName2}:registration:clientSecret"]);

        Assert.Equal("okta", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName2}:registration:provider"]);
        Assert.Equal("okta-issuer-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName2}:provider:issuerUri"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_TestProvider()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName1, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "github", Tuple.Create("client-id", "github-client-id"), Tuple.Create("client-secret", "github-client-secret"));

        GetConfigData(configData, _testBindingName2, SpringSecurityOAuth2PostProcessor.BindingTypeKey, "okta", Tuple.Create("client-id", "okta-client-id"), Tuple.Create("client-secret", "okta-client-secret"), Tuple.Create("issuer-uri", "okta-issuer-uri"));

        GetConfigData(
            configData,
            _testBindingName3,
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

        GetConfigData(configData, _testMissingProvider, SpringSecurityOAuth2PostProcessor.BindingTypeKey, Tuple.Create("client-id", "my-provider-client-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("my-provider", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:registration:provider"]);

        Assert.Equal("my-provider-client-id", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:registration:clientId"]);

        Assert.Equal("my-provider-client-secret", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:registration:clientSecret"]);

        Assert.Equal("my-provider-client-authentication-method", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:registration:clientAuthenticationMethod"]);

        Assert.Equal("my-provider-authorization-grant-type", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:registration:authorizationGrantType"]);

        Assert.Equal("my-provider-redirect-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:registration:redirectUri"]);

        Assert.Equal("my-provider-scope1,my-provider-scope2", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:registration:scope"]);

        Assert.Equal("my-provider-client-name", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:registration:clientName"]);

        Assert.Equal("my-provider-authorization-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:provider:authorizationUri"]);

        Assert.Equal("my-provider-token-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:provider:tokenUri"]);

        Assert.Equal("my-provider-user-info-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:provider:userInfoUri"]);

        Assert.Equal("my-provider-user-info-authentication-method", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:provider:userInfoAuthenticationMethod"]);

        Assert.Equal("my-provider-jwk-set-uri", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:provider:jwkSetUri"]);

        Assert.Equal("my-provider-user-name-attribute", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName3}:provider:userNameAttribute"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_AuthGrantTypes()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, SpringSecurityOAuth2PostProcessor.BindingTypeKey, Tuple.Create("provider", "some-provider"), Tuple.Create("authorization-grant-types", "authorization_code,client_credentials"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);

        Assert.Equal("authorization_code,client_credentials", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName}:registration:authorizationGrantTypes"]);
    }

    [Fact]
    public void SpringSecurityOauth2Test_RedirectUris()
    {
        var postProcessor = new SpringSecurityOAuth2PostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, SpringSecurityOAuth2PostProcessor.BindingTypeKey, Tuple.Create("provider", "some-provider"), Tuple.Create("redirect-uris", "https://app.example.com/authorized,https://other-app.example.com/login"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SpringSecurityOAuth2PostProcessor.BindingTypeKey, true), configData);

        Assert.Equal("https://app.example.com/authorized,https://other-app.example.com/login", configData[$"steeltoe:{SpringSecurityOAuth2PostProcessor.BindingTypeKey}:{_testBindingName}:registration:redirectUris"]);
    }

    [Fact]
    public void SqlServerTest_BindingTypeDisabled()
    {
        var postProcessor = new SqlServerPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, SqlServerPostProcessor.BindingTypeKey, Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{_testBindingName}:database", out _));
    }

    [Fact]
    public void SqlServerTest_BindingTypeEnabled()
    {
        var postProcessor = new SqlServerPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, SqlServerPostProcessor.BindingTypeKey, Tuple.Create("database", "test-database"), Tuple.Create("host", "test-host"), Tuple.Create("password", "test-password"), Tuple.Create("port", "test-port"), Tuple.Create("jdbc-url", "test-jdbc-url"), Tuple.Create("username", "test-username"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, SqlServerPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-database", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{_testBindingName}:database"]);
        Assert.Equal("test-host", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{_testBindingName}:host"]);
        Assert.Equal("test-password", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{_testBindingName}:password"]);
        Assert.Equal("test-port", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{_testBindingName}:port"]);
        Assert.Equal("test-username", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{_testBindingName}:username"]);
        Assert.Equal("test-jdbc-url", configData[$"steeltoe:{SqlServerPostProcessor.BindingTypeKey}:{_testBindingName}:jdbcUrl"]);
    }

    [Fact]
    public void VaultTest_BindingTypeDisabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "token"), Tuple.Create("token", "test-token"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:token", out _));
    }

    [Fact]
    public void VaultTokenAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "token"), Tuple.Create("token", "test-token"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-token", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:token"]);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("token", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
    }

    [Fact]
    public void VaultAppRoleAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("app-role-path", "test-app-role-path"), Tuple.Create("authentication-method", "approle"), Tuple.Create("role", "test-role"), Tuple.Create("role-id", "test-role-id"), Tuple.Create("secret-id", "test-secret-id"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.Equal("approle", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("test-role-id", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:approle:roleId"]);
        Assert.Equal("test-secret-id", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:approle:secretId"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:approle:role"]);
        Assert.Equal("test-app-role-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:approle:appRolePath"]);
    }

    [Fact]
    public void VaultCubbyHoleAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "cubbyhole"), Tuple.Create("token", "test-token"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.Equal("cubbyhole", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("test-token", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:token"]);
    }

    [Fact]
    public void VaultCertAuthenticationTest_BindingTypeEnabled()
    {
        // Note: This will likely need to be revisited.  See the VaultPostProcessor for more comments
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "cert"), Tuple.Create("cert-auth-path", "test-cert-auth-path"), Tuple.Create("key-store-password", "test-key-store-password"), Tuple.Create("key-store", "key store contents!"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.Equal("cert", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("test-cert-auth-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:ssl:certAuthPath"]);
        Assert.Equal("test-key-store-password", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:ssl:keyStorePassword"]);
        Assert.Equal("key store contents!", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:ssl:keyStore"]);
    }

    [Fact]
    public void VaultAwsEc2AuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "aws_ec2"), Tuple.Create("aws-ec2-instance-identity-document", "test-identity-document"), Tuple.Create("nonce", "test-nonce"), Tuple.Create("aws-ec2-path", "test-aws-ec2-path"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.Equal("aws_ec2", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:awsEc2:role"]);
        Assert.Equal("test-aws-ec2-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:awsEc2:awsEc2Path"]);
        Assert.Equal("test-identity-document", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:awsEc2:identityDocument"]);
        Assert.Equal("test-nonce", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:awsEc2:nonce"]);
    }

    [Fact]
    public void VaultAwsIamAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "aws_iam"), Tuple.Create("aws-iam-server-id", "test-server-id"), Tuple.Create("aws-path", "test-aws-path"), Tuple.Create("aws-sts-endpoint-uri", "test-endpoint-uri"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.Equal("aws_iam", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:awsIam:role"]);
        Assert.Equal("test-aws-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:awsIam:awsPath"]);
        Assert.Equal("test-server-id", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:awsIam:serverId"]);
        Assert.Equal("test-endpoint-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:awsIam:endpointUri"]);
    }

    [Fact]
    public void VaultAzureMsiAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "azure_msi"), Tuple.Create("azure-path", "test-azure-path"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.Equal("azure_msi", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:azureMsi:role"]);
        Assert.Equal("test-azure-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:azureMsi:azurePath"]);
    }

    [Fact]
    public void VaultGcpGceAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "gcp_gce"), Tuple.Create("gcp-path", "test-gcp-path"), Tuple.Create("gcp-service-account", "test-service-account"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.Equal("gcp_gce", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpGce:role"]);
        Assert.Equal("test-gcp-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpGce:gcpPath"]);
        Assert.Equal("test-service-account", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpGce:gcpServiceAccount"]);
    }

    [Fact]
    public void VaultGcpIamAuthenticationTest_BindingTypeEnabled()
    {
        // Note: This will likely need to be revisited.  See the VaultPostProcessor for more comments
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "gcp_iam"), Tuple.Create("credentials.json", "credentials JSON contents!"), Tuple.Create("encoded-key", "test-encoded-key"), Tuple.Create("gcp-path", "test-gcp-path"), Tuple.Create("gcp-project-id", "test-project-id"), Tuple.Create("gcp-service-account", "test-service-account"), Tuple.Create("jwt-validity", "test-jwt-validity"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.Equal("gcp_iam", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("credentials JSON contents!", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpIam:credentialsJson"]);
        Assert.Equal("test-encoded-key", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpIam:encodedKey"]);
        Assert.Equal("test-gcp-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpIam:gcpPath"]);
        Assert.Equal("test-project-id", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpIam:gcpProjectId"]);
        Assert.Equal("test-service-account", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpIam:gcpServiceAccount"]);
        Assert.Equal("test-jwt-validity", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpIam:jwtValidity"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:gcpIam:role"]);
    }

    [Fact]
    public void VaultK8sAuthenticationTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"), Tuple.Create("authentication-method", "kubernetes"), Tuple.Create("kubernetes-path", "test-kubernetes-path"), Tuple.Create("role", "test-role"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.Equal("kubernetes", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication"]);
        Assert.Equal("test-role", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:kubernetes:role"]);
        Assert.Equal("test-kubernetes-path", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:kubernetes:kubernetesPath"]);
    }

    [Fact]
    public void VaultMissingProviderTest_BindingTypeEnabled()
    {
        var postProcessor = new VaultPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, VaultPostProcessor.BindingTypeKey, Tuple.Create("namespace", "test-namespace"), Tuple.Create("uri", "test-uri"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, VaultPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-namespace", configData[$"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:namespace"]);
        Assert.False(configData.TryGetValue($"steeltoe:{VaultPostProcessor.BindingTypeKey}:{_testBindingName}:authentication", out _));
    }

    [Fact]
    public void WavefrontTest_BindingTypeDisabled()
    {
        var postProcessor = new WavefrontPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, WavefrontPostProcessor.BindingTypeKey, Tuple.Create("api-token", "test-api-token"), Tuple.Create("uri", "test-uri"));

        // BindingType not enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, WavefrontPostProcessor.BindingTypeKey, false), configData);
        Assert.False(configData.TryGetValue($"steeltoe:{WavefrontPostProcessor.BindingTypeKey}:{_testBindingName}:uri", out _));
    }

    [Fact]
    public void WavefrontTest_BindingTypeEnabled()
    {
        var postProcessor = new WavefrontPostProcessor();

        Dictionary<string, string> configData = GetConfigData(_testBindingName, WavefrontPostProcessor.BindingTypeKey, Tuple.Create("api-token", "test-api-token"), Tuple.Create("uri", "test-uri"));

        // BindingType enabled
        postProcessor.PostProcessConfiguration(GetConfigurationProvider(postProcessor, WavefrontPostProcessor.BindingTypeKey, true), configData);
        Assert.Equal("test-uri", configData[$"steeltoe:{WavefrontPostProcessor.BindingTypeKey}:{_testBindingName}:uri"]);
        Assert.Equal("test-api-token", configData[$"steeltoe:{WavefrontPostProcessor.BindingTypeKey}:{_testBindingName}:apiToken"]);
    }
}
