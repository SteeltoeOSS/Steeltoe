// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connector.RabbitMQ;
using Xunit;

namespace Steeltoe.Connector.Test.RabbitMQ;

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
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddRabbitMQConnection(configurationRoot));
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddRabbitMQConnection(configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRabbitMQConnection_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddRabbitMQConnection(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddRabbitMQConnection(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRabbitMQConnection_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddRabbitMQConnection(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRabbitMQConnection_NoVCAPs_AddsConfiguredConnection()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddRabbitMQConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<IConnectionFactory>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddRabbitMQConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddRabbitMQConnection(configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRabbitMQConnection_MultipleRabbitMQServices_ThrowsConnectorException()
    {
        const string env2 = @"
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
        IConfigurationRoot configurationRoot = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddRabbitMQConnection(configurationRoot));
        Assert.Contains("Multiple", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRabbitMQConnection_MultipleRabbitMQServices_DoesNotThrow_IfNameUsed()
    {
        const string env2 = @"
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
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddRabbitMQConnection(configurationRoot, "myRabbitMQService2");
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
        const string env2 = @"
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
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddRabbitMQConnection(configurationRoot);

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
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddRabbitMQConnection(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RabbitMQHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddRabbitMQConnection_DoesNotAddsRabbitMQHealthContributor_WhenCommunityHealthCheckExists()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<RabbitMQConnectionInfo>();
        services.AddHealthChecks().AddRabbitMQ(ci.ConnectionString, name: ci.Name);

        services.AddRabbitMQConnection(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RabbitMQHealthContributor;

        Assert.Null(healthContributor);
    }

    [Fact]
    public void AddRabbitMQConnection_AddsRabbitMQHealthContributor_WhenCommunityHealthCheckExistsAndForced()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<RabbitMQConnectionInfo>();
        services.AddHealthChecks().AddRabbitMQ(ci.ConnectionString, name: ci.Name);

        services.AddRabbitMQConnection(configurationRoot, addSteeltoeHealthChecks: true);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RabbitMQHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
