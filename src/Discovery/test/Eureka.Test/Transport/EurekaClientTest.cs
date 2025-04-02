// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class EurekaClientTest
{
    private const string GetApplicationsFullJsonResponse = """
        {
          "applications": {
            "versions__delta": "1",
            "apps__hashcode": "UP_1_",
            "application": [
              {
                "name": "FOO",
                "instance": [
                  {
                    "instanceId": "localhost:foo",
                    "hostName": "localhost",
                    "app": "FOO",
                    "ipAddr": "192.168.56.1",
                    "status": "UP",
                    "port": {
                      "$": 8080,
                      "@enabled": "true"
                    },
                    "securePort": {
                      "$": 443,
                      "@enabled": "false"
                    },
                    "countryId": 1,
                    "dataCenterInfo": {
                      "@class": "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
                      "name": "MyOwn"
                    },
                    "leaseInfo": {
                      "renewalIntervalInSecs": 30,
                      "durationInSecs": 90,
                      "registrationTimestamp": 1457714988223,
                      "renewalTimestamp": 1457716158319,
                      "evictionTimestamp": 0,
                      "serviceUpTimestamp": 1457714988223
                    },
                    "metadata": {
                      "@class": "java.util.Collections$EmptyMap"
                    },
                    "homePageUrl": "http://localhost:8080/",
                    "statusPageUrl": "http://localhost:8080/info",
                    "healthCheckUrl": "http://localhost:8080/health",
                    "vipAddress": "foo",
                    "isCoordinatingDiscoveryServer": "false",
                    "lastUpdatedTimestamp": "1457714988223",
                    "lastDirtyTimestamp": "1457714988172",
                    "actionType": "ADDED"
                  }
                ]
              }
            ]
          }
        }
        """;

    private const string GetApplicationsDeltaJsonResponse = """
        {
          "applications": {
            "versions__delta": "3",
            "apps__hashcode": "UP_1_",
            "application": [
              {
                "name": "FOO",
                "instance": [
                  {
                    "instanceId": "localhost:foo",
                    "hostName": "localhost",
                    "app": "FOO",
                    "ipAddr": "192.168.56.1",
                    "status": "UP",
                    "overriddenstatus": "UNKNOWN",
                    "port": {
                      "$": 8080,
                      "@enabled": "true"
                    },
                    "securePort": {
                      "$": 443,
                      "@enabled": "false"
                    },
                    "countryId": 1,
                    "dataCenterInfo": {
                      "@class": "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
                      "name": "MyOwn"
                    },
                    "leaseInfo": {
                      "renewalIntervalInSecs": 30,
                      "durationInSecs": 90,
                      "registrationTimestamp": 1457714988223,
                      "renewalTimestamp": 1457716158319,
                      "evictionTimestamp": 0,
                      "serviceUpTimestamp": 1457714988223
                    },
                    "metadata": {
                      "@class": "java.util.Collections$EmptyMap"
                    },
                    "homePageUrl": "http://localhost:8080/",
                    "statusPageUrl": "http://localhost:8080/info",
                    "healthCheckUrl": "http://localhost:8080/health",
                    "vipAddress": "foo",
                    "isCoordinatingDiscoveryServer": "false",
                    "lastUpdatedTimestamp": "1457714988223",
                    "lastDirtyTimestamp": "1457714988172",
                    "actionType": "MODIFIED"
                  }
                ]
              }
            ]
          }
        }
        """;

    [Fact]
    public async Task RegisterAsync_ThrowsOnUnreachableServer()
    {
        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal));

        var services = new ServiceCollection();
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        services.AddOptions<EurekaClientOptions>().Configure(options => options.EurekaServerServiceUrls = "http://host-that-does-not-exist.net:9999/");
        services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory());
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        var instance = new InstanceInfo("some", "FOOBAR", "localhost", "127.0.0.1", new DataCenterInfo(), TimeProvider.System)
        {
            NonSecurePort = 8080,
            IsNonSecurePortEnabled = true,
            SecurePort = 9090,
            IsSecurePortEnabled = false,
            LastUpdatedTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc),
            LastDirtyTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc)
        };

        Func<Task> asyncAction = async () => await client.RegisterAsync(instance, TestContext.Current.CancellationToken);

        await asyncAction.Should().ThrowAsync<EurekaTransportException>().WithMessage("Failed to execute request on all known Eureka servers.");

        IList<string> logMessages = capturingLoggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo(
            $"DBUG {typeof(EurekaClient).FullName}: Sending POST request to 'http://host-that-does-not-exist.net:9999/apps/FOOBAR' with body: " +
            """{"instance":{"instanceId":"some","app":"FOOBAR","ipAddr":"127.0.0.1","port":{"@enabled":"true","$":8080},"securePort":{"@enabled":"false","$":9090},"dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},"hostName":"localhost","overriddenstatus":"UNKNOWN","metadata":{"@class":"java.util.Collections$EmptyMap"},"lastUpdatedTimestamp":"1708427732823","lastDirtyTimestamp":"1708427732823"}}.""",
            $"WARN {typeof(EurekaClient).FullName}: Failed to execute HTTP POST request to 'http://host-that-does-not-exist.net:9999/apps/FOOBAR' in attempt 1.");
    }

    [Fact]
    public async Task RegisterAsync_ThrowsOnErrorResponse()
    {
        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal));

        var services = new ServiceCollection();
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();

        httpClientHandler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOOBAR")
            .Respond(HttpStatusCode.NotFound, new StringContent("Sorry!", Encoding.UTF8, "application/json"));

        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        var instance = new InstanceInfo("some", "FOOBAR", "localhost", "127.0.0.1", new DataCenterInfo(), TimeProvider.System)
        {
            NonSecurePort = 8080,
            IsNonSecurePortEnabled = true,
            SecurePort = 9090,
            IsSecurePortEnabled = false,
            LastUpdatedTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc),
            LastDirtyTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc)
        };

        Func<Task> asyncAction = async () => await client.RegisterAsync(instance, TestContext.Current.CancellationToken);

        await asyncAction.Should().ThrowAsync<EurekaTransportException>().WithMessage("Failed to execute request on all known Eureka servers.");

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        IList<string> logMessages = capturingLoggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo(
        [
            $"DBUG {typeof(EurekaClient).FullName}: Sending POST request to 'http://localhost:8761/eureka/apps/FOOBAR' with body: " +
            """{"instance":{"instanceId":"some","app":"FOOBAR","ipAddr":"127.0.0.1","port":{"@enabled":"true","$":8080},"securePort":{"@enabled":"false","$":9090},"dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},"hostName":"localhost","overriddenstatus":"UNKNOWN","metadata":{"@class":"java.util.Collections$EmptyMap"},"lastUpdatedTimestamp":"1708427732823","lastDirtyTimestamp":"1708427732823"}}.""",
            $"DBUG {typeof(EurekaClient).FullName}: HTTP POST request to 'http://localhost:8761/eureka/apps/FOOBAR' returned status 404 in attempt 1.",
            $"INFO {typeof(EurekaClient).FullName}: HTTP POST request to 'http://localhost:8761/eureka/apps/FOOBAR' failed with status 404: Sorry!"
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task RegisterAsync_ThrowsOnRetryLimitReached()
    {
        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal));

        var services = new ServiceCollection();
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        services.AddOptions<EurekaClientOptions>().Configure(options => options.EurekaServer.RetryCount = 0);
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOOBAR").Respond(HttpStatusCode.NotFound);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        var instance = new InstanceInfo("some", "FOOBAR", "localhost", "127.0.0.1", new DataCenterInfo(), TimeProvider.System)
        {
            NonSecurePort = 8080,
            IsNonSecurePortEnabled = true,
            SecurePort = 9090,
            IsSecurePortEnabled = false,
            LastUpdatedTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc),
            LastDirtyTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc)
        };

        Func<Task> asyncAction = async () => await client.RegisterAsync(instance, TestContext.Current.CancellationToken);

        await asyncAction.Should().ThrowAsync<EurekaTransportException>().WithMessage("Retry limit reached; giving up on completing the HTTP request.");

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        IList<string> logMessages = capturingLoggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo(
        [
            $"DBUG {typeof(EurekaClient).FullName}: Sending POST request to 'http://localhost:8761/eureka/apps/FOOBAR' with body: " +
            """{"instance":{"instanceId":"some","app":"FOOBAR","ipAddr":"127.0.0.1","port":{"@enabled":"true","$":8080},"securePort":{"@enabled":"false","$":9090},"dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},"hostName":"localhost","overriddenstatus":"UNKNOWN","metadata":{"@class":"java.util.Collections$EmptyMap"},"lastUpdatedTimestamp":"1708427732823","lastDirtyTimestamp":"1708427732823"}}.""",
            $"DBUG {typeof(EurekaClient).FullName}: HTTP POST request to 'http://localhost:8761/eureka/apps/FOOBAR' returned status 404 in attempt 1.",
            $"INFO {typeof(EurekaClient).FullName}: HTTP POST request to 'http://localhost:8761/eureka/apps/FOOBAR' failed with status 404: "
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task RegisterAsync_LogsWarningOnCloudWithLocalhost()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal));

        var services = new ServiceCollection();
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOOBAR").Respond(HttpStatusCode.NotFound);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        var instance = new InstanceInfo("some", "FOOBAR", "localhost", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        Func<Task> asyncAction = async () => await client.RegisterAsync(instance, TestContext.Current.CancellationToken);

        await asyncAction.Should().ThrowAsync<EurekaTransportException>().WithMessage("Failed to execute request on all known Eureka servers.");

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        IList<string> logMessages = capturingLoggerProvider.GetAll();

        logMessages.Should().Contain(
            $"WARN {typeof(EurekaClient).FullName}: Registering with hostname 'localhost' in containerized or cloud environments may not be valid. Please configure Eureka:Instance:HostName with a non-localhost address.");
    }

    [Fact]
    public async Task RegisterAsync_SendsRequestToServer()
    {
        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal));

        using JsonDocument requestDocument = JsonDocument.Parse("""
            {
              "instance": {
                "instanceId": "some",
                "app": "FOOBAR",
                "ipAddr": "127.0.0.1",
                "port": {
                  "@enabled": "true",
                  "$": 8080
                },
                "securePort": {
                  "@enabled": "false",
                  "$": 9090
                },
                "dataCenterInfo": {
                  "@class": "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
                  "name": "MyOwn"
                },
                "hostName": "localhost",
                "overriddenstatus": "UNKNOWN",
                "metadata": {
                  "@class": "java.util.Collections$EmptyMap"
                },
                "lastUpdatedTimestamp": "1708427732823",
                "lastDirtyTimestamp": "1708427732823"
              }
            }
            """);

        string jsonRequest = JsonSerializer.Serialize(requestDocument);

        var services = new ServiceCollection();
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOOBAR").WithContent(jsonRequest).Respond(HttpStatusCode.NoContent);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        var instance = new InstanceInfo("some", "FOOBAR", "localhost", "127.0.0.1", new DataCenterInfo(), TimeProvider.System)
        {
            NonSecurePort = 8080,
            IsNonSecurePortEnabled = true,
            SecurePort = 9090,
            IsSecurePortEnabled = false,
            LastUpdatedTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc),
            LastDirtyTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc)
        };

        await client.RegisterAsync(instance, TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        IList<string> logMessages = capturingLoggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo(
        [
            $"DBUG {typeof(EurekaClient).FullName}: Sending POST request to 'http://localhost:8761/eureka/apps/FOOBAR' with body: " +
            """{"instance":{"instanceId":"some","app":"FOOBAR","ipAddr":"127.0.0.1","port":{"@enabled":"true","$":8080},"securePort":{"@enabled":"false","$":9090},"dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},"hostName":"localhost","overriddenstatus":"UNKNOWN","metadata":{"@class":"java.util.Collections$EmptyMap"},"lastUpdatedTimestamp":"1708427732823","lastDirtyTimestamp":"1708427732823"}}.""",
            $"DBUG {typeof(EurekaClient).FullName}: HTTP POST request to 'http://localhost:8761/eureka/apps/FOOBAR' returned status 204 in attempt 1."
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task RegisterAsync_TriesSecondServerIfFirstOneFails()
    {
        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal));

        var services = new ServiceCollection();
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        services.AddOptions<EurekaClientOptions>().Configure(options => options.EurekaServerServiceUrls = "http://server1:8761,http://server2:8761");
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Post, "http://server1:8761/apps/FOOBAR").Respond(HttpStatusCode.NotFound);
        httpClientHandler.Mock.Expect(HttpMethod.Post, "http://server2:8761/apps/FOOBAR").Respond(HttpStatusCode.NoContent);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        var instance = new InstanceInfo("some", "FOOBAR", "localhost", "127.0.0.1", new DataCenterInfo(), TimeProvider.System)
        {
            NonSecurePort = 8080,
            IsNonSecurePortEnabled = true,
            SecurePort = 9090,
            IsSecurePortEnabled = false,
            LastUpdatedTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc),
            LastDirtyTimeUtc = new DateTime(638_440_245_328_236_418, DateTimeKind.Utc)
        };

        await client.RegisterAsync(instance, TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        IList<string> logMessages = capturingLoggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo(
        [
            $"DBUG {typeof(EurekaClient).FullName}: Sending POST request to 'http://server1:8761/apps/FOOBAR' with body: " +
            """{"instance":{"instanceId":"some","app":"FOOBAR","ipAddr":"127.0.0.1","port":{"@enabled":"true","$":8080},"securePort":{"@enabled":"false","$":9090},"dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},"hostName":"localhost","overriddenstatus":"UNKNOWN","metadata":{"@class":"java.util.Collections$EmptyMap"},"lastUpdatedTimestamp":"1708427732823","lastDirtyTimestamp":"1708427732823"}}.""",
            $"DBUG {typeof(EurekaClient).FullName}: HTTP POST request to 'http://server1:8761/apps/FOOBAR' returned status 404 in attempt 1.",
            $"INFO {typeof(EurekaClient).FullName}: HTTP POST request to 'http://server1:8761/apps/FOOBAR' failed with status 404: ",
            $"DBUG {typeof(EurekaClient).FullName}: Sending POST request to 'http://server2:8761/apps/FOOBAR' with body: " +
            """{"instance":{"instanceId":"some","app":"FOOBAR","ipAddr":"127.0.0.1","port":{"@enabled":"true","$":8080},"securePort":{"@enabled":"false","$":9090},"dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},"hostName":"localhost","overriddenstatus":"UNKNOWN","metadata":{"@class":"java.util.Collections$EmptyMap"},"lastUpdatedTimestamp":"1708427732823","lastDirtyTimestamp":"1708427732823"}}.""",
            $"DBUG {typeof(EurekaClient).FullName}: HTTP POST request to 'http://server2:8761/apps/FOOBAR' returned status 204 in attempt 2."
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task RegisterAsync_AddsAuthorizationHeaderFromUsernamePasswordInUri()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<EurekaClientOptions>().Configure(options => options.EurekaServerServiceUrls = "http://user:pass@boo:123/eureka/");
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();

        httpClientHandler.Mock.Expect(HttpMethod.Post, "http://boo:123/eureka/apps/FOOBAR").WithHeaders("Authorization", "Basic dXNlcjpwYXNz")
            .Respond(HttpStatusCode.NoContent);

        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        var instance = new InstanceInfo("some", "FOOBAR", "localhost", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        await client.RegisterAsync(instance, TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RegisterAsync_AddsAuthorizationHeaderFromOnlyPasswordInUri()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<EurekaClientOptions>().Configure(options => options.EurekaServerServiceUrls = "http://:pass@boo:123/eureka/");
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();

        httpClientHandler.Mock.Expect(HttpMethod.Post, "http://boo:123/eureka/apps/FOOBAR").WithHeaders("Authorization", "Basic OnBhc3M=")
            .Respond(HttpStatusCode.NoContent);

        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        var instance = new InstanceInfo("some", "FOOBAR", "localhost", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        await client.RegisterAsync(instance, TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RegisterAsync_SendsTokenRequest()
    {
        const string accessTokenResponse = """
            {
                "access_token": "secret"
            }
            """;

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddOptions<EurekaClientOptions>().Configure(options =>
        {
            options.AccessTokenUri = "https://auth-server/oauth/token";
            options.ClientId = "cli?nt";
            options.ClientSecret = "s3cr?t";
        });

        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var accessTokenHttpClientHandler = new DelegateToMockHttpClientHandler();
        var eurekaHttpClientHandler = new DelegateToMockHttpClientHandler();

        accessTokenHttpClientHandler.Mock.Expect(HttpMethod.Post, "https://auth-server/oauth/token").WithHeaders("Authorization", "Basic Y2xpP250OnMzY3I/dA==")
            .WithFormData("grant_type=client_credentials").Respond("application/json", accessTokenResponse);

        eurekaHttpClientHandler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOOBAR").WithHeaders("Authorization", "Bearer secret")
            .WithHeaders("X-Discovery-AllowRedirect", "false").Respond(HttpStatusCode.NoContent);

        services.AddHttpClient("AccessTokenForEureka").ConfigurePrimaryHttpMessageHandler(_ => accessTokenHttpClientHandler);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => eurekaHttpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        var instance = new InstanceInfo("some", "FOOBAR", "localhost", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        await client.RegisterAsync(instance, TestContext.Current.CancellationToken);

        eurekaHttpClientHandler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DeregisterAsync_SendsRequestToServer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Delete, "http://localhost:8761/eureka/apps/foo/localhost%3Abar%3A1234").Respond(HttpStatusCode.NoContent);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        await client.DeregisterAsync("foo", "localhost:bar:1234", TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HeartbeatAsync_SendsRequestToServer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();

        httpClientHandler.Mock.Expect(HttpMethod.Put, "http://localhost:8761/eureka/apps/FOO/localhost%3Abar%3A1234")
            .WithQueryString("lastDirtyTimestamp", "1708369905756").Respond(HttpStatusCode.OK);

        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        await client.HeartbeatAsync("FOO", "localhost:bar:1234", new DateTime(638_439_667_057_566_585, DateTimeKind.Utc), TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetApplicationsAsync_SendsRequestToServer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", GetApplicationsFullJsonResponse);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        ApplicationInfoCollection apps = await client.GetApplicationsAsync(TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        apps.Should().NotBeNull();
        apps.ApplicationMap.Should().ContainSingle();

        ApplicationInfo? app = apps.GetRegisteredApplication("foo");

        app.Should().NotBeNull();
        app.Name.Should().Be("FOO");

        app.Instances.Should().ContainSingle();
        app.Instances[0].InstanceId.Should().Be("localhost:foo");
        app.Instances[0].VipAddress.Should().Be("foo");
        app.Instances[0].HostName.Should().Be("localhost");
        app.Instances[0].IPAddress.Should().Be("192.168.56.1");
        app.Instances[0].Status.Should().Be(InstanceStatus.Up);
    }

    [Fact]
    public async Task GetApplicationsAsync_ThrowsOnBrokenJsonResponse()
    {
        const string jsonResponse = """{"applications": {""";

        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal));

        var services = new ServiceCollection();
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", jsonResponse);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        Func<Task> asyncAction = async () => _ = await client.GetApplicationsAsync(TestContext.Current.CancellationToken);

        await asyncAction.Should().ThrowAsync<EurekaTransportException>().WithMessage("Failed to execute request on all known Eureka servers.");

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        IList<string> logMessages = capturingLoggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo(
        [
            $"DBUG {typeof(EurekaClient).FullName}: Sending GET request to 'http://localhost:8761/eureka/apps' without request body.",
            $"DBUG {typeof(EurekaClient).FullName}: HTTP GET request to 'http://localhost:8761/eureka/apps' returned status 200 in attempt 1.",
            $"DBUG {typeof(EurekaClient).FullName}: Failed to deserialize HTTP response from GET 'http://localhost:8761/eureka/apps'."
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetDeltaAsync_SendsRequestToServer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps/delta").Respond("application/json", GetApplicationsDeltaJsonResponse);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        ApplicationInfoCollection apps = await client.GetDeltaAsync(TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        apps.Should().NotBeNull();
        apps.ApplicationMap.Should().ContainSingle();

        ApplicationInfo? app = apps.GetRegisteredApplication("foo");

        app.Should().NotBeNull();
        app.Name.Should().Be("FOO");

        app.Instances.Should().ContainSingle();
        app.Instances[0].InstanceId.Should().Be("localhost:foo");
        app.Instances[0].VipAddress.Should().Be("foo");
        app.Instances[0].HostName.Should().Be("localhost");
        app.Instances[0].IPAddress.Should().Be("192.168.56.1");
        app.Instances[0].Status.Should().Be(InstanceStatus.Up);
    }

    [Fact]
    public async Task GetByVipAsync_SendsRequestToServer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/vips/foo").Respond("application/json", GetApplicationsFullJsonResponse);
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        ApplicationInfoCollection apps = await client.GetByVipAsync("foo", TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        apps.Should().NotBeNull();
        apps.ApplicationMap.Should().ContainSingle();

        ApplicationInfo? app = apps.GetRegisteredApplication("foo");

        app.Should().NotBeNull();
        app.Name.Should().Be("FOO");

        app.Instances.Should().ContainSingle();
        app.Instances[0].InstanceId.Should().Be("localhost:foo");
        app.Instances[0].VipAddress.Should().Be("foo");
        app.Instances[0].HostName.Should().Be("localhost");
        app.Instances[0].IPAddress.Should().Be("192.168.56.1");
        app.Instances[0].Status.Should().Be(InstanceStatus.Up);
    }

    [Fact]
    public async Task Redacts_HTTP_headers()
    {
        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("System.Net.Http.HttpClient", StringComparison.Ordinal));

        var services = new ServiceCollection();
        services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        services.AddOptions();
        services.AddSingleton<EurekaServiceUriStateManager>();
        services.AddSingleton<EurekaClient>();
        services.AddSingleton(TimeProvider.System);

        var httpClientHandler = new DelegateToMockHttpClientHandler();
        httpClientHandler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", "{}");
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => httpClientHandler);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var client = serviceProvider.GetRequiredService<EurekaClient>();

        _ = await client.GetApplicationsAsync(TestContext.Current.CancellationToken);

        httpClientHandler.Mock.VerifyNoOutstandingExpectation();

        string logMessages = string.Join(Environment.NewLine, capturingLoggerProvider.GetAll());
        logMessages.Should().Contain("User-Agent: *");
        logMessages.Should().Contain("Content-Type: *");
    }

    private sealed class TestHttpClientFactory(HttpClient? httpClient) : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient _httpClient = httpClient ?? new HttpClient();

        public TestHttpClientFactory()
            : this(null)
        {
        }

        public HttpClient CreateClient(string name)
        {
            return _httpClient;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
