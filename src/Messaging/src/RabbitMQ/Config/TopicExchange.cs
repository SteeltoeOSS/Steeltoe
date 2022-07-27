// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class TopicExchange : AbstractExchange, ITopicExchange
{
    public TopicExchange(string name)
        : base(name)
    {
    }

    public TopicExchange(string name, bool durable, bool autoDelete)
        : base(name, durable, autoDelete)
    {
    }

    public TopicExchange(string name, bool durable, bool autoDelete, Dictionary<string, object> arguments)
        : base(name, durable, autoDelete, arguments)
    {
    }

    public override string Type { get; } = ExchangeType.TOPIC;
}