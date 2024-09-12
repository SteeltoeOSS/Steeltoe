// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Connectors.Redis;

namespace Steeltoe.Connectors.Test.Redis;

public sealed class RedisConnectorTest
{
    private const string MultiVcapServicesJson = """
        {
          "p-redis": [
            {
              "label": "p-redis",
              "provider": null,
              "plan": "shared-vm",
              "name": "myRedisServiceOne",
              "tags": [
                "pivotal",
                "redis"
              ],
              "instance_guid": "a9eb9256-73fe-4d3a-92d2-c91bcb1a739e",
              "instance_name": "myRedisServiceOne",
              "binding_guid": "3fde92a3-83b2-48de-bd30-958f632c3e20",
              "binding_name": null,
              "credentials": {
                "host": "10.0.4.17",
                "password": "36c5b850-3b44-4bcb-8b2f-510eeb9b1c6e",
                "port": 34029
              },
              "syslog_drain_url": null,
              "volume_mounts": []
            },
            {
              "label": "p-redis",
              "provider": null,
              "plan": "shared-vm",
              "name": "myRedisServiceTwo",
              "tags": [
                "pivotal",
                "redis"
              ],
              "instance_guid": "415cbd98-18a2-4ebb-966a-d57d82425724",
              "instance_name": "myRedisServiceTwo",
              "binding_guid": "012f540d-c9fc-4fc4-98ae-bcda7e6e3830",
              "binding_name": null,
              "credentials": {
                "host": "10.0.4.17",
                "password": "aa786395-98c3-4e7e-aee4-ca02e5a8590a",
                "port": 44369
              },
              "syslog_drain_url": null,
              "volume_mounts": []
            }
          ]
        }
        """;

    private const string SingleVcapServicesJson = """
        {
          "p-redis": [
            {
              "label": "p-redis",
              "provider": null,
              "plan": "shared-vm",
              "name": "myRedisService",
              "tags": [
                "pivotal",
                "redis"
              ],
              "instance_guid": "3bdc54ae-9e8e-45b1-8f80-5ec0a73505bf",
              "instance_name": "myRedisService",
              "binding_guid": "776f5be6-c840-405f-8728-71563f1bff27",
              "binding_name": null,
              "credentials": {
                "host": "10.0.4.17",
                "password": "269493d4-579a-42f0-b43b-42e83741517d",
                "port": 37357
              },
              "syslog_drain_url": null,
              "volume_mounts": []
            }
          ]
        }
        """;

    [Fact]
    public async Task Binds_options_without_service_bindings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:myRedisServiceOne:ConnectionString"] = "server1:6380,keepAlive=30",
            ["Steeltoe:Client:Redis:myRedisServiceTwo:ConnectionString"] = "server2:6380,allowAdmin=true"
        });

        builder.AddRedis();

        await using WebApplication app = builder.Build();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        var optionsSnapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<RedisOptions>>();

        RedisOptions optionsOne = optionsSnapshot.Get("myRedisServiceOne");
        optionsOne.ConnectionString.Should().Be("server1:6380,keepAlive=30");

        RedisOptions optionsTwo = optionsSnapshot.Get("myRedisServiceTwo");
        optionsTwo.ConnectionString.Should().Be("server2:6380,allowAdmin=true");
    }

    [Fact]
    public async Task Binds_options_with_CloudFoundry_service_bindings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(MultiVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:myRedisServiceOne:ConnectionString"] = "localhost:12345,keepAlive=30,user=admin"
        });

        builder.AddRedis();

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<RedisOptions>>();

        RedisOptions optionsOne = optionsMonitor.Get("myRedisServiceOne");
        optionsOne.ConnectionString.Should().Be("10.0.4.17:34029,keepAlive=30,password=36c5b850-3b44-4bcb-8b2f-510eeb9b1c6e");

        RedisOptions optionsTwo = optionsMonitor.Get("myRedisServiceTwo");
        optionsTwo.ConnectionString.Should().Be("10.0.4.17:44369,password=aa786395-98c3-4e7e-aee4-ca02e5a8590a");
    }

    [Fact]
    public async Task Binds_options_with_Kubernetes_service_bindings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeDirectory("db");
        fileProvider.IncludeFile("db/provider", "bitnami");
        fileProvider.IncludeFile("db/type", "redis");
        fileProvider.IncludeFile("db/host", "10.0.111.168");
        fileProvider.IncludeFile("db/port", "6379");
        fileProvider.IncludeFile("db/password", "v5gjxPDxq4lacijzEus9vGi0cJh0tsOE");

        var reader = new KubernetesMemoryServiceBindingsReader(fileProvider);
        builder.Configuration.AddKubernetesServiceBindings(reader);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:db:ConnectionString"] = "localhost:12345,keepAlive=30,user=admin"
        });

        builder.AddRedis();

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<RedisOptions>>();

        RedisOptions dbOptions = optionsMonitor.Get("db");

        dbOptions.ConnectionString.Should().Be("10.0.111.168:6379,keepAlive=30,password=v5gjxPDxq4lacijzEus9vGi0cJh0tsOE");
    }

    [Fact]
    public async Task Registers_ConnectorFactory_for_IConnectionMultiplexer()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:myRedisServiceOne:ConnectionString"] = "server1:6380,keepAlive=30",
            ["Steeltoe:Client:Redis:myRedisServiceTwo:ConnectionString"] = "server2:6380,allowAdmin=true"
        });

        builder.AddRedis(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
                RedisOptions options = optionsMonitor.Get(serviceBindingName);

                return GetMockedConnectionMultiplexer(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<RedisOptions, IConnectionMultiplexer>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain("myRedisServiceOne");
        connectorFactory.ServiceBindingNames.Should().Contain("myRedisServiceTwo");

        IConnectionMultiplexer connectionOne = connectorFactory.Get("myRedisServiceOne").GetConnection();
        connectionOne.Configuration.Should().Be("server1:6380,keepAlive=30");

        IConnectionMultiplexer connectionTwo = connectorFactory.Get("myRedisServiceTwo").GetConnection();
        connectionTwo.Configuration.Should().Be("server2:6380,allowAdmin=true");

        IConnectionMultiplexer connectionOneAgain = connectorFactory.Get("myRedisServiceOne").GetConnection();
        connectionOneAgain.Should().BeSameAs(connectionOne);
    }

    [Fact]
    public async Task Registers_ConnectorFactory_for_IDistributedCache()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:myRedisServiceOne:ConnectionString"] = "server1:6380,keepAlive=30",
            ["Steeltoe:Client:Redis:myRedisServiceTwo:ConnectionString"] = "server2:6380,allowAdmin=true"
        });

        builder.AddRedis(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
                RedisOptions options = optionsMonitor.Get(serviceBindingName);

                return GetMockedConnectionMultiplexer(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<RedisOptions, IDistributedCache>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain("myRedisServiceOne");
        connectorFactory.ServiceBindingNames.Should().Contain("myRedisServiceTwo");

        var connectionOne = (RedisCache)connectorFactory.Get("myRedisServiceOne").GetConnection();
        IConnectionMultiplexer connectionMultiplexerOne = await ExtractUnderlyingMultiplexerFromRedisCacheAsync(connectionOne);
        connectionMultiplexerOne.Configuration.Should().Be("server1:6380,keepAlive=30");

        var connectionTwo = (RedisCache)connectorFactory.Get("myRedisServiceTwo").GetConnection();
        IConnectionMultiplexer connectionMultiplexerTwo = await ExtractUnderlyingMultiplexerFromRedisCacheAsync(connectionTwo);
        connectionMultiplexerTwo.Configuration.Should().Be("server2:6380,allowAdmin=true");

        IDistributedCache connectionOneAgain = connectorFactory.Get("myRedisServiceOne").GetConnection();
        connectionOneAgain.Should().BeSameAs(connectionOne);
    }

    [Fact]
    public async Task Registers_HealthContributors()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:myRedisServiceOne:ConnectionString"] = "server1:6380,keepAlive=30",
            ["Steeltoe:Client:Redis:myRedisServiceTwo:ConnectionString"] = "server2:6380,allowAdmin=true"
        });

        builder.AddRedis(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
                RedisOptions options = optionsMonitor.Get(serviceBindingName);

                return GetMockedConnectionMultiplexer(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        IHealthContributor[] healthContributors = app.Services.GetServices<IHealthContributor>().ToArray();
        RedisHealthContributor[] redisHealthContributors = healthContributors.Should().AllBeOfType<RedisHealthContributor>().Subject.ToArray();
        redisHealthContributors.Should().HaveCount(2);

        redisHealthContributors[0].Id.Should().Be("Redis");
        redisHealthContributors[0].ServiceName.Should().Be("myRedisServiceOne");
        redisHealthContributors[0].Host.Should().Be("server1");

        redisHealthContributors[1].Id.Should().Be("Redis");
        redisHealthContributors[1].ServiceName.Should().Be("myRedisServiceTwo");
        redisHealthContributors[1].Host.Should().Be("server2");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.AddRedis(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
                RedisOptions options = optionsMonitor.Get(serviceBindingName);

                return GetMockedConnectionMultiplexer(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<RedisOptions, IConnectionMultiplexer>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain(string.Empty);
        connectorFactory.ServiceBindingNames.Should().Contain("myRedisService");

        RedisOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().NotBeNullOrEmpty();

        RedisOptions namedOptions = connectorFactory.Get("myRedisService").Options;
        namedOptions.ConnectionString.Should().Be(defaultOptions.ConnectionString);

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_default_client_binding_found()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:Default:ConnectionString"] = "server1:6380,keepAlive=30"
        });

        builder.AddRedis(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
                RedisOptions options = optionsMonitor.Get(serviceBindingName);

                return GetMockedConnectionMultiplexer(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<RedisOptions, IConnectionMultiplexer>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(1);
        connectorFactory.ServiceBindingNames.Should().Contain(string.Empty);

        RedisOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().NotBeNullOrEmpty();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    private static object GetMockedConnectionMultiplexer(string? connectionString)
    {
        var databaseMock = new Mock<IDatabase>();

        var connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        connectionMultiplexerMock.Setup(connectionMultiplexer => connectionMultiplexer.Configuration).Returns(connectionString!);

        connectionMultiplexerMock.Setup(connectionMultiplexer => connectionMultiplexer.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(databaseMock.Object);

        databaseMock.Setup(database => database.Multiplexer).Returns(connectionMultiplexerMock.Object);

        return connectionMultiplexerMock.Object;
    }

    private static async Task<IConnectionMultiplexer> ExtractUnderlyingMultiplexerFromRedisCacheAsync(RedisCache redisCache)
    {
        _ = await redisCache.GetAsync("ignored");

        FieldInfo cacheField = typeof(RedisCache).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var database = (IDatabase)cacheField.GetValue(redisCache)!;
        return database.Multiplexer;
    }
}
