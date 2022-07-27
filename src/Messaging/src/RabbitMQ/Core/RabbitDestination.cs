// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class RabbitDestination
{
    private string _item1;
    private string _item2;

    public string QueueName
    {
        get
        {
            return _item2;
        }
    }

    public string RoutingKey
    {
        get
        {
            return _item2;
        }
    }

    public string ExchangeName
    {
        get
        {
            return _item1;
        }
    }

    public RabbitDestination(string queueName)
    {
        _item2 = queueName;
    }

    public RabbitDestination(string exchangeName, string routingKey)
    {
        _item1 = exchangeName;
        _item2 = routingKey;
    }

    public static implicit operator string(RabbitDestination destination)
    {
        if (string.IsNullOrEmpty(destination._item1))
        {
            return destination._item2;
        }

        return destination._item1 + "/" + destination._item2;
    }

    public static implicit operator RabbitDestination(string destination)
    {
        if (string.IsNullOrEmpty(destination))
        {
            return new RabbitDestination(string.Empty, string.Empty);
        }

        var result = ParseDestination(destination);
        return new RabbitDestination(result.Item1, result.Item2);
    }

    private static (string, string) ParseDestination(string destination)
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