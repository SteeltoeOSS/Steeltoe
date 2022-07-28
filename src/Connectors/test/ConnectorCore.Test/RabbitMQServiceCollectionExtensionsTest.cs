// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Connector.RabbitMQ.Test;

public class RabbitMQServiceCollectionExtensionsTest
{
    public RabbitMQServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddRabbitMQConnection_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot config = null;

        var ex =
            Assert.Throws<ArgumentNullException>(
                () => services.AddRabbitMQConnection(config));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 =
            Assert.Throws<ArgumentNullException>(
                () => services.AddRabbitMQConnection(config, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);
    }

    [Fact]
    public void AddRabbitMQConnection_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;

        var ex =
            Assert.Throws<ArgumentNullException>(
                () => services.AddRabbitMQConnection(config));
        Assert.Contains(nameof(config), ex.Message);

        var ex2 =
            Assert.Throws<ArgumentNullException>(
                () => services.AddRabbitMQConnection(config, "foobar"));
        Assert.Contains(nameof(config), ex2.Message);
    }

    [Fact]
    public void AddRabbitMQConnection_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;
        const string serviceName = null;

        var ex =
            Assert.Throws<ArgumentNullException>(
                () => services.AddRabbitMQConnection(config, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddRabbitMQConnection_NoVCAPs_AddsConfiguredConnection()
    {
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddRabbitMQConnection(config);

        var service = services.BuildServiceProvider().GetService<IConnectionFactory>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddRabbitMQConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var ex =
            Assert.Throws<ConnectorException>(
                () => services.AddRabbitMQConnection(config, "foobar"));
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddRabbitMQConnection_MultipleRabbitMQServices_ThrowsConnectorException()
    {
        var env2 = @"
                {
                    ""p-rabbitmq"": [{
                        ""credentials"": {
                            ""uri"": ""amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-rabbitmq"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myRabbitMQService1"",
                        ""tags"": [
                            ""rabbitmq"",
                            ""amqp""
                        ]
                    }, 
                    {
                        ""credentials"": {
                            ""uri"": ""amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-Rabbit"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myRabbitMQService2"",
                        ""tags"": [
                            ""rabbitmq"",
                            ""amqp""
                        ]
                    }]
                }";

        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var ex =
            Assert.Throws<ConnectorException>(
                () => services.AddRabbitMQConnection(config));
        Assert.Contains("Multiple", ex.Message);
    }

    [Fact]
    public void AddRabbitMQConnection_MultipleRabbitMQServices_DoesNotThrow_IfNameUsed()
    {
        var env2 = @"
                {
                    ""p-rabbitmq"": [{
                        ""credentials"": {
                            ""uri"": ""amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-rabbitmq"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myRabbitMQService1"",
                        ""tags"": [
                            ""rabbitmq"",
                            ""amqp""
                        ]
                    }, 
                    {
                        ""credentials"": {
                            ""uri"": ""amqp://a:b@192.168.0.91:3306/asdf""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-Rabbit"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myRabbitMQService2"",
                        ""tags"": [
                            ""rabbitmq"",
                            ""amqp""
                        ]
                    }]
                }";

        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        services.AddRabbitMQConnection(config, "myRabbitMQService2");
        var service = services.BuildServiceProvider().GetService<IConnectionFactory>() as ConnectionFactory;
        Assert.NotNull(service);
        Assert.Equal("asdf", service.VirtualHost);
        Assert.Equal(3306, service.Port);
        Assert.Equal("192.168.0.91", service.HostName);
        Assert.Equal("a", service.UserName);
        Assert.Equal("b", service.Password);
    }

    [Fact]
    public void AddRabbitMQConnection_WithVCAPs_AddsRabbitMQConnection()
    {
        var env2 = @"
                {
                    ""p-rabbitmq"": [{
                        ""credentials"": {
                            ""uri"": ""amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-rabbitmq"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myRabbitMQService"",
                        ""tags"": [
                            ""rabbitmq"",
                            ""amqp""
                        ]
                    }]
                }";

        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        services.AddRabbitMQConnection(config);

        var service = services.BuildServiceProvider().GetService<IConnectionFactory>() as ConnectionFactory;
        Assert.NotNull(service);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", service.VirtualHost);
        Assert.Equal(3306, service.Port);
        Assert.Equal("192.168.0.90", service.HostName);
        Assert.Equal("Dd6O1BPXUHdrmzbP", service.UserName);
        Assert.Equal("7E1LxXnlH2hhlPVt", service.Password);
    }

    [Fact]
    public void AddRabbitMQConnection_AddsRabbitMQHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        services.AddRabbitMQConnection(config);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RabbitMQHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddRabbitMQConnection_DoesNotAddsRabbitMQHealthContributor_WhenCommunityHealthCheckExists()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var cm = new ConnectionStringManager(config);
        var ci = cm.Get<RabbitMQConnectionInfo>();
        services.AddHealthChecks().AddRabbitMQ(ci.ConnectionString, name: ci.Name);

        services.AddRabbitMQConnection(config);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RabbitMQHealthContributor;

        Assert.Null(healthContributor);
    }

    [Fact]
    public void AddRabbitMQConnection_AddsRabbitMQHealthContributor_WhenCommunityHealthCheckExistsAndForced()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var cm = new ConnectionStringManager(config);
        var ci = cm.Get<RabbitMQConnectionInfo>();
        services.AddHealthChecks().AddRabbitMQ(ci.ConnectionString, name: ci.Name);

        services.AddRabbitMQConnection(config, addSteeltoeHealthChecks: true);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RabbitMQHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
