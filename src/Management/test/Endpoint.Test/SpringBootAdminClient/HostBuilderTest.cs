// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.Net;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class HostBuilderTest
{
    // Prevents HttpClient from timing out while stepping through code.
    private const string VeryHighConnectionTimeoutForDebuggingTests = "900000"; // 15 minutes

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

        await using WebApplication app = builder.Build();
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

        await using WebApplication app = builder.Build();
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
            ["Spring:Boot:Admin:Client:RefreshInterval"] = 100.Milliseconds().ToString(),
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock = handler.Mock.When(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        await app.StartAsync(TestContext.Current.CancellationToken);

        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(registerMock).Should().BeGreaterThan(1);
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

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock = handler.Mock.When(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        await app.StartAsync(TestContext.Current.CancellationToken);

        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(registerMock).Should().Be(1);
    }

    [Fact]
    public async Task PeriodicRefreshCanBeTurnedOnAfterStart()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, $$"""
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
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock = handler.Mock.When(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        await app.StartAsync(TestContext.Current.CancellationToken);

        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(registerMock).Should().Be(1);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, $$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server.com",
                      "RefreshInterval": "{{100.Milliseconds()}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();
        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(registerMock).Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task PeriodicRefreshCanBeTurnedOffAfterStart()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, $$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server.com",
                      "RefreshInterval": "{{100.Milliseconds()}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock = handler.Mock.When(HttpMethod.Post, "http://sba-server.com/instances").Respond("application/json", """{"Id":"1"}""");
        MockedRequest otherRegisterMock = handler.Mock.When(HttpMethod.Post, "http://other-server.com/instances").Respond("application/json", """{"Id":"2"}""");

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        await app.StartAsync(TestContext.Current.CancellationToken);

        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(registerMock).Should().BeGreaterThan(1);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, $$"""
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
        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(otherRegisterMock).Should().Be(0);
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
            await app.StartAsync(TestContext.Current.CancellationToken);

            await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

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
            await app.StartAsync(TestContext.Current.CancellationToken);

            await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

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
        MockedRequest unregisterMock = handler.Mock.When(HttpMethod.Delete, "http://sba-server.com/instances/1").Respond(HttpStatusCode.InternalServerError);

        await using (WebApplication app = builder.Build())
        {
            app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
            await app.StartAsync(TestContext.Current.CancellationToken);

            await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        handler.Mock.GetMatchCount(unregisterMock).Should().Be(0);
        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UnregistersFromPreviousServerOnConfigurationChange()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, $$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server1.com",
                      "RefreshInterval": "{{100.Milliseconds()}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock1 = handler.Mock.When(HttpMethod.Post, "http://sba-server1.com/instances").Respond("application/json", """{"Id":"1"}""");
        MockedRequest unregisterMock1 = handler.Mock.When(HttpMethod.Delete, "http://sba-server1.com/instances/1").Respond(HttpStatusCode.NoContent);
        MockedRequest registerMock2 = handler.Mock.When(HttpMethod.Post, "http://sba-server2.com/instances").Respond("application/json", """{"Id":"2"}""");
        MockedRequest unregisterMock2 = handler.Mock.When(HttpMethod.Delete, "http://sba-server2.com/instances/2").Respond(HttpStatusCode.NoContent);

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        await app.StartAsync(TestContext.Current.CancellationToken);

        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(registerMock1).Should().BeGreaterThan(1);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, $$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server2.com",
                      "RefreshInterval": "{{100.Milliseconds()}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();
        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(unregisterMock1).Should().Be(1);
        handler.Mock.GetMatchCount(registerMock2).Should().BeGreaterThan(1);
        handler.Mock.GetMatchCount(unregisterMock2).Should().Be(0);
    }

    [Fact]
    public async Task UnregistersFromPreviousServerOnShutdownAfterConfigurationBecameInvalid()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, $$"""
            {
              "Spring": {
                "Boot": {
                  "Admin": {
                    "Client": {
                      "Url": "http://sba-server1.com",
                      "RefreshInterval": "{{100.Milliseconds()}}",
                      "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                    }
                  }
                }
              }
            }
            """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest registerMock1 = handler.Mock.When(HttpMethod.Post, "http://sba-server1.com/instances").Respond("application/json", """{"Id":"1"}""");
        MockedRequest unregisterMock1 = handler.Mock.When(HttpMethod.Delete, "http://sba-server1.com/instances/1").Respond(HttpStatusCode.NoContent);
        MockedRequest registerMock2 = handler.Mock.When(HttpMethod.Post, "http://sba-server2.com/instances").Respond("application/json", """{"Id":"2"}""");
        MockedRequest unregisterMock2 = handler.Mock.When(HttpMethod.Delete, "http://sba-server2.com/instances/2").Respond(HttpStatusCode.NoContent);

        await using (WebApplication app = builder.Build())
        {
            app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
            await app.StartAsync(TestContext.Current.CancellationToken);

            await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

            handler.Mock.GetMatchCount(registerMock1).Should().BeGreaterThan(1);

            fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, $$"""
                {
                  "Spring": {
                    "Boot": {
                      "Admin": {
                        "Client": {
                          "Url": "http://sba-server2.com",
                          "BaseUrl": "not-a-valid-uri",
                          "ConnectionTimeoutMs": {{VeryHighConnectionTimeoutForDebuggingTests}}
                        }
                      }
                    }
                  }
                }
                """);

            fileProvider.NotifyChanged();
            await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

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
            ["Spring:Boot:Admin:Client:RefreshInterval"] = 100.Milliseconds().ToString(),
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = VeryHighConnectionTimeoutForDebuggingTests
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
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

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        await app.StartAsync(TestContext.Current.CancellationToken);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();

        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        handler.Mock.GetMatchCount(registerMock).Should().BeGreaterThan(1);
        runner.LastRegistrationId.Should().BeNull();

        isServerOnline = true;
        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        runner.LastRegistrationId.Should().NotBeNull();

        isServerOnline = false;
        await Task.Delay(500.Milliseconds(), TestContext.Current.CancellationToken);

        runner.LastRegistrationId.Should().NotBeNull();
    }
}
