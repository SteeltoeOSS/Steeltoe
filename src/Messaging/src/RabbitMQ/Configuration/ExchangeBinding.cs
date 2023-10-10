// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Configuration;

public class ExchangeBinding : Binding, IExchangeBinding
{
    public ExchangeBinding(string bindingName)
        : base(bindingName)
    {
    }

    public ExchangeBinding(string name, string exchangeDestination, string exchange, string routingKey, Dictionary<string, object> arguments)
        : base(name, exchangeDestination, DestinationType.Exchange, exchange, routingKey, arguments)
    {
    }
}