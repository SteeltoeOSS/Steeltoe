// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;
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

        // The handler responds so much later than the timeout that, even on a heavily loaded machine, only the timeout can end the request.
        using var handler = new SlowHttpClientHandler(30.Seconds(), new HttpResponseMessage());
        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);

        provider.Load();

        List<Uri> requestUris = [new("http://localhost:9999/app/profile")];

        Func<Task> action = async () => await provider.RemoteLoadAsync(provider.ClientOptions, requestUris, null, TestContext.Current.CancellationToken);

        (await action.Should().ThrowExactlyAsync<TaskCanceledException>()).WithInnerExceptionExactly<TimeoutException>();
    }

    [Fact]
    public async Task RemoteLoadAsync_ConfigServerReturnsInternalServerError()
    {
        ConfigServerClientOptions options = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond(HttpStatusCode.InternalServerError);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);

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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);

        ConfigEnvironment? result = await provider.RemoteLoadAsync(provider.ClientOptions, options.GetUris(), null, TestContext.Current.CancellationToken);

        handler.Mock.VerifyNoOutstandingExpectation();
        result.Should().BeNull();
    }

    [Fact]
    public async Task Polling_reloads_from_Config_Server_without_changing_configuration_when_data_is_unchanged()
    {
        await TestFailureTracer.CaptureAsync(tracer =>
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

            MockedRequest testLabelRequest = handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production/test-label")
                .Respond("application/json", responseJson);

            var timeProvider = new FakeTimeProvider();

            using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, timeProvider, tracer.LoggerFactory);
            provider.Load();

            handler.Mock.GetMatchCount(testLabelRequest).Should().Be(1);

            // Each polling interval causes one request.
            timeProvider.Advance(options.PollingInterval);
            handler.Mock.GetMatchCount(testLabelRequest).Should().Be(2);

            timeProvider.Advance(options.PollingInterval);
            handler.Mock.GetMatchCount(testLabelRequest).Should().Be(3);

            // The data is unchanged, so the configuration did not change.
            provider.GetReloadToken().HasChanged.Should().BeFalse();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Create_FailFastEnabledAndExceptionThrownDuringPolledConfigurationReload_DoesNotCrash()
    {
        await TestFailureTracer.CaptureAsync(tracer =>
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
            int requestCount = 0;

            MockedRequest testLabelRequest = handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production/test-label").Respond(_ =>
            {
                // Loading and the first poll succeed. Every request after that fails.
                if (Interlocked.Increment(ref requestCount) <= 2)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            });

            var timeProvider = new FakeTimeProvider();

            using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, timeProvider, tracer.LoggerFactory);
            provider.Load();

            handler.Mock.GetMatchCount(testLabelRequest).Should().Be(1);

            timeProvider.Advance(options.PollingInterval);
            handler.Mock.GetMatchCount(testLabelRequest).Should().Be(2);

            // With fail-fast enabled, a failing poll must still not throw from the timer callback.
            Action failingPoll = () => timeProvider.Advance(options.PollingInterval);
            failingPoll.Should().NotThrow();
            handler.Mock.GetMatchCount(testLabelRequest).Should().Be(3);

            // Polling continues after the failure.
            timeProvider.Advance(options.PollingInterval);
            handler.Mock.GetMatchCount(testLabelRequest).Should().Be(4);

            provider.GetReloadToken().HasChanged.Should().BeFalse();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public void Create_WithNonZeroPollingIntervalAndClientDisabled_PollingConfigurationReloadDisabled()
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

        var timeProvider = new FakeTimeProvider();

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, timeProvider, NullLoggerFactory.Instance);
        provider.Load();

        provider.InnerConfigurationReloadTimer.Should().BeNull();

        // Many polling intervals pass, but Config Server is never contacted.
        timeProvider.Advance(1.Hours());
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
        using ConfigurationRoot configurationRoot = configurationBuilder.BuildAsRoot();

        ConfigServerConfigurationProvider provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        provider.InnerConfigurationReloadTimer.Should().NotBeNull();

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

        provider.InnerConfigurationReloadTimer.Should().BeNull();
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

        // The initial interval is too long for a poll to happen during this test.
        fileProvider.IncludeAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "name": "myName",
                    "enabled": true,
                    "pollingInterval": "00:00:30"
                  }
                }
              }
            }
            """);

        using var handler = new DelegateToMockHttpClientHandler();
        using var request2CountdownEvent = new CountdownEvent(1);
        int requestCount = 0;

        handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production").Respond(_ =>
        {
            // The first request happens when loading. A second request can only come from the rescheduled timer.
            if (Interlocked.Increment(ref requestCount) >= 2 && !request2CountdownEvent.IsSet)
            {
                request2CountdownEvent.Signal();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        configurationBuilder.AddConfigServer(new ConfigServerClientOptions(), null, () => handler, NullLoggerFactory.Instance);
        using ConfigurationRoot configurationRoot = configurationBuilder.BuildAsRoot();

        ConfigServerConfigurationProvider provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        provider.InnerConfigurationReloadTimer.Should().NotBeNull();
        request2CountdownEvent.IsSet.Should().BeFalse();

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "name": "myName",
                    "enabled": true,
                    "pollingInterval": "00:00:00.200"
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        provider.InnerConfigurationReloadTimer.Should().NotBeNull();

        bool pollHappened = request2CountdownEvent.Wait(10.Seconds(), TestContext.Current.CancellationToken);
        pollHappened.Should().BeTrue();
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
        using ConfigurationRoot configurationRoot = configurationBuilder.BuildAsRoot();

        ConfigServerConfigurationProvider provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();
        provider.InnerVaultRenewTimer.Should().NotBeNull();

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

        provider.InnerVaultRenewTimer.Should().BeNull();
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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);
        provider.Load();

        provider.TryGet("key1", out string? value).Should().BeTrue();
        value.Should().Be("value1");
    }

    [Fact]
    public void Load_MultipleConfigServers_SocketErrorFromAccessTokenUri_LogsWarnings()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level == LogLevel.Warning);
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        using var handler = new DelegateToMockHttpClientHandler();

        MockedRequest accessTokenRequest = handler.Mock.When(HttpMethod.Post, "http://auth-server.com")
            .Throw(new HttpRequestException("Connection refused", new SocketException((int)SocketError.ConnectionRefused)));

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            AccessTokenUri = "http://auth-server.com",
            Uri = "http://config-server1:8888,http://config-server2:8888"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, loggerFactory);
        provider.Load();

        IList<string> logMessages = loggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo([
            $"WARN {typeof(ConfigServerConfigurationProvider)}: Failed to fetch access token from 'http://auth-server.com/'.",
            $"WARN {typeof(ConfigServerConfigurationProvider)}: Failed to fetch access token from 'http://auth-server.com/'.",
            $"WARN {typeof(ConfigServerConfigurationProvider)}: Failed fetching remote configuration from server(s)."
        ], assertionOptions => assertionOptions.WithStrictOrdering());

        // The access token is requested with POST, once for each Config Server.
        handler.Mock.GetMatchCount(accessTokenRequest).Should().Be(2);

        provider.InnerData.Should().BeEmpty();
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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);
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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);
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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);

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
    public void Load_MultipleConfigServers_ReturnsInternalServerError_StopsChecking()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://localhost:8888, http://localhost:8888";

        using var handler = new DelegateToMockHttpClientHandler();

        MockedRequest request = handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}")
            .Respond(HttpStatusCode.InternalServerError);

        var timeProvider = new FakeTimeProvider();

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, timeProvider, NullLoggerFactory.Instance);
        provider.Load();

        handler.Mock.GetMatchCount(request).Should().Be(1);

        // Nothing may be scheduled that checks again later, however much time passes.
        timeProvider.Advance(1.Hours());

        handler.Mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public void Load_MultipleConfigServers_ReturnsNotFoundStatus_DoesNotContinueChecking()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://localhost:8888, http://localhost:8888";

        using var handler = new DelegateToMockHttpClientHandler();

        MockedRequest request = handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}")
            .Respond(HttpStatusCode.NotFound);

        var timeProvider = new FakeTimeProvider();

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, timeProvider, NullLoggerFactory.Instance);
        provider.Load();

        handler.Mock.GetMatchCount(request).Should().Be(1);

        // Nothing may be scheduled that checks again later, however much time passes.
        timeProvider.Advance(1.Hours());

        handler.Mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public void Load_ConfigServerReturnsNotFoundStatus()
    {
        ConfigServerClientOptions options = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}").Respond(HttpStatusCode.NotFound);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);
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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);

        Action action = provider.Load;

        action.Should().ThrowExactly<ConfigServerException>();
        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void Load_MultipleConfigServers_ReturnsNotFoundStatus__DoesNotContinueChecking_FailFastEnabled()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        options.Uri = "http://localhost:8888,http://localhost:8888";

        using var handler = new DelegateToMockHttpClientHandler();

        MockedRequest request = handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}")
            .Respond(HttpStatusCode.NotFound);

        var timeProvider = new FakeTimeProvider();

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, timeProvider, NullLoggerFactory.Instance);

        Action action = provider.Load;

        action.Should().ThrowExactly<ConfigServerException>();
        handler.Mock.GetMatchCount(request).Should().Be(1);

        // Nothing may be scheduled that checks again later, however much time passes.
        timeProvider.Advance(1.Hours());

        handler.Mock.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public void Load_UriInvalid_FailFastEnabled()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.Uri = "http://username:p@ssword@localhost:8888";
        options.FailFast = true;

        using var handler = new DelegateToMockHttpClientHandler();
        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);

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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);

        Action action = provider.Load;

        action.Should().ThrowExactly<ConfigServerException>();
        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void Load_MultipleConfigServers_ReturnsBadStatus_StopsChecking_FailFastEnabled()
    {
        ConfigServerClientOptions options = GetCommonOptions();
        options.FailFast = true;
        options.Uri = "http://localhost:8888, http://localhost:8888, http://localhost:8888";

        using var handler = new DelegateToMockHttpClientHandler();

        MockedRequest request = handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{options.Name}/{options.Environment}")
            .Respond(HttpStatusCode.InternalServerError);

        var timeProvider = new FakeTimeProvider();

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, timeProvider, NullLoggerFactory.Instance);

        Action action = provider.Load;

        action.Should().ThrowExactly<ConfigServerException>();
        handler.Mock.GetMatchCount(request).Should().Be(1);

        // Nothing may be scheduled that checks again later, however much time passes.
        timeProvider.Advance(1.Hours());

        handler.Mock.GetMatchCount(request).Should().Be(1);
    }

    [Theory]
    [InlineData(10, 1.1, 2000, 5, "10,11,12,13")]
    [InlineData(1000, 2.0, 3000, 5, "1000,2000,3000,3000")]
    public async Task Load_ConfigServerReturnsBadStatus_FailFastEnabled_RetryEnabled(int initialInterval, double multiplier, int maxInterval, int maxAttempts,
        string delaysExpected)
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
                    MaxAttempts = maxAttempts,
                    InitialInterval = initialInterval,
                    Multiplier = multiplier,
                    MaxInterval = maxInterval
                },
                Timeout = 1000
            };

            using var handler = new DelegateToMockHttpClientHandler();
            MockedRequest request = handler.Mock.When(HttpMethod.Get, "http://localhost:8888/myName/Production").Respond(HttpStatusCode.InternalServerError);

            var timeProvider = new RecordingFakeTimeProvider();
            using var configServerProvider = new ConfigServerConfigurationProvider(options, null, null, () => handler, timeProvider, tracer.LoggerFactory);

            // Loading blocks while it waits to retry, so it runs on another thread.
            Task loadTask = Task.Run(configServerProvider.Load, CancellationToken.None);

            // Each retry waits first. Advancing the clock by exactly that delay lets the next attempt start.
            for (int index = 0; index < maxAttempts - 1; index++)
            {
                TimeSpan delay = await timeProvider.WaitForNextTimerAsync(TestContext.Current.CancellationToken);
                timeProvider.Advance(delay);
            }

            Func<Task> action = async () => await loadTask;

            await action.Should().ThrowExactlyAsync<ConfigServerException>().WithMessage($"*after {maxAttempts} attempts*");

            handler.Mock.GetMatchCount(request).Should().Be(maxAttempts);

            TimeSpan[] dueTimesExpected = [.. delaysExpected.Split(',').Select(int.Parse).Select(ms => TimeSpan.FromMilliseconds(ms))];
            timeProvider.DueTimes.Should().Equal(dueTimesExpected);

            // After the last attempt, nothing may be scheduled that tries again later, however much time passes.
            timeProvider.Advance(1.Hours());

            handler.Mock.GetMatchCount(request).Should().Be(maxAttempts);
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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);
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

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);
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
        const string responseJson = """
            {
              "name": "name",
              "profiles": [
                "environment"
              ],
              "label": "main",
              "propertySources": [
                {
                  "name": "source",
                  "source": {
                    "response-key": "response-value"
                  }
                }
              ]
            }
            """;

        // Discovery, fail-fast and retry are not enabled, because they change the flow of loading.
        var options = new ConfigServerClientOptions
        {
            Enabled = true,
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
                InitialInterval = 500,
                MaxInterval = 5000,
                Multiplier = 2.0,
                MaxAttempts = 10
            },
            Discovery =
            {
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

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.When(HttpMethod.Post, "https://uaa.example.com/oauth/token").Respond("application/json", """{ "access_token": "access-token" }""");
        handler.Mock.When(HttpMethod.Get, "https://foo.bar/name/environment/main").Respond("application/json", responseJson);

        using var provider = new ConfigServerConfigurationProvider(options, null, null, () => handler, TimeProvider.System, NullLoggerFactory.Instance);
        provider.Load();

        // Proves that loading from Config Server took place, so the absence of the client settings below is meaningful.
        provider.TryGet("response-key", out string? responseValue).Should().BeTrue();
        responseValue.Should().Be("response-value");

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
        ConfigServerClientOptions clientOptions = GetCommonOptions();

        using var handler = new DelegateToMockHttpClientHandler();
        int requestCount = 0;

        handler.Mock.When(HttpMethod.Get, $"http://localhost:8888/{clientOptions.Name}/{clientOptions.Environment}").Respond(_ =>
        {
            // Alternate the response, so the data really changes between reloads.
            string alternatingValue = Interlocked.Increment(ref requestCount) % 2 == 0 ? "even" : "odd";

            string responseJson = $$"""
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
                        "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca",
                        "alternating": "{{alternatingValue}}"
                      }
                    }
                  ]
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddConfigServer(clientOptions, null, () => handler, NullLoggerFactory.Instance);
        using ConfigurationRoot configurationRoot = configurationBuilder.BuildAsRoot();

        using ConfigServerConfigurationProvider provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Single();

        int changeCount = 0;
        using IDisposable changeRegistration = ChangeToken.OnChange(configurationRoot.GetReloadToken, () => Interlocked.Increment(ref changeCount));

        TestOptions? testOptions = null;
        using var tokenSource = new CancellationTokenSource(250.Milliseconds());

        // Not passing the token to Task.Run: the loop observes it itself, and the task must always start so it can be awaited below.
        Task<int> reloadTask = Task.Run(() =>
        {
            int reloadCount = 0;

            while (!tokenSource.IsCancellationRequested)
            {
                configurationRoot.Reload();
                reloadCount++;
            }

            return reloadCount;
        }, CancellationToken.None);

        while (!tokenSource.IsCancellationRequested)
        {
            testOptions = configurationRoot.Get<TestOptions>();
        }

        // Surfaces any exception thrown while reloading in the background, and proves that reloading actually happened.
        int reloadsCompleted = await reloadTask;
        reloadsCompleted.Should().BeGreaterThan(0);
        changeCount.Should().BeGreaterThan(0);

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
