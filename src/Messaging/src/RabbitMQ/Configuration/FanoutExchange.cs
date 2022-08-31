// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Configuration;

public class FanOutExchange : AbstractExchange, IFanOutExchange
{
    public override string Type { get; } = ExchangeType.FanOut;

    public FanOutExchange(string name)
        : base(name)
    {
    }

    public FanOutExchange(string name, bool durable, bool autoDelete)
        : base(name, durable, autoDelete)
    {
    }

    public FanOutExchange(string name, bool durable, bool autoDelete, Dictionary<string, object> arguments)
        : base(name, durable, autoDelete, arguments)
    {
    }
}
