// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaDiscoveryClientTest
{
    private const string FooAddedJson = """
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
                    "overriddenStatus": "UNKNOWN",
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
                      "lastRenewalTimestamp": 1457716158319,
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

    private const string FooModifiedJson = """
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
                    "overriddenStatus": "UNKNOWN",
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
                      "lastRenewalTimestamp": 1457716158319,
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
    public async Task Constructor_Initializes_Correctly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:AppName"] = "demo"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication webApplication = builder.Build();

        EurekaDiscoveryClient discoveryClient = webApplication.Services.GetServices<IDiscoveryClient>().OfType<EurekaDiscoveryClient>().Single();

        Assert.NotNull(discoveryClient.Description);

        ISet<string> serviceIds = await discoveryClient.GetServiceIdsAsync(CancellationToken.None);
        Assert.Empty(serviceIds);

        IServiceInstance thisService = discoveryClient.GetLocalServiceInstance();
        Assert.NotNull(thisService);

        var instanceOptionsMonitor = webApplication.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = instanceOptionsMonitor.CurrentValue;

        Assert.Equal(instanceOptions.HostName, thisService.Host);
        Assert.Equal(instanceOptions.IsSecurePortEnabled, thisService.IsSecure);
        Assert.NotNull(thisService.Metadata);
        Assert.Equal(5000, thisService.Port);
        Assert.Equal("DEMO", thisService.ServiceId);
        Assert.NotNull(thisService.Uri);

        string scheme = instanceOptions.IsSecurePortEnabled ? "https" : "http";
        var uri = new Uri($"{scheme}://{instanceOptions.HostName}:{instanceOptions.NonSecurePort}");

        Assert.Equal(uri, thisService.Uri);
    }

    [Fact]
    public async Task Constructor_TimersNotStarted()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication webApplication = builder.Build();

        EurekaDiscoveryClient[] discoveryClients = webApplication.Services.GetServices<IDiscoveryClient>().OfType<EurekaDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);

        Assert.False(discoveryClients[0].IsCacheRefreshTimerStarted);
        Assert.False(discoveryClients[0].IsHeartbeatTimerStarted);
    }

    [Fact]
    public async Task FetchFullRegistryAsync_InvokesServer_ReturnsValidResponse()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", FooAddedJson);

        await using WebApplication webApplication = builder.Build();

        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();
        ApplicationInfoCollection apps = await discoveryClient.FetchFullRegistryAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.NotNull(apps);
        Assert.Equal(1, apps.Version);
        Assert.Equal("UP_1_", apps.AppsHashCode);

        IReadOnlyList<ApplicationInfo> registeredApplications = apps.RegisteredApplications;
        Assert.NotNull(registeredApplications);
        Assert.Single(registeredApplications);
        Assert.Equal("FOO", registeredApplications[0].Name);
    }

    [Fact]
    public async Task FetchRegistryDeltaAsync_InvokesServer_ReturnsValidResponse()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps/delta").Respond("application/json", FooModifiedJson);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        var instance = new InstanceInfo("localhost:foo", "FOO", "localhost", "192.168.56.1", new DataCenterInfo())
        {
            Status = InstanceStatus.Starting
        };

        var app = new ApplicationInfo("FOO", [instance]);
        var apps = new ApplicationInfoCollection([app]);

        discoveryClient.Applications = apps;

        ApplicationInfoCollection result = await discoveryClient.FetchRegistryDeltaAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.NotNull(result);
        Assert.Equal(3, result.Version);
        Assert.Equal("UP_1_", result.AppsHashCode);

        IReadOnlyList<ApplicationInfo> registeredApplications = result.RegisteredApplications;
        Assert.NotNull(registeredApplications);
        Assert.Single(registeredApplications);
        Assert.Equal("FOO", registeredApplications[0].Name);
        Assert.Single(registeredApplications[0].Instances);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenNotOKStatusReturned()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:AppName"] = "FOO"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.NotFound);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        Func<Task> action = async () => await discoveryClient.RegisterAsync(false, CancellationToken.None);

        await action.Should().ThrowExactlyAsync<EurekaTransportException>();

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RegisterAsync_Succeeds_WhenOKStatusReturned()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:AppName"] = "FOO"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.NoContent);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        await discoveryClient.RegisterAsync(false, CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RenewAsync_RegistersAgain_When404StatusFromHeartbeatReturned()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:AppName"] = "FOO",
            ["Eureka:Instance:InstanceId"] = "localhost:foo"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.OK);
        handler.Mock.Expect(HttpMethod.Put, "http://localhost:8761/eureka/apps/FOO/localhost%3Afoo").Respond(HttpStatusCode.NotFound);
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.OK);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        var appManager = webApplication.Services.GetRequiredService<EurekaApplicationInfoManager>();
        appManager.Instance.IsDirty = true;

        await discoveryClient.RenewAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RenewAsync_Succeeds_WhenOKStatusReturned()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:AppName"] = "FOO",
            ["Eureka:Instance:InstanceId"] = "localhost:foo"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.OK);
        handler.Mock.Expect(HttpMethod.Put, "http://localhost:8761/eureka/apps/FOO/localhost%3Afoo").Respond("application/json", "{}");

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        var appManager = webApplication.Services.GetRequiredService<EurekaApplicationInfoManager>();
        appManager.Instance.IsDirty = true;

        await discoveryClient.RenewAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UnRegisterAsync_Succeeds_WhenOKStatusReturned()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:AppName"] = "FOO",
            ["Eureka:Instance:InstanceId"] = "localhost:foo"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Delete, "http://localhost:8761/eureka/apps/FOO/localhost%3Afoo").Respond(HttpStatusCode.OK);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        await discoveryClient.DeregisterAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetInstancesByVipAddress_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        discoveryClient.Applications = new ApplicationInfoCollection([
            new ApplicationInfo("app1", [
                new InstanceInfo("id1", "app1", "localhost", "192.168.56.1", new DataCenterInfo())
                {
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.Down
                },
                new InstanceInfo("id2", "app1", "localhost", "192.168.56.1", new DataCenterInfo())
                {
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.Down
                }
            ]),
            new ApplicationInfo("app2", [
                new InstanceInfo("id21", "app2", "localhost", "192.168.56.1", new DataCenterInfo())
                {
                    VipAddress = "vapp2",
                    SecureVipAddress = "svapp2",
                    Status = InstanceStatus.Up
                },
                new InstanceInfo("id22", "app2", "localhost", "192.168.56.1", new DataCenterInfo())
                {
                    VipAddress = "vapp2",
                    SecureVipAddress = "svapp2",
                    Status = InstanceStatus.OutOfService
                }
            ])
        ]);

        IReadOnlyList<InstanceInfo> result = discoveryClient.GetInstancesByVipAddress("vapp1", false);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].InstanceId is "id1" or "id2");
        Assert.True(result[1].InstanceId is "id1" or "id2");

        result = discoveryClient.GetInstancesByVipAddress("boohoo", false);

        Assert.NotNull(result);
        Assert.Empty(result);

        discoveryClient.Applications.ReturnUpInstancesOnly = true;
        result = discoveryClient.GetInstancesByVipAddress("vapp1", false);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplication_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        discoveryClient.Applications = new ApplicationInfoCollection([
            new ApplicationInfo("app1", [
                new InstanceInfo("id1", "app1", "localhost", "192.168.56.1", new DataCenterInfo())
                {
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.Down
                },
                new InstanceInfo("id2", "app1", "localhost", "192.168.56.1", new DataCenterInfo())
                {
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.Down
                }
            ]),
            new ApplicationInfo("app2", [
                new InstanceInfo("id1", "app2", "localhost", "192.168.56.1", new DataCenterInfo())
                {
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.Up
                },
                new InstanceInfo("id2", "app2", "localhost", "192.168.56.1", new DataCenterInfo())
                {
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.OutOfService
                }
            ])
        ]);

        ApplicationInfo? result = discoveryClient.GetApplication("app1");

        Assert.NotNull(result);
        Assert.Equal("app1", result.Name);

        result = discoveryClient.GetApplication("boohoo");
        Assert.Null(result);
    }

    [Fact]
    public async Task RunHealthChecksAsync_CallsHealthCheckHandler_UpdatesInstanceStatus()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Client:Health:CheckEnabled"] = "true"
        };

        var myHandler = new TestHealthCheckHandler(InstanceStatus.Down);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IHealthCheckHandler>(myHandler);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();
        var appInfoManager = webApplication.Services.GetRequiredService<EurekaApplicationInfoManager>();

        await discoveryClient.RunHealthChecksAsync(CancellationToken.None);

        Assert.True(myHandler.Awaited);
        Assert.Equal(InstanceStatus.Down, appInfoManager.Instance.Status);
    }

    [Fact]
    public async Task RunHealthChecksAsync_SkipsHealthCheckHandler_WhenInStartingState()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Client:Health:CheckEnabled"] = "true",
            ["Eureka:Instance:InstanceEnabledOnInit"] = "false"
        };

        var myHandler = new TestHealthCheckHandler(InstanceStatus.Down);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IHealthCheckHandler>(myHandler);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();
        var appInfoManager = webApplication.Services.GetRequiredService<EurekaApplicationInfoManager>();

        await discoveryClient.RunHealthChecksAsync(CancellationToken.None);

        Assert.False(myHandler.Awaited);
        Assert.Equal(InstanceStatus.Starting, appInfoManager.Instance.Status);
    }

    [Fact]
    public async Task Can_manipulate_request_headers()
    {
        var extraHeadersHandler = new ExtraRequestHeadersDelegatingHandler();
        extraHeadersHandler.ExtraRequestHeaders.Add("X-Special-Feature", "enabled");

        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:AppName"] = "FOO"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();
        builder.Services.AddTransient(_ => extraHeadersHandler);

        builder.Services.Configure<HttpClientFactoryOptions>("Eureka", options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                handlerBuilder.AdditionalHandlers.Add(handlerBuilder.Services.GetRequiredService<ExtraRequestHeadersDelegatingHandler>()));
        });

        var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").WithHeaders("X-Special-Feature", "enabled")
            .Respond(HttpStatusCode.NoContent);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        await discoveryClient.RegisterAsync(false, CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ApplicationEventsFireOnChangeDuringFetch()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", FooAddedJson);
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps/delta").Respond("application/json", FooModifiedJson);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();
        int eventCount = 0;

        discoveryClient.ApplicationsFetched += (_, _) => eventCount++;

        await discoveryClient.FetchRegistryAsync(true, CancellationToken.None);
        await Task.Delay(500);
        Assert.Equal(1, eventCount);

        await discoveryClient.FetchRegistryAsync(false, CancellationToken.None);
        await Task.Delay(500);
        Assert.Equal(2, eventCount);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    private sealed class ExtraRequestHeadersDelegatingHandler : DelegatingHandler
    {
        public IDictionary<string, string> ExtraRequestHeaders { get; } = new Dictionary<string, string>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SetHeaders(request);
            return base.SendAsync(request, cancellationToken);
        }

        private void SetHeaders(HttpRequestMessage request)
        {
            foreach ((string name, string value) in ExtraRequestHeaders)
            {
                request.Headers.Add(name, value);
            }
        }
    }

    private sealed class TestHealthCheckHandler(InstanceStatus status) : IHealthCheckHandler
    {
        private readonly InstanceStatus _status = status;

        public bool Awaited { get; private set; }

        public async Task<InstanceStatus> GetStatusAsync(bool hasFirstHeartbeatCompleted, CancellationToken cancellationToken)
        {
            await Task.Yield();

            Awaited = true;
            return _status;
        }
    }
}
