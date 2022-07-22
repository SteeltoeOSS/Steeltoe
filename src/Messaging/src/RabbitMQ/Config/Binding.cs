// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class Binding : AbstractDeclarable, IBinding
{
    internal static IBinding Create(string bindingName, string destination, DestinationType destinationType, string exchange, string routingKey, Dictionary<string, object> arguments)
    {
        if (destinationType == DestinationType.EXCHANGE)
        {
            return new ExchangeBinding(bindingName, destination, exchange, routingKey, arguments);
        }

        return new QueueBinding(bindingName, destination, exchange, routingKey, arguments);
    }

    public enum DestinationType
    {
        QUEUE,
        EXCHANGE
    }

    public Binding(string bindingName)
        : base(null)
    {
        BindingName = ServiceName = bindingName;
    }

    public Binding(string bindingName, string destination, DestinationType destinationType, string exchange, string routingKey, Dictionary<string, object> arguments)
        : base(arguments)
    {
        BindingName = ServiceName = bindingName;
        Destination = destination;
        Type = destinationType;
        Exchange = exchange;
        RoutingKey = routingKey;
    }

    public string ServiceName { get; set; }

    public string Destination { get; set; }

    public string Exchange { get; set; }

    public string RoutingKey { get; set; }

    public DestinationType Type { get; set; }

    public bool IsDestinationQueue => Type == DestinationType.QUEUE;

    public string BindingName { get; set; }

    public override string ToString()
    {
        return "Binding [bindingName=" + BindingName + ", destination=" + Destination + ", exchange=" + Exchange + ", routingKey="
               + RoutingKey + ", arguments=" + Arguments + "]";
    }
}