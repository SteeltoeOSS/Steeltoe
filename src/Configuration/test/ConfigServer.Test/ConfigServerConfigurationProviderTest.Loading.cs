// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
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
            Timeout = 10
        };

        var httpClientHandler = new SlowHttpClientHandler(1.Seconds(), new HttpResponseMessage());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await provider.RemoteLoadAsync(["http://localhost:9999/app/profile"], null, TestContext.Current.CancellationToken);

        (await action.Should().ThrowExactlyAsync<TaskCanceledException>()).WithInnerExceptionExactly<TimeoutException>();
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsGreaterThanEqualBadRequest()
    {
        using var startup = new TestConfigServerStartup();
        startup.ReturnStatus = [500];

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await provider.RemoteLoadAsync(options.GetUris(), null, TestContext.Current.CancellationToken));

        startup.LastRequest.Should().NotBeNull();
        startup.LastRequest.Path.Value.Should().Be($"/{options.Name}/{options.Environment}");
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsLessThanBadRequest()
    {
        using var startup = new TestConfigServerStartup();
        startup.ReturnStatus = [204];

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        ConfigEnvironment? result = await provider.RemoteLoadAsync(options.GetUris(), null, TestContext.Current.CancellationToken);

        startup.LastRequest.Should().NotBeNull();
        startup.LastRequest.Path.Value.Should().Be($"/{options.Name}/{options.Environment}");
        result.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithPollingTimer()
    {
        await TestFailureTracer.CaptureAsync(async tracer =>
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

            using var startup = new TestConfigServerStartup();
            startup.Response = environment;
            startup.ReturnStatus = [.. Enumerable.Repeat(200, 100)];
            startup.Label = "test-label";

            await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
            startup.Configure(app);
            await app.StartAsync(TestContext.Current.CancellationToken);

            using TestServer server = app.GetTestServer();
            server.BaseAddress = new Uri("http://localhost:8888");

            var options = new ConfigServerClientOptions
            {
                Name = "myName",
                PollingInterval = 300.Milliseconds(),
                Label = "label,test-label"
            };

            using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
            using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, tracer.LoggerFactory);

            bool firstRequestCompleted = startup.WaitForFirstRequest(2.Seconds());
            firstRequestCompleted.Should().BeTrue();

            startup.RequestCount.Should().BeGreaterThanOrEqualTo(1);
            startup.LastRequest.Should().NotBeNull();

            await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

            startup.RequestCount.Should().BeGreaterThanOrEqualTo(2);
            provider.GetReloadToken().HasChanged.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Create_FailFastEnabledAndExceptionThrownDuringPolling_DoesNotCrash()
    {
        await TestFailureTracer.CaptureAsync(async tracer =>
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

            using var startup = new TestConfigServerStartup();
            startup.Response = environment;

            // Initial requests succeed, but later requests return 400 status code so that an exception is thrown during polling
            startup.ReturnStatus = [.. Enumerable.Repeat(200, 2).Concat(Enumerable.Repeat(400, 100))];
            startup.Label = "test-label";

            await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
            startup.Configure(app);
            await app.StartAsync(TestContext.Current.CancellationToken);

            using TestServer server = app.GetTestServer();
            server.BaseAddress = new Uri("http://localhost:8888");

            var options = new ConfigServerClientOptions
            {
                Name = "myName",
                PollingInterval = 300.Milliseconds(),
                FailFast = true,
                Label = "test-label"
            };

            using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
            using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, tracer.LoggerFactory);

            bool firstRequestCompleted = startup.WaitForFirstRequest(2.Seconds());
            firstRequestCompleted.Should().BeTrue();

            startup.RequestCount.Should().BeGreaterThanOrEqualTo(1);
            startup.LastRequest.Should().NotBeNull();

            await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

            startup.RequestCount.Should().BeGreaterThanOrEqualTo(2);
            provider.GetReloadToken().HasChanged.Should().BeFalse();
        });
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

        using var startup = new TestConfigServerStartup();
        startup.Response = environment;
        startup.ReturnStatus = [.. Enumerable.Repeat(200, 100)];
        startup.Label = "test-label";

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Enabled = false,
            PollingInterval = 300.Milliseconds(),
            Label = "label,test-label"
        };

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());

        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        startup.WaitForFirstRequest(2.Seconds()).Should().BeFalse();
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

        using var startup = new TestConfigServerStartup();
        startup.Response = environment;

        startup.ReturnStatus =
        [
            404,
            200
        ];

        startup.Label = "test-label";

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.Label = "label,test-label";

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.DoLoadAsync(true, TestContext.Current.CancellationToken);

        startup.LastRequest.Should().NotBeNull();
        startup.RequestCount.Should().Be(2);
        startup.LastRequest.Path.Value.Should().Be($"/{options.Name}/{options.Environment}/test-label");
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

        using var startup = new TestConfigServerStartup();
        startup.Response = environment;

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        ConfigEnvironment? env = await provider.RemoteLoadAsync(options.GetUris(), null, TestContext.Current.CancellationToken);

        Assert.NotNull(startup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", startup.LastRequest.Path.Value);
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
        using var startup = new TestConfigServerStartup();

        startup.ReturnStatus =
        [
            500,
            200
        ];

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://localhost:8888, http://localhost:8888";
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

        startup.LastRequest.Should().NotBeNull();
        startup.LastRequest.Path.Value.Should().Be($"/{options.Name}/{options.Environment}");
        startup.RequestCount.Should().Be(1);

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

        startup.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsNotFoundStatus_DoesNotContinueChecking()
    {
        using var startup = new TestConfigServerStartup();

        startup.ReturnStatus =
        [
            404,
            200
        ];

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://localhost:8888, http://localhost:8888";

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

        startup.LastRequest.Should().NotBeNull();
        startup.LastRequest.Path.Value.Should().Be($"/{options.Name}/{options.Environment}");
        startup.RequestCount.Should().Be(1);

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

        startup.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsNotFoundStatus()
    {
        using var startup = new TestConfigServerStartup();
        startup.ReturnStatus = [404];

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

        startup.LastRequest.Should().NotBeNull();
        startup.LastRequest.Path.Value.Should().Be($"/{options.Name}/{options.Environment}");
        provider.Properties.Count.Should().Be(27);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsNotFoundStatus_FailFastEnabled()
    {
        using var startup = new TestConfigServerStartup();
        startup.ReturnStatus = [404];

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
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
        using var startup = new TestConfigServerStartup();

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        options.Uri = "http://localhost:8888,http://localhost:8888";

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        startup.Reset();

        startup.ReturnStatus =
        [
            404,
            200
        ];

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken));
        startup.RequestCount.Should().Be(1);

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

        startup.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled()
    {
        using var startup = new TestConfigServerStartup();
        startup.ReturnStatus = [500];

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
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
        using var startup = new TestConfigServerStartup();

        startup.ReturnStatus =
        [
            500,
            500,
            500
        ];

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        options.Uri = "http://localhost:8888, http://localhost:8888, http://localhost:8888";

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken));
        startup.RequestCount.Should().Be(1);

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

        startup.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled_RetryEnabled()
    {
        await TestFailureTracer.CaptureAsync(async tracer =>
        {
            using var startup = new TestConfigServerStartup();
            startup.ReturnStatus = [.. Enumerable.Repeat(500, 100)];

            await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
            startup.Configure(app);
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
                Timeout = 1000
            };

            using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
            using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, tracer.LoggerFactory);

            await Assert.ThrowsAsync<ConfigServerException>(async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken));

            await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

            startup.RequestCount.Should().BeGreaterThan(3);
        });
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

        using var startup = new TestConfigServerStartup();
        startup.Response = environment;

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);
        Assert.NotNull(startup.LastRequest);
        Assert.Equal($"/{options.Name}/{options.Environment}", startup.LastRequest.Path.Value);

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

        using var startup = new TestConfigServerStartup();
        startup.Response = environment;

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        provider.Load();

        Assert.NotNull(startup.LastRequest);
        Assert.True(provider.TryGet("featureToggles:ShowModule:0", out string? value));
        Assert.Equal("FT1", value);
        Assert.True(provider.TryGet("featureToggles:ShowModule:1", out value));
        Assert.Equal("FT2", value);
        Assert.True(provider.TryGet("featureToggles:ShowModule:2", out value));
        Assert.Equal("FT3", value);
        Assert.True(provider.TryGet("enableSettings", out value));
        Assert.Equal("true", value);

        startup.Reset();

        startup.Response = """
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
            PollingInterval = 35.5.Seconds(),
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

        using var startup = new TestConfigServerStartup();
        startup.Response = environment;

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        ConfigServerClientOptions clientOptions = GetCommonOptions();
        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri(clientOptions.Uri!);

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(clientOptions, null, httpClientHandler, NullLoggerFactory.Instance);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.Add(new TestConfigServerConfigurationSource(provider));
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        TestOptions? testOptions = null;
        using var tokenSource = new CancellationTokenSource(250.Milliseconds());

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

        testOptions.Should().NotBeNull();
        testOptions.Name.Should().Be("my-app");
        testOptions.Version.Should().Be("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca");
    }

    private static ConfigServerClientOptions GetCommonOptions()
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
        // ReSharper disable PropertyCanBeMadeInitOnly.Global
        public string? Name { get; set; }
        public string? Version { get; set; }
        // ReSharper restore PropertyCanBeMadeInitOnly.Global
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed
    }
}
