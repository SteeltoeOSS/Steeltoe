// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;

// ReSharper disable AccessToDisposedClosure

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
        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => httpClientHandler, NullLoggerFactory.Instance);
        provider.Load();

        List<Uri> requestUris = [new("http://localhost:9999/app/profile")];

        Func<Task> action = async () => await provider.RemoteLoadAsync(provider.ClientOptions, requestUris, null, TestContext.Current.CancellationToken);

        (await action.Should().ThrowExactlyAsync<TaskCanceledException>()).WithInnerExceptionExactly<TimeoutException>();
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsGreaterThanEqualBadRequest()
    {
        ConfigServerClientOptions options = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond(HttpStatusCode.InternalServerError);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);

        Func<Task> action = async () => await provider.RemoteLoadAsync(provider.ClientOptions, options.GetUris(), null, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<HttpRequestException>();

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsLessThanBadRequest()
    {
        ConfigServerClientOptions options = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond(HttpStatusCode.NoContent);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);

        ConfigEnvironment? result = await provider.RemoteLoadAsync(provider.ClientOptions, options.GetUris(), null, TestContext.Current.CancellationToken);

        handler.Mock.VerifyNoOutstandingExpectation();
        result.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithConfigurationReloadTimer()
    {
        await TestFailureTracer.CaptureAsync(async tracer =>
        {
            const string responseJson = """
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

            var options = new ConfigServerClientOptions
            {
                Name = "myName",
                PollingInterval = 300.Milliseconds(),
                Label = "label,test-label"
            };

            using var handler = new DelegateToMockHttpClientHandler();
            handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production/label").Respond(HttpStatusCode.NotFound);

            using var firstRequestCountdownEvent = new CountdownEvent(1);

            MockedRequest testLabelRequest = handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production/test-label").Respond(_ =>
            {
                if (!firstRequestCountdownEvent.IsSet)
                {
                    firstRequestCountdownEvent.Signal();
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                };
            });

            using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, tracer.LoggerFactory);
            provider.Load();

            bool firstRequestCompleted = firstRequestCountdownEvent.Wait(2.Seconds(), TestContext.Current.CancellationToken);
            firstRequestCompleted.Should().BeTrue();

            handler.Mock.GetMatchCount(testLabelRequest).Should().BeGreaterThanOrEqualTo(1);

            await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

            handler.Mock.GetMatchCount(testLabelRequest).Should().BeGreaterThanOrEqualTo(2);
            provider.GetReloadToken().HasChanged.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Create_FailFastEnabledAndExceptionThrownDuringPolledConfigurationReload_DoesNotCrash()
    {
        await TestFailureTracer.CaptureAsync(async tracer =>
        {
            const string responseJson = """
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

            var options = new ConfigServerClientOptions
            {
                Name = "myName",
                PollingInterval = 300.Milliseconds(),
                FailFast = true,
                Label = "test-label"
            };

            using var handler = new DelegateToMockHttpClientHandler();
            using var firstRequestCountdownEvent = new CountdownEvent(1);
            int requestCount = 0;

            MockedRequest testLabelRequest = handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production/test-label").Respond(_ =>
            {
                int currentCount = Interlocked.Increment(ref requestCount);

                if (!firstRequestCountdownEvent.IsSet)
                {
                    firstRequestCountdownEvent.Signal();
                }

                if (currentCount <= 2)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            });

            using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, tracer.LoggerFactory);
            provider.Load();

            bool firstRequestCompleted = firstRequestCountdownEvent.Wait(2.Seconds(), TestContext.Current.CancellationToken);
            firstRequestCompleted.Should().BeTrue();

            handler.Mock.GetMatchCount(testLabelRequest).Should().BeGreaterThanOrEqualTo(1);

            await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

            handler.Mock.GetMatchCount(testLabelRequest).Should().BeGreaterThanOrEqualTo(2);
            provider.GetReloadToken().HasChanged.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Create_WithNonZeroPollingIntervalAndClientDisabled_PollingConfigurationReloadDisabled()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Enabled = false,
            PollingInterval = 300.Milliseconds(),
            Label = "label,test-label"
        };

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest request = handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production/label").Respond(HttpStatusCode.OK);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);
        provider.Load();

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);
        handler.Mock.GetMatchCount(request).Should().Be(0);
    }

    [Theory]
    [InlineData(false, "00:00:01")]
    [InlineData(true, "00:00:00")]
    public void OnSettingsChanged_stops_reload_timer_when_polling_no_longer_enabled(bool enabled, string pollingInterval)
    {
        const string responseJson = """
            {
              "name": "myName",
              "profiles": [
                "Production"
              ],
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
        handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production").Respond("application/json", responseJson);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        configurationBuilder.AddConfigServer(new ConfigServerClientOptions(), null, () => handler, NullLoggerFactory.Instance);
        IConfigurationRoot configuration = configurationBuilder.Build();

        ConfigServerConfigurationProvider provider = configuration.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        FieldInfo reloadTimerField =
            typeof(ConfigServerConfigurationProvider).GetField("_configurationReloadTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;

        reloadTimerField.GetValue(provider).Should().NotBeNull();

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

        reloadTimerField.GetValue(provider).Should().BeNull();
    }

    [Fact]
    public void OnSettingsChanged_reschedules_reload_timer_when_polling_interval_changes()
    {
        const string responseJson = """
            {
              "name": "myName",
              "profiles": [
                "Production"
              ],
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
                    "pollingInterval": "00:00:05"
                  }
                }
              }
            }
            """);

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production").Respond("application/json", responseJson);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        configurationBuilder.AddConfigServer(new ConfigServerClientOptions(), null, () => handler, NullLoggerFactory.Instance);
        IConfigurationRoot configuration = configurationBuilder.Build();

        ConfigServerConfigurationProvider provider = configuration.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        FieldInfo reloadTimerField =
            typeof(ConfigServerConfigurationProvider).GetField("_configurationReloadTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;

        reloadTimerField.GetValue(provider).Should().NotBeNull();

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "name": "myName",
                    "enabled": true,
                    "pollingInterval": "00:00:10"
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        reloadTimerField.GetValue(provider).Should().NotBeNull("timer should be rescheduled, not stopped");
    }

    [Fact]
    public void OnSettingsChanged_stops_vault_renew_timer_when_renewal_becomes_disabled()
    {
        const string responseJson = """
            {
              "name": "myName",
              "profiles": [
                "Production"
              ],
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
                    "token": "MyVaultToken"
                  }
                }
              }
            }
            """);

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production").Respond("application/json", responseJson);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        configurationBuilder.AddConfigServer(new ConfigServerClientOptions(), null, () => handler, NullLoggerFactory.Instance);
        IConfigurationRoot configuration = configurationBuilder.Build();

        ConfigServerConfigurationProvider provider = configuration.Providers.OfType<ConfigServerConfigurationProvider>().Single();
        FieldInfo vaultTimerField = typeof(ConfigServerConfigurationProvider).GetField("_vaultRenewTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;

        vaultTimerField.GetValue(provider).Should().NotBeNull();

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "name": "myName",
                    "token": "MyVaultToken",
                    "disableTokenRenewal": true
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        vaultTimerField.GetValue(provider).Should().BeNull();
    }

    [Fact]
    public void Load_MultipleConfigServers_SocketError_FallsBackToNextServer()
    {
        const string responseJson = """
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
                    "key1": "value1"
                  }
                }
              ]
            }
            """;

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.When(HttpMethod.Get, "http://server1:8888/myName/Production")
            .Throw(new HttpRequestException("Connection refused", new SocketException((int)SocketError.ConnectionRefused)));

        handler.Mock.When(HttpMethod.Get, "http://server2:8888/myName/Production").Respond("application/json", responseJson);

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Uri = "http://server1:8888, http://server2:8888"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);
        provider.Load();

        provider.TryGet("key1", out string? value).Should().BeTrue();
        value.Should().Be("value1");
    }

    [Fact]
    public void Load_IdenticalData_DoesNotTriggerReload()
    {
        const string responseJson = """
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
                    "key1": "value1"
                  }
                }
              ]
            }
            """;

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production").Respond("application/json", responseJson);

        var options = new ConfigServerClientOptions
        {
            Name = "myName"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);
        provider.Load();

        provider.TryGet("key1", out string? value).Should().BeTrue();
        value.Should().Be("value1");

        bool reloadFired = false;
        provider.GetReloadToken().RegisterChangeCallback(_ => reloadFired = true, null);

        provider.Load();

        reloadFired.Should().BeFalse("identical data should not trigger OnReload");
        provider.TryGet("key1", out value).Should().BeTrue();
        value.Should().Be("value1");
    }

    [Fact]
    public void Load_MultipleLabels_ChecksAllLabels()
    {
        const string responseJson = """
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

        ConfigServerClientOptions options = GetCommonOptions();
        options.Label = "label,test-label";

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}/label").Respond(HttpStatusCode.NotFound);
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}/test-label").Respond("application/json", responseJson);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);
        provider.Load();

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsGood()
    {
        const string responseJson = """
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

        ConfigServerClientOptions options = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond("application/json", responseJson);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);

        ConfigEnvironment? env = await provider.RemoteLoadAsync(provider.ClientOptions, options.GetUris(), null, TestContext.Current.CancellationToken);

        handler.Mock.VerifyNoOutstandingExpectation();

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
        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://localhost:8888, http://localhost:8888";

        using var handler = new DelegateToMockHttpClientHandler();

        MockedRequest request = handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}")
            .Respond(HttpStatusCode.InternalServerError);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);
        provider.Load();

        handler.Mock.GetMatchCount(request).Should().Be(1);

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsNotFoundStatus_DoesNotContinueChecking()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://localhost:8888, http://localhost:8888";

        using var handler = new DelegateToMockHttpClientHandler();

        MockedRequest request = handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}")
            .Respond(HttpStatusCode.NotFound);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);
        provider.Load();

        handler.Mock.GetMatchCount(request).Should().Be(1);

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public void Load_ConfigServerReturnsNotFoundStatus()
    {
        ConfigServerClientOptions options = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond(HttpStatusCode.NotFound);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);
        provider.Load();

        handler.Mock.VerifyNoOutstandingExpectation();
        provider.InnerData.Should().BeEmpty();
    }

    [Fact]
    public void Load_ConfigServerReturnsNotFoundStatus_FailFastEnabled()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond(HttpStatusCode.NotFound);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);

        Action action = provider.Load;

        action.Should().ThrowExactly<ConfigServerException>();
        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsNotFoundStatus__DoesNotContinueChecking_FailFastEnabled()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        options.Uri = "http://localhost:8888,http://localhost:8888";

        using var handler = new DelegateToMockHttpClientHandler();

        MockedRequest request = handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}")
            .Respond(HttpStatusCode.NotFound);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);

        Action action = provider.Load;

        action.Should().ThrowExactly<ConfigServerException>();
        handler.Mock.GetMatchCount(request).Should().Be(1);

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public void Load_UriInvalid_FailFastEnabled()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://username:p@ssword@localhost:8888";
        options.FailFast = true;

        using var handler = new DelegateToMockHttpClientHandler();
        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);

        Action action = provider.Load;

        action.Should().ThrowExactly<ConfigServerException>().WithMessage("One or more Config Server URIs in configuration are invalid.");
        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void Load_ConfigServerReturnsBadStatus_FailFastEnabled()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond(HttpStatusCode.InternalServerError);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);

        Action action = provider.Load;

        action.Should().ThrowExactly<ConfigServerException>();
        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Load_MultipleConfigServers_ReturnsBadStatus_StopsChecking_FailFastEnabled()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        options.Uri = "http://localhost:8888, http://localhost:8888, http://localhost:8888";

        using var handler = new DelegateToMockHttpClientHandler();

        MockedRequest request = handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}")
            .Respond(HttpStatusCode.InternalServerError);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);

        Action action = provider.Load;

        action.Should().ThrowExactly<ConfigServerException>();
        handler.Mock.GetMatchCount(request).Should().Be(1);

        await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled_RetryEnabled()
    {
        await TestFailureTracer.CaptureAsync(async tracer =>
        {
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

            using var handler = new DelegateToMockHttpClientHandler();
            MockedRequest request = handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production").Respond(HttpStatusCode.InternalServerError);

            using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, tracer.LoggerFactory);

            Action action = () => provider.Load();

            action.Should().ThrowExactly<ConfigServerException>();

            await Task.Delay(2.Seconds(), TestContext.Current.CancellationToken);

            handler.Mock.GetMatchCount(request).Should().BeGreaterThan(3);
        });
    }

    [Fact]
    public void Load_ChangesDataDictionary()
    {
        const string responseJson = """
            {
              "name": "test-name",
              "profiles": [
                "Production"
              ],
              "label": "test-label",
              "version": "test-version",
              "state": "test-state",
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

        ConfigServerClientOptions options = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond("application/json", responseJson);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);
        provider.Load();
        handler.Mock.VerifyNoOutstandingExpectation();

        provider.TryGet("key1", out string? value).Should().BeTrue();
        value.Should().Be("value1");
        provider.TryGet("key2", out value).Should().BeTrue();
        value.Should().Be("10");

        provider.TryGet("spring:cloud:config:client:version", out value).Should().BeTrue();
        value.Should().Be("test-version");
        provider.TryGet("spring:cloud:config:client:state", out value).Should().BeTrue();
        value.Should().Be("test-state");
    }

    [Fact]
    public void ReLoad_DataDictionary_With_New_Configurations()
    {
        const string responseJson = """
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

        ConfigServerClientOptions options = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond("application/json", responseJson);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, NullLoggerFactory.Instance);
        provider.Load();
        handler.Mock.VerifyNoOutstandingExpectation();

        provider.TryGet("featureToggles:ShowModule:0", out string? value).Should().BeTrue();
        value.Should().Be("FT1");
        provider.TryGet("featureToggles:ShowModule:1", out value).Should().BeTrue();
        value.Should().Be("FT2");
        provider.TryGet("featureToggles:ShowModule:2", out value).Should().BeTrue();
        value.Should().Be("FT3");
        provider.TryGet("enableSettings", out value).Should().BeTrue();
        value.Should().Be("true");

        handler.Mock.Clear();

        const string newResponseJson = """
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

        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond("application/json", newResponseJson);
        provider.Load();
        handler.Mock.VerifyNoOutstandingExpectation();

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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, null, NullLoggerFactory.Instance);
        provider.Load();

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
    public void Reload_And_Bind_Without_Throwing_Exception()
    {
        const string responseJson = """
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

        ConfigServerClientOptions clientOptions = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{clientOptions.Name}/{clientOptions.Environment}").Respond("application/json", responseJson);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddConfigServer(clientOptions, null, () => handler, NullLoggerFactory.Instance);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        using ConfigServerConfigurationProvider provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Single();

        TestOptions? testOptions = null;
        using var tokenSource = new CancellationTokenSource(250.Milliseconds());

        _ = Task.Run(() =>
        {
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
            Name = "myName",
            Environment = "Staging"
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
