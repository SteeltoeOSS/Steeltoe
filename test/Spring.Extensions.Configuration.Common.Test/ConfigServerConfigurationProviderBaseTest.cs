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

using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Spring.Extensions.Configuration.Common.Test
{
    public class ConfigServerConfigurationProviderBaseTest
    {

        [Fact]
        public void SettingsConstructor__ThrowsIfSettingsNull()
        {
            // Arrange
            ConfigServerClientSettingsBase settings = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProviderBase(settings));
            Assert.Contains(nameof(settings), ex.Message);
        }

        [Fact]
        public void SettingsConstructor__ThrowsIfHttpClientNull()
        {
            // Arrange
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            HttpClient httpClient = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProviderBase(settings, httpClient));
            Assert.Contains(nameof(httpClient), ex.Message);
        }

        [Fact]
        public void SettingsConstructor__WithLoggerFactorySucceeds()
        {
            // Arrange
            LoggerFactory logFactory = new LoggerFactory();
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();

            // Act and Assert
            var provider = new ConfigServerConfigurationProviderBase(settings, logFactory);
            Assert.NotNull(provider.Logger);
        }


        [Fact]
        public void GetConfigServerUri_NoLabel()
        {
            // Arrange
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase() { Name = "myName", Environment = "Production" };
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri(null);
            Assert.Equal(settings.Uri + "/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_WithLabel()
        {
            // Arrange
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase() { Name = "myName", Environment = "Production", Label = "myLabel" };
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri(settings.Label);
            Assert.Equal(settings.Uri + "/" + settings.Name + "/" + settings.Environment + "/" + settings.Label, path);
        }

        [Fact]
        public void Deserialize_EmptyStream()
        {
            // Arrange
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(new ConfigServerClientSettingsBase());
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
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(new ConfigServerClientSettingsBase());
            Stream stream = ConfigServerTestHelpers.StringToStream(environment);

            // Act and Assert
            Environment env = provider.Deserialize(stream);
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
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(new ConfigServerClientSettingsBase());
            Stream stream = ConfigServerTestHelpers.StringToStream(environment);

            // Act and Assert
            Environment env = provider.Deserialize(stream);
            Assert.NotNull(env);
            Assert.Equal("testname", env.Name);
            Assert.NotNull(env.Profiles);
            Assert.Equal(1, env.Profiles.Count);
            Assert.Equal("testlabel", env.Label);
            Assert.Equal("testversion", env.Version);
            Assert.NotNull(env.PropertySources);
            Assert.Equal(1, env.PropertySources.Count);
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
            IDictionary<string,object> properties = new Dictionary<string, object>();
            properties["a.b.c.d"] = "value1";
            properties["a"] = "value2";
            properties["b"] = 10;
            PropertySource source = new PropertySource("test", properties );
            source.Name = "test";
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(new ConfigServerClientSettingsBase());

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
        public async void RemoteLoadAsync_InvalidPath()
        {
            // Arrange
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(new ConfigServerClientSettingsBase());

            // Act and Assert
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.RemoteLoadAsync("foobar\\foobar\\"));
        }

        [Fact]
        public async void RemoteLoadAsync_HostTimesout()
        {
            // Arrange
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(new ConfigServerClientSettingsBase());

            // Act and Assert
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => provider.RemoteLoadAsync("http://foo.bar:9999/app/profile"));
        }

        [Fact]
        public async void RemoteLoadAsync_ConfigServerReturnsGreaterThanEqualBadRequest()
        {
            // Arrange
            var startup = new TestConfigServerStartup("",500);
            var server = TestServer.Create(startup.Configure);
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings, server.CreateClient());
            string path = provider.GetConfigServerUri(null);

            // Act and Assert
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => provider.RemoteLoadAsync(path));

            Assert.NotNull(startup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, startup.LastRequest.Path.Value);

        }
        [Fact]
        public async void RemoteLoadAsync_ConfigServerReturnsLessThanBadRequest()
        {
            // Arrange
            var startup = new TestConfigServerStartup("", 204);
            var server = TestServer.Create(startup.Configure);
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings, server.CreateClient());
            string path = provider.GetConfigServerUri(null);

            // Act and Assert
            Environment result = await provider.RemoteLoadAsync(path);

            Assert.NotNull(startup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, startup.LastRequest.Path.Value);
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
            var startup = new TestConfigServerStartup(environment, 200);
            var server = TestServer.Create(startup.Configure);
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Uri ="http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings, server.CreateClient());
            string path = provider.GetConfigServerUri(null);

            // Act and Assert
            Environment env = await provider.RemoteLoadAsync(path);
            Assert.NotNull(startup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, startup.LastRequest.Path.Value);
            Assert.NotNull(env);
            Assert.Equal("testname", env.Name);
            Assert.NotNull(env.Profiles);
            Assert.Equal(1, env.Profiles.Count);
            Assert.Equal("testlabel", env.Label);
            Assert.Equal("testversion", env.Version);
            Assert.NotNull(env.PropertySources);
            Assert.Equal(1, env.PropertySources.Count);
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
            var startup = new TestConfigServerStartup("", 404);
            var server = TestServer.Create(startup.Configure);
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings, server.CreateClient());

            // Act and Assert
            provider.Load();
            Assert.NotNull(startup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, startup.LastRequest.Path.Value);
            Assert.Equal(9, provider.Properties.Count);
        }

        [Fact]
        public void Load_ConfigServerReturnsNotFoundStatus_FailFastEnabled()
        {
            // Arrange
            var startup = new TestConfigServerStartup("", 404);
            var server = TestServer.Create(startup.Configure);
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            settings.FailFast = true;
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.Load());

        }


        [Fact]
        public void Load_ConfigServerReturnsBadStatus_FailFastEnabled()
        {
            // Arrange
            var startup = new TestConfigServerStartup("", 500);
            var server = TestServer.Create(startup.Configure);
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            settings.FailFast = true;
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings, server.CreateClient());

            // Act and Assert
            var ex = Assert.Throws<ConfigServerException>(() => provider.Load());

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
            var startup = new TestConfigServerStartup(environment, 200);
            var server = TestServer.Create(startup.Configure);
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Uri = "http://localhost:8888";
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings, server.CreateClient());

            // Act and Assert
            provider.Load();
            Assert.NotNull(startup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, startup.LastRequest.Path.Value);

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
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();

            settings.Enabled = true;
            settings.Environment = "environment";
            settings.FailFast = false;
            settings.Label = "label";
            settings.Name = "name";
            settings.Password = "password";
            settings.Uri = "http://foo.bar/";
            settings.Username = "username";
            settings.ValidateCertificates = false;
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings);


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
            Assert.Equal("http://foo.bar/", value);
            Assert.True(provider.TryGet("spring:cloud:config:username", out value));
            Assert.Equal("username", value);

            Assert.True(provider.TryGet("spring:cloud:config:enabled", out value));
            Assert.Equal("True", value);
            Assert.True(provider.TryGet("spring:cloud:config:failFast", out value));
            Assert.Equal("False", value);
            Assert.True(provider.TryGet("spring:cloud:config:validate_certificates", out value));
            Assert.Equal("False", value);

        }
        [Fact]
        public void GetLabels_Null()
        {
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings);

            string[] result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Equal(1, result.Length);
            Assert.Equal("", result[0]);
        }

        [Fact]
        public void GetLabels_Empty()
        {
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Label = string.Empty;
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings);

            string[] result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Equal(1, result.Length);
            Assert.Equal("", result[0]);
        }
        [Fact]
        public void GetLabels_SingleString()
        {
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Label = "foobar";
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings);

            string[] result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Equal(1, result.Length);
            Assert.Equal("foobar", result[0]);
        }
        [Fact]
        public void GetLabels_MultiString()
        {
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Label = "1,2,3,";
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings);

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
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            settings.Label = "1,,2,3,";
            ConfigServerConfigurationProviderBase provider = new ConfigServerConfigurationProviderBase(settings);

            string[] result = provider.GetLabels();
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

    }
}


