// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class RabbitDestination
{
    public string QueueName { get; }

    public string RoutingKey
    {
        get
        {
            return QueueName;
        }
    }

    public string ExchangeName { get; }

    public RabbitDestination(string queueName)
    {
        QueueName = queueName;
    }

    public RabbitDestination(string exchangeName, string routingKey)
    {
        ExchangeName = exchangeName;
        QueueName = routingKey;
    }

    public static implicit operator string(RabbitDestination destination)
    {
        if (string.IsNullOrEmpty(destination.ExchangeName))
        {
            return destination.QueueName;
        }

        return $"{destination.ExchangeName}/{destination.QueueName}";
    }

    public static implicit operator RabbitDestination(string destination)
    {
        if (string.IsNullOrEmpty(destination))
        {
            return new RabbitDestination(string.Empty, string.Empty);
        }

        var result = ParseDestination(destination);
        return new RabbitDestination(result.ExchangeName, result.RoutingKey);
    }

    private static (string ExchangeName, string RoutingKey) ParseDestination(string destination)
    {
        var split = destination.Split('/');
        if (split.Length >= 2)
        {
            return (split[0], split[1]);
        }
        else
        {
            return (string.Empty, split[0]);
        }
    }
}
