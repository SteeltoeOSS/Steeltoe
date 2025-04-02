// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed partial class ConfigServerConfigurationProviderTest
{
    [Fact]
    public async Task RemoteLoadAsync_InvalidUri()
    {
        var options = new ConfigServerClientOptions();
        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<UriFormatException>(async () =>
            await provider.RemoteLoadAsync([@"foobar\foobar\"], null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoteLoadAsync_HostTimesOut()
    {
        var options = new ConfigServerClientOptions
        {
            Timeout = 100
        };

        var httpClientHandler = new SlowHttpClientHandler(1.Seconds(), new HttpResponseMessage());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        Func<Task> action = async () => await provider.RemoteLoadAsync(["http://localhost:9999/app/profile"], null, TestContext.Current.CancellationToken);

        (await action.Should().ThrowExactlyAsync<TaskCanceledException>()).WithInnerExceptionExactly<TimeoutException>();
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsGreaterThanEqualBadRequest()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [500];

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await provider.RemoteLoadAsync(options.GetUris(), null, TestContext.Current.CancellationToken));

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsLessThanBadRequest()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [204];

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        ConfigEnvironment? result = await provider.RemoteLoadAsync(options.GetUris(), null, TestContext.Current.CancellationToken);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Null(result);
    }

    [Fact]
    public async Task Create_WithPollingTimer()
    {
        const string environment = """
            {
              "name": "test-name",
              "profiles": [
                "Production"
              ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": []
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        TestConfigServerStartup.ReturnStatus = [.. Enumerable.Repeat(200, 100)];
        TestConfigServerStartup.Label = "test-label";
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            PollingInterval = TimeSpan.FromMilliseconds(300),
            Label = "label,test-label"
        };

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);
        Assert.True(TestConfigServerStartup.InitialRequestLatch.Wait(1.Minutes(), TestContext.Current.CancellationToken));
        Assert.True(TestConfigServerStartup.RequestCount >= 1);
        await Task.Delay(1.Seconds(), TestContext.Current.CancellationToken);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.True(TestConfigServerStartup.RequestCount >= 2);
        Assert.False(provider.GetReloadToken().HasChanged);
    }

    [Fact]
    public async Task Create_FailFastEnabledAndExceptionThrownDuringPolling_DoesNotCrash()
    {
        const string environment = """
            {
              "name": "test-name",
              "profiles": [
                "Production"
              ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": []
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;

        // Initial requests succeed, but later requests return 400 status code so that an exception is thrown during polling
        TestConfigServerStartup.ReturnStatus = [.. Enumerable.Repeat(200, 2).Concat(Enumerable.Repeat(400, 100))];
        TestConfigServerStartup.Label = "test-label";
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            PollingInterval = TimeSpan.FromMilliseconds(300),
            FailFast = true,
            Label = "test-label"
        };

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());

        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        Assert.True(TestConfigServerStartup.InitialRequestLatch.Wait(1.Minutes(), TestContext.Current.CancellationToken));
        Assert.True(TestConfigServerStartup.RequestCount >= 1);
        await Task.Delay(1.Seconds(), TestContext.Current.CancellationToken);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.True(TestConfigServerStartup.RequestCount >= 2);
        Assert.False(provider.GetReloadToken().HasChanged);
    }

    [Fact]
    public async Task Create_WithNonZeroPollingIntervalAndClientDisabled_PollingDisabled()
    {
        const string environment = """
            {
              "name": "test-name",
              "profiles": [
                "Production"
              ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": []
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        TestConfigServerStartup.ReturnStatus = [.. Enumerable.Repeat(200, 100)];
        TestConfigServerStartup.Label = "test-label";
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Enabled = false,
            PollingInterval = TimeSpan.FromMilliseconds(300),
            Label = "label,test-label"
        };

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());

        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        Assert.False(TestConfigServerStartup.InitialRequestLatch.Wait(2.Seconds(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DoLoad_MultipleLabels_ChecksAllLabels()
    {
        const string environment = """
            {
              "name": "test-name",
              "profiles": [
                "Production"
              ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "key1": "value1",
                    "key2": 10
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;

        TestConfigServerStartup.ReturnStatus =
        [
            404,
            200
        ];

        TestConfigServerStartup.Label = "test-label";
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.Label = "label,test-label";
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.DoLoadAsync(true, TestContext.Current.CancellationToken);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal(2, TestConfigServerStartup.RequestCount);
        Assert.Equal($"/{options.Name}/{options.Environment}/test-label", TestConfigServerStartup.LastRequest.Path.Value);
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsGood()
    {
        const string environment = """
            {
              "name": "test-name",
              "profiles": [
                "Production"
              ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "key1": "value1",
                    "key2": 10
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        ConfigEnvironment? env = await provider.RemoteLoadAsync(options.GetUris(), null, TestContext.Current.CancellationToken);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.NotNull(env);
        Assert.Equal("test-name", env.Name);
        Assert.NotNull(env.Profiles);
        Assert.Single(env.Profiles);
        Assert.Equal("test-label", env.Label);
        Assert.Equal("test-version", env.Version);
        Assert.NotNull(env.PropertySources);
        Assert.Single(env.PropertySources);
        Assert.Equal("source", env.PropertySources[0].Name);
        Assert.NotNull(env.PropertySources[0].Source);
        Assert.Equal(2, env.PropertySources[0].Source.Count);
        Assert.Equal("value1", env.PropertySources[0].Source["key1"].ToString());
        Assert.Equal(10L, long.Parse(env.PropertySources[0].Source["key2"].ToString()!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsGreaterThanEqualBadRequest_StopsChecking()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            500,
            200
        ];

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://localhost:8888, http://localhost:8888";
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsNotFoundStatus_DoesNotContinueChecking()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            404,
            200
        ];

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://localhost:8888, http://localhost:8888";
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsNotFoundStatus()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [404];

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal(27, provider.Properties.Count);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsNotFoundStatus_FailFastEnabled()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [404];

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsNotFoundStatus__DoesNotContinueChecking_FailFastEnabled()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        options.Uri = "http://localhost:8888,http://localhost:8888";
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            404,
            200
        ];

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken));
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus = [500];

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsBadStatus_StopsChecking_FailFastEnabled()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            500,
            500,
            500
        ];

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        options.Uri = "http://localhost:8888, http://localhost:8888, http://localhost:8888";
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken));
        Assert.Equal(1, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled_RetryEnabled()
    {
        TestConfigServerStartup.Reset();

        TestConfigServerStartup.ReturnStatus =
        [
            500,
            500,
            500,
            500,
            500,
            500
        ];

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            FailFast = true,
            Retry =
            {
                Enabled = true,
                InitialInterval = 10
            },
            Timeout = 10
        };

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken));
        Assert.Equal(6, TestConfigServerStartup.RequestCount);
    }

    [Fact]
    public async Task Load_ChangesDataDictionary()
    {
        const string environment = """
            {
              "name": "test-name",
              "profiles": [
                "Production"
              ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "key1": "value1",
                    "key2": 10
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", TestConfigServerStartup.LastRequest.Path.Value);

        Assert.True(provider.TryGet("key1", out string? value));
        Assert.Equal("value1", value);
        Assert.True(provider.TryGet("key2", out value));
        Assert.Equal("10", value);
    }

    [Fact]
    public async Task ReLoad_DataDictionary_With_New_Configurations()
    {
        const string environment = """
            {
              "name": "test-name",
              "profiles": [
                "Production"
              ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "featureToggles.ShowModule[0]": "FT1",
                    "featureToggles.ShowModule[1]": "FT2",
                    "featureToggles.ShowModule[2]": "FT3",
                    "enableSettings": "true"
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        provider.Load();
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.True(provider.TryGet("featureToggles:ShowModule:0", out string? value));
        Assert.Equal("FT1", value);
        Assert.True(provider.TryGet("featureToggles:ShowModule:1", out value));
        Assert.Equal("FT2", value);
        Assert.True(provider.TryGet("featureToggles:ShowModule:2", out value));
        Assert.Equal("FT3", value);
        Assert.True(provider.TryGet("enableSettings", out value));
        Assert.Equal("true", value);

        TestConfigServerStartup.Reset();

        TestConfigServerStartup.Response = """
        {
          "name": "test-name",
          "profiles": [
            "Production"
          ],
          "label": "test-label",
          "version": "test-version",
          "propertySources": [
            {
              "name": "source",
              "source": {
                "featureToggles.ShowModule[0]": "none"
              }
            }
          ]
        }
        """;

        provider.Load();
        Assert.True(provider.TryGet("featureToggles:ShowModule:0", out value));
        Assert.Equal("none", value);
        Assert.False(provider.TryGet("featureToggles:ShowModule:1", out _));
        Assert.False(provider.TryGet("featureToggles:ShowModule:2", out _));
        Assert.False(provider.TryGet("enableSettings", out _));
    }

    [Fact]
    public void AddConfigServerClientSettings_ChangesDataDictionary()
    {
        var options = new ConfigServerClientOptions
        {
            Enabled = false,
            FailFast = true,
            Environment = "environment",
            Label = "label",
            Name = "name",
            Uri = "https://foo.bar/",
            Username = "username",
            Password = "password",
            Token = "vaultToken",
            Timeout = 75_000,
            PollingInterval = TimeSpan.FromSeconds(35.5),
            ValidateCertificates = false,
            AccessTokenUri = "https://token.server.com/",
            ClientSecret = "client_secret",
            ClientId = "client_id",
            TokenTtl = 2,
            TokenRenewRate = 1,
            DisableTokenRenewal = true,
            Retry =
            {
                Enabled = true,
                InitialInterval = 8,
                MaxInterval = 16,
                Multiplier = 1.1,
                MaxAttempts = 7
            },
            Discovery =
            {
                Enabled = true,
                ServiceId = "my-config-server"
            },
            Health =
            {
                Enabled = false,
                TimeToLive = 9
            },
            Headers =
            {
                ["headerName1"] = "headerValue1",
                ["headerName2"] = "headerValue2"
            }
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);
        provider.AddConfigServerClientOptions();

        AssertDataValue("spring:cloud:config:enabled", "False");
        AssertDataValue("spring:cloud:config:failFast", "True");
        AssertDataValue("spring:cloud:config:env", "environment");
        AssertDataValue("spring:cloud:config:label", "label");
        AssertDataValue("spring:cloud:config:name", "name");
        AssertDataValue("spring:cloud:config:uri", "https://foo.bar/");
        AssertDataValue("spring:cloud:config:username", "username");
        AssertDataValue("spring:cloud:config:password", "password");
        AssertDataValue("spring:cloud:config:token", "vaultToken");
        AssertDataValue("spring:cloud:config:timeout", "75000");
        AssertDataValue("spring:cloud:config:pollingInterval", "00:00:35.5000000");
        AssertDataValue("spring:cloud:config:validateCertificates", "False");
        AssertDataValue("spring:cloud:config:accessTokenUri", "https://token.server.com/");
        AssertDataValue("spring:cloud:config:clientSecret", "client_secret");
        AssertDataValue("spring:cloud:config:clientId", "client_id");
        AssertDataValue("spring:cloud:config:tokenTtl", "2");
        AssertDataValue("spring:cloud:config:tokenRenewRate", "1");
        AssertDataValue("spring:cloud:config:disableTokenRenewal", "True");
        AssertDataValue("spring:cloud:config:retry:enabled", "True");
        AssertDataValue("spring:cloud:config:retry:initialInterval", "8");
        AssertDataValue("spring:cloud:config:retry:maxInterval", "16");
        AssertDataValue("spring:cloud:config:retry:multiplier", "1.1");
        AssertDataValue("spring:cloud:config:retry:maxAttempts", "7");
        AssertDataValue("spring:cloud:config:discovery:enabled", "True");
        AssertDataValue("spring:cloud:config:discovery:serviceId", "my-config-server");
        AssertDataValue("spring:cloud:config:health:enabled", "False");
        AssertDataValue("spring:cloud:config:health:timeToLive", "9");
        AssertDataValue("spring:cloud:config:headers:headerName1", "headerValue1");
        AssertDataValue("spring:cloud:config:headers:headerName2", "headerValue2");

        void AssertDataValue(string key, string expected)
        {
            provider.TryGet(key, out string? value).Should().BeTrue();
            value.Should().Be(expected);
        }
    }

    [Fact]
    public async Task Reload_And_Bind_Without_Throwing_Exception()
    {
        const string environment = """
            {
              "name": "test-name",
              "profiles": [
                "Production"
              ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "name": "my-app",
                    "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
                  }
                }
              ]
            }
            """;

        TestConfigServerStartup.Reset();
        TestConfigServerStartup.Response = environment;

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestConfigServerStartup>();

        ConfigServerClientOptions clientOptions = GetCommonOptions();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri(clientOptions.Uri!);

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(clientOptions, null, httpClientHandler, NullLoggerFactory.Instance);

        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.Add(new TestConfigServerConfigurationSource(provider));

        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        TestOptions? testOptions = null;

        using var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        _ = Task.Run(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            while (!tokenSource.IsCancellationRequested)
            {
                configurationRoot.Reload();
            }
        }, tokenSource.Token);

        while (!tokenSource.IsCancellationRequested)
        {
            testOptions = configurationRoot.Get<TestOptions>();
        }

        Assert.NotNull(testOptions);
        Assert.Equal("my-app", testOptions.Name);
        Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", testOptions.Version);
    }

    private ConfigServerClientOptions GetCommonOptions()
    {
        return new ConfigServerClientOptions
        {
            Name = "myName"
        };
    }

    private sealed class SlowHttpClientHandler(TimeSpan sleepTime, HttpResponseMessage responseMessage) : HttpClientHandler
    {
        private readonly TimeSpan _sleepTime = sleepTime;
        private readonly HttpResponseMessage _responseMessage = responseMessage;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_sleepTime, cancellationToken);
            return _responseMessage;
        }
    }

    internal sealed class TestOptions
    {
#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
        public string? Name { get; set; }
        public string? Version { get; set; }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed
    }
}
