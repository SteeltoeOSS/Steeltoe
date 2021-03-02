// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Transport.Test
{
    public class EurekaHttpClientTest : AbstractBaseTest
    {
        [Fact]
        public void Constructor_Throws_IfConfigNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new EurekaHttpClient((IEurekaClientConfig)null));
            Assert.Contains("config", ex.Message);
        }

        [Fact]
        public void Constructor_Throws_IfHeadersNull()
        {
            IDictionary<string, string> headers = null;
            var ex = Assert.Throws<ArgumentNullException>(() => new EurekaHttpClient(new EurekaClientConfig(), headers));
            Assert.Contains("headers", ex.Message);
        }

        [Fact]
        public void Constructor_Throws_IfServiceUrlBad()
        {
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "foobar\\foobar"
            };
            var ex = Assert.Throws<UriFormatException>(() => new EurekaHttpClient(config));
            Assert.Contains("URI", ex.Message);
        }

        [Fact]
        public async void Register_Throws_IfInstanceInfoNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.RegisterAsync(null));
            Assert.Contains("info", ex.Message);
        }

        [Fact]
        public async void RegisterAsync_ThrowsHttpRequestException_ServerTimeout()
        {
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "http://localhost:9999/"
            };
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<EurekaTransportException>(() => client.RegisterAsync(new InstanceInfo()));
        }

        [Fact]
        public async void RegisterAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
        {
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 204;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            var config = new EurekaInstanceConfig();
            var info = InstanceInfo.FromInstanceConfig(config);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());

            var resp = await client.RegisterAsync(info);
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
            Assert.NotNull(resp.Headers);
        }

        [Fact]
        public async void RegisterAsync_SendsValidPOSTData()
        {
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 204;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            var config = new EurekaInstanceConfig()
            {
                AppName = "foobar"
            };

            var info = InstanceInfo.FromInstanceConfig(config);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var resp = await client.RegisterAsync(info);

            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("POST", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/FOOBAR", TestConfigServerStartup.LastRequest.Path.Value);

            // Check JSON payload
            var recvJson = JsonInstanceInfoRoot.Deserialize(TestConfigServerStartup.LastRequest.Body);
            Assert.NotNull(recvJson);
            Assert.NotNull(recvJson.Instance);

            // Compare a few random values
            var sentJsonObj = info.ToJsonInstance();
            Assert.Equal(sentJsonObj.Actiontype, recvJson.Instance.Actiontype);
            Assert.Equal(sentJsonObj.AppName, recvJson.Instance.AppName);
            Assert.Equal(sentJsonObj.HostName, recvJson.Instance.HostName);
        }

        [Fact]
        public async void SendHeartbeat_Throws_IfAppNameNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.SendHeartBeatAsync(null, "bar", new InstanceInfo(), InstanceStatus.DOWN));
            Assert.Contains("appName", ex.Message);
        }

        [Fact]
        public async void SendHeartbeat_Throws_IfIdNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.SendHeartBeatAsync("foo", null, new InstanceInfo(), InstanceStatus.DOWN));
            Assert.Contains("id", ex.Message);
        }

        [Fact]
        public async void SendHeartbeat_Throws_IfInstanceInfoNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendHeartBeatAsync("foo", "bar", null, InstanceStatus.DOWN));
            Assert.Contains("info", ex.Message);
        }

        [Fact]
        public async void SendHeartBeatAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
        {
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            var config = new EurekaInstanceConfig()
            {
                AppName = "foo",
                InstanceId = "id1"
            };
            var info = InstanceInfo.FromInstanceConfig(config);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var resp = await client.SendHeartBeatAsync("foo", "id1", info, InstanceStatus.UNKNOWN);
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.NotNull(resp.Headers);

            Assert.Equal("PUT", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/FOO/id1", TestConfigServerStartup.LastRequest.Path.Value);
            var time = DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc));
            Assert.Equal("?status=STARTING&lastDirtyTimestamp=" + time, TestConfigServerStartup.LastRequest.QueryString.Value);
        }

        [Fact]
        public async void GetApplicationsAsync_InvokesServer_ReturnsExpectedApplications()
        {
            var json = @"
                { 
                    ""applications"": { 
                        ""versions__delta"":""1"",
                        ""apps__hashcode"":""UP_1_"",
                        ""application"":[{
                            ""name"":""FOO"",
                            ""instance"":[{ 
                                ""instanceId"":""localhost:foo"",
                                ""hostName"":""localhost"",
                                ""app"":""FOO"",
                                ""ipAddr"":""192.168.56.1"",
                                ""status"":""UP"",
                                ""overriddenstatus"":""UNKNOWN"",
                                ""port"":{""$"":8080,""@enabled"":""true""},
                                ""securePort"":{""$"":443,""@enabled"":""false""},
                                ""countryId"":1,
                                ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                                ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":1457714988223,""lastRenewalTimestamp"":1457716158319,""evictionTimestamp"":0,""serviceUpTimestamp"":1457714988223},
                                ""metadata"":{""@class"":""java.util.Collections$EmptyMap""},
                                ""homePageUrl"":""http://localhost:8080/"",
                                ""statusPageUrl"":""http://localhost:8080/info"",
                                ""healthCheckUrl"":""http://localhost:8080/health"",
                                ""vipAddress"":""foo"",
                                ""isCoordinatingDiscoveryServer"":""false"",
                                ""lastUpdatedTimestamp"":""1457714988223"",
                                ""lastDirtyTimestamp"":""1457714988172"",
                                ""actionType"":""ADDED""
                            }]
                        }]
                    }
                }";
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = json;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var resp = await client.GetApplicationsAsync();
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/", TestConfigServerStartup.LastRequest.Path.Value);
            Assert.NotNull(resp.Headers);
            Assert.NotNull(resp.Response);
            Assert.NotNull(resp.Response.ApplicationMap);
            Assert.Single(resp.Response.ApplicationMap);
            var app = resp.Response.GetRegisteredApplication("foo");

            Assert.NotNull(app);
            Assert.Equal("FOO", app.Name);

            var instances = app.Instances;
            Assert.NotNull(instances);
            Assert.Equal(1, instances.Count);
            foreach (var instance in instances)
            {
                Assert.Equal("localhost:foo", instance.InstanceId);
                Assert.Equal("foo", instance.VipAddress);
                Assert.Equal("localhost", instance.HostName);
                Assert.Equal("192.168.56.1", instance.IpAddr);
                Assert.Equal(InstanceStatus.UP, instance.Status);
            }
        }

        [Fact]
        public async void GetVipAsync_Throws_IfVipAddressNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.GetVipAsync(null));
            Assert.Contains("vipAddress", ex.Message);
        }

        [Fact]
        public async void GetSecureVipAsync_Throws_IfVipAddressNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.GetSecureVipAsync(null));
            Assert.Contains("secureVipAddress", ex.Message);
        }

        [Fact]
        public async void GetApplicationAsync_Throws_IfAppNameNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.GetApplicationAsync(null));
            Assert.Contains("appName", ex.Message);
        }

        [Fact]
        public async void GetApplicationAsync_InvokesServer_ReturnsExpectedApplications()
        {
            var json = @"
                {
                    ""application"": {
                        ""name"":""FOO"",
                        ""instance"":[ {
                            ""instanceId"":""localhost:foo"",
                            ""hostName"":""localhost"",
                            ""app"":""FOO"",
                            ""ipAddr"":""192.168.56.1"",
                            ""status"":""UP"",
                            ""overriddenstatus"":""UNKNOWN"",
                            ""port"":{""$"":8080,""@enabled"":""true""},
                            ""securePort"":{""$"":443,""@enabled"":""false""},
                            ""countryId"":1,
                            ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                            ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":1458152330783,""lastRenewalTimestamp"":1458243422342,""evictionTimestamp"":0,""serviceUpTimestamp"":1458152330783},
                            ""metadata"":{""@class"":""java.util.Collections$EmptyMap""},
                            ""homePageUrl"":""http://localhost:8080/"",
                            ""statusPageUrl"":""http://localhost:8080/info"",
                            ""healthCheckUrl"":""http://localhost:8080/health"",
                            ""vipAddress"":""foo"",
                            ""isCoordinatingDiscoveryServer"":""false"",
                            ""lastUpdatedTimestamp"":""1458152330783"",
                            ""lastDirtyTimestamp"":""1458152330696"",
                            ""actionType"":""ADDED""
                        }]
                    }
                }";
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = json;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var resp = await client.GetApplicationAsync("foo");
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/foo", TestConfigServerStartup.LastRequest.Path.Value);
            Assert.NotNull(resp.Headers);
            Assert.NotNull(resp.Response);
            Assert.Equal("FOO", resp.Response.Name);

            var instances = resp.Response.Instances;
            Assert.NotNull(instances);
            Assert.Equal(1, instances.Count);
            foreach (var instance in instances)
            {
                Assert.Equal("localhost:foo", instance.InstanceId);
                Assert.Equal("foo", instance.VipAddress);
                Assert.Equal("localhost", instance.HostName);
                Assert.Equal("192.168.56.1", instance.IpAddr);
                Assert.Equal(InstanceStatus.UP, instance.Status);
            }

            Assert.Equal("http://localhost:8888/", client._serviceUrl);
        }

        [Fact]
        public async void GetApplicationAsync__FirstServerFails_InvokesSecondServer_ReturnsExpectedApplications()
        {
            var json = @"
                {
                    ""application"": {
                        ""name"":""FOO"",
                        ""instance"":[{
                            ""instanceId"":""localhost:foo"",
                            ""hostName"":""localhost"",
                            ""app"":""FOO"",
                            ""ipAddr"":""192.168.56.1"",
                            ""status"":""UP"",
                            ""overriddenstatus"":""UNKNOWN"",
                            ""port"":{""$"":8080,""@enabled"":""true""},
                            ""securePort"":{""$"":443,""@enabled"":""false""},
                            ""countryId"":1,
                            ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                            ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":1458152330783,""lastRenewalTimestamp"":1458243422342,""evictionTimestamp"":0,""serviceUpTimestamp"":1458152330783},
                            ""metadata"":{""@class"":""java.util.Collections$EmptyMap""},
                            ""homePageUrl"":""http://localhost:8080/"",
                            ""statusPageUrl"":""http://localhost:8080/info"",
                            ""healthCheckUrl"":""http://localhost:8080/health"",
                            ""vipAddress"":""foo"",
                            ""isCoordinatingDiscoveryServer"":""false"",
                            ""lastUpdatedTimestamp"":""1458152330783"",
                            ""lastDirtyTimestamp"":""1458152330696"",
                            ""actionType"":""ADDED""
                        }]
                    }
                }";
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = json;
            TestConfigServerStartup.ReturnStatus = 200;
            TestConfigServerStartup.Host = "localhost:8888";
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "https://bad.host:9999/," + uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var resp = await client.GetApplicationAsync("foo");
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/foo", TestConfigServerStartup.LastRequest.Path.Value);
            Assert.NotNull(resp.Headers);
            Assert.NotNull(resp.Response);
            Assert.Equal("FOO", resp.Response.Name);

            var instances = resp.Response.Instances;
            Assert.NotNull(instances);
            Assert.Equal(1, instances.Count);
            foreach (var instance in instances)
            {
                Assert.Equal("localhost:foo", instance.InstanceId);
                Assert.Equal("foo", instance.VipAddress);
                Assert.Equal("localhost", instance.HostName);
                Assert.Equal("192.168.56.1", instance.IpAddr);
                Assert.Equal(InstanceStatus.UP, instance.Status);
            }

            Assert.Equal("http://localhost:8888/", client._serviceUrl);
        }

        [Fact]
        public async void GetInstanceAsync_Throws_IfAppNameNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.GetInstanceAsync(null, "id"));
            Assert.Contains("appName", ex.Message);
        }

        [Fact]
        public async void GetInstanceAsync_Throws_IfAppNameNotNullAndIDNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.GetInstanceAsync("appName", null));
            Assert.Contains("id", ex.Message);
        }

        [Fact]
        public async void GetInstanceAsync_Throws_IfIDNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.GetInstanceAsync(null));
            Assert.Contains("id", ex.Message);
        }

        [Fact]
        public async void GetInstanceAsync_InvokesServer_ReturnsExpectedInstances()
        {
            var json = @"
                { 
                    ""instance"": {
                        ""instanceId"":""DESKTOP-GNQ5SUT"",
                        ""app"":""FOOBAR"",
                        ""appGroupName"":null,
                        ""ipAddr"":""192.168.0.147"",
                        ""sid"":""na"",
                        ""port"":{""@enabled"":true,""$"":80},
                        ""securePort"":{""@enabled"":false,""$"":443},
                        ""homePageUrl"":""http://DESKTOP-GNQ5SUT:80/"",
                        ""statusPageUrl"":""http://DESKTOP-GNQ5SUT:80/Status"",
                        ""healthCheckUrl"":""http://DESKTOP-GNQ5SUT:80/healthcheck"",
                        ""secureHealthCheckUrl"":null,
                        ""vipAddress"":""DESKTOP-GNQ5SUT:80"",
                        ""secureVipAddress"":""DESKTOP-GNQ5SUT:443"",
                        ""countryId"":1,
                        ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                        ""hostName"":""DESKTOP-GNQ5SUT"",
                        ""status"":""UP"",
                        ""overriddenstatus"":""UNKNOWN"",
                        ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":0,""lastRenewalTimestamp"":0,""renewalTimestamp"":0,""evictionTimestamp"":0,""serviceUpTimestamp"":0},
                        ""isCoordinatingDiscoveryServer"":false,
                        ""metadata"":{""@class"":""java.util.Collections$EmptyMap"",""metadata"":null},
                        ""lastUpdatedTimestamp"":1458116137663,
                        ""lastDirtyTimestamp"":1458116137663,
                        ""actionType"":""ADDED"",
                        ""asgName"":null
                    }
                }";
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = json;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var resp = await client.GetInstanceAsync("DESKTOP-GNQ5SUT");
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/instances/DESKTOP-GNQ5SUT", TestConfigServerStartup.LastRequest.Path.Value);
            Assert.NotNull(resp.Headers);
            Assert.NotNull(resp.Response);
            Assert.Equal("DESKTOP-GNQ5SUT", resp.Response.InstanceId);
            Assert.Equal("DESKTOP-GNQ5SUT:80", resp.Response.VipAddress);
            Assert.Equal("DESKTOP-GNQ5SUT", resp.Response.HostName);
            Assert.Equal("192.168.0.147", resp.Response.IpAddr);
            Assert.Equal(InstanceStatus.UP, resp.Response.Status);

            Assert.Equal("http://localhost:8888/", client._serviceUrl);
        }

        [Fact]
        public async void GetInstanceAsync_FirstServerFails_InvokesSecondServer_ReturnsExpectedInstances()
        {
            var json = @"
                { 
                    ""instance"":{
                        ""instanceId"":""DESKTOP-GNQ5SUT"",
                        ""app"":""FOOBAR"",
                        ""appGroupName"":null,
                        ""ipAddr"":""192.168.0.147"",
                        ""sid"":""na"",
                        ""port"":{""@enabled"":true,""$"":80},
                        ""securePort"":{""@enabled"":false,""$"":443},
                        ""homePageUrl"":""http://DESKTOP-GNQ5SUT:80/"",
                        ""statusPageUrl"":""http://DESKTOP-GNQ5SUT:80/Status"",
                        ""healthCheckUrl"":""http://DESKTOP-GNQ5SUT:80/healthcheck"",
                        ""secureHealthCheckUrl"":null,
                        ""vipAddress"":""DESKTOP-GNQ5SUT:80"",
                        ""secureVipAddress"":""DESKTOP-GNQ5SUT:443"",
                        ""countryId"":1,
                        ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                        ""hostName"":""DESKTOP-GNQ5SUT"",
                        ""status"":""UP"",
                        ""overriddenstatus"":""UNKNOWN"",
                        ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":0,""lastRenewalTimestamp"":0,""renewalTimestamp"":0,""evictionTimestamp"":0,""serviceUpTimestamp"":0},
                        ""isCoordinatingDiscoveryServer"":false,
                        ""metadata"":{""@class"":""java.util.Collections$EmptyMap"",""metadata"":null},
                        ""lastUpdatedTimestamp"":1458116137663,
                        ""lastDirtyTimestamp"":1458116137663,
                        ""actionType"":""ADDED"",
                        ""asgName"":null
                    }
                }";
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = json;
            TestConfigServerStartup.ReturnStatus = 200;
            TestConfigServerStartup.Host = "localhost:8888";
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "https://bad.host:9999/," + uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var resp = await client.GetInstanceAsync("DESKTOP-GNQ5SUT");
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/instances/DESKTOP-GNQ5SUT", TestConfigServerStartup.LastRequest.Path.Value);
            Assert.NotNull(resp.Headers);
            Assert.NotNull(resp.Response);
            Assert.Equal("DESKTOP-GNQ5SUT", resp.Response.InstanceId);
            Assert.Equal("DESKTOP-GNQ5SUT:80", resp.Response.VipAddress);
            Assert.Equal("DESKTOP-GNQ5SUT", resp.Response.HostName);
            Assert.Equal("192.168.0.147", resp.Response.IpAddr);
            Assert.Equal(InstanceStatus.UP, resp.Response.Status);

            Assert.Equal("http://localhost:8888/", client._serviceUrl);
        }

        [Fact]
        public async void CancelAsync_Throws_IfAppNameNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.CancelAsync(null, "id"));
            Assert.Contains("appName", ex.Message);
        }

        [Fact]
        public async void CancelAsync_Throws_IfAppNameNotNullAndIDNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.CancelAsync("appName", null));
            Assert.Contains("id", ex.Message);
        }

        [Fact]
        public async void CancelAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
        {
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var resp = await client.CancelAsync("foo", "bar");
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.NotNull(resp.Headers);
            Assert.Equal("DELETE", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/foo/bar", TestConfigServerStartup.LastRequest.Path.Value);

            Assert.Equal("http://localhost:8888/", client._serviceUrl);
        }

        [Fact]
        public async void StatusUpdateAsync_Throws_IfAppNameNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.StatusUpdateAsync(null, "id", InstanceStatus.UP, null));
            Assert.Contains("appName", ex.Message);
        }

        [Fact]
        public async void StatusUpdateAsync_Throws_IfIdNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.StatusUpdateAsync("appName", null, InstanceStatus.UP, null));
            Assert.Contains("id", ex.Message);
        }

        [Fact]
        public async void StatusUpdateAsync_Throws_IfInstanceInfoNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.StatusUpdateAsync("appName", "bar", InstanceStatus.UP, null));
            Assert.Contains("info", ex.Message);
        }

        [Fact]
        public async void StatusUpdateAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
        {
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);

            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var now = DateTime.UtcNow.Ticks;
            var javaTime = DateTimeConversions.ToJavaMillis(new DateTime(now, DateTimeKind.Utc));
            var resp = await client.StatusUpdateAsync("foo", "bar", InstanceStatus.DOWN, new InstanceInfo() { LastDirtyTimestamp = now });
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.NotNull(resp.Headers);
            Assert.Equal("PUT", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/foo/bar/status", TestConfigServerStartup.LastRequest.Path.Value);
            Assert.Equal("?value=DOWN&lastDirtyTimestamp=" + javaTime, TestConfigServerStartup.LastRequest.QueryString.Value);

            Assert.Equal("http://localhost:8888/", client._serviceUrl);
        }

        [Fact]
        public async void DeleteStatusOverrideAsync_Throws_IfAppNameNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.DeleteStatusOverrideAsync(null, "id", null));
            Assert.Contains("appName", ex.Message);
        }

        [Fact]
        public async void DeleteStatusOverrideAsync_Throws_IfIdNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => client.DeleteStatusOverrideAsync("appName", null, null));
            Assert.Contains("id", ex.Message);
        }

        [Fact]
        public async void DeleteStatusOverrideAsync_Throws_IfInstanceInfoNull()
        {
            var config = new EurekaClientConfig();
            var client = new EurekaHttpClient(config);
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteStatusOverrideAsync("appName", "bar", null));
            Assert.Contains("info", ex.Message);
        }

        [Fact]
        public async void DeleteStatusOverrideAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
        {
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var uri = "http://localhost:8888/";
            server.BaseAddress = new Uri(uri);
            var cconfig = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = uri
            };
            var client = new EurekaHttpClient(cconfig, server.CreateClient());
            var now = DateTime.UtcNow.Ticks;
            var javaTime = DateTimeConversions.ToJavaMillis(new DateTime(now, DateTimeKind.Utc));
            var resp = await client.DeleteStatusOverrideAsync("foo", "bar", new InstanceInfo() { LastDirtyTimestamp = now });
            Assert.NotNull(resp);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.NotNull(resp.Headers);
            Assert.Equal("DELETE", TestConfigServerStartup.LastRequest.Method);
            Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
            Assert.Equal("/apps/foo/bar/status", TestConfigServerStartup.LastRequest.Path.Value);
            Assert.Equal("?lastDirtyTimestamp=" + javaTime, TestConfigServerStartup.LastRequest.QueryString.Value);

            Assert.Equal("http://localhost:8888/", client._serviceUrl);
        }

        [Fact]
        public void MakeServiceUrl_Throws_IfServiceUrlBad()
        {
            var ex = Assert.Throws<UriFormatException>(() => EurekaHttpClient.MakeServiceUrl("foobar\\foobar"));
            Assert.Contains("URI", ex.Message);
        }

        [Fact]
        public void MakeServiceUrl_AppendsSlash_IfMissing()
        {
            var result = EurekaHttpClient.MakeServiceUrl("http://boo:123");
            Assert.Equal("http://boo:123/", result);
        }

        [Fact]
        public void MakeServiceUrl_DoesntAppendSlash_IfPresent()
        {
            var result = EurekaHttpClient.MakeServiceUrl("http://boo:123/");
            Assert.Equal("http://boo:123/", result);
        }

        [Fact]
        public void GetRequestMessage_ReturnsCorrectMesssage_WithAdditionalHeaders()
        {
            var headers = new Dictionary<string, string>()
            {
                { "foo", "bar" }
            };
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "http://boo:123/eureka/"
            };
            var client = new EurekaHttpClient(config, headers);
            var result = client.GetRequestMessage(HttpMethod.Post, new Uri("http://boo:123/eureka/"));
            Assert.Equal(HttpMethod.Post, result.Method);
            Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
            Assert.True(result.Headers.Contains("foo"));
        }

        [Fact]
        public void GetRequestMessage_No_Auth_When_Creds_Not_In_Url()
        {
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "http://boo:123/eureka/"
            };
            var client = new EurekaHttpClient(config);
            var result = client.GetRequestMessage(HttpMethod.Post, new Uri(config.EurekaServerServiceUrls));
            Assert.Equal(HttpMethod.Post, result.Method);
            Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
            Assert.False(result.Headers.Contains("Authorization"));

            // arrange
            var clientOptions = new EurekaClientOptions { ServiceUrl = "http://boo:123/eureka/" };
            var optionsMonitor = new TestOptionMonitorWrapper<EurekaClientOptions>(clientOptions);
            client = new EurekaHttpClient(optionsMonitor);

            // act
            result = client.GetRequestMessage(HttpMethod.Post, new Uri(clientOptions.EurekaServerServiceUrls));

            // assert
            Assert.Equal(HttpMethod.Post, result.Method);
            Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
            Assert.False(result.Headers.Contains("Authorization"));
        }

        [Fact]
        public void GetRequestMessage_Adds_Auth_When_Creds_In_Url()
        {
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "http://user:pass@boo:123/eureka/"
            };
            var client = new EurekaHttpClient(config);
            var result = client.GetRequestMessage(HttpMethod.Post, new Uri(config.EurekaServerServiceUrls));
            Assert.Equal(HttpMethod.Post, result.Method);
            Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
            Assert.True(result.Headers.Contains("Authorization"));

            // arrange
            var clientOptions = new EurekaClientOptions { ServiceUrl = "http://user:pass@boo:123/eureka/" };
            var optionsMonitor = new TestOptionMonitorWrapper<EurekaClientOptions>(clientOptions);
            client = new EurekaHttpClient(optionsMonitor);

            // act
            result = client.GetRequestMessage(HttpMethod.Post, new Uri(clientOptions.EurekaServerServiceUrls));

            // assert
            Assert.Equal(HttpMethod.Post, result.Method);
            Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
            Assert.True(result.Headers.Contains("Authorization"));
        }

        [Fact]
        public void GetRequestMessage_Adds_Auth_JustPassword_In_Url()
        {
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "http://:pass@boo:123/eureka/"
            };
            var client = new EurekaHttpClient(config);
            var result = client.GetRequestMessage(HttpMethod.Post, new Uri(config.EurekaServerServiceUrls));
            Assert.Equal(HttpMethod.Post, result.Method);
            Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
            Assert.True(result.Headers.Contains("Authorization"));

            // arrange
            var clientOptions = new EurekaClientOptions { ServiceUrl = "http://:pass@boo:123/eureka/" };
            var optionsMonitor = new TestOptionMonitorWrapper<EurekaClientOptions>(clientOptions);
            client = new EurekaHttpClient(optionsMonitor);

            // act
            result = client.GetRequestMessage(HttpMethod.Post, new Uri(clientOptions.EurekaServerServiceUrls));

            // assert
            Assert.Equal(HttpMethod.Post, result.Method);
            Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
            Assert.True(result.Headers.Contains("Authorization"));
        }

        [Fact]
        public void GetRequestUri_ReturnsCorrect_WithQueryArguments()
        {
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "http://boo:123/eureka/"
            };
            var client = new EurekaHttpClient(config, new HttpClient());
            var queryArgs = new Dictionary<string, string>()
            {
                { "foo", "bar" },
                { "bar", "foo" }
            };
            var result = client.GetRequestUri("http://boo:123/eureka", queryArgs);
            Assert.NotNull(result);
            Assert.Equal("http://boo:123/eureka?foo=bar&bar=foo", result.ToString());
        }

        [Fact]
        public void GetServiceUrlCandidates_NoFailingUrls_ReturnsExpected()
        {
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "http://user:pass@boo:123/eureka/,http://user:pass@foo:123/eureka"
            };
            var client = new EurekaHttpClient(config);
            var result = client.GetServiceUrlCandidates();
            Assert.Contains("http://user:pass@boo:123/eureka/", result);
            Assert.Contains("http://user:pass@foo:123/eureka/", result);
        }

        [Fact]
        public void GetServiceUrlCandidates_WithFailingUrls_ReturnsExpected()
        {
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "https://user:pass@boo:123/eureka/,https://user:pass@foo:123/eureka,https://user:pass@blah:123/eureka,https://user:pass@blah.blah:123/eureka"
            };
            var client = new EurekaHttpClient(config);
            client.AddToFailingServiceUrls("https://user:pass@foo:123/eureka/");
            client.AddToFailingServiceUrls("https://user:pass@blah.blah:123/eureka/");

            var result = client.GetServiceUrlCandidates();
            Assert.Contains("https://user:pass@boo:123/eureka/", result);
            Assert.Contains("https://user:pass@blah:123/eureka/", result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetServiceUrlCandidates_ThresholdHit_ReturnsExpected()
        {
            var config = new EurekaClientConfig()
            {
                EurekaServerServiceUrls = "http://user:pass@boo:123/eureka/,http://user:pass@foo:123/eureka"
            };
            var client = new EurekaHttpClient(config);
            client.AddToFailingServiceUrls("http://user:pass@foo:123/eureka/");

            var result = client.GetServiceUrlCandidates();
            Assert.Contains("http://user:pass@boo:123/eureka/", result);
            Assert.Contains("http://user:pass@foo:123/eureka/", result);
        }
    }
}
