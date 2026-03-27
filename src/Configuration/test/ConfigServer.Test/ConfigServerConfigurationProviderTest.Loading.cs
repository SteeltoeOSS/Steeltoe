// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed partial class ConfigServerConfigurationProviderTest
{
    [Fact]
    public async Task RemoteLoadAsync_HostTimesOut()
    {
        var options = new ConfigServerClientOptions
        {
            Timeout = 10
        };

        var httpClientHandler = new SlowHttpClientHandler(1.Seconds(), new HttpResponseMessage());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);
        List<Uri> requestUris = [new("http://localhost:9999/app/profile")];

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await provider.RemoteLoadAsync(provider.ClientOptions, requestUris, null, TestContext.Current.CancellationToken);

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

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await provider.RemoteLoadAsync(provider.ClientOptions, options.GetUris(), null, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<HttpRequestException>();

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

        ConfigEnvironment? result = await provider.RemoteLoadAsync(provider.ClientOptions, options.GetUris(), null, TestContext.Current.CancellationToken);

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

    [Theory]
    [InlineData(false, "00:00:01")]
    [InlineData(true, "00:00:00")]
    public void OnSettingsChanged_stops_timer_when_polling_becomes_ineffective(bool enabled, string pollingInterval)
    {
        const string configServerResponseJson = """
            {
              "name": "myName",
              "profiles": [ "Production" ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": []
            }
            """;

        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "name": "myName",
                    "enabled": true,
                    "pollingInterval": "00:00:01"
                  }
                }
              }
            }
            """);

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production").Respond("application/json", configServerResponseJson);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        configurationBuilder.AddConfigServer(new ConfigServerClientOptions(), handler, NullLoggerFactory.Instance);
        IConfigurationRoot configuration = configurationBuilder.Build();

        ConfigServerConfigurationProvider provider = configuration.Providers.OfType<ConfigServerConfigurationProvider>().Single();
        FieldInfo refreshTimerField = typeof(ConfigServerConfigurationProvider).GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;

        refreshTimerField.GetValue(provider).Should().NotBeNull();

        fileProvider.ReplaceAppSettingsJsonFile($$"""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "name": "myName",
                    "enabled": {{(enabled ? "true" : "false")}},
                    "pollingInterval": "{{pollingInterval}}"
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        refreshTimerField.GetValue(provider).Should().BeNull();
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

        await provider.DoLoadAsync(provider.ClientOptions, true, TestContext.Current.CancellationToken);

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

        ConfigEnvironment? env = await provider.RemoteLoadAsync(provider.ClientOptions, options.GetUris(), null, TestContext.Current.CancellationToken);

        startup.LastRequest.Should().NotBeNull();
        startup.LastRequest.Path.Value.Should().Be($"/{options.Name}/{options.Environment}");

        env.Should().NotBeNull();
        env.Name.Should().Be("test-name");
        env.Profiles.Should().ContainSingle();
        env.Label.Should().Be("test-label");
        env.Version.Should().Be("test-version");

        PropertySource source = env.PropertySources.Should().ContainSingle().Subject;
        source.Name.Should().Be("source");
        source.Source.Should().HaveCount(2);
        source.Source.Should().ContainKey("key1").WhoseValue.ToString().Should().Be("value1");
        source.Source.Should().ContainKey("key2").WhoseValue.ToString().Should().Be("10");
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
        provider.InnerData.Should().BeEmpty();
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

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<ConfigServerException>();
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

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<ConfigServerException>();
        startup.RequestCount.Should().Be(1);

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

        startup.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Load_UriInvalid_FailFastEnabled()
    {
        using var startup = new TestConfigServerStartup();
        startup.ReturnStatus = [500];

        await using WebApplication app = TestWebApplicationBuilderFactory.Create().Build();
        startup.Configure(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using TestServer server = app.GetTestServer();
        server.BaseAddress = new Uri("http://localhost:8888");

        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://username:p@ssword@localhost:8888";
        options.FailFast = true;

        using var httpClientHandler = new ForwardingHttpClientHandler(server.CreateHandler());
        using var provider = new ConfigServerConfigurationProvider(options, null, httpClientHandler, NullLoggerFactory.Instance);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<ConfigServerException>().WithMessage("One or more Config Server URIs in configuration are invalid.");
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

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<ConfigServerException>();
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

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<ConfigServerException>();
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

            // ReSharper disable once AccessToDisposedClosure
            Func<Task> action = async () => await provider.LoadInternalAsync(true, TestContext.Current.CancellationToken);

            await action.Should().ThrowExactlyAsync<ConfigServerException>();

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

        startup.LastRequest.Should().NotBeNull();
        startup.LastRequest.Path.Value.Should().Be($"/{options.Name}/{options.Environment}");

        provider.TryGet("key1", out string? value).Should().BeTrue();
        value.Should().Be("value1");
        provider.TryGet("key2", out value).Should().BeTrue();
        value.Should().Be("10");
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

        startup.LastRequest.Should().NotBeNull();
        provider.TryGet("featureToggles:ShowModule:0", out string? value).Should().BeTrue();
        value.Should().Be("FT1");
        provider.TryGet("featureToggles:ShowModule:1", out value).Should().BeTrue();
        value.Should().Be("FT2");
        provider.TryGet("featureToggles:ShowModule:2", out value).Should().BeTrue();
        value.Should().Be("FT3");
        provider.TryGet("enableSettings", out value).Should().BeTrue();
        value.Should().Be("true");

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

        provider.TryGet("featureToggles:ShowModule:0", out value).Should().BeTrue();
        value.Should().Be("none");
        provider.TryGet("featureToggles:ShowModule:1", out _).Should().BeFalse();
        provider.TryGet("featureToggles:ShowModule:2", out _).Should().BeFalse();
        provider.TryGet("enableSettings", out _).Should().BeFalse();
    }

    [Fact]
    public void DataDictionary_DoesNotContainRedundantClientSettings()
    {
        var options = new ConfigServerClientOptions
        {
            Enabled = false,
            FailFast = true,
            Environment = "environment",
            Label = "main",
            Name = "name",
            Uri = "https://foo.bar/",
            Username = "user",
            Password = "pass",
            Token = "vault-token",
            Timeout = 75_000,
            PollingInterval = TimeSpan.FromSeconds(30),
            ValidateCertificates = false,
            Retry =
            {
                Enabled = true,
                InitialInterval = 500,
                MaxInterval = 5000,
                Multiplier = 2.0,
                MaxAttempts = 10
            },
            Discovery =
            {
                Enabled = true,
                ServiceId = "my-config-server"
            },
            Health =
            {
                Enabled = false,
                TimeToLive = 999
            },
            AccessTokenUri = "https://uaa.example.com/oauth/token",
            ClientSecret = "secret",
            ClientId = "client-id",
            TokenTtl = 600_000,
            TokenRenewRate = 120_000,
            DisableTokenRenewal = true,
            Headers =
            {
                ["X-Custom"] = "value"
            }
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        provider.TryGet("spring:cloud:config:enabled", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:failFast", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:env", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:label", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:name", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:uri", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:username", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:password", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:token", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:timeout", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:pollingInterval", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:validateCertificates", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:validate_Certificates", out _).Should().BeFalse();

        provider.TryGet("spring:cloud:config:retry:enabled", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:retry:initialInterval", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:retry:maxInterval", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:retry:multiplier", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:retry:maxAttempts", out _).Should().BeFalse();

        provider.TryGet("spring:cloud:config:discovery:enabled", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:discovery:serviceId", out _).Should().BeFalse();

        provider.TryGet("spring:cloud:config:health:enabled", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:health:timeToLive", out _).Should().BeFalse();

        provider.TryGet("spring:cloud:config:accessTokenUri", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:clientSecret", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:clientId", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:tokenTtl", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:tokenRenewRate", out _).Should().BeFalse();
        provider.TryGet("spring:cloud:config:disableTokenRenewal", out _).Should().BeFalse();

        provider.TryGet("spring:cloud:config:headers:X-Custom", out _).Should().BeFalse();
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
