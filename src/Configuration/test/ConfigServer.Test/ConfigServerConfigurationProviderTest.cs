// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
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
    public async Task Deserialize_GoodJsonAsync()
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
                        { "key1", "value1" },
                        { "key2", 10 }
                    }
                }
            }
        };

        var content = JsonContent.Create(environment);

        var env = await content.ReadFromJsonAsync<ConfigEnvironment>(ConfigServerConfigurationProvider.SerializerOptions);
        Assert.NotNull(env);
        Assert.Equal("test-name", env.Name);
        Assert.NotNull(env.Profiles);
        Assert.Single(env.Profiles);
        Assert.Equal("test-label", env.Label);
        Assert.Equal("test-version", env.Version);
        Assert.Equal("test-state", env.State);
        Assert.NotNull(env.PropertySources);
        Assert.Single(env.PropertySources);
        Assert.Equal("source", env.PropertySources[0].Name);
        Assert.NotNull(env.PropertySources[0].Source);
        Assert.Equal(2, env.PropertySources[0].Source.Count);
        Assert.Equal("value1", env.PropertySources[0].Source["key1"].ToString());
        Assert.Equal(10L, long.Parse(env.PropertySources[0].Source["key2"].ToString()!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void GetLabels_Null()
    {
        var options = new ConfigServerClientOptions();
        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string[] result = provider.GetLabels();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);
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
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);
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
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("foobar", result[0]);
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
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("1", result[0]);
        Assert.Equal("2", result[1]);
        Assert.Equal("3", result[2]);
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
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal("1", result[0]);
        Assert.Equal("2", result[1]);
        Assert.Equal("3", result[2]);
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

        Uri requestUri = provider.BuildConfigServerUri(options.Uri, null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
        Assert.Equal(GetEncodedUserPassword("user", "password"), request.Headers.Authorization.Parameter);
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

        Uri requestUri = provider.BuildConfigServerUri(options.Uri, null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
        Assert.Equal(GetEncodedUserPassword("user", "password"), request.Headers.Authorization.Parameter);
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

        Uri requestUri = provider.BuildConfigServerUri(options.Uri, null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
        Assert.Equal(GetEncodedUserPassword("user", "password"), request.Headers.Authorization.Parameter);
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

        Uri requestUri = provider.BuildConfigServerUri(options.Uri!, null);
        HttpRequestMessage request = await provider.GetRequestMessageAsync(requestUri, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(requestUri, request.RequestUri);
        Assert.True(request.Headers.Contains(ConfigServerConfigurationProvider.TokenHeader));
        IEnumerable<string> headerValues = request.Headers.GetValues(ConfigServerConfigurationProvider.TokenHeader);
        Assert.Contains("MyVaultToken", headerValues);
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
        await provider.RefreshVaultTokenAsync(default);

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
        await provider.RefreshVaultTokenAsync(default);

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
                { "foo", "bar" },
                { "bar", "foo" }
            }
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);
        using HttpClient httpClient = provider.CreateHttpClient(options);

        Assert.NotNull(httpClient);
        Assert.Equal("bar", httpClient.DefaultRequestHeaders.GetValues("foo").SingleOrDefault());
        Assert.Equal("foo", httpClient.DefaultRequestHeaders.GetValues("bar").SingleOrDefault());
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
            Assert.True(provider.IsDiscoveryFirstEnabled());
        }

        var values = new Dictionary<string, string?>
        {
            { "spring:cloud:config:discovery:enabled", "True" }
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
            Assert.True(provider.IsDiscoveryFirstEnabled());
        }
    }

    [Fact]
    public void UpdateSettingsFromDiscovery_UpdatesSettingsCorrectly()
    {
        var values = new Dictionary<string, string?>
        {
            { "spring:cloud:config:discovery:enabled", "True" }
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
        Assert.Null(options.Username);
        Assert.Null(options.Password);
        Assert.Equal("http://localhost:8888/", options.Uri);

        var metadata1 = new Dictionary<string, string?>
        {
            { "password", "firstPassword" }
        };

        var metadata2 = new Dictionary<string, string?>
        {
            { "password", "secondPassword" },
            { "user", "secondUser" },
            { "configPath", "configPath" }
        };

        List<IServiceInstance> instances =
        [
            new TestServiceInstance("i1", new Uri("https://foo.bar:8888/"), metadata1),
            new TestServiceInstance("i2", new Uri("https://foo.bar.baz:9999/"), metadata2)
        ];

        provider.UpdateSettingsFromDiscovery(instances, options);
        Assert.Equal("secondUser", options.Username);
        Assert.Equal("secondPassword", options.Password);
        Assert.Equal("https://foo.bar:8888/,https://foo.bar.baz:9999/configPath", options.Uri);
    }

    [Fact]
    public async Task DiscoverServerInstances_FailsFast()
    {
        var values = new Dictionary<string, string?>
        {
            { "spring:cloud:config:discovery:enabled", "True" },
            { "spring:cloud:config:failFast", "True" },
            { "eureka:client:eurekaServer:retryCount", "0" }
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

        var exception = await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, CancellationToken.None));
        Assert.StartsWith("Could not locate Config Server via discovery", exception.Message, StringComparison.Ordinal);
    }

    private static string GetEncodedUserPassword(string user, string password)
    {
        return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}"));
    }

    private sealed class TestServiceInstance(string serviceId, Uri uri, IReadOnlyDictionary<string, string?> metadata) : IServiceInstance
    {
        public string ServiceId { get; } = serviceId;
        public string Host { get; } = uri.Host;
        public int Port { get; } = uri.Port;
        public bool IsSecure { get; } = uri.Scheme == Uri.UriSchemeHttps;
        public Uri Uri { get; } = uri;
        public IReadOnlyDictionary<string, string?> Metadata { get; } = metadata;
    }
}
