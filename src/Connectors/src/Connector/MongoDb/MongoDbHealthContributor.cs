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
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.MongoDb;

public class MongoDbHealthContributor : IHealthContributor
{
    private readonly ILogger<MongoDbHealthContributor> _logger;
    private readonly object _mongoClient;

    private int Timeout { get; }

    public string Id { get; }
    public string HostName { get; }

    public MongoDbHealthContributor(MongoDbConnectorFactory factory, ILogger<MongoDbHealthContributor> logger = null, int timeout = 5000)
    {
        _logger = logger;
        _mongoClient = factory.Create(null);
        Timeout = timeout;
        Id = "MongoDb";
        HostName = Id;
    }

    internal MongoDbHealthContributor(object mongoClient, string serviceName, string hostName, ILogger<MongoDbHealthContributor> logger)
    {
        _mongoClient = mongoClient;
        Id = serviceName;
        HostName = hostName;
        _logger = logger;
        Timeout = 5000;
    }

    public static IHealthContributor GetMongoDbHealthContributor(IConfiguration configuration, ILogger<MongoDbHealthContributor> logger = null)
    {
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<MongoDbServiceInfo>();
        var options = new MongoDbConnectorOptions(configuration);
        var factory = new MongoDbConnectorFactory(info, options, MongoDbTypeLocator.MongoClient);
        int timeout = 5000;

        if (options.Options.TryGetValue("connectTimeoutMS", out string value))
        {
            int.TryParse(value, out timeout);
        }

        var contributor = new MongoDbHealthContributor(factory, logger, timeout);

        return contributor;
    }

    public HealthCheckResult Health()
    {
        _logger?.LogTrace("Checking MongoDb connection health");
        var result = new HealthCheckResult();

        if (HostName != null)
        {
            result.Details.Add("host", HostName);
        }

        try
        {
            object databases = ReflectionHelpers.Invoke(MongoDbTypeLocator.ListDatabasesMethod, _mongoClient, new object[]
            {
                new CancellationTokenSource(Timeout).Token
            });

            if (databases == null)
            {
                throw new ConnectorException("Failed to open MongoDb connection!");
            }

            result.Details.Add("status", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Up;
            _logger?.LogTrace("MongoDb connection is up!");
        }
        catch (Exception e)
        {
            if (e is TargetInvocationException)
            {
                e = e.InnerException;
            }

            _logger?.LogError(e, "MongoDb connection is down! {HealthCheckException}", e.Message);
            result.Details.Add("error", $"{e.GetType().Name}: {e.Message}");
            result.Details.Add("status", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Down;
            result.Description = e.Message;
        }

        return result;
    }
}
