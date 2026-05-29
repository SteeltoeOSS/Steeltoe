// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

namespace Steeltoe.Configuration.CloudFoundry.Test.ServiceBindings;

public sealed class PostProcessorsTest : BasePostProcessorsTest
{
    [Fact]
    public void Processes_MySql_configuration()
    {
        List<string> tempPaths = [];

        using (var postProcessor = new MySqlCloudFoundryPostProcessor())
        {
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
                GetConfigurationData(TestProviderName, TestBindingName, [MySqlCloudFoundryPostProcessor.BindingType], null, secrets);

            PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

            postProcessor.PostProcessConfiguration(provider, configurationData);

            string keyPrefix = GetOutputKeyPrefix(TestBindingName, MySqlCloudFoundryPostProcessor.BindingType);
            configurationData.Should().ContainKey($"{keyPrefix}:host").WhoseValue.Should().Be("test-host");
            configurationData.Should().ContainKey($"{keyPrefix}:port").WhoseValue.Should().Be("test-port");
            configurationData.Should().ContainKey($"{keyPrefix}:database").WhoseValue.Should().Be("test-database");
            configurationData.Should().ContainKey($"{keyPrefix}:username").WhoseValue.Should().Be("test-username");
            configurationData.Should().ContainKey($"{keyPrefix}:password").WhoseValue.Should().Be("test-password");

            foreach ((string key, string expectedValue) in new Dictionary<string, string>
            {
                ["ssl-cert"] = "test-ssl-cert",
                ["ssl-key"] = "test-ssl-key",
                ["ssl-ca"] = "test-ssl-root-cert"
            })
            {
                string? tempPath = configurationData.Should().ContainKey($"{keyPrefix}:{key}").WhoseValue;

                AssertFileHasContent(tempPath, expectedValue);
                AssertUnixFileModeIsUserOnly(tempPath);

                tempPaths.Add(tempPath);
            }
        }

        foreach (string tempPath in tempPaths)
        {
            File.Exists(tempPath).Should().BeFalse();
        }
    }

    [Fact]
    public void Processes_PostgreSql_configuration()
    {
        List<string> tempPaths = [];

        using (var postProcessor = new PostgreSqlCloudFoundryPostProcessor())
        {
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
            configurationData.Should().ContainKey($"{keyPrefix}:host").WhoseValue.Should().Be("test-host");
            configurationData.Should().ContainKey($"{keyPrefix}:port").WhoseValue.Should().Be("test-port");
            configurationData.Should().ContainKey($"{keyPrefix}:database").WhoseValue.Should().Be("test-database");
            configurationData.Should().ContainKey($"{keyPrefix}:username").WhoseValue.Should().Be("test-username");
            configurationData.Should().ContainKey($"{keyPrefix}:password").WhoseValue.Should().Be("test-password");

            foreach ((string key, string expectedValue) in new Dictionary<string, string>
            {
                ["SSL Certificate"] = "test-ssl-cert",
                ["SSL Key"] = "test-ssl-key",
                ["Root Certificate"] = "test-ssl-root-cert"
            })
            {
                string? tempPath = configurationData.Should().ContainKey($"{keyPrefix}:{key}").WhoseValue;

                AssertFileHasContent(tempPath, expectedValue);
                AssertUnixFileModeIsUserOnly(tempPath);

                tempPaths.Add(tempPath);
            }
        }

        foreach (string tempPath in tempPaths)
        {
            File.Exists(tempPath).Should().BeFalse();
        }
    }

    [Fact]
    public void Processes_PostgreSql_Tanzu_High_Availability_configuration()
    {
        var postProcessor = new PostgreSqlCloudFoundryPostProcessor();

        var secrets = new[]
        {
            Tuple.Create("credentials:hosts:0", "test-host1"),
            Tuple.Create("credentials:hosts:1", "test-host2"),
            Tuple.Create("credentials:port", "test-port"),
            Tuple.Create("credentials:db", "test-database"),
            Tuple.Create("credentials:user", "test-username"),
            Tuple.Create("credentials:password", "test-password")
        };

        Dictionary<string, string?> configurationData =
            GetConfigurationData(TestProviderName, TestBindingName, [PostgreSqlCloudFoundryPostProcessor.TanzuBindingType], null, secrets);

        PostProcessorConfigurationProvider provider = GetConfigurationProvider(postProcessor);

        postProcessor.PostProcessConfiguration(provider, configurationData);

        string keyPrefix = GetOutputKeyPrefix(TestBindingName, PostgreSqlCloudFoundryPostProcessor.BindingType);
        configurationData.Should().ContainKey($"{keyPrefix}:host").WhoseValue.Should().Be("test-host1,test-host2");
        configurationData.Should().ContainKey($"{keyPrefix}:Target Session Attributes").WhoseValue.Should().Be("primary");
        configurationData.Should().ContainKey($"{keyPrefix}:port").WhoseValue.Should().Be("test-port");
        configurationData.Should().ContainKey($"{keyPrefix}:database").WhoseValue.Should().Be("test-database");
        configurationData.Should().ContainKey($"{keyPrefix}:username").WhoseValue.Should().Be("test-username");
        configurationData.Should().ContainKey($"{keyPrefix}:password").WhoseValue.Should().Be("test-password");
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
        configurationData.Should().ContainKey($"{keyPrefix}:useTls").WhoseValue.Should().Be("false");
        configurationData.Should().ContainKey($"{keyPrefix}:host").WhoseValue.Should().Be("test-host");
        configurationData.Should().ContainKey($"{keyPrefix}:port").WhoseValue.Should().Be("test-port");
        configurationData.Should().ContainKey($"{keyPrefix}:username").WhoseValue.Should().Be("test-username");
        configurationData.Should().ContainKey($"{keyPrefix}:password").WhoseValue.Should().Be("test-password");
        configurationData.Should().ContainKey($"{keyPrefix}:virtualHost").WhoseValue.Should().Be("test-vhost");
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
        configurationData.Should().ContainKey($"{keyPrefix}:host").WhoseValue.Should().Be("test-host");
        configurationData.Should().ContainKey($"{keyPrefix}:port").WhoseValue.Should().Be("test-port");
        configurationData.Should().ContainKey($"{keyPrefix}:password").WhoseValue.Should().Be("test-password");
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
        configurationData.Should().ContainKey($"{keyPrefix}:host").WhoseValue.Should().Be("test-host");
        configurationData.Should().ContainKey($"{keyPrefix}:port").WhoseValue.Should().Be("test-port");
        configurationData.Should().ContainKey($"{keyPrefix}:ssl").WhoseValue.Should().Be("true");
        configurationData.Should().ContainKey($"{keyPrefix}:password").WhoseValue.Should().Be("test-password");
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
        configurationData.Should().ContainKey($"{keyPrefix}:Data Source").WhoseValue.Should().Be("test-host,test-port");
        configurationData.Should().ContainKey($"{keyPrefix}:Initial Catalog").WhoseValue.Should().Be("test-database");
        configurationData.Should().ContainKey($"{keyPrefix}:User ID").WhoseValue.Should().Be("test-username");
        configurationData.Should().ContainKey($"{keyPrefix}:Password").WhoseValue.Should().Be("test-password");
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
        configurationData.Should().ContainKey($"{keyPrefix}:url").WhoseValue.Should().Be("mongodb://localhost:27017/auth-db?appname=sample");
        configurationData.Should().ContainKey($"{keyPrefix}:database").WhoseValue.Should().Be("auth-db");
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
        configurationData.Should().ContainKey($"{keyPrefix}:ServiceUrl").WhoseValue.Should().Be("test-uri/eureka/");
        configurationData.Should().ContainKey($"{keyPrefix}:ClientId").WhoseValue.Should().Be("test-client-id");
        configurationData.Should().ContainKey($"{keyPrefix}:ClientSecret").WhoseValue.Should().Be("test-client-secret");
        configurationData.Should().ContainKey($"{keyPrefix}:AccessTokenUri").WhoseValue.Should().Be("test-access-token-uri");
        configurationData.Should().ContainKey($"{keyPrefix}:Enabled").WhoseValue.Should().Be("true");
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
            configurationData.Should().ContainKey($"{keyPrefix}:Authority").WhoseValue.Should().Be("test-domain");
            configurationData.Should().ContainKey($"{keyPrefix}:ClientId").WhoseValue.Should().Be("test-id");
            configurationData.Should().ContainKey($"{keyPrefix}:ClientSecret").WhoseValue.Should().Be("test-secret");
        }
    }

    private static void AssertFileHasContent([NotNull] string? path, string expectedValue)
    {
        path.Should().NotBeNull();
        File.Exists(path).Should().BeTrue();

        string fileContent = File.ReadAllText(path);
        fileContent.Should().Be(expectedValue);
    }

    private static void AssertUnixFileModeIsUserOnly(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            UnixFileMode fileMode = File.GetUnixFileMode(path);
            fileMode.Should().Be(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
