// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Common.Util;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors.Redis;

public class RedisHealthContributor : IHealthContributor
{
    private readonly RedisServiceConnectorFactory _factory;
    private readonly Type _implType;
    private readonly ILogger<RedisHealthContributor> _logger;
    private readonly string _hostName;
    private object _connector;
    private object _database;
    private object _flags;
    private MethodInfo _pingMethod;
    private MethodInfo _getMethod;

    private bool IsMicrosoftImplementation => _implType.FullName.Contains("Microsoft", StringComparison.Ordinal);

    public string Id { get; }

    public RedisHealthContributor(RedisServiceConnectorFactory factory, Type implType, ILogger<RedisHealthContributor> logger = null)
    {
        _factory = factory;
        _implType = implType;
        _logger = logger;
        Id = IsMicrosoftImplementation ? "Redis-Cache" : "Redis";
    }

    internal RedisHealthContributor(object redisClient, string serviceName, string hostName, ILogger<RedisHealthContributor> logger)
    {
        _connector = redisClient;
        _implType = redisClient.GetType();
        Id = serviceName;
        _hostName = hostName;
        _logger = logger;
    }

    public static IHealthContributor GetRedisContributor(IConfiguration configuration, ILogger<RedisHealthContributor> logger = null)
    {
        ArgumentGuard.NotNull(configuration);

        Type redisImplementation = RedisTypeLocator.StackExchangeImplementation;
        Type redisOptions = RedisTypeLocator.StackExchangeOptions;
        MethodInfo initializer = RedisTypeLocator.StackExchangeInitializer;

        var info = configuration.GetSingletonServiceInfo<RedisServiceInfo>();
        var options = new RedisCacheConnectorOptions(configuration);
        var factory = new RedisServiceConnectorFactory(info, options, redisImplementation, redisOptions, initializer);
        return new RedisHealthContributor(factory, redisImplementation, logger);
    }

    public HealthCheckResult Health()
    {
        _logger?.LogTrace("Checking Redis connection health");
        var result = new HealthCheckResult();

        if (!string.IsNullOrEmpty(_hostName))
        {
            result.Details.Add("host", _hostName);
        }

        try
        {
            Connect();

            if (IsMicrosoftImplementation)
            {
                DoMicrosoftHealth();
            }
            else
            {
                DoStackExchangeHealth(result);
            }

            // Spring Boot health checks also include cluster size and slot metrics
            result.Details.Add("status", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Up;
            _logger?.LogTrace("Redis connection up!");
        }
        catch (Exception e)
        {
            if (e is TargetInvocationException)
            {
                e = e.InnerException;
            }

            _logger?.LogError(e, "Redis connection down! {HealthCheckException}", e.Message);
            result.Details.Add("error", $"{e.GetType().Name}: {e.Message}");
            result.Details.Add("status", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Down;
            result.Description = "Redis health check failed";
        }

        return result;
    }

    private void DoStackExchangeHealth(HealthCheckResult health)
    {
        var latency = (TimeSpan)ReflectionHelpers.Invoke(_pingMethod, _database, new[]
        {
            _flags
        });

        health.Details.Add("ping", latency.TotalMilliseconds);
    }

    private void DoMicrosoftHealth()
    {
        _getMethod.Invoke(_connector, new object[]
        {
            "1"
        });
    }

    private void Connect()
    {
        if (_connector == null)
        {
            _connector = _factory.Create(null);

            if (_connector == null)
            {
                throw new ConnectorException("Unable to connect to Redis");
            }
        }

        if (_database == null)
        {
            if (IsMicrosoftImplementation)
            {
                _getMethod = _connector.GetType().GetMethod("Get");
            }
            else
            {
                Type commandFlagsEnum = RedisTypeLocator.StackExchangeCommandFlagsNames;
                _flags = Enum.Parse(commandFlagsEnum, "None");
                MethodInfo getDatabaseMethod = ReflectionHelpers.FindMethod(_connector.GetType(), "GetDatabase");

                _database = ReflectionHelpers.Invoke(getDatabaseMethod, _connector, new object[]
                {
                    -1,
                    null
                });

                _pingMethod = ReflectionHelpers.FindMethod(_database.GetType(), "Ping");
            }
        }
    }
}
