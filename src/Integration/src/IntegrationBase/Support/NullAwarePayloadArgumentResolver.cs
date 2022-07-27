// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;

namespace Steeltoe.Integration.Support;

public class NullAwarePayloadArgumentResolver : PayloadMethodArgumentResolver
{
    public NullAwarePayloadArgumentResolver(IMessageConverter messageConverter)
        : base(messageConverter, false)
    {
    }

    public NullAwarePayloadArgumentResolver(IMessageConverter messageConverter, bool useDefaultResolution)
        : base(messageConverter, useDefaultResolution)
    {
    }

    protected override bool IsEmptyPayload(object payload)
    {
        return base.IsEmptyPayload(payload) || "KafkaNull".Equals(payload.GetType().Name);
    }
}