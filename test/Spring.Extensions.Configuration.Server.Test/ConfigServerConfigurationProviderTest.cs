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

namespace Spring.Extensions.Configuration.Server.Test
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
        public void ProvidersConstructor_InitializedWithDefaultsWhenNull()
        {
            // Arrange
            IEnumerable<IConfigurationProvider> providers = null;
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(providers);


            // Act and Assert
            ConfigServerTestHelpers.VerifyDefaults(provider.Settings);

        }

        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            // Arrange
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider();

            // Act and Assert
            ConfigServerTestHelpers.VerifyDefaults(provider.Settings);

        }

        [Fact]
        public void GetConfigServerUri_NoLabel()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Name = "myName", Environment = "Production" };
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri();
            Assert.Equal(settings.Uri + "/" + settings.Name + "/" + settings.Environment, path);
        }

        [Fact]
        public void GetConfigServerUri_WithLabel()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Name = "myName", Environment = "Production", Label = "myLabel" };
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings);

            // Act and Assert
            string path = provider.GetConfigServerUri();
            Assert.Equal(settings.Uri + "/" + settings.Name + "/" + settings.Environment + "/" + settings.Label, path);
        }

        [Fact]
        public void Deserialize_EmptyStream()
        {
            // Arrange
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider();
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
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider();
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
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider();
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
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider();

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
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider();

            // Act and Assert
            Environment env = await provider.RemoteLoadAsync("foobar\\foobar\\");
            Assert.Null(env);

        }

        [Fact]
        public async void RemoteLoadAsync_HostTimesout()
        {
            // Arrange
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider();

            // Act and Assert
            Environment env = await provider.RemoteLoadAsync("http://foo.bar:9999/app/profile");
            Assert.Null(env);

        }
        [Fact]
        public async void RemoteLoadAsync_ConfigServerReturnsNotOkStatus()
        {
            // Arrange
            var startup = new TestConfigServerStartup("",500);
            var server = TestServer.Create(startup.Configure);
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());
            string path = provider.GetConfigServerUri();

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
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());
            string path = provider.GetConfigServerUri();

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
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

            // Act and Assert
            provider.Load();
            Assert.NotNull(startup.LastRequest);
            Assert.Equal("/" + settings.Name + "/" + settings.Environment, startup.LastRequest.Path.Value);
            Assert.Equal(0, provider.Properties.Count);
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
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            settings.Name = "myName";
            server.BaseAddress = new Uri(settings.Uri);
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider(settings, server.CreateClient());

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

    }
}


