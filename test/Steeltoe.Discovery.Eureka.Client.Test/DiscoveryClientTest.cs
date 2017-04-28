//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.TestHost;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Client.Test;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class DiscoveryClientTest : AbstractBaseTest
    {

        [Fact]
        public void Constructor_Throws_IfInstanceConfigNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new DiscoveryClient(null));
            Assert.Contains("clientConfig", ex.Message);
        }

        [Fact]
        public void Constructor_TimersNotStarted()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldRegisterWithEureka = false,
                ShouldFetchRegistry = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            Assert.Null(client.CacheRefreshTimer);
            Assert.Null(client.HeartBeatTimer);
        }

        [Fact]
        public async void FetchFullRegistryAsync_InvokesServer_ReturnsValidResponse()
        {
            var json = @"{ 
'applications': { 
    'versions__delta':'1',
    'apps__hashcode':'UP_1_',
    'application':[
        {
        'name':'FOO',
        'instance':[
            { 
            'instanceId':'localhost:foo',
            'hostName':'localhost',
            'app':'FOO',
            'ipAddr':'192.168.56.1',
            'status':'UP',
            'overriddenstatus':'UNKNOWN',
            'port':{'$':8080,'@enabled':'true'},
            'securePort':{'$':443,'@enabled':'false'},
            'countryId':1,
            'dataCenterInfo':{'@class':'com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo','name':'MyOwn'},
            'leaseInfo':{'renewalIntervalInSecs':30,'durationInSecs':90,'registrationTimestamp':1457714988223,'lastRenewalTimestamp':1457716158319,'evictionTimestamp':0,'serviceUpTimestamp':1457714988223},
            'metadata':{'@class':'java.util.Collections$EmptyMap'},
            'homePageUrl':'http://localhost:8080/',
            'statusPageUrl':'http://localhost:8080/info',
            'healthCheckUrl':'http://localhost:8080/health',
            'vipAddress':'foo',
            'isCoordinatingDiscoveryServer':'false',
            'lastUpdatedTimestamp':'1457714988223',
            'lastDirtyTimestamp':'1457714988172',
            'actionType':'ADDED'
            }]
        }]
    }
}";

            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = json;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = uri.ToString()
            };

            var httpClient = new EurekaHttpClient(config, server.CreateClient());
            DiscoveryClient client = new DiscoveryClient(config, httpClient);
            var result = await client.FetchFullRegistryAsync();
            Assert.NotNull(result);
            Assert.Equal(1, result.Version);
            Assert.Equal("UP_1_", result.AppsHashCode);

            var apps = result.GetRegisteredApplications();
            Assert.NotNull(apps);
            Assert.Equal(1, apps.Count);
            Assert.Equal("FOO", apps[0].Name);
        }

        [Fact]
        public void FetchFullRegistryAsync_ReturnsNull_IfFetchCounterMismatch()
        {
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = "";
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = uri.ToString()
            };

            var httpClient = new EurekaHttpClient(config, server.CreateClient());
            DiscoveryClient client = new DiscoveryClient(config, httpClient);
            var result = client.FetchFullRegistryAsync();
            client.RegistryFetchCounter = 100;
            var apps = result.GetAwaiter().GetResult();
            Assert.Null(apps);
        }
        [Fact]
        public async void FetchRegistryDeltaAsync_InvokesServer_ReturnsValidResponse()
        {
            var json = @"{ 
'applications': { 
    'versions__delta':'3',
    'apps__hashcode':'UP_1_',
    'application':[
        {
        'name':'FOO',
        'instance':[
            { 
            'instanceId':'localhost:foo',
            'hostName':'localhost',
            'app':'FOO',
            'ipAddr':'192.168.56.1',
            'status':'UP',
            'overriddenstatus':'UNKNOWN',
            'port':{'$':8080,'@enabled':'true'},
            'securePort':{'$':443,'@enabled':'false'},
            'countryId':1,
            'dataCenterInfo':{'@class':'com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo','name':'MyOwn'},
            'leaseInfo':{'renewalIntervalInSecs':30,'durationInSecs':90,'registrationTimestamp':1457714988223,'lastRenewalTimestamp':1457716158319,'evictionTimestamp':0,'serviceUpTimestamp':1457714988223},
            'metadata':{'@class':'java.util.Collections$EmptyMap'},
            'homePageUrl':'http://localhost:8080/',
            'statusPageUrl':'http://localhost:8080/info',
            'healthCheckUrl':'http://localhost:8080/health',
            'vipAddress':'foo',
            'isCoordinatingDiscoveryServer':'false',
            'lastUpdatedTimestamp':'1457714988223',
            'lastDirtyTimestamp':'1457714988172',
            'actionType':'MODIFIED'
            }]
        }]
    }
}";
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = json;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = uri.ToString()
            };

            var httpClient = new EurekaHttpClient(config, server.CreateClient());
            DiscoveryClient client = new DiscoveryClient(config, httpClient);
            Applications apps = new Applications();
            Application app = new Application("FOO");
            InstanceInfo inst = new InstanceInfo()
            {
                InstanceId = "localhost:foo",
                HostName = "localhost",
                AppName = "FOO",
                IpAddr = "192.168.56.1",
                Status = InstanceStatus.STARTING
            };
            app.InstanceMap[inst.InstanceId] = inst;
            apps.Add(app);
            client.Applications = apps;

            var result = await client.FetchRegistryDeltaAsync();
            Assert.NotNull(result);
            Assert.Equal(3, result.Version);
            Assert.Equal("UP_1_", result.AppsHashCode);

            var reg = result.GetRegisteredApplications();
            Assert.NotNull(reg);
            Assert.Equal(1, reg.Count);
            Assert.Equal("FOO", reg[0].Name);
            Assert.Equal(1, reg[0].Instances.Count);
        }

        [Fact]
        public void FetchRegistryDeltaAsync_ReturnsNull_IfFetchCounterMismatch()
        {
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = "";
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);


            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = uri.ToString()
            };

            var httpClient = new EurekaHttpClient(config, server.CreateClient());
            DiscoveryClient client = new DiscoveryClient(config, httpClient);
            var result = client.FetchRegistryDeltaAsync();
            client.RegistryFetchCounter = 100;
            var apps = result.GetAwaiter().GetResult();
            Assert.Null(apps);
        }

        [Fact]
        public async void RegisterAsync_ReturnsFalse_WhenNotOKStatusReturned()
        {
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = "";
            TestConfigServerStartup.ReturnStatus = 404;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = uri.ToString()
            };
            InstanceInfo inst = new InstanceInfo()
            {
                InstanceId = "localhost:foo",
                HostName = "localhost",
                AppName = "FOO",
                IpAddr = "192.168.56.1",
                Status = InstanceStatus.STARTING
            };
            ApplicationInfoManager.Instance.InstanceInfo = inst;

            var httpClient = new EurekaHttpClient(config, server.CreateClient());
            DiscoveryClient client = new DiscoveryClient(config, httpClient);
            var result = await client.RegisterAsync();
            Assert.False(result);

            // Verify Register done
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("POST", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/FOO", TestConfigServerStartup.LastRequest.Path.Value);
        }
        [Fact]
        public async void RegisterAsync_InvokesServerReturnsTrue_WhenOKStatusReturned()
        {
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = "";
            TestConfigServerStartup.ReturnStatus = 204;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = uri.ToString()

            };

            InstanceInfo inst = new InstanceInfo()
            {
                InstanceId = "localhost:foo",
                HostName = "localhost",
                AppName = "FOO",
                IpAddr = "192.168.56.1",
                Status = InstanceStatus.STARTING
            };
            ApplicationInfoManager.Instance.InstanceInfo = inst;

            var httpClient = new EurekaHttpClient(config, server.CreateClient());
            DiscoveryClient client = new DiscoveryClient(config, httpClient);
            var result = await client.RegisterAsync();
            Assert.True(result);

            // Verify Register done
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("POST", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/FOO", TestConfigServerStartup.LastRequest.Path.Value);
        }
        [Fact]
        public async void RenewAsync_Registers_When404StatusReturned()
        {
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = "";
            TestConfigServerStartup.ReturnStatus = 404;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = uri.ToString()
            };
            InstanceInfo inst = new InstanceInfo()
            {
                InstanceId = "localhost:foo",
                HostName = "localhost",
                AppName = "FOO",
                IpAddr = "192.168.56.1",
                Status = InstanceStatus.STARTING
            };
            ApplicationInfoManager.Instance.InstanceInfo = inst;

            var httpClient = new EurekaHttpClient(config, server.CreateClient());
            DiscoveryClient client = new DiscoveryClient(config, httpClient);
            var result = await client.RenewAsync();

            // Verify Register done
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("POST", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/FOO", TestConfigServerStartup.LastRequest.Path.Value);

            // Still false as register returns 404 still
            Assert.False(result);
        }
        [Fact]
        public async void RenewAsync_ReturnsTrue_WhenOKStatusReturned()
        {
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = "";
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = uri.ToString()
            };

            InstanceInfo inst = new InstanceInfo()
            {
                InstanceId = "localhost:foo",
                HostName = "localhost",
                AppName = "FOO",
                IpAddr = "192.168.56.1",
                Status = InstanceStatus.STARTING
            };
            ApplicationInfoManager.Instance.InstanceInfo = inst;

            var httpClient = new EurekaHttpClient(config, server.CreateClient());
            DiscoveryClient client = new DiscoveryClient(config, httpClient);
            var result = await client.RenewAsync();
            Assert.True(result);
        }

        [Fact]
        public async void UnRegisterAsync_InvokesServerReturnsTrue_WhenOKStatusReturned()
        {
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = "";
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = uri.ToString()
            };

            InstanceInfo inst = new InstanceInfo()
            {
                InstanceId = "localhost:foo",
                HostName = "localhost",
                AppName = "FOO",
                IpAddr = "192.168.56.1",
                Status = InstanceStatus.STARTING
            };
            ApplicationInfoManager.Instance.InstanceInfo = inst;

            var httpClient = new EurekaHttpClient(config, server.CreateClient());
            DiscoveryClient client = new DiscoveryClient(config, httpClient);
            var result = await client.UnregisterAsync();
            Assert.True(result);

            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("DELETE", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/FOO/localhost:foo", TestConfigServerStartup.LastRequest.Path.Value);

        }

        [Fact]
        public void GetNextServerFromEureka_Throws_WhenVIPAddressNull()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            var ex = Assert.Throws<ArgumentException>(() => client.GetNextServerFromEureka(null, false));
            Assert.Contains("vipAddress", ex.Message);
        }

        [Fact]
        public void GetInstancesByVipAddress_Throws_WhenVIPAddressNull()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            var ex = Assert.Throws<ArgumentException>(() => client.GetInstancesByVipAddress(null, false));
            Assert.Contains("vipAddress", ex.Message);
        }

        [Fact]
        public void GetInstancesByVipAddress_ReturnsExpected()
        {

            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id21", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.UP });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id22", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.OUT_OF_SERVICE });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            client.Applications = apps;

            var result = client.GetInstancesByVipAddress("vapp1", false);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.True(result[0].InstanceId.Equals("id1") || result[0].InstanceId.Equals("id2"));
            Assert.True(result[1].InstanceId.Equals("id1") || result[1].InstanceId.Equals("id2"));

            result = client.GetInstancesByVipAddress("boohoo", false);
            Assert.NotNull(result);
            Assert.Equal(0, result.Count);

            apps.ReturnUpInstancesOnly = true;
            result = client.GetInstancesByVipAddress("vapp1", false);
            Assert.NotNull(result);
            Assert.Equal(0, result.Count);

        }

        [Fact]
        public void GetNextServerFromEureka_ReturnsExpected()
        {

            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id21", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.UP });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id22", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.OUT_OF_SERVICE });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            client.Applications = apps;

            var result = client.GetNextServerFromEureka("vapp1", false);
            Assert.NotNull(result);
            Assert.True(result.InstanceId.Equals("id1") || result.InstanceId.Equals("id2"));

            result = client.GetNextServerFromEureka("boohoo", false);
            Assert.Null(result);

            apps.ReturnUpInstancesOnly = true;
            result = client.GetNextServerFromEureka("vapp1", false);
            Assert.Null(result);

        }

        [Fact]
        public void GetInstancesById_Throws_WhenIdNull()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            var ex = Assert.Throws<ArgumentException>(() => client.GetInstanceById(null));
            Assert.Contains("id", ex.Message);
        }

        [Fact]
        public void GetInstancesById_Returns_EmptyListWhenNoApps()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            var result = client.GetInstanceById("myId");
            Assert.NotNull(result);
            Assert.Equal(0, result.Count);

        }
        [Fact]
        public void GetInstanceById_ReturnsExpected()
        {

            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.UP });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.OUT_OF_SERVICE });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            client.Applications = apps;

            var result = client.GetInstanceById("id1");
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.True(result[0].InstanceId.Equals("id1") && result[0].InstanceId.Equals("id1"));
            Assert.True(result[0].AppName.Equals("app1") || result[0].AppName.Equals("app2"));
            Assert.True(result[1].AppName.Equals("app1") || result[1].AppName.Equals("app2"));

            result = client.GetInstanceById("boohoo");
            Assert.NotNull(result);
            Assert.Equal(0, result.Count);

        }

        [Fact]
        public void GetApplication_Throws_WhenAppNameNull()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            var ex = Assert.Throws<ArgumentException>(() => client.GetApplication(null));
            Assert.Contains("appName", ex.Message);
        }

        [Fact]
        public void GetApplication_ReturnsExpected()
        {

            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.UP });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.OUT_OF_SERVICE });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            client.Applications = apps;

            var result = client.GetApplication("app1");
            Assert.NotNull(result);
            Assert.Equal("app1", result.Name);

            result = client.GetApplication("boohoo");
            Assert.Null(result);

        }

        [Fact]
        public void GetInstancesByVipAddressAndAppName_Throws_WhenAddressAndAppnameNull()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            var ex = Assert.Throws<ArgumentNullException>(() => client.GetInstancesByVipAddressAndAppName(null, null, false));
            Assert.Contains("appName", ex.Message);
        }

        [Fact]
        public void RefreshInstanceInfo_CallsHealthCheckHandler_UpdatesInstanceStatus()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            EurekaInstanceConfig iconfig = new EurekaInstanceConfig();
            ApplicationInfoManager.Instance.Initialize(iconfig);

            DiscoveryClient client = new DiscoveryClient(config);
            var myHandler = new MyHealthCheckHandler(InstanceStatus.DOWN);
            client.HealthCheckHandler = myHandler;

            client.RefreshInstanceInfo();

            Assert.True(myHandler.Called);
            Assert.Equal(InstanceStatus.DOWN, ApplicationInfoManager.Instance.InstanceInfo.Status);
        }

        [Fact]
        public void StartTimer_StartsTimer()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            _timerFuncCount = 0;
            var result = client.StartTimer("MyTimer", 100, TimerFunc);
            Assert.NotNull(result);
            System.Threading.Thread.Sleep(1000);
            Assert.True(_timerFuncCount > 0);
            result.Dispose();

        }

        [Fact]
        public void StartTimer_StartsTimer_KeepsRunningOnExceptions()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            _timerFuncCount = 0;
            var result = client.StartTimer("MyTimer", 100, TimerFuncThrows);
            Assert.NotNull(result);
            System.Threading.Thread.Sleep(1000);
            Assert.True(_timerFuncCount > 1);
            result.Dispose();
        }
        [Fact]
        public void StartTimer_StartsTimer_StopsAfterDispose()
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = false,
                ShouldRegisterWithEureka = false
            };
            DiscoveryClient client = new DiscoveryClient(config);
            _timerFuncCount = 0;
            var result = client.StartTimer("MyTimer", 100, TimerFuncThrows);
            Assert.NotNull(result);
            System.Threading.Thread.Sleep(1000);
            Assert.True(_timerFuncCount > 1);
            var currentCount = _timerFuncCount;
            result.Dispose();
            System.Threading.Thread.Sleep(1000);
            Assert.Equal(currentCount, _timerFuncCount);
        }

        private volatile int _timerFuncCount;
        public void TimerFunc()
        {
            ++_timerFuncCount;
        }
        public void TimerFuncThrows()
        {
            ++_timerFuncCount;
            throw new ArgumentException();
        }
    }


    class MyHealthCheckHandler : IHealthCheckHandler
    {
        private InstanceStatus _status;

        public bool Called { get; set; }
        public MyHealthCheckHandler(InstanceStatus status)
        {
            _status = status;
            Called = false;
        }
        public InstanceStatus GetStatus(InstanceStatus currentStatus)
        {
            Called = true;
            return _status;
        }
    }
}
