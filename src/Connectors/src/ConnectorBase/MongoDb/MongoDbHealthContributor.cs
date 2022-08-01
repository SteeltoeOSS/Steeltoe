// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System.Reflection;
using Steeltoe.Common.Util;

namespace Steeltoe.Connector.MongoDb;

public class MongoDbHealthContributor : IHealthContributor
{
    public static IHealthContributor GetMongoDbHealthContributor(IConfiguration configuration, ILogger<MongoDbHealthContributor> logger = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var info = configuration.GetSingletonServiceInfo<MongoDbServiceInfo>();
        var mongoOptions = new MongoDbConnectorOptions(configuration);
        var factory = new MongoDbConnectorFactory(info, mongoOptions, MongoDbTypeLocator.MongoClient);
        var timeout = 5000;
        if (mongoOptions.Options.TryGetValue("connectTimeoutMS", out var value))
        {
            int.TryParse(value, out timeout);
        }

        var contributor = new MongoDbHealthContributor(factory, logger, timeout);

        return contributor;
    }

    private readonly ILogger<MongoDbHealthContributor> _logger;
    private readonly object _mongoClient;

    public string Id => "MongoDb";

    private int Timeout { get; set; }

    public MongoDbHealthContributor(MongoDbConnectorFactory factory, ILogger<MongoDbHealthContributor> logger = null, int timeout = 5000)
    {
        _logger = logger;
        _mongoClient = factory.Create(null);
        Timeout = timeout;
    }

    public HealthCheckResult Health()
    {
        _logger?.LogTrace("Checking MongoDb connection health");
        var result = new HealthCheckResult();
        try
        {
            var databases = ReflectionHelpers.Invoke(MongoDbTypeLocator.ListDatabasesMethod, _mongoClient, new object[] { new CancellationTokenSource(Timeout).Token });

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

            _logger?.LogError("MongoDb connection is down! {HealthCheckException}", e.Message);
            result.Details.Add("error", $"{e.GetType().Name}: {e.Message}");
            result.Details.Add("status", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Down;
            result.Description = e.Message;
        }

        return result;
    }
}
