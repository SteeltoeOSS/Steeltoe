// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using System;
using System.Reflection;
using System.Threading;

namespace Steeltoe.CloudFoundry.Connector.MongoDb
{
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
            return new MongoDbHealthContributor(factory, logger);
        }

        private readonly ILogger<MongoDbHealthContributor> _logger;
        private readonly object _mongoClient;

        public string Id => "MongoDb";

        public MongoDbHealthContributor(MongoDbConnectorFactory factory, ILogger<MongoDbHealthContributor> logger = null)
        {
            _logger = logger;
            _mongoClient = factory.Create(null);
        }

        public HealthCheckResult Health()
        {
            _logger?.LogTrace("Checking MongoDb connection health");
            var result = new HealthCheckResult();
            try
            {
                var databases = ConnectorHelpers.Invoke(MongoDbTypeLocator.ListDatabasesMethod, _mongoClient, new object[] { new CancellationTokenSource(5000).Token });

                if (databases == null)
                {
                    throw new ConnectorException("Failed to open MongoDb connection!");
                }

                result.Details.Add("status", HealthStatus.UP.ToString());
                result.Status = HealthStatus.UP;
                _logger?.LogTrace("MongoDb connection is up!");
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                {
                    e = e.InnerException;
                }

                _logger?.LogError("MongoDb connection is down! {HealthCheckException}", e.Message);
                result.Details.Add("error", e.GetType().Name + ": " + e.Message);
                result.Details.Add("status", HealthStatus.DOWN.ToString());
                result.Status = HealthStatus.DOWN;
                result.Description = e.Message;
            }

            return result;
        }
    }
}
