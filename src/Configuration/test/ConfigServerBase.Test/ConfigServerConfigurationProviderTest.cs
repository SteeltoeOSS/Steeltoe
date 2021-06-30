// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class ConfigServerConfigurationProviderTest
    {
        [Fact]
        public void SettingsConstructor__ThrowsIfSettingsNull()
        {
            // Arrange
            ConfigServerClientSettings settings = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(settings));
            Assert.Contains(nameof(settings), ex.Message);
        }

        [Fact]
        public void SettingsConstructor__ThrowsIfHttpClientNull()
        {
            // Arrange
            var settings = new ConfigServerClientSettings();
            HttpClient httpClient = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(settings, httpClient));
            Assert.Contains(nameof(httpClient), ex.Message);
        }

        [Fact]
        public void SettingsConstructor__ThrowsIfEnvironmentNull()
        {
            // Arrange
            var settings = new ConfigServerClientSettings();
            HttpClient httpClient = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(settings, httpClient, null));
            Assert.Contains(nameof(httpClient), ex.Message);
        }

        [Fact]
        public void SettingsConstructor__WithLoggerFactorySucceeds()
        {
            // Arrange
            var logFactory = new LoggerFactory();
            var settings = new ConfigServerClientSettings();

            // Act and Assert
            var provider = new ConfigServerConfigurationProvider(settings, logFactory);
            Assert.NotNull(provider.Logger);
        }

        [Fact]
        public void DefaultConstructor_InitializedWithDefaultSettings()
        {
            // Arrange
            var provider = new ConfigServerConfigurationProvider();

            // Act and Assert
            TestHelper.VerifyDefaults(provider.Settings);
        }

        [Fact]
        public void SourceConstructor_WithDefaults_InitializesWithDefaultSettings()
        {
            // Arrange
            IConfiguration configuration = new ConfigurationBuilder().Build();
            var source = new ConfigServerConfigurationSource(configuration);
            var provider = new ConfigServerConfigurationProvider(source);

            // Act and Assert
            TestHelper.VerifyDefaults(provider.Settings);
        }

        [Fact]
        public void SourceConstructor_WithDefaults_ThrowsIfHttpClientNull()
        {
            // Arrange
            IConfiguration configuration = new ConfigurationBuilder().Build();
            var source = new ConfigServerConfigurationSource(configuration);

            // Act
            Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(source, null));
        }

        [Fact]
        public void GetConfigServerUri_NoBaseUri_Throws()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Name = "myName", Environment = "Production" };
            var provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            Assert.Throws<ArgumentException>(() => provider.GetConfigServerUri(null, null));
        }

        [Fact]
        public void GetConfigServerUri_NoLabel()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Name = "myName", Environment = "Production" };
            var provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            var path = provider.GetConfigServerUri(settings.RawUris[0], null);
            Assert.Equal(settings.RawUris[0] + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_WithLabel()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Name = "myName", Environment = "Production", Label = "myLabel" };
            var provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            var path = provider.GetConfigServerUri(settings.RawUris[0], settings.Label);
            Assert.Equal(settings.RawUris[0] + settings.Name + "/" + settings.Environment + "/" + settings.Label, path);
        }

        [Fact]
        public void GetConfigServerUri_WithLabelContainingSlash()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Name = "myName", Environment = "Production", Label = "myLabel/version" };
            var provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            var path = provider.GetConfigServerUri(settings.RawUris[0], settings.Label);
            Assert.Equal(settings.RawUris[0] + settings.Name + "/" + settings.Environment + "/" + "myLabel(_)version", path);
        }

        [Fact]
        public void GetConfigServerUri_WithExtraPathInfo()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "http://localhost:9999/myPath/path/", Name = "myName", Environment = "Production" };
            var provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            var path = provider.GetConfigServerUri(settings.RawUris[0], null);
            Assert.Equal("http://localhost:9999/myPath/path/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_WithExtraPathInfo_NoEndingSlash()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "http://localhost:9999/myPath/path", Name = "myName", Environment = "Production" };
            var provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            var path = provider.GetConfigServerUri(settings.RawUris[0], null);
            Assert.Equal("http://localhost:9999/myPath/path/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_NoEndingSlash()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "http://localhost:9999", Name = "myName", Environment = "Production" };
            var provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            var path = provider.GetConfigServerUri(settings.RawUris[0], null);
            Assert.Equal("http://localhost:9999/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_WithEndingSlash()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "http://localhost:9999/", Name = "myName", Environment = "Production" };
            var provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            var path = provider.GetConfigServerUri(settings.RawUris[0], null);
            Assert.Equal("http://localhost:9999/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void Deserialize_EmptyStream()
        {
            // Arrange
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var stream = new MemoryStream();

            // Act and Assert
            Assert.Null(provider.Deserialize(stream));
        }

        [Fact]
        public void Deserialize_BadJson()
        {
            // Arrange (propertySources array bad!)
            var environment = @"
                {
                    ""name"": ""testname"",
                    ""profiles"": [""Production""],
                    ""label"": ""testlabel"",
                    ""version"": ""testversion"",
                    ""propertySources"": [ 
                        { 
                            ""name"": ""source"",
                            ""source"": {
                                ""key1"": ""value1"",
                                ""key2"": 10
                            }
                        }
    
                }";
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var stream = TestHelpers.StringToStream(environment);

            // Act and Assert
            var env = provider.Deserialize(stream);
            Assert.Null(env);
        }

        [Fact]
        public void Deserialize_GoodJson()
        {
            // Arrange
            var environment = @"
                {
                    ""name"": ""testname"",
                    ""profiles"": [""Production""],
                    ""label"": ""testlabel"",
                    ""version"": ""testversion"",
                    ""propertySources"": [ 
                        { 
                            ""name"": ""source"",
                            ""source"": {
                                ""key1"": ""value1"",
                                ""key2"": 10
                            }
                        }
                    ]
                }";
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var stream = TestHelpers.StringToStream(environment);

            // Act and Assert
            var env = provider.Deserialize(stream);
            Assert.NotNull(env);
            Assert.Equal("testname", env.Name);
            Assert.NotNull(env.Profiles);
            Assert.Single(env.Profiles);
            Assert.Equal("testlabel", env.Label);
            Assert.Equal("testversion", env.Version);
            Assert.NotNull(env.PropertySources);
            Assert.Single(env.PropertySources);
            Assert.Equal("source", env.PropertySources[0].Name);
            Assert.NotNull(env.PropertySources[0].Source);
            Assert.Equal(2, env.PropertySources[0].Source.Count);
            Assert.Equal("value1", env.PropertySources[0].Source["key1"]);
            Assert.Equal(10L, env.PropertySources[0].Source["key2"]);
        }

        [Fact]
        [Obsolete]
        public void AddPropertySource_ChangesDataDictionary()
        {
            // Arrange
            IDictionary<string, object> properties = new Dictionary<string, object>
            {
                ["a.b.c.d"] = "value1",
                ["a"] = "value2",
                ["b"] = 10
            };
            var source = new PropertySource("test", properties)
            {
                Name = "test"
            };
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());

            // Act and Assert
            provider.AddPropertySource(source);

            Assert.True(provider.TryGet("a:b:c:d", out var value));
            Assert.Equal("value1", value);
            Assert.True(provider.TryGet("a", out value));
            Assert.Equal("value2", value);
            Assert.True(provider.TryGet("b", out value));
            Assert.Equal("10", value);
        }

        [Fact]
        public void ConvertArray_NotArrayValue()
        {
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var result = provider.ConvertArrayKey("foobar");
            Assert.Equal("foobar", result);
        }

        [Fact]
        public void ConvertArray_NotArrayValue2()
        {
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var result = provider.ConvertArrayKey("foobar[bar]");
            Assert.Equal("foobar[bar]", result);
        }

        [Fact]
        public void ConvertArray_WithArrayValue()
        {
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var result = provider.ConvertArrayKey("foobar[1234]");
            Assert.Equal("foobar:1234", result);
        }

        [Fact]
        public void ConvertArray_WithArrayArrayValue()
        {
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var result = provider.ConvertArrayKey("foobar[1234][5678]");
            Assert.Equal("foobar:1234:5678", result);
        }

        [Fact]
        public void ConvertArray_WithArrayArrayNotAtEnd()
        {
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var result = provider.ConvertArrayKey("foobar[1234][5678]barbar");
            Assert.Equal("foobar[1234][5678]barbar", result);
        }

        [Fact]
        public void ConvertKey_WithArrayArrayValue()
        {
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var result = provider.ConvertKey("a.b.foobar[1234][5678].barfoo.boo[123]");
            Assert.Equal("a:b:foobar:1234:5678:barfoo:boo:123", result);
        }

        [Fact]
        public void ConvertKey_WithEscapedDot()
        {
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            var result = provider.ConvertKey(@"a.b\.foobar");
            Assert.Equal("a:b.foobar", result);
        }

        [Fact]
        public async void RemoteLoadAsync_InvalidUri()
        {
            // Arrange
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());

            // Act and Assert
            var ex = await Assert.ThrowsAsync<UriFormatException>(() => provider.RemoteLoadAsync(new string[] { "foobar\\foobar\\" }, null));
        }

        [Fact]
        public async void RemoteLoadAsync_HostTimesOut()
        {
            // Arrange
            var provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());

            // Act and Assert
            try
            {
                var ex = await Assert.ThrowsAsync<HttpRequestException>(() => provider.RemoteLoadAsync(new string[] { "http://localhost:9999/app/profile" }, null));
            }
            catch (ThrowsException e)
            {
                if (e.InnerException is TaskCanceledException)
                {
                    return;
                }

                Assert.True(false, "Request didn't timeout or throw TaskCanceledException");
            }
        }

        [Fact]
        public async void RemoteLoadAsync_ConfigServerReturnsGreaterThanEqualBadRequest()
        {
            // Arrange
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 500 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment("testing");
            using var server = new TestServer(builder);
            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName"
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => provider.RemoteLoadAsync(settings.GetUris(), null));

            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);
        }

        [Fact]
        public async void RemoteLoadAsync_ConfigServerReturnsLessThanBadRequest()
        {
            // Arrange
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 204 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName"
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var result = await provider.RemoteLoadAsync(settings.GetRawUris(), null);

            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);
            Assert.Null(result);
        }

        [Fact]
        public void DoLoad_MultipleLabels_ChecksAllLabels()
        {
            // Arrange
            var environment = @"
                {
                    ""name"": ""testname"",
                    ""profiles"": [""Production""],
                    ""label"": ""testlabel"",
                    ""version"": ""testversion"",
                    ""propertySources"": [ 
                        { 
                            ""name"": ""source"",
                            ""source"": {
                                ""key1"": ""value1"",
                                ""key2"": 10
                            }
                        }
                    ]
                }";
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.Response = environment;
            TestConfigServerStartup.ReturnStatus = new int[] { 404, 200 };
            TestConfigServerStartup.Label = "testlabel";
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName",
                Label = "label,testlabel"
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            provider.DoLoad();

            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal(2, TestConfigServerStartup.RequestCount);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment + "/testlabel", TestConfigServerStartup.LastRequest.Path.Value);
        }

        [Fact]
        public async void RemoteLoadAsync_ConfigServerReturnsGood()
        {
            // Arrange
            var environment = @"
                {
                    ""name"": ""testname"",
                    ""profiles"": [""Production""],
                    ""label"": ""testlabel"",
                    ""version"": ""testversion"",
                    ""propertySources"": [ 
                        { 
                            ""name"": ""source"",
                            ""source"": {
                                ""key1"": ""value1"",
                                ""key2"": 10
                            }
                        }
                    ]
                }";
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.Response = environment;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName"
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var env = await provider.RemoteLoadAsync(settings.GetUris(), null);
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);
            Assert.NotNull(env);
            Assert.Equal("testname", env.Name);
            Assert.NotNull(env.Profiles);
            Assert.Single(env.Profiles);
            Assert.Equal("testlabel", env.Label);
            Assert.Equal("testversion", env.Version);
            Assert.NotNull(env.PropertySources);
            Assert.Single(env.PropertySources);
            Assert.Equal("source", env.PropertySources[0].Name);
            Assert.NotNull(env.PropertySources[0].Source);
            Assert.Equal(2, env.PropertySources[0].Source.Count);
            Assert.Equal("value1", env.PropertySources[0].Source["key1"]);
            Assert.Equal(10L, env.PropertySources[0].Source["key2"]);
        }

        [Fact]
        public void Load_MultipleConfigServers_ReturnsGreaterThanEqualBadRequest_StopsChecking()
        {
            // Arrange
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 500, 200 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888, http://localhost:8888",
                Name = "myName"
            };
            server.BaseAddress = new Uri("http://localhost:8888");
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            provider.LoadInternal();
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);
            Assert.Equal(1, TestConfigServerStartup.RequestCount);
        }

        [Fact]
        public void Load_MultipleConfigServers_ReturnsNotFoundStatus_DoesNotContinueChecking()
        {
            // Arrange
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 404, 200 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888, http://localhost:8888",
                Name = "myName"
            };
            server.BaseAddress = new Uri("http://localhost:8888");
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            provider.LoadInternal();
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);
            Assert.Equal(1, TestConfigServerStartup.RequestCount);
        }

        [Fact]
        public void Load_ConfigServerReturnsNotFoundStatus()
        {
            // Arrange
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 404 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName"
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            provider.LoadInternal();
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);
            Assert.Equal(26, provider.Properties.Count);
        }

        [Fact]
        public void Load_ConfigServerReturnsNotFoundStatus_FailFastEnabled()
        {
            // Arrange
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 404 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName",
                FailFast = true
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.LoadInternal());
        }

        [Fact]
        public void Load_MultipleConfigServers_ReturnsNotFoundStatus__DoesNotContinueChecking_FailFastEnabled()
        {
            // Arrange
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 404, 200 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888, http://localhost:8888 ",
                Name = "myName",
                FailFast = true
            };
            server.BaseAddress = new Uri("http://localhost:8888");
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.LoadInternal());
            Assert.Equal(1, TestConfigServerStartup.RequestCount);
        }

        [Fact]
        public void Load_ConfigServerReturnsBadStatus_FailFastEnabled()
        {
            // Arrange
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 500 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName",
                FailFast = true
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.LoadInternal());
        }

        [Fact]
        public void Load_MultipleConfigServers_ReturnsBadStatus_StopsChecking_FailFastEnabled()
        {
            // Arrange
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 500, 500, 500 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888, http://localhost:8888, http://localhost:8888",
                Name = "myName",
                FailFast = true
            };
            server.BaseAddress = new Uri("http://localhost:8888");
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.LoadInternal());
            Assert.Equal(1, TestConfigServerStartup.RequestCount);
        }

        [Fact]
        public void Load_ConfigServerReturnsBadStatus_FailFastEnabled_RetryEnabled()
        {
            // Arrange
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.ReturnStatus = new int[] { 500, 500, 500, 500, 500, 500 };
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName",
                FailFast = true,
                RetryEnabled = true
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.LoadInternal());
            Assert.Equal(6, TestConfigServerStartup.RequestCount);
        }

        [Fact]
        public void Load_ChangesDataDictionary()
        {
            // Arrange
            var environment = @"
                {
                    ""name"": ""testname"",
                    ""profiles"": [""Production""],
                    ""label"": ""testlabel"",
                    ""version"": ""testversion"",
                    ""propertySources"": [ 
                        { 
                            ""name"": ""source"",
                            ""source"": {
                                ""key1"": ""value1"",
                                ""key2"": 10
                            }
                        }
                    ]
                }";
            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.Response = environment;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName"
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            provider.LoadInternal();
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);

            Assert.True(provider.TryGet("key1", out var value));
            Assert.Equal("value1", value);
            Assert.True(provider.TryGet("key2", out value));
            Assert.Equal("10", value);
        }

        [Fact]
        public void ReLoad_DataDictionary_With_New_Configurations()
        {
            // Arrange
            var environment = @"
                    {
                        'name': 'testname',
                        'profiles': ['Production'],
                        'label': 'testlabel',
                        'version': 'testversion',
                        'propertySources': [ 
                            { 
                                'name': 'source',
                                'source': {
                                            'featureToggles.ShowModule[0]': 'FT1',
                                            'featureToggles.ShowModule[1]': 'FT2',
                                            'featureToggles.ShowModule[2]': 'FT3',
                                            'enableSettings':'true'
                                    }
                            }
                        ]
                    }";

            var envir = HostingHelpers.GetHostingEnvironment();
            TestConfigServerStartup.Reset();
            TestConfigServerStartup.Response = environment;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName"
            };
            server.BaseAddress = new Uri(settings.Uri);
            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            provider.Load();
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.True(provider.TryGet("featureToggles:ShowModule:0", out var value));
            Assert.Equal("FT1", value);
            Assert.True(provider.TryGet("featureToggles:ShowModule:1", out value));
            Assert.Equal("FT2", value);
            Assert.True(provider.TryGet("featureToggles:ShowModule:2", out value));
            Assert.Equal("FT3", value);
            Assert.True(provider.TryGet("enableSettings", out value));
            Assert.Equal("true", value);

            TestConfigServerStartup.Reset();
            TestConfigServerStartup.Response = @"
                {
                    'name': 'testname',
                    'profiles': ['Production'],
                    'label': 'testlabel',
                    'version': 'testversion',
                    'propertySources': [ 
                        { 
                            'name': 'source',
                            'source': {
                                'featureToggles.ShowModule[0]': 'none'
                            }
                        }
                    ]
                }";
            provider.Load();
            Assert.True(provider.TryGet("featureToggles:ShowModule:0", out var val));
            Assert.Equal("none", val);
            Assert.False(provider.TryGet("featureToggles:ShowModule:1", out val));
            Assert.False(provider.TryGet("featureToggles:ShowModule:2", out val));
            Assert.False(provider.TryGet("enableSettings", out val));
        }

        [Fact]
        public void AddConfigServerClientSettings_ChangesDataDictionary()
        {
            // Arrange
            var settings = new ConfigServerClientSettings
            {
                AccessTokenUri = "https://foo.bar/",
                ClientId = "client_id",
                ClientSecret = "client_secret",
                Enabled = true,
                Environment = "environment",
                FailFast = false,
                Label = "label",
                Name = "name",
                Password = "password",
                Uri = "https://foo.bar/",
                Username = "username",
                ValidateCertificates = false,
                Token = "vaulttoken",
                TokenRenewRate = 1,
                TokenTtl = 2,
                RetryMultiplier = 1.1
            };

            var provider = new ConfigServerConfigurationProvider(settings);
            var initialCulture = GetAndSetCurrentCulture(new CultureInfo("ru-RU"));

            try
            {
                // Act and Assert
                provider.AddConfigServerClientSettings();

                Assert.True(provider.TryGet("spring:cloud:config:access_token_uri", out var value));
                Assert.Equal("https://foo.bar/", value);
                Assert.True(provider.TryGet("spring:cloud:config:client_id", out value));
                Assert.Equal("client_id", value);
                Assert.True(provider.TryGet("spring:cloud:config:client_secret", out value));
                Assert.Equal("client_secret", value);
                Assert.True(provider.TryGet("spring:cloud:config:env", out value));
                Assert.Equal("environment", value);
                Assert.True(provider.TryGet("spring:cloud:config:label", out value));
                Assert.Equal("label", value);
                Assert.True(provider.TryGet("spring:cloud:config:name", out value));
                Assert.Equal("name", value);
                Assert.True(provider.TryGet("spring:cloud:config:password", out value));
                Assert.Equal("password", value);
                Assert.True(provider.TryGet("spring:cloud:config:uri", out value));
                Assert.Equal("https://foo.bar/", value);
                Assert.True(provider.TryGet("spring:cloud:config:username", out value));
                Assert.Equal("username", value);

                Assert.True(provider.TryGet("spring:cloud:config:enabled", out value));
                Assert.Equal("True", value);
                Assert.True(provider.TryGet("spring:cloud:config:failFast", out value));
                Assert.Equal("False", value);
                Assert.True(provider.TryGet("spring:cloud:config:validate_certificates", out value));
                Assert.Equal("False", value);
                Assert.True(provider.TryGet("spring:cloud:config:token", out value));
                Assert.Equal("vaulttoken", value);
                Assert.True(provider.TryGet("spring:cloud:config:timeout", out value));
                Assert.Equal("6000", value);
                Assert.True(provider.TryGet("spring:cloud:config:tokenRenewRate", out value));
                Assert.Equal("1", value);
                Assert.True(provider.TryGet("spring:cloud:config:tokenTtl", out value));
                Assert.Equal("2", value);
                Assert.True(provider.TryGet("spring:cloud:config:discovery:enabled", out value));
                Assert.Equal("False", value);
                Assert.True(provider.TryGet("spring:cloud:config:discovery:serviceId", out value));
                Assert.Equal(ConfigServerClientSettings.DEFAULT_CONFIGSERVER_SERVICEID, value);
                Assert.True(provider.TryGet("spring:cloud:config:retry:multiplier", out value));
                Assert.Equal("1.1", value);
            }
            finally
            {
                GetAndSetCurrentCulture(initialCulture);
            }
        }

        private static CultureInfo GetAndSetCurrentCulture(CultureInfo newCulture)
        {
            var oldCulture = CultureInfo.DefaultThreadCurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = newCulture;
            return oldCulture;
        }

        [Fact]
#pragma warning disable SA1202 // Elements should be ordered by access
        public void GetLabels_Null()
#pragma warning restore SA1202 // Elements should be ordered by access
        {
            var settings = new ConfigServerClientSettings();
            var provider = new ConfigServerConfigurationProvider(settings);

            var result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(string.Empty, result[0]);
        }

        [Fact]
        public void GetLabels_Empty()
        {
            var settings = new ConfigServerClientSettings
            {
                Label = string.Empty
            };
            var provider = new ConfigServerConfigurationProvider(settings);

            var result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(string.Empty, result[0]);
        }

        [Fact]
        public void GetLabels_SingleString()
        {
            var settings = new ConfigServerClientSettings
            {
                Label = "foobar"
            };
            var provider = new ConfigServerConfigurationProvider(settings);

            var result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("foobar", result[0]);
        }

        [Fact]
        public void GetLabels_MultiString()
        {
            var settings = new ConfigServerClientSettings
            {
                Label = "1,2,3,"
            };
            var provider = new ConfigServerConfigurationProvider(settings);

            var result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void GetLabels_MultiStringHoles()
        {
            var settings = new ConfigServerClientSettings
            {
                Label = "1,,2,3,"
            };
            var provider = new ConfigServerConfigurationProvider(settings);

            var result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void GetRequestMessage_AddsBasicAuthIfPassword()
        {
            var settings = new ConfigServerClientSettings
            {
                Uri = "http://user:password@localhost:8888/",
                Name = "foo",
                Environment = "development"
            };
            var provider = new ConfigServerConfigurationProvider(settings);

            var requestURI = provider.GetConfigServerUri(settings.RawUris[0], null);
            var request = provider.GetRequestMessage(requestURI, "user", "password");

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(requestURI, request.RequestUri.ToString());
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Basic", request.Headers.Authorization.Scheme);
            Assert.Equal(provider.GetEncoded("user", "password"), request.Headers.Authorization.Parameter);
        }

        [Fact]
        public void GetRequestMessage_AddsVaultToken_IfNeeded()
        {
            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888/",
                Name = "foo",
                Environment = "development",
                Token = "MyVaultToken"
            };
            var provider = new ConfigServerConfigurationProvider(settings);

            var requestURI = provider.GetConfigServerUri(settings.RawUris[0], null);
            var request = provider.GetRequestMessage(requestURI, null, null);

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(requestURI, request.RequestUri.ToString());
            Assert.True(request.Headers.Contains(ConfigServerConfigurationProvider.TOKEN_HEADER));
            var headerValues = request.Headers.GetValues(ConfigServerConfigurationProvider.TOKEN_HEADER);
            Assert.Contains("MyVaultToken", headerValues);
        }

        [Fact]
        public void IsDiscoveryFirstEnabled_ReturnsExpected()
        {
            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888/",
                Name = "foo",
                Environment = "development",
                DiscoveryEnabled = true
            };

            var provider = new ConfigServerConfigurationProvider(settings);
            Assert.True(provider.IsDiscoveryFirstEnabled());

            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:discovery:enabled", "True" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888/",
                Name = "foo",
                Environment = "development"
            };
            var source = new ConfigServerConfigurationSource(settings, configuration);
            provider = new ConfigServerConfigurationProvider(source);

            // Act and Assert
            Assert.True(provider.IsDiscoveryFirstEnabled());
        }

        [Fact]
        public void UpdateSettingsFromDiscovery_UpdatesSettingsCorrectly()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:discovery:enabled", "True" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888/",
                Name = "foo",
                Environment = "development"
            };
            var source = new ConfigServerConfigurationSource(settings, configuration);
            var provider = new ConfigServerConfigurationProvider(source);

            provider.UpdateSettingsFromDiscovery(new List<IServiceInstance>(), settings);
            Assert.Null(settings.Username);
            Assert.Null(settings.Password);
            Assert.Equal("http://localhost:8888/", settings.Uri);

            var metadata1 = new Dictionary<string, string>()
            {
                { "password", "firstPassword" }
            };

            var metadata2 = new Dictionary<string, string>()
            {
                { "password", "secondPassword" },
                { "user", "secondUser" },
                { "configPath", "configPath" }
            };

            var instances = new List<IServiceInstance>()
            {
                new TestServiceInfo(new Uri("https://foo.bar:8888/"), metadata1),
                new TestServiceInfo(new Uri("https://foo.bar.baz:9999/"), metadata2)
            };

            provider.UpdateSettingsFromDiscovery(instances, settings);
            Assert.Equal("secondUser", settings.Username);
            Assert.Equal("secondPassword", settings.Password);
            Assert.Equal("https://foo.bar:8888/,https://foo.bar.baz:9999/configPath", settings.Uri);
        }

        [Fact]
        public void DiscoverServerInstances_FailsFast()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:discovery:enabled", "True" },
                { "spring:cloud:config:failFast", "True" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888/",
                Name = "foo",
                Environment = "development"
            };
            var source = new ConfigServerConfigurationSource(settings, configuration);
            var provider = new ConfigServerConfigurationProvider(source);
            var service = new ConfigServerDiscoveryService(configuration, settings);
            Assert.Throws<ConfigServerException>(() => provider.DiscoverServerInstances(service));
        }

        private class TestServiceInfo : IServiceInstance
        {
            public TestServiceInfo(Uri uri, IDictionary<string, string> metadata)
            {
                Uri = uri;
                Metadata = metadata;
            }

            public string ServiceId => throw new NotImplementedException();

            public string Host => throw new NotImplementedException();

            public int Port => throw new NotImplementedException();

            public bool IsSecure => throw new NotImplementedException();

            public Uri Uri { get; private set; }

            public IDictionary<string, string> Metadata { get; private set; }
        }

        [Fact]
        public void Reload_And_Bind_Without_Throwing_Exception()
        {
            // Arrange
            var environment = @"
                {
                    ""name"": ""testname"",
                    ""profiles"": [""Production""],
                    ""label"": ""testlabel"",
                    ""version"": ""testversion"",
                    ""propertySources"": [ 
                        { 
                            ""name"": ""source"",
                            ""source"": {
                                ""name"": ""my-app"",
                                ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                            }
                        }
                    ]
                }";

            TestConfigServerStartup.Reset();
            TestConfigServerStartup.Response = environment;
            var hostingEnvironment = HostingHelpers.GetHostingEnvironment();
            var hostBuilder = new WebHostBuilder()
                .UseStartup<TestConfigServerStartup>()
                .UseEnvironment(hostingEnvironment.EnvironmentName);

            var settings = new ConfigServerClientSettings
            {
                Uri = "http://localhost:8888",
                Name = "myName"
            };

            var server = new TestServer(hostBuilder)
            {
                BaseAddress = new Uri(settings.Uri)
            };

            var provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.Add(new TestConfigServerConfigurationSource(provider));

            var configuration = configurationBuilder.Build();

            // Act
            TestOptions options = null;

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
            void ReloadLoop()
            {
                while (!cts.IsCancellationRequested)
                {
                    configuration.Reload();
                }
            }

            _ = Task.Run(ReloadLoop);

            while (!cts.IsCancellationRequested)
            {
                options = configuration.Get<TestOptions>();
            }

            // Assert
            Assert.Equal("my-app", options.Name);
            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.Version);
        }

        private sealed class TestOptions
        {
            public string Name { get; set; }

            public string Version { get; set; }
        }
    }
}
