// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed partial class ConfigServerConfigurationProviderTest
{
    [Fact]
    public async Task Deserialize_GoodJson()
    {
        var environment = new ConfigEnvironment
        {
            Name = "test-name",
            Label = "test-label",
            Profiles =
            {
                "Production"
            },
            Version = "test-version",
            State = "test-state",
            PropertySources =
            {
                new PropertySource
                {
                    Name = "source",
                    Source =
                    {
                        ["key1"] = "value1",
                        ["key2"] = 10
                    }
                }
            }
        };

        var content = JsonContent.Create(environment);

        var env = await content.ReadFromJsonAsync<ConfigEnvironment>(ConfigServerConfigurationProvider.SerializerOptions,
            TestContext.Current.CancellationToken);

        env.Should().NotBeNull();
        env.Name.Should().Be("test-name");
        env.Profiles.Should().ContainSingle();
        env.Label.Should().Be("test-label");
        env.Version.Should().Be("test-version");
        env.State.Should().Be("test-state");

        PropertySource source = env.PropertySources.Should().ContainSingle().Subject;
        source.Name.Should().Be("source");
        source.Source.Should().HaveCount(2);
        source.Source.Should().ContainKey("key1").WhoseValue.ToString().Should().Be("value1");
        source.Source.Should().ContainKey("key2").WhoseValue.ToString().Should().Be("10");
    }

    [Fact]
    public void GetLabels_Null()
    {
        var options = new ConfigServerClientOptions();
        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        result.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [Fact]
    public void GetLabels_Empty()
    {
        var options = new ConfigServerClientOptions
        {
            Label = string.Empty
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        result.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [Fact]
    public void GetLabels_SingleString()
    {
        var options = new ConfigServerClientOptions
        {
            Label = "foobar"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        result.Should().ContainSingle().Which.Should().Be("foobar");
    }

    [Fact]
    public void GetLabels_MultiString()
    {
        var options = new ConfigServerClientOptions
        {
            Label = "1,2,3,"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        result.Should().HaveCount(3);
        result.Should().HaveElementAt(0, "1");
        result.Should().HaveElementAt(1, "2");
        result.Should().HaveElementAt(2, "3");
    }

    [Fact]
    public void GetLabels_MultiStringHoles()
    {
        var options = new ConfigServerClientOptions
        {
            Label = "1,,2,3,"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        result.Should().HaveCount(3);
        result.Should().HaveElementAt(0, "1");
        result.Should().HaveElementAt(1, "2");
        result.Should().HaveElementAt(2, "3");
    }

    [Fact]
    public async Task GetRequestMessage_AddsBasicAuthIfUserNameAndPasswordInURL()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://user:password@localhost:8888/",
            Name = "foo",
            Environment = "development"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Uri requestUri = provider.BuildConfigServerUri(new Uri(options.Uri), null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, TestContext.Current.CancellationToken);

        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(requestUri);
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization.Scheme.Should().Be("Basic");
        request.Headers.Authorization.Parameter.Should().Be(GetEncodedUserPassword("user", "password"));
    }

    [Fact]
    public async Task GetRequestMessage_AddsBasicAuthIfUserNameAndPasswordInSettings()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:8888/",
            Name = "foo",
            Environment = "development",
            Username = "user",
            Password = "password"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Uri requestUri = provider.BuildConfigServerUri(new Uri(options.Uri), null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, TestContext.Current.CancellationToken);

        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(requestUri);
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization.Scheme.Should().Be("Basic");
        request.Headers.Authorization.Parameter.Should().Be(GetEncodedUserPassword("user", "password"));
    }

    [Fact]
    public async Task GetRequestMessage_BasicAuthInSettingsOverridesUserNameAndPasswordInURL()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://ignored-1:ignored-2@localhost:8888/",
            Name = "foo",
            Environment = "development",
            Username = "user",
            Password = "password"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Uri requestUri = provider.BuildConfigServerUri(new Uri(options.Uri), null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, TestContext.Current.CancellationToken);

        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(requestUri);
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization.Scheme.Should().Be("Basic");
        request.Headers.Authorization.Parameter.Should().Be(GetEncodedUserPassword("user", "password"));
    }

    [Fact]
    public async Task GetRequestMessage_AddsVaultToken_IfNeeded()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Token = "MyVaultToken"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Uri requestUri = provider.BuildConfigServerUri(new Uri(options.Uri!), null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, TestContext.Current.CancellationToken);

        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(requestUri);
        request.Headers.Contains(ConfigServerConfigurationProvider.TokenHeader).Should().BeTrue();
        IEnumerable<string> headerValues = request.Headers.GetValues(ConfigServerConfigurationProvider.TokenHeader);
        headerValues.Should().Contain("MyVaultToken");
    }

    [Fact]
    public async Task RefreshVaultToken_Succeeds()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Token = "MyVaultToken"
        };

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8888/vault/v1/auth/token/renew-self").WithHeaders("X-Vault-Token", "MyVaultToken")
            .WithContent("{\"increment\":300}").Respond(HttpStatusCode.NoContent);

        using var provider = new ConfigServerConfigurationProvider(options, null, handler, NullLoggerFactory.Instance);
        await provider.RefreshVaultTokenAsync(TestContext.Current.CancellationToken);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RefreshVaultToken_With_AccessTokenUri_Succeeds()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Token = "MyVaultToken",
            AccessTokenUri = "https://auth.server.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "https://auth.server.com/").WithHeaders("Authorization", "Basic dGVzdC1jbGllbnQtaWQ6dGVzdC1jbGllbnQtc2VjcmV0")
            .WithFormData("grant_type=client_credentials").Respond("application/json", "{ \"access_token\": \"secret\" }");

        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8888/vault/v1/auth/token/renew-self").WithHeaders("X-Vault-Token", "MyVaultToken")
            .WithHeaders("Authorization", "Bearer secret").WithContent("{\"increment\":300}").Respond(HttpStatusCode.NoContent);

        using var provider = new ConfigServerConfigurationProvider(options, null, handler, NullLoggerFactory.Instance);
        await provider.RefreshVaultTokenAsync(TestContext.Current.CancellationToken);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void GetHttpClient_AddsHeaders_IfConfigured()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Headers =
            {
                ["foo"] = "bar",
                ["bar"] = "foo"
            }
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);
        using HttpClient httpClient = provider.CreateHttpClient(options);

        httpClient.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.GetValues("foo").SingleOrDefault().Should().Be("bar");
        httpClient.DefaultRequestHeaders.GetValues("bar").SingleOrDefault().Should().Be("foo");
    }

    [Fact]
    public void IsDiscoveryFirstEnabled_ReturnsExpected()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Discovery =
            {
                Enabled = true
            }
        };

        using (var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance))
        {
            provider.IsDiscoveryFirstEnabled().Should().BeTrue();
        }

        var values = new Dictionary<string, string?>
        {
            ["spring:cloud:config:discovery:enabled"] = "True"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development"
        };

        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);

        using (var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance))
        {
            provider.IsDiscoveryFirstEnabled().Should().BeTrue();
        }
    }

    [Fact]
    public void UpdateSettingsFromDiscovery_UpdatesSettingsCorrectly()
    {
        var values = new Dictionary<string, string?>
        {
            ["spring:cloud:config:discovery:enabled"] = "True"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:8888/",
            Name = "foo",
            Environment = "development"
        };

        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);
        using var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        provider.UpdateSettingsFromDiscovery(new List<IServiceInstance>(), options);
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Uri.Should().Be("http://localhost:8888/");

        var metadata1 = new Dictionary<string, string?>
        {
            ["password"] = "firstPassword"
        };

        var metadata2 = new Dictionary<string, string?>
        {
            ["password"] = "secondPassword",
            ["user"] = "secondUser",
            ["configPath"] = "configPath"
        };

        List<IServiceInstance> instances =
        [
            new TestServiceInstance("s", "i1", new Uri("https://foo.bar:8888/"), metadata1),
            new TestServiceInstance("s", "i2", new Uri("https://foo.bar.baz:9999/"), metadata2)
        ];

        provider.UpdateSettingsFromDiscovery(instances, options);
        options.Username.Should().Be("secondUser");
        options.Password.Should().Be("secondPassword");
        options.Uri.Should().Be("https://foo.bar:8888/,https://foo.bar.baz:9999/configPath");
    }

    [Fact]
    public async Task DiscoverServerInstances_FailsFast()
    {
        var values = new Dictionary<string, string?>
        {
            ["spring:cloud:config:discovery:enabled"] = "True",
            ["spring:cloud:config:failFast"] = "True",
            ["eureka:client:eurekaServer:retryCount"] = "0"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var options = new ConfigServerClientOptions
        {
            Name = "foo",
            Environment = "development",
            Timeout = 10
        };

        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);
        using var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<ConfigServerException>().WithMessage("Could not locate Config Server via discovery*");
    }

    private static string GetEncodedUserPassword(string user, string password)
    {
        return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}"));
    }

    private sealed class TestServiceInstance(string serviceId, string instanceId, Uri uri, IReadOnlyDictionary<string, string?> metadata) : IServiceInstance
    {
        public string ServiceId { get; } = serviceId;
        public string InstanceId { get; } = instanceId;
        public string Host { get; } = uri.Host;
        public int Port { get; } = uri.Port;
        public bool IsSecure { get; } = uri.Scheme == Uri.UriSchemeHttps;
        public Uri Uri { get; } = uri;
        public IReadOnlyDictionary<string, string?> Metadata { get; } = metadata;
    }
}
