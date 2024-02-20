// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class DiscoveryClientTest : AbstractBaseTest
{
    private const string FooAddedJson = """
        {
            "applications": {
                "versions__delta":"1",
                "apps__hashcode":"UP_1_",
                "application":[{
                    "name":"FOO",
                    "instance":[{
                        "instanceId":"localhost:foo",
                        "hostName":"localhost",
                        "app":"FOO",
                        "ipAddr":"192.168.56.1",
                        "status":"UP",
                        "overriddenstatus":"UNKNOWN",
                        "port":{"$":8080,"@enabled":"true"},
                        "securePort":{"$":443,"@enabled":"false"},
                        "countryId":1,
                        "dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},
                        "leaseInfo":{"renewalIntervalInSecs":30,"durationInSecs":90,"registrationTimestamp":1457714988223,"lastRenewalTimestamp":1457716158319,"evictionTimestamp":0,"serviceUpTimestamp":1457714988223},
                        "metadata":{"@class":"java.util.Collections$EmptyMap"},
                        "homePageUrl":"http://localhost:8080/",
                        "statusPageUrl":"http://localhost:8080/info",
                        "healthCheckUrl":"http://localhost:8080/health",
                        "vipAddress":"foo",
                        "isCoordinatingDiscoveryServer":"false",
                        "lastUpdatedTimestamp":"1457714988223",
                        "lastDirtyTimestamp":"1457714988172",
                        "actionType":"ADDED"
                    }]
                }]
            }
        }
        """;

    private const string FooModifiedJson = """
        {
            "applications": {
                "versions__delta":"3",
                "apps__hashcode":"UP_1_",
                "application":[{
                    "name":"FOO",
                    "instance":[{
                        "instanceId":"localhost:foo",
                        "hostName":"localhost",
                        "app":"FOO",
                        "ipAddr":"192.168.56.1",
                        "status":"UP",
                        "overriddenstatus":"UNKNOWN",
                        "port":{"$":8080,"@enabled":"true"},
                        "securePort":{"$":443,"@enabled":"false"},
                        "countryId":1,
                        "dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},
                        "leaseInfo":{"renewalIntervalInSecs":30,"durationInSecs":90,"registrationTimestamp":1457714988223,"lastRenewalTimestamp":1457716158319,"evictionTimestamp":0,"serviceUpTimestamp":1457714988223},
                        "metadata":{"@class":"java.util.Collections$EmptyMap"},
                        "homePageUrl":"http://localhost:8080/",
                        "statusPageUrl":"http://localhost:8080/info",
                        "healthCheckUrl":"http://localhost:8080/health",
                        "vipAddress":"foo",
                        "isCoordinatingDiscoveryServer":"false",
                        "lastUpdatedTimestamp":"1457714988223",
                        "lastDirtyTimestamp":"1457714988172",
                        "actionType":"MODIFIED"
                    }]
                }]
            }
        }
        """;

    private volatile int _timerFuncCount;

    [Fact]
    public void Constructor_TimersNotStarted()
    {
        var clientOptions = new EurekaClientOptions
        {
            ShouldRegisterWithEureka = false,
            ShouldFetchRegistry = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>();

        var appManager = new EurekaApplicationInfoManager(instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        IHttpClientFactory httpClientFactory = new TestHttpClientFactory(new HttpClient());

        var eurekaServiceUriStateManager = new EurekaServiceUriStateManager(clientOptionsMonitor, NullLogger<EurekaServiceUriStateManager>.Instance);
        var eurekaClient = new EurekaClient(httpClientFactory, clientOptionsMonitor, eurekaServiceUriStateManager, NullLogger<EurekaClient>.Instance);
        var discoveryClient = new EurekaDiscoveryClient(clientOptionsMonitor, instanceOptionsMonitor, appManager, eurekaClient, NullLoggerFactory.Instance);

        Assert.Null(discoveryClient.CacheRefreshTimer);
        Assert.Null(discoveryClient.HeartbeatTimer);
    }

    [Fact]
    public async Task FetchFullRegistryAsync_InvokesServer_ReturnsValidResponse()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", FooAddedJson);

        await using WebApplication webApplication = builder.Build();

        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();
        Applications applications = await discoveryClient.FetchFullRegistryAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.NotNull(applications);
        Assert.Equal(1, applications.Version);
        Assert.Equal("UP_1_", applications.AppsHashCode);

        IList<Application> registeredApplications = applications.GetRegisteredApplications();
        Assert.NotNull(registeredApplications);
        Assert.Single(registeredApplications);
        Assert.Equal("FOO", registeredApplications[0].Name);
    }

    [Fact]
    public async Task FetchFullRegistryAsync_ReturnsNull_IfFetchCounterMismatch()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", FooAddedJson);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();
        Task<Applications> applicationsTask = discoveryClient.FetchFullRegistryAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        discoveryClient.SetRegistryFetchCounter(100);
        Applications applications = await applicationsTask;
        Assert.Null(applications);
    }

    [Fact]
    public async Task FetchRegistryDeltaAsync_InvokesServer_ReturnsValidResponse()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps/delta").Respond("application/json", FooModifiedJson);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        var instance = new InstanceInfo
        {
            InstanceId = "localhost:foo",
            HostName = "localhost",
            AppName = "FOO",
            IPAddress = "192.168.56.1",
            Status = InstanceStatus.Starting
        };

        var app = new Application("FOO")
        {
            InstanceMap =
            {
                [instance.InstanceId] = instance
            }
        };

        var apps = new Applications();
        apps.Add(app);

        discoveryClient.Applications = apps;

        Applications result = await discoveryClient.FetchRegistryDeltaAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.NotNull(result);
        Assert.Equal(3, result.Version);
        Assert.Equal("UP_1_", result.AppsHashCode);

        IList<Application> registeredApplications = result.GetRegisteredApplications();
        Assert.NotNull(registeredApplications);
        Assert.Single(registeredApplications);
        Assert.Equal("FOO", registeredApplications[0].Name);
        Assert.Single(registeredApplications[0].Instances);
    }

    [Fact]
    public async Task FetchRegistryDeltaAsync_ReturnsNull_IfFetchCounterMismatch()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps/delta").Respond("application/json", FooModifiedJson);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        Task<Applications> applicationsTask = discoveryClient.FetchRegistryDeltaAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        discoveryClient.SetRegistryFetchCounter(100);
        Applications apps = await applicationsTask;
        Assert.Null(apps);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsFalse_WhenNotOKStatusReturned()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.NotFound);

        builder.Services.Replace(ServiceDescriptor.Singleton(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

            var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance)
            {
                InstanceInfo = new InstanceInfo
                {
                    InstanceId = "localhost:foo",
                    HostName = "localhost",
                    AppName = "FOO",
                    IPAddress = "192.168.56.1",
                    Status = InstanceStatus.Starting
                }
            };

            return appManager;
        }));

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        bool result = await discoveryClient.RegisterAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.False(result);
    }

    [Fact]
    public async Task RegisterAsync_InvokesServerReturnsTrue_WhenOKStatusReturned()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.NoContent);

        builder.Services.Replace(ServiceDescriptor.Singleton(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

            var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance)
            {
                InstanceInfo = new InstanceInfo
                {
                    InstanceId = "localhost:foo",
                    HostName = "localhost",
                    AppName = "FOO",
                    IPAddress = "192.168.56.1",
                    Status = InstanceStatus.Starting
                }
            };

            return appManager;
        }));

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        bool result = await discoveryClient.RegisterAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.True(result);
    }

    [Fact]
    public async Task RenewAsync_Registers_When404StatusReturned()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.NotFound);
        handler.Mock.Expect(HttpMethod.Put, "http://localhost:8761/eureka/apps/FOO/localhost:foo").Respond(HttpStatusCode.NotFound);
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.NotFound);

        builder.Services.Replace(ServiceDescriptor.Singleton(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

            var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance)
            {
                InstanceInfo = new InstanceInfo
                {
                    InstanceId = "localhost:foo",
                    HostName = "localhost",
                    AppName = "FOO",
                    IPAddress = "192.168.56.1",
                    Status = InstanceStatus.Starting
                }
            };

            return appManager;
        }));

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        bool result = await discoveryClient.RenewAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.False(result);
    }

    [Fact]
    public async Task RenewAsync_ReturnsTrue_WhenOKStatusReturned()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.OK);
        handler.Mock.Expect(HttpMethod.Put, "http://localhost:8761/eureka/apps/FOO/localhost:foo").Respond("application/json", "{}");

        builder.Services.Replace(ServiceDescriptor.Singleton(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

            var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance)
            {
                InstanceInfo = new InstanceInfo
                {
                    InstanceId = "localhost:foo",
                    HostName = "localhost",
                    AppName = "FOO",
                    IPAddress = "192.168.56.1",
                    Status = InstanceStatus.Starting
                }
            };

            return appManager;
        }));

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        bool result = await discoveryClient.RenewAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.True(result);
    }

    [Fact]
    public async Task UnRegisterAsync_InvokesServerReturnsTrue_WhenOKStatusReturned()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Delete, "http://localhost:8761/eureka/apps/FOO/localhost:foo").Respond(HttpStatusCode.OK);

        builder.Services.Replace(ServiceDescriptor.Singleton(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

            var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance)
            {
                InstanceInfo = new InstanceInfo
                {
                    InstanceId = "localhost:foo",
                    HostName = "localhost",
                    AppName = "FOO",
                    IPAddress = "192.168.56.1",
                    Status = InstanceStatus.Starting
                }
            };

            return appManager;
        }));

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        await discoveryClient.DeregisterAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetInstancesByVipAddress_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        discoveryClient.Applications = new Applications([
            new Application("app1", [
                new InstanceInfo
                {
                    AppName = "app1",
                    InstanceId = "id1",
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.Down
                },
                new InstanceInfo
                {
                    AppName = "app1",
                    InstanceId = "id2",
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.Down
                }
            ]),
            new Application("app2", [
                new InstanceInfo
                {
                    AppName = "app2",
                    InstanceId = "id21",
                    VipAddress = "vapp2",
                    SecureVipAddress = "svapp2",
                    Status = InstanceStatus.Up
                },
                new InstanceInfo
                {
                    AppName = "app2",
                    InstanceId = "id22",
                    VipAddress = "vapp2",
                    SecureVipAddress = "svapp2",
                    Status = InstanceStatus.OutOfService
                }
            ])
        ]);

        IList<InstanceInfo> result = discoveryClient.GetInstancesByVipAddress("vapp1", false);

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
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        discoveryClient.Applications = new Applications([
            new Application("app1", [
                new InstanceInfo
                {
                    AppName = "app1",
                    InstanceId = "id1",
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.Down
                },
                new InstanceInfo
                {
                    AppName = "app1",
                    InstanceId = "id2",
                    VipAddress = "vapp1",
                    SecureVipAddress = "svapp1",
                    Status = InstanceStatus.Down
                }
            ]),
            new Application("app2", [
                new InstanceInfo
                {
                    AppName = "app2",
                    InstanceId = "id1",
                    VipAddress = "vapp2",
                    SecureVipAddress = "svapp2",
                    Status = InstanceStatus.Up
                },
                new InstanceInfo
                {
                    AppName = "app2",
                    InstanceId = "id2",
                    VipAddress = "vapp2",
                    SecureVipAddress = "svapp2",
                    Status = InstanceStatus.OutOfService
                }
            ])
        ]);

        Application result = discoveryClient.GetApplication("app1");

        Assert.NotNull(result);
        Assert.Equal("app1", result.Name);

        result = discoveryClient.GetApplication("boohoo");
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshInstanceInfo_CallsHealthCheckHandler_UpdatesInstanceStatus()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        var myHandler = new TestHealthCheckHandler(InstanceStatus.Down);
        discoveryClient.HealthCheckHandler = myHandler;

        await discoveryClient.RefreshInstanceInfoAsync(CancellationToken.None);

        Assert.True(myHandler.Awaited);
        Assert.Equal(InstanceStatus.Down, discoveryClient.AppInfoManager.InstanceInfo.Status);
    }

    [Fact]
    public async Task StartTimer_StartsTimer()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        _timerFuncCount = 0;
        await using Timer timer = discoveryClient.StartTimer("MyTimer", 10, TimerFunc);
        Assert.NotNull(timer);
        await Task.Delay(1000);
        Assert.True(_timerFuncCount > 0);
    }

    [Fact]
    public async Task StartTimer_StartsTimer_KeepsRunningOnExceptions()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        _timerFuncCount = 0;
        await using Timer timer = discoveryClient.StartTimer("MyTimer", 10, TimerFuncThrows);
        Assert.NotNull(timer);
        await Task.Delay(1000);
        Assert.True(_timerFuncCount >= 1);
    }

    [Fact]
    public async Task StartTimer_StartsTimer_StopsAfterDispose()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        await using WebApplication webApplication = builder.Build();

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        _timerFuncCount = 0;

        await using (Timer timer = discoveryClient.StartTimer("MyTimer", 10, TimerFuncThrows))
        {
            Assert.NotNull(timer);
            await Task.Delay(1000);
            Assert.True(_timerFuncCount >= 1);
        }

        int currentCount = _timerFuncCount;
        await Task.Delay(1000);
        Assert.Equal(currentCount, _timerFuncCount);
    }

    [Fact]
    public async Task ApplicationEventsFireOnChangeDuringFetch()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", FooAddedJson);
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps/delta").Respond("application/json", FooModifiedJson);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        int eventCount = 0;

        discoveryClient.OnApplicationsChange += (_, _) =>
        {
            eventCount++;
        };

        await discoveryClient.FetchFullRegistryAsync(CancellationToken.None);
        Assert.Equal(1, eventCount);

        await discoveryClient.FetchRegistryDeltaAsync(CancellationToken.None);
        Assert.Equal(2, eventCount);

        handler.Mock.VerifyNoOutstandingExpectation();
    }


    [Fact]
    public async Task Can_manipulate_request_headers()
    {
        var extraHeadersHandler = new ExtraRequestHeadersDelegatingHandler();
        extraHeadersHandler.ExtraRequestHeaders.Add("X-Special-Feature", "enabled");

        var appSettings = new Dictionary<string, string>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());
        builder.Services.AddTransient(_ => extraHeadersHandler);

        builder.Services.Configure<HttpClientFactoryOptions>("Eureka", options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                handlerBuilder.AdditionalHandlers.Add(handlerBuilder.Services.GetRequiredService<ExtraRequestHeadersDelegatingHandler>()));
        });

        var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").WithHeaders("X-Special-Feature", "enabled")
            .Respond(HttpStatusCode.NoContent);

        builder.Services.Replace(ServiceDescriptor.Singleton(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

            var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance)
            {
                InstanceInfo = new InstanceInfo
                {
                    InstanceId = "localhost:foo",
                    HostName = "localhost",
                    AppName = "FOO",
                    IPAddress = "192.168.56.1",
                    Status = InstanceStatus.Starting
                }
            };

            return appManager;
        }));

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        bool result = await discoveryClient.RegisterAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.True(result);
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

    private void TimerFunc()
    {
        Interlocked.Increment(ref _timerFuncCount);
    }

    private void TimerFuncThrows()
    {
        Interlocked.Increment(ref _timerFuncCount);
        throw new FormatException();
    }

    private sealed class TestHealthCheckHandler : IHealthCheckHandler
    {
        private readonly InstanceStatus _status;

        public bool Awaited { get; private set; }

        public TestHealthCheckHandler(InstanceStatus status)
        {
            _status = status;
            Awaited = false;
        }

        public async Task<InstanceStatus> GetStatusAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            Awaited = true;
            return _status;
        }
    }
}
