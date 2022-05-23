// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Hystrix.Test
{
    public class HystrixProviderConfigurerTest
    {
        [Fact]
        public void UpdateConfiguration_WithNullHystrixRabbitMQServiceInfo_ReturnsInitialConfiguration()
        {
            var configurer = new HystrixProviderConfigurer();
            var config = new HystrixProviderConnectorOptions
            {
                Server = "localhost",
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
            Assert.Null(config.Uri);
        }

        [Fact]
        public void UpdateConfiguration_WithHystrixRabbitMQServiceInfo_UpdatesConfigurationFromServiceInfo()
        {
            var configurer = new HystrixProviderConfigurer();
            var config = new HystrixProviderConnectorOptions
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                VirtualHost = "vhost"
            };
            var si = new HystrixRabbitMQServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost", false);

            configurer.UpdateConfiguration(si, config);

            Assert.False(config.SslEnabled);
            Assert.Equal("example.com", config.Server);
            Assert.Equal(5672, config.Port);
            Assert.Equal("si_username", config.Username);
            Assert.Equal("si_password", config.Password);
            Assert.Equal("si_vhost", config.VirtualHost);
        }

        [Fact]
        public void UpdateConfiguration_WithHystrixRabbitMQSSLServiceInfo_UpdatesConfigurationFromServiceInfo()
        {
            var configurer = new HystrixProviderConfigurer();
            var config = new HystrixProviderConnectorOptions
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                VirtualHost = "vhost"
            };
            var si = new HystrixRabbitMQServiceInfo("MyId", "amqps://si_username:si_password@example.com:5671/si_vhost", false);

            configurer.UpdateConfiguration(si, config);

            Assert.True(config.SslEnabled);
            Assert.Equal("example.com", config.Server);
            Assert.Equal(5671, config.SslPort);
            Assert.Equal("si_username", config.Username);
            Assert.Equal("si_password", config.Password);
            Assert.Equal("si_vhost", config.VirtualHost);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsProvidedConnectorOptions()
        {
            var config = new HystrixProviderConnectorOptions
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                VirtualHost = "vhost"
            };

            var configurer = new HystrixProviderConfigurer();
            var opts = configurer.Configure(null, config);
            var uri = new UriInfo(opts);

            Assert.False(config.SslEnabled);
            Assert.Equal("localhost", uri.Host);
            Assert.Equal(1234, uri.Port);
            Assert.Equal("username", uri.UserName);
            Assert.Equal("password", uri.Password);
            Assert.Equal("vhost", uri.Path);
        }

        [Fact]
        public void Configure_ServiceInfoOveridesConfig_ReturnsOverriddenConnectionString()
        {
            var config = new HystrixProviderConnectorOptions
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                VirtualHost = "vhost"
            };

            var configurer = new HystrixProviderConfigurer();
            var si = new HystrixRabbitMQServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost", false);

            var opts = configurer.Configure(si, config);
            var uri = new UriInfo(opts);

            Assert.Equal("example.com", uri.Host);
            Assert.Equal(5672, uri.Port);
            Assert.Equal("si_username", uri.UserName);
            Assert.Equal("si_password", uri.Password);
            Assert.Equal("si_vhost", uri.Path);
        }

        [Fact]
        public void Configure_SSLServiceInfoOveridesConfig_ReturnsOverriddenConnectionString()
        {
            var config = new HystrixProviderConnectorOptions
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                VirtualHost = "vhost"
            };

            var configurer = new HystrixProviderConfigurer();
            var si = new HystrixRabbitMQServiceInfo("MyId", "amqps://si_username:si_password@example.com/si_vhost", false);

            var opts = configurer.Configure(si, config);
            var uri = new UriInfo(opts);

            Assert.Equal("example.com", uri.Host);
            Assert.Equal("amqps", uri.Scheme);
            Assert.Equal("si_username", uri.UserName);
            Assert.Equal("si_password", uri.Password);
            Assert.Equal("si_vhost", uri.Path);
        }
    }
}
