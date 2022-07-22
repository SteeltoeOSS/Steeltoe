// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Connector.RabbitMQ;

public class RabbitMQHealthContributor : IHealthContributor
{
    public static IHealthContributor GetRabbitMQContributor(IConfiguration configuration, ILogger<RabbitMQHealthContributor> logger = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var rabbitMQImplementationType = RabbitMQTypeLocator.ConnectionFactory;

        var info = configuration.GetSingletonServiceInfo<RabbitMQServiceInfo>();

        var rabbitMQConfig = new RabbitMQProviderConnectorOptions(configuration);
        var factory = new RabbitMQProviderConnectorFactory(info, rabbitMQConfig, rabbitMQImplementationType);
        return new RabbitMQHealthContributor(factory, logger);
    }

    private readonly RabbitMQProviderConnectorFactory _factory;
    private readonly ILogger<RabbitMQHealthContributor> _logger;
    private readonly object _connFactory;
    private object _connection;

    public RabbitMQHealthContributor(RabbitMQProviderConnectorFactory factory, ILogger<RabbitMQHealthContributor> logger = null)
    {
        _factory = factory;
        _logger = logger;
        _connFactory = _factory.Create(null);
    }

    public string Id => "RabbitMQ";

    public HealthCheckResult Health()
    {
        _logger?.LogTrace("Checking RabbitMQ connection health");
        var result = new HealthCheckResult();
        try
        {
            _connection ??= ReflectionHelpers.Invoke(RabbitMQTypeLocator.CreateConnectionMethod, _connFactory, null);

            if (_connection == null)
            {
                throw new ConnectorException("Failed to open RabbitMQ connection!");
            }

            if (RabbitMQTypeLocator.IConnection.GetProperty("IsOpen").GetValue(_connection).Equals(false))
            {
                throw new ConnectorException("RabbitMQ connection is closed!");
            }

            try
            {
                var serverproperties = RabbitMQTypeLocator.IConnection.GetProperty("ServerProperties").GetValue(_connection) as Dictionary<string, object>;
                result.Details.Add("version", Encoding.UTF8.GetString(serverproperties["version"] as byte[]));
            }
            catch (Exception e)
            {
                _logger?.LogTrace(e, "Failed to find server version while checking RabbitMQ connection health");
            }

            result.Details.Add("status", HealthStatus.UP.ToString());
            result.Status = HealthStatus.UP;
            _logger?.LogTrace("RabbitMQ connection up!");
        }
        catch (Exception e)
        {
            if (e is TargetInvocationException)
            {
                e = e.InnerException;
            }

            _logger?.LogError("RabbitMQ connection down! {HealthCheckException}", e.Message);
            result.Details.Add("error", e.GetType().Name + ": " + e.Message);
            result.Details.Add("status", HealthStatus.DOWN.ToString());
            result.Status = HealthStatus.DOWN;
            result.Description = e.Message;
        }

        return result;
    }
}