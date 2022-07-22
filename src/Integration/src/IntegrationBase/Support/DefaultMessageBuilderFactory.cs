// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System.Collections.Generic;

namespace Steeltoe.Integration.Support;

public class DefaultMessageBuilderFactory : IMessageBuilderFactory
{
    public IList<string> ReadOnlyHeaders { get; set; } = new List<string>();

    public void AddReadOnlyHeaders(params string[] readOnlyHeaders)
    {
        foreach (var h in readOnlyHeaders)
        {
            ReadOnlyHeaders.Add(h);
        }
    }

    public IMessageBuilder<T> FromMessage<T>(IMessage<T> message)
    {
        return IntegrationMessageBuilder<T>.FromMessage(message)
            .ReadOnlyHeaders(ReadOnlyHeaders);
    }

    public IMessageBuilder FromMessage(IMessage message)
    {
        return IntegrationMessageBuilder.FromMessage(message)
            .ReadOnlyHeaders(ReadOnlyHeaders);
    }

    public IMessageBuilder<T> WithPayload<T>(T payload)
    {
        return IntegrationMessageBuilder<T>.WithPayload(payload)
            .ReadOnlyHeaders(ReadOnlyHeaders);
    }

    public IMessageBuilder WithPayload(object payload)
    {
        return IntegrationMessageBuilder.WithPayload(payload)
            .ReadOnlyHeaders(ReadOnlyHeaders);
    }
}