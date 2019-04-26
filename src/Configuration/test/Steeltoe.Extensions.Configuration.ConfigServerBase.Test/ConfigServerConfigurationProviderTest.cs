// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Xunit;

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
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            HttpClient httpClient = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(settings, httpClient));
            Assert.Contains(nameof(httpClient), ex.Message);
        }

        [Fact]
        public void SettingsConstructor__ThrowsIfEnvironmentNull()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            HttpClient httpClient = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(settings, httpClient, null));
            Assert.Contains(nameof(httpClient), ex.Message);
        }

        [Fact]
        public void SettingsConstructor__WithLoggerFactorySucceeds()
        {
            // Arrange
            LoggerFactory logFactory = new LoggerFactory();
            ConfigServerClientSettings settings = new ConfigServerClientSettings();

            // Act and Assert
            var provider = new ConfigServerConfigurationProvider(settings, logFactory);
            Assert.NotNull(provider.Logger);
        }

        [Fact]
        public void DefaultConstructor_InitializedWithDefaultSettings()
        {
            // Arrange
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider();

            // Act and Assert
            TestHelpers.VerifyDefaults(provider.Settings);
        }

        [Fact]
        public void GetConfigServerUri_NoLabel()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Name = "myName", Environment = "Production" };
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri(null);
            Assert.Equal(settings.RawUri + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_WithLabel()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Name = "myName", Environment = "Production", Label = "myLabel" };
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri(settings.Label);
            Assert.Equal(settings.RawUri + settings.Name + "/" + settings.Environment + "/" + settings.Label, path);
        }

        [Fact]
        public void GetConfigServerUri_WithExtraPathInfo()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "http://localhost:9999/myPath/path/", Name = "myName", Environment = "Production" };
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri(null);
            Assert.Equal("http://localhost:9999/myPath/path/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_WithExtraPathInfo_NoEndingSlash()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "http://localhost:9999/myPath/path", Name = "myName", Environment = "Production" };
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri(null);
            Assert.Equal("http://localhost:9999/myPath/path/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_NoEndingSlash()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "http://localhost:9999", Name = "myName", Environment = "Production" };
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri(null);
            Assert.Equal("http://localhost:9999/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_WithEndingSlash()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "http://localhost:9999/", Name = "myName", Environment = "Production" };
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri(null);
            Assert.Equal("http://localhost:9999/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void Deserialize_EmptyStream()
        {
            // Arrange
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            MemoryStream stream = new MemoryStream();

            // Act and Assert
            Assert.Null(provider.Deserialize(stream));
        }

        [Fact]
        public void Deserialize_BadJson()
        {
            // Arrange (propertySources array bad!)
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
                'key1': 'value1',
                'key2': 10
            }
        }
    
}";
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            Stream stream = TestHelpers.StringToStream(environment);

            // Act and Assert
            ConfigEnvironment env = provider.Deserialize(stream);
            Assert.Null(env);
        }

        [Fact]
        public void Deserialize_GoodJson()
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
                'key1': 'value1',
                'key2': 10
            }
        }
    ]
}";
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            Stream stream = TestHelpers.StringToStream(environment);

            // Act and Assert
            ConfigEnvironment env = provider.Deserialize(stream);
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
            Assert.Equal((long)10, env.PropertySources[0].Source["key2"]);
        }

        [Fact]
        public void AddPropertySource_ChangesDataDictionary()
        {
            // Arrange
            IDictionary<string, object> properties = new Dictionary<string, object>();
            properties["a.b.c.d"] = "value1";
            properties["a"] = "value2";
            properties["b"] = 10;
            PropertySource source = new PropertySource("test", properties);
            source.Name = "test";
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());

            // Act and Assert
            provider.AddPropertySource(source);

            string value;
            Assert.True(provider.TryGet("a:b:c:d", out value));
            Assert.Equal("value1", value);
            Assert.True(provider.TryGet("a", out value));
            Assert.Equal("value2", value);
            Assert.True(provider.TryGet("b", out value));
            Assert.Equal("10", value);
        }

        [Fact]
        public void ConvertArray_NotArrayValue()
        {
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            string result = provider.ConvertArrayKey("foobar");
            Assert.Equal("foobar", result);
        }

        [Fact]
        public void ConvertArray_NotArrayValue2()
        {
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            string result = provider.ConvertArrayKey("foobar[bar]");
            Assert.Equal("foobar[bar]", result);
        }

        [Fact]
        public void ConvertArray_WithArrayValue()
        {
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            string result = provider.ConvertArrayKey("foobar[1234]");
            Assert.Equal("foobar:1234", result);
        }

        [Fact]
        public void ConvertArray_WithArrayArrayValue()
        {
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            string result = provider.ConvertArrayKey("foobar[1234][5678]");
            Assert.Equal("foobar:1234:5678", result);
        }

        [Fact]
        public void ConvertArray_WithArrayArrayNotAtEnd()
        {
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            string result = provider.ConvertArrayKey("foobar[1234][5678]barbar");
            Assert.Equal("foobar[1234][5678]barbar", result);
        }

        [Fact]
        public void ConvertKey_WithArrayArrayValue()
        {
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());
            string result = provider.ConvertKey("a.b.foobar[1234][5678].barfoo.boo[123]");
            Assert.Equal("a:b:foobar:1234:5678:barfoo:boo:123", result);
        }

        [Fact]
        public async void RemoteLoadAsync_InvalidPath()
        {
            // Arrange
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());

            // Act and Assert
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.RemoteLoadAsync("foobar\\foobar\\"));
        }

        [Fact]
        public async void RemoteLoadAsync_HostTimesout()
        {
            // Arrange
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(new ConfigServerClientSettings());

            // Act and Assert
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => provider.RemoteLoadAsync("http://localhost:9999/app/profile"));
        }

        [Fact]
        public async void RemoteLoadAsync_ConfigServerReturnsGreaterThanEqualBadRequest()
        {
            // Arrange
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 500;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());
            string path = provider.GetConfigServerUri(null);

            // Act and Assert
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => provider.RemoteLoadAsync(path));

            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);
        }

        [Fact]
        public async void RemoteLoadAsync_ConfigServerReturnsLessThanBadRequest()
        {
            // Arrange
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 204;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());
            string path = provider.GetConfigServerUri(null);

            // Act and Assert
            ConfigEnvironment result = await provider.RemoteLoadAsync(path);

            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);
            Assert.Null(result);
        }

        [Fact]
        public async void RemoteLoadAsync_ConfigServerReturnsGood()
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
                'key1': 'value1',
                'key2': 10
            }
        }
    ]
}";
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = environment;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());
            string path = provider.GetConfigServerUri(null);

            // Act and Assert
            ConfigEnvironment env = await provider.RemoteLoadAsync(path);
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
            Assert.Equal((long)10, env.PropertySources[0].Source["key2"]);
        }

        [Fact]
        public void Load_ConfigServerReturnsNotFoundStatus()
        {
            // Arrange
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 404;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            provider.Load();
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);
            Assert.Equal(16, provider.Properties.Count);
        }

        [Fact]
        public void Load_ConfigServerReturnsNotFoundStatus_FailFastEnabled()
        {
            // Arrange
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 404;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            settings.FailFast = true;
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.Load());
        }

        [Fact]
        public void Load_ConfigServerReturnsBadStatus_FailFastEnabled()
        {
            // Arrange
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 500;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            settings.FailFast = true;
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.Load());
        }

        [Fact]
        public void Load_ConfigServerReturnsBadStatus_FailFastEnabled_RetryEnabled()
        {
            // Arrange
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = string.Empty;
            TestConfigServerStartup.ReturnStatus = 500;
            TestConfigServerStartup.RequestCount = 0;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            settings.FailFast = true;
            settings.RetryEnabled = true;
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.Load());
            Assert.Equal(6, TestConfigServerStartup.RequestCount);
        }

        [Fact]
        public void Load_ChangesDataDictionary()
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
                'key1': 'value1',
                'key2': 10
            }
        }
    ]
}";
            IHostingEnvironment envir = new HostingEnvironment();
            TestConfigServerStartup.Response = environment;
            TestConfigServerStartup.ReturnStatus = 200;
            var builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(envir.EnvironmentName);
            var server = new TestServer(builder);

            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            provider.Load();
            Assert.NotNull(TestConfigServerStartup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, TestConfigServerStartup.LastRequest.Path.Value);

            string value;
            Assert.True(provider.TryGet("key1", out value));
            Assert.Equal("value1", value);
            Assert.True(provider.TryGet("key2", out value));
            Assert.Equal("10", value);
        }

        [Fact]
        public void AddConfigServerClientSettings_ChangesDataDictionary()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings();

            settings.Enabled = true;
            settings.Environment = "environment";
            settings.FailFast = false;
            settings.Label = "label";
            settings.Name = "name";
            settings.Password = "password";
            settings.Uri = "https://foo.bar/";
            settings.Username = "username";
            settings.ValidateCertificates = false;
            settings.Token = "vaulttoken";
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            provider.AddConfigServerClientSettings();

            string value;

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
        }

        [Fact]
        public void GetLabels_Null()
        {
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            string[] result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(string.Empty, result[0]);
        }

        [Fact]
        public void GetLabels_Empty()
        {
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Label = string.Empty;
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            string[] result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(string.Empty, result[0]);
        }

        [Fact]
        public void GetLabels_SingleString()
        {
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Label = "foobar";
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            string[] result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("foobar", result[0]);
        }

        [Fact]
        public void GetLabels_MultiString()
        {
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Label = "1,2,3,";
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            string[] result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void GetLabels_MultiStringHoles()
        {
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Label = "1,,2,3,";
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            string[] result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void GetRequestMessage_AddsBasicAuthIfPassword()
        {
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://user:password@localhost:8888/";
            settings.Name = "foo";
            settings.Environment = "development";
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            string requestURI = provider.GetConfigServerUri(null);
            var request = provider.GetRequestMessage(requestURI);

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(requestURI, request.RequestUri.ToString());
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Basic", request.Headers.Authorization.Scheme);
            Assert.Equal(provider.GetEncoded("user", "password"), request.Headers.Authorization.Parameter);
        }

        [Fact]
        public void GetRequestMessage_AddsVaultToken_IfNeeded()
        {
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Uri = "http://localhost:8888/";
            settings.Name = "foo";
            settings.Environment = "development";
            settings.Token = "MyVaultToken";
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            string requestURI = provider.GetConfigServerUri(null);
            var request = provider.GetRequestMessage(requestURI);

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(requestURI, request.RequestUri.ToString());
            Assert.True(request.Headers.Contains(ConfigServerConfigurationProvider.TOKEN_HEADER));
            var headerValues = request.Headers.GetValues(ConfigServerConfigurationProvider.TOKEN_HEADER);
            Assert.Contains("MyVaultToken", headerValues);
        }
    }
}
