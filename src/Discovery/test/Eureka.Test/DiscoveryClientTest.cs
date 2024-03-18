// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
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
        var client = new DiscoveryClient(clientOptionsMonitor);

        Assert.Null(client.CacheRefreshTimer);
        Assert.Null(client.HeartBeatTimer);
    }

    [Fact]
    public async Task FetchFullRegistryAsync_InvokesServer_ReturnsValidResponse()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = FooAddedJson;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);
        var client = new DiscoveryClient(clientOptionsMonitor, httpClient);
        Applications result = await client.FetchFullRegistryAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Version);
        Assert.Equal("UP_1_", result.AppsHashCode);

        IList<Application> apps = result.GetRegisteredApplications();
        Assert.NotNull(apps);
        Assert.Single(apps);
        Assert.Equal("FOO", apps[0].Name);
    }

    [Fact]
    public async Task FetchFullRegistryAsync_ReturnsNull_IfFetchCounterMismatch()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);
        var client = new DiscoveryClient(clientOptionsMonitor, httpClient);

        Task<Applications> result = client.FetchFullRegistryAsync(CancellationToken.None);
        client.RegistryFetchCounter = 100;
        Applications apps = await result;
        Assert.Null(apps);
    }

    [Fact]
    public async Task FetchRegistryDeltaAsync_InvokesServer_ReturnsValidResponse()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = FooModifiedJson;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);
        var client = new DiscoveryClient(clientOptionsMonitor, httpClient);
        var apps = new Applications();
        var app = new Application("FOO");

        var instance = new InstanceInfo
        {
            InstanceId = "localhost:foo",
            HostName = "localhost",
            AppName = "FOO",
            IPAddress = "192.168.56.1",
            Status = InstanceStatus.Starting
        };

        app.InstanceMap[instance.InstanceId] = instance;
        apps.Add(app);
        client.Applications = apps;

        Applications result = await client.FetchRegistryDeltaAsync(CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(3, result.Version);
        Assert.Equal("UP_1_", result.AppsHashCode);

        IList<Application> reg = result.GetRegisteredApplications();
        Assert.NotNull(reg);
        Assert.Single(reg);
        Assert.Equal("FOO", reg[0].Name);
        Assert.Single(reg[0].Instances);
    }

    [Fact]
    public async Task FetchRegistryDeltaAsync_ReturnsNull_IfFetchCounterMismatch()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);
        var client = new DiscoveryClient(clientOptionsMonitor, httpClient);

        Task<Applications> result = client.FetchRegistryDeltaAsync(CancellationToken.None);

        client.RegistryFetchCounter = 100;
        Applications apps = await result;
        Assert.Null(apps);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsFalse_WhenNotOKStatusReturned()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 404;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        var instance = new InstanceInfo
        {
            InstanceId = "localhost:foo",
            HostName = "localhost",
            AppName = "FOO",
            IPAddress = "192.168.56.1",
            Status = InstanceStatus.Starting
        };

        EurekaApplicationInfoManager.SharedInstance.InstanceInfo = instance;

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);
        var client = new DiscoveryClient(clientOptionsMonitor, httpClient);

        bool result = await client.RegisterAsync(CancellationToken.None);

        Assert.False(result);

        // Verify Register done
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal("POST", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/FOO", TestConfigServerStartup.LastRequest.Path.Value);
    }

    [Fact]
    public async Task RegisterAsync_InvokesServerReturnsTrue_WhenOKStatusReturned()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 204;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        var instance = new InstanceInfo
        {
            InstanceId = "localhost:foo",
            HostName = "localhost",
            AppName = "FOO",
            IPAddress = "192.168.56.1",
            Status = InstanceStatus.Starting
        };

        EurekaApplicationInfoManager.SharedInstance.InstanceInfo = instance;

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);
        var client = new DiscoveryClient(clientOptionsMonitor, httpClient);

        bool result = await client.RegisterAsync(CancellationToken.None);

        Assert.True(result);

        // Verify Register done
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal("POST", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/FOO", TestConfigServerStartup.LastRequest.Path.Value);
    }

    [Fact]
    public async Task RenewAsync_Registers_When404StatusReturned()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 404;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        var instance = new InstanceInfo
        {
            InstanceId = "localhost:foo",
            HostName = "localhost",
            AppName = "FOO",
            IPAddress = "192.168.56.1",
            Status = InstanceStatus.Starting
        };

        EurekaApplicationInfoManager.SharedInstance.InstanceInfo = instance;

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);
        var client = new DiscoveryClient(clientOptionsMonitor, httpClient);

        bool result = await client.RenewAsync(CancellationToken.None);

        // Verify Register done
        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal("POST", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/FOO", TestConfigServerStartup.LastRequest.Path.Value);

        // Still false as register returns 404 still
        Assert.False(result);
    }

    [Fact]
    public async Task RenewAsync_ReturnsTrue_WhenOKStatusReturned()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        var instance = new InstanceInfo
        {
            InstanceId = "localhost:foo",
            HostName = "localhost",
            AppName = "FOO",
            IPAddress = "192.168.56.1",
            Status = InstanceStatus.Starting
        };

        EurekaApplicationInfoManager.SharedInstance.InstanceInfo = instance;

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);
        var client = new DiscoveryClient(clientOptionsMonitor, httpClient);

        bool result = await client.RenewAsync(CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task UnRegisterAsync_InvokesServerReturnsTrue_WhenOKStatusReturned()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        var instance = new InstanceInfo
        {
            InstanceId = "localhost:foo",
            HostName = "localhost",
            AppName = "FOO",
            IPAddress = "192.168.56.1",
            Status = InstanceStatus.Starting
        };

        EurekaApplicationInfoManager.SharedInstance.InstanceInfo = instance;

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);
        var client = new DiscoveryClient(clientOptionsMonitor, httpClient);

        bool result = await client.UnregisterAsync(CancellationToken.None);

        Assert.True(result);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal("DELETE", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/FOO/localhost:foo", TestConfigServerStartup.LastRequest.Path.Value);
    }

    [Fact]
    public void GetInstancesByVipAddress_ReturnsExpected()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id21",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Up
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id22",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.OutOfService
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);

        var client = new DiscoveryClient(clientOptionsMonitor)
        {
            Applications = apps
        };

        IList<InstanceInfo> result = client.GetInstancesByVipAddress("vapp1", false);
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].InstanceId == "id1" || result[0].InstanceId == "id2");
        Assert.True(result[1].InstanceId == "id1" || result[1].InstanceId == "id2");

        result = client.GetInstancesByVipAddress("boohoo", false);
        Assert.NotNull(result);
        Assert.Empty(result);

        apps.ReturnUpInstancesOnly = true;
        result = client.GetInstancesByVipAddress("vapp1", false);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetNextServerFromEureka_ReturnsExpected()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id21",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Up
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id22",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.OutOfService
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);

        var client = new DiscoveryClient(clientOptionsMonitor)
        {
            Applications = apps
        };

        InstanceInfo result = client.GetNextServerFromEureka("vapp1", false);
        Assert.NotNull(result);
        Assert.True(result.InstanceId == "id1" || result.InstanceId == "id2");

        result = client.GetNextServerFromEureka("boohoo", false);
        Assert.Null(result);

        apps.ReturnUpInstancesOnly = true;
        result = client.GetNextServerFromEureka("vapp1", false);
        Assert.Null(result);
    }

    [Fact]
    public void GetInstancesById_Returns_EmptyListWhenNoApps()
    {
        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var client = new DiscoveryClient(clientOptionsMonitor);

        IList<InstanceInfo> result = client.GetInstanceById("myId");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetInstanceById_ReturnsExpected()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Up
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.OutOfService
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);

        var client = new DiscoveryClient(clientOptionsMonitor)
        {
            Applications = apps
        };

        IList<InstanceInfo> result = client.GetInstanceById("id1");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].InstanceId == "id1" && result[0].InstanceId == "id1");
        Assert.True(result[0].AppName == "app1" || result[0].AppName == "app2");
        Assert.True(result[1].AppName == "app1" || result[1].AppName == "app2");

        result = client.GetInstanceById("boohoo");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetApplication_ReturnsExpected()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Up
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.OutOfService
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);

        var client = new DiscoveryClient(clientOptionsMonitor)
        {
            Applications = apps
        };

        Application result = client.GetApplication("app1");
        Assert.NotNull(result);
        Assert.Equal("app1", result.Name);

        result = client.GetApplication("boohoo");
        Assert.Null(result);
    }

    [Fact]
    public void GetInstancesByVipAddressAndAppName_Throws_WhenAddressAndAppNameNull()
    {
        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var client = new DiscoveryClient(clientOptionsMonitor);

        var ex = Assert.Throws<ArgumentException>(() => client.GetInstancesByVipAddressAndAppName(null, null, false));
        Assert.Contains("appName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshInstanceInfo_CallsHealthCheckHandler_UpdatesInstanceStatus()
    {
        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>();
        EurekaApplicationInfoManager.SharedInstance.Initialize(instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var client = new DiscoveryClient(clientOptionsMonitor);

        var myHandler = new TestHealthCheckHandler(InstanceStatus.Down);
        client.HealthCheckHandler = myHandler;

        await client.RefreshInstanceInfoAsync(CancellationToken.None);

        Assert.True(myHandler.Awaited);
        Assert.Equal(InstanceStatus.Down, EurekaApplicationInfoManager.SharedInstance.InstanceInfo.Status);
    }

    [Fact]
    public async Task StartTimer_StartsTimer()
    {
        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var client = new DiscoveryClient(clientOptionsMonitor);

        _timerFuncCount = 0;
        Timer result = client.StartTimer("MyTimer", 10, TimerFunc);
        Assert.NotNull(result);
        await Task.Delay(1000);
        Assert.True(_timerFuncCount > 0);
        await result.DisposeAsync();
    }

    [Fact]
    public async Task StartTimer_StartsTimer_KeepsRunningOnExceptions()
    {
        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var client = new DiscoveryClient(clientOptionsMonitor);

        _timerFuncCount = 0;
        Timer result = client.StartTimer("MyTimer", 10, TimerFuncThrows);
        Assert.NotNull(result);
        await Task.Delay(1000);
        Assert.True(_timerFuncCount >= 1);
        await result.DisposeAsync();
    }

    [Fact]
    public async Task StartTimer_StartsTimer_StopsAfterDispose()
    {
        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var client = new DiscoveryClient(clientOptionsMonitor);

        _timerFuncCount = 0;
        Timer result = client.StartTimer("MyTimer", 10, TimerFuncThrows);
        Assert.NotNull(result);
        await Task.Delay(1000);
        Assert.True(_timerFuncCount >= 1);
        await result.DisposeAsync();
        int currentCount = _timerFuncCount;
        await Task.Delay(1000);
        Assert.Equal(currentCount, _timerFuncCount);
    }

    [Fact]
    public async Task ApplicationEventsFireOnChangeDuringFetch()
    {
        int eventCount = 0;
        TestConfigServerStartup.Response = FooAddedJson;
        TestConfigServerStartup.ReturnStatus = 200;

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>()
            .UseEnvironment(HostingHelpers.GetHostingEnvironment().EnvironmentName);

        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientOptions = new EurekaClientOptions
        {
            ShouldFetchRegistry = false,
            ShouldRegisterWithEureka = false,
            EurekaServerServiceUrls = uri
        };

        TestOptionsMonitor<EurekaClientOptions> clientOptionsMonitor = TestOptionsMonitor.Create(clientOptions);
        var httpClientFactory = new TestHttpClientFactory(server.CreateClient());
        var httpClient = new EurekaHttpClient(clientOptionsMonitor, httpClientFactory, NullLoggerFactory.Instance);

        var client = new DiscoveryClient(clientOptionsMonitor, httpClient)
        {
            Applications = new Applications()
        };

        client.OnApplicationsChange += (_, _) =>
        {
            eventCount++;
        };

        await client.FetchFullRegistryAsync(CancellationToken.None);
        Assert.Equal(1, eventCount);

        TestConfigServerStartup.Response = FooModifiedJson;

        await client.FetchRegistryDeltaAsync(CancellationToken.None);
        Assert.Equal(2, eventCount);
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

        public bool Awaited { get; set; }

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
