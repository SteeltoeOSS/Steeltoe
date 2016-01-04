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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Xml;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.Configuration.CommandLine;

namespace Spring.Extensions.Configuration.Server.Test
{
    public class ConfigServerClientSettingsTest
    {
        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings();

            // Act and Assert
            ConfigServerTestHelpers.VerifyDefaults(settings);

        }

        [Fact]
        public void ProvidersConstructor_InitializedWithDefaultsWhenNull()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings(null);

            // Act and Assert
            ConfigServerTestHelpers.VerifyDefaults(settings);
        }

        [Fact]
        public void ProvidersConstructor_InitializedWithJsonProvider()
        {
            // Arrange
            var json = @"
{
    'spring': {
      'cloud': {
        'config': {
            'uri': 'http://foo.com:9999',
            'enabled': false,
            'failFast': true,
            'label': 'myLabel',
            'name': 'myName',
            'username': 'myUsername',
            'password': 'myPassword'
        }
      }
    }
}";
            var path = ConfigServerTestHelpers.CreateTempFile(json);
            var provider = new JsonConfigurationProvider(path);
            provider.Load();
            List<IConfigurationProvider> providers = new List<IConfigurationProvider>() { provider };
            ConfigServerClientSettings settings = new ConfigServerClientSettings(providers.AsEnumerable<IConfigurationProvider>());

            // Act and Assert
            Assert.False(settings.Enabled);
            Assert.True(settings.FailFast);
            Assert.Equal(settings.Uri, "http://foo.com:9999");
            Assert.Equal(settings.Environment, "Development");
            Assert.Equal(settings.Name,"myName");
            Assert.Equal(settings.Label, "myLabel");
            Assert.Equal(settings.Username, "myUsername");
            Assert.Equal(settings.Password, "myPassword");

        }

        [Fact]
        public void ProvidersConstructor_InitializedWithXmlProvider()
        {
            // Arrange
            var xml = @"
<settings>
    <spring>
      <cloud>
        <config>
            <uri>http://foo.com:9999</uri>
            <enabled>false</enabled>
            <failFast>true</failFast>
            <label>myLabel</label>
            <name>myName</name>
            <username>myUsername</username>
            <password>myPassword</password>
        </config>
      </cloud>
    </spring>
</settings>";
            var path = ConfigServerTestHelpers.CreateTempFile(xml);
            var provider = new XmlConfigurationProvider(path);
            provider.Load();
            List<IConfigurationProvider> providers = new List<IConfigurationProvider>() { provider };
            ConfigServerClientSettings settings = new ConfigServerClientSettings(providers.AsEnumerable<IConfigurationProvider>());

            // Act and Assert
            Assert.False(settings.Enabled);
            Assert.True(settings.FailFast);
            Assert.Equal(settings.Uri, "http://foo.com:9999");
            Assert.Equal(settings.Environment, "Development");
            Assert.Equal(settings.Name, "myName");
            Assert.Equal(settings.Label, "myLabel");
            Assert.Equal(settings.Username, "myUsername");
            Assert.Equal(settings.Password, "myPassword");

        }
        [Fact]
        public void ProvidersConstructor_InitializedWithIniProvider()
        {
            // Arrange
            var ini = @"
[spring:cloud:config]
    uri=http://foo.com:9999
    enabled=false
    failFast=true
    label=myLabel
    name=myName
    username=myUsername
    password=myPassword
";
            var path = ConfigServerTestHelpers.CreateTempFile(ini);
            var provider = new IniConfigurationProvider(path);
            provider.Load();
            List<IConfigurationProvider> providers = new List<IConfigurationProvider>() { provider };
            ConfigServerClientSettings settings = new ConfigServerClientSettings(providers.AsEnumerable<IConfigurationProvider>());

            // Act and Assert
            Assert.False(settings.Enabled);
            Assert.True(settings.FailFast);
            Assert.Equal(settings.Uri, "http://foo.com:9999");
            Assert.Equal(settings.Environment, "Development");
            Assert.Equal(settings.Name, "myName");
            Assert.Equal(settings.Label, "myLabel");
            Assert.Equal(settings.Username, "myUsername");
            Assert.Equal(settings.Password, "myPassword");

        }

        [Fact]
        public void ProvidersConstructor_InitializedWithCommandLineProvider()
        {
            // Arrange
            var args = new string[]
                {
                    "spring:cloud:config:enabled=false",
                    "--spring:cloud:config:failFast=true",
                    "/spring:cloud:config:uri=http://foo.com:9999",
                    "--spring:cloud:config:name", "myName",
                    "/spring:cloud:config:label", "myLabel",
                    "--spring:cloud:config:username", "myUsername",
                    "--spring:cloud:config:password", "myPassword"
                };
            var provider = new CommandLineConfigurationProvider(args);
            provider.Load();
            List<IConfigurationProvider> providers = new List<IConfigurationProvider>() { provider };
            ConfigServerClientSettings settings = new ConfigServerClientSettings(providers.AsEnumerable<IConfigurationProvider>());

            // Act and Assert
            Assert.False(settings.Enabled);
            Assert.True(settings.FailFast);
            Assert.Equal(settings.Uri, "http://foo.com:9999");
            Assert.Equal(settings.Environment, "Development");
            Assert.Equal(settings.Name, "myName");
            Assert.Equal(settings.Label, "myLabel");
            Assert.Equal(settings.Username, "myUsername");
            Assert.Equal(settings.Password, "myPassword");

        }

    }

}
