// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;

namespace Steeltoe.Integration.Support.Converter;

public class ObjectStringMessageConverter : StringMessageConverter
{
    protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
    {
        object payload = message.Payload;

        if (payload is string || payload is byte[])
        {
            return base.ConvertFromInternal(message, targetClass, conversionHint);
        }

        return payload.ToString();
    }
}
