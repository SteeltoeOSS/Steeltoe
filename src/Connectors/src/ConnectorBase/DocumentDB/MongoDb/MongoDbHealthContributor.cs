// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
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

            var mongoDbImplementationType = MongoDbTypeLocator.MongoClient;
            var info = configuration.GetSingletonServiceInfo<MongoDbServiceInfo>();

            var rabbitMQConfig = new MongoDbConnectorOptions(configuration);
            var factory = new MongoDbConnectorFactory(info, rabbitMQConfig, mongoDbImplementationType);
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
                var databases = ReflectionHelpers.Invoke(MongoDbTypeLocator.ListDatabasesMethod, _mongoClient, new object[] { new CancellationTokenSource(5000) });

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
