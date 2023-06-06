// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Maintenance;
using StackExchange.Redis.Profiling;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Connectors.Redis;
using Xunit;

namespace Steeltoe.Connectors.Test.Redis;

public sealed class RedisConnectorTests
{
    private const string MultiVcapServicesJson = @"{
  ""p-redis"": [
    {
      ""label"": ""p-redis"",
      ""provider"": null,
      ""plan"": ""shared-vm"",
      ""name"": ""myRedisServiceOne"",
      ""tags"": [
        ""pivotal"",
        ""redis""
      ],
      ""instance_guid"": ""a9eb9256-73fe-4d3a-92d2-c91bcb1a739e"",
      ""instance_name"": ""myRedisServiceOne"",
      ""binding_guid"": ""3fde92a3-83b2-48de-bd30-958f632c3e20"",
      ""binding_name"": null,
      ""credentials"": {
        ""host"": ""10.0.4.17"",
        ""password"": ""36c5b850-3b44-4bcb-8b2f-510eeb9b1c6e"",
        ""port"": 34029
      },
      ""syslog_drain_url"": null,
      ""volume_mounts"": []
    },
    {
      ""label"": ""p-redis"",
      ""provider"": null,
      ""plan"": ""shared-vm"",
      ""name"": ""myRedisServiceTwo"",
      ""tags"": [
        ""pivotal"",
        ""redis""
      ],
      ""instance_guid"": ""415cbd98-18a2-4ebb-966a-d57d82425724"",
      ""instance_name"": ""myRedisServiceTwo"",
      ""binding_guid"": ""012f540d-c9fc-4fc4-98ae-bcda7e6e3830"",
      ""binding_name"": null,
      ""credentials"": {
        ""host"": ""10.0.4.17"",
        ""password"": ""aa786395-98c3-4e7e-aee4-ca02e5a8590a"",
        ""port"": 44369
      },
      ""syslog_drain_url"": null,
      ""volume_mounts"": []
    }
  ]
}";

    private const string SingleVcapServicesJson = @"{
  ""p-redis"": [
    {
      ""label"": ""p-redis"",
      ""provider"": null,
      ""plan"": ""shared-vm"",
      ""name"": ""myRedisService"",
      ""tags"": [
        ""pivotal"",
        ""redis""
      ],
      ""instance_guid"": ""3bdc54ae-9e8e-45b1-8f80-5ec0a73505bf"",
      ""instance_name"": ""myRedisService"",
      ""binding_guid"": ""776f5be6-c840-405f-8728-71563f1bff27"",
      ""binding_name"": null,
      ""credentials"": {
        ""host"": ""10.0.4.17"",
        ""password"": ""269493d4-579a-42f0-b43b-42e83741517d"",
        ""port"": 37357
      },
      ""syslog_drain_url"": null,
      ""volume_mounts"": []
    }
  ]
}";

    [Fact]
    public async Task Binds_options_without_service_bindings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:Redis:myRedisServiceOne:ConnectionString"] = "server1:6380,keepAlive=30",
            ["Steeltoe:Client:Redis:myRedisServiceTwo:ConnectionString"] = "server2:6380,allowAdmin=true"
        });

        builder.AddRedis();

        await using WebApplication app = builder.Build();
        var optionsSnapshot = app.Services.GetRequiredService<IOptionsSnapshot<RedisOptions>>();

        RedisOptions optionsOne = optionsSnapshot.Get("myRedisServiceOne");
        optionsOne.ConnectionString.Should().Be("server1:6380,keepAlive=30");

        RedisOptions optionsTwo = optionsSnapshot.Get("myRedisServiceTwo");
        optionsTwo.ConnectionString.Should().Be("server2:6380,allowAdmin=true");
    }

    [Fact]
    public async Task Binds_options_with_CloudFoundry_service_bindings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(MultiVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:Redis:myRedisServiceOne:ConnectionString"] = "localhost:12345,keepAlive=30"
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
    public async Task Registers_ConnectorFactory_for_IConnectionMultiplexer()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
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

                return new FakeConnectionMultiplexer(options.ConnectionString);
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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
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

                return new FakeConnectionMultiplexer(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<RedisOptions, IDistributedCache>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain("myRedisServiceOne");
        connectorFactory.ServiceBindingNames.Should().Contain("myRedisServiceTwo");

        var connectionOne = (RedisCache)connectorFactory.Get("myRedisServiceOne").GetConnection();
        FakeConnectionMultiplexer connectionMultiplexerOne = await ExtractUnderlyingMultiplexerFromRedisCacheAsync(connectionOne);
        connectionMultiplexerOne.Configuration.Should().Be("server1:6380,keepAlive=30");

        var connectionTwo = (RedisCache)connectorFactory.Get("myRedisServiceTwo").GetConnection();
        FakeConnectionMultiplexer connectionMultiplexerTwo = await ExtractUnderlyingMultiplexerFromRedisCacheAsync(connectionTwo);
        connectionMultiplexerTwo.Configuration.Should().Be("server2:6380,allowAdmin=true");

        IDistributedCache connectionOneAgain = connectorFactory.Get("myRedisServiceOne").GetConnection();
        connectionOneAgain.Should().BeSameAs(connectionOne);
    }

    [Fact]
    public async Task Registers_HealthContributors()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
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

                return new FakeConnectionMultiplexer(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        IHealthContributor[] healthContributors = app.Services.GetServices<IHealthContributor>().ToArray();
        healthContributors.Should().AllBeOfType<RedisHealthContributor>();

        healthContributors.Should().HaveCount(2);
        healthContributors[0].Id.Should().Be("Redis-myRedisServiceOne");
        healthContributors[1].Id.Should().Be("Redis-myRedisServiceTwo");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.AddRedis(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
                RedisOptions options = optionsMonitor.Get(serviceBindingName);

                return new FakeConnectionMultiplexer(options.ConnectionString);
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
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:Redis:Default:ConnectionString"] = "server1:6380,keepAlive=30"
        });

        builder.AddRedis(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
                RedisOptions options = optionsMonitor.Get(serviceBindingName);

                return new FakeConnectionMultiplexer(options.ConnectionString);
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

    private static async Task<FakeConnectionMultiplexer> ExtractUnderlyingMultiplexerFromRedisCacheAsync(RedisCache redisCache)
    {
        _ = await redisCache.GetAsync("ignored");
        FieldInfo connectionField = typeof(RedisCache).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (FakeConnectionMultiplexer)connectionField.GetValue(redisCache)!;
    }

    private sealed class FakeConnectionMultiplexer : IConnectionMultiplexer
    {
        public string Configuration { get; }
        public string ClientName => "FakeClientName";

        public int TimeoutMilliseconds => throw new NotImplementedException();
        public long OperationCount => throw new NotImplementedException();

        public bool PreserveAsyncOrder
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool IsConnected => throw new NotImplementedException();
        public bool IsConnecting => throw new NotImplementedException();

        public bool IncludeDetailInExceptions
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public int StormLogThreshold
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public event EventHandler<RedisErrorEventArgs> ErrorMessage
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<InternalErrorEventArgs> InternalError
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<ConnectionFailedEventArgs> ConnectionRestored
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<EndPointEventArgs> ConfigurationChanged
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<EndPointEventArgs> ConfigurationChangedBroadcast
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<ServerMaintenanceEvent>? ServerMaintenanceEvent
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<HashSlotMovedEventArgs> HashSlotMoved
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public FakeConnectionMultiplexer(string? connectionString)
        {
            Configuration = connectionString!;
        }

        public void Dispose()
        {
        }

        public void RegisterProfiler(Func<ProfilingSession> profilingSessionProvider)
        {
            throw new NotImplementedException();
        }

        public ServerCounters GetCounters()
        {
            throw new NotImplementedException();
        }

        public EndPoint[] GetEndPoints(bool configuredOnly = false)
        {
            return Array.Empty<EndPoint>();
        }

        public void Wait(Task task)
        {
            throw new NotImplementedException();
        }

        public T Wait<T>(Task<T> task)
        {
            throw new NotImplementedException();
        }

        public void WaitAll(params Task[] tasks)
        {
            throw new NotImplementedException();
        }

        public int HashSlot(RedisKey key)
        {
            throw new NotImplementedException();
        }

        public ISubscriber GetSubscriber(object? asyncState = null)
        {
            throw new NotImplementedException();
        }

        public IDatabase GetDatabase(int db = -1, object? asyncState = null)
        {
            return new FakeDatabase();
        }

        public IServer GetServer(string host, int port, object? asyncState = null)
        {
            throw new NotImplementedException();
        }

        public IServer GetServer(string hostAndPort, object? asyncState = null)
        {
            throw new NotImplementedException();
        }

        public IServer GetServer(IPAddress host, int port)
        {
            throw new NotImplementedException();
        }

        public IServer GetServer(EndPoint endpoint, object? asyncState = null)
        {
            throw new NotImplementedException();
        }

        public IServer[] GetServers()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ConfigureAsync(TextWriter? log = null)
        {
            throw new NotImplementedException();
        }

        public bool Configure(TextWriter? log = null)
        {
            throw new NotImplementedException();
        }

        public string GetStatus()
        {
            throw new NotImplementedException();
        }

        public void GetStatus(TextWriter log)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public void Close(bool allowCommandsToComplete = true)
        {
        }

        public Task CloseAsync(bool allowCommandsToComplete = true)
        {
            throw new NotImplementedException();
        }

        public string GetStormLog()
        {
            throw new NotImplementedException();
        }

        public void ResetStormLog()
        {
            throw new NotImplementedException();
        }

        public long PublishReconfigure(CommandFlags flags = CommandFlags.None)
        {
            throw new NotImplementedException();
        }

        public Task<long> PublishReconfigureAsync(CommandFlags flags = CommandFlags.None)
        {
            throw new NotImplementedException();
        }

        public int GetHashSlot(RedisKey key)
        {
            throw new NotImplementedException();
        }

        public void ExportConfiguration(Stream destination, ExportOptions options = ExportOptions.All)
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        private sealed class FakeDatabase : IDatabase
        {
            public IConnectionMultiplexer Multiplexer => throw new NotImplementedException();

            public int Database => throw new NotImplementedException();

            public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool TryWait(Task task)
            {
                throw new NotImplementedException();
            }

            public void Wait(Task task)
            {
                throw new NotImplementedException();
            }

            public T Wait<T>(Task<T> task)
            {
                throw new NotImplementedException();
            }

            public void WaitAll(params Task[] tasks)
            {
                throw new NotImplementedException();
            }

            public TimeSpan Ping(CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool IsConnected(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task KeyMigrateAsync(RedisKey key, EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0,
                MigrateOptions migrateOptions = MigrateOptions.None, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> DebugObjectAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> GeoAddAsync(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> GeoAddAsync(RedisKey key, GeoEntry value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> GeoAddAsync(RedisKey key, GeoEntry[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> GeoRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<double?> GeoDistanceAsync(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = GeoUnit.Meters,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<string?[]> GeoHashAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<string?> GeoHashAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<GeoPosition?[]> GeoPositionAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<GeoPosition?> GeoPositionAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, RedisValue member, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1,
                Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, double longitude, double latitude, double radius, GeoUnit unit = GeoUnit.Meters,
                int count = -1, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<GeoRadiusResult[]> GeoSearchAsync(RedisKey key, RedisValue member, GeoSearchShape shape, int count = -1, bool demandClosest = true,
                Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<GeoRadiusResult[]> GeoSearchAsync(RedisKey key, double longitude, double latitude, GeoSearchShape shape, int count = -1,
                bool demandClosest = true, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> GeoSearchAndStoreAsync(RedisKey sourceKey, RedisKey destinationKey, RedisValue member, GeoSearchShape shape, int count = -1,
                bool demandClosest = true, Order? order = null, bool storeDistances = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> GeoSearchAndStoreAsync(RedisKey sourceKey, RedisKey destinationKey, double longitude, double latitude, GeoSearchShape shape,
                int count = -1, bool demandClosest = true, Order? order = null, bool storeDistances = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> HashDecrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<double> HashDecrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HashDeleteAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> HashDeleteAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HashExistsAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
            {
                return Task.FromResult(Array.Empty<RedisValue>());
            }

            public Task<Lease<byte>?> HashGetLeaseAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<HashEntry[]> HashGetAllAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> HashIncrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<double> HashIncrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> HashKeysAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> HashLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> HashRandomFieldAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> HashRandomFieldsAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<HashEntry[]> HashRandomFieldsWithValuesAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<HashEntry> HashScanAsync(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0,
                int pageOffset = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task HashSetAsync(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value, When when = When.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> HashStringLengthAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> HashValuesAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> HyperLogLogLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> HyperLogLogLengthAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey[] sourceKeys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<EndPoint?> IdentifyEndpointAsync(RedisKey key = default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyCopyAsync(RedisKey sourceKey, RedisKey destinationKey, int destinationDatabase = -1, bool replace = false,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> KeyDeleteAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<byte[]?> KeyDumpAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<string?> KeyEncodingAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> KeyExistsAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry, ExpireWhen when = ExpireWhen.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyExpireAsync(RedisKey key, DateTime? expiry, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyExpireAsync(RedisKey key, DateTime? expiry, ExpireWhen when = ExpireWhen.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<DateTime?> KeyExpireTimeAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long?> KeyFrequencyAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<TimeSpan?> KeyIdleTimeAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyMoveAsync(RedisKey key, int database, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyPersistAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisKey> KeyRandomAsync(CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long?> KeyRefCountAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyRenameAsync(RedisKey key, RedisKey newKey, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task KeyRestoreAsync(RedisKey key, byte[] value, TimeSpan? expiry = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<TimeSpan?> KeyTimeToLiveAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisType> KeyTypeAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> ListGetByIndexAsync(RedisKey key, long index, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListInsertAfterAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListInsertBeforeAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> ListLeftPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> ListLeftPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<ListPopResult> ListLeftPopAsync(RedisKey[] keys, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListPositionAsync(RedisKey key, RedisValue element, long rank = 1, long maxLength = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long[]> ListPositionsAsync(RedisKey key, RedisValue element, long count, long rank = 1, long maxLength = 0,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListLeftPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListLeftPushAsync(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListLeftPushAsync(RedisKey key, RedisValue[] values, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> ListMoveAsync(RedisKey sourceKey, RedisKey destinationKey, ListSide sourceSide, ListSide destinationSide,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListRemoveAsync(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> ListRightPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> ListRightPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<ListPopResult> ListRightPopAsync(RedisKey[] keys, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> ListRightPopLeftPushAsync(RedisKey source, RedisKey destination, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListRightPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListRightPushAsync(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> ListRightPushAsync(RedisKey key, RedisValue[] values, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task ListSetByIndexAsync(RedisKey key, long index, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task ListTrimAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> LockExtendAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> LockQueryAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> LockReleaseAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> LockTakeAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisResult> ExecuteAsync(string command, params object[] args)
            {
                throw new NotImplementedException();
            }

            public Task<RedisResult> ExecuteAsync(string command, ICollection<object>? args, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[]? keys = null, RedisValue[]? values = null,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisResult> ScriptEvaluateAsync(byte[] hash, RedisKey[]? keys = null, RedisValue[]? values = null,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisResult> ScriptEvaluateAsync(LuaScript script, object? parameters = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisResult> ScriptEvaluateAsync(LoadedLuaScript script, object? parameters = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisResult> ScriptEvaluateReadOnlyAsync(string script, RedisKey[]? keys = null, RedisValue[]? values = null,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisResult> ScriptEvaluateReadOnlyAsync(byte[] hash, RedisKey[]? keys = null, RedisValue[]? values = null,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SetAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SetContainsAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool[]> SetContainsAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SetIntersectionLengthAsync(RedisKey[] keys, long limit = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SetLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SetMoveAsync(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> SetPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SetPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> SetRandomMemberAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SetRandomMembersAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SetRemoveAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<RedisValue> SetScanAsync(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0,
                int pageOffset = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SortAsync(RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending,
                SortType sortType = SortType.Numeric, RedisValue by = default, RedisValue[]? get = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortAndStoreAsync(RedisKey destination, RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending,
                SortType sortType = SortType.Numeric, RedisValue by = default, RedisValue[]? get = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, When when, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, SortedSetWhen when = SortedSetWhen.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, When when, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = SortedSetWhen.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SortedSetCombineAsync(SetOperation operation, RedisKey[] keys, double[]? weights = null,
                Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<SortedSetEntry[]> SortedSetCombineWithScoresAsync(SetOperation operation, RedisKey[] keys, double[]? weights = null,
                Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second,
                Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, double[]? weights = null,
                Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<double> SortedSetDecrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<double> SortedSetIncrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetIntersectionLengthAsync(RedisKey[] keys, long limit = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetLengthAsync(RedisKey key, double min = double.NegativeInfinity, double max = double.PositiveInfinity,
                Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetLengthByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> SortedSetRandomMemberAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SortedSetRandomMembersAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<SortedSetEntry[]> SortedSetRandomMembersWithScoresAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SortedSetRangeByRankAsync(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetRangeAndStoreAsync(RedisKey sourceKey, RedisKey destinationKey, RedisValue start, RedisValue stop,
                SortedSetOrder sortedSetOrder = SortedSetOrder.ByRank, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0,
                long? take = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SortedSetRangeByScoreAsync(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity,
                Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(RedisKey key, double start = double.NegativeInfinity,
                double stop = double.PositiveInfinity, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SortedSetRangeByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude, long skip, long take = -1,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> SortedSetRangeByValueAsync(RedisKey key, RedisValue min = default, RedisValue max = default,
                Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long?> SortedSetRankAsync(RedisKey key, RedisValue member, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetRemoveRangeByRankAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key, double start, double stop, Exclude exclude = Exclude.None,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetRemoveRangeByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<SortedSetEntry> SortedSetScanAsync(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0,
                int pageOffset = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<double?> SortedSetScoreAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<double?[]> SortedSetScoresAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> SortedSetUpdateAsync(RedisKey key, RedisValue member, double score, SortedSetWhen when = SortedSetWhen.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> SortedSetUpdateAsync(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = SortedSetWhen.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<SortedSetEntry?> SortedSetPopAsync(RedisKey key, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<SortedSetEntry[]> SortedSetPopAsync(RedisKey key, long count, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<SortedSetPopResult> SortedSetPopAsync(RedisKey[] keys, long count, Order order = Order.Ascending,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue groupName, RedisValue messageId, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue groupName, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StreamAddAsync(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? messageId = null,
                int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StreamAddAsync(RedisKey key, NameValueEntry[] streamPairs, RedisValue? messageId = null, int? maxLength = null,
                bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamAutoClaimResult> StreamAutoClaimAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs,
                RedisValue startAtId, int? count = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamAutoClaimIdsOnlyResult> StreamAutoClaimIdsOnlyAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer,
                long minIdleTimeInMs, RedisValue startAtId, int? count = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamEntry[]> StreamClaimAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs,
                RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> StreamClaimIdsOnlyAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs,
                RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StreamConsumerGroupSetPositionAsync(RedisKey key, RedisValue groupName, RedisValue position,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamConsumerInfo[]> StreamConsumerInfoAsync(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StreamCreateConsumerGroupAsync(RedisKey key, RedisValue groupName, RedisValue? position, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StreamCreateConsumerGroupAsync(RedisKey key, RedisValue groupName, RedisValue? position = null, bool createStream = true,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StreamDeleteAsync(RedisKey key, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StreamDeleteConsumerAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StreamDeleteConsumerGroupAsync(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamGroupInfo[]> StreamGroupInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamInfo> StreamInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StreamLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamPendingInfo> StreamPendingAsync(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamPendingMessageInfo[]> StreamPendingMessagesAsync(RedisKey key, RedisValue groupName, int count, RedisValue consumerName,
                RedisValue? minId = null, RedisValue? maxId = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamEntry[]> StreamRangeAsync(RedisKey key, RedisValue? minId = null, RedisValue? maxId = null, int? count = null,
                Order messageOrder = Order.Ascending, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisStream[]> StreamReadAsync(StreamPosition[] streamPositions, int? countPerStream = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<StreamEntry[]> StreamReadGroupAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position, int? count,
                CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<StreamEntry[]> StreamReadGroupAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null,
                int? count = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisStream[]> StreamReadGroupAsync(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName,
                int? countPerStream, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<RedisStream[]> StreamReadGroupAsync(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName,
                int? countPerStream = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StreamTrimAsync(RedisKey key, int maxLength, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringAppendAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringBitCountAsync(RedisKey key, long start, long end, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringBitCountAsync(RedisKey key, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = default,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringBitPositionAsync(RedisKey key, bool bit, long start, long end, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringBitPositionAsync(RedisKey key, bool bit, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringDecrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<double> StringDecrementAsync(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue[]> StringGetAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<Lease<byte>?> StringGetLeaseAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StringGetBitAsync(RedisKey key, long offset, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StringGetRangeAsync(RedisKey key, long start, long end, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StringGetSetAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StringGetSetExpiryAsync(RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StringGetSetExpiryAsync(RedisKey key, DateTime expiry, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StringGetDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValueWithExpiry> StringGetWithExpiryAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringIncrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<double> StringIncrementAsync(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<string?> StringLongestCommonSubsequenceAsync(RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> StringLongestCommonSubsequenceLengthAsync(RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<LCSMatchResult> StringLongestCommonSubsequenceWithMatchesAsync(RedisKey first, RedisKey second, long minLength = 0,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false, When when = When.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StringSetAsync(KeyValuePair<RedisKey, RedisValue>[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StringSetAndGetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StringSetAndGetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false, When when = When.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> StringSetBitAsync(RedisKey key, long offset, bool bit, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<RedisValue> StringSetRangeAsync(RedisKey key, long offset, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<bool> KeyTouchAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Task<long> KeyTouchAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public IBatch CreateBatch(object? asyncState = null)
            {
                throw new NotImplementedException();
            }

            public ITransaction CreateTransaction(object? asyncState = null)
            {
                throw new NotImplementedException();
            }

            public void KeyMigrate(RedisKey key, EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0,
                MigrateOptions migrateOptions = MigrateOptions.None, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue DebugObject(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool GeoAdd(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool GeoAdd(RedisKey key, GeoEntry value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long GeoAdd(RedisKey key, GeoEntry[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool GeoRemove(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public double? GeoDistance(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = GeoUnit.Meters,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public string[] GeoHash(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public string GeoHash(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public GeoPosition?[] GeoPosition(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public GeoPosition? GeoPosition(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public GeoRadiusResult[] GeoRadius(RedisKey key, RedisValue member, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1,
                Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public GeoRadiusResult[] GeoRadius(RedisKey key, double longitude, double latitude, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1,
                Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public GeoRadiusResult[] GeoSearch(RedisKey key, RedisValue member, GeoSearchShape shape, int count = -1, bool demandClosest = true,
                Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public GeoRadiusResult[] GeoSearch(RedisKey key, double longitude, double latitude, GeoSearchShape shape, int count = -1, bool demandClosest = true,
                Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long GeoSearchAndStore(RedisKey sourceKey, RedisKey destinationKey, RedisValue member, GeoSearchShape shape, int count = -1,
                bool demandClosest = true, Order? order = null, bool storeDistances = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long GeoSearchAndStore(RedisKey sourceKey, RedisKey destinationKey, double longitude, double latitude, GeoSearchShape shape, int count = -1,
                bool demandClosest = true, Order? order = null, bool storeDistances = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long HashDecrement(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public double HashDecrement(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool HashDelete(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long HashDelete(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool HashExists(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue HashGet(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] HashGet(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Lease<byte> HashGetLease(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public HashEntry[] HashGetAll(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long HashIncrement(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public double HashIncrement(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] HashKeys(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long HashLength(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue HashRandomField(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] HashRandomFields(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public HashEntry[] HashRandomFieldsWithValues(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern, int pageSize, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public void HashSet(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool HashSet(RedisKey key, RedisValue hashField, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long HashStringLength(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] HashValues(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool HyperLogLogAdd(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool HyperLogLogAdd(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long HyperLogLogLength(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long HyperLogLogLength(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public void HyperLogLogMerge(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public void HyperLogLogMerge(RedisKey destination, RedisKey[] sourceKeys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public EndPoint IdentifyEndpoint(RedisKey key = default, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool KeyCopy(RedisKey sourceKey, RedisKey destinationKey, int destinationDatabase = -1, bool replace = false,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool KeyDelete(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long KeyDelete(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public byte[] KeyDump(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public string KeyEncoding(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool KeyExists(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long KeyExists(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool KeyExpire(RedisKey key, TimeSpan? expiry, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public bool KeyExpire(RedisKey key, TimeSpan? expiry, ExpireWhen when = ExpireWhen.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool KeyExpire(RedisKey key, DateTime? expiry, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public bool KeyExpire(RedisKey key, DateTime? expiry, ExpireWhen when = ExpireWhen.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public DateTime? KeyExpireTime(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long? KeyFrequency(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public TimeSpan? KeyIdleTime(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool KeyMove(RedisKey key, int database, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool KeyPersist(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisKey KeyRandom(CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long? KeyRefCount(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool KeyRename(RedisKey key, RedisKey newKey, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public void KeyRestore(RedisKey key, byte[] value, TimeSpan? expiry = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public TimeSpan? KeyTimeToLive(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisType KeyType(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue ListGetByIndex(RedisKey key, long index, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListInsertAfter(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListInsertBefore(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue ListLeftPop(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] ListLeftPop(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public ListPopResult ListLeftPop(RedisKey[] keys, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListPosition(RedisKey key, RedisValue element, long rank = 1, long maxLength = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long[] ListPositions(RedisKey key, RedisValue element, long count, long rank = 1, long maxLength = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListLeftPush(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListLeftPush(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListLeftPush(RedisKey key, RedisValue[] values, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public long ListLength(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue ListMove(RedisKey sourceKey, RedisKey destinationKey, ListSide sourceSide, ListSide destinationSide,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] ListRange(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListRemove(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue ListRightPop(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] ListRightPop(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public ListPopResult ListRightPop(RedisKey[] keys, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue ListRightPopLeftPush(RedisKey source, RedisKey destination, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListRightPush(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListRightPush(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long ListRightPush(RedisKey key, RedisValue[] values, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public void ListSetByIndex(RedisKey key, long index, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public void ListTrim(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool LockExtend(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue LockQuery(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool LockRelease(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool LockTake(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long Publish(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisResult Execute(string command, params object[] args)
            {
                throw new NotImplementedException();
            }

            public RedisResult Execute(string command, ICollection<object> args, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisResult ScriptEvaluate(string script, RedisKey[]? keys = null, RedisValue[]? values = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisResult ScriptEvaluate(byte[] hash, RedisKey[]? keys = null, RedisValue[]? values = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisResult ScriptEvaluate(LuaScript script, object? parameters = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisResult ScriptEvaluate(LoadedLuaScript script, object? parameters = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisResult ScriptEvaluateReadOnly(string script, RedisKey[]? keys = null, RedisValue[]? values = null,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisResult ScriptEvaluateReadOnly(byte[] hash, RedisKey[]? keys = null, RedisValue[]? values = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool SetAdd(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SetAdd(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SetCombine(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SetCombine(SetOperation operation, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool SetContains(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool[] SetContains(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SetIntersectionLength(RedisKey[] keys, long limit = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SetLength(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SetMembers(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool SetMove(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue SetPop(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SetPop(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue SetRandomMember(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SetRandomMembers(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool SetRemove(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SetRemove(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<RedisValue> SetScan(RedisKey key, RedisValue pattern, int pageSize, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<RedisValue> SetScan(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] Sort(RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending, SortType sortType = SortType.Numeric,
                RedisValue by = default, RedisValue[]? get = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortAndStore(RedisKey destination, RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending,
                SortType sortType = SortType.Numeric, RedisValue by = default, RedisValue[]? get = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool SortedSetAdd(RedisKey key, RedisValue member, double score, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public bool SortedSetAdd(RedisKey key, RedisValue member, double score, When when, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool SortedSetAdd(RedisKey key, RedisValue member, double score, SortedSetWhen when = SortedSetWhen.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, When when, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = SortedSetWhen.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SortedSetCombine(SetOperation operation, RedisKey[] keys, double[]? weights = null, Aggregate aggregate = Aggregate.Sum,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public SortedSetEntry[] SortedSetCombineWithScores(SetOperation operation, RedisKey[] keys, double[]? weights = null,
                Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second,
                Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey[] keys, double[]? weights = null,
                Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public double SortedSetDecrement(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public double SortedSetIncrement(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetIntersectionLength(RedisKey[] keys, long limit = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetLength(RedisKey key, double min = double.NegativeInfinity, double max = double.PositiveInfinity,
                Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetLengthByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue SortedSetRandomMember(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SortedSetRandomMembers(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public SortedSetEntry[] SortedSetRandomMembersWithScores(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SortedSetRangeByRank(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetRangeAndStore(RedisKey sourceKey, RedisKey destinationKey, RedisValue start, RedisValue stop,
                SortedSetOrder sortedSetOrder = SortedSetOrder.ByRank, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0,
                long? take = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public SortedSetEntry[] SortedSetRangeByRankWithScores(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SortedSetRangeByScore(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity,
                Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public SortedSetEntry[] SortedSetRangeByScoreWithScores(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity,
                Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SortedSetRangeByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude, long skip, long take = -1,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] SortedSetRangeByValue(RedisKey key, RedisValue min = default, RedisValue max = default, Exclude exclude = Exclude.None,
                Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long? SortedSetRank(RedisKey key, RedisValue member, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool SortedSetRemove(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetRemove(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetRemoveRangeByRank(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetRemoveRangeByScore(RedisKey key, double start, double stop, Exclude exclude = Exclude.None,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetRemoveRangeByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<SortedSetEntry> SortedSetScan(RedisKey key, RedisValue pattern, int pageSize, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<SortedSetEntry> SortedSetScan(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0,
                int pageOffset = 0, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public double? SortedSetScore(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public double?[] SortedSetScores(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public SortedSetEntry? SortedSetPop(RedisKey key, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public SortedSetEntry[] SortedSetPop(RedisKey key, long count, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public SortedSetPopResult SortedSetPop(RedisKey[] keys, long count, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool SortedSetUpdate(RedisKey key, RedisValue member, double score, SortedSetWhen when = SortedSetWhen.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long SortedSetUpdate(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = SortedSetWhen.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StreamAcknowledge(RedisKey key, RedisValue groupName, RedisValue messageId, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StreamAcknowledge(RedisKey key, RedisValue groupName, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StreamAdd(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? messageId = null, int? maxLength = null,
                bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StreamAdd(RedisKey key, NameValueEntry[] streamPairs, RedisValue? messageId = null, int? maxLength = null,
                bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamAutoClaimResult StreamAutoClaim(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs,
                RedisValue startAtId, int? count = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamAutoClaimIdsOnlyResult StreamAutoClaimIdsOnly(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer,
                long minIdleTimeInMs, RedisValue startAtId, int? count = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamEntry[] StreamClaim(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue[] messageIds,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] StreamClaimIdsOnly(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs,
                RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool StreamConsumerGroupSetPosition(RedisKey key, RedisValue groupName, RedisValue position, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamConsumerInfo[] StreamConsumerInfo(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool StreamCreateConsumerGroup(RedisKey key, RedisValue groupName, RedisValue? position, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public bool StreamCreateConsumerGroup(RedisKey key, RedisValue groupName, RedisValue? position = null, bool createStream = true,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StreamDelete(RedisKey key, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StreamDeleteConsumer(RedisKey key, RedisValue groupName, RedisValue consumerName, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool StreamDeleteConsumerGroup(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamGroupInfo[] StreamGroupInfo(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamInfo StreamInfo(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StreamLength(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamPendingInfo StreamPending(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamPendingMessageInfo[] StreamPendingMessages(RedisKey key, RedisValue groupName, int count, RedisValue consumerName,
                RedisValue? minId = null, RedisValue? maxId = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamEntry[] StreamRange(RedisKey key, RedisValue? minId = null, RedisValue? maxId = null, int? count = null,
                Order messageOrder = Order.Ascending, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamEntry[] StreamRead(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisStream[] StreamRead(StreamPosition[] streamPositions, int? countPerStream = null, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public StreamEntry[] StreamReadGroup(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position, int? count,
                CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public StreamEntry[] StreamReadGroup(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null, int? count = null,
                bool noAck = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisStream[] StreamReadGroup(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName, int? countPerStream,
                CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public RedisStream[] StreamReadGroup(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName, int? countPerStream = null,
                bool noAck = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StreamTrim(RedisKey key, int maxLength, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StringAppend(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StringBitCount(RedisKey key, long start, long end, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public long StringBitCount(RedisKey key, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StringBitOperation(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = default,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StringBitOperation(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StringBitPosition(RedisKey key, bool bit, long start, long end, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public long StringBitPosition(RedisKey key, bool bit, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StringDecrement(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public double StringDecrement(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StringGet(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue[] StringGet(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public Lease<byte> StringGetLease(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool StringGetBit(RedisKey key, long offset, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StringGetRange(RedisKey key, long start, long end, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StringGetSet(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StringGetSetExpiry(RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StringGetSetExpiry(RedisKey key, DateTime expiry, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StringGetDelete(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValueWithExpiry StringGetWithExpiry(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StringIncrement(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public double StringIncrement(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StringLength(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public string StringLongestCommonSubsequence(RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long StringLongestCommonSubsequenceLength(RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public LCSMatchResult StringLongestCommonSubsequenceWithMatches(RedisKey first, RedisKey second, long minLength = 0,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry, When when)
            {
                throw new NotImplementedException();
            }

            public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false, When when = When.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool StringSet(KeyValuePair<RedisKey, RedisValue>[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StringSetAndGet(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags)
            {
                throw new NotImplementedException();
            }

            public RedisValue StringSetAndGet(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false, When when = When.Always,
                CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool StringSetBit(RedisKey key, long offset, bool bit, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public RedisValue StringSetRange(RedisKey key, long offset, RedisValue value, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public bool KeyTouch(RedisKey key, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }

            public long KeyTouch(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
            {
                throw new NotImplementedException();
            }
        }
    }
}
