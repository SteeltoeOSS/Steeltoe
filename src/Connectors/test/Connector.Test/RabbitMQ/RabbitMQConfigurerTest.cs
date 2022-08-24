// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.RabbitMQ.Test;

public class RabbitMQConfigurerTest
{
    [Fact]
    public void UpdateConfiguration_WithNullRabbitMQServiceInfo_ReturnsInitialConfiguration()
    {
        var configurer = new RabbitMQProviderConfigurer();

        var options = new RabbitMQProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            VirtualHost = "vhost"
        };

        configurer.UpdateConfiguration(null, options);

        Assert.Equal("localhost", options.Server);
        Assert.Equal(1234, options.Port);
        Assert.Equal("username", options.Username);
        Assert.Equal("password", options.Password);
        Assert.Equal("vhost", options.VirtualHost);
        Assert.Null(options.Uri);
    }

    [Fact]
    public void UpdateConfiguration_WithRabbitMQServiceInfo_UpdatesConfigurationFromServiceInfo()
    {
        var configurer = new RabbitMQProviderConfigurer();

        var options = new RabbitMQProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            VirtualHost = "vhost"
        };

        var si = new RabbitMQServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost");

        configurer.UpdateConfiguration(si, options);

        Assert.False(options.SslEnabled);
        Assert.Equal("example.com", options.Server);
        Assert.Equal(5672, options.Port);
        Assert.Equal("si_username", options.Username);
        Assert.Equal("si_password", options.Password);
        Assert.Equal("si_vhost", options.VirtualHost);
    }

    [Fact]
    public void UpdateConfiguration_WithRabbitMQ_SSLServiceInfo_UpdatesConfigurationFromServiceInfo()
    {
        var configurer = new RabbitMQProviderConfigurer();

        var options = new RabbitMQProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            VirtualHost = "vhost"
        };

        var si = new RabbitMQServiceInfo("MyId", "amqps://si_username:si_password@example.com:5671/si_vhost");

        configurer.UpdateConfiguration(si, options);

        Assert.True(options.SslEnabled);
        Assert.Equal("example.com", options.Server);
        Assert.Equal(5671, options.SslPort);
        Assert.Equal("si_username", options.Username);
        Assert.Equal("si_password", options.Password);
        Assert.Equal("si_vhost", options.VirtualHost);
    }

    [Fact]
    public void Configure_NoServiceInfo_ReturnsProvidedConnectorOptions()
    {
        var options = new RabbitMQProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            VirtualHost = "vhost"
        };

        var configurer = new RabbitMQProviderConfigurer();
        string opts = configurer.Configure(null, options);
        var uri = new UriInfo(opts);

        Assert.False(options.SslEnabled);
        Assert.Equal("localhost", uri.Host);
        Assert.Equal(1234, uri.Port);
        Assert.Equal("username", uri.UserName);
        Assert.Equal("password", uri.Password);
        Assert.Equal("vhost", uri.Path);
    }

    [Fact]
    public void Configure_ServiceInfoOverridesConfig_ReturnsOverriddenConnectionString()
    {
        var options = new RabbitMQProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            VirtualHost = "vhost"
        };

        var configurer = new RabbitMQProviderConfigurer();
        var si = new RabbitMQServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost");

        string opts = configurer.Configure(si, options);
        var uri = new UriInfo(opts);

        Assert.Equal("example.com", uri.Host);
        Assert.Equal(5672, uri.Port);
        Assert.Equal("si_username", uri.UserName);
        Assert.Equal("si_password", uri.Password);
        Assert.Equal("si_vhost", uri.Path);
    }

    [Fact]
    public void Configure_SSLServiceInfoOverridesConfig_ReturnsOverriddenConnectionString()
    {
        var options = new RabbitMQProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1234,
            Username = "username",
            Password = "password",
            VirtualHost = "vhost"
        };

        var configurer = new RabbitMQProviderConfigurer();
        var si = new RabbitMQServiceInfo("MyId", "amqps://si_username:si_password@example.com/si_vhost");

        string configuration = configurer.Configure(si, options);
        var uri = new UriInfo(configuration);

        Assert.Equal("example.com", uri.Host);
        Assert.Equal("amqps", uri.Scheme);
        Assert.Equal("si_username", uri.UserName);
        Assert.Equal("si_password", uri.Password);
        Assert.Equal("si_vhost", uri.Path);
    }
}
