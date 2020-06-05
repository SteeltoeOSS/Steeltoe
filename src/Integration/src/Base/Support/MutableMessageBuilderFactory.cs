// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support
{
    public class MutableMessageBuilderFactory : IMessageBuilderFactory
    {
        public IMessageBuilder<T> FromMessage<T>(IMessage<T> message)
        {
            return MutableMessageBuilder<T>.FromMessage(message);
        }

        public IMessageBuilder FromMessage(IMessage message)
        {
            return MutableMessageBuilder.FromMessage(message);
        }

        public IMessageBuilder<T> WithPayload<T>(T payload)
        {
            return MutableMessageBuilder<T>.WithPayload(payload);
        }

        public IMessageBuilder WithPayload(object payload)
        {
            return MutableMessageBuilder.WithPayload(payload);
        }
    }
}
