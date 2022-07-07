// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Connector.Hystrix.Test;

public class HystrixProviderServiceCollectionExtensionsTest
{
    public HystrixProviderServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddHystrixConnection_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot config = null;

        var ex =
            Assert.Throws<ArgumentNullException>(
                () => services.AddHystrixConnection(config));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 =
            Assert.Throws<ArgumentNullException>(
                () => services.AddHystrixConnection(config, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);
    }

    [Fact]
    public void AddHystrixConnection_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;

        var ex =
            Assert.Throws<ArgumentNullException>(
                () => services.AddHystrixConnection(config));
        Assert.Contains(nameof(config), ex.Message);

        var ex2 =
            Assert.Throws<ArgumentNullException>(
                () => services.AddHystrixConnection(config, "foobar"));
        Assert.Contains(nameof(config), ex2.Message);
    }

    [Fact]
    public void AddHystrixConnection_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;
        const string serviceName = null;

        var ex =
            Assert.Throws<ArgumentNullException>(
                () => services.AddHystrixConnection(config, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddHystrixConnection_NoVCAPs_AddsConfiguredConnection()
    {
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddHystrixConnection(config);

        var service = services.BuildServiceProvider().GetService<HystrixConnectionFactory>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddHystrixConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var ex =
            Assert.Throws<ConnectorException>(
                () => services.AddHystrixConnection(config, "foobar"));
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddHystrixConnection_MultipleHystrixServices_ThrowsConnectorException()
    {
        var env2 = @"
                {
                    ""p-circuit-breaker-dashboard"": [{
                        ""credentials"": {
                            ""stream"": ""https://turbine-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com"",
                            ""amqp"": {
                                ""http_api_uris"": [""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/""],
                                ""ssl"": false,
                                ""dashboard_url"": ""https://pivotal-rabbitmq.system.testcloud.com/#/login/a0f39f25-28a2-438e-a0e7-6c09d6d34dbd/1clgf5ipeop36437dmr2em4duk"",
                                ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                ""protocols"": {
                                    ""amqp"": {
                                        ""vhost"": ""06f0b204-9f95-4829-a662-844d3c3d6120"",
                                        ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                        ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                        ""port"": 5672,
                                        ""host"": ""192.168.1.55"",
                                        ""hosts"": [""192.168.1.55""],
                                        ""ssl"": false,
                                        ""uri"": ""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120"",
                                        ""uris"": [""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120""]
                                    },
                                    ""management"": {
                                        ""path"": ""/api/"",
                                        ""ssl"": false,
                                        ""hosts"": [""192.168.1.55""],
                                        ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                        ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                        ""port"": 15672,
                                        ""host"": ""192.168.1.55"",
                                        ""uri"": ""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/"",
                                        ""uris"": [""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/""]
                                    }
                                },
                                ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                ""hostname"": ""192.168.1.55"",
                                ""hostnames"": [""192.168.1.55""],
                                ""vhost"": ""06f0b204-9f95-4829-a662-844d3c3d6120"",
                                ""http_api_uri"": ""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/"",
                                ""uri"": ""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120"",
                                ""uris"": [""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120""]
                            },
                            ""dashboard"": ""https://hystrix-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com""
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""p-circuit-breaker-dashboard"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myHystrixService1"",
                        ""tags"": [
                            ""circuit-breaker"",
                            ""hystrix-amqp"",
                            ""spring-cloud""
                        ]
                    },
                    {
                        ""credentials"": {
                            ""stream"": ""https://turbine-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com"",
                            ""amqp"": {
                                ""http_api_uris"": [""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/""],
                                ""ssl"": false,
                                ""dashboard_url"": ""https://pivotal-rabbitmq.system.testcloud.com/#/login/a0f39f25-28a2-438e-a0e7-6c09d6d34dbd/1clgf5ipeop36437dmr2em4duk"",
                                ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                ""protocols"": {
                                    ""amqp"": {
                                        ""vhost"": ""06f0b204-9f95-4829-a662-844d3c3d6120"",
                                        ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                        ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                        ""port"": 5672,
                                        ""host"": ""192.168.1.55"",
                                        ""hosts"": [""192.168.1.55""],
                                        ""ssl"": false,
                                        ""uri"": ""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120"",
                                        ""uris"": [""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120""]
                                    },
                                    ""management"": {
                                        ""path"": ""/api/"",
                                        ""ssl"": false,
                                        ""hosts"": [""192.168.1.55""],
                                        ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                        ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                        ""port"": 15672,
                                        ""host"": ""192.168.1.55"",
                                        ""uri"": ""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/"",
                                        ""uris"": [""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/""]
                                    }
                                },
                                ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                ""hostname"": ""192.168.1.55"",
                                ""hostnames"": [""192.168.1.55""],
                                ""vhost"": ""06f0b204-9f95-4829-a662-844d3c3d6120"",
                                ""http_api_uri"": ""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/"",
                                ""uri"": ""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120"",
                                ""uris"": [""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120""]
                            },
                            ""dashboard"": ""https://hystrix-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com""
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""p-circuit-breaker-dashboard"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myHystrixService2"",
                        ""tags"": [
                            ""circuit-breaker"",
                            ""hystrix-amqp"",
                            ""spring-cloud""
                        ]
                    }]
                }";

        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var ex =
            Assert.Throws<ConnectorException>(
                () => services.AddHystrixConnection(config));
        Assert.Contains("Multiple", ex.Message);
    }

    [Fact]
    public void AddHystrixConnection_WithVCAPs_AddsHystrixConnectionFactory()
    {
        var env2 = @"
                {
                    ""p-circuit-breaker-dashboard"": [{
                    ""credentials"": {
                        ""stream"": ""https://turbine-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com"",
                        ""amqp"": {
                            ""http_api_uris"": [""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/""],
                            ""ssl"": false,
                            ""dashboard_url"": ""https://pivotal-rabbitmq.system.testcloud.com/#/login/a0f39f25-28a2-438e-a0e7-6c09d6d34dbd/1clgf5ipeop36437dmr2em4duk"",
                            ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                            ""protocols"": {
                                ""amqp"": {
                                    ""vhost"": ""06f0b204-9f95-4829-a662-844d3c3d6120"",
                                    ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                    ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                    ""port"": 5672,
                                    ""host"": ""192.168.1.55"",
                                    ""hosts"": [""192.168.1.55""],
                                    ""ssl"": false,
                                    ""uri"": ""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120"",
                                    ""uris"": [""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120""]
                                },
                                ""management"": {
                                    ""path"": ""/api/"",
                                    ""ssl"": false,
                                    ""hosts"": [""192.168.1.55""],
                                    ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                    ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                    ""port"": 15672,
                                    ""host"": ""192.168.1.55"",
                                    ""uri"": ""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/"",
                                    ""uris"": [""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/""]
                                }
                            },
                            ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                            ""hostname"": ""192.168.1.55"",
                            ""hostnames"": [""192.168.1.55""],
                            ""vhost"": ""06f0b204-9f95-4829-a662-844d3c3d6120"",
                            ""http_api_uri"": ""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/"",
                            ""uri"": ""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120"",
                            ""uris"": [""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120""]
                        },
                        ""dashboard"": ""https://hystrix-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com""
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""p-circuit-breaker-dashboard"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myHystrixService"",
                        ""tags"": [
                            ""circuit-breaker"",
                            ""hystrix-amqp"",
                            ""spring-cloud""
                        ]
                    }]
                }";

        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        services.AddHystrixConnection(config);

        var hystrixService = services.BuildServiceProvider().GetService<HystrixConnectionFactory>();
        Assert.NotNull(hystrixService);
        var service = hystrixService.ConnectionFactory as ConnectionFactory;
        Assert.NotNull(service);
        Assert.Equal("06f0b204-9f95-4829-a662-844d3c3d6120", service.VirtualHost);
        Assert.Equal(5672, service.Port);
        Assert.Equal("192.168.1.55", service.HostName);
        Assert.Equal("a0f39f25-28a2-438e-a0e7-6c09d6d34dbd", service.UserName);
        Assert.Equal("1clgf5ipeop36437dmr2em4duk", service.Password);
    }
}
