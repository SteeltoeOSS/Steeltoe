// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.Net;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class HostBuilderTest
{
    // Prevents HttpClient from timing out while stepping through code.
    private static readonly string VeryHighConnectionTimeoutForDebuggingTests = 15.Minutes().TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

    // Tests that refresh periodically use a fake clock, so they advance time instead of waiting for it.
    private static readonly TimeSpan RefreshInterval = 1.Minutes();

    // Note: These tests intentionally contain sleeps (Task.Delay), because signaling can't be used to indicate something did NOT happen.
    // Advancing the clock far beyond the refresh interval first makes any scheduled refresh due, so the sleep only gives it time to run.
    // Under high load, this may result in tests succeeding when they should have failed, which is still better than randomly failing tests.

    [Fact]
    public async Task CanUseDynamicHttpPort()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = "http://*:0",
            ["Spring:Boot:Admin:Client:Url"] = "http://sba-server.com",
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        Application? requestApplication = null;

        handler.Mock.Expect(HttpMethod.Post, "http://sba-server.com/instances").With(message =>
        {
            requestApplication = message.Content?.ReadFromJsonAsync<Application>().GetAwaiter().GetResult();
            return true;
        }).Respond("application/json", """{"Id":"1"}""");

        await using HostWrapper app = HostWrapper.Wrap(builder.Build());
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        await app.StartAsync(TestContext.Current.CancellationToken);

        handler.Mock.VerifyNoOutstandingExpectation();

        requestApplication.Should().NotBeNull();
        requestApplication.ServiceUrl.Scheme.Should().Be("http");
        requestApplication.ServiceUrl.Port.Should().NotBe(5000);
        requestApplication.ServiceUrl.Port.Should().BePositive();
    }

    [FactSkippedOnPlatform(nameof(OSPlatform.OSX))]
    public async Task CanUseDynamicHttpsPort()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = "https://*:0",
            ["Spring:Boot:Admin:Client:Url"] = "http://sba-server.com",
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        Application? requestApplication = null;

        handler.Mock.Expect(HttpMethod.Post, "http://sba-server.com/instances").With(message =>
        {
            requestApplication = message.Content?.ReadFromJsonAsync<Application>().GetAwaiter().GetResult();
            return true;
        }).Respond("application/json", """{"Id":"1"}""");

        await using HostWrapper app = HostWrapper.Wrap(builder.Build());
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        await app.StartAsync(TestContext.Current.CancellationToken);

        handler.Mock.VerifyNoOutstandingExpectation();

        requestApplication.Should().NotBeNull();
        requestApplication.ServiceUrl.Scheme.Should().Be("https");
        requestApplication.ServiceUrl.Port.Should().NotBe(5000);
        requestApplication.ServiceUrl.Port.Should().BePositive();
    }

    [Fact]
    public async Task PerformsPeriodicRefresh()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://sba-server.com",
            ["Spring:Boot:Admin:Client:RefreshInterval"] = RefreshInterval.ToString(),
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        var timeProvider = new FakeTimeProvider();

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock = handler.Mock.When(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");

        await using HostWrapper app = HostWrapper.Wrap(builder.Build());
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
        using var cycleWaiter = new RefreshCycleWaiter(runner);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await cycleWaiter.WaitForCyclesAsync(1);

        timeProvider.Advance(RefreshInterval);
        await cycleWaiter.WaitForCyclesAsync(1);

        handler.Mock.GetMatchCount(registerMock).Should().Be(2);
    }

    [Fact]
    public async Task PeriodicRefreshCanBeOffAtStartup()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://sba-server.com",
            ["Spring:Boot:Admin:Client:RefreshInterval"] = "0",
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        var timeProvider = new FakeTimeProvider();

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock = handler.Mock.When(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");

        await using HostWrapper app = HostWrapper.Wrap(builder.Build());
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
        using var cycleWaiter = new RefreshCycleWaiter(runner);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await cycleWaiter.WaitForCyclesAsync(1);

        timeProvider.Advance(1.Hours());
        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(registerMock).Should().Be(1);
        cycleWaiter.PendingCycles.Should().Be(0);
    }

    [Fact]
    public async Task PeriodicRefreshCanBeTurnedOnAfterStart()
    {
        var timeProvider = new FakeTimeProvider();
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile($$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server.com",
                      "RefreshInterval": "0",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryAppSettingsJsonFile(fileProvider);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock = handler.Mock.When(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");

        await using HostWrapper app = HostWrapper.Wrap(builder.Build());
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
        using var cycleWaiter = new RefreshCycleWaiter(runner);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await cycleWaiter.WaitForCyclesAsync(1);

        timeProvider.Advance(1.Hours());
        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(registerMock).Should().Be(1);
        cycleWaiter.PendingCycles.Should().Be(0);

        fileProvider.ReplaceAppSettingsJsonFile($$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server.com",
                      "RefreshInterval": "{{RefreshInterval}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        timeProvider.Advance(RefreshInterval);
        await cycleWaiter.WaitForCyclesAsync(1);

        handler.Mock.GetMatchCount(registerMock).Should().Be(2);
    }

    [Fact]
    public async Task PeriodicRefreshCanBeTurnedOffAfterStart()
    {
        var timeProvider = new FakeTimeProvider();
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile($$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server.com",
                      "RefreshInterval": "{{RefreshInterval}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryAppSettingsJsonFile(fileProvider);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock = handler.Mock.When(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");
        MockedRequest otherRegisterMock = handler.Mock.When(HttpMethod.Post, "http://other-server.com/instances").Respond("application/json", """{"Id":"2"}""");

        await using HostWrapper app = HostWrapper.Wrap(builder.Build());
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
        using var cycleWaiter = new RefreshCycleWaiter(runner);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await cycleWaiter.WaitForCyclesAsync(1);

        timeProvider.Advance(RefreshInterval);
        await cycleWaiter.WaitForCyclesAsync(1);

        handler.Mock.GetMatchCount(registerMock).Should().Be(2);

        fileProvider.ReplaceAppSettingsJsonFile($$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://other-server.com",
                      "RefreshInterval": "0",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        timeProvider.Advance(1.Hours());
        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(otherRegisterMock).Should().Be(0);
        cycleWaiter.PendingCycles.Should().Be(0);
    }

    [Fact]
    public async Task UnregistersAtShutdown()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://sba-server.com",
            ["Spring:Boot:Admin:Client:RefreshInterval"] = "0",
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");
        handler.Mock.Expect(HttpMethod.Delete, "http://sba-server.com/instances/1").Respond(HttpStatusCode.NoContent);

        await using (WebApplication app = builder.Build())
        {
            app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
            var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
            using var cycleWaiter = new RefreshCycleWaiter(runner);

            await app.StartAsync(TestContext.Current.CancellationToken);
            await cycleWaiter.WaitForCyclesAsync(1);

            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DoesNotCrashWhenUnregisterFails()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://sba-server.com",
            ["Spring:Boot:Admin:Client:RefreshInterval"] = "0",
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");
        handler.Mock.Expect(HttpMethod.Delete, "http://sba-server.com/instances/1").Respond(HttpStatusCode.InternalServerError);

        await using (WebApplication app = builder.Build())
        {
            app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
            var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
            using var cycleWaiter = new RefreshCycleWaiter(runner);

            await app.StartAsync(TestContext.Current.CancellationToken);
            await cycleWaiter.WaitForCyclesAsync(1);

            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DoesNotUnregisterIfRegisterFailed()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://sba-server.com",
            ["Spring:Boot:Admin:Client:RefreshInterval"] = "0",
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://sba-server.com/instances").Respond(HttpStatusCode.InternalServerError);
        MockedRequest unregisterMock = handler.Mock.When(HttpMethod.Delete, "http://sba-server.com/instances/*").Respond(HttpStatusCode.InternalServerError);

        await using (WebApplication app = builder.Build())
        {
            app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
            var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
            using var cycleWaiter = new RefreshCycleWaiter(runner);

            await app.StartAsync(TestContext.Current.CancellationToken);
            await cycleWaiter.WaitForCyclesAsync(1);

            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        handler.Mock.GetMatchCount(unregisterMock).Should().Be(0);
        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UnregistersFromPreviousServerOnConfigurationChange()
    {
        var timeProvider = new FakeTimeProvider();
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile($$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server1.com",
                      "RefreshInterval": "{{RefreshInterval}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryAppSettingsJsonFile(fileProvider);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock1 = handler.Mock.When(HttpMethod.Post, "http://sba-server1.com/instances").Respond("application/json", """{"Id":"1"}""");
        MockedRequest unregisterMock1 = handler.Mock.When(HttpMethod.Delete, "http://sba-server1.com/instances/1").Respond(HttpStatusCode.NoContent);
        MockedRequest registerMock2 = handler.Mock.When(HttpMethod.Post, "http://sba-server2.com/instances").Respond("application/json", """{"Id":"2"}""");
        MockedRequest unregisterMock2 = handler.Mock.When(HttpMethod.Delete, "http://sba-server2.com/instances/2").Respond(HttpStatusCode.NoContent);

        await using HostWrapper app = HostWrapper.Wrap(builder.Build());
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
        using var cycleWaiter = new RefreshCycleWaiter(runner);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await cycleWaiter.WaitForCyclesAsync(1);

        timeProvider.Advance(RefreshInterval);
        await cycleWaiter.WaitForCyclesAsync(1);

        handler.Mock.GetMatchCount(registerMock1).Should().Be(2);

        fileProvider.ReplaceAppSettingsJsonFile($$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server2.com",
                      "RefreshInterval": "{{RefreshInterval}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        timeProvider.Advance(RefreshInterval);
        await cycleWaiter.WaitForCyclesAsync(1);

        handler.Mock.GetMatchCount(unregisterMock1).Should().Be(1);
        handler.Mock.GetMatchCount(registerMock2).Should().Be(1);
        handler.Mock.GetMatchCount(unregisterMock2).Should().Be(0);
    }

    [Fact]
    public async Task UnregistersFromPreviousServerOnShutdownAfterConfigurationBecameInvalid()
    {
        var timeProvider = new FakeTimeProvider();
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile($$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server1.com",
                      "RefreshInterval": "{{RefreshInterval}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryAppSettingsJsonFile(fileProvider);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock1 = handler.Mock.When(HttpMethod.Post, "http://sba-server1.com/instances").Respond("application/json", """{"Id":"1"}""");
        MockedRequest unregisterMock1 = handler.Mock.When(HttpMethod.Delete, "http://sba-server1.com/instances/1").Respond(HttpStatusCode.NoContent);
        MockedRequest registerMock2 = handler.Mock.When(HttpMethod.Post, "http://sba-server2.com/instances").Respond("application/json", """{"Id":"2"}""");
        MockedRequest unregisterMock2 = handler.Mock.When(HttpMethod.Delete, "http://sba-server2.com/instances/2").Respond(HttpStatusCode.NoContent);

        await using (WebApplication app = builder.Build())
        {
            app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
            var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
            using var cycleWaiter = new RefreshCycleWaiter(runner);

            await app.StartAsync(TestContext.Current.CancellationToken);
            await cycleWaiter.WaitForCyclesAsync(1);

            timeProvider.Advance(RefreshInterval);
            await cycleWaiter.WaitForCyclesAsync(1);

            handler.Mock.GetMatchCount(registerMock1).Should().Be(2);

            fileProvider.ReplaceAppSettingsJsonFile($$"""
                {
                  "Spring": {
                    "Boot": {
                      "Admin": {
                        "Client": {
                          "Url": "http://sba-server2.com",
                          "BaseUrl": "not-a-valid-uri",
                          "RefreshInterval": "{{RefreshInterval}}",
                          "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                        }
                      }
                    }
                  }
                }
                """);

            fileProvider.NotifyChanged();

            // Two refresh cycles with the invalid configuration must not affect the registration with the previous server.
            timeProvider.Advance(RefreshInterval);
            await cycleWaiter.WaitForCyclesAsync(1);

            timeProvider.Advance(RefreshInterval);
            await cycleWaiter.WaitForCyclesAsync(1);

            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        handler.Mock.GetMatchCount(unregisterMock1).Should().Be(1);
        handler.Mock.GetMatchCount(registerMock2).Should().Be(0);
        handler.Mock.GetMatchCount(unregisterMock2).Should().Be(0);
    }

    [Fact]
    public async Task RecoversFromSpringBootAdminServerRestarts()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://sba-server.com",
            ["Spring:Boot:Admin:Client:RefreshInterval"] = RefreshInterval.ToString(),
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        var timeProvider = new FakeTimeProvider();

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSpringBootAdminClient();

        bool isServerOnline = false;
        using var handler = new DelegateToMockHttpClientHandler();

        // ReSharper disable once AccessToModifiedClosure
        MockedRequest registerMock = handler.Mock.When(HttpMethod.Post, "http://sba-server.com/instances").Respond(_ => isServerOnline
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"Id":"1"}""", Encoding.UTF8, "application/json")
            }
            : throw new HttpRequestException("Server is unreachable."));

        await using HostWrapper app = HostWrapper.Wrap(builder.Build());
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();
        using var cycleWaiter = new RefreshCycleWaiter(runner);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await cycleWaiter.WaitForCyclesAsync(1);

        timeProvider.Advance(RefreshInterval);
        await cycleWaiter.WaitForCyclesAsync(1);

        handler.Mock.GetMatchCount(registerMock).Should().Be(2);
        runner.LastRegistrationId.Should().BeNull();

        isServerOnline = true;
        timeProvider.Advance(RefreshInterval);
        await cycleWaiter.WaitForCyclesAsync(1);

        runner.LastRegistrationId.Should().NotBeNull();

        isServerOnline = false;
        timeProvider.Advance(RefreshInterval);
        await cycleWaiter.WaitForCyclesAsync(1);

        runner.LastRegistrationId.Should().NotBeNull();
    }

    private sealed class RefreshCycleWaiter : IDisposable
    {
        private readonly SpringBootAdminRefreshRunner _runner;
        private readonly SemaphoreSlim _signal = new(0);

        public int PendingCycles => _signal.CurrentCount;

        public RefreshCycleWaiter(SpringBootAdminRefreshRunner runner)
        {
            _runner = runner;
            _runner.CycleCompleted += OnCycleCompleted;
        }

        public async Task WaitForCyclesAsync(int count)
        {
            using var timeoutSource = new CancellationTokenSource(30.Seconds());
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, timeoutSource.Token);

            try
            {
                for (int index = 0; index < count; index++)
                {
                    await _signal.WaitAsync(linkedSource.Token);
                }
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !TestContext.Current.CancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Timed out waiting for {count} refresh cycle(s) to complete.");
            }
        }

        public void Dispose()
        {
            _runner.CycleCompleted -= OnCycleCompleted;
            _signal.Dispose();
        }

        private void OnCycleCompleted(object? sender, EventArgs args)
        {
            _signal.Release();
        }
    }
}
