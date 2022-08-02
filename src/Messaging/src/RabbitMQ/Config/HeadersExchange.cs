// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class HeadersExchange : AbstractExchange, IHeadersExchange
{
    public override string Type { get; } = ExchangeType.Headers;

    public HeadersExchange(string name)
        : base(name)
    {
    }

    public HeadersExchange(string name, bool durable, bool autoDelete)
        : base(name, durable, autoDelete)
    {
    }

    public HeadersExchange(string name, bool durable, bool autoDelete, Dictionary<string, object> arguments)
        : base(name, durable, autoDelete, arguments)
    {
    }
}
