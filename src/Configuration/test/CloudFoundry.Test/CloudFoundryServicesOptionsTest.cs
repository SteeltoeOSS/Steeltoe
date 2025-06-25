// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryServicesOptionsTest
{
    [Fact]
    public async Task NoVcapServicesConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddCloudFoundryOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryServicesOptions>>();
        CloudFoundryServicesOptions options = optionsMonitor.CurrentValue;

        options.Services.Should().BeEmpty();
    }

    [Fact]
    public async Task SingleServiceConfiguration()
    {
        const string vcapServicesJson = """
            {
                "p-config-server": [{
                    "credentials": {
                        "access_token_uri": "https://p-spring-cloud-services.uaa.wise.com/oauth/token",
                        "client_id": "p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef",
                        "client_secret": "e8KF1hXvAnGd",
                        "uri": "http://localhost:8888"
                    },
                    "label": "p-config-server",
                    "name": "My Config Server",
                    "plan": "standard",
                    "tags": ["configuration","spring-cloud"]
                }]
            }
            """;

        using var scope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServicesJson);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddCloudFoundryOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryServicesOptions>>();
        CloudFoundryServicesOptions options = optionsMonitor.CurrentValue;

        options.Services.Should().ContainSingle();
        CloudFoundryService service = options.Services.Should().ContainKey("p-config-server").WhoseValue.Should().ContainSingle().Subject;

        service.Label.Should().Be("p-config-server");
        service.Name.Should().Be("My Config Server");
        service.Plan.Should().Be("standard");

        service.Tags.Should().HaveCount(2);
        service.Tags.Should().Contain("configuration");
        service.Tags.Should().Contain("spring-cloud");

        service.Credentials.Should().HaveCount(4);
        service.Credentials.Should().ContainKey("access_token_uri").WhoseValue.Value.Should().Be("https://p-spring-cloud-services.uaa.wise.com/oauth/token");
        service.Credentials.Should().ContainKey("client_id").WhoseValue.Value.Should().Be("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef");
        service.Credentials.Should().ContainKey("client_secret").WhoseValue.Value.Should().Be("e8KF1hXvAnGd");
        service.Credentials.Should().ContainKey("uri").WhoseValue.Value.Should().Be("http://localhost:8888");
    }

    [Fact]
    public async Task ComplexSingleServiceConfiguration()
    {
        const string vcapServicesJson = """
            {
                "p-rabbitmq": [{
                    "name": "rabbitmq",
                    "label": "p-rabbitmq",
                    "tags": [
                        "rabbitmq",
                        "messaging",
                        "message-queue",
                        "amqp",
                        "stomp",
                        "mqtt",
                        "pivotal"
                    ],
                    "plan": "standard",
                    "credentials": {
                        "http_api_uris": [
                            "https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@pivotal-rabbitmq.system.test-cloud.com/api/"
                        ],
                        "ssl": false,
                        "dashboard_url": "https://pivotal-rabbitmq.system.test-cloud.com/#/login/268371bd-07e5-46f3-aec7-d1633ae20bbb/3fnpvbqm0djq5jl9fp6fc697f4",
                        "password": "3fnpvbqm0djq5jl9fp6fc697f4",
                        "protocols": {
                            "management": {
                                "path": "/api/",
                                "ssl": false,
                                "hosts": ["192.168.0.97"],
                                "password": "3fnpvbqm0djq5jl9fp6fc697f4",
                                "username": "268371bd-07e5-46f3-aec7-d1633ae20bbb",
                                "port": 15672,
                                "host": "192.168.0.97",
                                "uri": "https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:15672/api/",
                                "uris": ["https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:15672/api/"]
                            },
                            "amqp": {
                                "vhost": "2260a117-cf28-4725-86dd-37b3b8971052",
                                "username": "268371bd-07e5-46f3-aec7-d1633ae20bbb",
                                "password": "3fnpvbqm0djq5jl9fp6fc697f4",
                                "port": 5672,
                                "host": "192.168.0.97",
                                "hosts": [ "192.168.0.97"],
                                "ssl": false,
                                "uri": "amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052",
                                "uris": ["amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052"]
                            }
                        },
                        "username": "268371bd-07e5-46f3-aec7-d1633ae20bbb",
                        "hostname": "192.168.0.97",
                        "hostnames": [
                            "192.168.0.97"
                            ],
                        "vhost": "2260a117-cf28-4725-86dd-37b3b8971052",
                        "http_api_uri": "https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@pivotal-rabbitmq.system.test-cloud.com/api/",
                        "uri": "amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97/2260a117-cf28-4725-86dd-37b3b8971052",
                        "uris": [
                            "amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97/2260a117-cf28-4725-86dd-37b3b8971052"
                        ]
                    }
                }]
            }
            """;

        using var scope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServicesJson);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddCloudFoundryOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryServicesOptions>>();
        CloudFoundryServicesOptions options = optionsMonitor.CurrentValue;

        options.Services.Should().ContainSingle();
        CloudFoundryService service = options.Services.Should().ContainKey("p-rabbitmq").WhoseValue.Should().ContainSingle().Subject;

        service.Label.Should().Be("p-rabbitmq");
        service.Name.Should().Be("rabbitmq");
        service.Plan.Should().Be("standard");

        service.Tags.Should().HaveCount(7);
        service.Tags.Should().Contain("rabbitmq");
        service.Tags.Should().Contain("pivotal");

        service.Credentials.Should().HaveCount(12);

        service.Credentials.Should().ContainKey("dashboard_url").WhoseValue.Value.Should()
            .Be("https://pivotal-rabbitmq.system.test-cloud.com/#/login/268371bd-07e5-46f3-aec7-d1633ae20bbb/3fnpvbqm0djq5jl9fp6fc697f4");

        service.Credentials.Should().ContainKey("username").WhoseValue.Value.Should().Be("268371bd-07e5-46f3-aec7-d1633ae20bbb");
        service.Credentials.Should().ContainKey("password").WhoseValue.Value.Should().Be("3fnpvbqm0djq5jl9fp6fc697f4");

        CloudFoundryCredentials? amqp = service.Credentials.Should().ContainKey("protocols").WhoseValue.Should().ContainKey("amqp").WhoseValue;

        amqp.Should().ContainKey("username").WhoseValue.Value.Should().Be("268371bd-07e5-46f3-aec7-d1633ae20bbb");
        amqp.Should().ContainKey("password").WhoseValue.Value.Should().Be("3fnpvbqm0djq5jl9fp6fc697f4");

        amqp.Should().ContainKey("uris").WhoseValue.Should().ContainKey("0").WhoseValue.Value.Should().Be(
            "amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052");
    }

    [Fact]
    public async Task MultipleServicesConfiguration()
    {
        const string vcapServicesJson = """
            {
                "p-mysql": [
                {
                    "name": "mySql1",
                    "label": "p-mysql",
                    "tags": ["mysql","relational"],
                    "plan": "100mb-dev",
                    "credentials": {
                        "hostname": "192.168.0.97",
                        "port": 3306,
                        "name": "cf_0f5dda44_e678_4727_993f_30e6d455cc31",
                        "username": "9vD0Mtk3wFFuaaaY",
                        "password": "Cjn4HsAiKV8sImst",
                        "uri": "mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true",
                        "jdbcUrl": "jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst"
                    }
                }],
                "p-config-server": [{
                "credentials": {
                    "access_token_uri": "https://p-spring-cloud-services.uaa.wise.com/oauth/token",
                    "client_id": "p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef",
                    "client_secret": "e8KF1hXvAnGd",
                    "uri": "http://localhost:8888"
                },
                "label": "p-config-server",
                "name": "My Config Server",
                "plan": "standard",
                "tags": ["configuration","spring-cloud"]
            }]
            }
            """;

        using var scope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServicesJson);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddCloudFoundryOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryServicesOptions>>();
        CloudFoundryServicesOptions options = optionsMonitor.CurrentValue;

        options.Services.Should().HaveCount(2);
        CloudFoundryService service1 = options.Services.Should().ContainKey("p-mysql").WhoseValue.Should().ContainSingle().Subject;
        service1.Label.Should().Be("p-mysql");
        service1.Credentials.Should().ContainKey("hostname").WhoseValue.Value.Should().Be("192.168.0.97");
        service1.Credentials.Should().ContainKey("port").WhoseValue.Value.Should().Be("3306");
        service1.Credentials.Should().ContainKey("name").WhoseValue.Value.Should().Be("cf_0f5dda44_e678_4727_993f_30e6d455cc31");

        CloudFoundryService service2 = options.Services.Should().ContainKey("p-config-server").WhoseValue.Should().ContainSingle().Subject;

        service2.Label.Should().Be("p-config-server");
        service2.Name.Should().Be("My Config Server");
        service2.Plan.Should().Be("standard");

        service2.Tags.Should().HaveCount(2);
        service2.Tags.Should().Contain("configuration");
        service2.Tags.Should().Contain("spring-cloud");

        service2.Credentials.Should().HaveCount(4);
        service2.Credentials.Should().ContainKey("access_token_uri").WhoseValue.Value.Should().Be("https://p-spring-cloud-services.uaa.wise.com/oauth/token");
        service2.Credentials.Should().ContainKey("client_id").WhoseValue.Value.Should().Be("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef");
        service2.Credentials.Should().ContainKey("client_secret").WhoseValue.Value.Should().Be("e8KF1hXvAnGd");
        service2.Credentials.Should().ContainKey("uri").WhoseValue.Value.Should().Be("http://localhost:8888");

        IList<CloudFoundryService> allServices = options.GetAllServices();
        allServices.Should().HaveCount(2);
        allServices.Should().ContainSingle(service => service.Label == "p-mysql");
        allServices.Should().ContainSingle(service => service.Label == "p-config-server");

        IList<CloudFoundryService> typedServices1 = options.GetServicesOfType("p-mysql");
        typedServices1.Should().ContainSingle();
        typedServices1.Should().ContainSingle(service => service.Label == "p-mysql");

        IList<CloudFoundryService> typedServices2 = options.GetServicesOfType("p-config-server");
        typedServices2.Should().ContainSingle();
        typedServices2.Should().ContainSingle(service => service.Label == "p-config-server");
    }

    [Fact]
    public async Task MultipleServicesOfSameTypeConfiguration()
    {
        const string vcapServicesJson = """
            {
                "p-mysql": [
                {
                    "name": "mySql1",
                    "label": "p-mysql",
                    "tags": ["mysql","relational"],
                    "plan": "100mb-dev",
                    "credentials": {
                        "hostname": "192.168.0.97",
                        "port": 3306,
                        "name": "cf_0f5dda44_e678_4727_993f_30e6d455cc31",
                        "username": "9vD0Mtk3wFFuaaaY",
                        "password": "Cjn4HsAiKV8sImst",
                        "uri": "mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true",
                        "jdbcUrl": "jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst"
                    }
                },
                {
                    "name": "mySql2",
                    "label": "p-mysql",
                    "tags": ["mysql","relational"],
                    "plan": "100mb-dev",
                    "credentials": {
                        "hostname": "192.168.0.98",
                        "port": 3307,
                        "name": "cf_0f5dda44_e678_4727_993f_30e6d455cc32",
                        "username": "9vD0Mtk3wFFuaaaY",
                        "password": "Cjn4HsAiKV8sImst",
                        "uri": "mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true",
                        "jdbcUrl": "jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst"
                    }
                }]
            }
            """;

        using var scope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServicesJson);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddCloudFoundryOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryServicesOptions>>();
        CloudFoundryServicesOptions options = optionsMonitor.CurrentValue;

        options.Services.Should().ContainSingle();
        options.Services.Should().ContainKey("p-mysql").WhoseValue.Should().HaveCount(2);

        CloudFoundryService service1 = options.Services["p-mysql"].Should().ContainSingle(service => service.Name == "mySql1").Subject;
        service1.Label.Should().Be("p-mysql");
        service1.Credentials.Should().ContainKey("hostname").WhoseValue.Value.Should().Be("192.168.0.97");
        service1.Credentials.Should().ContainKey("port").WhoseValue.Value.Should().Be("3306");
        service1.Credentials.Should().ContainKey("name").WhoseValue.Value.Should().Be("cf_0f5dda44_e678_4727_993f_30e6d455cc31");

        CloudFoundryService service2 = options.Services["p-mysql"].Should().ContainSingle(service => service.Name == "mySql2").Subject;
        service2.Label.Should().Be("p-mysql");
        service2.Credentials.Should().ContainKey("hostname").WhoseValue.Value.Should().Be("192.168.0.98");
        service2.Credentials.Should().ContainKey("port").WhoseValue.Value.Should().Be("3307");
        service2.Credentials.Should().ContainKey("name").WhoseValue.Value.Should().Be("cf_0f5dda44_e678_4727_993f_30e6d455cc32");

        IList<CloudFoundryService> allServices = options.GetAllServices();
        allServices.Should().HaveCount(2);
        allServices.Should().OnlyContain(service => service.Label == "p-mysql");

        IList<CloudFoundryService> typedServices = options.GetServicesOfType("p-mysql");
        typedServices.Should().HaveCount(2);
        typedServices.Should().OnlyContain(service => service.Label == "p-mysql");
    }
}
