// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support;

public class MutableIntegrationMessageBuilderFactory : IMessageBuilderFactory
{
    public IMessageBuilder<T> FromMessage<T>(IMessage<T> message)
    {
        return MutableIntegrationMessageBuilder<T>.FromMessage(message);
    }

    public IMessageBuilder FromMessage(IMessage message)
    {
        return MutableIntegrationMessageBuilder.FromMessage(message);
    }

    public IMessageBuilder<T> WithPayload<T>(T payload)
    {
        return MutableIntegrationMessageBuilder<T>.WithPayload(payload);
    }

    public IMessageBuilder WithPayload(object payload)
    {
        return MutableIntegrationMessageBuilder.WithPayload(payload);
    }
}
