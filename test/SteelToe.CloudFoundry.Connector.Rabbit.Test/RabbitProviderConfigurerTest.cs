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

using SteelToe.CloudFoundry.Connector.Services;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.Rabbit.Test
{
    public class RabbitProviderConfigurerTest {
        [Fact]
        public void UpdateConfiguration_WithNullRabbitServiceInfo_ReturnsInitialConfiguration()
        {
            RabbitProviderConfigurer configurer = new RabbitProviderConfigurer();
            RabbitProviderConnectorOptions config = new RabbitProviderConnectorOptions()
            {
                Server ="localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                VirtualHost = "vhost"
            };
            configurer.UpdateConfiguration(null, config);

            Assert.Equal("localhost", config.Server);
            Assert.Equal(1234, config.Port);
            Assert.Equal("username", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Equal("vhost", config.VirtualHost);
            Assert.Equal(null, config.Uri);
        }

        [Fact]
        public void UpdateConfiguration_WithRabbitServiceInfo_UpdatesConfigurationFromServiceInfo()
        {
            RabbitProviderConfigurer configurer = new RabbitProviderConfigurer();
            RabbitProviderConnectorOptions config = new RabbitProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                VirtualHost = "vhost"
            };
            RabbitServiceInfo si = new RabbitServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost");

            configurer.UpdateConfiguration(si, config);

            Assert.Equal("example.com", config.Server);
            Assert.Equal(5672, config.Port);
            Assert.Equal("si_username", config.Username);
            Assert.Equal("si_password", config.Password);
            Assert.Equal("si_vhost", config.VirtualHost);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsProvidedConnectorOptions()
        {
            RabbitProviderConnectorOptions config = new RabbitProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                VirtualHost = "vhost"
            };

            RabbitProviderConfigurer configurer = new RabbitProviderConfigurer();
            var opts = configurer.Configure(null, config);
            var uri = new UriInfo(opts);

            Assert.Equal(uri.Host, "localhost");
            Assert.Equal(uri.Port, 1234);
            Assert.Equal(uri.UserName, "username");
            Assert.Equal(uri.Password, "password");
            Assert.Equal(uri.Path, "vhost");
        }

        [Fact]
        public void Configure_ServiceInfoOveridesConfig_ReturnsOverriddenConnectionString()
            {
            RabbitProviderConnectorOptions config = new RabbitProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                VirtualHost = "vhost"
            };

            RabbitProviderConfigurer configurer = new RabbitProviderConfigurer();
            RabbitServiceInfo si = new RabbitServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost");

            var opts = configurer.Configure(si, config);
            var uri = new UriInfo(opts);

            Assert.Equal(uri.Host, "example.com");
            Assert.Equal(uri.Port, 5672);
            Assert.Equal(uri.UserName, "si_username");
            Assert.Equal(uri.Password, "si_password");
            Assert.Equal(uri.Path, "si_vhost");
        }
    }
}
